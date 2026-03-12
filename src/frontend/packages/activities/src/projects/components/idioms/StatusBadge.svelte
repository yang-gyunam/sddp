<!--
  StatusBadge Component
  Generic status badge with customizable variants
-->
<script lang="ts">
  import { Icon } from '@sddp/ui';

  type Variant = 'default' | 'success' | 'warning' | 'error' | 'info';

  interface Props {
    label: string;
    variant?: Variant;
    icon?: string;
    showIcon?: boolean;
    size?: 'sm' | 'md';
    class?: string;
  }

  let {
    label,
    variant = 'default',
    icon,
    showIcon = true,
    size = 'sm',
    class: className = '',
  }: Props = $props();

  const variantStyles: Record<Variant, { bg: string; text: string; border: string }> = {
    default: {
      bg: 'bg-gray-50 dark:bg-gray-900',
      text: 'text-gray-600 dark:text-gray-400',
      border: 'border-gray-200 dark:border-gray-700',
    },
    success: {
      bg: 'bg-green-50 dark:bg-green-950',
      text: 'text-green-700 dark:text-green-300',
      border: 'border-green-200 dark:border-green-800',
    },
    warning: {
      bg: 'bg-yellow-50 dark:bg-yellow-950',
      text: 'text-yellow-700 dark:text-yellow-300',
      border: 'border-yellow-200 dark:border-yellow-800',
    },
    error: {
      bg: 'bg-red-50 dark:bg-red-950',
      text: 'text-red-700 dark:text-red-300',
      border: 'border-red-200 dark:border-red-800',
    },
    info: {
      bg: 'bg-blue-50 dark:bg-blue-950',
      text: 'text-blue-700 dark:text-blue-300',
      border: 'border-blue-200 dark:border-blue-800',
    },
  };

  const style = $derived(variantStyles[variant]);
  const sizeClasses = $derived(size === 'sm' ? 'text-xs px-1.5 py-0.5' : 'text-sm px-2 py-1');
</script>

<span
  class="inline-flex items-center gap-1 font-medium rounded {style.bg} {style.text} border {style.border} {sizeClasses} {className}"
>
  {#if showIcon && icon}
    <Icon name={icon} size="xs" />
  {/if}
  {label}
</span>
