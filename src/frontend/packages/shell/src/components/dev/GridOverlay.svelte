<!--
  GridOverlay Component
  Development tool for visualizing responsive grid layout.
  Aligns with the first visible CardGrid element (.card-grid) in #main-content.
  Uses the same 12-column CSS Grid system and container query breakpoints as CardGrid.
  On pages without CardGrid, only shows content area boundary and info label.
-->
<script lang="ts">
  interface Props {
    cols?: 2 | 3 | 4 | 6;
    gap?: 'sm' | 'md' | 'lg';
    color?: string;
    opacity?: number;
    class?: string;
  }

  let {
    cols = 4,
    gap = 'md',
    color = '#ff00ff',
    opacity = 0.1,
    class: className = '',
  }: Props = $props();

  const gapPxMap = { sm: 8, md: 12, lg: 16 } as const;
  const BREAKPOINT_SM = 480;
  const BREAKPOINT_MD = 768;

  let overlayRect = $state({ left: 0, top: 0, width: 0, height: 0 });
  let hasCardGrid = $state(false);

  $effect(() => {
    const mainEl = document.getElementById('main-content');
    if (!mainEl) return;

    function measure() {
      const mainRect = mainEl!.getBoundingClientRect();

      // Find the first *visible* CardGrid (skip hidden tabs with zero-size rects)
      let cardGrid: Element | null = null;
      for (const cg of mainEl!.querySelectorAll('.card-grid')) {
        const r = cg.getBoundingClientRect();
        if (r.width > 0 && r.height > 0) { cardGrid = cg; break; }
      }

      hasCardGrid = !!cardGrid;

      if (cardGrid) {
        const cgRect = cardGrid.getBoundingClientRect();
        overlayRect = {
          left: cgRect.left,
          top: mainRect.top,
          width: cgRect.width,
          height: mainRect.height,
        };
      } else {
        overlayRect = {
          left: mainRect.left,
          top: mainRect.top,
          width: mainRect.width,
          height: mainRect.height,
        };
      }
    }

    const ro = new ResizeObserver(measure);
    ro.observe(mainEl);

    const mo = new MutationObserver(measure);
    mo.observe(mainEl, { childList: true, subtree: true });

    measure();

    return () => {
      ro.disconnect();
      mo.disconnect();
    };
  });

  let visibleCols = $derived(
    overlayRect.width < BREAKPOINT_SM ? 1 :
    overlayRect.width < BREAKPOINT_MD ? 2 :
    cols
  );

  let gapPx = $derived(gapPxMap[gap] ?? gapPxMap.md);
  let colSpan = $derived(Math.floor(12 / visibleCols));
  let columnArray = $derived(Array.from({ length: visibleCols }, (_, i) => i));

  let breakpointLabel = $derived(
    overlayRect.width < BREAKPOINT_SM ? `XS (<${BREAKPOINT_SM}px)` :
    overlayRect.width < BREAKPOINT_MD ? `SM (${BREAKPOINT_SM}px+)` :
    overlayRect.width < 1024 ? `MD (${BREAKPOINT_MD}px+)` :
    overlayRect.width < 1280 ? `LG (1024px+)` :
    overlayRect.width < 1536 ? `XL (1280px+)` :
    '2XL (1536px+)'
  );
</script>

<div
  class="pointer-events-none select-none {className}"
  style="
    position: fixed;
    z-index: 9999;
    left: {overlayRect.left}px;
    top: {overlayRect.top}px;
    width: {overlayRect.width}px;
    height: {overlayRect.height}px;
  "
  aria-hidden="true"
>
  {#if hasCardGrid}
    <!-- Grid columns — 12-column CSS Grid matching CardGrid -->
    <div
      class="h-full"
      style="
        display: grid;
        grid-template-columns: repeat(12, minmax(0, 1fr));
        gap: {gapPx}px;
      "
    >
      {#each columnArray as col (col)}
        <div
          class="h-full"
          style="
            grid-column: span {colSpan};
            background-color: {color};
            opacity: {opacity};
          "
        ></div>
      {/each}
    </div>
  {/if}

  <!-- Info label at top -->
  <div
    class="absolute top-0 left-0 right-0 h-6 flex items-center justify-center text-xs font-mono"
    style="
      color: {color};
      opacity: 0.8;
      background: linear-gradient(to bottom, rgba(0,0,0,0.5), transparent);
    "
  >
    <span class="px-2 py-0.5 rounded bg-black/50">
      {#if hasCardGrid}
        {visibleCols} col{visibleCols > 1 ? 's' : ''} | {gapPx}px gap | {Math.round(overlayRect.width)}px content
      {:else}
        No CardGrid | {Math.round(overlayRect.width)}px content
      {/if}
    </span>
  </div>

  <!-- Breakpoint indicator -->
  <div
    class="absolute bottom-2 right-2 px-2 py-1 rounded text-xs font-mono"
    style="
      color: white;
      background-color: {color};
      opacity: 0.8;
    "
  >
    {breakpointLabel}
  </div>
</div>
