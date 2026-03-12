<script lang="ts">
  interface Props {
    cols?: 2 | 3 | 4 | 6;
    gap?: 'sm' | 'md' | 'lg';
    class?: string;
    children?: import('svelte').Snippet;
  }

  let {
    cols = 3,
    gap = 'md',
    class: className = '',
    children,
  }: Props = $props();

  const gapMap = {
    sm: 'var(--space-2)',
    md: 'var(--space-3)',
    lg: 'var(--space-4)',
  } as const;

  const colSpanMap = {
    2: 6,
    3: 4,
    4: 3,
    6: 2,
  } as const;

  const gapValue = $derived(gapMap[gap] ?? gapMap.md);
  const colSpanValue = $derived(colSpanMap[cols] ?? colSpanMap[3]);
</script>

<div class="card-grid {className}" style:gap={gapValue} style:--col-span={colSpanValue}>
  {#if children}
    {@render children()}
  {/if}
</div>

<style>
  .card-grid {
    display: grid;
    grid-template-columns: repeat(12, minmax(0, 1fr));

    /* Container Query - */
    container-type: inline-size;
  }

  /* default: 1 () */
  .card-grid > :global(*) {
    grid-column: span 12;
  }

  /* >= 480px: 2 */
  @container (min-width: 480px) {
    .card-grid > :global(*) {
      grid-column: span 6;
    }
  }

  /* >= 768px: cols prop */
  @container (min-width: 768px) {
    .card-grid > :global(*) {
      grid-column: span var(--col-span, 4);
    }
  }
</style>
