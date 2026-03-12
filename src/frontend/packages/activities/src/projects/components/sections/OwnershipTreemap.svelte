<!-- Section: OwnershipTreemap — Projects > Dashboard -->
<script lang="ts">
  import { untrack } from 'svelte';
  import { SvelteMap } from 'svelte/reactivity';
  import { treemap, hierarchy, treemapSquarify, type HierarchyRectangularNode } from 'd3-hierarchy';
  import { Button } from '@sddp/ui';
  import { getAvatarHexColor } from '@sddp/shell';
  import type { OwnershipItem, ProjectMember } from '../../types';

  interface Props {
    items: OwnershipItem[];
    members?: ProjectMember[];
    height?: number;
    class?: string;
  }

  let {
    items,
    members: _members = [],
    height = 460,
    class: className = '',
  }: Props = $props();

  // Dynamic width from container
  let containerEl: HTMLDivElement | undefined = $state(undefined);
  let width = $state(800);

  $effect(() => {
    if (!containerEl) return;
    const observer = new ResizeObserver((entries) => {
      const entry = entries[0];
      if (entry) {
        width = Math.floor(entry.contentRect.width);
      }
    });
    observer.observe(containerEl);
    return () => observer.disconnect();
  });

  // ============================================
  // Constants
  // ============================================

  // SVG fill/stroke requires hex colors — CSS custom properties don't support
  // hex-alpha suffixes (e.g. `var(--color-info-500)30` is invalid CSS).
  const TREEMAP_ENTITY_COLORS: Record<string, string> = {
    Spec: '#3b82f6',
    Requirement: '#22c55e',
    GlossaryTerm: '#f59e0b',
    Task: '#14b8a6',
    Artifact: '#06b6d4',
  };
  const TREEMAP_DEFAULT_COLOR = '#71717a';
  const TREEMAP_UNASSIGNED_COLOR = '#9ca3af';

  const HEADER_HEIGHT = 28;

  // ============================================
  // Treemap hierarchy data
  // ============================================

  interface TreeNode {
    name: string;
    value?: number;
    children?: TreeNode[];
    entityType?: string;
    entityId?: string;
    depth?: number;
  }

  const hierarchyData = $derived.by(() => {
    // Group by owner, then by entityType
    const ownerMap = new SvelteMap<string, OwnershipItem[]>();
    for (const item of items) {
      const key = item.ownerName ?? 'Unassigned';
      const list = ownerMap.get(key) ?? [];
      list.push(item);
      ownerMap.set(key, list);
    }

    const children: TreeNode[] = [];
    for (const [ownerName, ownerItems] of ownerMap) {
      // Group by entityType
      const typeMap = new SvelteMap<string, OwnershipItem[]>();
      for (const item of ownerItems) {
        const list = typeMap.get(item.entityType) ?? [];
        list.push(item);
        typeMap.set(item.entityType, list);
      }

      const typeChildren: TreeNode[] = [];
      for (const [entityType, typeItems] of typeMap) {
        const leafChildren: TreeNode[] = typeItems.map((item) => ({
          name: item.entityName,
          value: 1,
          entityType: item.entityType,
          entityId: item.entityId,
        }));
        typeChildren.push({
          name: entityType,
          entityType,
          children: leafChildren,
        });
      }

      children.push({
        name: ownerName,
        children: typeChildren,
      });
    }

    return { name: 'Project', children } as TreeNode;
  });

  // ============================================
  // Zoom state
  // ============================================

  type TreemapNode = HierarchyRectangularNode<TreeNode>;

  let currentRoot = $state<TreemapNode | null>(null);
  let breadcrumb = $state<{ label: string; node: TreemapNode | null }[]>([]);

  // Compute treemap layout
  const treemapLayout = $derived.by(() => {
    const root = hierarchy(hierarchyData)
      .sum((d) => d.value ?? 0)
      .sort((a, b) => (b.value ?? 0) - (a.value ?? 0));

    treemap<TreeNode>()
      .size([width, height - HEADER_HEIGHT])
      .padding(2)
      .paddingTop(22)
      .round(true)
      .tile(treemapSquarify)(root);

    return root as TreemapNode;
  });

  // Initialize current view when data changes
  $effect(() => {
    const layout = treemapLayout;
    untrack(() => {
      currentRoot = layout;
      breadcrumb = [{ label: 'All Members', node: layout }];
    });
  });

  // Visible nodes: children of currentRoot
  const visibleNodes = $derived.by(() => {
    if (!currentRoot) return [];

    const target = currentRoot;
    // Recompute layout scoped to current root
    const root = hierarchy(target.data)
      .sum((d) => d.value ?? 0)
      .sort((a, b) => (b.value ?? 0) - (a.value ?? 0));

    treemap<TreeNode>()
      .size([width, height - HEADER_HEIGHT])
      .padding(2)
      .paddingTop(22)
      .round(true)
      .tile(treemapSquarify)(root);

    return (root.children ?? []) as TreemapNode[];
  });

  // ============================================
  // Interactions
  // ============================================

  let hoveredNode = $state<TreemapNode | null>(null);
  let tooltip = $state<{ x: number; y: number; text: string; sub: string } | null>(null);

  function handleNodeClick(node: TreemapNode) {
    if (!node.children || node.children.length === 0) return;

    // Find the corresponding node in the full hierarchy
    const fullNode = findNodeInTree(treemapLayout, node.data.name, breadcrumb.length);
    if (!fullNode) return;

    currentRoot = fullNode;
    breadcrumb = [...breadcrumb, { label: node.data.name, node: fullNode }];
  }

  function handleBreadcrumbClick(index: number) {
    if (index >= breadcrumb.length - 1) return;
    const target = breadcrumb[index];
    if (!target) return;
    currentRoot = target.node;
    breadcrumb = breadcrumb.slice(0, index + 1);
  }

  function findNodeInTree(
    root: TreemapNode,
    name: string,
    targetDepth: number,
  ): TreemapNode | null {
    if (root.depth === targetDepth && root.data.name === name) return root;
    if (!root.children) return null;
    for (const child of root.children) {
      const found = findNodeInTree(child, name, targetDepth);
      if (found) return found;
    }
    return null;
  }

  function handleMouseEnter(e: MouseEvent, node: TreemapNode) {
    hoveredNode = node;
    const count = node.value ?? 0;
    const entityType = node.data.entityType;
    const sub = entityType
      ? `${entityType} \u00b7 ${count} item${count !== 1 ? 's' : ''}`
      : `${count} item${count !== 1 ? 's' : ''}`;

    tooltip = {
      x: e.clientX,
      y: e.clientY,
      text: node.data.name,
      sub,
    };
  }

  function handleMouseMove(e: MouseEvent) {
    if (tooltip) {
      tooltip = { ...tooltip, x: e.clientX, y: e.clientY };
    }
  }

  function handleMouseLeave() {
    hoveredNode = null;
    tooltip = null;
  }

  // ============================================
  // Rendering helpers
  // ============================================

  function getNodeColor(node: TreemapNode): string {
    const data = node.data;
    // Leaf: use entity type color
    const entityColor = data.entityType ? TREEMAP_ENTITY_COLORS[data.entityType] : undefined;
    if (entityColor) {
      return entityColor;
    }
    // Member level: unassigned → gray, others → avatar hash color
    if (node.depth === 1 || (currentRoot && currentRoot.depth === 0)) {
      return data.name === 'Unassigned' ? TREEMAP_UNASSIGNED_COLOR : getAvatarHexColor(data.name);
    }
    return TREEMAP_DEFAULT_COLOR;
  }

  function getNodeFill(node: TreemapNode): string {
    const color = getNodeColor(node);
    // Children → darker fill, leaf → lighter fill
    if (node.children && node.children.length > 0) {
      return color + '30'; // 19% opacity
    }
    return color + '22'; // 13% opacity
  }

  function getNodeStroke(node: TreemapNode, isHovered: boolean): string {
    const color = getNodeColor(node);
    return isHovered ? color : color + '60';
  }

  function getTextColor(node: TreemapNode): string {
    const color = getNodeColor(node);
    return color;
  }

  function getNodeWidth(node: TreemapNode): number {
    return Math.max(0, (node.x1 ?? 0) - (node.x0 ?? 0));
  }

  function getNodeHeight(node: TreemapNode): number {
    return Math.max(0, (node.y1 ?? 0) - (node.y0 ?? 0));
  }

  function shouldShowLabel(node: TreemapNode): boolean {
    return getNodeWidth(node) > 40 && getNodeHeight(node) > 18;
  }

  function truncateLabel(text: string, maxWidth: number): string {
    const maxChars = Math.floor(maxWidth / 7);
    if (text.length <= maxChars) return text;
    return text.slice(0, Math.max(0, maxChars - 1)) + '\u2026';
  }

  function shouldShowCount(node: TreemapNode): boolean {
    return getNodeWidth(node) > 50 && getNodeHeight(node) > 32;
  }
