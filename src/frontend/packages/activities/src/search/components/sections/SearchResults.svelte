<!-- Section: SearchResults — Search > Global/Tab -->
<script lang="ts">
  /**
   * Search Results
   * Display search results with sorting and pagination
   */

  import { Button, Select } from '@sddp/ui';
  import type { SearchResult, SearchSort, SearchResultItem } from '../../types';
  import { searchQueryStore, setSearchSort } from '../../stores/searchQuery.store';
  import { formatRelativeTime } from '@sddp/shell';

  interface Props {
    results: SearchResult | null;
    loading?: boolean;
    hasMore?: boolean;
    loadingMore?: boolean;
    onResultClick?: (item: SearchResultItem) => void;
    onLoadMore?: () => void;
  }

  let {
    results,
    loading = false,
    hasMore: hasMoreProp = false,
    loadingMore = false,
    onResultClick,
    onLoadMore,
  }: Props = $props();

  let sortBy = $state<SearchSort>('relevance');
  const hasMore = $derived(
    hasMoreProp || (!!results && results.results.length < results.total)
  );

  $effect(() => {
    const unsubscribe = searchQueryStore.subscribe((state) => {
      sortBy = state.query.sort;
    });
    return unsubscribe;
  });

  const sortOptions: Array<{ value: SearchSort; label: string }> = [
    { value: 'relevance', label: 'Relevance' },
    { value: 'date-desc', label: 'Date (Newest)' },
    { value: 'date-asc', label: 'Date (Oldest)' },
    { value: 'title', label: 'Title (A-Z)' },
    { value: 'project', label: 'Project' },
  ];

  function handleSortChange(value: string) {
    setSearchSort(value as SearchSort);
  }

  function handleResultClick(item: SearchResultItem) {
    if (onResultClick) {
      onResultClick(item);
    }
  }

  function getEntityIcon(type: string): string {
    const icons: Record<string, string> = {
      conversation: '💬',
      requirement: '📋',
      spec: '📄',
      task: '✓',
      glossary: '📖',
      artifact: '📦',
    };
    return icons[type] || '📄';
  }

  function formatDate(dateStr: string): string {
    return formatRelativeTime(dateStr, undefined, { locale: 'en' });
  }

  function handleScroll(e: Event): void {
    if (!hasMore || loading || loadingMore) return;

    const target = e.currentTarget as HTMLElement;
    const nearBottom = target.scrollHeight - target.scrollTop - target.clientHeight <= 100;
    if (nearBottom) {
      onLoadMore?.();
    }
  }
</script>

