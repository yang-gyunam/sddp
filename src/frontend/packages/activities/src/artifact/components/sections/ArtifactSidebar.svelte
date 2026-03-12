<!-- Section: ArtifactSidebar — Projects > Artifacts, Artifacts > Global -->
<script lang="ts">
  import { Icon, Input, IconButton, Button, Spinner } from '@sddp/ui';
  import { Dropdown } from '@sddp/shell';
  import type { ArtifactTypeGroup, ArtifactFilterType } from '../../types';
  import ArtifactTypeGroupComponent from '../idioms/ArtifactTypeGroup.svelte';

  interface Props {
    typeGroups: ArtifactTypeGroup[];
    selectedArtifactId?: string | null;
    searchQuery?: string;
    filterType?: ArtifactFilterType;
    statusCounts?: Record<ArtifactFilterType, number>;
    onSearch?: (query: string) => void;
    onFilterChange?: (filter: ArtifactFilterType) => void;
    onToggleType?: (type: string) => void;
    onSelectArtifact?: (artifactId: string) => void;
    onRegenerate?: () => void;
    onVerifyAll?: () => void;
    hasMore?: boolean;
    loadingMore?: boolean;
    onLoadMore?: () => void;
    class?: string;
  }

  let {
    typeGroups,
    selectedArtifactId = null,
    searchQuery = '',
    filterType = 'all',
    statusCounts = { all: 0, valid: 0, modified: 0, missing: 0 },
    onSearch,
    onFilterChange,
    onToggleType,
    onSelectArtifact,
    onRegenerate,
    onVerifyAll,
    hasMore = false,
    loadingMore = false,
    onLoadMore,
    class: className = '',
  }: Props = $props();

  const filters: { key: ArtifactFilterType; label: string; icon: string }[] = [
    { key: 'all', label: 'All', icon: 'layers' },
    { key: 'valid', label: 'Valid', icon: 'check-circle' },
    { key: 'modified', label: 'Modified', icon: 'alert-triangle' },
    { key: 'missing', label: 'Missing', icon: 'x-circle' },
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

<div class="flex flex-col h-full {className}">
  <!-- Header with Search and Actions -->
  <div class="flex-shrink-0 flex items-center p-2 min-h-12 border-b border-[var(--color-border-primary)]">
    <div class="flex items-center gap-1 w-full">
      <!-- Search -->
      <div class="relative flex-1">
        <Icon
          name="search"
          size="sm"
          class="absolute left-2.5 top-1/2 -translate-y-1/2 text-[var(--color-text-tertiary)]"
        />
        <Input
          type="text"
          placeholder="Search artifacts..."
          value={searchQuery}
          oninput={handleSearchInput}
          variant="flat"
          class="pl-8 w-full"
          size="sm"
        />
      </div>
      {#if onVerifyAll}
        <IconButton icon="workspace-trusted" size="sm" variant="ghost" onclick={onVerifyAll} title="Verify All" />
      {/if}
      {#if onRegenerate}
        <IconButton icon="lightbulb-sparkle" size="sm" variant="ghost" onclick={onRegenerate} title="Regenerate" />
      {/if}
      <!-- Filter dropdown -->
      <Dropdown position="bottom-right">
        {#snippet trigger()}
          <IconButton icon="more-vertical" size="sm" variant="ghost" title="Filter options" />
        {/snippet}
        <div class="py-1 min-w-[180px]">
          <div class="px-3 py-1 text-xs font-semibold text-[var(--color-text-tertiary)] uppercase">
            Status
          </div>
          {#each filters as filter (filter.key)}
            <Button
              variant="unstyled"
              class="w-full flex items-center gap-2 px-3 py-1.5 text-sm text-left transition-colors
                {filterType === filter.key
                  ? 'bg-[var(--color-accent-primary)]/10 text-[var(--color-accent-primary)]'
                  : 'text-[var(--color-text-secondary)] hover:bg-[var(--color-bg-tertiary)]'}"
              onclick={() => onFilterChange?.(filter.key)}
            >
              <Icon name={filter.icon} size="sm" />
              <span class="flex-1">{filter.label}</span>
              {#if filterType === filter.key}
                <Icon name="check" size="sm" class="text-[var(--color-accent-primary)]" />
              {/if}
              <span class="text-xs opacity-70">({statusCounts[filter.key]})</span>
            </Button>
          {/each}
        </div>
      </Dropdown>
    </div>
  </div>

  <!-- Type Groups -->
  <div class="flex-1 overflow-y-auto pb-1" onscroll={handleScroll}>
    {#each typeGroups as group (group.type)}
      <ArtifactTypeGroupComponent
        {group}
        {selectedArtifactId}
        onToggle={() => onToggleType?.(group.type)}
        onSelectArtifact={onSelectArtifact}
      />
    {/each}

    {#if typeGroups.length === 0}
      <div class="p-4 text-center text-sm text-[var(--color-text-tertiary)]">
        <Icon name="package" size="xl" class="mx-auto mb-2 opacity-50" />
        <p>No artifacts found</p>
      </div>
    {/if}

    {#if loadingMore}
      <div class="flex items-center justify-center py-3">
        <Spinner size="sm" />
      </div>
    {/if}
  </div>
</div>
