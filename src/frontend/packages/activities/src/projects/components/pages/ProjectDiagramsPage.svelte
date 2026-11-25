<!-- Activity: Projects > Diagrams -->
<script lang="ts">
  import { onDestroy, untrack } from 'svelte';
  import { Icon, IconButton, Button, Input, Spinner } from '@sddp/ui';
  import {
    PageShell,
    PageHeader,
    PageBody,
    SidebarDetailLayout,
    EmptyState,
    getTabState,
    setTabState,
    Dropdown,
    RichListItem,
  } from '@sddp/shell';
  import { getAuthState } from '@sddp/shell/auth';
  import { authStore } from '@sddp/shell/auth';
  import { createProjectCapabilityStore } from '../../../shared/state/projectCapabilities';
  import DiagramWorkbench from '../../../diagrams/components/sections/DiagramWorkbench2.svelte';
  import ProjectListLayout from '../../../shared/components/sections/ProjectListLayout.svelte';
  import EntityFullDetailTab from '../../../shared/components/sections/EntityFullDetailTab.svelte';
  import { useDetailRoute } from '../../routing/useDetailRoute.svelte';
  import { listDiagrams } from '../../../diagrams/services/DiagramService';
  import type { DiagramListItemDto } from '../../../diagrams/types';
  import type { ProjectDetail } from '../../types';

  let {
    projectId,
    projectName = '',
    project,
    tabId = '',
    class: className = '',
  }: {
    projectId: string;
    projectName?: string;
    project?: ProjectDetail;
    tabId?: string;
    class?: string;
  } = $props();
  const instanceId = $props.id();
  const searchInputId = `diagram-sidebar-search-${instanceId}`;
  type KindFilter = 'all' | DiagramListItemDto['kind'];
  interface TabState {
    projectId: string;
    search: string;
    selectedId: string | null;
    creating?: boolean;
    kindFilter?: KindFilter;
  }
  let rows = $state<DiagramListItemDto[]>([]);
  let selectedId = $state<string | null>(null);
  let creating = $state(false);
  let detail: { confirmLeave: () => Promise<boolean> } | undefined = $state();
  let creator: { confirmLeave: () => Promise<boolean> } | undefined = $state();
  const capabilities = createProjectCapabilityStore();
  const tenant = $derived($authStore.user?.tenantId ?? '');
  const actor = $derived($authStore.user?.id ?? '');
  const capabilityScope = $derived(JSON.stringify([projectId, tenant, actor]));
  $effect(() => {
    const [project, tenantId, user] = JSON.parse(capabilityScope) as [string, string, string];
    untrack(() => {
      void capabilities.load(project, tenantId, user);
    });
    return () => capabilities.clear();
  });
  const canCreate = $derived(
    $capabilities.userId === actor &&
      $capabilities.data?.tenantId === tenant &&
      $capabilities.data.projectId === projectId &&
      $capabilities.data.canWrite &&
      $capabilities.data.permissions.includes('diagram:create')
  );
  let search = $state('');
  let kindFilter = $state<KindFilter>('all');
  const visibleRows = $derived(
    kindFilter === 'all' ? rows : rows.filter((row) => row.kind === kindFilter)
  );
  const kindOptions = [
    { value: 'all', label: 'All' },
    { value: 'authored', label: 'Authored' },
    { value: 'derived', label: 'Derived' },
  ] as const;
  let page = $state(0);
  let total = $state(0);
  let loading = $state(false);
  let loadingMore = $state(false);
  let error = $state<string | null>(null);
  let restoredScope = $state('');
  let detailRefresh = $state(0);
  let generation = 0;
  onDestroy(() => {
    ++generation;
  });
  const stateKey = $derived(tabId || `project-${projectId}-diagrams`);
  const scopeKey = $derived(JSON.stringify([getAuthState().user?.tenantId, projectId, stateKey]));

  $effect(() => {
    const scope = scopeKey;
    untrack(() => {
      if (restoredScope === scope) return;
      ++generation;
      rows = [];
      total = 0;
      page = 0;
      error = null;
      const saved = getTabState<TabState>(stateKey);
      search = saved?.projectId === projectId ? saved.search : '';
      kindFilter =
        saved?.projectId === projectId &&
        (saved.kindFilter === 'authored' || saved.kindFilter === 'derived')
          ? saved.kindFilter
          : 'all';
      selectedId = saved?.projectId === projectId ? saved.selectedId : null;
      creating = saved?.projectId === projectId && !!saved.creating;
      restoredScope = scope;
    });
  });
  $effect(() => {
    if (restoredScope !== scopeKey) return;
    const state = { projectId, search, selectedId, creating, kindFilter };
    untrack(() => setTabState(stateKey, { ...getTabState(stateKey), ...state }));
  });
  $effect(() => {
    const query = search;
    const scope = scopeKey;
    if (restoredScope !== scope) return;
    untrack(() => {
      void load(true, query);
    });
  });

  async function load(reset: boolean, query = search) {
    const request = ++generation;
    const tenant = getAuthState().user?.tenantId ?? '';
    const pid = projectId;
    const scope = scopeKey;
    const nextPage = reset ? 1 : page + 1;
    if (reset) {
      rows = [];
      total = 0;
      loading = true;
    } else loadingMore = true;
    error = null;
    try {
      const result = await listDiagrams(tenant, pid, nextPage, query);
      if (request !== generation || scope !== scopeKey) return;
      rows = reset ? result.items : [...rows, ...result.items];
      total = result.totalCount;
      page = result.page;
    } catch (err) {
      if (request !== generation || scope !== scopeKey) return;
      rows = [];
      total = 0;
      error = err instanceof Error ? err.message : 'Failed to load diagrams';
    } finally {
      if (request === generation && scope === scopeKey) {
        loading = false;
        loadingMore = false;
      }
    }
  }
  async function canLeave() {
    return (creating ? creator?.confirmLeave() : detail?.confirmLeave()) ?? true;
  }
  async function select(id: string | null) {
    if (id === selectedId && !creating) return true;
    if (!(await canLeave())) return false;
    creating = false;
    selectedId = id;
    return true;
  }
  async function refresh() {
    if (!(await canLeave())) return;
    ++detailRefresh;
    void load(true);
  }
  useDetailRoute({
    tabId: () => tabId,
    path: () => `/project/${projectId}/diagrams`,
    selection: () => selectedId,
    ready: () => restoredScope === scopeKey,
    restore: select,
  });
