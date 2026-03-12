/**
 * Tangled Tree Layout Engine
 *
 * Bundled layout algorithm ported from Nitaku's "Tangled Tree Visualization II"
 * (Observable HQ, MIT License, Matteo Abrate).
 *
 * Produces metro-style routed edges with arc corners and bundle grouping
 * for nodes sharing the same parent set.
 */

import type {
  TraceabilityNode,
  TraceabilityCrossLink,
  PositionedNode,
  CrossLinkPath,
  TangledTreeLayoutResult,
  TangledTreeLayoutConfig,
  BundledEdge,
} from '../types';
import { NODE_COLORS } from '../types';
import { DEFAULT_TANGLED_TREE_CONFIG } from '../types';

// ============================================
// Internal types for Nitaku algorithm
// ============================================

interface TangleNode {
  id: string;
  level: number;
  parents: TangleNode[];
  x: number;
  y: number;
  height: number;
  bundles: TangleBundle[][];
  bundlesIndex: Record<string, TangleBundle[]>;
  bundle?: TangleBundle;
  data: TraceabilityNode;
}

interface TangleBundle {
  id: string;
  parents: TangleNode[];
  level: number;
  span: number;
  i: number;
  x: number;
  y: number;
  links: TangleLink[];
}

interface TangleLink {
  source: TangleNode;
  target: TangleNode;
  bundle: TangleBundle;
  xt: number;
  yt: number;
  xb: number;
  yb: number;
  xs: number;
  ys: number;
  c1: number;
  c2: number;
}

interface LevelData {
  nodes: TangleNode[];
  bundles: TangleBundle[];
}

// ============================================
// Public API
// ============================================

/**
 * Compute a tangled tree layout with Nitaku-style bundling.
 */
export function computeTangledTreeLayout(
  nodes: TraceabilityNode[],
  crossLinks: TraceabilityCrossLink[],
  _width: number,
  _height: number,
  orientation: 'horizontal' | 'vertical' = 'horizontal',
  config: TangledTreeLayoutConfig = DEFAULT_TANGLED_TREE_CONFIG,
): TangledTreeLayoutResult {
  const empty: TangledTreeLayoutResult = {
    nodes: [],
    hierarchyEdges: [],
    crossLinkPaths: [],
    bundledEdges: [],
    layoutWidth: 0,
    layoutHeight: 0,
  };
  if (nodes.length === 0) return empty;

  try {
    // 1. Convert flat nodes to levels grouped by layer
    const levels = convertToLevels(nodes);
    if (levels.length === 0) return empty;

    // 2. Run bundled layout (swap nodeWidth/nodeHeight for vertical orientation)
    const layoutConfig = orientation === 'vertical'
      ? { ...config, nodeWidth: config.nodeHeight, nodeHeight: 200 }
      : config;
    const result = constructBundledLayout(levels, layoutConfig);

    // 3. Convert to output format, applying orientation swap
    const positionedNodes: PositionedNode[] = result.nodes.map((n) => {
      const pos = applyOrientation(n.x, n.y, orientation);
      return {
        id: n.id,
        entityType: n.data.entityType,
        label: n.data.label,
        code: n.data.code,
        status: n.data.status,
        layer: n.data.layer,
        x: pos.x,
        y: pos.y,
        bundleHeight: n.height,
      };
    });

    // 4. Build bundled edges with metro-style paths
    const bundledEdges: BundledEdge[] = result.links.map((link) => {
      const path = buildMetroPath(link, orientation);
      const parentType = link.target.data.entityType;
      const color = NODE_COLORS[parentType] ?? '#9ca3af';
      return {
        sourceId: link.source.id,
        targetId: link.target.id,
        path,
        bundleId: link.bundle.id,
        color,
      };
    });

    // 5. Build cross-link paths (unchanged approach)
    const nodeMap = new Map(positionedNodes.map((n) => [n.id, n]));
    const crossLinkPaths: CrossLinkPath[] = [];
    for (const cl of crossLinks) {
      const source = nodeMap.get(cl.sourceId);
      const target = nodeMap.get(cl.targetId);
      if (!source || !target) continue;
      crossLinkPaths.push({
        id: cl.id,
        sourceId: cl.sourceId,
        targetId: cl.targetId,
        type: cl.type,
        typeLabel: cl.typeLabel,
        path: computeCrossLinkPath(source, target, orientation),
      });
    }

    // 6. Compute layout bounds (natural = horizontal: width=depth, height=rows)
    const lw =
      orientation === 'horizontal' ? result.layoutWidth : result.layoutHeight;
    const lh =
      orientation === 'horizontal' ? result.layoutHeight : result.layoutWidth;

    return {
      nodes: positionedNodes,
      hierarchyEdges: [],
      crossLinkPaths,
      bundledEdges,
      layoutWidth: lw,
      layoutHeight: lh,
    };
  } catch {
    return flatFallback(nodes, crossLinks, _width, _height);
  }
}

