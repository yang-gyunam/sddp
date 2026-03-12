<!-- Activity: Search > Nav: Search Tab -->
<script lang="ts">
  /**
   * SearchTabPage - Tab-friendly search content (without sidebar).
   * Used when SearchSidebar is rendered separately in the App aside.
   */
  import { SearchInput, SearchResults } from '../sections';
  import { AdvancedSearchModal } from '../idioms';
  import SearchService from '../../services/SearchService';
  import {
    searchQueryStore,
    setSearchText,
    setSearchPage,
    addRecentSearch,
    getSearchQuery,
  } from '../../stores/searchQuery.store';
  import {
    searchResultsStore,
    setSearchResults,
    appendSearchResults,
    setSearchLoading,
    setSearchError,
  } from '../../stores/searchResults.store';
  import type { SearchResult, SearchResultItem, AdvancedSearchOptions } from '../../types';
  import {
    toast,
    navigateToSpec,
    navigateToRequirement,
    navigateToConversation,
    navigateToGlossary,
  } from '@sddp/shell';

  let showAdvancedModal = $state(false);
  let results = $state<SearchResult | null>(null);
  let loading = $state(false);
  let queryText = $state('');
  let currentPage = $state(1);
  let hasMore = $state(false);
  let loadingMore = $state(false);

  $effect(() => {
    const unsubscribe = searchResultsStore.subscribe((state) => {
      results = state.results;
      loading = state.loading;
      if (state.results) {
        hasMore = state.results.results.length < state.results.total;
      } else {
        hasMore = false;
      }
    });
    return unsubscribe;
  });

  $effect(() => {
    const unsubscribe = searchQueryStore.subscribe((state) => {
      queryText = state.query.text;
      currentPage = state.query.page;
    });
    return unsubscribe;
  });

  async function handleSearch(text: string) {
    setSearchText(text);
    if (!text.trim()) return;
    addRecentSearch(text);
    try {
      setSearchLoading(true);
      const query = getSearchQuery();
      const result = await SearchService.search(query);
      setSearchResults(result);
    } catch (error) {
      console.error('Search failed:', error);
      setSearchError('Search failed. Please try again.');
    }
  }

  function handleAdvancedSearch(options: AdvancedSearchOptions) {
    const parts: string[] = [];
    if (options.allWords) parts.push(options.allWords.split(' ').join(' AND '));
    if (options.exactPhrase) parts.push(`"${options.exactPhrase}"`);
    if (options.anyWords) parts.push(`(${options.anyWords.split(' ').join(' OR ')})`);
    if (options.noneWords) parts.push(options.noneWords.split(' ').map((w) => `NOT ${w}`).join(' '));
    handleSearch(parts.join(' '));
  }

  function handleResultClick(item: SearchResultItem) {
    switch (item.type) {
      case 'spec': navigateToSpec(item.id); break;
      case 'requirement': navigateToRequirement(item.id); break;
      case 'conversation': navigateToConversation(item.id); break;
      case 'glossary': navigateToGlossary(item.id); break;
      default: toast.info(`Opening ${item.type}: ${item.title}`);
    }
  }

  async function handleLoadMore() {
    if (loading || loadingMore || !hasMore) return;
    const nextPage = currentPage + 1;
    setSearchPage(nextPage);
    loadingMore = true;
    try {
      const query = getSearchQuery();
      const result = await SearchService.search(query);
      appendSearchResults(result);
    } catch (error) {
      console.error('Load more failed:', error);
      setSearchError('Failed to load more results.');
      setSearchPage(currentPage);
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
