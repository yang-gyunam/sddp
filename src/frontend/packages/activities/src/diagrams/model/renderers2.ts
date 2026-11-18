import type { Component } from 'svelte';
import type { RendererInput2 } from './scene2';
import type { DiagramType2 } from '../types2';
import SvgDiagramRenderer2 from '../components/idioms/SvgDiagramRenderer2.svelte';
import TableDiagramRenderer2 from '../components/idioms/TableDiagramRenderer2.svelte';
export interface DiagramRendererAdapter2 {
  id: string;
  label: string;
  schemaVersions: readonly number[];
  kinds: readonly DiagramType2[];
  changeHighlighting: boolean;
  moveNodes: boolean;
  resizeNodes: boolean;
  component: Component<RendererInput2>;
}
const kinds: DiagramType2[] = ['graph', 'flowchart', 'sequence', 'class', 'state', 'er', 'c4'];
export const diagramRenderers2: readonly DiagramRendererAdapter2[] = [
  {
    id: 'sddp-svg-v2',
    label: 'Canvas',
    schemaVersions: [2],
    kinds,
    changeHighlighting: true,
    moveNodes: true,
    resizeNodes: true,
    component: SvgDiagramRenderer2,
  },
  {
    id: 'sddp-table-v2',
    label: 'Table',
    schemaVersions: [2],
    kinds,
    changeHighlighting: true,
    moveNodes: false,
    resizeNodes: false,
    component: TableDiagramRenderer2,
  },
];
export function resolveRenderer2(id: string, kind: DiagramType2): DiagramRendererAdapter2 {
  return (
    diagramRenderers2.find((r) => r.id === id && r.kinds.includes(kind)) ?? diagramRenderers2[1]!
  );
}
