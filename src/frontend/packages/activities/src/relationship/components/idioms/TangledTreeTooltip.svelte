<script lang="ts">
  import { Icon } from '@sddp/ui';
  import type { PositionedNode } from '../../types';
  import { ENTITY_TYPE_STYLES, NODE_COLORS } from '../../types';

  interface Props {
    node: PositionedNode;
    x: number;
    y: number;
    class?: string;
  }

  let { node, x, y, class: className = '' }: Props = $props();

  const style = $derived(ENTITY_TYPE_STYLES[node.entityType]);
  const nodeColor = $derived(NODE_COLORS[node.entityType]);
</script>

<div
  class="absolute z-50 pointer-events-none bg-[var(--color-bg-secondary)] border border-[var(--color-border-primary)] rounded-lg shadow-lg px-3 py-2 text-xs min-w-[160px] {className}"
  style="left: {x + 12}px; top: {y - 8}px;"
>
  <div class="flex items-center gap-1.5 mb-1">
    <span
      class="w-2 h-2 rounded-full shrink-0"
      style="background-color: {nodeColor}"
    ></span>
    <span class="font-medium text-[var(--color-text-primary)]">
      {style?.label ?? node.entityType}
    </span>
  </div>
  <div class="flex items-center gap-1 mb-0.5">
    <Icon name={style?.icon ?? 'circle'} size="xs" class="text-[var(--color-text-tertiary)] shrink-0" />
    <span class="text-[var(--color-text-secondary)] truncate max-w-[200px]">{node.label}</span>
  </div>
  {#if node.code}
    <div class="text-[var(--color-text-tertiary)] mt-0.5 font-mono">{node.code}</div>
  {/if}
  {#if node.status}
    <div class="text-[var(--color-text-tertiary)] mt-0.5">Status: {node.status}</div>
  {/if}
</div>
