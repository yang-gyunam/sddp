<script lang="ts">
  import type { SimulationNode } from '../../types';
  import { ENTITY_TYPE_STYLES, NODE_COLORS } from '../../types';

  interface Props {
    node: SimulationNode;
    x: number;
    y: number;
    class?: string;
  }

  let { node, x, y, class: className = '' }: Props = $props();

  const style = $derived(ENTITY_TYPE_STYLES[node.entityType]);
</script>

<div
  class="absolute bg-gray-900 dark:bg-gray-800 text-white px-3 py-2 rounded-lg text-xs pointer-events-none z-50 shadow-lg border border-gray-700 {className}"
  style="left: {x + 16}px; top: {y - 12}px;"
>
  <div class="flex items-center gap-1.5 mb-1">
    <span
      class="w-2 h-2 rounded-full shrink-0"
      style="background-color: {NODE_COLORS[node.entityType]}"
    ></span>
    <span class="font-medium">{node.label}</span>
  </div>
  <div class="text-gray-400">{style.label}</div>
  {#if node.status}
    <div class="text-gray-500 mt-0.5">Status: {node.status}</div>
  {/if}
  {#if node.depth > 0}
    <div class="text-gray-500">Depth: {node.depth}</div>
  {/if}
</div>
