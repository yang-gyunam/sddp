<script lang="ts">
  /**
   * HeatmapGrid
   * GitHub-style contribution heatmap grid for a 4-week window (28 days).
   * Ordered left-to-right, top-to-bottom: oldest on top-left, newest on bottom-right.
   */

  import type { DayContribution } from '../../types';
  import { formatDateWithOptions } from '@sddp/shell';
  import { Button } from '@sddp/ui';

  interface Props {
    data?: DayContribution[];
    title?: string;
  }

  let { data = [], title = '' }: Props = $props();

  // -------------------------------------------------------------------------
  // Constants
  // -------------------------------------------------------------------------

  const COLUMNS = 4;
  const ROWS = 7;
  const TOTAL_DAYS = COLUMNS * ROWS; // 28

  /** Day-of-week labels rendered on the left side (index 0 = row 0 = oldest day row) */
  const DAY_LABELS = ['Mon', '', 'Wed', '', 'Fri', '', ''];

  // -------------------------------------------------------------------------
  // Colour mapping
  // -------------------------------------------------------------------------

  function getColor(count: number): string {
    if (count === 0) return 'var(--color-bg-tertiary)';
    if (count <= 2) return 'var(--color-heatmap-level-1)';
    if (count <= 5) return 'var(--color-heatmap-level-2)';
    if (count <= 10) return 'var(--color-heatmap-level-3)';
    return 'var(--color-heatmap-level-4)';
  }

  // -------------------------------------------------------------------------
  // Derived: normalise data into exactly TOTAL_DAYS cells
  // Cells are ordered oldest → newest, reading left-to-right, top-to-bottom.
  // The grid has COLUMNS columns and ROWS rows, so:
  //   cell index = col * ROWS + row
  // -------------------------------------------------------------------------

  const cells = $derived((): DayContribution[] => {
    if (!data || data.length === 0) return [];

    // Sort ascending by date (oldest first)
    const sorted = [...data].sort((a, b) => a.date.localeCompare(b.date));

    // Take at most the last TOTAL_DAYS entries
    const sliced = sorted.slice(-TOTAL_DAYS);

    // Pad from the front with empty entries if fewer than TOTAL_DAYS
    const padCount = TOTAL_DAYS - sliced.length;
    const padded: DayContribution[] = [];
    for (let i = 0; i < padCount; i++) {
      padded.push({ date: '', count: 0, specsCreated: 0, comments: 0, signOffs: 0, tasksCompleted: 0 });
    }
    return [...padded, ...sliced];
  });

  /**
   * Re-map the flat array (index 0 = top-left) into a column-major grid
   * so that reading column by column fills the grid left-to-right, top-to-bottom.
   * col=0 row=0 → cell index 0 (oldest), col=3 row=6 → cell index 27 (newest).
   */
  const grid = $derived((): DayContribution[][] => {
    const allCells = cells();
    if (allCells.length === 0) return [];

    const cols: DayContribution[][] = [];
    for (let col = 0; col < COLUMNS; col++) {
      const column: DayContribution[] = [];
      for (let row = 0; row < ROWS; row++) {
        const idx = col * ROWS + row;
        const cell = allCells[idx] ?? { date: '', count: 0, specsCreated: 0, comments: 0, signOffs: 0, tasksCompleted: 0 };
        column.push(cell);
      }
      cols.push(column);
    }
    return cols;
  });

  const totalContributions = $derived((): number =>
    cells().reduce((sum, c) => sum + c.count, 0)
  );

  const hasData = $derived((): boolean => data != null && data.length > 0);

  // -------------------------------------------------------------------------
  // Tooltip state
  // -------------------------------------------------------------------------

  let tooltipCell = $state<DayContribution | null>(null);
  let tooltipX = $state(0);
  let tooltipY = $state(0);

  function formatDate(dateStr: string): string {
    if (!dateStr) return '';
    const d = new Date(dateStr);
    if (isNaN(d.getTime())) return dateStr;
    return formatDateWithOptions(d, { weekday: 'short', year: 'numeric', month: 'short', day: 'numeric' });
  }

  function onCellMouseEnter(e: MouseEvent, cell: DayContribution) {
    if (!cell.date) return;
    const target = e.currentTarget as HTMLElement;
    const rect = target.getBoundingClientRect();
    const containerRect = (target.closest('.heatmap-grid') as HTMLElement)?.getBoundingClientRect();
    tooltipX = rect.left - (containerRect?.left ?? 0) + rect.width / 2;
    tooltipY = rect.top - (containerRect?.top ?? 0) - 8;
    tooltipCell = cell;
  }

  function onCellMouseLeave() {
    tooltipCell = null;
  }
