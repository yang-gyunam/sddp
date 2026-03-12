<!--
  ContentDetailLayout Component
  Reusable 2-column layout with resizable main content and flexible detail panel

  Layout:
  ┌────────────────────────────┬───┬─────────────────────┐
  │ Main Content               │ R │ Detail Panel        │
  │ (min/max constrained)      │ H │ (flexible)          │
  └────────────────────────────┴───┴─────────────────────┘
-->
<script lang="ts">
  import type { Snippet } from 'svelte';
  import { ResizeHandle } from '@sddp/ui';

  interface Props {
    /** Main content (default slot) */
    children: Snippet;
    /** Right detail panel content */
    detailPanel?: Snippet;

    /** Whether to show the detail panel */
    showDetailPanel?: boolean;

    /** Main content width configuration */
    mainContentWidth?: number;
    minMainContentWidth?: number;
    maxMainContentWidth?: number;

    /** Detail panel minimum width (to prevent it from disappearing) */
    minDetailPanelWidth?: number;

    /** Callback for width changes (for persistence) */
    onMainContentWidthChange?: (width: number) => void;

    /** Additional CSS classes */
    class?: string;
  }

  let {
    children,
    detailPanel,
    showDetailPanel = true,
    mainContentWidth: initialMainContentWidth = 720,
    minMainContentWidth = 720,
    maxMainContentWidth = 1200,
    minDetailPanelWidth = 280,
    onMainContentWidthChange,
    class: className = '',
  }: Props = $props();

  // svelte-ignore state_referenced_locally
  let mainContentWidth = $state(initialMainContentWidth);
  let isResizing = $state(false);
  let rafId: number | null = null;

  function handleResizeStart(e: PointerEvent) {
    e.preventDefault();
    isResizing = true;
    const startX = e.clientX;
    const startWidth = mainContentWidth;

    function onPointerMove(moveEvent: PointerEvent) {
      // Cancel previous frame if pending
      if (rafId !== null) {
        cancelAnimationFrame(rafId);
      }

      // Schedule update on next animation frame
      rafId = requestAnimationFrame(() => {
        const delta = moveEvent.clientX - startX;
        const newWidth = Math.min(maxMainContentWidth, Math.max(minMainContentWidth, startWidth + delta));
        mainContentWidth = newWidth;
        rafId = null;
      });
    }

    function onPointerUp() {
      // Clean up any pending animation frame
      if (rafId !== null) {
        cancelAnimationFrame(rafId);
        rafId = null;
      }

      isResizing = false;
      onMainContentWidthChange?.(mainContentWidth);
      document.removeEventListener('pointermove', onPointerMove);
      document.removeEventListener('pointerup', onPointerUp);
    }

    document.addEventListener('pointermove', onPointerMove);
    document.addEventListener('pointerup', onPointerUp);
  }
</script>

<div class="flex flex-1 min-h-0 h-full {className}">
  <!-- Main Content (constrained width) -->
  <div
    class="flex-shrink-0 overflow-auto"
    class:resizing={isResizing}
    style="width: {mainContentWidth}px; min-width: {minMainContentWidth}px; max-width: {maxMainContentWidth}px;"
  >
    {@render children()}
  </div>

  <!-- Detail Panel (conditional) -->
  {#if showDetailPanel && detailPanel}
    <!-- Resize Handle -->
    <ResizeHandle
      orientation="vertical"
      {isResizing}
      onpointerdown={handleResizeStart}
      ariaLabel="Resize panels"
    />

    <!-- Detail Panel (flexible, fills remaining space) -->
    <div
      class="flex-1 min-w-0 border-l border-[var(--color-border-primary)] overflow-hidden bg-[var(--color-bg-secondary)]"
      class:resizing={isResizing}
      style="min-width: {minDetailPanelWidth}px;"
    >
      {@render detailPanel()}
    </div>
  {/if}
</div>

<style>
  .resizing {
    will-change: width;
    pointer-events: none;
    user-select: none;
  }
</style>
