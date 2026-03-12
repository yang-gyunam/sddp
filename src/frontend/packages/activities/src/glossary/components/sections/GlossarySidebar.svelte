<!-- Section: GlossarySidebar — Glossary > Global, Projects > Glossary -->
<script lang="ts">
  import { Button, Icon, Input, IconButton, Spinner } from '@sddp/ui';
  import { Dropdown } from '@sddp/shell';
  import type { CategoryTermGroup, GlossaryFilterType } from '../../types';
  import CategoryTermGroupComponent from '../idioms/CategoryTermGroup.svelte';

  interface Props {
    categoryGroups: CategoryTermGroup[];
    selectedTermId?: string | null;
    searchQuery?: string;
    filterType?: GlossaryFilterType;
    statusCounts?: Record<string, number>;
    onSearch?: (query: string) => void;
    onFilterChange?: (filter: GlossaryFilterType) => void;
    onToggleCategory?: (category: string) => void;
    onSelectTerm?: (termId: string) => void;
    onNewTerm?: () => void;
    hasMore?: boolean;
    loadingMore?: boolean;
    onLoadMore?: () => void;
    class?: string;
  }

  let {
    categoryGroups,
    selectedTermId = null,
    searchQuery = '',
    filterType = 'all',
    onSearch,
    onFilterChange,
    onToggleCategory,
    onSelectTerm,
    onNewTerm,
    hasMore = false,
    loadingMore = false,
    onLoadMore,
    class: className = '',
  }: Props = $props();

  // Filter options
  const filterOptions: { value: GlossaryFilterType; label: string; icon: string }[] = [
    { value: 'all', label: 'All', icon: 'list' },
    { value: 'draft', label: 'Draft', icon: 'edit' },
    { value: 'active', label: 'Active', icon: 'check-circle' },
    { value: 'deprecated', label: 'Deprecated', icon: 'archive' },
  ];

  function handleSearchInput(e: Event) {
    const target = e.target as HTMLInputElement;
    onSearch?.(target.value);
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

<div class="flex flex-col h-full bg-[var(--color-bg-primary)] {className}">
  <!-- Search + Action Buttons (follows SpecsSidebar pattern) -->
  <div class="sidebar-header flex-shrink-0 flex items-center p-2 min-h-12 border-b border-[var(--color-border-primary)]">
    <div class="flex items-center gap-1 w-full">
      <!-- Search Input -->
      <div class="relative flex-1">
        <Icon
          name="search"
          size="sm"
          class="absolute left-2.5 top-1/2 -translate-y-1/2 text-[var(--color-text-tertiary)]"
        />
        <Input
          type="text"
          placeholder="Search terms..."
          value={searchQuery}
          oninput={handleSearchInput}
          variant="flat"
          class="pl-8 w-full"
          size="sm"
        />
      </div>

      <!-- New Term Button -->
      {#if onNewTerm}
        <IconButton icon="plus" size="sm" variant="ghost" onclick={onNewTerm} title="New Term" />
      {/if}

      <!-- Filter Dropdown -->
      <Dropdown position="bottom-right">
        {#snippet trigger()}
          <IconButton icon="more-vertical" size="sm" variant="ghost" title="Filter options" />
        {/snippet}
        <div class="py-1 min-w-[180px]">
          <div class="px-3 py-1 text-xs font-semibold text-[var(--color-text-tertiary)] uppercase">
            Status
          </div>
          {#each filterOptions as option (option.value)}
            <Button
              variant="unstyled"
              class="w-full flex items-center gap-2 px-3 py-1.5 text-sm text-left
                     hover:bg-[var(--color-bg-tertiary)] transition-colors"
              onclick={() => onFilterChange?.(option.value)}
            >
              <Icon name={option.icon} size="sm" class="text-[var(--color-text-tertiary)]" />
              <span class="flex-1">{option.label}</span>
              {#if filterType === option.value}
                <Icon name="check" size="sm" class="text-[var(--color-accent-primary)]" />
              {/if}
            </Button>
          {/each}
        </div>
      </Dropdown>
    </div>
  </div>

  <!-- Category Groups -->
  <div class="category-groups flex-1 overflow-y-auto pb-1" onscroll={handleScroll}>
    {#each categoryGroups as group (group.category)}
      <CategoryTermGroupComponent
        {group}
        {selectedTermId}
        onToggle={() => onToggleCategory?.(group.category)}
        onSelectTerm={onSelectTerm}
      />
    {/each}

    {#if categoryGroups.length === 0}
      <div class="p-4 text-center text-sm text-[var(--color-text-tertiary)]">
        <Icon name="book" size="xl" class="mx-auto mb-2 opacity-50" />
        <p>No terms found</p>
      </div>
    {/if}

    {#if loadingMore}
      <div class="flex items-center justify-center py-3">
        <Spinner size="sm" />
      </div>
    {/if}
  </div>
</div>
