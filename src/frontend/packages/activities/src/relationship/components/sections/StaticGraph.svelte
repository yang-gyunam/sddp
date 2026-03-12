<!-- Section: StaticGraph -->
<script lang="ts">
  import { SvelteMap, SvelteSet } from 'svelte/reactivity';
  import type { RelationshipGraphData, GraphNode, GraphEdge, RelationType } from '../../types';
  import { RELATION_TYPE_STYLES, ENTITY_TYPE_STYLES, EDGE_COLORS, EDGE_DASH_ARRAYS, EDGE_LINE_STYLES, NODE_COLORS } from '../../types';
  import { truncate } from '@sddp/shell';
  import { Icon, IconButton, Button } from '@sddp/ui';

  interface Props {
    graph: RelationshipGraphData;
    width?: number;
    height?: number;
    layout?: 'radial' | 'tree';
    onNodeClick?: (node: GraphNode) => void;
    onEdgeClick?: (edge: GraphEdge) => void;
    class?: string;
  }

  let {
    graph,
    width = 600,
    height = 400,
    layout = 'radial',
    onNodeClick,
    onEdgeClick,
    class: className = '',
  }: Props = $props();

  let hoveredNodeId: string | null = $state(null);
  let hoveredEdgeId: string | null = $state(null);
  let selectedNodeId: string | null = $state(null);

  const centerX = $derived(width / 2);
  const centerY = $derived(height / 2);
  const baseRadius = $derived(Math.min(width, height) / 4);

  const connectedNodeIds = $derived.by(() => {
    if (!hoveredNodeId) return null;
    const ids = new SvelteSet<string>([hoveredNodeId]);
    for (const edge of graph.edges) {
      if (edge.sourceId === hoveredNodeId) ids.add(edge.targetId);
      if (edge.targetId === hoveredNodeId) ids.add(edge.sourceId);
    }
    return ids;
  });

  const nodePositions = $derived.by(() => {
    const positions = new SvelteMap<string, { x: number; y: number }>();
    const nodesByDepth = new SvelteMap<number, GraphNode[]>();

    for (const node of graph.nodes) {
      if (!nodesByDepth.has(node.depth)) {
        nodesByDepth.set(node.depth, []);
      }
      nodesByDepth.get(node.depth)!.push(node);
    }

    if (layout === 'tree') {
      const depthLevels = [...nodesByDepth.keys()].sort((a, b) => a - b);
      const maxDepth = depthLevels[depthLevels.length - 1] ?? 0;
      const margin = 40;
      const columnWidth = maxDepth > 0 ? (width - margin * 2) / maxDepth : 0;

      for (const depth of depthLevels) {
        const nodes = nodesByDepth.get(depth) ?? [];
        const rowGap = nodes.length > 0 ? (height - margin * 2) / (nodes.length + 1) : 0;
        nodes.forEach((node, index) => {
          const x = margin + depth * columnWidth;
          const y = margin + (index + 1) * rowGap;
          positions.set(node.id, { x, y });
        });
      }

      return positions;
    }

    for (const [depth, nodes] of nodesByDepth) {
      const radius = depth === 0 ? 0 : baseRadius * (depth * 0.6);
      const angleStep = (2 * Math.PI) / Math.max(nodes.length, 1);
      const startAngle = -Math.PI / 2;

      nodes.forEach((node, index) => {
        if (depth === 0) {
          positions.set(node.id, { x: centerX, y: centerY });
        } else {
          const angle = startAngle + index * angleStep;
          positions.set(node.id, {
            x: centerX + radius * Math.cos(angle),
            y: centerY + radius * Math.sin(angle),
          });
        }
      });
    }

    return positions;
  });

  function getEdgePath(edge: GraphEdge): string {
    const source = nodePositions.get(edge.sourceId);
    const target = nodePositions.get(edge.targetId);
    if (!source || !target) return '';

    if (layout === 'tree') {
      return `M ${source.x} ${source.y} L ${target.x} ${target.y}`;
    }

    const midX = (source.x + target.x) / 2;
    const midY = (source.y + target.y) / 2;
    const sourceNode = graph.nodes.find((n) => n.id === edge.sourceId);
    const targetNode = graph.nodes.find((n) => n.id === edge.targetId);

    if (sourceNode && targetNode && sourceNode.depth === targetNode.depth && sourceNode.depth > 0) {
      const curveOffset = 30;
      const dx = target.x - source.x;
      const dy = target.y - source.y;
      const distance = Math.sqrt(dx * dx + dy * dy) || 1;
      const nx = (-dy / distance) * curveOffset;
      const ny = (dx / distance) * curveOffset;
      return `M ${source.x} ${source.y} Q ${midX + nx} ${midY + ny} ${target.x} ${target.y}`;
    }

    return `M ${source.x} ${source.y} L ${target.x} ${target.y}`;
  }

  function getEdgeOpacity(edge: GraphEdge): number {
    if (!connectedNodeIds) return 0.6;
    return connectedNodeIds.has(edge.sourceId) && connectedNodeIds.has(edge.targetId) ? 0.8 : 0.1;
  }

  function getNodeOpacity(nodeId: string): number {
    if (!connectedNodeIds) return 1;
    return connectedNodeIds.has(nodeId) ? 1 : 0.2;
  }

  const selectedNode = $derived(graph.nodes.find((n) => n.id === selectedNodeId) ?? null);

  function handleNodeSelect(node: GraphNode): void {
    selectedNodeId = node.id;
  }

  function handleOpenNode(): void {
    if (!selectedNode) return;
    onNodeClick?.(selectedNode);
    selectedNodeId = null;
  }

  function closePopover(): void {
    selectedNodeId = null;
  }

  function getEdgeDashArray(type: RelationType): string | undefined {
    const dash = EDGE_DASH_ARRAYS[EDGE_LINE_STYLES[type]];
    return dash === 'none' ? undefined : dash;
  }
