<script lang="ts">
  /**
   * Bar Chart
   * Simple CSS-based horizontal bar chart
   */

  interface DataItem {
    projectId?: string;
    projectName?: string;
    activeUsers?: number;
    label?: string;
    value?: number;
  }

  interface Props {
    data?: DataItem[];
    title?: string;
  }
  let { data = [], title = '' }: Props = $props();

  // Calculate max value for scaling
  const maxValue = $derived(() => {
    if (!data || data.length === 0) return 10;
    return Math.max(...data.map(d => d.activeUsers ?? d.value ?? 0)) || 10;
  });
</script>

<div class="bar-chart">
  <h3>{title}</h3>
  {#if data.length > 0}
    <div class="chart-rows">
      {#each data as item, i (item.projectId ?? i)}
        {@const value = item.activeUsers ?? item.value ?? 0}
        {@const width = Math.max(4, (value / maxValue()) * 100)}
        <div class="chart-row">
          <span class="label">{item.projectName ?? item.label ?? 'Unknown'}</span>
          <div class="bar-container">
            <div class="bar" style="width: {width}%"></div>
            <span class="value">{value}</span>
          </div>
        </div>
      {/each}
    </div>
  {:else}
    <div class="empty-chart">
      <span class="codicon codicon-graph"></span>
      <span>No distribution data</span>
    </div>
  {/if}
</div>

<style>
  .bar-chart {
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

  .chart-rows {
    display: flex;
    flex-direction: column;
    gap: 0.75rem;
  }

  .chart-row {
    display: flex;
    align-items: center;
    gap: 1rem;
  }

  .label {
    min-width: 100px;
    font-size: 0.875rem;
    color: var(--color-text-secondary);
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
  }

  .bar-container {
    flex: 1;
    display: flex;
    align-items: center;
    gap: 0.5rem;
    height: 24px;
    background: var(--color-bg-tertiary);
    border-radius: 4px;
    overflow: hidden;
    padding-right: 0.5rem;
  }

  .bar {
    height: 100%;
    background: linear-gradient(to right, var(--color-accent-primary), var(--color-accent-primary));
    border-radius: 4px 0 0 4px;
    transition: width 0.3s ease;
    min-width: 4px;
  }

  .value {
    font-size: 0.75rem;
    font-weight: 600;
    color: var(--color-text-primary);
    min-width: 2rem;
    text-align: right;
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
