<!-- Section: MemberOwnershipPanel — Projects > Effort -->
<script lang="ts">
  import { SurfaceCard, formatDate } from '@sddp/shell';
  import { Button, Input, IconButton, Icon, Select, Spinner } from '@sddp/ui';
  import { navigateToGlossary, navigateToRequirement, navigateToSpec } from '@sddp/shell/core/services';
  import { loadMemberOwnership } from '../../stores';
  import type {
    MemberOwnershipPage,
    OwnershipFilter,
    OwnershipItemType,
    MemberOwnershipItem,
  } from '../../types';

  interface Props {
    ownership: MemberOwnershipPage;
    userId: string;
    isLoading?: boolean;
    showHeader?: boolean;
    class?: string;
  }

  let {
    ownership,
    userId,
    isLoading = false,
    showHeader = true,
    class: className = '',
  }: Props = $props();

  let searchQuery = $state('');
  let filter = $state<OwnershipFilter>('all');
  let pageSize = $state(8);
  let page = $state(1);
  let searchTimer: ReturnType<typeof setTimeout> | null = null;

  const totalPages = $derived(Math.max(1, Math.ceil(ownership.totalCount / pageSize)));

  function setFilter(next: OwnershipFilter): void {
    filter = next;
    page = 1;
    reloadOwnership();
  }

  function setPageSize(value: number): void {
    pageSize = value;
    page = 1;
    reloadOwnership();
  }

  function setPage(next: number): void {
    page = next;
    reloadOwnership();
  }

  function getTypeLabel(type: OwnershipItemType): string {
    if (type === 'requirements') return 'Requirement';
    if (type === 'specs') return 'Spec';
    if (type === 'glossary') return 'Glossary';
    return 'Artifact';
  }

  function resolveOpenHandler(item: MemberOwnershipItem): (() => void) | undefined {
    if (item.type === 'requirements') {
      return () => navigateToRequirement(item.id, item.title);
    }
    if (item.type === 'specs') {
      return () => navigateToSpec(item.specId ?? item.id, item.specCode ?? item.title);
    }
    if (item.type === 'glossary') {
      return () => navigateToGlossary(item.id, item.title);
    }
    if (item.type === 'artifacts' && item.specId) {
      return () => navigateToSpec(item.specId!, item.specCode ?? item.title);
    }
    return undefined;
  }

  function reloadOwnership(): void {
    if (!userId || isLoading) return;
    loadMemberOwnership(userId, {
      type: filter,
      query: searchQuery.trim(),
      page,
      pageSize,
    });
  }

  function scheduleSearch(): void {
    page = 1;
    if (searchTimer) {
      clearTimeout(searchTimer);
    }
    searchTimer = setTimeout(() => {
      reloadOwnership();
    }, 250);
  }

  $effect(() => {
    searchQuery = ownership.query ?? '';
    filter = ownership.filter ?? 'all';
    page = ownership.page ?? 1;
    pageSize = ownership.pageSize ?? 8;
  });
</script>

