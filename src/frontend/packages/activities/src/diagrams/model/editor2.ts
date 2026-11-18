import type { DiagramRevision2, ElementRef } from '../types2';
import {
  clone2,
  getElement2,
  elements2,
  owner2,
  refKey2,
  sameRef2,
  presentation2,
  repairOrders2,
  validate2,
  type Element2,
} from './model2';
import { nodeSize } from './nodeGeometry';
import { projectScene2, token2 } from './scene2';
export type DiagramCommand2 =
  | { type: 'add'; stencil: string; owner?: ElementRef; x?: number; y?: number }
  | { type: 'connect'; from: ElementRef; to: ElementRef }
  | { type: 'set'; target: ElementRef; field: string; value: unknown }
  | { type: 'move'; target: ElementRef; x: number; y: number }
  | { type: 'resize'; target: ElementRef; x: number; y: number; width: number; height: number }
  | { type: 'remove'; target: ElementRef }
  | { type: 'reorder'; target: ElementRef; owner: ElementRef; index: number }
  | { type: 'activation'; participant: string; owner?: ElementRef };
export interface DiagramEditorAdapter2 {
  id: string;
  commands: readonly DiagramCommand2['type'][];
  apply: (model: DiagramRevision2, command: DiagramCommand2, revision: number) => DiagramRevision2;
}
export interface Stencil2 {
  id: string;
  label: string;
  collection: ElementRef['collection'];
  kind?: string;
}
export function stencils2(model: DiagramRevision2): Stencil2[] {
  switch (model.diagramType) {
    case 'graph':
      return [{ id: 'node', label: 'Node', collection: 'nodes' }];
    case 'flowchart':
      return ['process', 'terminator', 'decision'].map((kind) => ({
        id: kind,
        label: kind === 'process' ? 'Process' : kind === 'terminator' ? 'Start / end' : 'Decision',
        collection: 'nodes' as const,
        kind,
      }));
    case 'state':
      return ['state', 'initial', 'final'].map((kind) => ({
        id: kind,
        label: kind === 'state' ? 'State' : kind === 'initial' ? 'Initial' : 'Final',
        collection: 'states' as const,
        kind,
      }));
    case 'class':
      return [{ id: 'type', label: 'Class', collection: 'types' }];
    case 'er':
      return [{ id: 'entity', label: 'Entity', collection: 'entities' }];
    case 'sequence':
      return [
        {
          id: 'participant',
          label: 'Participant',
          collection: 'participants',
          kind: 'participant',
        },
        { id: 'actor', label: 'Actor', collection: 'participants', kind: 'actor' },
        ...['alt', 'opt', 'loop'].map((kind) => ({
          id: kind,
          label: kind.toUpperCase(),
          collection: 'events' as const,
          kind,
        })),
      ];
    case 'c4':
      return [
        ...['person', 'system', 'externalSystem'].map((kind) => ({
          id: kind,
          label:
            kind === 'externalSystem' ? 'External system' : kind === 'person' ? 'Person' : 'System',
          collection: 'elements' as const,
          kind,
        })),
        ...(model.content.view === 'container'
          ? [
              { id: 'boundary', label: 'Boundary', collection: 'boundaries' as const },
              ...['container', 'database'].map((kind) => ({
                id: kind,
                label: kind === 'database' ? 'Database' : 'Container',
                collection: 'elements' as const,
                kind,
              })),
            ]
          : []),
      ];
  }
}
export interface Field2 {
  key: string;
  label: string;
  nullable?: boolean;
  kind: 'text' | 'boolean' | 'select' | 'keys';
  options?: { value: string; label: string }[];
}
export function fields2(model: DiagramRevision2, ref: ElementRef): Field2[] {
  const e = getElement2(model, ref);
  if (!e) return [];
  const options = (values: string[]) => values.map((value) => ({ value, label: value }));
  const refs = (collection: string) =>
    elements2(model)
      .filter((v) => v.ref.collection === collection)
      .map((v) => ({
        value: v.ref.id,
        label: String(v.element.name ?? v.element.label ?? v.ref.id),
      }));
  return Object.keys(e)
    .filter(
      (key) =>
        !(model.diagramType === 'state' && e.kind !== 'state' && key === 'name') &&
        key !== 'presentation' &&
        !['type', 'parameterOrder', 'branchOrder', 'events'].includes(key) &&
        !(ref.collection === 'branches' && key === 'kind')
    )
    .map((key) => {
      const field: Field2 = {
        key,
        label:
          (
            {
              typeName: 'Type',
              returnType: 'Return type',
              fromCardinality: 'From cardinality',
              toCardinality: 'To cardinality',
            } as Record<string, string>
          )[key] ?? key[0]!.toUpperCase() + key.slice(1),
        kind: 'text',
        nullable:
          e[key] === null ||
          ['returnType', 'description', 'technology', 'boundary'].includes(key) ||
          (ref.collection === 'parameters' && key === 'typeName'),
      };
      if (key === 'identifying') return { ...field, kind: 'boolean' };
      if (key === 'keys') return { ...field, kind: 'keys' };
      if (key === 'visibility')
        return {
          ...field,
          kind: 'select',
          options: options(['unspecified', 'public', 'private', 'protected', 'internal']),
        };
      if (key.endsWith('Cardinality'))
        return {
          ...field,
          kind: 'select',
          options: options(['zeroOrOne', 'one', 'zeroOrMore', 'oneOrMore']),
        };
      if (key === 'kind') {
        const kinds =
          model.diagramType === 'flowchart'
            ? ['process', 'terminator', 'decision']
            : model.diagramType === 'state'
              ? ['state', 'initial', 'final']
              : model.diagramType === 'class'
                ? [
                    'inheritance',
                    'composition',
                    'aggregation',
                    'association',
                    'dependency',
                    'realization',
                  ]
                : model.diagramType === 'sequence'
                  ? ref.collection === 'participants'
                    ? ['participant', 'actor']
                    : ['solidArrow', 'dashedArrow']
                  : model.diagramType === 'c4'
                    ? model.content.view === 'context'
                      ? ['person', 'system', 'externalSystem']
                      : ['person', 'system', 'externalSystem', 'container', 'database']
                    : [];
        if (e.type === 'fragment')
          return { ...field, kind: 'select', options: options(['alt', 'opt', 'loop']) };
        if (kinds.length) return { ...field, kind: 'select', options: options(kinds) };
      }
      if (['from', 'to', 'owner', 'boundary', 'participant', 'start'].includes(key)) {
        const collection =
          key === 'boundary'
            ? 'boundaries'
            : key === 'start'
              ? 'events'
              : key === 'participant'
                ? 'participants'
                : key === 'owner'
                  ? model.diagramType === 'class'
                    ? 'types'
                    : 'entities'
                  : model.diagramType === 'sequence'
                    ? 'participants'
                    : model.diagramType === 'state'
                      ? 'states'
                      : model.diagramType === 'class'
                        ? 'types'
                        : model.diagramType === 'er'
                          ? 'entities'
                          : model.diagramType === 'c4'
                            ? 'elements'
                            : 'nodes';
        let values = refs(collection);
        if (key === 'start')
          values = values.filter(
            (v) =>
              getElement2(model, { collection: 'events', id: v.value })?.type === 'activationStart'
          );
        return { ...field, kind: 'select', options: values };
      }
      return field;
    });
}
function timeline2(model: DiagramRevision2, owner: ElementRef): string[] {
  if (model.diagramType !== 'sequence') throw new Error('Timeline is not supported');
  if (owner.collection === 'diagram') return model.content.timeline;
  const branch = model.content.branches[owner.id];
  if (owner.collection !== 'branches' || !branch) throw new Error('Select a timeline or branch');
  return branch.events;
}
function add2(
  model: DiagramRevision2,
  collection: ElementRef['collection'],
  value: Element2
): ElementRef {
  const map = (model.content as unknown as Record<string, Record<string, Element2>>)[collection];
  if (!map) throw new Error('Unsupported collection');
  const id = crypto.randomUUID();
  map[id] = value;
  return { collection, id };
}
function numeric(value: number, max = 100000) {
  if (!Number.isFinite(value) || Math.abs(value) > max)
    throw new Error('Coordinates are outside the display range');
  return String(value);
}
function attach2(model: DiagramRevision2, ref: ElementRef, owner: ElementRef, index = Infinity) {
  if (ref.collection === 'events') {
    if (model.diagramType !== 'sequence') throw new Error('Unsupported timeline');
    for (const list of [
      model.content.timeline,
      ...Object.values(model.content.branches).map((b) => b.events),
    ]) {
      const i = list.indexOf(ref.id);
      if (i >= 0) list.splice(i, 1);
    }
    const list = timeline2(model, owner);
    list.splice(Math.min(index, list.length), 0, ref.id);
  } else if (ref.collection === 'parameters') {
    if (
      model.diagramType !== 'class' ||
      owner.collection !== 'operations' ||
      !model.content.operations[owner.id]
    )
      throw new Error('Select an operation');
    for (const op of Object.values(model.content.operations)) {
      const i = op.parameterOrder.indexOf(ref.id);
      if (i >= 0) op.parameterOrder.splice(i, 1);
    }
    const list = model.content.operations[owner.id]!.parameterOrder;
    list.splice(Math.min(index, list.length), 0, ref.id);
  } else throw new Error('This element has no semantic timeline order');
}
function erase2(model: DiagramRevision2, ref: ElementRef, seen = new Set<string>()) {
  if (seen.has(refKey2(ref))) return;
  seen.add(refKey2(ref));
  const item = getElement2(model, ref);
  if (!item) throw new Error('Element is missing');
  const dependents = elements2(model).filter(({ ref: r, element: e }) => {
    if (sameRef2(r, ref)) return false;
    if (sameRef2(owner2(model, r), ref)) return true;
    if (
      ['nodes', 'states', 'types', 'entities', 'elements', 'participants'].includes(
        ref.collection
      ) &&
      ('from' in e || 'to' in e)
    )
      return e.from === ref.id || e.to === ref.id;
    if (ref.collection === 'participants' && e.type === 'activationStart')
      return e.participant === ref.id;
    if (item.type === 'activationStart' && e.type === 'activationEnd') return e.start === ref.id;
    if (item.type === 'activationEnd' && r.collection === 'events') return r.id === item.start;
    return false;
  });
  for (const d of dependents) if (getElement2(model, d.ref)) erase2(model, d.ref, seen);
  delete (model.content as unknown as Record<string, Record<string, Element2>>)[ref.collection]![
    ref.id
  ];
  if (model.diagramType === 'sequence') {
    model.content.timeline = model.content.timeline.filter((id) => !!model.content.events[id]);
    for (const branch of Object.values(model.content.branches))
      branch.events = branch.events.filter((id) => !!model.content.events[id]);
    for (const e of Object.values(model.content.events))
      if (e.type === 'fragment')
        e.branchOrder = e.branchOrder.filter((id) => !!model.content.branches[id]);
  }
  if (model.diagramType === 'class')
    for (const op of Object.values(model.content.operations))
      op.parameterOrder = op.parameterOrder.filter((id) => !!model.content.parameters[id]);
}
export function apply2(
  model: DiagramRevision2,
  command: DiagramCommand2,
  revision: number
): DiagramRevision2 {
  validate2(model);
  if (revision !== model.revision) throw new Error('Revision changed');
  const next = clone2(model);
  let semantic = true;
  if (command.type === 'add') {
    const stencil = stencils2(next).find((s) => s.id === command.stencil);
    const p = presentation2();
    if (command.x !== undefined) p.properties.x = numeric(command.x);
    if (command.y !== undefined) p.properties.y = numeric(command.y);
    if (stencil) {
      let value: Element2 = { presentation: p };
      if (next.diagramType === 'graph') value.label = 'New node';
      else value.name = stencil.label;
      if (stencil.kind) value.kind = stencil.kind;
      if (next.diagramType === 'state' && stencil.kind !== 'state') value.name = '';
      if (next.diagramType === 'sequence' && stencil.collection === 'events') {
        delete value.name;
        value = { type: 'fragment', kind: stencil.kind, branchOrder: [], presentation: p };
        const branch = add2(next, 'branches', {
          kind: stencil.kind === 'alt' ? 'condition' : 'body',
          label: 'condition',
          events: [],
          presentation: presentation2(),
        });
        value.branchOrder = [branch.id];
      }
      if (next.diagramType === 'c4' && stencil.collection === 'elements') {
        const internal = stencil.kind === 'container' || stencil.kind === 'database';
        const boundary = Object.keys(next.content.boundaries)[0];
        if (internal && !boundary) throw new Error('Create a boundary before adding containers');
        value.description = null;
        value.technology = internal ? '' : null;
        value.boundary = internal ? boundary : null;
      }
      const added = add2(next, stencil.collection, value);
      if (next.diagramType === 'sequence' && stencil.collection === 'events')
        attach2(next, added, command.owner ?? { collection: 'diagram', id: next.diagramId });
    } else {
      const owner = command.owner;
      if (!owner || !getElement2(next, owner))
        throw new Error('Select the owning class, entity or operation');
      if (
        command.stencil === 'attribute' &&
        next.diagramType === 'class' &&
        owner.collection === 'types'
      )
        add2(next, 'attributes', {
          owner: owner.id,
          name: uniqueName(next, 'attributes', owner.id, 'property'),
          typeName: 'String',
          visibility: 'unspecified',
          presentation: p,
        });
      else if (
        command.stencil === 'attribute' &&
        next.diagramType === 'er' &&
        owner.collection === 'entities'
      )
        add2(next, 'attributes', {
          owner: owner.id,
          name: uniqueName(next, 'attributes', owner.id, 'property'),
          typeName: 'string',
          keys: [],
          presentation: p,
        });
      else if (
        command.stencil === 'operation' &&
        next.diagramType === 'class' &&
        owner.collection === 'types'
      )
        add2(next, 'operations', {
          owner: owner.id,
          name: uniqueName(next, 'operations', owner.id, 'operation'),
          visibility: 'unspecified',
          returnType: null,
          parameterOrder: [],
          presentation: p,
        });
      else if (
        command.stencil === 'parameter' &&
        next.diagramType === 'class' &&
        owner.collection === 'operations'
      ) {
        const added = add2(next, 'parameters', {
          name: uniqueName(next, 'parameters', owner.id, 'parameter'),
          typeName: null,
          presentation: p,
        });
        attach2(next, added, owner);
      } else if (
        command.stencil === 'else' &&
        next.diagramType === 'sequence' &&
        owner.collection === 'events'
      ) {
        const fragment = next.content.events[owner.id];
        if (
          fragment?.type !== 'fragment' ||
          fragment.kind !== 'alt' ||
          fragment.branchOrder.length !== 1
        )
          throw new Error('Select an ALT with one branch');
        fragment.branchOrder.push(
          add2(next, 'branches', {
            kind: 'else',
            label: 'otherwise',
            events: [],
            presentation: presentation2(),
          }).id
        );
      } else throw new Error('Unsupported stencil for the current selection');
    }
  } else if (command.type === 'connect') {
    const from = getElement2(next, command.from),
      to = getElement2(next, command.to);
    if (!from || !to || command.from.collection !== command.to.collection)
      throw new Error('Select compatible endpoints');
    const value: Element2 = {
      from: command.from.id,
      to: command.to.id,
      label: '',
      presentation: presentation2(),
    };
    if (next.diagramType === 'graph' || next.diagramType === 'flowchart') {
      if (command.from.collection !== 'nodes') throw new Error('Connect nodes');
      add2(next, 'edges', value);
    } else if (next.diagramType === 'state') {
      if (command.from.collection !== 'states') throw new Error('Connect states');
      delete value.label;
      value.event = '';
      add2(next, 'transitions', value);
    } else if (next.diagramType === 'class') {
      if (command.from.collection !== 'types') throw new Error('Connect classes');
      value.kind = 'association';
      add2(next, 'relations', value);
    } else if (next.diagramType === 'er') {
      if (command.from.collection !== 'entities') throw new Error('Connect entities');
      Object.assign(value, {
        fromCardinality: 'one',
        toCardinality: 'zeroOrMore',
        identifying: true,
      });
      add2(next, 'relations', value);
    } else if (next.diagramType === 'c4') {
      if (command.from.collection !== 'elements') throw new Error('Connect C4 elements');
      value.technology = null;
      add2(next, 'relations', value);
    } else {
      if (command.from.collection !== 'participants') throw new Error('Connect participants');
      delete value.label;
      Object.assign(value, { type: 'message', kind: 'solidArrow', text: 'Message' });
      const added = add2(next, 'events', value);
      attach2(next, added, { collection: 'diagram', id: next.diagramId });
    }
  } else if (command.type === 'activation') {
    if (next.diagramType !== 'sequence' || !next.content.participants[command.participant])
      throw new Error('Select a participant');
    const owner = command.owner ?? ({ collection: 'diagram', id: next.diagramId } as ElementRef);
    const start = add2(next, 'events', {
      type: 'activationStart',
      participant: command.participant,
      presentation: presentation2(),
    });
    const end = add2(next, 'events', {
      type: 'activationEnd',
      start: start.id,
      presentation: presentation2(),
    });
    attach2(next, start, owner);
    attach2(next, end, owner);
  } else {
    const element = getElement2(next, command.target);
    if (!element) throw new Error('Element is missing');
    if (command.type === 'remove') erase2(next, command.target);
    else if (command.type === 'reorder') {
      if (!Number.isInteger(command.index) || command.index < 0) throw new Error('Invalid order');
      attach2(next, command.target, command.owner, command.index);
    } else if (command.type === 'move' || command.type === 'resize') {
      semantic = false;
      if (
        next.diagramType === 'sequence' &&
        ['events', 'branches'].includes(command.target.collection)
      )
        throw new Error('Use semantic timeline order for sequence events');
      if (next.diagramType === 'c4' && command.target.collection === 'boundaries') {
        const positions = projectScene2(next).model.nodes;
        const previous = positions[token2(command.target)]!.presentation;
        const dx = command.x - Number(previous.x),
          dy = command.y - Number(previous.y);
        for (const { ref, element: child } of elements2(next).filter((v) =>
          sameRef2(owner2(next, v.ref), command.target)
        )) {
          const position = positions[token2(ref)]!.presentation;
          Object.assign(child.presentation.properties, {
            x: numeric(Number(position.x) + dx),
            y: numeric(Number(position.y) + dy),
          });
        }
      }
      Object.assign(element.presentation.properties, {
        x: numeric(command.x),
        y: numeric(command.y),
      });
      if (command.type === 'resize') {
        if (
          !Number.isFinite(command.width) ||
          !Number.isFinite(command.height) ||
          command.width < nodeSize.minWidth ||
          command.height < nodeSize.minHeight ||
          command.width > nodeSize.max ||
          command.height > nodeSize.max
        )
          throw new Error('Invalid shape dimensions');
        Object.assign(element.presentation.properties, {
          width: numeric(command.width, nodeSize.max),
          height: numeric(command.height, nodeSize.max),
        });
      }
    } else if (command.type === 'set') {
      const field = fields2(next, command.target).find((f) => f.key === command.field);
      if (!field) throw new Error('This field is not editable');
      if (
        field.kind === 'select' &&
        command.value !== null &&
        !field.options?.some((o) => o.value === command.value)
      )
        throw new Error('Unsupported field value');
      if (
        field.kind === 'text' &&
        typeof command.value !== 'string' &&
        !(field.nullable && command.value === null)
      )
        throw new Error('Expected text');
      element[command.field] = clone2(command.value);
      if (
        next.diagramType === 'sequence' &&
        element.type === 'fragment' &&
        command.field === 'kind'
      ) {
        const ids = element.branchOrder as string[];
        if (ids.length > 1 && command.value !== 'alt')
          throw new Error('Remove the ELSE branch before changing fragment kind');
        next.content.branches[ids[0]!]!.kind = command.value === 'alt' ? 'condition' : 'body';
      }
      if (next.diagramType === 'state' && command.field === 'kind' && command.value !== 'state')
        element.name = '';
      if (next.diagramType === 'c4' && command.field === 'kind') {
        const internal = command.value === 'container' || command.value === 'database';
        const boundary = Object.keys(next.content.boundaries)[0];
        if (internal && !boundary) throw new Error('Create a boundary first');
        element.boundary = internal ? boundary : null;
        element.technology = internal ? (element.technology ?? '') : null;
      }
    }
  }
  if (next.sourceMap)
    next.sourceMap.bindings = next.sourceMap.bindings.filter((b) => !!getElement2(next, b.element));
  repairOrders2(next);
  validate2(next);
  if (JSON.stringify(next) === JSON.stringify(model)) return next;
  if (semantic && next.sourceState !== 'notApplicable') next.sourceState = 'stale';
  return next;
}
function uniqueName(
  model: DiagramRevision2,
  collection: ElementRef['collection'],
  owner: string,
  prefix: string
) {
  let index = 1;
  const names = elements2(model)
    .filter((e) => e.ref.collection === collection && owner2(model, e.ref).id === owner)
    .map((e) => e.element.name);
  while (names.includes(prefix + index)) index++;
  return prefix + index;
}
export const commonEditor2: DiagramEditorAdapter2 = {
  id: 'sddp-common-v2',
  commands: ['add', 'connect', 'set', 'move', 'resize', 'remove', 'reorder', 'activation'],
  apply: apply2,
};
export const renameEditor2: DiagramEditorAdapter2 = {
  id: 'sddp-rename-v2',
  commands: ['set'],
  apply: (model, command, revision) => {
    if (command.type !== 'set' || !['name', 'label', 'text'].includes(command.field))
      throw new Error('This editor supports renaming only');
    return apply2(model, command, revision);
  },
};