</script>

<PageShell class={className}>
  <PageHeader title="Diagrams" meta={project?.name || projectName || undefined} {loading}>
    {#snippet actions()}
      <IconButton
        icon="refresh-cw"
        size="sm"
        variant="ghost"
        title="Refresh diagrams"
        onclick={refresh}
      />
      {#if canCreate}<IconButton
          icon="plus"
          size="sm"
          variant="ghost"
          title="New diagram"
          onclick={async () => {
            if (await canLeave()) {
              selectedId = null;
              creating = true;
            }
          }}
        />{/if}
    {/snippet}
  </PageHeader>
  <PageBody class="p-0">
    <SidebarDetailLayout>
      {#snippet sidebar()}
        <ProjectListLayout lines={2}>
          <div
            class="project-list-search flex-shrink-0 flex items-center p-2 min-h-12 border-b border-[var(--color-border-primary)]"
            role="search"
            aria-label="Diagram search and filters"
          >
            <div class="flex items-center gap-1 w-full">
              <div class="relative flex-1">
                <label for={searchInputId} class="sr-only">Search diagrams</label>
                <Icon
                  name="search"
                  size="sm"
                  class="absolute left-2.5 top-1/2 -translate-y-1/2 text-[var(--color-text-tertiary)]"
                />
                <Input
                  type="text"
                  placeholder="Search diagrams..."
                  id={searchInputId}
                  size="sm"
                  variant="flat"
                  class="pl-8 w-full"
                  value={search}
                  oninput={(e) => (search = (e.target as HTMLInputElement).value)}
                />
              </div>
              <Dropdown position="bottom-right">
                {#snippet trigger({ toggle, open })}
                  <IconButton
                    onclick={toggle}
                    ariaHasPopup
                    ariaExpanded={open}
                    icon="more-vertical"
                    size="sm"
                    variant="ghost"
                    title="Filter options"
                  />
                {/snippet}
                <div class="py-1 min-w-[180px]">
                  <div
                    class="px-3 py-1 text-xs font-semibold text-[var(--color-text-tertiary)] uppercase"
                  >
                    Kind
                  </div>
                  {#each kindOptions as option (option.value)}
                    <Button
                      variant="unstyled"
                      role="menuitemradio"
                      aria-checked={kindFilter === option.value}
                      class="w-full flex items-center gap-2 px-3 py-1.5 text-sm text-left transition-colors {kindFilter ===
                      option.value
                        ? 'bg-[var(--color-accent-primary)]/10 text-[var(--color-accent-text)]'
                        : 'text-[var(--color-text-secondary)] hover:bg-[var(--color-bg-tertiary)]'}"
                      onclick={() => (kindFilter = option.value)}
                    >
                      <span class="flex-1">{option.label}</span>
                      {#if kindFilter === option.value}<Icon
                          name="check"
                          size="sm"
                          class="text-[var(--color-accent-text)]"
                        />{/if}
                    </Button>
                  {/each}
                </div>
              </Dropdown>
            </div>
          </div>
          <div
            class="project-list-heading flex items-center gap-2 px-3 py-1.5 border-b border-[var(--color-border-primary)] bg-[var(--color-bg-secondary)]"
            role="group"
            aria-label="Diagram list columns"
          >
            <span class="w-4"></span>
            <span class="flex-1 text-xs font-medium text-[var(--color-text-tertiary)] uppercase"
              >Title</span
            >
            <span
              class="w-12 text-xs font-medium text-[var(--color-text-tertiary)] uppercase text-right"
              >Rev</span
            >
          </div>
          <div class="flex-1 overflow-y-auto" aria-label="Diagram list">
            {#if loading}<div class="flex justify-center p-5"><Spinner size="sm" /></div>
            {:else if error}
              <div class="p-4 text-sm" role="alert">
                <p>{error}</p>
                <Button variant="secondary" size="sm" onclick={() => load(true)}>Retry</Button>
              </div>
            {:else}
              {#each visibleRows as row (row.id)}
                <RichListItem
                  selected={selectedId === row.id}
                  title={row.title}
                  leadingIcon="git-branch"
                  onclick={() => select(row.id)}
                >
                  {#snippet trailing()}
                    <span
                      class="ml-auto w-12 text-right flex-shrink-0 whitespace-nowrap text-xs text-[var(--color-text-tertiary)]"
                      title={`Revision ${row.revision}`}>v{row.revision}</span
                    >
                  {/snippet}
                  {#snippet badges()}
                    <span class="text-xs text-[var(--color-text-tertiary)]"
                      >{row.kind === 'authored' ? 'Authored' : 'Derived'}</span
                    >
                  {/snippet}
                </RichListItem>
              {/each}
              {#if visibleRows.length === 0}
                <div class="p-4 text-center text-sm text-[var(--color-text-tertiary)]">
                  <Icon name="inbox" size="lg" class="mx-auto mb-2 opacity-50" />
                  <p>
                    {search || kindFilter !== 'all'
                      ? rows.length < total
                        ? 'Load more diagrams to continue filtering.'
                        : 'No matching diagrams'
                      : 'No diagrams found'}
                  </p>
                </div>
              {/if}
              {#if rows.length < total}<div class="p-2">
                  <Button
                    variant="secondary"
                    size="sm"
                    disabled={loadingMore}
                    onclick={() => load(false)}>{loadingMore ? 'Loading…' : 'Load more'}</Button
                  >
                </div>{/if}
            {/if}
          </div>
        </ProjectListLayout>
      {/snippet}
      {#if creating}
        <div class="h-full overflow-auto">
          <DiagramWorkbench
            bind:this={creator}
            {projectId}
            {tabId}
            onsaved={(saved) => {
              creating = false;
              selectedId = saved.id;
              void load(true);
            }}
          />
        </div>
      {:else if selectedId}
        <EntityFullDetailTab
          bind:this={detail}
          entityType="Diagram"
          entityId={selectedId}
          {projectId}
          {tabId}
          refreshKey={detailRefresh}
          ondiagramsaved={() => {
            void load(true);
          }}
        />
      {:else if !loading && !error}
        {#if !search && total === 0}
          <EmptyState icon="git-branch" heading="No diagrams yet" />
        {:else if visibleRows.length > 0}
          <EmptyState
            icon="mouse-pointer"
            iconSize="lg"
            heading="Select a diagram"
            subtext="Choose a diagram from the sidebar to view details."
          />
        {/if}
      {/if}
    </SidebarDetailLayout>
  </PageBody>
</PageShell>
