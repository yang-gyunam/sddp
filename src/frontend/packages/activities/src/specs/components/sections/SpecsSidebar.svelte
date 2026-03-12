<!-- Section: SpecsSidebar — Specs > Global -->
<script lang="ts">
  import { Icon, Input, Button, Spinner } from '@sddp/ui';
  import type { ProjectSpecGroup, SpecFilterType } from '../../types';
  import { ProjectSpecGroup as ProjectGroup } from '../idioms';

  interface Props {
    projectGroups: ProjectSpecGroup[];
    selectedSpecId?: string | null;
    searchQuery?: string;
    filterType?: SpecFilterType;
    onSpecSelect?: (id: string) => void;
    onSearchChange?: (query: string) => void;
    onFilterChange?: (filter: SpecFilterType) => void;
    onProjectToggle?: (projectId: string) => void;
    hasMore?: boolean;
    loadingMore?: boolean;
    onLoadMore?: () => void;
    class?: string;
  }

  let {
    projectGroups,
    selectedSpecId = null,
    searchQuery = '',
    filterType = 'all',
    onSpecSelect,
    onSearchChange,
    onFilterChange,
    onProjectToggle,
    hasMore = false,
    loadingMore = false,
    onLoadMore,
    class: className = '',
  }: Props = $props();

  // Filter options
  const filterOptions: { value: SpecFilterType; label: string; icon: string }[] = [
    { value: 'all', label: 'All', icon: 'list-flat' },
    { value: 'draft', label: 'Draft', icon: 'edit' },
    { value: 'inReview', label: 'Review', icon: 'eye' },
    { value: 'approved', label: 'Approved', icon: 'check' },
    { value: 'locked', label: 'Locked', icon: 'lock' },
  ];

  // Total count
  const totalCount = $derived(
    projectGroups.reduce((sum, g) => sum + g.specs.length, 0)
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

<div class="specs-sidebar flex flex-col h-full bg-[var(--color-bg-secondary)] {className}">
  <!-- Header -->
  <div class="sidebar-header flex-shrink-0 p-3 border-b border-[var(--color-border-primary)]">
    <div class="flex items-center justify-between mb-3">
      <h2 class="text-sm font-semibold uppercase tracking-wide text-[var(--color-text-secondary)]">
        Specs
      </h2>
      <span class="text-xs text-[var(--color-text-tertiary)]">
        {totalCount} items
      </span>
    </div>

    <!-- Search -->
    <div class="relative">
      <Icon
        name="search"
        size="sm"
        class="absolute left-2.5 top-1/2 -translate-y-1/2 text-[var(--color-text-tertiary)]"
      />
      <Input
        type="text"
        placeholder="Search specs..."
        value={searchQuery}
        oninput={handleSearchInput}
        variant="flat"
        class="pl-8 w-full"
        size="sm"
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

  <!-- List Header -->
  <div class="flex items-center gap-2 px-3 py-1.5 border-b border-[var(--color-border-primary)] bg-[var(--color-bg-secondary)]">
    <span class="w-4"></span>
    <span class="w-16 text-xs font-medium text-[var(--color-text-tertiary)] uppercase">Code</span>
    <span class="flex-1 text-xs font-medium text-[var(--color-text-tertiary)] uppercase">Title</span>
    <span class="w-8 text-xs font-medium text-[var(--color-text-tertiary)] uppercase text-right">Status</span>
  </div>

  <!-- Project Groups -->
  <div class="project-groups flex-1 overflow-y-auto pb-1" onscroll={handleScroll}>
    {#each projectGroups as group (group.projectId)}
      <ProjectGroup
        {group}
        {selectedSpecId}
        onToggleExpand={() => onProjectToggle?.(group.projectId)}
        onSelectSpec={onSpecSelect}
      />
    {/each}

    {#if projectGroups.length === 0}
      <div class="p-4 text-center text-sm text-[var(--color-text-tertiary)]">
        <Icon name="inbox" size="xl" class="mx-auto mb-2 opacity-50" />
        <p>No specs found</p>
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
      variant="unstyled"
      class="w-full flex items-center justify-center gap-2 px-3 py-2 text-sm rounded
             bg-[var(--color-accent-primary)] text-white
             hover:bg-[var(--color-accent-primary)]/90"
    >
      <Icon name="plus" size="xs" />
      <span>New Spec</span>
    </Button>
  </div>
</div>
