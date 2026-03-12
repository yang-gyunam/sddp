<!--
  PageShell Component
  Provides consistent page-level layout structure
-->
<script lang="ts">
  interface Props {
    class?: string;
    children?: import('svelte').Snippet;
  }

  let { class: className = '', children }: Props = $props();
</script>

<div class="page-shell flex flex-col h-full min-h-0 bg-[var(--color-bg-page)] {className}">
  {#if children}
    {@render children()}
  {/if}
</div>
