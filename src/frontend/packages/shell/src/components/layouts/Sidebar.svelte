<script lang="ts">
  import { Icon, Typography, IconButton, Button, ResizeHandle } from '@sddp/ui';
  import { clamp } from '../../utils/number.utils';

  interface Props {
    title?: string;
    width?: number;
    minWidth?: number;
    maxWidth?: number;
    collapsed?: boolean;
    onResize?: (width: number) => void;
    onToggle?: () => void;
    headerActions?: import('svelte').Snippet;
    children?: import('svelte').Snippet;
  }

  let {
    title = 'Explorer',
    width = 300,
    minWidth = 200,
    maxWidth = 600,
    collapsed = false,
    onResize,
    onToggle,
    headerActions,
    children,
  }: Props = $props();

  let isResizing = $state(false);
  let startX = $state(0);
  let startWidth = $state(0);

  function handlePointerDown(e: PointerEvent) {
    e.preventDefault();
    isResizing = true;
    startX = e.clientX;
    startWidth = width;

    const target = e.target as HTMLElement;
    target.setPointerCapture(e.pointerId);

    document.addEventListener('pointermove', handlePointerMove);
    document.addEventListener('pointerup', handlePointerUp);
    document.body.style.cursor = 'col-resize';
    document.body.style.userSelect = 'none';
  }

  function handlePointerMove(e: PointerEvent) {
    if (!isResizing) return;

    const diff = e.clientX - startX;
    const newWidth = clamp(startWidth + diff, minWidth, maxWidth);
    onResize?.(newWidth);
  }

  function handlePointerUp(e: PointerEvent) {
    isResizing = false;

    const target = e.target as HTMLElement;
    if (target.hasPointerCapture?.(e.pointerId)) {
      target.releasePointerCapture(e.pointerId);
    }

    document.removeEventListener('pointermove', handlePointerMove);
    document.removeEventListener('pointerup', handlePointerUp);
    document.body.style.cursor = '';
    document.body.style.userSelect = '';
  }

  function handleToggle() {
    onToggle?.();
  }

  function handleKeyDown(e: KeyboardEvent) {
    if (e.key === 'Enter' || e.key === ' ') {
      e.preventDefault();
      handleToggle();
    }
  }
</script>

{#if !collapsed}
  <div
    class="relative flex flex-col h-full bg-[var(--color-bg-secondary)] border-r border-[var(--color-border)]"
    style="width: {width}px"
  >
    <!-- Header -->
    <div
      class="flex items-center justify-between h-9 px-3 border-b border-[var(--color-border)] shrink-0"
    >
      <Typography as="span" variant="caption" color="secondary" class="uppercase tracking-wider font-semibold">
        {title}
      </Typography>
      <div class="flex items-center gap-1">
        {#if headerActions}
          {@render headerActions()}
        {/if}
        <IconButton icon="layout-sidebar-left" title="Collapse sidebar (Ctrl+B)" onclick={handleToggle} />
      </div>
    </div>

    <!-- Content -->
    <div class="flex-1 overflow-auto">
      {#if children}
        {@render children()}
      {/if}
    </div>

    <!-- Resize handle -->
    <ResizeHandle
      orientation="vertical"
      onpointerdown={handlePointerDown}
      onkeydown={handleKeyDown}
      isResizing={isResizing}
      class="absolute top-0 right-0"
    />
  </div>
{:else}
  <Button
    variant="unstyled"
    onclick={handleToggle}
    class="flex items-center justify-center w-10 h-full
      bg-[var(--color-bg-secondary)] border-r border-[var(--color-border)]
      text-[var(--color-text-tertiary)] hover:text-[var(--color-text-primary)]
      hover:bg-[var(--color-bg-tertiary)] transition-colors duration-150"
    title="Expand sidebar (Ctrl+B)"
  >
    <Icon name="panel-left-open" size="sm" />
  </Button>
{/if}
