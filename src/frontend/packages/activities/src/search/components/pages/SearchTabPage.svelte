<!-- Activity: Search > Nav: Search Tab -->
<script lang="ts">
  import { navigateToEntity } from '../../../shared/navigation/entityNavigation';
  /**
   * SearchTabPage - Tab-friendly search content (without sidebar).
   * Used when SearchSidebar is rendered separately in the App aside.
   */
  import { SearchInput, SearchResults } from '../sections';
  import { AdvancedSearchModal } from '../idioms';
  import { createSearchService } from '../../services/SearchService';
  import { addRecentSearch, getSearchQuery } from '../../stores/searchQuery.store';
  import type { SearchResult, SearchResultItem, AdvancedSearchOptions } from '../../types';
  import { toast, getAuthState } from '@sddp/shell';

  interface Props {
    tenantId?: string;
    projectId?: string;
  }

  let { tenantId: propTenantId = '', projectId }: Props = $props();
  const tenantId = $derived(propTenantId || getAuthState().user?.tenantId || '');

  let showAdvancedModal = $state(false);
  let results = $state<SearchResult | null>(null);
  let loading = $state(false);
  let queryText = $state(getSearchQuery().text);
  let currentPage = $state(1);
  let hasMore = $state(false);
  let loadingMore = $state(false);
  let latestRequestId = 0;

  async function handleSearch(text: string) {
    queryText = text;
    currentPage = 1;
    if (!text.trim()) return;
    addRecentSearch(text);
    const requestId = ++latestRequestId;
    try {
      loading = true;
      const query = { ...getSearchQuery(), text, page: 1 };
      const result = await createSearchService(tenantId, projectId).search(query);
      if (requestId !== latestRequestId) return;
      results = result;
      hasMore = result.results.length < result.total;
    } catch (error) {
      if (requestId !== latestRequestId) return;
      console.error('Search failed:', error);
      toast.error('Search failed. Please try again.');
    } finally {
      if (requestId === latestRequestId) loading = false;
    }
  }

  function handleAdvancedSearch(options: AdvancedSearchOptions) {
    const parts: string[] = [];
    if (options.allWords) parts.push(options.allWords.split(' ').join(' AND '));
    if (options.exactPhrase) parts.push(`"${options.exactPhrase}"`);
    if (options.anyWords) parts.push(`(${options.anyWords.split(' ').join(' OR ')})`);
    if (options.noneWords)
      parts.push(
        options.noneWords
          .split(' ')
          .map((w) => `NOT ${w}`)
          .join(' ')
      );
    handleSearch(parts.join(' '));
  }

  function handleResultClick(item: SearchResultItem) {
    if (!navigateToEntity(item.projectId, item.type, item.id)) {
      toast.info('This item cannot be opened without project context.');
    }
  }

  async function handleLoadMore() {
    if (loading || loadingMore || !hasMore) return;
    const nextPage = currentPage + 1;
    loadingMore = true;
    try {
      const query = { ...getSearchQuery(), text: queryText, page: nextPage };
      const result = await createSearchService(tenantId, projectId).search(query);
      results = results ? { ...result, results: [...results.results, ...result.results] } : result;
      currentPage = nextPage;
      hasMore = results.results.length < results.total;
    } catch (error) {
      console.error('Load more failed:', error);
      toast.error('Failed to load more results.');
    } finally {
      loadingMore = false;
    }
  }
</script>

<div class="search-tab-content">
  <SearchInput
    value={queryText}
    onSearch={handleSearch}
    onAdvanced={() => (showAdvancedModal = true)}
  />
  <SearchResults
    {results}
    {loading}
    {hasMore}
    {loadingMore}
    onResultClick={handleResultClick}
    onLoadMore={handleLoadMore}
  />
</div>

<AdvancedSearchModal
  isOpen={showAdvancedModal}
  onSearch={handleAdvancedSearch}
  onClose={() => (showAdvancedModal = false)}
/>

<style>
  .search-tab-content {
    display: flex;
    flex-direction: column;
    height: 100%;
    overflow: hidden;
  }
</style>
