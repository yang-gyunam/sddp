<!-- Section: RequirementList -->
<!--
  RequirementList Component
  Displays a list of requirements with filtering
-->
<script lang="ts">
  import { Icon, IconButton, Button, Spinner } from '@sddp/ui';
  import { EmptyState, RichListItem, formatDate as formatDateUtil } from '@sddp/shell';
  import RequirementLevelBadge from '../idioms/RequirementLevelBadge.svelte';
  import RequirementStatusBadge from '../idioms/RequirementStatusBadge.svelte';
  import type { Requirement, RequirementLevel, RequirementStatus } from '../../types';

  interface Props {
    requirements: Requirement[];
    selectedId?: string | null;
    loading?: boolean;
    levelFilter?: RequirementLevel | null;
    statusFilter?: RequirementStatus | null;
    onSelect: (requirement: Requirement) => void;
    onCreate?: () => void;
    onLevelFilterChange?: (level: RequirementLevel | null) => void;
    onStatusFilterChange?: (status: RequirementStatus | null) => void;
    class?: string;
  }

  let {
    requirements,
    selectedId = null,
    loading = false,
    levelFilter = null,
    statusFilter = null,
    onSelect,
    onCreate,
    onLevelFilterChange,
    onStatusFilterChange,
    class: className = '',
  }: Props = $props();

  let showFilters = $state(false);

  const levels: RequirementLevel[] = ['A', 'B', 'C'];
  const statuses: RequirementStatus[] = ['Draft', 'InReview', 'Approved', 'Deprecated'];

  function formatDateStr(dateStr: string): string {
    return formatDateUtil(dateStr, { month: 'short', year: undefined });
  }

  function hasActiveFilters(): boolean {
    return levelFilter !== null || statusFilter !== null;
  }

  function clearFilters(): void {
    onLevelFilterChange?.(null);
    onStatusFilterChange?.(null);
  }
</script>

<div class="flex flex-col h-full {className}">
  <!-- Header -->
  <div class="flex items-center justify-between px-4 py-3 border-b border-[var(--color-border-primary)]">
    <h2 class="text-sm font-medium text-[var(--color-text-primary)]">Requirements</h2>
    <div class="flex items-center gap-1">
      <Button
        variant="ghost"
        size="sm"
        title="Filter"
        onclick={() => (showFilters = !showFilters)}
        class={showFilters || hasActiveFilters() ? 'bg-[var(--color-surface-200)]' : ''}
      >
        <Icon name="filter" size="sm" />
        {#if hasActiveFilters()}
          <span class="ml-1 w-2 h-2 rounded-full bg-[var(--color-accent-primary)]"></span>
        {/if}
      </Button>
      {#if onCreate}
        <IconButton icon="plus" variant="ghost" size="sm" title="New Requirement" onclick={onCreate} />
      {/if}
    </div>
  </div>

  <!-- Filters -->
  {#if showFilters}
    <div class="px-4 py-3 border-b border-[var(--color-border-secondary)] bg-[var(--color-surface-100)] space-y-3">
      <!-- Level filter -->
      <div>
        <span class="block text-xs font-medium text-[var(--color-text-muted)] mb-1">Level</span>
        <div class="flex flex-wrap gap-1">
          {#each levels as level (level)}
            <Button
              variant="unstyled"
              onclick={() => onLevelFilterChange?.(levelFilter === level ? null : level)}
              class="px-2 py-1 text-xs rounded border transition-colors {levelFilter === level
                ? 'bg-[var(--color-accent-primary)] text-white border-[var(--color-accent-primary)]'
                : 'bg-[var(--color-surface-50)] text-[var(--color-text-secondary)] border-[var(--color-border-secondary)] hover:bg-[var(--color-surface-200)]'}"
            >
              {level}
            </Button>
          {/each}
        </div>
      </div>

      <!-- Status filter -->
      <div>
        <span class="block text-xs font-medium text-[var(--color-text-muted)] mb-1">Status</span>
        <div class="flex flex-wrap gap-1">
          {#each statuses as status (status)}
            <Button
              variant="unstyled"
              onclick={() => onStatusFilterChange?.(statusFilter === status ? null : status)}
              class="px-2 py-1 text-xs rounded border transition-colors {statusFilter === status
                ? 'bg-[var(--color-accent-primary)] text-white border-[var(--color-accent-primary)]'
                : 'bg-[var(--color-surface-50)] text-[var(--color-text-secondary)] border-[var(--color-border-secondary)] hover:bg-[var(--color-surface-200)]'}"
            >
              {status}
            </Button>
          {/each}
        </div>
      </div>

      {#if hasActiveFilters()}
        <Button
          variant="unstyled"
          onclick={clearFilters}
          class="text-xs text-[var(--color-accent-primary)] hover:underline"
        >
          Clear filters
        </Button>
      {/if}
    </div>
  {/if}

  <!-- List -->
  <div class="flex-1 overflow-y-auto">
    {#if loading}
      <div class="flex-1 flex items-center justify-center">
        <Spinner size="lg" />
      </div>
    {:else if requirements.length === 0}
      <EmptyState
        icon="file-text"
        heading="No requirements found"
        subtext={hasActiveFilters() ? 'Try adjusting your filters' : 'Create a new requirement to get started'}
        action={onCreate && !hasActiveFilters() ? { label: 'New Requirement', onclick: onCreate } : undefined}
      />
    {:else}
      <div>
        {#each requirements as requirement (requirement.id)}
          <RichListItem
            selected={selectedId === requirement.id}
            title={requirement.title}
            meta={formatDateStr(requirement.updatedAt)}
            onclick={() => onSelect(requirement)}
          >
            {#snippet leading()}
              <RequirementLevelBadge level={requirement.level} />
            {/snippet}
            {#snippet trailing()}
              <span class="text-xs text-[var(--color-text-tertiary)] font-mono">
                {requirement.code}
              </span>
            {/snippet}
            {#snippet badges()}
              <RequirementStatusBadge status={requirement.status} showIcon={false} />
              {#if requirement.childrenCount > 0}
                <span class="text-xs text-[var(--color-text-tertiary)] flex items-center gap-1">
                  <Icon name="git-branch" size="xs" />
                  {requirement.childrenCount}
                </span>
              {/if}
            {/snippet}
          </RichListItem>
        {/each}
      </div>
    {/if}
  </div>
</div>
