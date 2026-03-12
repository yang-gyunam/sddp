<script lang="ts">
  import {
    ENTITY_TYPE_STYLES,
    RELATION_TYPE_STYLES,
    NODE_COLORS,
    EDGE_COLORS,
    EDGE_LINE_STYLES,
    EDGE_DASH_ARRAYS,
  } from '../../types';
  import type { EntityType, RelationType } from '../../types';

  interface Props {
    class?: string;
  }

  let { class: className = '' }: Props = $props();

  const entityTypes = Object.entries(ENTITY_TYPE_STYLES) as [EntityType, (typeof ENTITY_TYPE_STYLES)[EntityType]][];
  const relationTypes = Object.entries(RELATION_TYPE_STYLES) as [RelationType, (typeof RELATION_TYPE_STYLES)[RelationType]][];
</script>

<div
  class="absolute bottom-3 left-3 bg-white/95 dark:bg-gray-800/95 backdrop-blur-sm rounded-lg border border-gray-200 dark:border-gray-700 p-3 text-xs shadow-md {className}"
>
  <div class="font-medium mb-2 text-gray-700 dark:text-gray-300">Entities</div>
  <div class="flex flex-wrap gap-x-3 gap-y-1 mb-3">
    {#each entityTypes as [type, style] (type)}
      <div class="flex items-center gap-1.5">
        <span
          class="w-3 h-3 rounded-full shrink-0"
          style="background-color: {NODE_COLORS[type]}"
        ></span>
        <span class="text-gray-600 dark:text-gray-400">{style.label}</span>
      </div>
    {/each}
  </div>

  <div class="font-medium mb-2 text-gray-700 dark:text-gray-300">Relations</div>
  <div class="grid grid-cols-2 gap-x-4 gap-y-1">
    {#each relationTypes as [type, style] (type)}
      {@const lineStyle = EDGE_LINE_STYLES[type]}
      {@const dashArray = EDGE_DASH_ARRAYS[lineStyle]}
      <div class="flex items-center gap-1.5">
        <svg width="20" height="8" class="shrink-0">
          <line
            x1="0"
            y1="4"
            x2="20"
            y2="4"
            stroke={EDGE_COLORS[type]}
            stroke-width="2"
            stroke-dasharray={dashArray === 'none' ? undefined : dashArray}
          />
        </svg>
        <span class="text-gray-600 dark:text-gray-400">{style.label}</span>
      </div>
    {/each}
  </div>
</div>
