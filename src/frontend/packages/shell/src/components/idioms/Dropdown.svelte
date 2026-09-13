<!--
  Dropdown Component
  Trigger-based dropdown menu
-->
<script lang="ts">
  /**
   * The trigger snippet owns the control. Wrapping caller content in our own button nested a
   * button inside a button wherever an IconButton was passed, which axe reports as
   * nested-interactive and which gives screen readers two controls for one action.
   */
  interface TriggerContext {
    /** Opens or closes the menu. Wire this to the trigger control's onclick. */
    toggle: () => void;
    /** Current menu state, for aria-expanded on the trigger control. */
    open: boolean;
  }

  interface Props {
    open?: boolean;
    position?: 'bottom-left' | 'bottom-right' | 'top-left' | 'top-right';
    class?: string;
    menuClass?: string;
    closeOnSelect?: boolean;
    onOpenChange?: (open: boolean) => void;
    trigger: import('svelte').Snippet<[TriggerContext]>;
    children?: import('svelte').Snippet;
  }

  let {
    open = $bindable(false),
    position = 'bottom-left',
    class: className = '',
    menuClass = '',
    closeOnSelect = true,
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
  <!-- Trigger: the caller's control, which carries onclick, aria-haspopup and aria-expanded -->
  {@render trigger({ toggle, open })}

  <!-- Menu -->
  {#if open}
    <div
      class="absolute z-50 min-w-40 bg-[var(--color-bg-secondary)] border border-[var(--color-border)] rounded shadow-lg py-1 {positionClasses[
        position
      ]} {menuClass}"
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
