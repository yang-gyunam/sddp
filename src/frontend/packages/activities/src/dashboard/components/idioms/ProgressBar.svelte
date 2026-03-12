<script lang="ts">
  /**
   * Progress Bar
   * Horizontal progress bar with percentage
   */
  import { formatNumber, formatPercent } from '@sddp/shell';

  interface Props {
    value: number;
    max?: number;
    label?: string;
    showPercentage?: boolean;
  }
  let { value, max = 100, label = '', showPercentage = true }: Props = $props();

  let percentage = $derived(Math.round((value / max) * 100));
  let displayLabel = $derived(label || `${formatNumber(value)} / ${formatNumber(max)}`);
  let percentageLabel = $derived(formatPercent(percentage / 100, { maximumFractionDigits: 0 }));
</script>

<div class="progress-bar-container">
  {#if label || showPercentage}
    <div class="progress-header">
      <span class="progress-label">{displayLabel}</span>
      {#if showPercentage}
        <span class="progress-percentage">{percentageLabel}</span>
      {/if}
    </div>
  {/if}
  <div class="progress-bar">
    <div class="progress-fill" style="width: {percentage}%"></div>
  </div>
</div>

<style>
  .progress-bar-container {
    width: 100%;
  }

  .progress-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 0.5rem;
    font-size: 0.875rem;
  }

  .progress-label {
    color: var(--text-primary);
  }

  .progress-percentage {
    color: var(--text-secondary);
    font-weight: 600;
  }

  .progress-bar {
    width: 100%;
    height: 0.5rem;
    background: var(--bg-secondary);
    border-radius: 4px;
    overflow: hidden;
  }

  .progress-fill {
    height: 100%;
    background: var(--accent-color);
    transition: width 0.3s ease;
  }
</style>