/**
 * Filter out descendants of collapsed nodes.
 */
export function getVisibleNodes(
  nodes: TraceabilityNode[],
  collapsedNodeIds: Set<string>,
): { visibleNodes: TraceabilityNode[]; hiddenChildrenCounts: Map<string, number> } {
  if (collapsedNodeIds.size === 0) {
    return { visibleNodes: nodes, hiddenChildrenCounts: new Map() };
  }

  const childrenMap = new Map<string, string[]>();
  for (const n of nodes) {
    if (n.parentId) {
      if (!childrenMap.has(n.parentId)) childrenMap.set(n.parentId, []);
      childrenMap.get(n.parentId)!.push(n.id);
    }
  }

  const hiddenIds = new Set<string>();
  const hiddenChildrenCounts = new Map<string, number>();

  for (const collapsedId of collapsedNodeIds) {
    const directChildren = childrenMap.get(collapsedId) ?? [];
    // Hide all descendants (BFS)
    const queue = [...directChildren];
    const visited = new Set<string>();
    for (let i = 0; i < queue.length; i++) {
      const childId = queue[i]!;
      if (visited.has(childId)) continue;
      visited.add(childId);
      hiddenIds.add(childId);
      const grandChildren = childrenMap.get(childId) ?? [];
      queue.push(...grandChildren);
    }
    // Count only direct children
    hiddenChildrenCounts.set(collapsedId, directChildren.length);
  }

  return {
    visibleNodes: nodes.filter((n) => !hiddenIds.has(n.id)),
    hiddenChildrenCounts,
  };
}

/**
 * Build a map of nodeId → direct children IDs.
 */
export function buildChildrenMap(nodes: TraceabilityNode[]): Map<string, string[]> {
  const map = new Map<string, string[]>();
  for (const n of nodes) {
    if (n.parentId) {
      if (!map.has(n.parentId)) map.set(n.parentId, []);
      map.get(n.parentId)!.push(n.id);
    }
  }
  return map;
}

// ============================================
// Nitaku Bundled Layout Algorithm
// ============================================

/**
 * Convert flat TraceabilityNode[] to levels grouped by layer.
 * Each node's parentId becomes parents[].
 */
function convertToLevels(nodes: TraceabilityNode[]): LevelData[] {
  const nodeIndex = new Map<string, TraceabilityNode>();
  for (const n of nodes) nodeIndex.set(n.id, n);

  // Group by layer
  const layerMap = new Map<number, TraceabilityNode[]>();
  for (const n of nodes) {
    if (!layerMap.has(n.layer)) layerMap.set(n.layer, []);
    layerMap.get(n.layer)!.push(n);
  }

  const sortedLayers = [...layerMap.keys()].sort((a, b) => a - b);

  // Build TangleNode objects (parents resolved later)
  const tangleNodeIndex = new Map<string, TangleNode>();
  const levels: LevelData[] = [];

  for (let li = 0; li < sortedLayers.length; li++) {
    const layerKey = sortedLayers[li]!;
    const layerNodes = layerMap.get(layerKey)!;
    const tangleNodes: TangleNode[] = [];

    for (const n of layerNodes) {
      const tn: TangleNode = {
        id: n.id,
        level: li,
        parents: [],
        x: 0,
        y: 0,
        height: 0,
        bundles: [],
        bundlesIndex: {},
        data: n,
      };
      tangleNodeIndex.set(n.id, tn);
      tangleNodes.push(tn);
    }

    levels.push({ nodes: tangleNodes, bundles: [] });
  }

  // Resolve parent references
  for (const [, tn] of tangleNodeIndex) {
    const parentId = tn.data.parentId;
    if (parentId && tangleNodeIndex.has(parentId)) {
      tn.parents = [tangleNodeIndex.get(parentId)!];
    }
  }

  return levels;
}