</script>

<div class="heatmap-chart">
  {#if title}
    <h3>{title}</h3>
  {/if}

  {#if hasData()}
    <div class="heatmap-grid">
      <!-- Day labels column -->
      <div class="day-labels" aria-hidden="true">
        {#each DAY_LABELS as label, i (i)}
          <span class="day-label">{label}</span>
        {/each}
      </div>

      <!-- Grid columns -->
      <div class="grid-columns">
        {#each grid() as column, colIdx (colIdx)}
          <div class="grid-column">
            {#each column as cell, rowIdx (rowIdx)}
              <Button
                variant="unstyled"
                class="cell"
                style="background-color: {getColor(cell.count)};"
                aria-label={cell.date
                  ? `${formatDate(cell.date)}: ${cell.count} contribution${cell.count !== 1 ? 's' : ''}`
                  : 'No data'}
                onmouseenter={(e: MouseEvent) => onCellMouseEnter(e, cell)}
                onmouseleave={onCellMouseLeave}
              ></Button>
            {/each}
          </div>
        {/each}
      </div>

      <!-- Tooltip -->
      {#if tooltipCell && tooltipCell.date}
        <div
          class="tooltip"
          style="left: {tooltipX}px; top: {tooltipY}px;"
          role="tooltip"
        >
          <p class="tooltip-date">{formatDate(tooltipCell.date)}</p>
          <p class="tooltip-total">
            <strong>{tooltipCell.count}</strong>
            contribution{tooltipCell.count !== 1 ? 's' : ''}
          </p>
          <ul class="tooltip-breakdown">
            <li>
              <span class="tooltip-dot" style="background:var(--color-heatmap-level-2)"></span>
              Specs: {tooltipCell.specsCreated}
            </li>
            <li>
              <span class="tooltip-dot" style="background:var(--color-heatmap-level-1)"></span>
              Comments: {tooltipCell.comments}
            </li>
            <li>
              <span class="tooltip-dot" style="background:var(--color-heatmap-level-3)"></span>
              Sign-offs: {tooltipCell.signOffs}
            </li>
            <li>
              <span class="tooltip-dot" style="background:var(--color-heatmap-level-4)"></span>
              Tasks: {tooltipCell.tasksCompleted}
            </li>
          </ul>
        </div>
      {/if}
    </div>

    <!-- Legend and summary -->
    <div class="heatmap-footer">
      <span class="summary">
        {totalContributions()} contribution{totalContributions() !== 1 ? 's' : ''} in the last 28 days
      </span>
      <div class="legend" aria-label="Contribution intensity legend">
        <span class="legend-label">Less</span>
        <span class="legend-cell" style="background-color: var(--color-bg-tertiary);" aria-label="0 contributions"></span>
        <span class="legend-cell" style="background-color: var(--color-heatmap-level-1);" aria-label="1-2 contributions"></span>
        <span class="legend-cell" style="background-color: var(--color-heatmap-level-2);" aria-label="3-5 contributions"></span>
        <span class="legend-cell" style="background-color: var(--color-heatmap-level-3);" aria-label="6-10 contributions"></span>
        <span class="legend-cell" style="background-color: var(--color-heatmap-level-4);" aria-label="11+ contributions"></span>
        <span class="legend-label">More</span>
      </div>
    </div>
  {:else}
    <div class="empty-chart">
      <span class="codicon codicon-calendar"></span>
      <span>No contribution data available</span>
    </div>
  {/if}
</div>

<style>
  .heatmap-chart {
    position: relative;
    padding: 1.5rem;
    background: var(--color-bg-secondary);
    border: 1px solid var(--color-border);
    border-radius: 8px;
    min-height: 200px;
  }

  h3 {
    margin: 0 0 1rem;
    font-size: 0.875rem;
    font-weight: 600;
    color: var(--color-text-primary);
  }

  /* ------------------------------------------------------------------ */
  /* Grid layout                                                          */
  /* ------------------------------------------------------------------ */

  .heatmap-grid {
    position: relative;
    display: flex;
    align-items: flex-start;
    gap: 4px;
    width: fit-content;
  }

  .day-labels {
    display: grid;
    grid-template-rows: repeat(7, 14px);
    gap: 3px;
    padding-top: 0;
  }

  .day-label {
    font-size: 0.625rem;
    color: var(--color-text-tertiary);
    line-height: 14px;
    text-align: right;
    white-space: nowrap;
    user-select: none;
  }

  .grid-columns {
    display: flex;
    gap: 3px;
  }

  .grid-column {
    display: flex;
    flex-direction: column;
    gap: 3px;
  }

  /* ------------------------------------------------------------------ */
  /* Individual cell                                                      */
  /* ------------------------------------------------------------------ */

  :global(.cell) {
    display: block;
    width: 14px;
    height: 14px;
    border: none;
    border-radius: 2px;
    cursor: pointer;
    padding: 0;
    transition: opacity 0.15s ease, outline 0.1s ease;
    outline: 2px solid transparent;
    outline-offset: 1px;
  }

  :global(.cell:hover) {
    opacity: 0.85;
    outline-color: var(--color-text-secondary);
  }

  :global(.cell:focus-visible) {
    outline-color: var(--color-accent-primary);
    opacity: 1;
  }

  /* ------------------------------------------------------------------ */
  /* Tooltip                                                              */
  /* ------------------------------------------------------------------ */

  .tooltip {
    position: absolute;
    transform: translate(-50%, -100%);
    background: var(--color-bg-primary, #1e1e1e);
    border: 1px solid var(--color-border);
    border-radius: 6px;
    padding: 0.5rem 0.75rem;
    pointer-events: none;
    z-index: 100;
    min-width: 160px;
    box-shadow: 0 4px 12px rgba(0, 0, 0, 0.4);
  }

  .tooltip-date {
    margin: 0 0 0.25rem;
    font-size: 0.75rem;
    font-weight: 600;
    color: var(--color-text-primary);
    white-space: nowrap;
  }

  .tooltip-total {
    margin: 0 0 0.375rem;
    font-size: 0.75rem;
    color: var(--color-text-secondary);
  }

  .tooltip-total strong {
    color: var(--color-text-primary);
  }

  .tooltip-breakdown {
    margin: 0;
    padding: 0;
    list-style: none;
    display: flex;
    flex-direction: column;
    gap: 0.2rem;
    border-top: 1px solid var(--color-border);
    padding-top: 0.375rem;
  }

  .tooltip-breakdown li {
    display: flex;
    align-items: center;
    gap: 0.375rem;
    font-size: 0.6875rem;
    color: var(--color-text-secondary);
  }

  .tooltip-dot {
    display: inline-block;
    width: 8px;
    height: 8px;
    border-radius: 2px;
    flex-shrink: 0;
  }

  /* ------------------------------------------------------------------ */
  /* Footer: summary + legend                                             */
  /* ------------------------------------------------------------------ */

  .heatmap-footer {
    display: flex;
    align-items: center;
    justify-content: space-between;
    margin-top: 0.875rem;
    flex-wrap: wrap;
    gap: 0.5rem;
  }

  .summary {
    font-size: 0.75rem;
    color: var(--color-text-tertiary);
  }

  .legend {
    display: flex;
    align-items: center;
    gap: 3px;
  }

  .legend-label {
    font-size: 0.625rem;
    color: var(--color-text-tertiary);
    user-select: none;
  }

  .legend-cell {
    display: inline-block;
    width: 12px;
    height: 12px;
    border-radius: 2px;
  }

  /* ------------------------------------------------------------------ */
  /* Empty state                                                          */
  /* ------------------------------------------------------------------ */

  .empty-chart {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    height: 140px;
    gap: 0.5rem;
    color: var(--color-text-tertiary);
    font-size: 0.875rem;
  }

  .empty-chart .codicon {
    font-size: 2rem;
    opacity: 0.5;
  }
</style>
