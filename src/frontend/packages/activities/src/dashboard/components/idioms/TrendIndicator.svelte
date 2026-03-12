<script lang="ts">
  /**
   * Trend Indicator
   * Shows trend direction and value
   */

  interface Props {
    value: number;
    label?: string;
  }
  let { value, label = '' }: Props = $props();

  let trend = $derived(value > 0 ? 'up' : value < 0 ? 'down' : 'neutral');
  let displayValue = $derived(value > 0 ? `+${value}` : value.toString());
</script>

<div class="trend-indicator" class:up={trend === 'up'} class:down={trend === 'down'}>
  <span class="trend-icon">{trend === 'up' ? '↑' : trend === 'down' ? '↓' : '→'}</span>
  <span class="trend-value">{displayValue}</span>
  {#if label}
    <span class="trend-label">{label}</span>
  {/if}
</div>

<style>
  .trend-indicator {
    display: inline-flex;
    align-items: center;
    gap: 0.25rem;
    font-size: 0.75rem;
    color: var(--text-secondary);
  }

  .trend-indicator.up {
    color: var(--color-success-500);
  }

  .trend-indicator.down {
    color: var(--color-error-500);
  }

  .trend-icon {
    font-size: 0.875rem;
  }

  .trend-value {
    font-weight: 600;
  }

  .trend-label {
    opacity: 0.8;
  }
</style>