/**
 * Core Nitaku algorithm — computes bundled positions and link control points.
 */
function constructBundledLayout(
  levels: LevelData[],
  config: TangledTreeLayoutConfig,
): { nodes: TangleNode[]; links: TangleLink[]; layoutWidth: number; layoutHeight: number } {
  const {
    padding,
    nodeHeight,
    nodeWidth,
    bundleWidth,
    levelYPadding,
    metroD,
    minFamilyHeight,
    c,
  } = config;
  const bigc = nodeWidth + c;

  // ---- Step 1: Precompute bundles per level ----
  for (let i = 0; i < levels.length; i++) {
    const level = levels[i]!;
    const bundleIndex: Record<string, TangleBundle> = {};

    for (const n of level.nodes) {
      if (n.parents.length === 0) continue;

      const id = n.parents
        .map((p) => p.id)
        .sort()
        .join('-X-');

      if (id in bundleIndex) {
        bundleIndex[id]!.parents = bundleIndex[id]!.parents.concat(n.parents);
      } else {
        bundleIndex[id] = {
          id,
          parents: n.parents.slice(),
          level: i,
          span: i - Math.min(...n.parents.map((p) => p.level)),
          i: 0,
          x: 0,
          y: 0,
          links: [],
        };
      }
      n.bundle = bundleIndex[id]!;
    }

    level.bundles = Object.values(bundleIndex);
    level.bundles.forEach((b, bi) => {
      b.i = bi;
    });
  }

  // Collect all nodes and links
  const allNodes: TangleNode[] = levels.flatMap((l) => l.nodes);
  const allBundles: TangleBundle[] = levels.flatMap((l) => l.bundles);

  const links: TangleLink[] = [];
  for (const node of allNodes) {
    for (const parent of node.parents) {
      if (node.bundle) {
        links.push({
          source: node,
          target: parent,
          bundle: node.bundle,
          xt: 0, yt: 0, xb: 0, yb: 0, xs: 0, ys: 0, c1: 0, c2: 0,
        });
      }
    }
  }

  // ---- Step 2: Reverse pointer — parent → bundles ----
  for (const b of allBundles) {
    for (const p of b.parents) {
      if (!(b.id in p.bundlesIndex)) {
        p.bundlesIndex[b.id] = [];
      }
      p.bundlesIndex[b.id]!.push(b);
    }
  }

  for (const n of allNodes) {
    n.bundles = Object.keys(n.bundlesIndex).map((k) => n.bundlesIndex[k]!);
    n.bundles.sort(
      (a, b) =>
        Math.max(...b.map((d) => d.span)) - Math.max(...a.map((d) => d.span)),
    );
    n.bundles.forEach((bGroup, i) => {
      for (const bb of bGroup) {
        bb.i = i;
      }
    });
  }

  for (const l of links) {
    if (!l.bundle.links) l.bundle.links = [];
    l.bundle.links.push(l);
  }

  // ---- Step 3: Compute node heights ----
  for (const n of allNodes) {
    n.height = (Math.max(1, n.bundles.length) - 1) * metroD;
  }

  // ---- Step 4: Position nodes (x, y) ----
  let xOffset = padding;
  let yOffset = padding;

  for (const level of levels) {
    xOffset += level.bundles.length * bundleWidth;
    yOffset += levelYPadding;

    for (const n of level.nodes) {
      n.x = n.level * nodeWidth + xOffset;
      n.y = nodeHeight + yOffset + n.height / 2;
      yOffset += nodeHeight + n.height;
    }
  }

  // ---- Step 5: Position bundles ----
  let cumNodeCount = 0;
  for (const level of levels) {
    for (const b of level.bundles) {
      const maxParentX = Math.max(...b.parents.map((p) => p.x));
      b.x = maxParentX + nodeWidth + (level.bundles.length - 1 - b.i) * bundleWidth;
      b.y = cumNodeCount * nodeHeight;
    }
    cumNodeCount += level.nodes.length;
  }

  // ---- Step 6: Compute link control points ----
  for (const l of links) {
    l.xt = l.target.x;
    const targetBundles = l.target.bundlesIndex[l.bundle.id];
    const targetBundleI = targetBundles ? targetBundles[0]!.i : 0;
    const totalBundles = l.target.bundles.length;
    l.yt =
      l.target.y +
      targetBundleI * metroD -
      (totalBundles * metroD) / 2 +
      metroD / 2;
    l.xb = l.bundle.x;
    l.yb = l.bundle.y;
    l.xs = l.source.x;
    l.ys = l.source.y;
  }

  // ---- Step 7: Y-compression ----
  let yNegOffset = 0;
  for (const level of levels) {
    let levelMin = Infinity;
    for (const b of level.bundles) {
      for (const link of b.links) {
        const gap = link.ys - 2 * c - (link.yt + c);
        if (gap < levelMin) levelMin = gap;
      }
    }
    if (levelMin === Infinity) levelMin = 0;

    yNegOffset += -minFamilyHeight + levelMin;
    if (yNegOffset > 0) yNegOffset = 0; // don't expand

    for (const n of level.nodes) {
      n.y -= yNegOffset;
    }
  }

  // Recompute link y-coordinates after compression
  for (const l of links) {
    const targetBundles = l.target.bundlesIndex[l.bundle.id];
    const targetBundleI = targetBundles ? targetBundles[0]!.i : 0;
    const totalBundles = l.target.bundles.length;
    l.yt =
      l.target.y +
      targetBundleI * metroD -
      (totalBundles * metroD) / 2 +
      metroD / 2;
    l.ys = l.source.y;

    const levelDiff = l.source.level - l.target.level;
    l.c1 =
      levelDiff > 1
        ? Math.min(bigc, l.xb - l.xt, l.yb > l.yt ? l.yb - l.yt : Infinity) - c
        : c;
    if (l.c1 < 0) l.c1 = c;
    l.c2 = c;
  }

  // ---- Step 8: Layout bounds ----
  const layoutWidth =
    allNodes.length > 0
      ? Math.max(...allNodes.map((n) => n.x)) + nodeWidth + 2 * padding
      : 0;
  const layoutHeight =
    allNodes.length > 0
      ? Math.max(...allNodes.map((n) => n.y)) + nodeHeight / 2 + 2 * padding
      : 0;

  return { nodes: allNodes, links, layoutWidth, layoutHeight };
}

