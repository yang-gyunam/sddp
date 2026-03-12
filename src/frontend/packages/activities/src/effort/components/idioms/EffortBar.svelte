<!--
  EffortBar Component
  Shows allocated vs spent hours as overlapped horizontal bar
  - Allocated = track (light background)
  - Spent = fill (colored based on utilization)
-->
<script lang="ts">
  interface Props {
    /** Allocated hours (determines bar capacity) */
    allocated: number;
    /** Spent hours (determines fill amount) */
    spent: number;
    /** Maximum hours for scale (default 8 for daily) */
    maxHours?: number;
    /** Size preset */
    size?: 'sm' | 'md';
    /** Additional CSS classes */
    class?: string;
  }

  let {
    allocated,
    spent,
    maxHours = 8,
    size = 'sm',
    class: className = '',
  }: Props = $props();

  // Calculate percentages
  const allocatedPercent = $derived(Math.min((allocated / maxHours) * 100, 100));
  const spentPercent = $derived(Math.min((spent / maxHours) * 100, 100));

  // Utilization rate (spent / allocated)
  const utilization = $derived(allocated > 0 ? (spent / allocated) * 100 : 0);

  // Fill color based on utilization
  const fillColor = $derived.by(() => {
    if (allocated === 0) return 'var(--color-text-tertiary)';
    if (utilization >= 100) return 'var(--color-success-500)';
    if (utilization >= 80) return 'var(--color-accent-primary)';
    if (utilization >= 50) return 'var(--color-warning-500)';
    return 'var(--color-danger-500)';
  });

  // Size config
  const height = $derived(size === 'sm' ? '6px' : '8px');

  // Tooltip
  const tooltip = $derived(`Spent: ${spent}h, Allocated: ${allocated}h`);
</script>

<div
  class="effort-bar {className}"
  style="--bar-height: {height}"
  title={tooltip}
>
  <!-- Track (full width = maxHours) -->
  <div class="effort-bar__track">
    <!-- Allocated portion (light background) -->
    <div
      class="effort-bar__allocated"
      style="width: {allocatedPercent}%"
    ></div>
    <!-- Spent portion (colored fill) -->
    <div
      class="effort-bar__spent"
      style="width: {spentPercent}%; background-color: {fillColor}"
    ></div>
  </div>
</div>

<style>
  .effort-bar {
    display: flex;
    align-items: center;
    width: 100%;
    min-width: 40px;
  }

  .effort-bar__track {
    position: relative;
    width: 100%;
    height: var(--bar-height);
    background-color: var(--color-bg-tertiary);
    border-radius: 2px;
    overflow: hidden;
  }

  .effort-bar__allocated {
    position: absolute;
    top: 0;
    left: 0;
    height: 100%;
    background-color: var(--color-accent-primary);
    border-radius: 2px;
  }

  :global(.dark) .effort-bar__allocated {
    background-color: var(--color-accent-primary);
    opacity: 0.4;
  }

  .effort-bar__spent {
    position: absolute;
    top: 0;
    left: 0;
    height: 100%;
    border-radius: 2px;
    transition: width 150ms ease-in-out;
  }
</style>
