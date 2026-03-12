<script lang="ts">
  import { slide } from 'svelte/transition';
  import { Icon } from '@sddp/ui';

  interface Props {
    visible?: boolean;
    message?: string;
    variant?: 'warning' | 'error';
  }

  let {
    visible = false,
    message = 'Connection lost - reconnecting...',
    variant = 'warning',
  }: Props = $props();

  const variantStyles: Record<string, string> = {
    warning: 'bg-amber-500/90 text-amber-950',
    error: 'bg-red-500/90 text-white',
  };

  const variantIcon: Record<string, string> = {
    warning: 'alert-triangle',
    error: 'alert-circle',
  };
</script>

{#if visible}
  <div
    class="flex items-center justify-center gap-2 px-4 py-1.5 text-xs font-medium z-50 {variantStyles[variant] ?? variantStyles.warning}"
    transition:slide={{ duration: 200 }}
    role="status"
    aria-live="polite"
  >
    <Icon name={variantIcon[variant] ?? 'alert-triangle'} size="xs" />
    <span>{message}</span>
  </div>
{/if}
