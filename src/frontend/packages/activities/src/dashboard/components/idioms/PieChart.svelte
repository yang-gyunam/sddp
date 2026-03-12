<script lang="ts">
  /**
   * Pie Chart
   * SVG-based pie chart visualization
   */
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
  }

  let {
    data = [],
    title = '',
    showLegend = true,
    size = 160,
  }: Props = $props();

  // Default color palette
  const defaultColors = CHART_PALETTE;

  // Calculate total
  const total = $derived(data.reduce((sum, item) => sum + item.value, 0));

  // Calculate slice paths
  const slices = $derived(() => {
    if (total === 0) return [];

    const center = size / 2;
    const radius = size / 2 - 4; // Small padding
    let cumulativeAngle = -90; // Start from top

    return data.map((item, index) => {
      const percent = item.value / total;
      const angle = percent * 360;
      const startAngle = cumulativeAngle;
      const endAngle = startAngle + angle;
      const color = item.color || defaultColors[index % defaultColors.length];

      // Convert angles to radians
      const startRad = (startAngle * Math.PI) / 180;
      const endRad = (endAngle * Math.PI) / 180;

      // Calculate arc points
      const x1 = center + radius * Math.cos(startRad);
      const y1 = center + radius * Math.sin(startRad);
      const x2 = center + radius * Math.cos(endRad);
      const y2 = center + radius * Math.sin(endRad);

      // Determine if arc should be large (> 180 degrees)
      const largeArc = angle > 180 ? 1 : 0;

      // Create SVG path
      const path =
        angle >= 360
          ? // Full circle case
            `M ${center} ${center - radius}
             A ${radius} ${radius} 0 1 1 ${center} ${center + radius}
             A ${radius} ${radius} 0 1 1 ${center} ${center - radius}
             Z`
          : // Normal slice
            `M ${center} ${center}
             L ${x1} ${y1}
             A ${radius} ${radius} 0 ${largeArc} 1 ${x2} ${y2}
             Z`;

      cumulativeAngle = endAngle;

      return {
        ...item,
        percent,
        path,
        color,
      };
    });
  });
</script>

<div class="pie-chart">
  {#if title}
    <h3>{title}</h3>
  {/if}

  {#if data.length > 0 && total > 0}
    <div class="chart-content">
      <div class="chart-svg-container">
        <svg width={size} height={size} viewBox="0 0 {size} {size}">
          {#each slices() as slice, i (i)}
            <path d={slice.path} fill={slice.color} class="slice" />
          {/each}
        </svg>
      </div>

      {#if showLegend}
        <div class="legend">
          {#each slices() as slice (slice.label)}
            <div class="legend-item">
              <span class="legend-color" style="background: {slice.color}"></span>
              <span class="legend-label">{slice.label}</span>
              <span class="legend-value">{slice.value} ({(slice.percent * 100).toFixed(0)}%)</span>
            </div>
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
  .pie-chart {
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

  .slice {
    transition: opacity 0.2s ease, transform 0.2s ease;
    transform-origin: center;
  }

  .slice:hover {
    opacity: 0.85;
    transform: scale(1.02);
  }

  .legend {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
    min-width: 120px;
  }

  .legend-item {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    font-size: 0.8125rem;
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
