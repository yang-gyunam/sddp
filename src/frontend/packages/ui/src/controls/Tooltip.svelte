<script lang="ts">
  import type { TooltipPlacement } from '../types';

  interface Props {
    content: string;
    placement?: TooltipPlacement;
    delay?: number;
    disabled?: boolean;
    children?: import('svelte').Snippet;
    class?: string;
  }

  let {
    content,
    placement = 'top',
    delay = 200,
    disabled = false,
    children,
    class: className = '',
  }: Props = $props();

  let visible = $state(false);
  let timeoutId: ReturnType<typeof setTimeout> | null = null;

  const placementClasses: Record<TooltipPlacement, { position: string; arrow: string }> = {
    top: {
      position: 'bottom-full left-1/2 -translate-x-1/2 mb-2',
      arrow: 'top-full left-1/2 -translate-x-1/2 border-t-[var(--color-neutral-800)] border-x-transparent border-b-transparent',
    },
    bottom: {
      position: 'top-full left-1/2 -translate-x-1/2 mt-2',
      arrow: 'bottom-full left-1/2 -translate-x-1/2 border-b-[var(--color-neutral-800)] border-x-transparent border-t-transparent',
    },
    left: {
      position: 'right-full top-1/2 -translate-y-1/2 mr-2',
      arrow: 'left-full top-1/2 -translate-y-1/2 border-l-[var(--color-neutral-800)] border-y-transparent border-r-transparent',
    },
    right: {
      position: 'left-full top-1/2 -translate-y-1/2 ml-2',
      arrow: 'right-full top-1/2 -translate-y-1/2 border-r-[var(--color-neutral-800)] border-y-transparent border-l-transparent',
    },
  };

  function showTooltip() {
    if (disabled || !content) return;
    timeoutId = setTimeout(() => {
      visible = true;
    }, delay);
  }

  function hideTooltip() {
    if (timeoutId) {
      clearTimeout(timeoutId);
      timeoutId = null;
    }
    visible = false;
  }
</script>

<div
  role="presentation"
  class="relative inline-flex {className}"
  onmouseenter={showTooltip}
  onmouseleave={hideTooltip}
  onfocus={showTooltip}
  onblur={hideTooltip}
>
  {#if children}
    {@render children()}
  {/if}

  {#if visible && content}
    <div
      role="tooltip"
      class="absolute z-50 {placementClasses[placement].position} pointer-events-none"
    >
      <div
        class="px-2 py-1 text-xs font-medium text-white bg-[var(--color-neutral-800)] rounded shadow-lg whitespace-nowrap"
      >
        {content}
      </div>
      <div
        class="absolute w-0 h-0 border-4 {placementClasses[placement].arrow}"
      ></div>
    </div>
  {/if}
</div>
