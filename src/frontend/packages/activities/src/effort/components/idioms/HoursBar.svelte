<!--
  HoursBar Component
  Visual representation of hours using vertical bars filled like water

  Design:
  - Bar count = allocated hours (shows capacity with border)
  - Fill = spent hours (solid color, no border needed)
  - Each bar = 2 hours by default
-->
<script lang="ts">
  import { calculateUtilization, getUtilizationGrade } from '../../stores';

  interface Props {
    /** Allocated hours - determines number of bars (with border) */
    allocated: number;
    /** Spent hours - determines fill amount (solid color) */
    spent: number;
    /** Hours per bar (default: 2h per bar) */
    hoursPerBar?: number;
    /** Show numeric value alongside */
    showValue?: boolean;
    /** Size preset */
    size?: 'sm' | 'md';
    /** Additional CSS classes */
    class?: string;
  }

  let {
    allocated,
    spent,
    hoursPerBar = 2,
    showValue = true,
    size = 'sm',
    class: className = '',
  }: Props = $props();

  // Number of bars based on allocated hours
  const barCount = $derived(Math.ceil(allocated / hoursPerBar));

  // Calculate fill levels for each bar
  const barFills = $derived.by(() => {
    const fills: number[] = [];
    let remaining = Math.max(0, spent);

    for (let i = 0; i < barCount; i++) {
      if (remaining >= hoursPerBar) {
        fills.push(1); // Full
        remaining -= hoursPerBar;
      } else if (remaining > 0) {
        fills.push(remaining / hoursPerBar); // Partial
        remaining = 0;
      } else {
        fills.push(0); // Empty
      }
    }
    return fills;
  });

  // Utilization rate and color from shared logic
  const utilization = $derived(calculateUtilization(spent, allocated));
  const fillColor = $derived(getUtilizationGrade(utilization).color);

  // Size classes
  const sizeConfig = $derived(
    size === 'sm'
      ? { width: 6, height: 16, gap: 2 }
      : { width: 8, height: 20, gap: 3 }
  );

  // Tooltip
  const tooltip = $derived(`${spent}h / ${allocated}h`);
</script>

<div class="hours-bar {className}" title={tooltip}>
  {#if barCount > 0}
    <div class="hours-bar__bars" style="gap: {sizeConfig.gap}px">
      {#each barFills as fill, index (index)}
        <div
          class="hours-bar__bar"
          style="width: {sizeConfig.width}px; height: {sizeConfig.height}px"
        >
          {#if fill > 0}
            <!-- Filled portion: solid color -->
            <div
              class="hours-bar__fill"
              style="height: {fill * 100}%; background-color: {fillColor}"
            ></div>
          {/if}
        </div>
      {/each}
    </div>
  {:else}
    <span class="hours-bar__empty">-</span>
  {/if}
  {#if showValue && allocated > 0}
    <span class="hours-bar__value">{spent}/{allocated}h</span>
  {/if}
</div>

<style>
  .hours-bar {
    display: inline-flex;
    align-items: flex-end;
    gap: 0.375rem;
  }

  .hours-bar__bars {
    display: flex;
    align-items: flex-end;
  }

  .hours-bar__bar {
    position: relative;
    border: 1px solid var(--color-border-secondary);
    border-radius: 1px;
    overflow: hidden;
    background-color: transparent;
  }

  .hours-bar__fill {
    position: absolute;
    bottom: 0;
    left: 0;
    right: 0;
    transition: height 150ms ease-in-out;
  }

  .hours-bar__value {
    font-size: var(--text-xs);
    font-weight: 500;
    font-variant-numeric: tabular-nums;
    color: var(--color-text-secondary);
  }

  .hours-bar__empty {
    font-size: var(--text-xs);
    color: var(--color-text-tertiary);
  }
</style>
