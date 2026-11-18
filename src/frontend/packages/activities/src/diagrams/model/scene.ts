import type { Snippet } from 'svelte';
import { nodeSize, type NodeBounds } from './nodeGeometry';
import type { DiagramChange, DiagramRevision } from '../types';
export interface DiagramSelection {
  target: 'node' | 'edge';
  id: string;
}
export interface RendererInput {
  renderNode?: Snippet<[SceneNode]>;
  renderEdge?: Snippet<[SceneEdge]>;
  underlay?: Snippet;
  canResizeNode?: (id: string) => boolean;
  canConnectNode?: (id: string) => boolean;
  model: DiagramRevision;
  left?: DiagramRevision;
  changes?: DiagramChange[];
  selection?: DiagramSelection | null;
  onselect?: (selection: DiagramSelection) => void;
  onresize?: (id: string, bounds: NodeBounds) => void;
  onmove?: (id: string, x: number, y: number) => void;
  oncreateNode?: (x: number, y: number, stencil?: string) => void;
  onconnect?: (from: string, to: string) => void;
  onconnectstart?: (from: string) => void;
  onrename?: (id: string) => void;
}
interface ChangeFlags {
  semantic: boolean;
  presentation: boolean;
  removed: boolean;
  added: boolean;
}
export interface SceneNode extends ChangeFlags, NodeBounds {
  id: string;
  label: string;
  x: number;
  y: number;
}
export interface SceneEdge extends ChangeFlags {
  id: string;
  from: string;
  to: string;
  label: string;
  start?: SceneNode;
  end?: SceneNode;
}
export interface DiagramScene {
  nodes: SceneNode[];
  edges: SceneEdge[];
  diagnostics: string[];
  viewBox: string;
}
export function diagramScene(
  model: DiagramRevision,
  left?: DiagramRevision,
  changes: DiagramChange[] = []
): DiagramScene {
  const diagnostics: string[] = [];
  function unsupported(path: string, values: Record<string, string>, known: string[]) {
    for (const key of Object.keys(values))
      if (!known.includes(key)) diagnostics.push(`${path}.${key} is preserved but not displayed.`);
  }
  unsupported('presentation', model.presentation, ['direction']);
  if (model.presentation.direction && !['LR', 'TD'].includes(model.presentation.direction))
    diagnostics.push('Only LR and TD automatic layout are displayed.');
  const classify = (target: string, id: string) => ({
    semantic: changes.some((c) => c.target === target && c.id === id && c.category === 'semantic'),
    presentation: changes.some(
      (c) => c.target === target && c.id === id && c.category === 'presentation'
    ),
    removed: changes.some(
      (c) => c.target === target && c.id === id && c.kind === 'removed' && c.field == null
    ),
    added: changes.some(
      (c) => c.target === target && c.id === id && c.kind === 'added' && c.field == null
    ),
  });
  const nodes = Object.entries({ ...left?.nodes, ...model.nodes })
    .filter(([id]) => Object.hasOwn(model.nodes, id) || classify('node', id).removed)
    .map(([id, node], index) => {
      unsupported(`nodes.${id}.presentation`, node.presentation, ['x', 'y', 'width', 'height']);
      const coordinate = (key: 'x' | 'y', fallback: number) => {
        const raw = node.presentation[key];
        if (raw === undefined) return fallback;
        const value = raw.trim() ? Number(raw) : NaN;
        if (Number.isFinite(value) && Math.abs(value) <= 100000) return value;
        diagnostics.push(
          `nodes.${id}.presentation.${key} is preserved; the display range is -100000 to 100000.`
        );
        return fallback;
      };
      const dimension = (key: 'width' | 'height') => {
        const raw = node.presentation[key];
        const min = key === 'width' ? nodeSize.minWidth : nodeSize.minHeight;
        if (raw === undefined) return nodeSize[key];
        const value = raw.trim() ? Number(raw) : NaN;
        if (Number.isFinite(value) && value >= min && value <= nodeSize.max) return value;
        diagnostics.push(
          `nodes.${id}.presentation.${key} is preserved; the display range is ${min} to ${nodeSize.max}.`
        );
        return nodeSize[key];
      };
      const horizontal = model.presentation.direction !== 'TD';
      return {
        id,
        label: node.label,
        width: dimension('width'),
        height: dimension('height'),
        x: coordinate('x', horizontal ? 30 + (index % 4) * 210 : 30 + Math.floor(index / 4) * 210),
        y: coordinate('y', horizontal ? 35 + Math.floor(index / 4) * 115 : 35 + (index % 4) * 115),
        ...classify('node', id),
      };
    });
  const currentPositions = new Map(nodes.map((node) => [node.id, node]));
  const originalPositions = new Map(
    (left &&
    changes.some(
      (change) => change.target === 'edge' && change.kind === 'removed' && change.field == null
    )
      ? diagramScene(left).nodes
      : []
    ).map((node) => [node.id, node])
  );
  const edges = Object.entries({ ...left?.edges, ...model.edges })
    .filter(([id]) => Object.hasOwn(model.edges, id) || classify('edge', id).removed)
    .map(([id, edge]) => {
      unsupported(`edges.${id}.presentation`, edge.presentation, []);
      // A removed connection uses its original endpoints, including deleted nodes.
      const positions = (endpoint: string) =>
        (left && classify('edge', id).removed ? originalPositions : currentPositions).get(endpoint);
      return {
        id,
        ...edge,
        start: positions(edge.from),
        end: positions(edge.to),
        ...classify('edge', id),
      };
    });
  const drawnPositions = [
    ...nodes,
    ...edges.flatMap((edge) => [edge.start, edge.end].filter((node): node is SceneNode => !!node)),
  ];
  const minX = Math.min(0, ...drawnPositions.map((n) => n.x - 15));
  const minY = Math.min(0, ...drawnPositions.map((n) => n.y - 15));
  return {
    nodes,
    edges,
    diagnostics,
    viewBox: `${minX} ${minY} ${Math.max(600, ...drawnPositions.map((n) => n.x + n.width + 120)) - minX} ${Math.max(300, ...drawnPositions.map((n) => n.y + n.height + 50)) - minY}`,
  };
}
