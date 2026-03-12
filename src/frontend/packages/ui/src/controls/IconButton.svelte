<!--
  IconButton Control
  Icon-only button with fixed dimensions for consistent hover area.

  Sizes (fixed w/h):
  - xs: 20x20 (inline navigation, tight spaces)
  - sm: 24x24 (sidebar actions, compact areas)
  - md: 28x28 (default, header actions)
  - lg: 32x32 (prominent actions)

  Variants:
  - ghost: neutral hover (default)
  - danger: red hover (delete, destructive)
  - warn: orange hover (deactivate, caution)
  - success: green hover (save, confirm)
  - primary: accent color
-->
<script lang="ts">
  import Icon from './Icon.svelte';
  import type { IconSource } from '../types';

  type IconButtonSize = 'xs' | 'sm' | 'md' | 'lg';
  type IconButtonVariant = 'ghost' | 'danger' | 'warn' | 'success' | 'primary';

  interface Props {
    icon: string;
    source?: IconSource;
    variant?: IconButtonVariant;
    size?: IconButtonSize;
    disabled?: boolean;
    loading?: boolean;
    title?: string;
    type?: 'button' | 'submit' | 'reset';
    onclick?: (e: MouseEvent) => void;
    onmousedown?: (e: MouseEvent) => void;
    class?: string;
  }

  let {
    icon,
    source,
    variant = 'ghost',
    size = 'md',
    disabled = false,
    loading = false,
    title,
    type = 'button',
    onclick,
    onmousedown,
    class: className = '',
  }: Props = $props();

  const sizeConfig: Record<IconButtonSize, { button: string; icon: 'xs' | 'sm' | 'md' }> = {
    xs: { button: 'w-5 h-5', icon: 'xs' },
    sm: { button: 'w-6 h-6', icon: 'sm' },
    md: { button: 'w-7 h-7', icon: 'sm' },
    lg: { button: 'w-8 h-8', icon: 'md' },
  };

  const variantClasses: Record<IconButtonVariant, string> = {
    ghost: 'text-[var(--color-text-tertiary)] hover:text-[var(--color-text-primary)] hover:bg-[var(--color-bg-tertiary)]',
    danger: 'text-[var(--color-text-tertiary)] hover:text-[var(--color-error-500)] hover:bg-[var(--color-error-500)]/10',
    warn: 'text-[var(--color-text-tertiary)] hover:text-[var(--color-warning-500)] hover:bg-[var(--color-warning-500)]/10',
    success: 'text-[var(--color-text-tertiary)] hover:text-[var(--color-success-500)] hover:bg-[var(--color-success-500)]/10',
    primary: 'text-[var(--color-text-tertiary)] hover:text-[var(--color-accent-primary)] hover:bg-[var(--color-accent-primary)]/10',
  };

  const cfg = $derived(sizeConfig[size]);
  const variantCls = $derived(variantClasses[variant]);

  function handleClick(e: MouseEvent) {
    if (disabled || loading) {
      e.preventDefault();
      return;
    }
    onclick?.(e);
  }
</script>

<button
  {type}
  {title}
  aria-label={title ?? icon}
  disabled={disabled || loading}
  onclick={handleClick}
  onmousedown={onmousedown}
  class="inline-flex items-center justify-center rounded transition-colors cursor-pointer
    disabled:opacity-40 disabled:cursor-not-allowed disabled:hover:bg-transparent
    {cfg.button} {variantCls} {className}"
>
  {#if loading}
    <svg
      class="animate-spin w-4 h-4"
      xmlns="http://www.w3.org/2000/svg"
      fill="none"
      viewBox="0 0 24 24"
    >
      <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
      <path
        class="opacity-75"
        fill="currentColor"
        d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
      ></path>
    </svg>
  {:else}
    <Icon name={icon} {source} size={cfg.icon} />
  {/if}
</button>
