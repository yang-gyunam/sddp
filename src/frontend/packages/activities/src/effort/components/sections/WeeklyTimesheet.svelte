<!-- Section: WeeklyTimesheet — Projects > Effort -->
<script lang="ts">
  import { Button, Icon, Badge, Input, RadialProgress } from '@sddp/ui';
  import { formatDateWithOptions, formatPercent, toast } from '@sddp/shell';
  import type { WeekData, DailyEffort } from '../../types';
  import { formatHours, updateAllocation, logWork, getUtilizationGrade } from '../../stores';
  import { formatDate } from '../../services';
  import { HoursBar } from '../idioms';

  interface Props {
    weekData: WeekData;
    userId: string;
    userName: string;
    editable?: boolean;
    showHeader?: boolean;
    variant?: 'card' | 'embedded';
    onCellClick?: (date: string) => void;
    class?: string;
  }

  let {
    weekData,
    userId,
    userName,
    editable = true,
    showHeader = true,
    variant = 'card',
    onCellClick,
    class: className = '',
  }: Props = $props();

  // Editing state
  let editingCell: { date: string; type: 'allocated' | 'spent' } | null = $state(null);
  let editValue = $state('');

  function getDayName(dateStr: string): string {
    return formatDateWithOptions(dateStr, { weekday: 'short' });
  }

  function formatDateShort(dateStr: string): string {
    return formatDateWithOptions(dateStr, { month: 'numeric', day: 'numeric' });
  }

  function isToday(dateStr: string): boolean {
    return dateStr === formatDate(new Date());
  }

  function getCellClass(day: DailyEffort): string {
    const classes = ['timesheet__cell'];

    if (!day.isWorkingDay) {
      classes.push('timesheet__cell--offday');
    }
    if (day.hasConflict) {
      classes.push('timesheet__cell--conflict');
    }
    if (isToday(day.date)) {
      classes.push('timesheet__cell--today');
    }

    return classes.join(' ');
  }

  function startEditing(date: string, type: 'allocated' | 'spent', currentValue: number): void {
    if (!editable) return;

    editingCell = { date, type };
    editValue = currentValue > 0 ? currentValue.toString() : '';
  }

  async function saveEdit(): Promise<void> {
    if (!editingCell) return;

    const hours = parseFloat(editValue) || 0;

    try {
      if (editingCell.type === 'allocated') {
        await updateAllocation(userId, editingCell.date, hours);
      } else {
        await logWork(userId, editingCell.date, hours);
      }
    } catch (err) {
      console.error('Failed to save:', err);
      toast.error(err instanceof Error ? err.message : 'Failed to save');
    }

    editingCell = null;
    editValue = '';
  }

  function cancelEdit(): void {
    editingCell = null;
    editValue = '';
  }

  function handleKeydown(e: KeyboardEvent): void {
    if (e.key === 'Enter') {
      saveEdit();
    } else if (e.key === 'Escape') {
      cancelEdit();
    }
  }

  // Total utilization
  const totalUtilization = $derived(
    weekData.totalAllocated > 0
      ? Math.round((weekData.totalSpent / weekData.totalAllocated) * 100)
      : 0
  );
  const totalUtilizationLabel = $derived(
    formatPercent(totalUtilization / 100, { maximumFractionDigits: 0 })
  );
</script>

