<!-- Section: TangledTreeGraph — Projects > Traceability -->
<script lang="ts">
  import { SvelteSet } from 'svelte/reactivity';
  import { zoom, zoomIdentity, type ZoomBehavior } from 'd3-zoom';
  import { select } from 'd3-selection';
  import type { TraceabilityNode, TraceabilityCrossLink, PositionedNode, TangledTreeLayoutResult } from '../../types';
  import type { EntityType, RelationType } from '../../types';
  import { NODE_COLORS, EDGE_COLORS } from '../../types';
  import { computeTangledTreeLayout, getVisibleNodes, buildChildrenMap } from '../../graph';
  import TangledTreeTooltip from '../idioms/TangledTreeTooltip.svelte';
  import { Button } from '@sddp/ui';

  interface Props {
    nodes: TraceabilityNode[];
    crossLinks: TraceabilityCrossLink[];
    width?: number;
    height?: number;
    orientation?: 'horizontal' | 'vertical';
    showCrossLinks?: boolean;
    showLabels?: boolean;
    filterEntityTypes?: Set<EntityType>;
    selectedNodeId?: string | null;
    onNodeClick?: (nodeId: string, entityType: EntityType) => void;
    class?: string;
  }

  let {
    nodes,
    crossLinks,
    width = 800,
    height = 500,
    orientation = 'horizontal',
    showCrossLinks = true,
    showLabels = true,
    filterEntityTypes,
    selectedNodeId = null,
    onNodeClick,
    class: className = '',
  }: Props = $props();

  // SVG ref + zoom transform state
  let svgElement: SVGSVGElement | undefined = $state();
  let transform = $state({ x: 0, y: 0, k: 1 });
  let zoomBehavior: ZoomBehavior<SVGSVGElement, unknown> | null = null;

  // Hover + tooltip state
  let hoveredNodeId = $state<string | null>(null);
  let tooltipX = $state(0);
  let tooltipY = $state(0);

  // Collapse/expand state
  let collapsedNodeIds = new SvelteSet<string>();

  // Children map for all original nodes (unfiltered)
  const childrenMap = $derived(buildChildrenMap(nodes));

  // Step 1: Filter by entity type
  const entityFilteredNodes = $derived.by((): TraceabilityNode[] => {
    if (!filterEntityTypes || filterEntityTypes.size === 0) return nodes;
    return nodes.filter((n) => filterEntityTypes.has(n.entityType));
  });

  // Step 2: Filter by collapse state
  const { visibleNodes: filteredNodes, hiddenChildrenCounts } = $derived.by(() => {
    return getVisibleNodes(entityFilteredNodes, collapsedNodeIds);
  });

  // Step 3: Filter cross-links
  const filteredCrossLinks = $derived.by((): TraceabilityCrossLink[] => {
    if (!showCrossLinks) return [];
    const visibleIds = new Set(filteredNodes.map((n) => n.id));
    return crossLinks.filter((cl) => visibleIds.has(cl.sourceId) && visibleIds.has(cl.targetId));
  });

  // Step 4: Compute bundled layout
  const layout = $derived.by((): TangledTreeLayoutResult => {
    return computeTangledTreeLayout(filteredNodes, filteredCrossLinks, width, height, orientation);
  });

  // Hover highlighting: collect connected node IDs
  const connectedNodeIds = $derived.by((): SvelteSet<string> | null => {
    if (!hoveredNodeId) return null;
    const ids = new SvelteSet<string>([hoveredNodeId]);
    for (const edge of layout.bundledEdges) {
      if (edge.sourceId === hoveredNodeId) ids.add(edge.targetId);
      if (edge.targetId === hoveredNodeId) ids.add(edge.sourceId);
    }
    for (const edge of layout.hierarchyEdges) {
      if (edge.sourceId === hoveredNodeId) ids.add(edge.targetId);
      if (edge.targetId === hoveredNodeId) ids.add(edge.sourceId);
    }
    for (const cl of layout.crossLinkPaths) {
      if (cl.sourceId === hoveredNodeId) ids.add(cl.targetId);
      if (cl.targetId === hoveredNodeId) ids.add(cl.sourceId);
    }
    return ids;
  });

  // Hovered node for tooltip
  const hoveredNode = $derived(
    hoveredNodeId ? (layout.nodes.find((n) => n.id === hoveredNodeId) ?? null) : null,
  );

  // Compute node mark height based on bundles (for metro-style node marks)
  // Nodes with more bundle lines passing through get taller marks (like Nitaku)
  function getNodeMarkHeight(node: PositionedNode): number {
    // bundleHeight = (bundles - 1) * metroD from the layout engine
    // Minimum mark height of 4px, scales with bundle connections
    return Math.max(4, node.bundleHeight / 2 + 4);
  }

  // Initial transform per orientation
  const getInitialTransform = (o: 'horizontal' | 'vertical') =>
    o === 'horizontal' ? zoomIdentity.translate(40, -60) : zoomIdentity.translate(-220, 30);

  // Initialize zoom behavior
  $effect(() => {
    if (!svgElement) return;
    zoomBehavior = zoom<SVGSVGElement, unknown>()
      .scaleExtent([0.3, 4])
      .on('zoom', (event) => {
        transform = { x: event.transform.x, y: event.transform.y, k: event.transform.k };
      });
    select(svgElement).call(zoomBehavior);
    // Apply initial transform
    select(svgElement).call(zoomBehavior.transform, getInitialTransform(orientation));
    return () => {
      if (svgElement) select(svgElement).on('.zoom', null);
    };
  });

  // Reset zoom when orientation changes
  let prevOrientation: string | null = null;
  $effect(() => {
    if (prevOrientation !== null && orientation !== prevOrientation) {
      if (svgElement && zoomBehavior) {
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        (select(svgElement) as any).transition().duration(300).call(zoomBehavior.transform, getInitialTransform(orientation));
      }
    }
    prevOrientation = orientation;
  });

  function handleResetZoom() {
    if (!svgElement || !zoomBehavior) return;
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    (select(svgElement) as any).transition().duration(300).call(zoomBehavior.transform, getInitialTransform(orientation));
  }

  function handleNodeClick(node: PositionedNode) {
    onNodeClick?.(node.id, node.entityType);
  }

  function handleMouseMove(e: MouseEvent) {
    tooltipX = e.offsetX;
    tooltipY = e.offsetY;
  }

  function toggleCollapse(nodeId: string) {
    if (collapsedNodeIds.has(nodeId)) {
      collapsedNodeIds.delete(nodeId);
    } else {
      collapsedNodeIds.add(nodeId);
    }
  }

  function getNodeColor(entityType: EntityType): string {
    return NODE_COLORS[entityType] ?? '#6b7280';
  }

  function getCrossLinkColor(type: RelationType): string {
    return EDGE_COLORS[type] ?? '#6b7280';
  }

  function getNodeOpacity(nodeId: string): number {
    if (!connectedNodeIds) return 1;
    return connectedNodeIds.has(nodeId) ? 1 : 0.15;
  }

  function getEdgeOpacity(sourceId: string, targetId: string): number {
    if (!connectedNodeIds) return 0.7;
    return connectedNodeIds.has(sourceId) && connectedNodeIds.has(targetId) ? 0.85 : 0.06;
  }

  function getCrossLinkOpacity(sourceId: string, targetId: string): number {
    if (!connectedNodeIds) return 0.5;
    return connectedNodeIds.has(sourceId) && connectedNodeIds.has(targetId) ? 0.85 : 0.06;
  }

  function getNodeLabel(node: PositionedNode): string {
    return node.label;
  }

  /** Convert hex color to rgba with alpha (e.g. '#3b82f6' → 'rgba(59,130,246,0.3)') */
  function hexToRgba(hex: string, alpha: number): string {
    const r = parseInt(hex.slice(1, 3), 16);
    const g = parseInt(hex.slice(3, 5), 16);
    const b = parseInt(hex.slice(5, 7), 16);
    return `rgba(${r},${g},${b},${alpha})`;
  }
