<!-- Section: SpecList -->
<!--
  SpecList Component
  Displays a list of specs with filtering
-->
<script lang="ts">
  import { Icon, Button, Spinner } from '@sddp/ui';
  import { EmptyState, formatDate as formatDateUtil } from '@sddp/shell';
  import SpecStatusBadge from '../idioms/SpecStatusBadge.svelte';
  import type { Spec, SpecStatus } from '../../types';

  interface Props {
    specs: Spec[];
    selectedId?: string | null;
    loading?: boolean;
    statusFilter?: SpecStatus | null;
    onSelect: (spec: Spec) => void;
    onCreate?: () => void;
    onStatusFilterChange?: (status: SpecStatus | null) => void;
    class?: string;
  }

  let {
    specs,
    selectedId = null,
    loading = false,
    statusFilter = null,
    onSelect,
    onCreate,
    onStatusFilterChange,
    class: className = '',
  }: Props = $props();

  let showFilters = $state(false);

  const statuses: SpecStatus[] = ['Draft', 'InReview', 'Approved', 'Locked'];

  function formatDateStr(dateStr: string): string {
    return formatDateUtil(dateStr, { month: 'short', year: undefined });
  }

  function hasActiveFilters(): boolean {
    return statusFilter !== null;
  }

  function clearFilters(): void {
    onStatusFilterChange?.(null);
  }
</script>

<div class="flex flex-col h-full {className}">
  <!-- Header -->
  <div class="flex items-center justify-between px-4 py-3 border-b border-[var(--color-border-primary)]">
    <h2 class="text-sm font-medium text-[var(--color-text-primary)]">Specs</h2>
    <div class="flex items-center gap-1">
      <Button
        variant="ghost"
        size="sm"
        onclick={() => (showFilters = !showFilters)}
        class={showFilters || hasActiveFilters() ? 'bg-[var(--color-surface-200)]' : ''}
      >
        <Icon name="filter" size="sm" />
        {#if hasActiveFilters()}
          <span class="ml-1 w-2 h-2 rounded-full bg-[var(--color-accent-primary)]"></span>
        {/if}
      </Button>
      {#if onCreate}
        <Button variant="ghost" size="sm" onclick={onCreate}>
          <Icon name="plus" size="sm" />
        </Button>
      {/if}
    </div>
  </div>

  <!-- Filters -->
  {#if showFilters}
    <div class="px-4 py-3 border-b border-[var(--color-border-secondary)] bg-[var(--color-surface-100)] space-y-3">
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
    {:else if specs.length === 0}
      <EmptyState
        icon="file-code"
        heading="No specs found"
        subtext={hasActiveFilters() ? 'Try adjusting your filters' : 'Create a new spec to get started'}
        action={onCreate && !hasActiveFilters() ? { label: 'New Spec', onclick: onCreate } : undefined}
      />
    {:else}
      <ul class="divide-y divide-[var(--color-border-secondary)]">
        {#each specs as spec (spec.id)}
          <li>
            <Button
              variant="unstyled"
              onclick={() => onSelect(spec)}
              class="w-full text-left px-4 py-3 transition-colors
                {selectedId === spec.id
                  ? 'bg-[var(--color-accent-primary)]/10'
                  : 'hover:bg-[var(--color-bg-tertiary)]'}"
            >
              <div class="flex items-start justify-between gap-2">
                <div class="flex-1 min-w-0">
                  <div class="flex items-center gap-2 mb-1">
                    <span class="text-xs text-[var(--color-text-muted)] font-mono">
                      {spec.code}
                    </span>
                    <span class="text-xs text-[var(--color-text-muted)]">
                      v{spec.version}
                    </span>
                  </div>
                  <h3 class="text-sm font-medium text-[var(--color-text-primary)] truncate">
                    {spec.title}
                  </h3>
                  <div class="flex items-center gap-3 mt-1">
                    <SpecStatusBadge status={spec.status} showIcon={false} />
                    {#if spec.requirementId}
                      <span class="text-xs text-[var(--color-text-muted)] flex items-center gap-1">
                        <Icon name="link" size="xs" />
                        Req
                      </span>
                    {/if}
                  </div>
                </div>
                <span class="text-xs text-[var(--color-text-muted)] flex-shrink-0">
                  {formatDateStr(spec.updatedAt)}
                </span>
              </div>
            </Button>
          </li>
        {/each}
      </ul>
    {/if}
  </div>
</div>
