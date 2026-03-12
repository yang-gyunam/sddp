<script lang="ts">
  import type { Snippet } from 'svelte';
  import type { HTMLButtonAttributes } from 'svelte/elements';
  import type { ButtonVariant, ButtonSize } from '../types';

  interface Props
    extends Omit<HTMLButtonAttributes, 'type' | 'onclick' | 'onmousedown' | 'children' | 'class'>
  {
    variant?: ButtonVariant;
    size?: ButtonSize;
    disabled?: boolean;
    loading?: boolean;
    type?: HTMLButtonAttributes['type'];
    fullWidth?: boolean;
    onclick?: (e: MouseEvent) => void;
    onmousedown?: (e: MouseEvent) => void;
    children?: Snippet;
    class?: string;
    style?: string;
  }

  let {
    variant = 'primary',
    size = 'md',
    disabled = false,
    loading = false,
    type = 'button',
    fullWidth = false,
    title,
    onclick,
    onmousedown,
    children,
    class: className = '',
    style,
    ...rest
  }: Props = $props();

  const baseClasses =
    'inline-flex items-center justify-center font-medium rounded-md transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-offset-2 cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed';

  const variantClasses: Record<ButtonVariant, string> = {
    primary:
      'bg-[var(--color-accent-primary)] text-[var(--color-text-on-accent)] hover:bg-[var(--color-accent-primary)] focus-visible:ring-[var(--color-accent-primary)]',
    secondary:
      'bg-[var(--color-bg-tertiary)] text-[var(--color-text-primary)] hover:bg-[var(--color-bg-hover)] focus-visible:ring-[var(--color-neutral-400)]',
    ghost:
      'bg-transparent text-[var(--color-text-secondary)] hover:bg-[var(--color-bg-hover)] focus-visible:ring-[var(--color-neutral-400)]',
    danger:
      'bg-[var(--color-error-600)] text-[var(--color-text-on-accent)] hover:bg-[var(--color-error-700)] focus-visible:ring-[var(--color-error-500)]',
    outline:
      'border border-[var(--color-border)] bg-transparent text-[var(--color-text-primary)] hover:bg-[var(--color-bg-secondary)] focus-visible:ring-[var(--color-accent-primary)]',
    unstyled: '',
  };

  const sizeClasses: Record<ButtonSize, string> = {
    sm: 'px-3 py-1.5 text-sm gap-1.5',
    md: 'px-4 py-2 text-sm gap-2',
    lg: 'px-6 py-3 text-base gap-2.5',
  };

  function handleClick(e: MouseEvent) {
    if (disabled || loading) {
      e.preventDefault();
      return;
    }
    onclick?.(e);
  }
</script>

<button
  {...rest}
  {type}
  {title}
  {style}
  disabled={disabled || loading}
  onclick={handleClick}
  onmousedown={onmousedown}
  class="{variant === 'unstyled' ? '' : `${baseClasses} ${variantClasses[variant]} ${sizeClasses[size]}`} {fullWidth ? 'w-full' : ''} {className}"
>
  {#if loading}
    <svg
      class="animate-spin h-4 w-4"
      xmlns="http://www.w3.org/2000/svg"
      fill="none"
      viewBox="0 0 24 24"
    >
      <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"
      ></circle>
      <path
        class="opacity-75"
        fill="currentColor"
        d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
      ></path>
    </svg>
  {/if}
  {#if children}
    {@render children()}
  {/if}
</button>
