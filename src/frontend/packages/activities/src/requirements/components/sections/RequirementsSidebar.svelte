<!-- Section: RequirementsSidebar — Requirements > Global -->
<script lang="ts">
  import { Icon, Button, Input, Spinner } from '@sddp/ui';
  import type {
    ProjectRequirementGroup,
    RequirementFilterType,
    RequirementViewMode,
  } from '../../types';
  import { ProjectRequirementGroup as ProjectGroup } from '../idioms';

  interface Props {
    projectGroups: ProjectRequirementGroup[];
    selectedRequirementId?: string | null;
    expandedRequirements?: Set<string>;
    searchQuery?: string;
    filterType?: RequirementFilterType;
    viewMode?: RequirementViewMode;
    onRequirementSelect?: (id: string) => void;
    onSearchChange?: (query: string) => void;
    onFilterChange?: (filter: RequirementFilterType) => void;
    onViewModeChange?: (mode: RequirementViewMode) => void;
    onProjectToggle?: (projectId: string) => void;
    onRequirementToggle?: (requirementId: string) => void;
    hasMore?: boolean;
    loadingMore?: boolean;
    onLoadMore?: () => void;
    class?: string;
  }

  let {
    projectGroups,
    selectedRequirementId = null,
    expandedRequirements = new Set(),
    searchQuery = '',
    filterType = 'all',
    viewMode = 'tree',
    onRequirementSelect,
    onSearchChange,
    onFilterChange,
    onViewModeChange,
    onProjectToggle,
    onRequirementToggle,
    hasMore = false,
    loadingMore = false,
    onLoadMore,
    class: className = '',
  }: Props = $props();

  // Filter options
  const filterOptions: { value: RequirementFilterType; label: string; icon: string }[] = [
    { value: 'all', label: 'All', icon: 'list-flat' },
    { value: 'draft', label: 'Draft', icon: 'edit' },
    { value: 'inReview', label: 'In Review', icon: 'eye' },
    { value: 'approved', label: 'Approved', icon: 'check-circle' },
    { value: 'levelA', label: 'Level A', icon: 'flame' },
    { value: 'levelB', label: 'Level B', icon: 'zap' },
    { value: 'levelC', label: 'Level C', icon: 'minus' },
  ];

  // Total count
  const totalCount = $derived(
    projectGroups.reduce((sum, g) => sum + g.requirements.length, 0)
  );

  function handleSearchInput(e: Event) {
    const target = e.target as HTMLInputElement;
    onSearchChange?.(target.value);
  }

  function handleScroll(e: Event): void {
    if (!hasMore || loadingMore) return;

    const target = e.currentTarget as HTMLElement;
    const nearBottom = target.scrollHeight - target.scrollTop - target.clientHeight <= 100;
    if (nearBottom) {
      onLoadMore?.();
    }
  }
</script>

