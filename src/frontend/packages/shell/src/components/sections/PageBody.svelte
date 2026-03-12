<!--
  PageBody Component
  Standardized page body container
-->
<script lang="ts">
  interface Props {
    class?: string;
    padded?: boolean;
    noPadding?: boolean;
    scrollable?: boolean;
    children?: import('svelte').Snippet;
  }

  let {
    class: className = '',
    padded = true,
    noPadding = false,
    scrollable = true,
    children,
  }: Props = $props();

  // noPadding overrides padded
  const effectivePadded = $derived(noPadding ? false : padded);
</script>

<div
  class="page-body flex-1 min-h-0 {scrollable ? 'overflow-auto' : ''} {effectivePadded ? 'px-0 pb-2' : ''} {className}"
>
  {#if children}
    {@render children()}
  {/if}
</div>
