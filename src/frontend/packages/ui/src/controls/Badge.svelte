<script lang="ts">
  import type { BadgeVariant, BadgeSize } from '../types';

  interface Props {
    variant?: BadgeVariant;
    size?: BadgeSize;
    closable?: boolean;
    onclose?: () => void;
    children?: import('svelte').Snippet;
  }

  let { variant = 'default', size = 'md', closable = false, onclose, children }: Props = $props();

  const baseClasses =
    'inline-flex items-center font-medium rounded-full transition-colors';

  const variantClasses: Record<BadgeVariant, string> = {
    default:
      'bg-[var(--color-neutral-100)] text-[var(--color-neutral-600)] dark:bg-[var(--color-neutral-800)] dark:text-[var(--color-neutral-400)]',
    primary:
      'bg-[var(--color-accent-primary)]/10 text-[var(--color-accent-primary)] dark:bg-[var(--color-accent-primary)]/15 dark:text-[var(--color-accent-primary)]',
    secondary:
      'bg-[var(--color-neutral-200)] text-[var(--color-neutral-700)] dark:bg-[var(--color-neutral-700)] dark:text-[var(--color-neutral-300)]',
    success:
      'bg-[var(--color-success-500)]/10 text-[var(--color-success-600)] dark:bg-[var(--color-success-500)]/15 dark:text-[var(--color-success-400)]',
    warning:
      'bg-[var(--color-warning-500)]/10 text-[var(--color-warning-600)] dark:bg-[var(--color-warning-500)]/15 dark:text-[var(--color-warning-400)]',
    error:
      'bg-[var(--color-error-500)]/10 text-[var(--color-error-600)] dark:bg-[var(--color-error-500)]/15 dark:text-[var(--color-error-400)]',
    info: 
      'bg-[var(--color-info-500)]/10 text-[var(--color-info-600)] dark:bg-[var(--color-info-500)]/15 dark:text-[var(--color-info-400)]',
  };

  const sizeClasses: Record<BadgeSize, string> = {
    sm: 'px-2 py-0.5 text-xs gap-1',
    md: 'px-2.5 py-0.5 text-sm gap-1.5',
    lg: 'px-3 py-1 text-sm gap-2',
  };

  function handleClose(e: MouseEvent) {
    e.stopPropagation();
    onclose?.();
  }
</script>

<span class="{baseClasses} {variantClasses[variant]} {sizeClasses[size]}">
  {#if children}
    {@render children()}
  {/if}
  {#if closable}
    <button
      type="button"
      onclick={handleClose}
      class="ml-1 -mr-0.5 h-4 w-4 inline-flex items-center justify-center rounded-full hover:bg-[var(--color-neutral-900)]/10 focus:outline-none"
      aria-label="Remove"
    >
      <svg class="h-3 w-3" fill="currentColor" viewBox="0 0 20 20">
        <path
          fill-rule="evenodd"
          d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z"
          clip-rule="evenodd"
        />
      </svg>
    </button>
  {/if}
</span>