</script>

<div class="relative {className}">
  <svg
    {width}
    {height}
    class="bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-700"
  >
    <defs>
      {#each Object.keys(RELATION_TYPE_STYLES) as type (type)}
        <marker
          id="static-arrow-{type}"
          viewBox="0 0 10 10"
          refX="20"
          refY="5"
          markerWidth="6"
          markerHeight="6"
          orient="auto-start-reverse"
        >
          <path d="M 0 0 L 10 5 L 0 10 z" fill={EDGE_COLORS[type as RelationType]} />
        </marker>
      {/each}
    </defs>

    <g class="edges">
      {#each graph.edges as edge (edge.id)}
        {@const path = getEdgePath(edge)}
        {@const color = EDGE_COLORS[edge.type]}
        {@const isHovered = hoveredEdgeId === edge.id}
        <g
          class="cursor-pointer"
          role="button"
          tabindex="0"
          aria-label="{edge.type} relationship"
          onclick={() => onEdgeClick?.(edge)}
          onkeydown={(e) => e.key === 'Enter' && onEdgeClick?.(edge)}
          onmouseenter={() => (hoveredEdgeId = edge.id)}
          onmouseleave={() => (hoveredEdgeId = null)}
        >
          <path
            d={path}
            fill="none"
            stroke={color}
            stroke-width={isHovered ? 3 : 2}
            stroke-opacity={getEdgeOpacity(edge)}
            stroke-dasharray={getEdgeDashArray(edge.type)}
            marker-end="url(#static-arrow-{edge.type})"
            class="transition-all"
          />
        </g>
      {/each}
    </g>

    <g class="nodes">
      {#each graph.nodes as node (node.id)}
        {@const pos = nodePositions.get(node.id)}
        {@const isRoot = node.depth === 0}
        {@const isHovered = hoveredNodeId === node.id}
        {@const color = NODE_COLORS[node.entityType] ?? '#6b7280'}
        {@const iconName = ENTITY_TYPE_STYLES[node.entityType].icon}
        {#if pos}
          <g
            class="cursor-pointer"
            style="opacity: {getNodeOpacity(node.id)}; transition: opacity 200ms;"
            transform="translate({pos.x}, {pos.y})"
            role="button"
            tabindex="0"
            aria-label={node.label}
            onclick={() => handleNodeSelect(node)}
            onkeydown={(e) => e.key === 'Enter' && handleNodeSelect(node)}
            onmouseenter={() => (hoveredNodeId = node.id)}
            onmouseleave={() => (hoveredNodeId = null)}
          >
            <circle
              r={isRoot ? 24 : 18}
              fill={isHovered || isRoot ? color : 'white'}
              stroke={color}
              stroke-width={isRoot ? 3 : 2}
              class="transition-all dark:fill-gray-800"
            />
            <foreignObject
              x={-8}
              y={-8}
              width="16"
              height="16"
              class="pointer-events-none"
            >
              <div class="w-4 h-4 flex items-center justify-center" style="color: {isHovered || isRoot ? 'white' : color}">
                <Icon name={iconName} size="xs" />
              </div>
            </foreignObject>
            <text
              y={isRoot ? 36 : 30}
              text-anchor="middle"
              class="text-[0.625rem] font-medium fill-gray-700 dark:fill-gray-300 pointer-events-none"
            >
              {truncate(node.label, 12)}
            </text>
          </g>
        {/if}
      {/each}
    </g>
  </svg>

  {#if selectedNode}
    {@const pos = nodePositions.get(selectedNode.id)}
    {#if pos}
      <div
        class="absolute z-20 bg-white dark:bg-gray-800 rounded-lg shadow-lg border border-gray-200 dark:border-gray-700 p-3 text-xs min-w-[180px]"
        style="left: {pos.x + 16}px; top: {pos.y - 12}px;"
      >
        <div class="flex items-center justify-between mb-2">
          <div class="font-medium text-gray-700 dark:text-gray-200">{selectedNode.label}</div>
          <IconButton icon="x" variant="ghost" size="sm" onclick={closePopover} title="Close" />
        </div>
        <div class="text-gray-500 dark:text-gray-400">{ENTITY_TYPE_STYLES[selectedNode.entityType].label}</div>
        {#if selectedNode.status}
          <div class="text-gray-500 dark:text-gray-400 mt-1">Status: {selectedNode.status}</div>
        {/if}
        <div class="mt-3 flex justify-end">
          <Button variant="primary" size="sm" onclick={handleOpenNode}>
            Open
          </Button>
        </div>
      </div>
    {/if}
  {/if}
</div>
