<!--
  SidePanel Component
  Right-side slide-in panel for editing functionality
-->
<script lang="ts">
  import { Button, IconButton, ResizeHandle } from '@sddp/ui';
  import type { SidePanelState } from '../../types';
  import { sidePanel } from '../../stores/side-panel.store';
  import { clamp } from '../../utils/number.utils';
  import EmptyState from '../idioms/EmptyState.svelte';

  interface Props {
    onClose?: () => void;
    onResize?: (width: number) => void;
    children?: import('svelte').Snippet;
  }

  let { onClose, onResize, children }: Props = $props();

  // State from store
  let visible = $state(false);
  let title = $state('');
  let width = $state(400);
  let minWidth = $state(280);
  let maxWidth = $state(800);
  let resizing = $state(false);
  let animating = $state(false);

  // Resizer state
  let startX = $state(0);
  let startWidth = $state(0);

  // Subscribe to side panel state
  $effect(() => {
    const unsubscribe = sidePanel.subscribe((state: SidePanelState) => {
      visible = state.visible;
      title = state.title;
      width = state.width;
      minWidth = state.minWidth;
      maxWidth = state.maxWidth;
      resizing = state.resizing;
      animating = state.animating;
    });
    return unsubscribe;
  });

  // Handle resize start
  function handleResizeStart(event: MouseEvent) {
    if (animating) return;

    event.preventDefault();
    resizing = true;
    startX = event.clientX;
    startWidth = width;

    sidePanel.startResize();

    document.addEventListener('mousemove', handleResizeMove);
    document.addEventListener('mouseup', handleResizeEnd);
    document.body.style.cursor = 'col-resize';
    document.body.style.userSelect = 'none';
  }

  // Handle resize move
  function handleResizeMove(event: MouseEvent) {
    if (!resizing) return;

    const deltaX = startX - event.clientX;
    const newWidth = clamp(startWidth + deltaX, minWidth, maxWidth);

    requestAnimationFrame(() => {
      sidePanel.setWidth(newWidth);
      onResize?.(newWidth);
    });
  }

  // Handle resize end
  function handleResizeEnd() {
    resizing = false;
    sidePanel.endResize();

    document.removeEventListener('mousemove', handleResizeMove);
    document.removeEventListener('mouseup', handleResizeEnd);
    document.body.style.cursor = '';
    document.body.style.userSelect = '';
  }

  // Handle close
  function handleClose() {
    sidePanel.hide();
    onClose?.();
  }

  // Handle escape key
  function handleKeydown(event: KeyboardEvent) {
    if (event.key === 'Escape' && visible) {
      handleClose();
    }
  }

  // Handle backdrop click
  function handleBackdropClick(event: MouseEvent) {
    if (event.target === event.currentTarget) {
      handleClose();
    }
  }

  $effect(() => {
    document.addEventListener('keydown', handleKeydown);
    return () => {
      document.removeEventListener('keydown', handleKeydown);
      if (resizing) {
        document.removeEventListener('mousemove', handleResizeMove);
        document.removeEventListener('mouseup', handleResizeEnd);
        document.body.style.cursor = '';
        document.body.style.userSelect = '';
      }
    };
  });
</script>

{#if visible}
  <!-- Backdrop -->
  <Button
    variant="unstyled"
    class="fixed inset-0 z-40 bg-[var(--color-overlay)]/30 transition-opacity duration-300"
    onclick={handleBackdropClick}
    onkeydown={(e) => e.key === 'Escape' && handleClose()}
    tabindex={-1}
    aria-label="Close panel"
  />

  <!-- Side Panel -->
  <div
    class="fixed top-0 right-0 bottom-0 z-50 flex transition-transform duration-300
      {animating ? 'animate-slide-in' : ''}"
    style="width: {width}px"
  >
    <!-- Resize Handle -->
    <ResizeHandle orientation="vertical" onmousedown={handleResizeStart} isResizing={resizing} class="bg-[var(--color-border)]" />

    <!-- Panel Content -->
    <div class="flex-1 flex flex-col bg-[var(--color-bg-secondary)] border-l border-[var(--color-border)] shadow-lg">
      <!-- Header -->
      <div class="flex items-center justify-between px-4 py-3 border-b border-[var(--color-border)] bg-[var(--color-bg-tertiary)]">
        <h3 class="text-sm font-semibold text-[var(--color-text-primary)]">
          {title}
        </h3>
        <IconButton icon="x" title="Close panel" onclick={handleClose} />
      </div>

      <!-- Body -->
      <div class="flex-1 overflow-auto">
        {#if children}
          {@render children()}
        {:else}
          <EmptyState icon="file" heading="No content to display" />
        {/if}
      </div>
    </div>
  </div>
{/if}

<style>
  @keyframes slide-in {
    from {
      transform: translateX(100%);
    }
    to {
      transform: translateX(0);
    }
  }

  .animate-slide-in {
    animation: slide-in 0.3s cubic-bezier(0.4, 0, 0.2, 1);
  }
</style>
