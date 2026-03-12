<!--
  Modal Component
  Overlay modal with customizable size and close behavior
-->
<script lang="ts">
  import { fade, scale } from 'svelte/transition';
  import { IconButton } from '@sddp/ui';
  import type { ModalSize } from '../../types';

  interface Props {
    open?: boolean;
    title?: string;
    size?: ModalSize;
    closable?: boolean;
    closeOnOverlay?: boolean;
    closeOnEscape?: boolean;
    class?: string;
    onClose?: () => void;
    header?: import('svelte').Snippet;
    footer?: import('svelte').Snippet;
    children?: import('svelte').Snippet;
  }

  let {
    open = false,
    title = '',
    size = 'md',
    closable = true,
    closeOnOverlay = true,
    closeOnEscape = true,
    class: className = '',
    onClose,
    header,
    footer,
    children,
  }: Props = $props();

  let modalRef = $state<HTMLDivElement | undefined>();
  let triggerElement = $state<HTMLElement | null>(null);

  // Size classes
  const sizeClasses: Record<ModalSize, string> = {
    sm: 'max-w-sm',
    md: 'max-w-md',
    lg: 'max-w-lg',
    xl: 'max-w-xl',
    full: 'max-w-[90vw] max-h-[90vh]',
  };

  function handleClose() {
    if (closable) {
      onClose?.();
    }
  }

  function handleOverlayClick(event: MouseEvent) {
    if (closeOnOverlay && event.target === event.currentTarget) {
      handleClose();
    }
  }

  function handleKeydown(event: KeyboardEvent) {
    if (closeOnEscape && event.key === 'Escape') {
      handleClose();
    }
  }

  // Trap focus within modal
  function trapFocus(event: KeyboardEvent) {
    if (!modalRef || event.key !== 'Tab') return;

    const focusableElements = modalRef.querySelectorAll(
      'button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])'
    );
    const firstElement = focusableElements[0] as HTMLElement;
    const lastElement = focusableElements[focusableElements.length - 1] as HTMLElement;
    if (!firstElement || !lastElement) return;

    if (event.shiftKey) {
      if (document.activeElement === firstElement) {
        event.preventDefault();
        lastElement?.focus();
      }
    } else {
      if (document.activeElement === lastElement) {
        event.preventDefault();
        firstElement?.focus();
      }
    }
  }

  // Setup event listeners and focus management
  $effect(() => {
    if (open) {
      // Save trigger element for focus restoration on close
      triggerElement = document.activeElement as HTMLElement | null;

      document.addEventListener('keydown', handleKeydown);
      document.body.style.overflow = 'hidden';

      // Focus first focusable element
      const timeoutId = setTimeout(() => {
        const firstFocusable = modalRef?.querySelector(
          'button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])'
        ) as HTMLElement;
        if (firstFocusable) {
          firstFocusable.focus();
        } else {
          modalRef?.focus();
        }
      }, 100);

      return () => {
        clearTimeout(timeoutId);
        document.removeEventListener('keydown', handleKeydown);
        document.body.style.overflow = '';

        // Restore focus to the element that opened the modal
        if (triggerElement && typeof triggerElement.focus === 'function') {
          triggerElement.focus();
          triggerElement = null;
        }
      };
    }
  });
</script>

{#if open}
  <!-- Overlay -->
  <div
    class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-[var(--color-overlay)]/50"
    onclick={handleOverlayClick}
    onkeydown={trapFocus}
    role="dialog"
    aria-modal="true"
    aria-labelledby={title ? 'modal-title' : undefined}
    tabindex="-1"
    transition:fade={{ duration: 150 }}
  >
    <!-- Modal content -->
    <div
      bind:this={modalRef}
      tabindex="-1"
      class="w-full bg-[var(--color-bg-primary)] border border-[var(--color-border)] rounded-lg shadow-xl overflow-hidden {sizeClasses[size]} {className}"
      transition:scale={{ duration: 150, start: 0.95 }}
    >
      <!-- Header -->
      {#if header}
        {@render header()}
      {:else if title || closable}
        <div class="flex items-center justify-between px-4 py-3 border-b border-[var(--color-border)]">
          {#if title}
            <h2 id="modal-title" class="text-lg font-semibold text-[var(--color-text-primary)]">
              {title}
            </h2>
          {:else}
            <div></div>
          {/if}
          {#if closable}
            <IconButton icon="x" title="Close modal" onclick={handleClose} />
          {/if}
        </div>
      {/if}

      <!-- Body -->
      <div class="px-4 py-4 max-h-[60vh] overflow-auto">
        {#if children}
          {@render children()}
        {/if}
      </div>

      <!-- Footer -->
      {#if footer}
        <div class="px-4 py-3 border-t border-[var(--color-border)] bg-[var(--color-bg-secondary)]">
          {@render footer()}
        </div>
      {/if}
    </div>
  </div>
{/if}
