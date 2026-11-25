<!--
  SidebarDetailLayout Component
  Reusable layout with resizable sidebar, main content, and optional right panel.
  Supports an optional header snippet that spans content + right panel (Pattern B).

  Without header (Pattern A):
  ┌──────────┬───┬────────────────────┬───┬─────────────┐
  │ Sidebar  │ R │ Main Content       │ R │ Right Panel │
  │ (resize) │ H │                    │ H │ (optional)  │
  └──────────┴───┴────────────────────┴───┴─────────────┘

  With header (Pattern B):
  ┌──────────┬───┬──────────────────────────────────────┐
  │ Sidebar  │ R │ Header (full width)                  │
  │ (resize) │ H ├────────────────────┬───┬─────────────┤
  │          │   │ Main Content       │ R │ Right Panel │
  │          │   │                    │ H │ (optional)  │
  └──────────┴───┴────────────────────┴───┴─────────────┘
-->
<script lang="ts">
  import { onDestroy, type Snippet } from 'svelte';
  import { ResizeHandle } from '@sddp/ui';
  import { SIDEBAR_DETAIL_LAYOUT } from '../../types';

  interface Props {
    /** Sidebar content */
    sidebar?: Snippet;
    /** Main content (default slot) */
    children: Snippet;
    /** Right panel content (optional) */
    rightPanel?: Snippet;
    /** Header spanning content + right panel (Pattern B) */
    header?: Snippet;

    /** Whether to show the right panel */
    showRightPanel?: boolean;
    /** Optional fourth pane, below the shared detail header. */
    auxiliaryPanel?: Snippet;
    showAuxiliaryPanel?: boolean;

    /** Sidebar configuration */
    sidebarWidth?: number;
    minSidebarWidth?: number;
    maxSidebarWidth?: number;

    /** Right panel configuration */
    rightPanelWidth?: number;
    minRightPanelWidth?: number;
    maxRightPanelWidth?: number;

    /** Callbacks for width changes (for persistence) */
    onSidebarWidthChange?: (width: number) => void;
    onRightPanelWidthChange?: (width: number) => void;

    /** Additional CSS classes */
    class?: string;
  }

  let {
    sidebar,
    children,
    rightPanel,
    header,
    showRightPanel = false,
    auxiliaryPanel,
    showAuxiliaryPanel = false,
    sidebarWidth: initialSidebarWidth = SIDEBAR_DETAIL_LAYOUT.sidebarWidth,
    minSidebarWidth = SIDEBAR_DETAIL_LAYOUT.minSidebarWidth,
    maxSidebarWidth = SIDEBAR_DETAIL_LAYOUT.maxSidebarWidth,
    rightPanelWidth: initialRightPanelWidth = SIDEBAR_DETAIL_LAYOUT.rightPanelWidth,
    minRightPanelWidth = SIDEBAR_DETAIL_LAYOUT.minRightPanelWidth,
    maxRightPanelWidth = SIDEBAR_DETAIL_LAYOUT.maxRightPanelWidth,
    onSidebarWidthChange,
    onRightPanelWidthChange,
    class: className = '',
  }: Props = $props();

  type Pane = 'sidebar' | 'right' | 'auxiliary';
  const auxiliaryWidthStorageKey = 'sddp-ai-chat-width';
  const detailMin = 220;
  let layout: HTMLDivElement | undefined = $state();
  let layoutWidth = $state(0);
  $effect(() => {
    if (!layout || !auxiliaryPanel) return;
    layoutWidth = layout.clientWidth;
    if (typeof ResizeObserver === 'undefined') return;
    const observer = new ResizeObserver(([entry]) => {
      if (entry) layoutWidth = entry.target.clientWidth;
    });
    observer.observe(layout);
    return () => observer.disconnect();
  });
  // svelte-ignore state_referenced_locally
  let preferred = $state({
    sidebar: initialSidebarWidth,
    right: initialRightPanelWidth,
    auxiliary: undefined as number | undefined,
  });
  let active = $state<Pane | null>(null);
  let stopDrag: (() => void) | undefined;
  const hasRight = $derived(!!rightPanel && showRightPanel);
  const auxiliaryOpen = $derived(!!auxiliaryPanel && showAuxiliaryPanel);
  $effect(() => {
    if (!auxiliaryOpen) return;
    try {
      const saved = Number(window.localStorage.getItem(auxiliaryWidthStorageKey));
      preferred.auxiliary = Number.isFinite(saved) && saved > 0 ? saved : undefined;
    } catch {
      // Keep the current preference when browser storage is unavailable.
    }
  });
  const inline = $derived(
    layoutWidth >=
      detailMin +
        minRightPanelWidth +
        (sidebar ? minSidebarWidth : 0) +
        (hasRight ? minRightPanelWidth : 0)
  );
  const budget = $derived(layoutWidth - detailMin);
  const bounds = $derived({
    sidebar: { min: minSidebarWidth, max: maxSidebarWidth },
    right: { min: minRightPanelWidth, max: maxRightPanelWidth },
    auxiliary: { min: minRightPanelWidth, max: maxRightPanelWidth },
  });
  const auxiliaryPreference = $derived(
    Math.max(
      minRightPanelWidth,
      Math.min(maxRightPanelWidth, preferred.auxiliary ?? maxRightPanelWidth)
    )
  );
  const widths = $derived.by(() => {
    const sidebarWidth = !sidebar
      ? 0
      : auxiliaryOpen && inline
        ? Math.min(
            preferred.sidebar,
            budget - minRightPanelWidth - (hasRight ? minRightPanelWidth : 0)
          )
        : preferred.sidebar;
    const right = !hasRight
      ? 0
      : auxiliaryOpen && inline
        ? Math.min(preferred.right, budget - minRightPanelWidth - sidebarWidth)
        : preferred.right;
    const auxiliary =
      auxiliaryOpen && inline ? Math.min(auxiliaryPreference, budget - sidebarWidth - right) : 0;
    return { sidebar: sidebarWidth, right, auxiliary };
  });

  function maxWidth(pane: Pane): number {
    const max = bounds[pane].max;
    if (!auxiliaryOpen || !inline) return max;
    return Math.min(max, budget - widths.sidebar - widths.right - widths.auxiliary + widths[pane]);
  }

  function resize(pane: Pane, value: number) {
    const width = Math.max(bounds[pane].min, Math.min(maxWidth(pane), value));
    if (pane === 'auxiliary' && width === widths.auxiliary) return false;
    preferred[pane] = width;
    return true;
  }

  function commit(pane: Pane) {
    if (pane === 'sidebar') onSidebarWidthChange?.(preferred.sidebar);
    if (pane === 'right') onRightPanelWidthChange?.(preferred.right);
    if (pane === 'auxiliary' && preferred.auxiliary !== undefined) {
      try {
        window.localStorage.setItem(auxiliaryWidthStorageKey, String(preferred.auxiliary));
      } catch {
        // The width still applies in this layout if it cannot be saved.
      }
    }
  }

  function startResize(pane: Pane, event: PointerEvent) {
    if (event.button !== 0 || active) return;
    event.preventDefault();
    (event.currentTarget as HTMLElement | null)?.focus();
    const startX = event.clientX;
    const startWidth = widths[pane];
    const startPreference = preferred.auxiliary;
    const pointerId = event.pointerId;
    active = pane;
    let frame = 0;

    function move(next: PointerEvent) {
      if (next.pointerId !== pointerId) return;
      cancelAnimationFrame(frame);
      const delta = (next.clientX - startX) * (pane === 'sidebar' ? 1 : -1);
      frame = requestAnimationFrame(() => resize(pane, startWidth + delta));
    }

    function finish(next: PointerEvent) {
      if (next.pointerId !== pointerId) return;
      stopDrag?.();
      if (next.type === 'pointerup') {
        const delta = (next.clientX - startX) * (pane === 'sidebar' ? 1 : -1);
        if (delta || pane === 'auxiliary') resize(pane, startWidth + delta);
      }
      active = null;
      if (
        pane === 'auxiliary' &&
        (next.type === 'pointercancel' || widths.auxiliary === startWidth)
      ) {
        preferred.auxiliary = startPreference;
      } else {
        commit(pane);
      }
    }

    stopDrag = () => {
      cancelAnimationFrame(frame);
      document.removeEventListener('pointermove', move);
      document.removeEventListener('pointerup', finish);
      document.removeEventListener('pointercancel', finish);
      stopDrag = undefined;
    };
    document.addEventListener('pointermove', move);
    document.addEventListener('pointerup', finish);
    document.addEventListener('pointercancel', finish);
  }

  function resizeByKey(pane: Pane, event: KeyboardEvent) {
    let next: number;
    const step = pane === 'sidebar' ? 10 : -10;
    switch (event.key) {
      case 'ArrowRight':
        next = widths[pane] + step;
        break;
      case 'ArrowLeft':
        next = widths[pane] - step;
        break;
      case 'Home':
        next = bounds[pane].min;
        break;
      case 'End':
        next = maxWidth(pane);
        break;
      default:
        return;
    }
    event.preventDefault();
    if (resize(pane, next)) commit(pane);
  }

  onDestroy(() => stopDrag?.());
