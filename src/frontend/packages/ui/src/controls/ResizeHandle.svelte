<script lang="ts">
  import type { ResizeDirection } from '../types';
  import type { MouseEventHandler, KeyboardEventHandler } from 'svelte/elements';

  interface Props {
    // --- Self-contained mode (direction-based, built-in drag logic) ---
    direction?: ResizeDirection;
    size?: number;
    minSize?: number;
    maxSize?: number;
    onresize?: (delta: { x: number; y: number }) => void;
    onresizestart?: () => void;
    onresizeend?: () => void;

    // --- Delegated mode (orientation-based, parent manages drag) ---
    orientation?: 'horizontal' | 'vertical';
    onpointerdown?: (e: PointerEvent) => void;
    onmousedown?: MouseEventHandler<HTMLDivElement>;
    onkeydown?: KeyboardEventHandler<HTMLDivElement>;
    isResizing?: boolean;
    ariaLabel?: string;

    // --- Common ---
    class?: string;
  }

  let {
    // Self-contained mode
    direction,
    size = 4,
    minSize: _minSize,
    maxSize: _maxSize,
    onresize,
    onresizestart,
    onresizeend,

    // Delegated mode
    orientation,
    onpointerdown: externalPointerDown,
    onmousedown: externalMouseDown,
    onkeydown: externalKeyDown,
    isResizing = false,
    ariaLabel,

    // Common
    class: className = '',
  }: Props = $props();

  // Determine which mode: if `orientation` is provided, use delegated mode
  const isDelegated = $derived(orientation !== undefined);
  // Resolve effective direction for styling
  const effectiveDirection = $derived<ResizeDirection>(
    isDelegated ? (orientation as ResizeDirection) : (direction ?? 'horizontal')
  );

  // --- Self-contained mode state ---
  let isDragging = $state(false);
  let startX = 0;
  let startY = 0;

  // --- Self-contained mode: direction-based classes ---
  const directionClasses: Record<ResizeDirection, string> = {
    horizontal: 'w-1 h-full cursor-col-resize hover:bg-[var(--color-accent-primary)]/50',
    vertical: 'h-1 w-full cursor-row-resize hover:bg-[var(--color-accent-primary)]/50',
    both: 'w-3 h-3 cursor-nwse-resize hover:bg-[var(--color-accent-primary)]/50 rounded-sm',
  };

  // --- Delegated mode: orientation-based classes ---
  const orientationClasses = $derived(
    orientation === 'horizontal'
      ? 'h-1 w-full cursor-row-resize'
      : 'w-1 h-full cursor-col-resize'
  );

  // Active highlight class
  const activeClass = $derived(
    isDelegated
      ? (isResizing ? 'bg-[var(--color-accent-primary)]' : '')
      : (isDragging ? 'bg-[var(--color-accent-primary)]' : '')
  );

  // --- Self-contained drag handlers ---
  function handleMouseDown(e: MouseEvent) {
    if (isDelegated) return;
    e.preventDefault();
    isDragging = true;
    startX = e.clientX;
    startY = e.clientY;
    onresizestart?.();

    document.addEventListener('mousemove', handleMouseMove);
    document.addEventListener('mouseup', handleMouseUp);
  }

  function handleMouseMove(e: MouseEvent) {
    if (!isDragging) return;

    const deltaX = e.clientX - startX;
    const deltaY = e.clientY - startY;

    startX = e.clientX;
    startY = e.clientY;

    onresize?.({ x: deltaX, y: deltaY });
  }

  function handleMouseUp() {
    isDragging = false;
    onresizeend?.();

    document.removeEventListener('mousemove', handleMouseMove);
    document.removeEventListener('mouseup', handleMouseUp);
  }

  function handleTouchStart(e: TouchEvent) {
    if (isDelegated) return;
    const touch = e.touches[0];
    if (e.touches.length !== 1 || !touch) return;
    e.preventDefault();
    isDragging = true;
    startX = touch.clientX;
    startY = touch.clientY;
    onresizestart?.();
  }

  function handleTouchMove(e: TouchEvent) {
    if (isDelegated) return;
    const touch = e.touches[0];
    if (!isDragging || e.touches.length !== 1 || !touch) return;

    const deltaX = touch.clientX - startX;
    const deltaY = touch.clientY - startY;

    startX = touch.clientX;
    startY = touch.clientY;

    onresize?.({ x: deltaX, y: deltaY });
  }

  function handleTouchEnd() {
    if (isDelegated) return;
    isDragging = false;
    onresizeend?.();
  }

  function handleSelfKeyDown(e: KeyboardEvent) {
    if (isDelegated) return;
    const step = e.shiftKey ? 10 : 1;
    let deltaX = 0;
    let deltaY = 0;

    switch (e.key) {
      case 'ArrowLeft':
        deltaX = -step;
        break;
      case 'ArrowRight':
        deltaX = step;
        break;
      case 'ArrowUp':
        deltaY = -step;
        break;
      case 'ArrowDown':
        deltaY = step;
        break;
      default:
        return;
    }

    e.preventDefault();
    onresize?.({ x: deltaX, y: deltaY });
  }
</script>

{#if isDelegated}
  <!-- Delegated mode: parent controls drag behavior -->
  <!-- svelte-ignore a11y_no_noninteractive_tabindex -->
  <!-- svelte-ignore a11y_no_noninteractive_element_interactions -->
  <div
    onpointerdown={externalPointerDown}
    onmousedown={externalMouseDown}
    onkeydown={externalKeyDown}
    class="{orientationClasses} bg-transparent hover:bg-[var(--color-accent-primary)]/30 transition-colors {activeClass} {orientation === 'vertical' ? '-ml-1' : ''} {className}"
    role="separator"
    aria-orientation={orientation}
    aria-valuenow={50}
    aria-valuemin={0}
    aria-valuemax={100}
    aria-label={ariaLabel ?? 'Resize panel'}
    tabindex="0"
  >
  </div>
{:else}
  <!-- Self-contained mode: built-in drag logic -->
  <div
    role="slider"
    aria-orientation={effectiveDirection === 'vertical' ? 'horizontal' : 'vertical'}
    aria-valuenow={0}
    aria-valuemin={0}
    aria-valuemax={100}
    tabindex="0"
    class="flex-shrink-0 bg-[var(--color-border)] transition-colors {directionClasses[effectiveDirection]} {activeClass} {className}"
    style={effectiveDirection === 'horizontal' ? `width: ${size}px` : effectiveDirection === 'vertical' ? `height: ${size}px` : `width: ${size * 3}px; height: ${size * 3}px`}
    onmousedown={handleMouseDown}
    ontouchstart={handleTouchStart}
    ontouchmove={handleTouchMove}
    ontouchend={handleTouchEnd}
    onkeydown={handleSelfKeyDown}
  >
    {#if effectiveDirection === 'both'}
      <svg class="w-full h-full text-[var(--color-text-tertiary)]" viewBox="0 0 6 6" fill="currentColor">
        <circle cx="1" cy="1" r="0.5" />
        <circle cx="3" cy="1" r="0.5" />
        <circle cx="1" cy="3" r="0.5" />
        <circle cx="3" cy="3" r="0.5" />
        <circle cx="5" cy="3" r="0.5" />
        <circle cx="3" cy="5" r="0.5" />
        <circle cx="5" cy="5" r="0.5" />
      </svg>
    {/if}
  </div>
{/if}
