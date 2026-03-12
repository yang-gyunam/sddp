<!--
  ContextMenu Component
  Right-click context menu with customizable actions
-->
<script lang="ts">
  import { Icon, Button } from '@sddp/ui';
  import type { ContextMenuItem } from '../../types';

  interface Props {
    visible?: boolean;
    x?: number;
    y?: number;
    items?: ContextMenuItem[];
    class?: string;
    onClose: () => void;
  }

  let {
    visible = false,
    x = 0,
    y = 0,
    items = [],
    class: className = '',
    onClose,
  }: Props = $props();

  let menuElement = $state<HTMLDivElement | undefined>();

  // Adjust menu position to stay within viewport
  function adjustPosition(element: HTMLDivElement) {
    if (!element) return;

    const rect = element.getBoundingClientRect();
    const viewportWidth = window.innerWidth;
    const viewportHeight = window.innerHeight;

    let adjustedX = x;
    let adjustedY = y;

    // Adjust horizontal position
    if (x + rect.width > viewportWidth) {
      adjustedX = viewportWidth - rect.width - 10;
    }

    // Adjust vertical position
    if (y + rect.height > viewportHeight) {
      adjustedY = viewportHeight - rect.height - 10;
    }

    element.style.left = `${Math.max(0, adjustedX)}px`;
    element.style.top = `${Math.max(0, adjustedY)}px`;
  }

  // Handle menu item click
  function handleItemClick(event: MouseEvent, item: ContextMenuItem) {
    if (!item.disabled && item.action) {
      event.preventDefault();
      event.stopPropagation();
      item.action();
      onClose();
    }
  }

  // Close menu when clicking outside
  function handleClickOutside(event: MouseEvent) {
    if (menuElement && event.target && !menuElement.contains(event.target as Node)) {
      onClose();
    }
  }

  // Close menu on escape key
  function handleKeydown(event: KeyboardEvent) {
    if (event.key === 'Escape') {
      onClose();
    }
  }

  // Setup event listeners when menu becomes visible
  $effect(() => {
    if (visible) {
      // Use capture phase to handle clicks before they bubble up
      document.addEventListener('click', handleClickOutside, true);
      document.addEventListener('keydown', handleKeydown);

      // Adjust position after render
      if (menuElement) {
        adjustPosition(menuElement);
      }

      return () => {
        document.removeEventListener('click', handleClickOutside, true);
        document.removeEventListener('keydown', handleKeydown);
      };
    }
  });
</script>

{#if visible}
  <div
    bind:this={menuElement}
    class="fixed z-50 min-w-48 bg-[var(--color-bg-secondary)] border border-[var(--color-border)] rounded shadow-lg py-1 {className}"
    style="left: {x}px; top: {y}px;"
    role="menu"
    aria-label="Context menu"
  >
    {#each items as item (item.id)}
      {#if item.separator}
        <div class="border-t border-[var(--color-border)] my-1"></div>
      {:else}
        <Button
          variant="unstyled"
          class="w-full flex items-center justify-between px-3 py-1.5 text-sm text-left transition-colors duration-150
            {item.disabled
              ? 'opacity-50 cursor-not-allowed text-[var(--color-text-tertiary)]'
              : 'text-[var(--color-text-primary)] hover:bg-[var(--color-bg-tertiary)] cursor-pointer'}"
          disabled={item.disabled}
          onclick={(event) => handleItemClick(event, item)}
        >
          <div class="flex items-center">
            {#if item.icon}
              <Icon name={item.icon} size="sm" class="mr-2 flex-shrink-0" />
            {:else}
              <span class="w-5 mr-2"></span>
            {/if}
            <span>{item.label}</span>
          </div>
          {#if item.shortcut}
            <span class="text-xs text-[var(--color-text-tertiary)] ml-4">{item.shortcut}</span>
          {/if}
        </Button>
      {/if}
    {/each}
  </div>
{/if}
