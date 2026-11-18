import type { DiagramRevision, DiagramChange } from '../types';
import type { DiagramRevision2, DiagramChange2, ElementRef } from '../types2';
import { emptyModel } from './editor';
import {
  elements2,
  getElement2,
  owner2,
  sameRef2,
  refKey2,
  elementName2,
  type Element2,
} from './model2';
import type { NodeBounds } from './nodeGeometry';
export interface RendererInput2 {
  model: DiagramRevision2;
  left?: DiagramRevision2;
  changes?: DiagramChange2[];
  selection?: ElementRef | null;
  onselect?: (ref: ElementRef) => void;
  onmove?: (ref: ElementRef, x: number, y: number) => void;
  onresize?: (ref: ElementRef, bounds: NodeBounds) => void;
  oncreateNode?: (x: number, y: number, stencil?: string) => void;
  onconnect?: (from: ElementRef, to: ElementRef) => void;
  onconnectstart?: (ref: ElementRef) => void;
  onrename?: (ref: ElementRef) => void;
}
export const token2 = refKey2;
export function ref2(token: string): ElementRef {
  const [collection, id] = JSON.parse(token);
  return { collection, id };
}
export function resizable2(model: DiagramRevision2, ref: ElementRef) {
  return (
    ['nodes', 'states', 'types', 'entities', 'elements', 'boundaries', 'participants'].includes(
      ref.collection
    ) && !!getElement2(model, ref)
  );
}
export function connectable2(model: DiagramRevision2, ref: ElementRef) {
  return resizable2(model, ref) && ref.collection !== 'boundaries';
}
export interface Projection2 {
  model: DiagramRevision;
  changes: DiagramChange[];
  diagnostics: string[];
  eventY: Record<string, number>;
  participantX: Record<string, number>;
  bottom: number;
}
/** Render-only view. Typed ownership, timeline and domain fields stay in the common model. */
export function projectScene2(
  model: DiagramRevision2,
  changes: DiagramChange2[] = [],
  left?: DiagramRevision2
): Projection2 {
  const view = emptyModel();
  view.diagramId = model.diagramId;
  view.revision = model.revision;
  view.source = { ...model.source };
  view.presentation = { ...model.presentation.properties };
  const diagnostics: string[] = [];
  if (
    model.diagramType !== 'graph' &&
    model.diagramType !== 'flowchart' &&
    view.presentation.direction !== undefined
  ) {
    diagnostics.push(
      `presentation.direction is preserved but not displayed for ${model.diagramType}.`
    );
    delete view.presentation.direction;
  }
  const eventY: Record<string, number> = {};
  const participantX: Record<string, number> = {};
  let bottom = 400;
  const node = (
    ref: ElementRef,
    e: Element2,
    index: number,
    defaults: Record<string, string> = {}
  ) => {
    const properties = {
      x: String(
        40 +
          (model.presentation.properties.direction === 'TD' ? Math.floor(index / 3) : index % 3) *
            290
      ),
      y: String(
        40 +
          (model.presentation.properties.direction === 'TD' ? index % 3 : Math.floor(index / 3)) *
            230
      ),
      width: '220',
      height: '100',
      ...defaults,
      ...(model.diagramType === 'sequence' && ['events', 'branches'].includes(ref.collection)
        ? {}
        : e.presentation.properties),
    };
    view.nodes[token2(ref)] = { label: elementName2(e), presentation: properties };
  };
  const top = (collection: string) =>
    elements2(model)
      .filter((e) => e.ref.collection === collection)
      .sort((a, b) => {
        const parent = owner2(model, a.ref);
        const order =
          parent.collection !== 'diagram' && sameRef2(parent, owner2(model, b.ref))
            ? (getElement2(model, parent)?.presentation.order ?? [])
            : model.presentation.order;
        const ai = order.findIndex((r) => sameRef2(r, a.ref)),
          bi = order.findIndex((r) => sameRef2(r, b.ref));
        if (ai >= 0 || bi >= 0) return (ai < 0 ? Infinity : ai) - (bi < 0 ? Infinity : bi);
        return a.ref.id < b.ref.id ? -1 : a.ref.id > b.ref.id ? 1 : 0;
      });
  if (model.diagramType === 'graph' || model.diagramType === 'flowchart')
    top('nodes').forEach(({ ref, element }, i) =>
      node(ref, element, i, { width: '160', height: '60' })
    );
  if (model.diagramType === 'state')
    top('states').forEach(({ ref, element }, i) =>
      node(ref, element, i, {
        width: element.kind === 'state' ? '160' : '80',
        height: element.kind === 'state' ? '60' : '40',
      })
    );
  if (model.diagramType === 'class' || model.diagramType === 'er')
    top(model.diagramType === 'class' ? 'types' : 'entities').forEach(({ ref, element }, i) => {
      const count = elements2(model).filter((e) => sameRef2(owner2(model, e.ref), ref)).length;
      node(ref, element, i, { height: String(65 + Math.max(2, count) * 25) });
    });
  if (model.diagramType === 'c4') {
    top('boundaries').forEach(({ ref, element }, i) =>
      node(ref, element, i, { x: '30', y: '210', width: '660', height: '360' })
    );
    top('elements').forEach(({ ref, element }, i) => {
      const internal = typeof element.boundary === 'string';
      const siblings = top('elements').filter(
        (v) => (typeof v.element.boundary === 'string') === internal
      );
      const index = siblings.findIndex((v) => sameRef2(v.ref, ref));
      const parent = internal ? model.content.boundaries[element.boundary as string] : null;
      const x = Number(parent?.presentation.properties.x ?? 30),
        y = Number(parent?.presentation.properties.y ?? 210);
      node(ref, element, i, {
        x: String(internal ? x + 35 + (index % 2) * 300 : 45 + index * 280),
        y: String(internal ? y + 60 + Math.floor(index / 2) * 150 : 35),
        width: '230',
        height: '110',
      });
    });
  }
  if (model.diagramType === 'sequence') {
    const ordered = model.presentation.order.length
      ? model.presentation.order
      : top('participants')
          .map((v) => v.ref)
          .sort((a, b) => (a.id < b.id ? -1 : 1));
    ordered.forEach((ref, i) => {
      const element = getElement2(model, ref)!;
      node(ref, element, i, { x: String(100 + i * 230), y: '25', width: '160', height: '70' });
      const p = view.nodes[token2(ref)]!.presentation;
      participantX[ref.id] = Number(p.x) + Number(p.width) / 2;
    });
    let row = 0;
    const width = String(Math.max(350, ordered.length * 230));
    const walk = (ids: string[], depth = 0) => {
      const inset = Math.min(depth, 5) * 12;
      for (const id of ids) {
        const e = model.content.events[id]!;
        const ref: ElementRef = { collection: 'events', id };
        const y = 130 + row++ * 70;
        eventY[id] = y;
        if (e.type === 'fragment') {
          const start = row;
          for (const branchId of e.branchOrder) {
            const b = model.content.branches[branchId]!;
            const by = 130 + row++ * 70;
            node(
              { collection: 'branches', id: branchId },
              getElement2(model, { collection: 'branches', id: branchId })!,
              0,
              {
                x: String(65 + inset),
                y: String(by),
                width: String(Number(width) - 2 * inset - 40),
                height: '32',
              }
            );
            walk(b.events, depth + 1);
          }
          node(ref, getElement2(model, ref)!, 0, {
            x: String(45 + inset),
            y: String(y),
            width: String(Number(width) - 2 * inset),
            height: String(Math.max(80, (row - start + 1) * 70 - 8)),
          });
        } else if (e.type === 'message') {
          const from = participantX[e.from]!,
            to = participantX[e.to]!;
          node(ref, getElement2(model, ref)!, 0, {
            x: String(Math.min(from, to) - 15),
            y: String(y),
            width: String(Math.max(90, Math.abs(to - from) + 30)),
            height: '40',
          });
        } else {
          const start = e.type === 'activationStart' ? e : model.content.events[e.start];
          const x = start?.type === 'activationStart' ? participantX[start.participant]! : 120;
          node(ref, getElement2(model, ref)!, 0, {
            x: String(x - 10),
            y: String(y),
            width: '80',
            height: '32',
          });
        }
      }
    };
    walk(model.content.timeline);
    bottom = 150 + row * 70;
    // Frames are under their owned messages; membership never comes from SVG nesting.
    view.nodes = Object.fromEntries(
      Object.entries(view.nodes).sort(([a], [b]) => {
        const ea = getElement2(model, ref2(a)),
          eb = getElement2(model, ref2(b));
        return Number(eb?.type === 'fragment') - Number(ea?.type === 'fragment');
      })
    );
  }
  for (const { ref, element: e } of elements2(model)) {
    const edgeCollections = ['edges', 'transitions', 'relations'];
    if (!edgeCollections.includes(ref.collection)) continue;
    const collection =
      model.diagramType === 'state'
        ? 'states'
        : model.diagramType === 'class'
          ? 'types'
          : model.diagramType === 'er'
            ? 'entities'
            : model.diagramType === 'c4'
              ? 'elements'
              : 'nodes';
    view.edges[token2(ref)] = {
      from: token2({ collection, id: String(e.from) }),
      to: token2({ collection, id: String(e.to) }),
      label: String(e.label ?? e.event ?? ''),
      presentation: {},
    };
    for (const key of Object.keys(e.presentation.properties))
      diagnostics.push(`${ref.collection}.${ref.id}.${key} is preserved but not displayed.`);
  }
  for (const { ref, element } of elements2(model))
    if (
      !view.nodes[token2(ref)] ||
      (model.diagramType === 'sequence' && ['events', 'branches'].includes(ref.collection))
    ) {
      if (view.edges[token2(ref)]) continue;
      for (const key of Object.keys(element.presentation.properties))
        diagnostics.push(`${ref.collection}.${ref.id}.${key} is preserved but not displayed.`);
    }
  const projectedChanges: DiagramChange[] = [];
  for (const c of changes) {
    if (c.target.collection === 'diagram') {
      const ids =
        c.category === 'semantic'
          ? Object.keys(view.nodes).filter((id) => ref2(id).collection === 'events')
          : Object.keys(view.nodes);
      for (const id of ids)
        projectedChanges.push({
          target: 'node',
          id,
          field: c.field?.join('.') ?? null,
          kind: 'modified',
          category: c.category,
          before: null,
          after: null,
        });
      continue;
    }
    let target = c.target;
    const source = c.kind === 'removed' && left ? left : model;
    while (
      !view.nodes[token2(target)] &&
      !view.edges[token2(target)] &&
      target.collection !== 'diagram' &&
      ![
        'nodes',
        'states',
        'types',
        'entities',
        'elements',
        'boundaries',
        'participants',
        'edges',
        'transitions',
        'relations',
        'events',
        'branches',
      ].includes(target.collection)
    )
      target = owner2(source, target);
    if (target.collection === 'diagram') continue;
    projectedChanges.push({
      target: ['edges', 'transitions', 'relations'].includes(target.collection) ? 'edge' : 'node',
      id: token2(target),
      field: c.field?.join('.') ?? null,
      kind: sameRef2(target, c.target) ? c.kind : 'modified',
      category: c.category,
      before: null,
      after: null,
    });
  }
  return { model: view, changes: projectedChanges, diagnostics, eventY, participantX, bottom };
}