</script>

<div class="relative w-full h-full {className}">
  <!-- svelte-ignore a11y_no_static_element_interactions -->
  <svg
    bind:this={svgElement}
    role="img"
    aria-label="Tangled tree traceability graph"
    class="w-full h-full bg-[var(--color-bg-primary)] border-b border-[var(--color-border-primary)] cursor-grab active:cursor-grabbing"
    onmousemove={handleMouseMove}
  >
    <defs>
      {#each Object.entries(EDGE_COLORS) as [type, color] (type)}
        <marker
          id="tangled-arrow-{type}"
          viewBox="0 0 10 10"
          refX="20"
          refY="5"
          markerWidth="5"
          markerHeight="5"
          orient="auto-start-reverse"
        >
          <path d="M 0 0 L 10 5 L 0 10 z" fill={color} />
        </marker>
      {/each}
    </defs>

    <g transform="translate({transform.x}, {transform.y}) scale({transform.k})">
      <!-- Layer 1: Bundled hierarchy edges (Nitaku double-stroke metro style) -->
      {#if layout.bundledEdges.length > 0}
        <g class="bundled-edges">
          {#each layout.bundledEdges as edge (`${edge.sourceId}-${edge.targetId}-${edge.bundleId}`)}
            <!-- White/bg background stroke for clean visual separation -->
            <path
              d={edge.path}
              fill="none"
              stroke="var(--color-bg-primary, white)"
              stroke-width="5"
              opacity={getEdgeOpacity(edge.sourceId, edge.targetId)}
              class="transition-opacity duration-150"
            />
            <!-- Colored foreground stroke -->
            <path
              d={edge.path}
              fill="none"
              stroke={edge.color}
              stroke-width="2"
              opacity={getEdgeOpacity(edge.sourceId, edge.targetId)}
              class="transition-opacity duration-150"
            />
          {/each}
        </g>
      {:else}
        <!-- Fallback: Legacy hierarchy edges -->
        <g class="hierarchy-edges">
          {#each layout.hierarchyEdges as edge (`${edge.sourceId}-${edge.targetId}`)}
            <path
              d={edge.path}
              fill="none"
              stroke="#9ca3af"
              stroke-width="1.5"
              opacity={getEdgeOpacity(edge.sourceId, edge.targetId)}
              class="transition-opacity duration-150"
            />
          {/each}
        </g>
      {/if}

      <!-- Layer 2: Cross-links (curved dashed lines) -->
      {#if showCrossLinks}
        <g class="cross-links">
          {#each layout.crossLinkPaths as cl (cl.id)}
            {@const clColor = getCrossLinkColor(cl.type)}
            <path
              d={cl.path}
              fill="none"
              stroke={clColor}
              stroke-width="1.5"
              stroke-dasharray="4,3"
              opacity={getCrossLinkOpacity(cl.sourceId, cl.targetId)}
              marker-end="url(#tangled-arrow-{cl.type})"
              class="transition-opacity duration-150"
            />
          {/each}
        </g>
      {/if}

      <!-- Layer 3: Nodes (Nitaku-style: vertical mark + text) -->
      <g class="nodes">
        {#each layout.nodes as node (node.id)}
          {@const nodeColor = getNodeColor(node.entityType)}
          {@const nodeFill = hexToRgba(nodeColor, 0.15)}
          {@const isHovered = hoveredNodeId === node.id}
          {@const isSelected = selectedNodeId === node.id}
          {@const hasChildren = childrenMap.has(node.id)}
          {@const isCollapsed = collapsedNodeIds.has(node.id)}
          {@const hiddenCount = hiddenChildrenCounts.get(node.id) ?? 0}
          {@const markH = getNodeMarkHeight(node)}
          <g
            style="opacity: {getNodeOpacity(node.id)}; transition: opacity 150ms;"
            onmouseenter={() => (hoveredNodeId = node.id)}
            onmouseleave={() => (hoveredNodeId = null)}
          >
            <!-- Node shape: click → collapse/expand (or select if leaf) -->
            <g
              class="cursor-pointer"
              role="button"
              tabindex="0"
              aria-label={node.label}
              onclick={() => {
                if (hasChildren) {
                  toggleCollapse(node.id);
                } else {
                  handleNodeClick(node);
                }
              }}
              onkeydown={(e) => {
                if (e.key === 'Enter') {
                  if (hasChildren) {
                    toggleCollapse(node.id);
                  } else {
                    handleNodeClick(node);
                  }
                }
              }}
            >
              {#if markH <= 4}
                <!-- Leaf node: opaque bg circle + colored fill -->
                {#if isSelected}
                  <circle cx={node.x} cy={node.y} r="10" fill={nodeColor} opacity="0.15" />
                {/if}
                <circle
                  cx={node.x}
                  cy={node.y}
                  r={isHovered || isSelected ? 6 : 5}
                  fill="var(--color-bg-primary, white)"
                />
                <circle
                  cx={node.x}
                  cy={node.y}
                  r={isHovered || isSelected ? 6 : 5}
                  fill={nodeFill}
                  stroke={nodeColor}
                  stroke-width="1.5"
                />
                {#if hasChildren}
                  <text
                    x={node.x}
                    y={node.y + 3}
                    text-anchor="middle"
                    class="text-[7px] font-bold pointer-events-none select-none"
                    fill={nodeColor}
                  >
                    {isCollapsed ? hiddenCount : '−'}
                  </text>
                {/if}
              {:else}
                <!-- Parent node: opaque bg mark + colored mark -->
                {#if isSelected}
                  <rect
                    x={node.x - 7}
                    y={node.y - markH - 5}
                    width="14"
                    height={markH * 2 + 10}
                    rx="4"
                    fill={nodeColor}
                    opacity="0.15"
                  />
                {/if}
                <path
                  d="M {node.x} {node.y - markH} L {node.x} {node.y + markH}"
                  stroke="var(--color-bg-primary, white)"
                  stroke-width={isHovered || isSelected ? 12 : 10}
                  stroke-linecap="round"
                />
                <path
                  d="M {node.x} {node.y - markH} L {node.x} {node.y + markH}"
                  stroke={nodeFill}
                  stroke-width={isHovered || isSelected ? 10 : 8}
                  stroke-linecap="round"
                />
                <path
                  d="M {node.x} {node.y - markH} L {node.x} {node.y + markH}"
                  stroke={nodeColor}
                  stroke-width="1.5"
                  stroke-linecap="round"
                />
                {#if hasChildren}
                  <text
                    x={node.x}
                    y={node.y + 3}
                    text-anchor="middle"
                    class="text-[8px] font-bold pointer-events-none select-none"
                    fill={nodeColor}
                  >
                    {isCollapsed ? hiddenCount : '−'}
                  </text>
                {/if}
              {/if}
            </g>

            <!-- Text label: click → select (open detail panel) -->
            {#if showLabels}
              {#if orientation === 'vertical'}
                {@const textY = markH <= 4 ? node.y + 16 : node.y + markH + 12}
                <text
                  x={node.x}
                  y={textY}
                  text-anchor="middle"
                  class="text-[0.625rem] select-none"
                  stroke="var(--color-bg-primary, white)"
                  stroke-width="3"
                  fill="var(--color-bg-primary, white)"
                >
                  {getNodeLabel(node)}
                </text>
                <!-- svelte-ignore a11y_click_events_have_key_events a11y_no_static_element_interactions -->
                <text
                  x={node.x}
                  y={textY}
                  text-anchor="middle"
                  class="text-[0.625rem] select-none cursor-pointer"
                  fill={isHovered || isSelected ? nodeColor : 'var(--color-text-primary, #1f2937)'}
                  font-weight={isSelected ? 'bold' : 'normal'}
                  onclick={() => handleNodeClick(node)}
                >
                  {getNodeLabel(node)}
                </text>
              {:else}
                {@const textY = markH <= 4 ? node.y - 4 : node.y - markH}
                <text
                  x={node.x + 8}
                  y={textY}
                  class="text-[0.625rem] select-none"
                  stroke="var(--color-bg-primary, white)"
                  stroke-width="3"
                  fill="var(--color-bg-primary, white)"
                >
                  {getNodeLabel(node)}
                </text>
                <!-- svelte-ignore a11y_click_events_have_key_events a11y_no_static_element_interactions -->
                <text
                  x={node.x + 8}
                  y={textY}
                  class="text-[0.625rem] select-none cursor-pointer"
                  fill={isHovered || isSelected ? nodeColor : 'var(--color-text-primary, #1f2937)'}
                  font-weight={isSelected ? 'bold' : 'normal'}
                  onclick={() => handleNodeClick(node)}
                >
                  {getNodeLabel(node)}
                </text>
              {/if}
            {/if}

          </g>
        {/each}
      </g>

    </g>
  </svg>

  <!-- Reset zoom button -->
  <Button
    variant="unstyled"
    class="absolute top-3 right-3 p-1.5 bg-[var(--color-bg-primary)] rounded-md border border-[var(--color-border-primary)] text-[var(--color-text-tertiary)] hover:text-[var(--color-text-primary)] shadow-sm text-xs"
    onclick={handleResetZoom}
    title="Reset zoom"
  >
    Reset
  </Button>

  <!-- Tooltip -->
  {#if hoveredNode}
    <TangledTreeTooltip node={hoveredNode} x={tooltipX} y={tooltipY} />
  {/if}
</div>
