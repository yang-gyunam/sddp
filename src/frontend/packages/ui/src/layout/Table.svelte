<script lang="ts">
  import type { Snippet } from 'svelte';

  let {
    label,
    header,
    children,
    class: className = '',
  }: {
    label: string;
    header: Snippet;
    children: Snippet;
    class?: string;
  } = $props();
</script>

<table aria-label={label} class={className}>
  <thead>{@render header()}</thead>
  <tbody>{@render children()}</tbody>
</table>

<style>
  table {
    width: 100%;
    border-collapse: separate;
    border-spacing: 0;
  }
  table :global(th),
  table :global(td) {
    padding: 6px 16px;
    border-bottom: 1px solid var(--color-border-primary);
  }
  thead :global(th) {
    position: sticky;
    top: 0;
    z-index: 1;
    padding-block: 9px;
    background: var(--color-bg-secondary);
    color: var(--color-text-secondary);
    font-weight: 600;
  }
</style>
