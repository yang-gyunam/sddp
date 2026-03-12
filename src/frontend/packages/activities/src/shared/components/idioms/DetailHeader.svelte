<!--
  DetailHeader Idiom
  Shared header layout for detail/overview panels
-->
<script lang="ts">
  import type { Snippet } from 'svelte';

  interface Props {
    leading?: Snippet;
    actions?: Snippet;
    children: Snippet;
    class?: string;
    style?: string;
  }

  let {
    leading,
    actions,
    children,
    class: className = '',
    style: styleAttr = '',
  }: Props = $props();
</script>

<div class="detail-header {className}" style={styleAttr}>
  <div class="detail-header__left">
    {#if leading}
      <div class="detail-header__leading">
        {@render leading()}
      </div>
    {/if}
    <div class="detail-header__content">
      {@render children()}
    </div>
  </div>
  {#if actions}
    <div class="detail-header__actions">
      {@render actions()}
    </div>
  {/if}
</div>

<style>
  .detail-header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    min-height: var(--detail-header-min-height, 3rem);
    padding: var(--detail-header-padding, 0.5rem 1rem);
    border-bottom: 1px solid var(--detail-header-border, var(--color-border-primary));
    background: var(--detail-header-bg, var(--color-bg-section));
  }

  .detail-header__left {
    display: flex;
    align-items: center;
    gap: 0.75rem;
    min-width: 0;
  }

  .detail-header__leading {
    display: flex;
    align-items: center;
    flex-shrink: 0;
  }

  .detail-header__content {
    display: flex;
    align-items: baseline;
    gap: 0.5rem;
    min-width: 0;
  }

  .detail-header__actions {
    display: flex;
    align-items: center;
    gap: 0.25rem;
    flex-shrink: 0;
  }
</style>
