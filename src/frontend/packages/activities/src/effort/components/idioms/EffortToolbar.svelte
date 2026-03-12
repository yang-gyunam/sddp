<!--
  EffortToolbar Component
  Navigation and controls for effort management
-->
<script lang="ts">
  import { Button, IconButton, Badge } from '@sddp/ui';
  import { formatMonthShort, formatNumber } from '@sddp/shell';
  import { effortStore, previousWeek, nextWeek, goToToday, subscribeEffort } from '../../stores';

  interface Props {
    onSettingsClick?: () => void;
    onExportClick?: () => void;
    class?: string;
  }

  let { onSettingsClick, onExportClick, class: className = '' }: Props = $props();

  // Reactive state - synced with store
  let dateRange = $state(effortStore.get().dateRange);
  let isLoading = $state(effortStore.get().isLoading);
  let conflicts = $state(effortStore.get().conflicts);

  // Subscribe to store changes
  $effect(() => {
    const unsubscribe = subscribeEffort((state) => {
      dateRange = state.dateRange;
      isLoading = state.isLoading;
      conflicts = state.conflicts;
    });
    return unsubscribe;
  });

  // Format date range for display
  const dateRangeDisplay = $derived.by(() => {
    const start = new Date(dateRange.start);
    const end = new Date(dateRange.end);

    const startMonth = formatMonthShort(start);
    const endMonth = formatMonthShort(end);
    const year = start.getFullYear();

    if (startMonth === endMonth) {
      return `${startMonth} ${start.getDate()} - ${end.getDate()}, ${year}`;
    }
    return `${startMonth} ${start.getDate()} - ${endMonth} ${end.getDate()}, ${year}`;
  });

  // Check if current week is displayed
  const isCurrentWeek = $derived.by(() => {
    const today = new Date();
    const start = new Date(dateRange.start);
    const end = new Date(dateRange.end);
    return today >= start && today <= end;
  });

  const hasConflicts = $derived(conflicts.length > 0);
</script>

<div class="effort-toolbar {className}">
  <div class="effort-toolbar__nav">
    <IconButton
      icon="chevron-left"
      size="sm"
      variant="ghost"
      title="Previous week"
      onclick={previousWeek}
      disabled={isLoading}
    />

    <span class="effort-toolbar__date-text">{dateRangeDisplay}</span>

    <IconButton
      icon="chevron-right"
      size="sm"
      variant="ghost"
      title="Next week"
      onclick={nextWeek}
      disabled={isLoading}
    />
  </div>

  <div class="effort-toolbar__actions">
    {#if hasConflicts}
      <Badge variant="warning" size="sm">
        {formatNumber(conflicts.length)} conflict{conflicts.length > 1 ? 's' : ''}
      </Badge>
    {/if}

    {#if !isCurrentWeek}
      <Button
        variant="ghost"
        size="sm"
        onclick={goToToday}
        disabled={isLoading}
      >
        Today
      </Button>
    {/if}

    <IconButton
      icon="settings-complex"
      size="sm"
      variant="ghost"
      title="Working day settings"
      onclick={onSettingsClick}
    />

    <IconButton
      icon="download"
      size="sm"
      variant="ghost"
      title="Export effort data"
      onclick={onExportClick}
    />
  </div>
</div>

<style>
  .effort-toolbar {
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding: 0.75rem 1rem;
    border-bottom: 1px solid var(--color-border);
    background-color: var(--color-bg-primary);
  }

  .effort-toolbar__nav {
    display: flex;
    align-items: center;
    gap: 0.5rem;
  }

  .effort-toolbar__date-range {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    padding: 0.375rem 0.75rem;
    border-radius: var(--radius-md, 6px);
    background: transparent;
    border: none;
    cursor: pointer;
    transition: background-color 150ms ease-in-out;
  }

  .effort-toolbar__date-range:hover:not(:disabled) {
    background-color: var(--color-bg-hover);
  }

  .effort-toolbar__date-range:disabled {
    cursor: not-allowed;
    opacity: 0.6;
  }

  .effort-toolbar__date-text {
    font-size: var(--text-sm);
    font-weight: 500;
    color: var(--color-text-primary);
  }

  .effort-toolbar__actions {
    display: flex;
    align-items: center;
    gap: 0.5rem;
  }
</style>
