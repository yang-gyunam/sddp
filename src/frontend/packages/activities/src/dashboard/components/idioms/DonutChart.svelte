<script lang="ts">
  /**
   * Donut Chart
   * SVG-based donut chart visualization
   */
  import { Button } from '@sddp/ui';
  import { CHART_PALETTE } from '../../../shared/constants/semanticColors';

  interface DataItem {
    label: string;
    value: number;
    color?: string;
  }

  interface Props {
    data?: DataItem[];
    title?: string;
    showLegend?: boolean;
    size?: number;
    strokeWidth?: number;
    onSegmentClick?: (label: string, value: number) => void;
  }

  let {
    data = [],
    title = '',
    showLegend = true,
    size = 160,
    strokeWidth = 24,
    onSegmentClick,
  }: Props = $props();

  // Default color palette
  const defaultColors = CHART_PALETTE;

  // Calculate total and percentages
  const total = $derived(data.reduce((sum, item) => sum + item.value, 0));

  // Calculate segments with angles
  const segments = $derived(() => {
    if (total === 0) return [];

    const radius = (size - strokeWidth) / 2;
    const circumference = 2 * Math.PI * radius;
    let cumulativePercent = 0;

    return data.map((item, index) => {
      const percent = item.value / total;
      const dashArray = percent * circumference;
      const dashOffset = -cumulativePercent * circumference;
      const color = item.color || defaultColors[index % defaultColors.length];

      cumulativePercent += percent;

      return {
        ...item,
        percent,
        dashArray,
        dashOffset,
        circumference,
        color,
      };
    });
  });

  const center = $derived(size / 2);
  const radius = $derived((size - strokeWidth) / 2);
</script>

<div class="donut-chart">
  {#if title}
    <h3>{title}</h3>
  {/if}

  {#if data.length > 0 && total > 0}
    <div class="chart-content">
      <div class="chart-svg-container">
        <svg width={size} height={size} viewBox="0 0 {size} {size}">
          <!-- Background circle -->
          <circle
            cx={center}
            cy={center}
            r={radius}
            fill="none"
            stroke="var(--color-bg-tertiary)"
            stroke-width={strokeWidth}
          />

          <!-- Data segments -->
          {#each segments() as segment, i (i)}
            <!-- svelte-ignore a11y_click_events_have_key_events -->
            <!-- svelte-ignore a11y_no_static_element_interactions -->
            <circle
              cx={center}
              cy={center}
              r={radius}
              fill="none"
              stroke={segment.color}
              stroke-width={strokeWidth}
              stroke-dasharray="{segment.dashArray} {segment.circumference}"
              stroke-dashoffset={segment.dashOffset}
              transform="rotate(-90 {center} {center})"
              class="segment"
              class:clickable={!!onSegmentClick}
              onclick={() => onSegmentClick?.(segment.label, segment.value)}
            />
          {/each}

          <!-- Center text -->
          <text
            x={center}
            y={center}
            text-anchor="middle"
            dominant-baseline="middle"
            class="center-text"
          >
            <tspan x={center} dy="-0.3em" class="center-value">{total}</tspan>
            <tspan x={center} dy="1.4em" class="center-label">Total</tspan>
          </text>
        </svg>
      </div>

      {#if showLegend}
        <div class="legend">
          {#each segments() as segment (segment.label)}
            <Button
              variant="unstyled"
              class="legend-item {onSegmentClick ? 'clickable' : ''}"
              onclick={() => onSegmentClick?.(segment.label, segment.value)}
              disabled={!onSegmentClick}
            >
              <span class="legend-color" style="background: {segment.color}"></span>
              <span class="legend-label">{segment.label}</span>
              <span class="legend-value">{segment.value} ({(segment.percent * 100).toFixed(0)}%)</span>
            </Button>
          {/each}
        </div>
      {/if}
    </div>
  {:else}
    <div class="empty-chart">
      <span class="codicon codicon-pie-chart"></span>
      <span>No data available</span>
    </div>
  {/if}
</div>

<style>
  .donut-chart {
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

  .chart-content {
    display: flex;
    align-items: center;
    gap: 1.5rem;
    flex-wrap: wrap;
    justify-content: center;
  }

  .chart-svg-container {
    flex-shrink: 0;
  }

  .segment {
    transition: opacity 0.2s ease;
  }

  .segment:hover {
    opacity: 0.8;
  }

  .segment.clickable {
    cursor: pointer;
  }

  .center-text {
    font-family: var(--font-family);
  }

  .center-value {
    font-size: 1.5rem;
    font-weight: 700;
    fill: var(--color-text-primary);
  }

  .center-label {
    font-size: 0.75rem;
    fill: var(--color-text-tertiary);
  }

  .legend {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
    min-width: 120px;
  }

  .legend :global(.legend-item) {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    font-size: 0.8125rem;
  }

  .legend :global(.legend-item.clickable) {
    cursor: pointer;
  }

  .legend :global(.legend-item.clickable:hover) {
    background: var(--color-bg-tertiary);
    border-radius: 4px;
  }

  .legend-color {
    width: 12px;
    height: 12px;
    border-radius: 2px;
    flex-shrink: 0;
  }

  .legend-label {
    color: var(--color-text-secondary);
    flex: 1;
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
  }

  .legend-value {
    color: var(--color-text-primary);
    font-weight: 500;
    white-space: nowrap;
  }

  .empty-chart {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    height: 160px;
    gap: 0.5rem;
    color: var(--color-text-tertiary);
  }

  .empty-chart .codicon {
    font-size: 2rem;
    opacity: 0.5;
  }
</style>
