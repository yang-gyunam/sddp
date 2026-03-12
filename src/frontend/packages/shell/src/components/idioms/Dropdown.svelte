<!--
  Dropdown Component
  Trigger-based dropdown menu
-->
<script lang="ts">
  import { Button } from '@sddp/ui';

  interface Props {
    open?: boolean;
    position?: 'bottom-left' | 'bottom-right' | 'top-left' | 'top-right';
    class?: string;
    triggerClass?: string;
    menuClass?: string;
    closeOnSelect?: boolean;
    triggerLabel?: string;
    onOpenChange?: (open: boolean) => void;
    trigger: import('svelte').Snippet;
    children?: import('svelte').Snippet;
  }

  let {
    open = $bindable(false),
    position = 'bottom-left',
    class: className = '',
    triggerClass = '',
    menuClass = '',
    closeOnSelect = true,
    triggerLabel,
    onOpenChange,
    trigger,
    children,
  }: Props = $props();

  let containerRef = $state<HTMLDivElement | undefined>();

  function toggle() {
    open = !open;
    onOpenChange?.(open);
  }

  function close() {
    open = false;
    onOpenChange?.(false);
  }

  // Handle click outside
  function handleClickOutside(event: MouseEvent) {
    if (containerRef && !containerRef.contains(event.target as Node)) {
      close();
    }
  }

  // Handle escape key
  function handleKeydown(event: KeyboardEvent) {
    if (event.key === 'Escape') {
      close();
    }
  }

  // Handle menu item click
  function handleMenuClick() {
    if (closeOnSelect) {
      close();
    }
  }

  // Setup event listeners
  $effect(() => {
    if (open) {
      document.addEventListener('click', handleClickOutside, true);
      document.addEventListener('keydown', handleKeydown);

      return () => {
        document.removeEventListener('click', handleClickOutside, true);
        document.removeEventListener('keydown', handleKeydown);
      };
    }
  });

  // Position classes
  const positionClasses = {
    'bottom-left': 'top-full left-0 mt-1',
    'bottom-right': 'top-full right-0 mt-1',
    'top-left': 'bottom-full left-0 mb-1',
    'top-right': 'bottom-full right-0 mb-1',
  };
</script>

<div bind:this={containerRef} class="relative inline-block {className}">
  <!-- Trigger -->
  <Button
    variant="unstyled"
    class="dropdown-trigger {triggerClass}"
    onclick={toggle}
    aria-haspopup="true"
    aria-expanded={open}
    aria-label={triggerLabel ?? 'Toggle dropdown'}
  >
    {@render trigger()}
  </Button>

  <!-- Menu -->
  {#if open}
    <div
      class="absolute z-50 min-w-40 bg-[var(--color-bg-secondary)] border border-[var(--color-border)] rounded shadow-lg py-1 {positionClasses[position]} {menuClass}"
      role="menu"
      tabindex="-1"
      aria-label="Dropdown menu"
      onclick={handleMenuClick}
      onkeydown={(e) => e.key === 'Escape' && close()}
    >
      {#if children}
        {@render children()}
      {/if}
    </div>
  {/if}
</div>
