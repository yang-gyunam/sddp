<!-- Section: RelationshipGraph -->
<script lang="ts">
  import { SvelteMap, SvelteSet } from 'svelte/reactivity';
  import type { RelationshipGraphData, GraphNode, GraphEdge, EntityType, RelationType, GraphLayoutMode } from '../../types';
  import {
    DEFAULT_GRAPH_FILTERS,
    ENTITY_TYPE_STYLES,
    RELATION_TYPE_STYLES,
  } from '../../types';
  import ForceGraph from './ForceGraph.svelte';
  import StaticGraph from './StaticGraph.svelte';
  import { Icon, Button, Checkbox } from '@sddp/ui';

  interface Props {
    graph: RelationshipGraphData;
    width?: number;
    height?: number;
    showControls?: boolean;
    onNodeClick?: (node: GraphNode) => void;
    onEdgeClick?: (edge: GraphEdge) => void;
    class?: string;
  }

  let {
    graph,
    width = 640,
    height = 360,
    showControls = true,
    onNodeClick,
    onEdgeClick,
    class: className = '',
  }: Props = $props();

  let layoutMode = $state<GraphLayoutMode>('force');
  let clusterEnabled = $state(true);
  let entityFilters = new SvelteSet<EntityType>([...DEFAULT_GRAPH_FILTERS.entityTypes]);
  let relationFilters = new SvelteSet<RelationType>([...DEFAULT_GRAPH_FILTERS.relationTypes]);

  const entityTypes = Object.keys(ENTITY_TYPE_STYLES) as EntityType[];
  const relationTypes = Object.keys(RELATION_TYPE_STYLES) as RelationType[];

  function toggleEntity(type: EntityType): void {
    if (entityFilters.has(type)) {
      entityFilters.delete(type);
    } else {
      entityFilters.add(type);
    }
  }

  function toggleRelation(type: RelationType): void {
    if (relationFilters.has(type)) {
      relationFilters.delete(type);
    } else {
      relationFilters.add(type);
    }
  }

  function clusterGraph(data: RelationshipGraphData): RelationshipGraphData {
    const threshold = 70;
    if (!clusterEnabled || data.nodes.length <= threshold) return data;

    const rootNode = data.nodes.find((node) => node.id === data.rootEntityId) ?? data.nodes[0];
    const retained = data.nodes.filter((node) => node.depth <= 1 && node.id !== rootNode?.id);
    const clusteredNodes = data.nodes.filter((node) => node.depth > 1);

    const clusterNodes: GraphNode[] = [];
    const clusterEdges: GraphEdge[] = [];
    const byType = new SvelteMap<EntityType, GraphNode[]>();
    clusteredNodes.forEach((node) => {
      if (!byType.has(node.entityType)) byType.set(node.entityType, []);
      byType.get(node.entityType)!.push(node);
    });

    for (const [type, nodes] of byType.entries()) {
      const clusterId = `cluster-${type}`;
      clusterNodes.push({
        id: clusterId,
        entityType: type,
        label: `${ENTITY_TYPE_STYLES[type].label} (${nodes.length})`,
        status: 'Cluster',
        depth: 2,
      });

      if (rootNode) {
        clusterEdges.push({
          id: `cluster-edge-${clusterId}`,
          sourceId: rootNode.id,
          targetId: clusterId,
          type: 'Affects',
          typeLabel: 'Cluster',
          strength: 1,
        });
      }
    }

    const baseNodes = rootNode ? [rootNode, ...retained] : retained;
    return {
      ...data,
      nodes: [...baseNodes, ...clusterNodes],
      edges: [...data.edges.filter((edge) =>
        baseNodes.some((node) => node.id === edge.sourceId || node.id === edge.targetId)
      ), ...clusterEdges],
    };
  }

  const filteredGraph = $derived.by(() => {
    if (!graph) return null;
    const clustered = clusterGraph(graph);
    const allowedEntityTypes = new Set(entityFilters);
    const allowedRelationTypes = new Set(relationFilters);

    const nodes = clustered.nodes.filter((node) =>
      node.id === clustered.rootEntityId || allowedEntityTypes.has(node.entityType)
    );
    const nodeIds = new Set(nodes.map((node) => node.id));
    const edges = clustered.edges.filter(
      (edge) =>
        allowedRelationTypes.has(edge.type) &&
        nodeIds.has(edge.sourceId) &&
        nodeIds.has(edge.targetId)
    );

    return {
      ...clustered,
      nodes,
      edges,
    } satisfies RelationshipGraphData;
  });
</script>

<div class="flex flex-col gap-3 {className}">
  {#if showControls}
    <div class="flex flex-wrap gap-3 text-xs items-start">
      <div class="flex items-center gap-2">
        <span class="text-[var(--color-text-secondary)] font-medium">Layout</span>
        <div class="flex items-center gap-1">
          {#each ['force', 'tree', 'radial'] as mode (mode)}
            <Button
              variant="unstyled"
              class="px-2 py-1 rounded border text-[var(--color-text-secondary)]
                {layoutMode === mode
                  ? 'border-[var(--color-accent-primary)] bg-[var(--color-accent-primary)]/10'
                  : 'border-[var(--color-border-secondary)]'}"
              onclick={() => (layoutMode = mode as GraphLayoutMode)}
            >
              {mode}
            </Button>
          {/each}
        </div>
      </div>

      <div class="flex items-center gap-2">
        <Checkbox bind:checked={clusterEnabled} label="Cluster large graphs" />
      </div>

      <div class="flex items-center gap-2 flex-wrap">
        <span class="text-[var(--color-text-secondary)] font-medium">Entities</span>
        {#each entityTypes as type (type)}
          {@const style = ENTITY_TYPE_STYLES[type]}
          <Button
            variant="unstyled"
            class="px-2 py-1 rounded border text-[var(--color-text-secondary)] flex items-center gap-1
              {entityFilters.has(type)
                ? 'border-[var(--color-accent-primary)] bg-[var(--color-accent-primary)]/10'
                : 'border-[var(--color-border-secondary)] opacity-60'}"
            onclick={() => toggleEntity(type)}
          >
            <Icon name={style.icon} size="xs" />
            {style.label}
          </Button>
        {/each}
      </div>

      <div class="flex items-center gap-2 flex-wrap">
        <span class="text-[var(--color-text-secondary)] font-medium">Relations</span>
        {#each relationTypes as type (type)}
          {@const style = RELATION_TYPE_STYLES[type]}
          <Button
            variant="unstyled"
            class="px-2 py-1 rounded border text-[var(--color-text-secondary)] flex items-center gap-1
              {relationFilters.has(type)
                ? 'border-[var(--color-accent-primary)] bg-[var(--color-accent-primary)]/10'
                : 'border-[var(--color-border-secondary)] opacity-60'}"
            onclick={() => toggleRelation(type)}
          >
            <Icon name={style.icon} size="xs" />
            {style.label}
          </Button>
        {/each}
      </div>
    </div>
  {/if}

  {#if filteredGraph && filteredGraph.nodes.length > 0}
    {#if layoutMode === 'force'}
      <ForceGraph {width} {height} graph={filteredGraph} {onNodeClick} {onEdgeClick} />
    {:else}
      <StaticGraph
        {width}
        {height}
        graph={filteredGraph}
        layout={layoutMode === 'tree' ? 'tree' : 'radial'}
        {onNodeClick}
        {onEdgeClick}
      />
    {/if}
  {:else}
    <div class="text-xs text-[var(--color-text-tertiary)]">
      No graph data available for the selected filters.
    </div>
  {/if}
</div>
