<!-- Section: TimelineSection -->
<!--
  TimelineSection Component
  Collapsible timeline section showing recent project activity
-->
<script lang="ts">
  import { Icon, Button, Spinner } from '@sddp/ui';
  import { CollapsiblePanel } from '@sddp/shell';
  import type { TimelineEvent as TimelineEventType, TimelineFilter } from '../../types';
  import { TimelineEventItem } from '../idioms';

  interface Props {
    events: TimelineEventType[];
    filter?: TimelineFilter;
    expanded?: boolean;
    loading?: boolean;
    onToggle?: (expanded: boolean) => void;
    onEventClick?: (event: TimelineEventType) => void;
    onFilterChange?: (filter: TimelineFilter) => void;
    onRefresh?: () => void;
    class?: string;
  }

  let {
    events,
    filter = { period: 'minutes-10' },
    expanded = true,
    loading = false,
    onToggle,
    onEventClick,
    onFilterChange,
    onRefresh,
    class: className = '',
  }: Props = $props();

  // Filter menu state
  let showFilterMenu = $state(false);

  const actions = $derived([
    {
      id: 'filter',
      icon: 'filter',
      label: 'Filter',
      onClick: () => {
        showFilterMenu = !showFilterMenu;
      },
    },
    {
      id: 'refresh',
      icon: 'refresh-cw',
      label: 'Refresh',
      onClick: () => onRefresh?.(),
    },
  ]);

  const periodLabels: Record<TimelineFilter['period'], string> = {
    'minutes-10': 'Last 10 minutes',
    'hour-1': 'Last hour',
    'today': 'Today',
    'week': 'This week',
    'all': 'All time',
  };

  const periodOptions: Array<{ value: TimelineFilter['period']; label: string }> = [
    { value: 'minutes-10', label: 'Last 10 minutes' },
    { value: 'hour-1', label: 'Last hour' },
    { value: 'today', label: 'Today' },
    { value: 'week', label: 'This week' },
  ];

  function handlePeriodSelect(period: TimelineFilter['period']) {
    showFilterMenu = false;
    onFilterChange?.({ ...filter, period });
  }

  function closeFilterMenu() {
    showFilterMenu = false;
  }
</script>

<div class="relative">
  <CollapsiblePanel
    title="TIMELINE"
    {expanded}
    {actions}
    onToggle={onToggle}
    class={className}
  >
    <div class="timeline-content px-1">
      <!-- Current filter badge -->
      <div class="flex items-center gap-1 px-2 py-1 mb-1 text-xs text-[var(--color-text-tertiary)]">
        <Icon name="clock" size="xs" />
        <span>{periodLabels[filter.period]}</span>
      </div>

      {#if loading}
        <div class="flex items-center justify-center py-4">
          <Spinner size="lg" />
        </div>
      {:else if events.length === 0}
        <div class="flex flex-col items-center justify-center py-4 text-center">
          <Icon name="clock" size="md" class="text-[var(--color-text-tertiary)] mb-2" />
          <span class="text-xs text-[var(--color-text-tertiary)]">
            No activity in {periodLabels[filter.period].toLowerCase()}
          </span>
        </div>
      {:else}
        <div class="flex flex-col gap-0.5">
          {#each events as event (event.id)}
            <TimelineEventItem {event} onClick={onEventClick} />
          {/each}
        </div>
      {/if}
    </div>
  </CollapsiblePanel>

  <!-- Filter Dropdown Menu -->
  {#if showFilterMenu}
    <Button
      variant="unstyled"
      class="fixed inset-0 z-40"
      onclick={closeFilterMenu}
      onkeydown={(e) => e.key === 'Escape' && closeFilterMenu()}
      tabindex={-1}
      aria-label="Close menu"
    ></Button>
    <div
      class="absolute z-50 right-0 top-8 min-w-40 bg-[var(--color-bg-secondary)] border border-[var(--color-border)] rounded-lg shadow-lg py-1"
      role="menu"
    >
      <div class="px-3 py-1.5 text-xs font-medium text-[var(--color-text-tertiary)] uppercase">
        Time Period
      </div>
      {#each periodOptions as option (option.value)}
        <Button
          variant="unstyled"
          class="w-full flex items-center gap-2 px-3 py-2 text-sm transition-colors
            {filter.period === option.value
              ? 'bg-[var(--color-accent-primary)]/10 text-[var(--color-accent-primary)]'
              : 'text-[var(--color-text-primary)] hover:bg-[var(--color-bg-tertiary)]'}"
          onclick={() => handlePeriodSelect(option.value)}
          role="menuitem"
        >
          {#if filter.period === option.value}
            <Icon name="check" size="sm" />
          {:else}
            <span class="w-4"></span>
          {/if}
          {option.label}
        </Button>
      {/each}
    </div>
  {/if}
</div>
