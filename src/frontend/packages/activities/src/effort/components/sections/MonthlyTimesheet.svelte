<!-- Section: MonthlyTimesheet — Projects > Effort -->
<script lang="ts">
  import { Button } from '@sddp/ui';
  import { formatDateWithOptions } from '@sddp/shell';
  import type { DailyEffort } from '../../types';
  import { formatHours, getUtilizationGrade } from '../../stores';
  import { getWeekBoundaries, getDateRange, parseDate, isWeekend, formatDate } from '../../services';

  interface Props {
    dailyEffort: DailyEffort[];
    monthStart: string;
    monthEnd: string;
    selectedWeekStart?: string | null;
    onWeekSelect?: (range: { start: string; end: string }) => void;
    class?: string;
  }

  let {
    dailyEffort,
    monthStart,
    monthEnd,
    selectedWeekStart = null,
    onWeekSelect,
    class: className = '',
  }: Props = $props();

  interface MonthDay {
    date: string;
    effort: DailyEffort | null;
    isInMonth: boolean;
  }

  interface MonthWeek {
    weekStart: string;
    weekEnd: string;
    days: MonthDay[];
  }

  const effortMap = $derived.by(() => new Map(dailyEffort.map((d) => [d.date, d])));

  const weeks = $derived.by(() => {
    if (!monthStart || !monthEnd) return [] as MonthWeek[];
    const monthStartDate = parseDate(monthStart);
    const monthEndDate = parseDate(monthEnd);
    const gridStart = getWeekBoundaries(monthStartDate).start;
    const gridEnd = getWeekBoundaries(monthEndDate).end;
    const dates = getDateRange(gridStart, gridEnd);

    const result: MonthWeek[] = [];
    for (let i = 0; i < dates.length; i += 7) {
      const weekDates = dates.slice(i, i + 7);
      const weekStart = weekDates[0];
      const weekEnd = weekDates[weekDates.length - 1];
      const days = weekDates.map((date) => ({
        date,
        effort: effortMap.get(date) ?? null,
        isInMonth: date >= monthStart && date <= monthEnd,
      }));
      result.push({ weekStart: weekStart!, weekEnd: weekEnd!, days });
    }
    return result;
  });

  const weekdayLabels = ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'];

  function formatDayNumber(dateStr: string): string {
    return formatDateWithOptions(dateStr, { day: 'numeric' });
  }

  function isToday(dateStr: string): boolean {
    return dateStr === formatDate(new Date());
  }

  function handleWeekSelect(week: MonthWeek): void {
    onWeekSelect?.({ start: week.weekStart, end: week.weekEnd });
  }

  function getUtilizationColor(effort: DailyEffort | null): string {
    if (!effort || effort.allocatedHours <= 0) return 'var(--color-text-tertiary)';
    return getUtilizationGrade((effort.spentHours / effort.allocatedHours) * 100).color;
  }
</script>

<div class="monthly-timesheet {className}">
  <div class="monthly-timesheet__grid">
    <div class="monthly-timesheet__row monthly-timesheet__row--header">
      <div class="monthly-timesheet__week-col"></div>
      {#each weekdayLabels as label (label)}
        <div class="monthly-timesheet__day-header">{label}</div>
      {/each}
    </div>

    {#each weeks as week (week.weekStart)}
      <div
        class="monthly-timesheet__row"
        class:monthly-timesheet__row--selected={week.weekStart === selectedWeekStart}
      >
        <Button
          variant="unstyled"
          class="monthly-timesheet__week-label"
          onclick={() => handleWeekSelect(week)}
        >
          {formatDateWithOptions(week.weekStart, { month: 'numeric', day: 'numeric' })}
          -
          {formatDateWithOptions(week.weekEnd, { month: 'numeric', day: 'numeric' })}
        </Button>

        {#each week.days as day (day.date)}
          <div
            class="monthly-timesheet__day"
            class:monthly-timesheet__day--outside={!day.isInMonth}
            class:monthly-timesheet__day--today={isToday(day.date)}
            class:monthly-timesheet__day--offday={day.effort ? !day.effort.isWorkingDay : isWeekend(day.date)}
          >
            <div class="monthly-timesheet__day-number">{formatDayNumber(day.date)}</div>
            {#if day.effort}
              <div class="monthly-timesheet__day-hours" style="color: {getUtilizationColor(day.effort)}">
                <span>{formatHours(day.effort.spentHours)}</span>
                <span class="monthly-timesheet__day-sep">/</span>
                <span>{formatHours(day.effort.allocatedHours)}</span>
              </div>
            {:else}
              <div class="monthly-timesheet__day-hours monthly-timesheet__day-hours--empty">-</div>
            {/if}
          </div>
        {/each}
      </div>
    {/each}
  </div>
</div>

<style>
  .monthly-timesheet {
    width: 100%;
    --monthly-today-border: color-mix(in srgb, var(--color-accent-primary) 45%, var(--color-border-secondary));
    --monthly-today-ring: color-mix(in srgb, var(--color-accent-primary) 28%, transparent);
  }

  .monthly-timesheet__grid {
    display: flex;
    flex-direction: column;
    gap: 0.25rem;
  }

  .monthly-timesheet__row {
    display: grid;
    grid-template-columns: 86px repeat(7, minmax(0, 1fr));
    gap: 0.25rem;
  }

  .monthly-timesheet__row--header {
    margin-bottom: 0.25rem;
  }

  :global(.monthly-timesheet__row--selected .monthly-timesheet__week-label) {
    background-color: color-mix(in srgb, var(--color-accent-primary) 10%, transparent);
    color: var(--color-text-primary);
  }

  .monthly-timesheet__week-col {
    height: 100%;
  }

  :global(.monthly-timesheet__week-label) {
    width: 100%;
    padding: 0.4rem 0.5rem;
    font-size: var(--text-xs);
    font-weight: 600;
    color: var(--color-text-secondary);
    background-color: var(--color-bg-tertiary);
    border: 1px solid var(--color-border);
    border-radius: var(--radius-sm);
    text-align: center;
    cursor: pointer;
  }

  :global(.monthly-timesheet__week-label:hover) {
    background-color: var(--color-bg-hover);
  }

  .monthly-timesheet__day-header {
    text-align: center;
    font-size: var(--text-2xs, 0.65rem);
    font-weight: 600;
    color: var(--color-text-tertiary);
    text-transform: uppercase;
    letter-spacing: 0.04em;
  }

  .monthly-timesheet__day {
    display: flex;
    flex-direction: column;
    gap: 0.2rem;
    padding: 0.35rem 0.45rem;
    border: 1px solid var(--color-border-secondary);
    border-radius: var(--radius-sm);
    background-color: var(--color-bg-primary);
    min-height: 3rem;
  }

  .monthly-timesheet__day--outside {
    opacity: 0.45;
  }

  .monthly-timesheet__day--today {
    border-color: var(--monthly-today-border);
    box-shadow: 0 0 0 1px var(--monthly-today-ring);
  }

  .monthly-timesheet__day--offday {
    background-color: var(--color-bg-secondary);
  }

  .monthly-timesheet__day-number {
    font-size: var(--text-xs);
    font-weight: 600;
    color: var(--color-text-secondary);
  }

  .monthly-timesheet__day-hours {
    display: flex;
    align-items: baseline;
    gap: 0.2rem;
    font-size: var(--text-2xs, 0.65rem);
    font-weight: 500;
    font-variant-numeric: tabular-nums;
  }

  .monthly-timesheet__day-hours--empty {
    color: var(--color-text-tertiary);
  }

  .monthly-timesheet__day-sep {
    color: var(--color-text-tertiary);
  }
</style>
