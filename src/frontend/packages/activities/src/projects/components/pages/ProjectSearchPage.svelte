<!-- Activity: Projects > Search -->
<script lang="ts">
  import { onDestroy, tick, untrack } from 'svelte';
  import { Badge, Button, FileIcon, Icon, IconButton, Input, TabBar } from '@sddp/ui';
  import {
    Dropdown,
    EmptyState,
    PageShell,
    PageBody,
    RichListItem,
    getTabState,
    setTabState,
  } from '@sddp/shell';
  import { authStore } from '@sddp/shell/auth/stores';
  import ProjectListLayout from '../../../shared/components/sections/ProjectListLayout.svelte';
  import { ApiRequestError, isApiAccessFailure } from '../../../shared/api/fetchWithAuth';
  import { createSearchSnippetSegments } from '../../../search/utils/highlight.utils';
  import {
    ENTITY_TYPE_STYLES,
    type EntityType,
  } from '../../../relationship/types/relationship.types';
  import { PROJECT_PAGE_CONFIG } from '../../types';
  import {
    searchProject,
    type ProjectSearchHit,
    type ProjectSearchOptions,
    type SearchMenu,
  } from '../../services/ProjectSearchService';
  import { openProjectSearchHit } from '../../routing/openProjectSearchHit';

  let {
    projectId,
    projectName = '',
    tabId = '',
  }: { projectId: string; projectName?: string; tabId?: string } = $props();
  const instance = $props.id();
  const menus = Object.entries(PROJECT_PAGE_CONFIG).filter(([id]) => id !== 'search');
  const areas = { all: '모든 필드', title: '제목·코드', body: '본문', path: '위치·메타데이터' };
  const sortLabels = { relevance: '관련도순', date: '최근 수정순' };
  const quick = ['all', 'specs', 'requirements', 'conversations', 'files'] as const;
  const menuTitles: Record<string, string> = {
    all: '전체',
    specs: '스펙',
    requirements: '요구사항',
    conversations: '대화',
    files: '파일',
  };
  const entities: Partial<Record<SearchMenu, EntityType>> = {
    specs: 'Spec',
    requirements: 'Requirement',
    conversations: 'Conversation',
    glossary: 'GlossaryTerm',
    artifacts: 'Artifact',
    diagrams: 'Diagram',
    tasks: 'Task',
  };
  const defaults: ProjectSearchOptions = {
    q: '',
    mode: 'AND',
    field: 'all',
    menu: 'all',
    sort: 'relevance',
  };
  type Saved = {
    owner: string;
    options: ProjectSearchOptions;
    shown: number;
    selected: string;
    scroll: number;
  };
  let options = $state({ ...defaults }),
    rows = $state<ProjectSearchHit[]>([]),
    counts = $state<Record<string, number>>({});
  let terms = $state<string[]>([]),
    nextOffset = $state<number | null>(null),
    loading = $state(false),
    loadingMore = $state(false);
  let unavailable = $state(false),
    error = $state(''),
    retryFirstPage = $state(false),
    selected = $state(''),
    restored = $state('');
  let queryInput = $state<HTMLInputElement>(),
    scrollPane: HTMLDivElement;
  let nextCursor: string | null = null;
  let wanted = 10,
    restoreScroll = 0,
    controller: AbortController | undefined;
  const tenant = $derived($authStore.user?.tenantId || ''),
    actor = $derived($authStore.user?.id || '');
  const key = $derived(tabId || `project-${projectId}-search`),
    owner = $derived(JSON.stringify([tenant, actor, projectId])),
    viewKey = $derived(JSON.stringify([owner, key]));
  const text = $derived(options.q),
    mode = $derived(options.mode),
    scope = $derived(options.menu),
    sort = $derived(options.sort),
    area = $derived(options.field);
  const queryWords = $derived(text.match(/"[^"]*"|[()]|[^\s()"]+|"/gu) ?? []);
  const explicitOperators = $derived(queryWords.some((word) => /^(AND|OR)$/i.test(word)));
  const expressionText = $derived.by(() => {
    const parts: string[] = [];
    let afterOperand = false;
    for (const word of queryWords) {
      const operator = /^(AND|OR)$/i.test(word);
      if (afterOperand && !operator && word !== ')') parts.push(explicitOperators ? 'AND' : mode);
      parts.push(word);
      afterOperand = !operator && word !== '(';
    }
    return parts.join(' ');
  });
  const shown = $derived(rows.length);
  const total = $derived(
    scope === 'all' ? Object.values(counts).reduce((a, b) => a + b, 0) : (counts[scope] ?? 0)
  );
  const priorityQuick = $derived(
    quick.map((id) => ({
      id,
      label:
        menuTitles[id] +
        ' ' +
        (id === 'all' ? Object.values(counts).reduce((a, b) => a + b, 0) : (counts[id] ?? 0)),
    }))
  );

  function persist() {
    if (restored !== viewKey) return;
    setTabState<Saved>(key, {
      owner,
      options: { ...options },
      shown: Math.max(10, rows.length),
      selected,
      scroll: scrollPane?.scrollTop ?? 0,
    });
  }
  let timer: ReturnType<typeof setTimeout> | undefined;
  $effect(() => {
    const identity = viewKey,
      tabKey = key;
    untrack(() => {
      if (restored === identity) return;
      const saved = getTabState<Saved>(tabKey);
      const state = saved?.owner === owner ? saved : undefined;
      options = state?.options ?? { ...defaults };
      selected = state?.selected ?? '';
      wanted = state?.shown ?? 10;
      restoreScroll = state?.scroll ?? 0;
      restored = identity;
      startSearch();
    });
  });
  function change(key: string, value: string) {
    const names: Record<string, keyof ProjectSearchOptions> = {
      query: 'q',
      scope: 'menu',
      area: 'field',
      mode: 'mode',
      sort: 'sort',
    };
    const property = names[key];
    if (!property || options[property] === value) return;
    options = { ...options, [property]: value };
    wanted = 10;
    restoreScroll = 0;
    selected = '';
    if (scrollPane) scrollPane.scrollTop = 0;
    startSearch();
    persist();
  }
  async function load(
    offset: number,
    request: AbortController,
    state: ProjectSearchOptions,
    pid: string,
    tid: string
  ) {
    const page = await searchProject(
      pid,
      tid,
      state,
      offset,
      request.signal,
      offset ? nextCursor : null
    );
    if (request.signal.aborted) return;
    // A successful continuation can exclude a source that supplied rows on a previous page.
    // Re-read the first page so its rows and facets describe the same searchable sources.
    if (
      offset &&
      (page.filesUnavailable !== unavailable ||
        menus.some(([menu]) => (page.counts[menu as SearchMenu] ?? 0) < (counts[menu] ?? 0)))
    ) {
      selected = '';
      wanted = 10;
      restoreScroll = 0;
      startSearch();
      persist();
      return;
    }
    const seen = new Set(offset ? rows.map((row) => row.menu + '/' + row.key) : []);
    rows = [
      ...(offset ? rows : []),
      ...page.items.filter((row) => !seen.has(row.menu + '/' + row.key)),
    ];
    counts = page.counts;
    terms = page.terms;
    nextOffset = page.nextOffset;
    nextCursor = page.nextCursor;
    unavailable = page.filesUnavailable;
  }
  function clearResults() {
    rows = [];
    counts = {};
    terms = [];
    nextOffset = null;
    nextCursor = null;
    unavailable = false;
  }
  function searchFailed(cause: unknown) {
    const lostAccess =
      isApiAccessFailure(cause) || (cause instanceof ApiRequestError && cause.status === 404);
    if (lostAccess) {
      clearResults();
      selected = '';
      persist();
    }
    retryFirstPage =
      !lostAccess &&
      !rows.length &&
      (!(cause instanceof ApiRequestError) ||
        cause.status >= 500 ||
        [408, 429].includes(cause.status));
    error = cause instanceof Error ? cause.message : '검색하지 못했습니다.';
  }
  function startSearch() {
    clearTimeout(timer);
    controller?.abort();
    const state = { ...options },
      pid = projectId,
      tid = tenant;
    const request = new AbortController();
    controller = request;
    clearResults();
    error = '';
    retryFirstPage = false;
    loadingMore = false;
    const active = !!state.q.trim() && !!actor && !!tid;
    loading = active;
    if (!active) return;
    timer = setTimeout(async () => {
      try {
        await load(0, request, state, pid, tid);
        while (!request.signal.aborted && nextOffset !== null && rows.length < wanted)
          await load(nextOffset, request, state, pid, tid);
        if (!request.signal.aborted) {
          await tick();
          if (scrollPane) scrollPane.scrollTop = restoreScroll;
          wanted = 10;
        }
      } catch (e) {
        if (!request.signal.aborted) searchFailed(e);
      } finally {
        if (!request.signal.aborted) loading = false;
      }
    }, 250);
  }
  onDestroy(() => {
    clearTimeout(timer);
    controller?.abort();
  });
  async function more() {
    const request = controller;
    if (loading || loadingMore || !request || request.signal.aborted || nextOffset === null) return;
    loadingMore = true;
    error = '';
    try {
      await load(nextOffset, request, { ...options }, projectId, tenant);
      if (!request.signal.aborted) persist();
    } catch (e) {
      if (!request.signal.aborted) searchFailed(e);
    } finally {
      if (!request.signal.aborted) loadingMore = false;
    }
  }
  function open(row: ProjectSearchHit) {
    selected = row.menu + '/' + row.key;
    persist();
    openProjectSearchHit(projectId, row);
  }
