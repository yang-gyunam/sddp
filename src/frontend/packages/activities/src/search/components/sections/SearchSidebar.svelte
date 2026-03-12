<!-- Section: SearchSidebar — Search > Global -->
<script lang="ts">
  /**
   * Search Sidebar
   * Filters and saved searches
   */

  import { Button } from '@sddp/ui';
  import { FilterSection, DateRangePicker, SavedSearchItem } from '../idioms';
  import type { SearchFilters, EntityType, SavedSearch, DateRange } from '../../types';
  import {
    searchQueryStore,
    setSearchFilters,
    clearFilters,
    hasActiveFilters,
  } from '../../stores/searchQuery.store';
  import { savedSearchesStore } from '../../stores/savedSearches.store';

  interface Props {
    onSaveSearch?: () => void;
    onSelectSavedSearch?: (search: SavedSearch) => void;
    onDeleteSavedSearch?: (id: string) => void;
  }

  let { onSaveSearch, onSelectSavedSearch, onDeleteSavedSearch }: Props = $props();

  let filters = $state<SearchFilters>({
    types: [],
    projectIds: [],
    statuses: [],
    dateRange: undefined,
  });
  let savedSearches = $state<SavedSearch[]>([]);
  let recentSearches = $state<string[]>([]);

  $effect(() => {
    const unsubscribe = searchQueryStore.subscribe((state) => {
      filters = state.query.filters;
      recentSearches = state.recentSearches;
    });
    return unsubscribe;
  });

  $effect(() => {
    const unsubscribe = savedSearchesStore.subscribe((state) => {
      savedSearches = state.searches;
    });
    return unsubscribe;
  });

  // Type options
  const typeOptions = [
    { value: 'conversation', label: 'Conversations' },
    { value: 'requirement', label: 'Requirements' },
    { value: 'spec', label: 'Specs' },
    { value: 'task', label: 'Tasks' },
    { value: 'glossary', label: 'Glossary' },
    { value: 'artifact', label: 'Artifacts' },
  ];

  // Project options (mock data)
  const projectOptions = [
    { value: 'proj-a', label: 'Project A' },
    { value: 'proj-b', label: 'Project B' },
    { value: 'proj-c', label: 'Project C' },
  ];

  // Status options
  const statusOptions = [
    { value: 'Active', label: 'Active' },
    { value: 'Archived', label: 'Archived' },
    { value: 'Draft', label: 'Draft' },
    { value: 'InReview', label: 'In Review' },
    { value: 'Approved', label: 'Approved' },
    { value: 'Locked', label: 'Locked' },
  ];

  function toggleType(type: string) {
    const types = filters.types.includes(type as EntityType)
      ? filters.types.filter((t) => t !== type)
      : [...filters.types, type as EntityType];
    setSearchFilters({ types });
  }

  function toggleProject(projectId: string) {
    const projectIds = filters.projectIds.includes(projectId)
      ? filters.projectIds.filter((p) => p !== projectId)
      : [...filters.projectIds, projectId];
    setSearchFilters({ projectIds });
  }

  function toggleStatus(status: string) {
    const statuses = filters.statuses.includes(status)
      ? filters.statuses.filter((s) => s !== status)
      : [...filters.statuses, status];
    setSearchFilters({ statuses });
  }

  function handleDateRangeChange(range: DateRange | undefined) {
    setSearchFilters({ dateRange: range });
  }

  function handleClearFilters() {
    clearFilters();
  }

  function handleSaveSearch() {
    if (onSaveSearch) {
      onSaveSearch();
    }
  }
</script>

<div class="search-sidebar">
  <!-- Recent Searches -->
  {#if recentSearches.length > 0}
    <div class="sidebar-section">
      <div class="section-header">
        <span class="section-title">RECENT SEARCHES</span>
      </div>
      <div class="recent-searches">
        {#each recentSearches as search, index (index)}
          <div class="recent-search-item">
            <span class="recent-icon">🕐</span>
            <span class="recent-text">{search}</span>
          </div>
        {/each}
      </div>
    </div>
  {/if}

  <!-- Saved Searches -->
  {#if savedSearches.length > 0}
    <div class="sidebar-section">
      <div class="section-header">
        <span class="section-title">SAVED SEARCHES</span>
      </div>
      <div class="saved-searches">
        {#each savedSearches as search (search.id)}
          <SavedSearchItem
            {search}
            onSelect={onSelectSavedSearch}
            onDelete={onDeleteSavedSearch}
          />
        {/each}
      </div>
    </div>
  {/if}

  <!-- Filters -->
  <div class="sidebar-section">
    <div class="section-header">
      <span class="section-title">FILTERS</span>
    </div>

    <FilterSection
      title="TYPE"
      options={typeOptions}
      selected={filters.types}
      onToggle={toggleType}
    />

    <FilterSection
      title="PROJECT"
      options={projectOptions}
      selected={filters.projectIds}
      onToggle={toggleProject}
    />

    <FilterSection
      title="STATUS"
      options={statusOptions}
      selected={filters.statuses}
      onToggle={toggleStatus}
    />

    <FilterSection title="DATE RANGE" expanded={false}>
      <DateRangePicker value={filters.dateRange} onChange={handleDateRangeChange} />
    </FilterSection>
  </div>

  <!-- Actions -->
  <div class="sidebar-actions">
    {#if hasActiveFilters()}
      <Button variant="ghost" onclick={handleClearFilters} fullWidth>Clear All</Button>
    {/if}
    <Button variant="primary" onclick={handleSaveSearch} fullWidth>Save Search</Button>
  </div>
</div>

<style>
  .search-sidebar {
    display: flex;
    flex-direction: column;
    height: 100%;
    overflow-y: auto;
  }

  .sidebar-section {
    padding: 1rem 0;
    border-bottom: 1px solid var(--border-color);
  }

  .section-header {
    padding: 0 0.5rem 0.5rem;
  }

  .section-title {
    font-weight: 600;
    font-size: 0.75rem;
    text-transform: uppercase;
    letter-spacing: 0.05em;
    color: var(--text-secondary);
  }

  .recent-searches {
    padding: 0 0.5rem;
  }

  .recent-search-item {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    padding: 0.375rem 0.5rem;
    cursor: pointer;
    font-size: 0.875rem;
    border-radius: 4px;
  }

  .recent-search-item:hover {
    background: var(--hover-bg);
  }

  .recent-icon {
    font-size: 0.875rem;
  }

  .recent-text {
    flex: 1;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
  }

  .saved-searches {
    padding: 0 0.5rem;
  }

  .sidebar-actions {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
    padding: 1rem;
    margin-top: auto;
  }

</style>