<div class="requirements-sidebar flex flex-col h-full bg-[var(--color-bg-secondary)] {className}">
  <!-- Header -->
  <div class="sidebar-header flex-shrink-0 p-3 border-b border-[var(--color-border-primary)]">
    <div class="flex items-center justify-between mb-3">
      <h2 class="text-sm font-semibold uppercase tracking-wide text-[var(--color-text-secondary)]">
        Requirements
      </h2>
      <span class="text-xs text-[var(--color-text-tertiary)]">
        {totalCount} items
      </span>
    </div>

    <!-- Search -->
    <div class="relative">
      <Icon
        name="search"
        size="xs"
        class="absolute left-2.5 top-1/2 -translate-y-1/2 text-[var(--color-text-tertiary)]"
      />
      <Input
        unstyled
        placeholder="Search requirements..."
        value={searchQuery}
        oninput={handleSearchInput}
        class="w-full pl-8 pr-3 py-1.5 text-sm rounded
               bg-[var(--color-bg-primary)]
               border border-[var(--color-border-primary)]
               text-[var(--color-text-primary)]
               placeholder:text-[var(--color-text-tertiary)]
               focus:outline-none focus:border-[var(--color-accent-primary)]"
      />
    </div>
  </div>

  <!-- Filters -->
  <div class="filters flex-shrink-0 px-3 py-2 border-b border-[var(--color-border-primary)]">
    <div class="flex items-center gap-1 flex-wrap">
      {#each filterOptions as option (option.value)}
        <Button
          variant="unstyled"
          class="filter-btn flex items-center gap-1 px-2 py-1 text-xs rounded
                 transition-colors
                 {filterType === option.value
                   ? 'bg-[var(--color-accent-primary)] text-white'
                   : 'bg-[var(--color-bg-tertiary)] text-[var(--color-text-secondary)] hover:bg-[var(--color-bg-tertiary)]/80'}"
          onclick={() => onFilterChange?.(option.value)}
        >
          <Icon name={option.icon} size="xs" />
          <span>{option.label}</span>
        </Button>
      {/each}
    </div>
  </div>

  <!-- View Mode Toggle -->
  <div class="view-mode flex-shrink-0 px-3 py-2 border-b border-[var(--color-border-primary)]">
    <div class="flex items-center gap-2">
      <span class="text-xs text-[var(--color-text-tertiary)]">View:</span>
      <Button
        variant="unstyled"
        class="flex items-center gap-1 px-2 py-1 text-xs rounded
               {viewMode === 'tree'
                 ? 'bg-[var(--color-accent-primary)] text-white'
                 : 'bg-transparent text-[var(--color-text-primary)] hover:bg-[var(--color-bg-tertiary)]'}"
        onclick={() => onViewModeChange?.('tree')}
      >
        <Icon name="list-tree" size="xs" />
        <span>Tree</span>
      </Button>
      <Button
        variant="unstyled"
        class="flex items-center gap-1 px-2 py-1 text-xs rounded
               {viewMode === 'flat'
                 ? 'bg-[var(--color-accent-primary)] text-white'
                 : 'bg-transparent text-[var(--color-text-primary)] hover:bg-[var(--color-bg-tertiary)]'}"
        onclick={() => onViewModeChange?.('flat')}
      >
        <Icon name="list-flat" size="xs" />
        <span>Flat</span>
      </Button>
    </div>
  </div>

  <!-- List Header -->
  <div class="flex items-center gap-2 px-3 py-1.5 border-b border-[var(--color-border-primary)] bg-[var(--color-bg-secondary)]">
    <span class="w-5"></span>
    <span class="w-5 text-xs font-medium text-[var(--color-text-tertiary)] uppercase text-center">Lv</span>
    <span class="w-16 text-xs font-medium text-[var(--color-text-tertiary)] uppercase">Code</span>
    <span class="flex-1 text-xs font-medium text-[var(--color-text-tertiary)] uppercase">Title</span>
    <span class="w-4"></span>
  </div>

  <!-- Project Groups -->
  <div class="project-groups flex-1 overflow-y-auto pb-1" onscroll={handleScroll}>
    {#each projectGroups as group (group.projectId)}
      <ProjectGroup
        {group}
        {selectedRequirementId}
        {expandedRequirements}
        onToggleExpand={() => onProjectToggle?.(group.projectId)}
        onSelectRequirement={onRequirementSelect}
        onToggleRequirementExpand={onRequirementToggle}
      />
    {/each}

    {#if projectGroups.length === 0}
      <div class="p-4 text-center text-sm text-[var(--color-text-tertiary)]">
        <Icon name="inbox" size="xl" class="mx-auto mb-2 opacity-50" />
        <p>No requirements found</p>
      </div>
    {/if}

    {#if loadingMore}
      <div class="flex items-center justify-center py-3">
        <Spinner size="sm" />
      </div>
    {/if}
  </div>

  <!-- Footer Actions -->
  <div class="sidebar-footer flex-shrink-0 p-2 border-t border-[var(--color-border-primary)]">
    <Button
      variant="primary"
      size="sm"
      fullWidth
    >
      <Icon name="plus" size="xs" />
      <span>New Requirement</span>
    </Button>
  </div>
</div>