<div class="timesheet {variant === 'embedded' ? 'timesheet--embedded' : ''} {className}">
  {#if showHeader}
    <div class="timesheet__header">
      <div class="timesheet__member">
        <span class="timesheet__member-name">{userName}</span>
        <span class="timesheet__member-summary">
          {formatHours(weekData.totalSpent)} / {formatHours(weekData.totalAllocated)}
          <span class="timesheet__utilization">({totalUtilizationLabel})</span>
        </span>
      </div>
    </div>
  {/if}

  <div class="timesheet__grid">
    <!-- Header row -->
    <div class="timesheet__row timesheet__row--header">
      <div class="timesheet__label-cell"></div>
      {#each weekData.days as day (day.date)}
        <div class="timesheet__header-cell" class:timesheet__header-cell--today={isToday(day.date)}>
          <span class="timesheet__day-label">
            <span class="timesheet__day-date">{formatDateShort(day.date)}</span>
            <span class="timesheet__day-name">({getDayName(day.date)})</span>
          </span>
          {#if !day.isWorkingDay}
            <Badge variant="default" size="sm">
              {day.workingDayType === 'holiday' ? 'Holiday' : 'Off'}
            </Badge>
          {/if}
        </div>
      {/each}
      <div class="timesheet__total-cell">Total</div>
    </div>

    <!-- Hours row -->
    <div class="timesheet__row">
      <div class="timesheet__label-cell">
        <Icon name="clock" size="sm" />
        <span>Hours</span>
      </div>
      {#each weekData.days as day (day.date)}
        {@const isEditing = editingCell?.date === day.date && editingCell?.type === 'spent'}
        <div class={getCellClass(day)}>
          {#if isEditing}
            <Input
              type="number"
              bind:value={editValue}
              size="sm"
              onblur={saveEdit}
              onkeydown={handleKeydown}
              class="timesheet__input"
            />
          {:else}
            <Button
              variant="unstyled"
              class="timesheet__value"
              onclick={() => {
                onCellClick?.(day.date);
                startEditing(day.date, 'spent', day.spentHours);
              }}
              disabled={!editable}
            >
              <HoursBar allocated={day.allocatedHours} spent={day.spentHours} showValue={false} />
            </Button>
          {/if}
          {#if day.hasConflict}
            <Icon name="alert-triangle" size="xs" class="timesheet__conflict-icon" />
          {/if}
        </div>
      {/each}
      <div class="timesheet__total-cell timesheet__total-cell--value">
        <RadialProgress
          value={totalUtilization}
          size="sm"
          variant={getUtilizationGrade(totalUtilization).variant}
        />
      </div>
    </div>

    <!-- Status row -->
    <div class="timesheet__row timesheet__row--status">
      <div class="timesheet__label-cell">
        <span>Status</span>
      </div>
      {#each weekData.days as day (day.date)}
        <div class="timesheet__status-cell">
          {#if day.allocatedHours === 0 && day.spentHours === 0}
            <span class="timesheet__status timesheet__status--empty">-</span>
          {:else if day.spentHours >= day.allocatedHours}
            <Icon name="check-circle" size="sm" class="timesheet__status--complete" />
          {:else}
            <span class="timesheet__status timesheet__status--progress">
              {day.spentHours}h/{day.allocatedHours}h
            </span>
          {/if}
        </div>
      {/each}
      <div class="timesheet__total-cell timesheet__total-cell--status">
        {#if weekData.totalSpent >= weekData.totalAllocated}
          <Icon name="check-circle" size="sm" class="timesheet__status--complete" />
          <span class="timesheet__status">({totalUtilizationLabel})</span>
        {:else}
          <span class="timesheet__status timesheet__status--progress">
            {formatHours(weekData.totalSpent)}/{formatHours(weekData.totalAllocated)} ({totalUtilizationLabel})
          </span>
        {/if}
      </div>
    </div>
  </div>
</div>

<style>
  .timesheet {
    --timesheet-today-header-bg: color-mix(in srgb, var(--color-accent-primary) 12%, var(--color-bg-secondary));
    --timesheet-today-cell-bg: color-mix(in srgb, var(--color-accent-primary) 8%, var(--color-bg-primary));
    --timesheet-today-border: color-mix(in srgb, var(--color-accent-primary) 35%, var(--color-border));
    background-color: var(--color-bg-primary);
    border: 1px solid var(--color-border);
    border-radius: var(--radius-lg, 8px);
    overflow: hidden;
  }

  .timesheet--embedded {
    background-color: transparent;
    border: none;
    border-radius: 0;
  }

  .timesheet__header {
    padding: 0.75rem 1rem;
    border-bottom: 1px solid var(--color-border);
    background-color: var(--color-bg-secondary);
  }

  .timesheet__member {
    display: flex;
    align-items: center;
    gap: 1rem;
  }

  .timesheet__member-name {
    font-size: var(--text-sm);
    font-weight: 600;
    color: var(--color-text-primary);
  }

  .timesheet__member-summary {
    font-size: var(--text-xs);
    color: var(--color-text-tertiary);
  }

  .timesheet__utilization {
    color: var(--color-text-secondary);
    font-weight: 500;
  }

  .timesheet__grid {
    display: flex;
    flex-direction: column;
  }

  .timesheet__row {
    display: flex;
    border-bottom: 1px solid var(--color-border);
  }

  .timesheet__row:last-child {
    border-bottom: none;
  }

  .timesheet__row--header {
    background-color: var(--color-bg-secondary);
  }

  .timesheet__row--status {
    background-color: var(--color-bg-secondary);
  }

  .timesheet__label-cell {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    width: 100px;
    padding: 0.75rem 1rem;
    font-size: var(--text-sm);
    color: var(--color-text-secondary);
    background-color: var(--color-bg-secondary);
    border-right: 1px solid var(--color-border);
  }

  .timesheet__header-cell {
    flex: 1;
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    gap: 0.25rem;
    padding: 0.5rem;
    text-align: center;
    border-right: 1px solid var(--color-border);
  }

  .timesheet__header-cell:last-of-type {
    border-right: none;
  }

  .timesheet__header-cell--today {
    background-color: var(--timesheet-today-header-bg);
    box-shadow: inset 0 -1px 0 var(--timesheet-today-border);
  }

  .timesheet__day-label {
    display: flex;
    flex-wrap: wrap;
    justify-content: center;
    gap: 0.25rem;
  }

  .timesheet__day-date {
    font-size: var(--text-xs);
    font-weight: 600;
    color: var(--color-text-primary);
  }

  .timesheet__day-name {
    font-size: var(--text-xs);
    color: var(--color-text-tertiary);
  }

  .timesheet__cell {
    flex: 1;
    display: flex;
    align-items: center;
    justify-content: center;
    padding: 0.5rem;
    border-right: 1px solid var(--color-border);
    position: relative;
    min-height: 48px;
  }

  .timesheet__cell:last-of-type {
    border-right: none;
  }

  .timesheet__cell--offday {
    background-color: var(--color-bg-tertiary);
  }

  .timesheet__cell--conflict {
    background-color: var(--color-warning-50);
  }

  :global(.dark) .timesheet__cell--conflict {
    background-color: rgba(245, 158, 11, 0.1);
  }

  .timesheet__cell--today {
    background-color: var(--timesheet-today-cell-bg);
    box-shadow: inset 0 0 0 1px var(--timesheet-today-border);
  }

  .timesheet__value {
    display: flex;
    align-items: center;
    justify-content: center;
    padding: 0.25rem;
    background: transparent;
    border: none;
    cursor: pointer;
    border-radius: var(--radius-sm, 4px);
  }

  .timesheet__value:disabled {
    cursor: default;
    opacity: 0.5;
  }

  :global(.timesheet__input) {
    width: 50px;
    text-align: center;
  }

  :global(.timesheet__conflict-icon) {
    position: absolute;
    top: 4px;
    right: 4px;
    color: var(--color-warning-500);
  }

  .timesheet__total-cell {
    flex-shrink: 0;
    width: 100px;
    display: flex;
    align-items: center;
    justify-content: center;
    padding: 0.5rem;
    font-size: var(--text-xs);
    color: var(--color-text-secondary);
    background-color: var(--color-bg-secondary);
    border-left: 1px solid var(--color-border);
  }

  .timesheet__total-cell--value {
    padding: 0.5rem 0.75rem;
  }

  .timesheet__total-cell--status {
    flex-direction: row;
    flex-wrap: wrap;
    gap: 0.25rem;
    white-space: nowrap;
  }

  .timesheet__status-cell {
    flex: 1;
    display: flex;
    align-items: center;
    justify-content: center;
    padding: 0.5rem;
    border-right: 1px solid var(--color-border);
  }

  .timesheet__status-cell:last-of-type {
    border-right: none;
  }

  .timesheet__status {
    font-size: var(--text-xs);
    font-variant-numeric: tabular-nums;
  }

  .timesheet__status--empty {
    color: var(--color-text-tertiary);
  }

  .timesheet__status--progress {
    color: var(--color-warning-600);
    font-weight: 500;
  }

  :global(.dark) .timesheet__status--progress {
    color: var(--color-warning-400);
  }

  :global(.timesheet__status--complete) {
    color: var(--color-success-500);
  }
</style>