</script>

{#snippet highlight(value: string)}
  {#each createSearchSnippetSegments(value, terms) as part, index (index)}{#if part.highlighted}<mark
        >{part.text}</mark
      >{:else}{part.text}{/if}{/each}
{/snippet}
{#snippet choice(
  id: string,
  label: string,
  active: string,
  action: (id: string) => void,
  count: number | undefined = undefined
)}
  <Button
    variant="unstyled"
    role="menuitemradio"
    aria-checked={active === id}
    class="flex w-full items-center gap-2 px-3 py-1.5 text-sm text-left hover:bg-[var(--color-bg-tertiary)]"
    onclick={() => action(id)}
  >
    <span class="flex-1">{label}</span>
    {#if count !== undefined}<span class="text-xs tabular-nums text-[var(--color-text-tertiary)]"
        >{count}</span
      >{/if}
    {#if active === id}<Icon name="check" size="sm" class="text-[var(--color-accent-text)]" />{/if}
  </Button>
{/snippet}
<div class="workarea" bind:this={scrollPane} onscroll={persist}>
  <PageShell
    ><PageBody noPadding scrollable={false}>
      <div class="query-area">
        <div class="heading">
          <h2>프로젝트 검색</h2>
          <span class="micro-label">{projectName}</span><span class="scope-line"
            ><Icon name="layers" size="xs" />{menus.length}개 메뉴</span
          >
        </div>
        <div class="query-line" role="search" aria-label="프로젝트 전체 검색">
          <div class="relative flex-1 min-w-0">
            <label for={instance + '-q'} class="sr-only">프로젝트 검색어</label>
            <Icon
              name="search"
              size="sm"
              class="absolute left-2.5 top-1/2 -translate-y-1/2 text-[var(--color-accent-text)]"
            />
            <Input
              id={instance + '-q'}
              bind:element={queryInput}
              size="sm"
              variant="flat"
              type="text"
              placeholder="제목, 코드 또는 본문 검색"
              class="pl-8 pr-9 w-full h-9"
              value={text}
              maxlength={240}
              oninput={(e) => change('query', (e.currentTarget as HTMLInputElement).value)}
            />
            {#if text}<IconButton
                icon="x"
                size="sm"
                variant="ghost"
                title="검색어 지우기"
                class="absolute right-1 top-1/2 -translate-y-1/2"
                onclick={() => {
                  change('query', '');
                  queryInput?.focus();
                }}
              />{/if}
          </div>
          <Dropdown position="bottom-right">
            {#snippet trigger({ toggle, open })}
              <Button
                variant="outline"
                size="sm"
                aria-haspopup="menu"
                aria-expanded={open}
                aria-label="검색 필드 선택"
                onclick={toggle}
                ><span>{areas[area]}</span><Icon name="chevron-down" size="xs" /></Button
              >
            {/snippet}
            {#each Object.entries(areas) as [id, label] (id)}{@render choice(
                id,
                label,
                area,
                (id) => change('area', id)
              )}{/each}
          </Dropdown>
        </div>
        <div class="logic-row">
          {#if explicitOperators}
            <span class="micro-label">직접 입력한 검색식</span>
          {:else}
            <span class="micro-label">검색 조건</span>
            <TabBar
              size="sm"
              variant="pill"
              tabs={[
                { id: 'AND', label: '모두 포함 (AND)' },
                { id: 'OR', label: '하나 이상 (OR)' },
              ]}
              activeTab={mode}
              onchange={(value) => change('mode', value)}
            />
          {/if}
          {#if expressionText}<code class="expression" title={expressionText}>{expressionText}</code
            >{/if}
        </div>
        {#if error}<div class="query-error" role="alert">
            <Icon name="alert-circle" size="sm" />{error}
            {#if retryFirstPage}
              <Button variant="outline" size="sm" onclick={startSearch}>검색 다시 시도</Button>
            {/if}
          </div>{/if}
      </div>
      <div class="facets">
        <div class="fast-wide">
          <TabBar
            tabs={priorityQuick}
            size="sm"
            activeTab={scope}
            onchange={(id) => change('scope', id)}
          />
        </div>
        <div class="fast-narrow">
          <TabBar
            tabs={priorityQuick.slice(0, 2)}
            size="sm"
            activeTab={scope}
            onchange={(id) => change('scope', id)}
          />
        </div>
        <Dropdown position="bottom-right" menuClass="max-h-80 overflow-y-auto">
          {#snippet trigger({ toggle, open })}
            <Button
              size="sm"
              variant="ghost"
              aria-label="전체 메뉴 목록"
              aria-expanded={open}
              aria-haspopup="menu"
              onclick={toggle}
              >{quick.some((id) => id === scope)
                ? '메뉴 전체'
                : scope === 'all'
                  ? '전체'
                  : PROJECT_PAGE_CONFIG[scope].label}<Icon name="chevron-down" size="xs" /></Button
            >
          {/snippet}
          {@render choice(
            'all',
            '전체',
            scope,
            (id) => change('scope', id),
            Object.values(counts).reduce((a, b) => a + b, 0)
          )}
          {#each menus as [id, m] (id)}{@render choice(
              id,
              m.label,
              scope,
              (id) => change('scope', id),
              counts[id] ?? 0
            )}{/each}
        </Dropdown>
      </div>
      {#if unavailable}<p
          class="px-3 py-1 text-xs text-[var(--color-text-secondary)]"
          role="status"
        >
          저장소에 연결할 수 없어 파일 본문과 경로는 검색에서 제외됐습니다.
        </p>{/if}
      <ProjectListLayout lines={3}>
        <div class="result-head">
          <div class="outcome" role="status">
            <h3>
              {scope === 'all'
                ? '검색 결과'
                : (menuTitles[scope] || PROJECT_PAGE_CONFIG[scope].label) + ' 검색 결과'}
            </h3>
            <strong>{loading ? '…' : total}</strong>{#if !error && rows.length}<span
                class="page-range">1–{shown} 표시</span
              >{/if}
          </div>
          <Dropdown position="bottom-right">
            {#snippet trigger({ toggle, open })}
              <Button
                variant="ghost"
                size="sm"
                aria-label="검색 결과 정렬"
                aria-haspopup="menu"
                aria-expanded={open}
                onclick={toggle}>{sortLabels[sort]}<Icon name="chevron-down" size="xs" /></Button
              >
            {/snippet}
            {#each Object.entries(sortLabels) as [id, label] (id)}{@render choice(
                id,
                label,
                sort,
                (id) => change('sort', id)
              )}{/each}
          </Dropdown>
        </div>
        {#each rows as row (row.menu + '/' + row.key)}
          {@const style = entities[row.menu] ? ENTITY_TYPE_STYLES[entities[row.menu]!] : undefined}
          <div data-key={row.key} class="hit">
            <RichListItem
              title={row.title}
              selected={selected === row.menu + '/' + row.key}
              titleClass="font-semibold"
              onclick={() => open(row)}
            >
              {#snippet leading()}
                <span
                  class={'type-icon ' +
                    (style?.color || 'text-[var(--color-text-secondary)]') +
                    ' ' +
                    (style?.bgColor || 'bg-[var(--color-bg-secondary)]')}
                >
                  {#if row.menu === 'files'}<FileIcon path={row.location} size="sm" />{:else}<Icon
                      name={PROJECT_PAGE_CONFIG[row.menu].icon}
                      size="sm"
                    />{/if}
                </span>
              {/snippet}
              {#snippet titleContent()}{@render highlight(row.title)}{/snippet}
              {#snippet trailing()}
                <span class="status-block">
                  {#if ['승인', '게시', 'Approved', 'Published'].includes(row.status)}<Badge
                      variant="success"
                      size="sm">{row.status}</Badge
                    >{:else if ['검토 중', '진행 중', 'InReview', 'InProgress'].includes(row.status)}<Badge
                      variant="warning"
                      size="sm">{row.status}</Badge
                    >{:else}<span>{row.status}</span>{/if}
                  {#if row.version}<span class="rev">{row.version}</span>{/if}
                </span>
              {/snippet}
              {#snippet badges()}
                <div class="row-lines">
                  <span class="excerpt">{@render highlight(row.snippet)}</span>
                  <span class="origin"
                    ><b>{PROJECT_PAGE_CONFIG[row.menu].label}</b><i>›</i>{#if row.location}<span
                        class="truncate">{@render highlight(row.location)}</span
                      ><i>›</i>{/if}<span class="truncate"
                      >{@render highlight(row.line ? row.line + '행' : row.code || row.kind)}</span
                    ></span
                  >
                </div>
              {/snippet}
              {#snippet footerMeta()}<span class="updated"
                  >{row.updatedAt.slice(0, 10).replaceAll('-', '.')}</span
                >{/snippet}
            </RichListItem>
          </div>
        {:else}
          {#if !error && !loading}
            <EmptyState
              icon="search"
              heading={text.trim() ? '일치하는 결과가 없습니다' : '어떤 내용을 찾으시나요?'}
              subtext={text.trim()
                ? '검색어를 바꾸거나 검색 범위를 넓혀 보세요.'
                : '검색어를 입력하세요. 예: 토큰 AND (만료 OR 갱신)'}
            />
          {/if}
        {/each}
        {#if nextOffset !== null}
          <div class="pager">
            <span>{shown} / {total}건</span><Button
              variant="secondary"
              size="sm"
              disabled={loading || loadingMore}
              onclick={more}
              >{loadingMore ? '불러오는 중…' : '결과 더 보기'}
              <Icon name="chevron-down" size="xs" /></Button
            >
          </div>
        {/if}
      </ProjectListLayout>
    </PageBody></PageShell
  >
</div>

<style>
  .workarea {
    height: 100%;
    overflow: auto;
    color: var(--color-text-primary);
    background: var(--color-bg-primary);
    container-type: inline-size;
    min-width: 0;
  }
  .scope-line,
  .micro-label,
  .page-range,
  .expression,
  .updated,
  .status-block,
  .pager {
    font-size: var(--text-xs);
    color: var(--color-text-tertiary);
  }
  .scope-line,
  .outcome,
  .logic-row,
  .query-line,
  .query-error,
  .facets,
  .result-head,
  .pager,
  .heading,
  .status-block {
    display: flex;
    align-items: center;
  }
  .scope-line {
    gap: var(--space-2);
    white-space: nowrap;
  }

  .origin i {
    color: var(--color-border-hover);
    font-style: normal;
  }

  .query-area {
    padding: var(--space-5) var(--space-6) var(--space-3);
    border-bottom: 1px solid var(--color-border-primary);
    background: var(--color-bg-primary);
  }
  .heading {
    gap: var(--space-3);
    margin-bottom: var(--space-3);
  }
  .heading h2 {
    margin: 0;
    font-size: var(--text-2xl);
    font-weight: 650;
    letter-spacing: -0.035em;
  }
  .scope-line {
    padding-inline-start: var(--space-3);
    border-inline-start: 1px solid var(--color-border-secondary);
  }
  .query-line {
    min-height: var(--space-12);
    gap: var(--space-2);
  }
  .logic-row {
    gap: var(--space-2);
    flex-wrap: wrap;
    margin-top: var(--space-1);
  }
  .micro-label,
  .page-range {
    white-space: nowrap;
  }
  .expression {
    margin-inline-start: auto;
    font-family: var(--font-mono, monospace);
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
    max-width: 100%;
  }
  .query-error {
    gap: var(--space-2);
    color: var(--color-error-600);
    font-size: var(--text-sm);
    margin-top: var(--space-2);
  }
  .facets {
    justify-content: space-between;
    gap: var(--space-1);
    padding-inline: var(--space-3);
    border-bottom: 1px solid var(--color-border-primary);
    background: var(--color-bg-primary);
  }
  .fast-narrow {
    display: none;
  }
  .result-head {
    justify-content: space-between;
    gap: var(--space-2);
    padding: var(--space-2) var(--space-6);
    background: var(--color-bg-secondary);
    border-bottom: 1px solid var(--color-border-primary);
  }
  .outcome {
    gap: var(--space-2);
  }
  .outcome h3,
  .outcome strong {
    font-size: var(--text-sm);
    font-weight: 600;
  }
  .outcome strong {
    color: var(--color-accent-text);
    font-variant-numeric: tabular-nums;
  }
  .outcome h3 {
    margin: 0;
  }
  .page-range {
    border-inline-start: 1px solid var(--color-border-secondary);
    padding-inline-start: var(--space-2);
  }
  .hit :global(.rich-list-item) {
    padding-inline: var(--space-6);
  }
  .type-icon {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    width: var(--space-7);
    height: var(--space-7);
    border-radius: var(--space-1);
    margin-inline-end: var(--space-1);
  }
  .status-block {
    margin-inline-start: auto;
    gap: var(--space-2);
    white-space: nowrap;
  }
  .rev {
    font-family: var(--font-mono, monospace);
  }
  .row-lines {
    min-width: 0;
    display: grid;
    gap: var(--space-1);
    text-align: start;
  }
  .hit :global(.rich-list-item .flex.items-center.justify-between.gap-2.mt-1\.5 > :first-child) {
    min-width: 0;
    flex: 1;
  }
  .excerpt,
  .origin {
    overflow: hidden;
    white-space: nowrap;
    text-overflow: ellipsis;
  }
  .excerpt {
    font-size: var(--text-sm);
    color: var(--color-text-secondary);
  }
  .origin {
    display: flex;
    align-items: center;
    gap: var(--space-1-5);
    font-size: var(--text-xs);
    color: var(--color-text-tertiary);
  }
  .origin b {
    font-weight: 500;
    flex-shrink: 0;
  }
  mark {
    color: inherit;
    background: var(--color-warning-100);
    border-radius: 2px;
  }
  :global(.dark) mark {
    color: var(--color-text-primary);
    background: var(--color-warning-12);
  }
  .updated,
  .pager {
    font-variant-numeric: tabular-nums;
  }
  .updated {
    white-space: nowrap;
  }
  .pager {
    justify-content: space-between;
    gap: var(--space-2);
    padding: var(--space-3) var(--space-6);
    background: var(--color-bg-secondary);
  }
  @container (max-width:600px) {
    .fast-wide {
      display: none;
    }
    .fast-narrow {
      display: block;
    }
    .rev,
    .updated {
      display: none;
    }
    .query-area {
      padding-inline: var(--space-3);
    }
    .result-head,
    .hit :global(.rich-list-item),
    .pager {
      padding-inline: var(--space-3);
    }
    .expression {
      margin-inline-start: 0;
    }
  }
  @container (max-width:400px) {
    .status-block,
    .page-range {
      display: none;
    }
    .facets {
      padding-inline: var(--space-1);
    }
    .logic-row {
      row-gap: var(--space-1);
    }
    .expression {
      flex-basis: 100%;
    }
    .type-icon {
      width: var(--space-6);
      height: var(--space-6);
      margin-inline-end: 0;
    }
  }
</style>
