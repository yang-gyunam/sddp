<!-- Shared entity row for trace panels and file relationships. -->
<script lang="ts">
  import { Icon, Button } from '@sddp/ui';
  import { ENTITY_TYPE_STYLES, type EntityType } from '../../types';

  let {
    entityType,
    label,
    code,
    current = false,
    iconTitle,
    onclick,
    class: className = '',
  }: {
    entityType: EntityType;
    label: string;
    code?: string | null;
    current?: boolean;
    iconTitle?: string;
    onclick?: () => void;
    class?: string;
  } = $props();
  const style = $derived(ENTITY_TYPE_STYLES[entityType]);
</script>

<Button
  variant="unstyled"
  {onclick}
  title={label}
  class="w-full flex items-center gap-2 p-1.5 rounded-lg transition-colors {current
    ? 'bg-[var(--color-accent-primary)]/10 border border-[var(--color-accent-primary)]/30'
    : 'hover:bg-[var(--color-bg-tertiary)]'} {className}"
>
  <div
    title={iconTitle}
    class="flex items-center justify-center w-5 h-5 rounded flex-shrink-0 {style?.bgColor ||
      'bg-[var(--color-bg-tertiary)]'}"
  >
    <Icon
      name={style?.icon || 'file'}
      size="xs"
      class={style?.color || 'text-[var(--color-text-tertiary)]'}
    />
  </div>
  <div class="flex-1 min-w-0 text-left">
    <div class="text-sm font-medium text-[var(--color-text-primary)] truncate">{label}</div>
    {#if code}<div class="text-xs text-[var(--color-text-tertiary)] truncate">{code}</div>{/if}
  </div>
  {#if current}<span class="text-xs text-[var(--color-accent-text)] font-medium flex-shrink-0"
      >Current</span
    >{/if}
</Button>
