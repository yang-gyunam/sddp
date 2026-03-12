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
  import type { Snippet } from 'svelte';
  import { ResizeHandle } from '@sddp/ui';
  import { SIDEBAR_DETAIL_LAYOUT } from '../../types';

  interface Props {
    /** Sidebar content */
    sidebar: Snippet;
    /** Main content (default slot) */
    children: Snippet;
    /** Right panel content (optional) */
    rightPanel?: Snippet;
    /** Header spanning content + right panel (Pattern B) */
    header?: Snippet;

    /** Whether to show the right panel */
    showRightPanel?: boolean;

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

  // Sidebar resize state
  // svelte-ignore state_referenced_locally
  let sidebarWidth = $state(initialSidebarWidth);
  let isSidebarResizing = $state(false);
  let sidebarRafId: number | null = null;

  function handleSidebarResizeStart(e: PointerEvent) {
    e.preventDefault();
    isSidebarResizing = true;
    const startX = e.clientX;
    const startWidth = sidebarWidth;

    function onPointerMove(moveEvent: PointerEvent) {
      if (sidebarRafId !== null) {
        cancelAnimationFrame(sidebarRafId);
      }

      sidebarRafId = requestAnimationFrame(() => {
        const delta = moveEvent.clientX - startX;
        const newWidth = Math.min(maxSidebarWidth, Math.max(minSidebarWidth, startWidth + delta));
        sidebarWidth = newWidth;
        sidebarRafId = null;
      });
    }

    function onPointerUp() {
      if (sidebarRafId !== null) {
        cancelAnimationFrame(sidebarRafId);
        sidebarRafId = null;
      }

      isSidebarResizing = false;
      onSidebarWidthChange?.(sidebarWidth);
      document.removeEventListener('pointermove', onPointerMove);
      document.removeEventListener('pointerup', onPointerUp);
    }

    document.addEventListener('pointermove', onPointerMove);
    document.addEventListener('pointerup', onPointerUp);
  }

  // Right panel resize state
  // svelte-ignore state_referenced_locally
  let rightPanelWidth = $state(initialRightPanelWidth);
  let isRightPanelResizing = $state(false);
  let rightPanelRafId: number | null = null;

  function handleRightPanelResizeStart(e: PointerEvent) {
    e.preventDefault();
    isRightPanelResizing = true;
    const startX = e.clientX;
    const startWidth = rightPanelWidth;

    function onPointerMove(moveEvent: PointerEvent) {
      if (rightPanelRafId !== null) {
        cancelAnimationFrame(rightPanelRafId);
      }

      rightPanelRafId = requestAnimationFrame(() => {
        // Reverse direction: drag left = increase width
        const delta = startX - moveEvent.clientX;
        const newWidth = Math.min(maxRightPanelWidth, Math.max(minRightPanelWidth, startWidth + delta));
        rightPanelWidth = newWidth;
        rightPanelRafId = null;
      });
    }

    function onPointerUp() {
      if (rightPanelRafId !== null) {
        cancelAnimationFrame(rightPanelRafId);
        rightPanelRafId = null;
      }

      isRightPanelResizing = false;
      onRightPanelWidthChange?.(rightPanelWidth);
      document.removeEventListener('pointermove', onPointerMove);
      document.removeEventListener('pointerup', onPointerUp);
    }

    document.addEventListener('pointermove', onPointerMove);
    document.addEventListener('pointerup', onPointerUp);
  }

  // Combined resizing state for CSS
  const isResizing = $derived(isSidebarResizing || isRightPanelResizing);
</script>

<div class="flex flex-1 min-h-0 h-full {className}">
  <!-- Sidebar -->
  <div
    class="flex-shrink-0 border-r border-[var(--color-border-primary)]"
    class:resizing={isResizing}
    style="width: {sidebarWidth}px"
  >
    {@render sidebar()}
  </div>

  <!-- Sidebar Resize Handle (z-10 to stay above scrolling content) -->
  <div class="relative z-10 flex-shrink-0">
    <ResizeHandle
      orientation="vertical"
      isResizing={isSidebarResizing}
      onpointerdown={handleSidebarResizeStart}
      ariaLabel="Resize sidebar"
    />
  </div>

  <!-- Center Area (header + content row) -->
  <div class="flex-1 min-w-0 flex flex-col overflow-hidden">
    {#if header}
      <!-- Header (full width, Pattern B) -->
      <div class="flex-shrink-0">
        {@render header()}
      </div>
    {/if}

    <!-- Content Row -->
    <div class="flex flex-1 min-h-0" class:resizing={isResizing}>
      <!-- Main Content -->
      <div class="flex-1 min-w-0 flex flex-col overflow-hidden">
        {@render children()}
      </div>

      <!-- Right Panel (conditional) -->
      {#if showRightPanel && rightPanel}
        <!-- Right Panel Resize Handle (z-10 to stay above scrolling content) -->
        <div class="relative z-10 flex-shrink-0">
          <ResizeHandle
            orientation="vertical"
            isResizing={isRightPanelResizing}
            onpointerdown={handleRightPanelResizeStart}
            ariaLabel="Resize right panel"
          />
        </div>

        <!-- Right Panel -->
        <div
          class="flex-shrink-0 border-l border-[var(--color-border-primary)] overflow-hidden"
          class:resizing={isResizing}
          style="width: {rightPanelWidth}px"
        >
          {@render rightPanel()}
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
</style>