<div class="search-results" onscroll={handleScroll}>
  {#if loading && (!results || results.results.length === 0)}
    <div class="loading-state">
      <div class="spinner"></div>
      <p>Searching...</p>
    </div>
  {:else if results}
    <!-- Results Header -->
    <div class="results-header">
      <span class="results-count">Results ({results.total})</span>
      <div class="sort-control">
        <Select bind:value={sortBy} onchange={handleSortChange} options={sortOptions} size="sm" />
      </div>
    </div>

    <!-- Results List -->
    {#if results.results.length > 0}
      <div class="results-list">
        {#each results.results as item (item.id)}
          <Button
            variant="unstyled"
            class="result-card"
            onclick={() => handleResultClick(item)}
            aria-label="{item.type}: {item.title}"
          >
            <div class="result-header">
              <span class="entity-icon">{getEntityIcon(item.type)}</span>
              <span class="entity-id">{item.metadata.code || `#${item.id}`}</span>
              <span class="project-name">{item.projectName}</span>
            </div>

            <h3 class="result-title">{item.title}</h3>

            <div class="result-snippet">
              <!-- eslint-disable-next-line svelte/no-at-html-tags -->
              {@html item.snippet}
            </div>

            <div class="result-footer">
              {#if item.metadata.status}
                <span class="status-badge">{item.metadata.status}</span>
              {/if}
              {#if item.metadata.version}
                <span class="metadata-item">v{item.metadata.version}</span>
              {/if}
              {#if item.metadata.level}
                <span class="metadata-item">Level: {item.metadata.level}</span>
              {/if}
              {#if item.metadata.messageCount}
                <span class="metadata-item">{item.metadata.messageCount} msgs</span>
              {/if}
              <span class="metadata-item date">{formatDate(item.updatedAt)}</span>
            </div>
          </Button>
        {/each}
      </div>

      {#if loadingMore}
        <div class="load-more">
          <div class="spinner"></div>
        </div>
      {/if}
    {:else}
      <div class="empty-state">
        <div class="empty-icon">🔍</div>
        <h3>No results found</h3>
        <p>Try adjusting your search or filters:</p>
        <ul>
          <li>Check your spelling</li>
          <li>Use fewer or different keywords</li>
          <li>Remove some filters</li>
        </ul>
      </div>
    {/if}
  {/if}
</div>

<style>
  .search-results {
    display: flex;
    flex-direction: column;
    height: 100%;
    overflow-y: auto;
  }

  .loading-state {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    padding: 4rem 2rem;
    color: var(--text-secondary);
  }

  .spinner {
    width: 2rem;
    height: 2rem;
    border: 3px solid var(--border-color);
    border-top-color: var(--accent-color);
    border-radius: 50%;
    animation: spin 1s linear infinite;
  }

  @keyframes spin {
    to {
      transform: rotate(360deg);
    }
  }

  .results-header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding: 1rem;
    border-bottom: 1px solid var(--border-color);
  }

  .results-count {
    font-weight: 600;
    font-size: 0.875rem;
  }

  .sort-control {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    font-size: 0.875rem;
  }

  .results-list {
    padding: 1rem;
  }

  :global(.result-card) {
    width: 100%;
    text-align: left;
    padding: 1rem;
    margin-bottom: 1rem;
    background: var(--bg-secondary);
    border: 1px solid var(--border-color);
    border-radius: 4px;
    cursor: pointer;
    transition: all 0.2s;
  }

  :global(.result-card:hover) {
    border-color: var(--accent-color);
    box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
  }

  .result-header {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    margin-bottom: 0.5rem;
    font-size: 0.875rem;
  }

  .entity-icon {
    font-size: 1rem;
  }

  .entity-id {
    font-weight: 600;
    color: var(--text-secondary);
  }

  .project-name {
    margin-left: auto;
    color: var(--text-tertiary);
    font-size: 0.75rem;
  }

  .result-title {
    margin: 0 0 0.5rem;
    font-size: 1rem;
    font-weight: 600;
    color: var(--text-primary);
  }

  .result-snippet {
    margin-bottom: 0.75rem;
    font-size: 0.875rem;
    line-height: 1.5;
    color: var(--text-secondary);
  }

  .result-snippet :global(mark) {
    background: var(--highlight-bg);
    color: var(--text-primary);
    font-weight: 600;
    padding: 0 0.125rem;
  }

  .result-footer {
    display: flex;
    align-items: center;
    gap: 0.75rem;
    flex-wrap: wrap;
    font-size: 0.75rem;
  }

  .status-badge {
    padding: 0.125rem 0.5rem;
    background: var(--accent-color);
    color: white;
    border-radius: 12px;
    font-weight: 500;
  }

  .metadata-item {
    color: var(--text-tertiary);
  }

  .metadata-item.date {
    margin-left: auto;
  }

  .load-more {
    display: flex;
    justify-content: center;
    padding: 1rem;
  }

  .empty-state {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    padding: 4rem 2rem;
    text-align: center;
  }

  .empty-icon {
    font-size: 3rem;
    margin-bottom: 1rem;
  }

  .empty-state h3 {
    margin: 0 0 0.5rem;
    font-size: 1.25rem;
    color: var(--text-primary);
  }

  .empty-state p {
    margin: 0 0 1rem;
    color: var(--text-secondary);
  }

  .empty-state ul {
    list-style: none;
    padding: 0;
    margin: 0;
    color: var(--text-tertiary);
    font-size: 0.875rem;
  }

  .empty-state li {
    margin-bottom: 0.25rem;
  }
</style>
