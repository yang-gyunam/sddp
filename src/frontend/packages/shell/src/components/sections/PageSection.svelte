<!--
  PageSection Component
  Standardized section block inside a page
-->
<script lang="ts">
  interface Props {
    title?: string;
    description?: string;
    class?: string;
    children?: import('svelte').Snippet;
  }

  let {
    title,
    description,
    class: className = '',
    children,
  }: Props = $props();
</script>

<section class="page-section space-y-2 {className}">
  {#if title}
    <div class="space-y-0.5">
      <div class="text-[0.6875rem] font-semibold uppercase tracking-[0.08em] text-[var(--color-text-tertiary)]">
        {title}
      </div>
      {#if description}
        <div class="text-xs text-[var(--color-text-secondary)]">
          {description}
        </div>
      {/if}
    </div>
  {/if}

  {#if children}
    {@render children()}
  {/if}
</section>
