<script lang="ts">
  /**
   * Line Chart
   * Simple CSS-based chart visualization
   */

  interface DataPoint {
    date?: string;
    tasksCompleted?: number;
    specsCreated?: number;
    comments?: number;
    conversations?: number;
  }

  interface Props {
    data?: DataPoint[];
    title?: string;
  }
  let { data = [], title = '' }: Props = $props();

  // Calculate max value for scaling
  const maxValue = $derived(() => {
    if (!data || data.length === 0) return 10;
    return Math.max(
      ...data.map(d => Math.max(
        d.tasksCompleted ?? 0,
        d.specsCreated ?? 0,
        d.comments ?? 0,
        d.conversations ?? 0
      ))
    ) || 10;
  });

  // Get display data (last 14 days for better visibility)
  const displayData = $derived(data.slice(-14));
</script>

<div class="line-chart">
  <h3>{title}</h3>
  {#if displayData.length > 0}
    <div class="chart-container">
      <div class="chart-bars">
        {#each displayData as point, i (i)}
          {@const value = point.tasksCompleted ?? point.specsCreated ?? point.comments ?? point.conversations ?? 0}
          {@const height = Math.max(4, (value / maxValue()) * 100)}
          <div class="bar-wrapper" title="{point.date}: {value}">
            <div class="bar" style="height: {height}%"></div>
          </div>
        {/each}
      </div>
      <div class="chart-baseline"></div>
    </div>
  {:else}
    <div class="empty-chart">
      <span class="codicon codicon-graph-line"></span>
      <span>No activity data</span>
    </div>
  {/if}
</div>

<style>
  .line-chart {
    padding: 1.5rem;
    background: var(--color-bg-secondary);
    border: 1px solid var(--color-border);
    border-radius: 8px;
    min-height: 180px;
  }

  h3 {
    margin: 0 0 1rem;
    font-size: 0.875rem;
    font-weight: 600;
    color: var(--color-text-primary);
  }

  .chart-container {
    position: relative;
    height: 120px;
  }

  .chart-bars {
    display: flex;
    align-items: flex-end;
    justify-content: space-between;
    height: 100%;
    gap: 2px;
    padding-bottom: 1px;
  }

  .bar-wrapper {
    flex: 1;
    height: 100%;
    display: flex;
    align-items: flex-end;
    cursor: pointer;
  }

  .bar {
    width: 100%;
    background: linear-gradient(to top, var(--color-accent-primary), var(--color-accent-primary));
    border-radius: 2px 2px 0 0;
    transition: height 0.3s ease, opacity 0.2s;
    min-height: 4px;
  }

  .bar-wrapper:hover .bar {
    opacity: 0.8;
  }

  .chart-baseline {
    position: absolute;
    bottom: 0;
    left: 0;
    right: 0;
    height: 1px;
    background: var(--color-border);
  }

  .empty-chart {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    height: 120px;
    gap: 0.5rem;
    color: var(--color-text-tertiary);
  }

  .empty-chart .codicon {
    font-size: 2rem;
    opacity: 0.5;
  }
</style>