</script>

<div class="relative {className}" bind:this={containerEl}>
  <!-- Breadcrumb -->
  <div
    class="flex items-center gap-1 px-2 text-xs"
    style="height: {HEADER_HEIGHT}px;"
  >
    {#each breadcrumb as crumb, i (i)}
      {#if i > 0}
        <span class="text-[var(--color-text-tertiary)]">/</span>
      {/if}
      {#if i < breadcrumb.length - 1}
        <Button
          variant="ghost"
          size="sm"
          class="!px-1 !py-0 text-[var(--color-accent-primary)] hover:underline"
          onclick={() => handleBreadcrumbClick(i)}
        >
          {crumb.label}
        </Button>
      {:else}
        <span class="text-[var(--color-text-secondary)] font-medium">{crumb.label}</span>
      {/if}
    {/each}
  </div>

  <!-- Treemap SVG -->
  <svg
    {width}
    height={height - HEADER_HEIGHT}
    class="rounded-lg border border-[var(--color-border)] bg-[var(--color-bg-secondary)]"
  >
    {#each visibleNodes as node (node.data.name + '-' + node.depth)}
      {@const nodeW = getNodeWidth(node)}
      {@const nodeH = getNodeHeight(node)}
      {@const isHovered = hoveredNode === node}
      {@const hasChildren = !!(node.children && node.children.length > 0)}
      <!-- svelte-ignore a11y_no_noninteractive_tabindex -->
      <g
        transform="translate({node.x0}, {node.y0})"
        class={hasChildren ? 'cursor-pointer' : 'cursor-default'}
        role={hasChildren ? 'button' : 'img'}
        tabindex={hasChildren ? 0 : -1}
        onclick={() => handleNodeClick(node)}
        onkeydown={(e) => e.key === 'Enter' && handleNodeClick(node)}
        onmouseenter={(e) => handleMouseEnter(e, node)}
        onmousemove={handleMouseMove}
        onmouseleave={handleMouseLeave}
      >
        <!-- Background rect -->
        <rect
          width={nodeW}
          height={nodeH}
          rx="3"
          fill={getNodeFill(node)}
          stroke={getNodeStroke(node, isHovered)}
          stroke-width={isHovered ? 2 : 1}
          class="transition-[fill,stroke,stroke-width] duration-150"
        />

        <!-- Label -->
        {#if shouldShowLabel(node)}
          <text
            x="6"
            y="14"
            class="text-[0.6875rem] font-medium pointer-events-none select-none"
            fill={getTextColor(node)}
          >
            {truncateLabel(node.data.name, nodeW - 12)}
          </text>
        {/if}

        <!-- Count badge -->
        {#if shouldShowCount(node)}
          <text
            x="6"
            y="26"
            class="text-[0.5625rem] pointer-events-none select-none"
            fill={getTextColor(node)}
            opacity="0.7"
          >
            {node.value}
          </text>
        {/if}
      </g>
    {/each}
  </svg>

  <!-- Tooltip -->
  {#if tooltip}
    <div
      class="fixed z-50 pointer-events-none px-2.5 py-1.5 rounded-md shadow-lg text-xs
             bg-[var(--color-bg-primary)] border border-[var(--color-border)]"
      style="left: {tooltip.x + 12}px; top: {tooltip.y - 8}px;"
    >
      <div class="font-medium text-[var(--color-text-primary)]">{tooltip.text}</div>
      <div class="text-[var(--color-text-tertiary)]">{tooltip.sub}</div>
    </div>
  {/if}

  <!-- Entity type legend -->
  <div class="flex flex-wrap gap-3 mt-2 px-1">
    {#each Object.entries(TREEMAP_ENTITY_COLORS) as [type, color] (type)}
      <div class="flex items-center gap-1.5 text-[0.625rem] text-[var(--color-text-tertiary)]">
        <span
          class="inline-block w-2.5 h-2.5 rounded-sm"
          style="background-color: {color}40; border: 1px solid {color}80;"
        ></span>
        {type}
      </div>
    {/each}
  </div>
</div>