<SurfaceCard class="flex flex-col gap-3 {className}">
  <div class="ownership-panel__header {showHeader ? '' : 'ownership-panel__header--compact'}">
    {#if showHeader}
      <div>
        <h4 class="ownership-panel__title surface-card__title">Owned Deliverables</h4>
        <span class="ownership-panel__count surface-card__meta">{ownership.counts.total} items</span>
      </div>
    {/if}
    {#if isLoading}
      <div class="ownership-panel__loading">
        <Spinner size="sm" />
        <span>Loading...</span>
      </div>
    {/if}
    <div class="ownership-panel__filters">
      <Button
        variant="unstyled"
        class="ownership-panel__filter-button {filter === 'all' ? 'ownership-panel__filter-button--active' : ''}"
        onclick={() => setFilter('all')}
        disabled={isLoading}
      >
        All ({ownership.counts.total})
      </Button>
      <Button
        variant="unstyled"
        class="ownership-panel__filter-button {filter === 'requirements' ? 'ownership-panel__filter-button--active' : ''}"
        onclick={() => setFilter('requirements')}
        disabled={isLoading}
      >
        Requirements ({ownership.counts.requirements})
      </Button>
      <Button
        variant="unstyled"
        class="ownership-panel__filter-button {filter === 'specs' ? 'ownership-panel__filter-button--active' : ''}"
        onclick={() => setFilter('specs')}
        disabled={isLoading}
      >
        Specs ({ownership.counts.specs})
      </Button>
      <Button
        variant="unstyled"
        class="ownership-panel__filter-button {filter === 'glossary' ? 'ownership-panel__filter-button--active' : ''}"
        onclick={() => setFilter('glossary')}
        disabled={isLoading}
      >
        Glossary ({ownership.counts.glossaryTerms})
      </Button>
      <Button
        variant="unstyled"
        class="ownership-panel__filter-button {filter === 'artifacts' ? 'ownership-panel__filter-button--active' : ''}"
        onclick={() => setFilter('artifacts')}
        disabled={isLoading}
      >
        Artifacts ({ownership.counts.artifacts})
      </Button>
    </div>
  </div>

  <div class="ownership-panel__toolbar">
    <Input
      type="search"
      placeholder="Search owned deliverables..."
      bind:value={searchQuery}
      size="sm"
      oninput={scheduleSearch}
      disabled={isLoading}
    />
    <div class="ownership-panel__paging">
      <Select
        unstyled
        class="ownership-panel__select"
        value={String(pageSize)}
        onchange={(value) => setPageSize(Number(value))}
        disabled={isLoading}
      >
        <option value="6">6 / page</option>
        <option value="8">8 / page</option>
        <option value="12">12 / page</option>
      </Select>
      <div class="ownership-panel__pager">
        <IconButton
          icon="chevron-left"
          size="sm"
          variant="ghost"
          onclick={() => setPage(Math.max(1, page - 1))}
          disabled={page <= 1 || isLoading}
          title="Previous page"
        />
        <span class="ownership-panel__page-info">{page} / {totalPages}</span>
        <IconButton
          icon="chevron-right"
          size="sm"
          variant="ghost"
          onclick={() => setPage(Math.min(totalPages, page + 1))}
          disabled={page >= totalPages || isLoading}
          title="Next page"
        />
      </div>
    </div>
  </div>

  {#if ownership.items.length === 0}
    <div class="ownership-panel__empty">No owned deliverables found.</div>
  {:else}
    <div class="ownership-panel__list">
      {#each ownership.items as item (`${item.type}-${item.id}`)}
        {@const onOpen = resolveOpenHandler(item)}
        <Button
          variant="unstyled"
          class="ownership-item {onOpen ? 'ownership-item--clickable' : ''}"
          onclick={() => onOpen?.()}
          disabled={!onOpen}
        >
          <div class="ownership-item__type">{getTypeLabel(item.type)}</div>
          <div class="ownership-item__content">
            <div class="ownership-item__title">{item.title}</div>
            {#if item.subtitle}
              <div class="ownership-item__meta">{item.subtitle}</div>
            {/if}
            {#if item.artifactPath}
              <div class="ownership-item__meta">{item.artifactPath}</div>
            {/if}
            <div class="ownership-item__meta">{formatDate(item.updatedAt)}</div>
          </div>
          {#if onOpen}
            <Icon
              name="external-link"
              size="xs"
              class="text-[var(--color-text-tertiary)]"
            />
          {/if}
        </Button>
      {/each}
    </div>
  {/if}
</SurfaceCard>

<style>
  .ownership-panel__header {
    display: flex;
    align-items: flex-start;
    justify-content: space-between;
    gap: 1rem;
  }

  .ownership-panel__header--compact {
    justify-content: flex-start;
  }

  .ownership-panel__title {
    margin: 0;
  }

  .ownership-panel__count {
    font-size: var(--text-xs);
  }

  .ownership-panel__loading {
    display: flex;
    align-items: center;
    gap: 0.35rem;
    font-size: var(--text-xs);
    color: var(--color-text-tertiary);
  }

  :global(.ownership-panel__loading svg) {
    animation: spin 1s linear infinite;
  }

  .ownership-panel__filters {
    display: flex;
    flex-wrap: wrap;
    gap: 0.35rem;
  }

  :global(.ownership-panel__filter-button) {
    border: 1px solid var(--color-border-secondary);
    background: var(--color-surface-50);
    color: var(--color-text-secondary);
    border-radius: 999px;
    padding: 0.2rem 0.6rem;
    font-size: var(--text-xs);
    cursor: pointer;
  }

  :global(.ownership-panel__filter-button--active) {
    border-color: var(--color-accent-primary);
    color: var(--color-text-primary);
    background: var(--color-accent-primary-muted, rgba(99, 102, 241, 0.08));
  }

  .ownership-panel__toolbar {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 0.6rem;
  }

  .ownership-panel__paging {
    display: flex;
    align-items: center;
    gap: 0.6rem;
  }

  :global(.ownership-panel__select) {
    border: 1px solid var(--color-border-secondary);
    background: var(--color-surface-50);
    color: var(--color-text-primary);
    border-radius: var(--radius-md, 6px);
    padding: 0.25rem 0.5rem;
    font-size: var(--text-xs);
  }

  .ownership-panel__pager {
    display: flex;
    align-items: center;
    gap: 0.35rem;
  }

  .ownership-panel__page-info {
    font-size: var(--text-xs);
    color: var(--color-text-tertiary);
  }

  .ownership-panel__list {
    display: flex;
    flex-direction: column;
    gap: 0.4rem;
  }

  .ownership-panel__empty {
    font-size: var(--text-xs);
    color: var(--color-text-tertiary);
  }

  :global(.ownership-item) {
    display: flex;
    align-items: center;
    gap: 0.75rem;
    border: 1px solid var(--color-border-secondary);
    background-color: var(--color-surface-50);
    border-radius: var(--radius-md, 6px);
    padding: 0.5rem 0.75rem;
    text-align: left;
    width: 100%;
    transition: background-color 150ms ease, border-color 150ms ease;
  }

  :global(.ownership-item--clickable) {
    cursor: pointer;
  }

  :global(.ownership-item--clickable:hover) {
    border-color: var(--color-accent-primary);
    background-color: var(--color-surface-200);
  }

  :global(.ownership-item:disabled) {
    cursor: default;
    opacity: 0.7;
  }

  .ownership-item__type {
    font-size: var(--text-2xs, 0.65rem);
    text-transform: uppercase;
    letter-spacing: 0.04em;
    color: var(--color-text-tertiary);
    border: 1px solid var(--color-border-secondary);
    background: var(--color-surface-50);
    padding: 0.1rem 0.4rem;
    border-radius: 999px;
    white-space: nowrap;
  }

  .ownership-item__content {
    flex: 1;
    display: flex;
    flex-direction: column;
    gap: 0.15rem;
  }

  .ownership-item__title {
    font-size: var(--text-sm);
    font-weight: 500;
    color: var(--color-text-primary);
  }

  .ownership-item__meta {
    font-size: var(--text-xs);
    color: var(--color-text-tertiary);
  }

  @keyframes spin {
    from { transform: rotate(0deg); }
    to { transform: rotate(360deg); }
  }
</style>
