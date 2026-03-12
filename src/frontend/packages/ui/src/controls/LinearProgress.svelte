<script lang="ts">
  /**
   * LinearProgress
   * Horizontal progress indicator — indeterminate (sliding) or determinate (fixed width).
   */

  type ProgressSize = 'xs' | 'sm' | 'md';

  interface Props {
    /** 0–100 for determinate; omit or undefined for indeterminate */
    value?: number;
    size?: ProgressSize;
    class?: string;
  }

  let { value, size = 'xs', class: className = '' }: Props = $props();

  const heightMap: Record<ProgressSize, string> = { xs: '2px', sm: '3px', md: '4px' };
  const h = $derived(heightMap[size]);
  const isDeterminate = $derived(value !== undefined);
  const clampedValue = $derived(Math.max(0, Math.min(value ?? 0, 100)));
</script>

<div
  class="linear-progress {className}"
  style="height:{h};"
  role="progressbar"
  aria-valuenow={isDeterminate ? clampedValue : undefined}
  aria-valuemin={isDeterminate ? 0 : undefined}
  aria-valuemax={isDeterminate ? 100 : undefined}
>
  {#if isDeterminate}
    <div class="bar determinate" style="width:{clampedValue}%;height:{h};"></div>
  {:else}
    <div class="bar indeterminate" style="height:{h};"></div>
  {/if}
</div>

<style>
  .linear-progress {
    position: relative;
    width: 100%;
    overflow: hidden;
    border-radius: 1px;
  }

  .bar {
    background: var(--color-accent-primary, #6366f1);
    border-radius: 1px;
  }

  .determinate {
    transition: width 0.3s ease;
  }

  .indeterminate {
    width: 40%;
    animation: lp-slide 1.2s ease-in-out infinite;
  }

  @keyframes lp-slide {
    0% { transform: translateX(-100%); }
    100% { transform: translateX(350%); }
  }
</style>
