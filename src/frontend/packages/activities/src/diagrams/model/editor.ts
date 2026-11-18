import { nodeSize } from './nodeGeometry';
import type { DiagramRevision } from '../types';

export type DiagramCommand =
  | { type: 'addNode'; id: string; label: string; x?: number; y?: number }
  | { type: 'renameNode'; id: string; label: string }
  | { type: 'resizeNode'; id: string; x: number; y: number; width: number; height: number }
  | { type: 'moveNode'; id: string; x: number; y: number }
  | { type: 'removeNode'; id: string }
  | { type: 'addEdge'; id: string; from: string; to: string; label: string }
  | { type: 'updateEdge'; id: string; from: string; to: string; label: string }
  | { type: 'removeEdge'; id: string };

export interface DiagramEditorAdapter {
  id: string;
  commands: readonly DiagramCommand['type'][];
  sourceExport: boolean;
  apply(model: DiagramRevision, baseRevision: number, command: DiagramCommand): DiagramRevision;
}
export const cloneModel = (model: DiagramRevision): DiagramRevision =>
  JSON.parse(JSON.stringify(model));
export function emptyModel(): DiagramRevision {
  return {
    schemaVersion: 1,
    diagramId: 'new',
    revision: 1,
    source: { format: 'sddp-model', provider: 'sddp-editor', providerVersion: '1', text: '' },
    presentation: {},
    nodes: {},
    edges: {},
  };
}
/** Stable equality for tab drafts; insertion order never creates an unsaved edit. */
export function modelKey(value: unknown): string {
  if (value === null || typeof value !== 'object') return JSON.stringify(value);
  if (Array.isArray(value)) return `[${value.map(modelKey).join(',')}]`;
  return `{${Object.entries(value)
    .sort(([a], [b]) => (a < b ? -1 : a > b ? 1 : 0))
    .map(([key, entry]) => `${JSON.stringify(key)}:${modelKey(entry)}`)
    .join(',')}}`;
}
export const commonEditor: DiagramEditorAdapter = {
  id: 'sddp-common-editor-v1',
  sourceExport: false,
  commands: [
    'addNode',
    'renameNode',
    'moveNode',
    'resizeNode',
    'removeNode',
    'addEdge',
    'updateEdge',
    'removeEdge',
  ],
  apply(model, baseRevision, command) {
    if (model.revision !== baseRevision) throw new Error('The draft base revision has changed.');
    const next = cloneModel(model);
    const node = (id: string) => {
      if (!Object.hasOwn(next.nodes, id)) throw new Error(`Unknown node: ${id}`);
      return next.nodes[id]!;
    };
    const edge = (id: string) => {
      if (!Object.hasOwn(next.edges, id)) throw new Error(`Unknown connection: ${id}`);
      return next.edges[id]!;
    };
    if (!command.id.trim()) throw new Error('An element ID is required.');
    switch (command.type) {
      case 'addNode':
        if (Object.hasOwn(next.nodes, command.id)) throw new Error('Node ID already exists.');
        if (
          (command.x !== undefined || command.y !== undefined) &&
          (!Number.isFinite(command.x) || !Number.isFinite(command.y))
        )
          throw new Error('Coordinates must be finite.');
        next.nodes = {
          ...next.nodes,
          [command.id]: {
            label: command.label,
            presentation:
              command.x === undefined ? {} : { x: String(command.x), y: String(command.y) },
          },
        };
        break;
      case 'renameNode':
        node(command.id).label = command.label;
        break;
      case 'moveNode':
        if (!Number.isFinite(command.x) || !Number.isFinite(command.y))
          throw new Error('Coordinates must be finite.');
        node(command.id).presentation = {
          ...node(command.id).presentation,
          x: String(command.x),
          y: String(command.y),
        };
        break;
      case 'resizeNode':
        if (
          ![command.x, command.y, command.width, command.height].every(Number.isFinite) ||
          Math.abs(command.x) > 100000 ||
          Math.abs(command.y) > 100000 ||
          command.width < nodeSize.minWidth ||
          command.height < nodeSize.minHeight ||
          command.width > nodeSize.max ||
          command.height > nodeSize.max
        )
          throw new Error('Invalid node bounds.');
        node(command.id).presentation = {
          ...node(command.id).presentation,
          x: String(command.x),
          y: String(command.y),
          width: String(command.width),
          height: String(command.height),
        };
        break;
      case 'removeNode':
        node(command.id);
        delete next.nodes[command.id];
        next.edges = Object.fromEntries(
          Object.entries(next.edges).filter(([, e]) => e.from !== command.id && e.to !== command.id)
        );
        break;
      case 'addEdge':
      case 'updateEdge':
        node(command.from);
        node(command.to);
        if (command.type === 'addEdge' && Object.hasOwn(next.edges, command.id))
          throw new Error('Connection ID already exists.');
        if (command.type === 'updateEdge') edge(command.id);
        next.edges = {
          ...next.edges,
          [command.id]: {
            from: command.from,
            to: command.to,
            label: command.label,
            presentation: command.type === 'updateEdge' ? { ...edge(command.id).presentation } : {},
          },
        };
        break;
      case 'removeEdge':
        edge(command.id);
        delete next.edges[command.id];
        break;
    }
    return next;
  },
};
