<script lang="ts">
  /**
   * Area Chart
   * SVG-based area chart with filled region
   */

  interface DataPoint {
    label?: string;
    date?: string;
    value: number;
  }

  interface Props {
    data?: DataPoint[];
    title?: string;
    height?: number;
    showGrid?: boolean;
    showLabels?: boolean;
    color?: string;
    fillOpacity?: number;
  }

  let {
    data = [],
    title = '',
    height = 180,
    showGrid = true,
    showLabels = true,
    color = 'var(--color-accent-primary)',
    fillOpacity = 0.3,
  }: Props = $props();

  // Chart dimensions
  const padding = { top: 20, right: 20, bottom: 30, left: 40 };
  const chartWidth = 400;
  const chartHeight = $derived(height);

  const innerWidth = $derived(chartWidth - padding.left - padding.right);
  const innerHeight = $derived(chartHeight - padding.top - padding.bottom);

  // Calculate min/max values
  const minValue = $derived(data.length > 0 ? Math.min(...data.map((d) => d.value)) : 0);
  const maxValue = $derived(data.length > 0 ? Math.max(...data.map((d) => d.value)) : 10);
  const valueRange = $derived(maxValue - minValue || 1);

  // Add padding to the value range
  const paddedMin = $derived(Math.max(0, minValue - valueRange * 0.1));
  const paddedMax = $derived(maxValue + valueRange * 0.1);
  const paddedRange = $derived(paddedMax - paddedMin || 1);

  // Calculate points
  const points = $derived(() => {
    if (data.length === 0) return [];

    return data.map((point, index) => {
      const x = padding.left + (index / Math.max(1, data.length - 1)) * innerWidth;
      const y = padding.top + innerHeight - ((point.value - paddedMin) / paddedRange) * innerHeight;
      return { x, y, ...point };
    });
  });

  // Create line path
  const linePath = $derived(() => {
    const pts = points();
    if (pts.length === 0) return '';

    return pts.map((p, i) => `${i === 0 ? 'M' : 'L'} ${p.x} ${p.y}`).join(' ');
  });

  // Create area path (closed polygon)
  const areaPath = $derived(() => {
    const pts = points();
    if (pts.length === 0) return '';

    const baseline = padding.top + innerHeight;
    const firstX = pts[0]!.x;
    const lastX = pts[pts.length - 1]!.x;

    return `${linePath()} L ${lastX} ${baseline} L ${firstX} ${baseline} Z`;
  });

  // Y-axis ticks
  const yTicks = $derived(() => {
    const tickCount = 5;
    const ticks = [];
    for (let i = 0; i <= tickCount; i++) {
      const value = paddedMin + (paddedRange * i) / tickCount;
      const y = padding.top + innerHeight - (i / tickCount) * innerHeight;
      ticks.push({ value: Math.round(value), y });
    }
    return ticks;
  });
</script>

<div class="area-chart">
  {#if title}
    <h3>{title}</h3>
  {/if}

  {#if data.length > 0}
    <div class="chart-container">
      <svg
        width="100%"
        height={chartHeight}
        viewBox="0 0 {chartWidth} {chartHeight}"
        preserveAspectRatio="xMidYMid meet"
      >
        <!-- Grid lines -->
        {#if showGrid}
          {#each yTicks() as tick (tick.value)}
            <line
              x1={padding.left}
              y1={tick.y}
              x2={chartWidth - padding.right}
              y2={tick.y}
              class="grid-line"
            />
          {/each}
        {/if}

        <!-- Y-axis labels -->
        {#if showLabels}
          {#each yTicks() as tick (tick.value)}
            <text x={padding.left - 8} y={tick.y} class="axis-label y-label">
              {tick.value}
            </text>
          {/each}
        {/if}

        <!-- Area fill -->
        <path d={areaPath()} fill={color} fill-opacity={fillOpacity} class="area-fill" />

        <!-- Line -->
        <path d={linePath()} fill="none" stroke={color} stroke-width="2" class="area-line" />

        <!-- Data points -->
        {#each points() as point, i (i)}
          <circle cx={point.x} cy={point.y} r="4" fill={color} class="data-point">
            <title>{point.label ?? point.date ?? `Point ${i + 1}`}: {point.value}</title>
          </circle>
        {/each}

        <!-- X-axis labels -->
        {#if showLabels && data.length <= 14}
          {#each points() as point, i (i)}
            {#if i % Math.ceil(data.length / 7) === 0 || i === data.length - 1}
              <text x={point.x} y={chartHeight - 8} class="axis-label x-label">
                {point.label ?? point.date?.slice(-5) ?? i + 1}
              </text>
            {/if}
          {/each}
        {/if}
      </svg>
    </div>
  {:else}
    <div class="empty-chart">
      <span class="codicon codicon-graph-line"></span>
      <span>No data available</span>
    </div>
  {/if}
</div>

<style>
  .area-chart {
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

  .chart-container {
    width: 100%;
  }

  .grid-line {
    stroke: var(--color-border);
    stroke-width: 1;
    stroke-dasharray: 4 4;
  }

  .axis-label {
    font-size: 10px;
    fill: var(--color-text-tertiary);
    font-family: var(--font-family);
  }

  .y-label {
    text-anchor: end;
    dominant-baseline: middle;
  }

  .x-label {
    text-anchor: middle;
    dominant-baseline: hanging;
  }

  .area-fill {
    transition: fill-opacity 0.2s ease;
  }

  .area-line {
    transition: stroke-width 0.2s ease;
  }

  .data-point {
    transition:
      r 0.2s ease,
      opacity 0.2s ease;
    cursor: pointer;
  }

  .data-point:hover {
    r: 6;
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
