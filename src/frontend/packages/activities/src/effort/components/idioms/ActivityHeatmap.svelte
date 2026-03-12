<!--
  ActivityHeatmap Component
  GitHub-style contribution heatmap showing utilization over 12 weeks

  Y-axis: Days of week (Mon-Fri)
  X-axis: Weeks (12 weeks, newest on right)
  Color: Utilization rate (Spent/Allocated)
-->
<script lang="ts">
  import { Button } from '@sddp/ui';
  import { formatDateWithOptions, formatNumber, formatPercent } from '@sddp/shell';
  import type { HeatmapCell, HeatmapWeek } from '../../types';

  interface Props {
    /** Heatmap data - 12 weeks of daily utilization */
    data: HeatmapWeek[];
    /** Number of weeks to display */
    weeks?: number;
    /** Callback when a cell is clicked */
    onCellClick?: (date: string) => void;
    /** Additional CSS classes */
    class?: string;
  }

  let {
    data,
    weeks = 12,
    onCellClick,
    class: className = '',
  }: Props = $props();

  const dayLabels = ['Mon', 'Tue', 'Wed', 'Thu', 'Fri'];

  // Get color based on utilization
  function getColor(utilization: number | null): string {
    if (utilization === null) return 'var(--color-bg-tertiary)';
    if (utilization === 0) return 'var(--color-bg-tertiary)';
    if (utilization < 50) return 'var(--color-danger-200)';
    if (utilization < 80) return 'var(--color-warning-300)';
    if (utilization < 100) return 'var(--color-accent-primary)';
    return 'var(--color-success-400)';
  }

  // Get dark mode color
  function getDarkColor(utilization: number | null): string {
    if (utilization === null) return 'var(--color-bg-tertiary)';
    if (utilization === 0) return 'var(--color-bg-tertiary)';
    if (utilization < 50) return 'var(--color-danger-800)';
    if (utilization < 80) return 'var(--color-warning-700)';
    if (utilization < 100) return 'var(--color-accent-primary)';
    return 'var(--color-success-600)';
  }

  // Format date for tooltip
  function formatDate(dateStr: string): string {
    return formatDateWithOptions(dateStr, { month: 'short', day: 'numeric' });
  }

  // Get tooltip text
  function getTooltip(cell: HeatmapCell | null): string {
    if (!cell) return 'No data';
    if (cell.allocated === 0) return `${formatDate(cell.date)}: No allocation`;
    const utilizationLabel = formatPercent(Math.round(cell.utilization) / 100, { maximumFractionDigits: 0 });
    return `${formatDate(cell.date)}: ${formatNumber(cell.spent)}h/${formatNumber(cell.allocated)}h (${utilizationLabel})`;
  }
</script>

<div class="heatmap {className}">
  <div class="heatmap__container">
    <!-- Day labels (Y-axis) -->
    <div class="heatmap__day-labels">
      {#each dayLabels as day (day)}
        <span class="heatmap__day-label">{day}</span>
      {/each}
    </div>

    <!-- Grid -->
    <div class="heatmap__grid" style="--weeks: {weeks}">
      {#each data.slice(-weeks) as week, weekIdx (week.weekStart ?? weekIdx)}
        <div class="heatmap__week">
          {#each week.days as cell, dayIdx (cell?.date ?? `${week.weekStart}-${dayIdx}`)}
            <Button
              variant="unstyled"
              class="heatmap__cell"
              style="--cell-color: {getColor(cell?.utilization ?? null)}; --cell-color-dark: {getDarkColor(cell?.utilization ?? null)}"
              title={getTooltip(cell)}
              onclick={() => cell && onCellClick?.(cell.date)}
              disabled={!cell}
            ></Button>
          {/each}
        </div>
      {/each}
    </div>
  </div>

  <!-- Legend -->
  <div class="heatmap__legend">
    <span class="heatmap__legend-label">Less</span>
    <div class="heatmap__legend-cells">
      <span class="heatmap__legend-cell" style="--cell-color: var(--color-bg-tertiary); --cell-color-dark: var(--color-bg-tertiary)" title="0%"></span>
      <span class="heatmap__legend-cell" style="--cell-color: var(--color-danger-200); --cell-color-dark: var(--color-danger-800)" title="1-49%"></span>
      <span class="heatmap__legend-cell" style="--cell-color: var(--color-warning-300); --cell-color-dark: var(--color-warning-700)" title="50-79%"></span>
      <span class="heatmap__legend-cell" style="--cell-color: var(--color-accent-primary); --cell-color-dark: var(--color-accent-primary)" title="80-99%"></span>
      <span class="heatmap__legend-cell" style="--cell-color: var(--color-success-400); --cell-color-dark: var(--color-success-600)" title="100%+"></span>
    </div>
    <span class="heatmap__legend-label">More</span>
  </div>
</div>

<style>
  .heatmap {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
  }

  .heatmap__container {
    display: flex;
    gap: 0.5rem;
  }

  .heatmap__day-labels {
    display: flex;
    flex-direction: column;
    gap: 2px;
    padding-top: 0;
  }

  .heatmap__day-label {
    height: 12px;
    font-size: 9px;
    color: var(--color-text-tertiary);
    line-height: 12px;
    text-align: right;
    padding-right: 4px;
  }

  .heatmap__grid {
    display: flex;
    gap: 2px;
  }

  .heatmap__week {
    display: flex;
    flex-direction: column;
    gap: 2px;
  }

  .heatmap__cell {
    width: 12px;
    height: 12px;
    border-radius: 2px;
    border: none;
    padding: 0;
    background-color: var(--cell-color);
    cursor: pointer;
    transition: transform 100ms ease, opacity 100ms ease;
  }

  :global(.dark) .heatmap__cell {
    background-color: var(--cell-color-dark);
  }

  .heatmap__cell:hover:not(:disabled) {
    transform: scale(1.2);
    outline: 1px solid var(--color-border-secondary);
  }

  .heatmap__cell:disabled {
    cursor: default;
    opacity: 0.5;
  }

  .heatmap__legend {
    display: flex;
    align-items: center;
    gap: 0.375rem;
    justify-content: flex-end;
  }

  .heatmap__legend-label {
    font-size: 9px;
    color: var(--color-text-tertiary);
  }

  .heatmap__legend-cells {
    display: flex;
    gap: 2px;
  }

  .heatmap__legend-cell {
    width: 10px;
    height: 10px;
    border-radius: 2px;
    background-color: var(--cell-color);
  }

  :global(.dark) .heatmap__legend-cell {
    background-color: var(--cell-color-dark);
  }
</style>
