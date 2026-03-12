<!-- Section: ForceGraph -->
<script lang="ts">
  import { SvelteSet } from 'svelte/reactivity';
  import { zoom, zoomIdentity, type ZoomBehavior } from 'd3-zoom';
  import { select } from 'd3-selection';
  import { truncate } from '@sddp/shell';
  import type {
    RelationshipGraphData,
    GraphNode,
    GraphEdge,
    SimulationNode,
    SimulationLink,
    RelationType,
  } from '../../types';
  import {
    NODE_COLORS,
    EDGE_COLORS,
    EDGE_LINE_STYLES,
    EDGE_DASH_ARRAYS,
    DEFAULT_FORCE_GRAPH_CONFIG,
    toSimulationNodes,
    toSimulationLinks,
    ENTITY_TYPE_STYLES,
  } from '../../types';
  import { ForceDirectedLayout } from '../../graph';
  import ForceGraphTooltip from '../idioms/ForceGraphTooltip.svelte';
  import ForceGraphLegend from '../idioms/ForceGraphLegend.svelte';
  import { Icon, IconButton, Button } from '@sddp/ui';

  interface Props {
    graph: RelationshipGraphData;
    width?: number;
    height?: number;
    onNodeClick?: (node: GraphNode) => void;
    onEdgeClick?: (edge: GraphEdge) => void;
    class?: string;
  }

  let {
    graph,
    width = 600,
    height = 400,
    onNodeClick,
    onEdgeClick,
    class: className = '',
  }: Props = $props();

  // Simulation state
  let simNodes: SimulationNode[] = $state([]);
  let simLinks: SimulationLink[] = $state([]);
  let transform = $state({ x: 0, y: 0, k: 1 });

  // Interaction state
  let hoveredNodeId: string | null = $state(null);
  let draggedNodeId: string | null = $state(null);
  let selectedNodeId: string | null = $state(null);
  // Tooltip position derived from node coordinates + zoom transform
  const tooltipPos = $derived.by(() => {
    if (!hoveredNode) return { x: 0, y: 0 };
    return {
      x: hoveredNode.x * transform.k + transform.x,
      y: hoveredNode.y * transform.k + transform.y,
    };
  });

  const popoverPos = $derived.by(() => {
    if (!selectedNode) return { x: 0, y: 0 };
    return {
      x: selectedNode.x * transform.k + transform.x,
      y: selectedNode.y * transform.k + transform.y,
    };
  });

  // Refs
  let svgElement: SVGSVGElement | undefined = $state();
  const layout = new ForceDirectedLayout();
  let zoomBehavior: ZoomBehavior<SVGSVGElement, unknown> | null = null;

  // Connected node IDs for hover highlighting
  const connectedNodeIds = $derived.by(() => {
    if (!hoveredNodeId) return null;
    const ids = new SvelteSet<string>([hoveredNodeId]);
    for (const link of simLinks) {
      const srcId = typeof link.source === 'object' ? link.source.id : link.source;
      const tgtId = typeof link.target === 'object' ? link.target.id : link.target;
      if (srcId === hoveredNodeId) ids.add(tgtId as string);
      if (tgtId === hoveredNodeId) ids.add(srcId as string);
    }
    return ids;
  });

  // Initialize simulation when graph changes
  $effect(() => {
    if (!graph || graph.nodes.length === 0) return;

    const nodes = toSimulationNodes(graph.nodes);
    const links = toSimulationLinks(graph.edges, nodes);

    // Position root node at center
    const rootNode = nodes.find((n) => n.id === graph.rootEntityId);
    if (rootNode) {
      rootNode.fx = width / 2;
      rootNode.fy = height / 2;
    }

    layout.setCallbacks({
      onTick: (tickNodes, tickLinks) => {
        // Shallow copy to trigger Svelte reactivity
        simNodes = [...tickNodes];
        simLinks = [...tickLinks];
      },
      onEnd: () => {
        // Unpin root after simulation settles
        if (rootNode) {
          rootNode.fx = null;
          rootNode.fy = null;
        }
      },
    });

    layout.init(nodes, links, width, height, DEFAULT_FORCE_GRAPH_CONFIG);

    return () => {
      layout.destroy();
    };
  });

  // Initialize zoom behavior
  $effect(() => {
    if (!svgElement) return;

    zoomBehavior = zoom<SVGSVGElement, unknown>()
      .scaleExtent([0.3, 4])
      .on('zoom', (event) => {
        transform = {
          x: event.transform.x,
          y: event.transform.y,
          k: event.transform.k,
        };
      });

    select(svgElement).call(zoomBehavior);

    return () => {
      if (svgElement) {
        select(svgElement).on('.zoom', null);
      }
    };
  });

  // Drag handling via Svelte pointer events
  function handlePointerDown(e: PointerEvent, node: SimulationNode) {
    e.stopPropagation();
    (e.currentTarget as Element).setPointerCapture(e.pointerId);
    draggedNodeId = node.id;
    layout.dragStart(node);
  }

  function handlePointerMove(e: PointerEvent, node: SimulationNode) {
    if (draggedNodeId !== node.id) return;
    // Convert screen coords to graph coords
    const x = (e.offsetX - transform.x) / transform.k;
    const y = (e.offsetY - transform.y) / transform.k;
    layout.dragMove(node, x, y);
  }

  function handlePointerUp(e: PointerEvent, node: SimulationNode) {
    if (draggedNodeId !== node.id) return;
    (e.currentTarget as Element).releasePointerCapture(e.pointerId);
    draggedNodeId = null;
    layout.dragEnd(node);
  }

  function handleNodeHover(node: SimulationNode | null) {
    hoveredNodeId = node?.id ?? null;
  }

  function handleNodeSelect(node: SimulationNode) {
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

  function handleResetZoom() {
    if (!svgElement || !zoomBehavior) return;
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    (select(svgElement) as any).transition().duration(300).call(zoomBehavior.transform, zoomIdentity);
  }

  // Edge path helpers
  function getEdgePath(link: SimulationLink): string {
    const sx = link.source.x;
    const sy = link.source.y;
    const tx = link.target.x;
    const ty = link.target.y;
    return `M ${sx} ${sy} L ${tx} ${ty}`;
  }

  function getEdgeDashArray(type: RelationType): string | undefined {
    const dash = EDGE_DASH_ARRAYS[EDGE_LINE_STYLES[type]];
    return dash === 'none' ? undefined : dash;
  }

  function getNodeOpacity(nodeId: string): number {
    if (!connectedNodeIds) return 1;
    if (selectedNodeId === nodeId) return 1;
    return connectedNodeIds.has(nodeId) ? 1 : 0.2;
  }

  function getEdgeOpacity(link: SimulationLink): number {
    if (!connectedNodeIds) return 0.6;
    const srcId = typeof link.source === 'object' ? link.source.id : link.source;
    const tgtId = typeof link.target === 'object' ? link.target.id : link.target;
    return connectedNodeIds.has(srcId as string) && connectedNodeIds.has(tgtId as string)
      ? 0.8
      : 0.1;
  }

  const hoveredNode = $derived(simNodes.find((n) => n.id === hoveredNodeId) ?? null);
  const selectedNode = $derived(simNodes.find((n) => n.id === selectedNodeId) ?? null);
</script>

<div class="relative {className}">
  <svg
    bind:this={svgElement}
    {width}
    {height}
    class="bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-700 cursor-grab active:cursor-grabbing"
  >
    <defs>
      {#each Object.entries(EDGE_COLORS) as [type, color] (type)}
        <marker
          id="force-arrow-{type}"
          viewBox="0 0 10 10"
          refX="22"
          refY="5"
          markerWidth="6"
          markerHeight="6"
          orient="auto-start-reverse"
        >
          <path d="M 0 0 L 10 5 L 0 10 z" fill={color} />
        </marker>
      {/each}
    </defs>

    <g transform="translate({transform.x}, {transform.y}) scale({transform.k})">
      <!-- Edges -->
      <g class="edges">
        {#each simLinks as link (link.id)}
          {@const color = EDGE_COLORS[link.type]}
          <g
            class="cursor-pointer"
            role="button"
            tabindex="-1"
            aria-label="Relationship link"
            onclick={() =>
              onEdgeClick?.({
                id: link.id,
                sourceId: link.source.id,
                targetId: link.target.id,
                type: link.type,
                typeLabel: link.typeLabel,
              })}
            onkeydown={(e) => {
              if (e.key === 'Enter')
                onEdgeClick?.({
                  id: link.id,
                  sourceId: link.source.id,
                  targetId: link.target.id,
                  type: link.type,
                  typeLabel: link.typeLabel,
                });
            }}
          >
            <path
              d={getEdgePath(link)}
              fill="none"
              stroke={color}
              stroke-width="2"
              stroke-opacity={getEdgeOpacity(link)}
              stroke-dasharray={getEdgeDashArray(link.type)}
              marker-end="url(#force-arrow-{link.type})"
              class="transition-opacity duration-200"
            />
          </g>
        {/each}
      </g>

      <!-- Nodes -->
      <g class="nodes">
        {#each simNodes as node (node.id)}
          {@const color = NODE_COLORS[node.entityType] ?? '#6b7280'}
          {@const isRoot = node.id === graph.rootEntityId}
          {@const isHovered = hoveredNodeId === node.id}
          {@const isDragging = draggedNodeId === node.id}
          {@const r = isRoot ? 24 : 18}
          <g
            class="cursor-pointer"
            style="opacity: {getNodeOpacity(node.id)}; transition: opacity 200ms;"
            role="button"
            tabindex="0"
            aria-label={node.label}
            onclick={() => handleNodeSelect(node)}
            onkeydown={(e) => e.key === 'Enter' && handleNodeSelect(node)}
            onpointerdown={(e) => handlePointerDown(e, node)}
            onpointermove={(e) => handlePointerMove(e, node)}
            onpointerup={(e) => handlePointerUp(e, node)}
            onmouseenter={() => handleNodeHover(node)}
            onmouseleave={() => handleNodeHover(null)}
          >
            <!-- Node circle -->
            <circle
              cx={node.x}
              cy={node.y}
              {r}
              fill={isHovered || isRoot ? color : 'var(--node-fill, white)'}
              stroke={color}
              stroke-width={isRoot ? 3 : isDragging ? 3 : 2}
              class="dark:[--node-fill:theme(colors.gray.800)]
                     [--node-fill:white]"
            />
            <foreignObject
              x={node.x - 8}
              y={node.y - 8}
              width="16"
              height="16"
              class="pointer-events-none"
            >
              <div class="w-4 h-4 flex items-center justify-center" style="color: {isHovered || isRoot ? 'white' : color}">
                <Icon name={ENTITY_TYPE_STYLES[node.entityType].icon} size="xs" />
              </div>
            </foreignObject>
            <!-- Label below node -->
            <text
              x={node.x}
              y={node.y + r + 14}
              text-anchor="middle"
              class="text-[0.625rem] font-medium pointer-events-none select-none fill-gray-700 dark:fill-gray-300"
            >
              {truncate(node.label, 14)}
            </text>
          </g>
        {/each}
      </g>
    </g>
  </svg>

  <!-- Reset zoom button -->
  <Button
    variant="unstyled"
    class="absolute top-3 right-3 p-1.5 bg-white dark:bg-gray-800 rounded-md border border-gray-200 dark:border-gray-700 text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-300 shadow-sm text-xs"
    onclick={handleResetZoom}
    title="Reset zoom"
  >
    Reset
  </Button>

  <!-- Legend -->
  <ForceGraphLegend />

  <!-- Tooltip -->
  {#if hoveredNode && !draggedNodeId}
    <ForceGraphTooltip node={hoveredNode} x={tooltipPos.x} y={tooltipPos.y} />
  {/if}

  {#if selectedNode}
    <div
      class="absolute z-20 bg-white dark:bg-gray-800 rounded-lg shadow-lg border border-gray-200 dark:border-gray-700 p-3 text-xs min-w-[200px]"
      style="left: {popoverPos.x + 16}px; top: {popoverPos.y - 12}px;"
    >
      <div class="flex items-center justify-between mb-2">
        <div class="font-medium text-gray-700 dark:text-gray-200">{selectedNode.label}</div>
        <IconButton icon="x" variant="ghost" size="sm" onclick={closePopover} title="Close" />
      </div>
      <div class="flex items-center gap-2 text-gray-500 dark:text-gray-400">
        <Icon name={ENTITY_TYPE_STYLES[selectedNode.entityType].icon} size="xs" />
        <span>{ENTITY_TYPE_STYLES[selectedNode.entityType].label}</span>
      </div>
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
</div>
