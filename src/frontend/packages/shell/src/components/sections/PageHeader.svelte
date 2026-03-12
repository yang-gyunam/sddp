<!--
  PageHeader Component
  Standardized page-level header for content area
-->
<script lang="ts">
  import { LinearProgress } from '@sddp/ui';

  interface Props {
    title: string;
    subtitle?: string;
    meta?: string;
    loading?: boolean;
    /** Delay in ms before showing progress bar (prevents flicker on fast loads) */
    loadingDelay?: number;
    leading?: import('svelte').Snippet;
    titleAddon?: import('svelte').Snippet;
    actions?: import('svelte').Snippet;
    class?: string;
  }

  let {
    title,
    subtitle,
    meta,
    loading = false,
    loadingDelay = 300,
    leading,
    titleAddon,
    actions,
    class: className = '',
  }: Props = $props();

  // Deferred visibility: show progress bar only after loadingDelay ms,
  // and keep it visible for at least 500ms to avoid flash.
  const MIN_VISIBLE_MS = 500;
  let showProgress = $state(false);
  let delayTimer: ReturnType<typeof setTimeout> | null = null;
  let minTimer: ReturnType<typeof setTimeout> | null = null;
  let visibleSince = 0;

  $effect(() => {
    if (loading) {
      // Start delay timer — show bar only if loading persists past threshold
      delayTimer = setTimeout(() => {
        showProgress = true;
        visibleSince = Date.now();
      }, loadingDelay);
    } else {
      // Loading ended — clear delay if bar hasn't appeared yet
      if (delayTimer) {
        clearTimeout(delayTimer);
        delayTimer = null;
      }
      if (showProgress) {
        // Keep bar visible for minimum duration to avoid flash
        const elapsed = Date.now() - visibleSince;
        const remaining = MIN_VISIBLE_MS - elapsed;
        if (remaining > 0) {
          minTimer = setTimeout(() => {
            showProgress = false;
          }, remaining);
        } else {
          showProgress = false;
        }
      }
    }

    return () => {
      if (delayTimer) clearTimeout(delayTimer);
      if (minTimer) clearTimeout(minTimer);
    };
  });
</script>

<div
  class="page-header relative flex items-center justify-between min-h-12 px-4 bg-[var(--color-bg-primary)] border-b border-[var(--color-border-primary)] {className}"
>
  <div class="min-w-0">
    <div class="flex items-center gap-2 min-w-0">
      {#if leading}
        {@render leading()}
      {/if}
      <span class="text-sm font-medium text-[var(--color-text-primary)] truncate">{title}</span>
      {#if titleAddon}
        {@render titleAddon()}
      {/if}
      {#if meta}
        <span class="text-xs text-[var(--color-text-tertiary)] truncate">{meta}</span>
      {/if}
    </div>
    {#if subtitle}
      <div class="text-xs text-[var(--color-text-secondary)] truncate">{subtitle}</div>
    {/if}
  </div>

  {#if actions}
    <div class="flex items-center gap-1 flex-shrink-0">
      {@render actions()}
    </div>
  {/if}
</div>
{#if showProgress}
  <LinearProgress />
{/if}