// ============================================
// Path Generators
// ============================================

/**
 * Build metro-style SVG path: M → L → Arc → L → Arc → L
 *
 * Natural Nitaku orientation is LEFT-TO-RIGHT (horizontal):
 *   x = level depth (columns), y = within-level position (rows)
 *
 * For vertical (top-to-bottom), swap x↔y and adjust arc sweeps.
 */
function buildMetroPath(
  link: TangleLink,
  orientation: 'horizontal' | 'vertical',
): string {
  const { xt, yt, xb, xs, ys, c1, c2 } = link;

  if (orientation === 'horizontal') {
    // Natural: left-to-right (Nitaku's default)
    return [
      `M ${xt} ${yt}`,
      `L ${xb - c1} ${yt}`,
      `A ${c1} ${c1} 90 0 1 ${xb} ${yt + c1}`,
      `L ${xb} ${ys - c2}`,
      `A ${c2} ${c2} 90 0 0 ${xb + c2} ${ys}`,
      `L ${xs} ${ys}`,
    ].join(' ');
  }

  // Vertical (top-to-bottom): swap x↔y, flip arc sweep directions
  const vxt = yt, vyt = xt;
  const vyb = xb;
  const vxs = ys, vys = xs;

  return [
    `M ${vxt} ${vyt}`,
    `L ${vxt} ${vyb - c1}`,
    `A ${c1} ${c1} 90 0 0 ${vxt + c1} ${vyb}`,
    `L ${vxs - c2} ${vyb}`,
    `A ${c2} ${c2} 90 0 1 ${vxs} ${vyb + c2}`,
    `L ${vxs} ${vys}`,
  ].join(' ');
}

