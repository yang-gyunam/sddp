<!--
  ConflictWarnings Component
  Displays allocation conflict warnings
-->
<script lang="ts">
  import { Button, Icon, Badge } from '@sddp/ui';
  import { formatDateWithOptions, formatNumber } from '@sddp/shell';
  import type { AllocationConflict } from '../../types';
  import { formatHours } from '../../stores';

  interface Props {
    conflicts: AllocationConflict[];
    onConflictClick?: (conflict: AllocationConflict) => void;
    class?: string;
  }

  let { conflicts, onConflictClick, class: className = '' }: Props = $props();

  let isExpanded = $state(true);

  function formatDate(dateStr: string): string {
    return formatDateWithOptions(dateStr, {
      weekday: 'short',
      month: 'short',
      day: 'numeric',
    });
  }
</script>

{#if conflicts.length > 0}
  <div class="conflicts {className}">
    <Button
      variant="unstyled"
      class="conflicts__header"
      onclick={() => (isExpanded = !isExpanded)}
    >
      <div class="conflicts__header-left">
        <Icon name="alert-triangle" size="sm" class="conflicts__icon" />
        <span class="conflicts__title">Allocation Conflicts</span>
        <Badge variant="warning" size="sm">{formatNumber(conflicts.length)}</Badge>
      </div>
      <Icon
        name={isExpanded ? 'chevron-up' : 'chevron-down'}
        size="sm"
        class="conflicts__chevron"
      />
    </Button>

    {#if isExpanded}
      <div class="conflicts__list">
        {#each conflicts as conflict (conflict.userId + '-' + conflict.date)}
          <Button
            variant="unstyled"
            class="conflicts__item"
            onclick={() => onConflictClick?.(conflict)}
          >
            <div class="conflicts__item-header">
              <span class="conflicts__user">{conflict.userName}</span>
              <span class="conflicts__date">{formatDate(conflict.date)}</span>
              <Badge variant="error" size="sm">
                {formatHours(conflict.totalAllocated)} total
              </Badge>
            </div>
            <div class="conflicts__projects">
              {#each conflict.projects as project (project.projectId)}
                <span class="conflicts__project">
                  <Icon name="folder" size="xs" />
                  {project.projectName}: {formatHours(project.allocatedHours)}
                </span>
              {/each}
            </div>
          </Button>
        {/each}
      </div>
    {/if}
  </div>
{/if}

<style>
  .conflicts {
    background-color: var(--color-warning-50);
    border: 1px solid var(--color-warning-200);
    border-radius: var(--radius-lg, 8px);
    overflow: hidden;
  }

  :global(.dark) .conflicts {
    background-color: rgba(245, 158, 11, 0.1);
    border-color: rgba(245, 158, 11, 0.3);
  }

  .conflicts__header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    width: 100%;
    padding: 0.75rem 1rem;
    background: transparent;
    border: none;
    cursor: pointer;
    transition: background-color 150ms ease-in-out;
  }

  .conflicts__header:hover {
    background-color: var(--color-warning-100);
  }

  :global(.dark) .conflicts__header:hover {
    background-color: rgba(245, 158, 11, 0.15);
  }

  .conflicts__header-left {
    display: flex;
    align-items: center;
    gap: 0.5rem;
  }

  :global(.conflicts__icon) {
    color: var(--color-warning-600);
  }

  .conflicts__title {
    font-size: var(--text-sm);
    font-weight: 600;
    color: var(--color-warning-800);
  }

  :global(.dark) .conflicts__title {
    color: var(--color-warning-200);
  }

  :global(.conflicts__chevron) {
    color: var(--color-warning-600);
  }

  .conflicts__list {
    border-top: 1px solid var(--color-warning-200);
  }

  :global(.dark) .conflicts__list {
    border-color: rgba(245, 158, 11, 0.3);
  }

  .conflicts__item {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
    width: 100%;
    padding: 0.75rem 1rem;
    background: transparent;
    border: none;
    border-bottom: 1px solid var(--color-warning-100);
    text-align: left;
    cursor: pointer;
    transition: background-color 150ms ease-in-out;
  }

  :global(.dark) .conflicts__item {
    border-color: rgba(245, 158, 11, 0.2);
  }

  .conflicts__item:last-child {
    border-bottom: none;
  }

  .conflicts__item:hover {
    background-color: var(--color-warning-100);
  }

  :global(.dark) .conflicts__item:hover {
    background-color: rgba(245, 158, 11, 0.15);
  }

  .conflicts__item-header {
    display: flex;
    align-items: center;
    gap: 0.75rem;
  }

  .conflicts__user {
    font-size: var(--text-sm);
    font-weight: 500;
    color: var(--color-text-primary);
  }

  .conflicts__date {
    font-size: var(--text-xs);
    color: var(--color-text-secondary);
  }

  .conflicts__projects {
    display: flex;
    flex-wrap: wrap;
    gap: 0.5rem;
    padding-left: 1rem;
  }

  .conflicts__project {
    display: inline-flex;
    align-items: center;
    gap: 0.25rem;
    font-size: var(--text-xs);
    color: var(--color-text-secondary);
  }
</style>