</script>

<div
  class="flex flex-1 min-h-0 h-full {className}"
  bind:this={layout}
  class:with-auxiliary={!!auxiliaryPanel}
  class:dragging={active !== null}
>
  {#if sidebar}
    <div
      class="pane flex-shrink-0 border-r border-[var(--color-border-primary)]"
      class:resizing={active !== null}
      style:width="{widths.sidebar}px"
    >
      {@render sidebar()}
    </div>
    <div class="relative z-10 flex-shrink-0">
      <ResizeHandle
        orientation="vertical"
        isResizing={active === 'sidebar'}
        onpointerdown={(event) => startResize('sidebar', event)}
        onkeydown={(event) => resizeByKey('sidebar', event)}
        ariaLabel="Resize sidebar"
        ariaValueNow={widths.sidebar}
        ariaValueMin={bounds.sidebar.min}
        ariaValueMax={maxWidth('sidebar')}
      />
    </div>
  {/if}

  <!-- Center Area (header + content row) -->
  <div class="flex-1 min-w-0 flex flex-col overflow-hidden">
    {#if header}
      <!-- Header (full width, Pattern B) -->
      <div class="flex-shrink-0">
        {@render header()}
      </div>
    {/if}

    <div
      class="content-row flex flex-1 min-h-0"
      class:resizing={active !== null}
      class:auxiliary-stacked={!!auxiliaryPanel && !inline}
    >
      <div class="main-pane flex-1 min-w-0 flex flex-col overflow-hidden">
        {@render children()}
      </div>

      {#if hasRight && rightPanel}
        <div class="relative z-10 flex-shrink-0">
          <ResizeHandle
            orientation="vertical"
            isResizing={active === 'right'}
            onpointerdown={(event) => startResize('right', event)}
            onkeydown={(event) => resizeByKey('right', event)}
            ariaLabel="Resize right panel"
            ariaValueNow={widths.right}
            ariaValueMin={bounds.right.min}
            ariaValueMax={maxWidth('right')}
          />
        </div>
        <div
          class="pane flex-shrink-0 border-l border-[var(--color-border-primary)] overflow-hidden"
          class:resizing={active !== null}
          style:width="{widths.right}px"
        >
          {@render rightPanel()}
        </div>
      {/if}

      {#if auxiliaryPanel}
        {#if auxiliaryOpen && inline}
          <div class="relative z-10 flex-shrink-0">
            <ResizeHandle
              orientation="vertical"
              isResizing={active === 'auxiliary'}
              onpointerdown={(event) => startResize('auxiliary', event)}
              onkeydown={(event) => resizeByKey('auxiliary', event)}
              ariaLabel="Resize AI Chat"
              ariaValueNow={widths.auxiliary}
              ariaValueMin={bounds.auxiliary.min}
              ariaValueMax={maxWidth('auxiliary')}
            />
          </div>
        {/if}
        <div
          class="auxiliary pane flex-shrink-0 overflow-hidden"
          class:auxiliary-open={auxiliaryOpen}
          style:width={inline ? `${auxiliaryOpen ? widths.auxiliary : 0}px` : '100%'}
          inert={!auxiliaryOpen}
        >
          {#if auxiliaryOpen}
            {@render auxiliaryPanel()}
          {/if}
        </div>
      {/if}
    </div>
  </div>
</div>

<style>
  .resizing {
    will-change: width;
    pointer-events: none;
    user-select: none;
  }

  @media (prefers-reduced-motion: no-preference) {
    .with-auxiliary:not(.dragging) .pane {
      transition:
        width 220ms ease,
        height 220ms ease;
    }
  }

  .auxiliary-stacked {
    flex-wrap: wrap;
  }

  .auxiliary-stacked:has(.auxiliary-open) .main-pane {
    min-height: min(180px, 40%);
  }

  .auxiliary-stacked .auxiliary {
    height: 0;
  }

  .auxiliary-stacked .auxiliary-open {
    height: 60%;
  }
</style>