/**
 * Apply orientation to coordinates.
 * Natural layout is horizontal (left-to-right): x = depth, y = position.
 * Vertical (top-to-bottom) swaps axes.
 */
function applyOrientation(
  x: number,
  y: number,
  orientation: 'horizontal' | 'vertical',
): { x: number; y: number } {
  return orientation === 'horizontal' ? { x, y } : { x: y, y: x };
}

/** Generate a cubic bezier path for cross-links */
function computeCrossLinkPath(
  source: PositionedNode,
  target: PositionedNode,
  orientation: 'horizontal' | 'vertical',
): string {
  const offset = Math.min(
    Math.abs(orientation === 'horizontal' ? target.y - source.y : target.x - source.x) * 0.4,
    60,
  );

  if (orientation === 'horizontal') {
    const midX = (source.x + target.x) / 2;
    return `M ${source.x} ${source.y} C ${midX} ${source.y - offset}, ${midX} ${target.y + offset}, ${target.x} ${target.y}`;
  }
  const midY = (source.y + target.y) / 2;
  return `M ${source.x} ${source.y} C ${source.x - offset} ${midY}, ${target.x + offset} ${midY}, ${target.x} ${target.y}`;
}

const FALLBACK_PADDING = { top: 40, right: 100, bottom: 40, left: 100 };

/** Fallback: arrange nodes in a simple grid when bundled layout fails */
function flatFallback(
  nodes: TraceabilityNode[],
  crossLinks: TraceabilityCrossLink[],
  width: number,
  height: number,
): TangledTreeLayoutResult {
  const layers = [0, 1, 2, 3];
  const layerNodes = layers.map((l) => nodes.filter((n) => n.layer === l));

  const colWidth =
    (width - FALLBACK_PADDING.left - FALLBACK_PADDING.right) / Math.max(layers.length - 1, 1);
  const positionedNodes: PositionedNode[] = [];

  for (let li = 0; li < layers.length; li++) {
    const ln = layerNodes[li] ?? [];
    const rowHeight =
      (height - FALLBACK_PADDING.top - FALLBACK_PADDING.bottom) / Math.max(ln.length + 1, 2);
    for (let ni = 0; ni < ln.length; ni++) {
      const n = ln[ni]!;
      positionedNodes.push({
        id: n.id,
        entityType: n.entityType,
        label: n.label,
        code: n.code,
        status: n.status,
        layer: n.layer,
        x: FALLBACK_PADDING.left + li * colWidth,
        y: FALLBACK_PADDING.top + (ni + 1) * rowHeight,
        bundleHeight: 0,
      });
    }
  }

  const nodeMap = new Map(positionedNodes.map((n) => [n.id, n]));
  const crossLinkPaths: CrossLinkPath[] = crossLinks
    .filter((cl) => nodeMap.has(cl.sourceId) && nodeMap.has(cl.targetId))
    .map((cl) => {
      const s = nodeMap.get(cl.sourceId)!;
      const t = nodeMap.get(cl.targetId)!;
      return { ...cl, path: computeCrossLinkPath(s, t, 'horizontal') };
    });

  return {
    nodes: positionedNodes,
    hierarchyEdges: [],
    crossLinkPaths,
    bundledEdges: [],
    layoutWidth: width,
    layoutHeight: height,
  };
}
