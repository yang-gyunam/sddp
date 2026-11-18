import { validateShape2 } from './schema2';
import type { DiagramRevision } from '../types';
import type { DiagramRevision2, DiagramType2, DiagramPresentation2, ElementRef } from '../types2';
export type Element2 = { presentation: DiagramPresentation2 } & Record<string, unknown>;
export const clone2 = <T>(value: T): T => JSON.parse(JSON.stringify(value));
export const refKey2 = (ref: ElementRef) => JSON.stringify([ref.collection, ref.id]);
export const sameRef2 = (a: ElementRef | null | undefined, b: ElementRef | null | undefined) =>
  !!a && !!b && a.collection === b.collection && a.id === b.id;
export const presentation2 = (properties: Record<string, string> = {}): DiagramPresentation2 => ({
  properties: { ...properties },
  order: [],
});
export function emptyModel2(
  kind: DiagramType2 = 'flowchart',
  view: 'context' | 'container' = 'context'
): DiagramRevision2 {
  const base = {
    schemaVersion: 2 as const,
    diagramId: crypto.randomUUID(),
    revision: 1,
    source: { format: 'sddp-model', provider: 'sddp', providerVersion: '2', text: '' },
    sourceState: 'notApplicable' as const,
    sourceMap: null,
    presentation: presentation2(),
  };
  switch (kind) {
    case 'graph':
      return { ...base, diagramType: kind, content: { nodes: {}, edges: {} } };
    case 'flowchart':
      return { ...base, diagramType: kind, content: { nodes: {}, edges: {} } };
    case 'state':
      return { ...base, diagramType: kind, content: { states: {}, transitions: {} } };
    case 'class':
      return {
        ...base,
        diagramType: kind,
        content: { types: {}, attributes: {}, operations: {}, parameters: {}, relations: {} },
      };
    case 'er':
      return {
        ...base,
        diagramType: kind,
        content: { entities: {}, attributes: {}, relations: {} },
      };
    case 'sequence':
      return {
        ...base,
        diagramType: kind,
        content: { participants: {}, events: {}, branches: {}, timeline: [] },
      };
    case 'c4':
      return {
        ...base,
        diagramType: kind,
        content: { view, elements: {}, boundaries: {}, relations: {} },
      };
  }
}
export function promoteModel1(model: DiagramRevision): DiagramRevision2 {
  return {
    schemaVersion: 2,
    diagramId: model.diagramId,
    revision: model.revision,
    diagramType: 'graph',
    source: { ...model.source },
    sourceState: 'unknown',
    sourceMap: null,
    presentation: presentation2(model.presentation),
    content: {
      nodes: Object.fromEntries(
        Object.entries(model.nodes).map(([id, node]) => [
          id,
          { label: node.label, presentation: presentation2(node.presentation) },
        ])
      ),
      edges: Object.fromEntries(
        Object.entries(model.edges).map(([id, edge]) => [
          id,
          { ...edge, presentation: presentation2(edge.presentation) },
        ])
      ),
    },
  };
}
export function collections2(model: DiagramRevision2): Record<string, Record<string, Element2>> {
  return Object.fromEntries(
    Object.entries(model.content).filter(
      ([, value]) => value && typeof value === 'object' && !Array.isArray(value)
    )
  ) as Record<string, Record<string, Element2>>;
}
export function elements2(model: DiagramRevision2): { ref: ElementRef; element: Element2 }[] {
  return Object.entries(collections2(model)).flatMap(([collection, entries]) =>
    Object.entries(entries).map(([id, element]) => ({
      ref: { collection: collection as ElementRef['collection'], id },
      element,
    }))
  );
}
export function getElement2(model: DiagramRevision2, ref: ElementRef): Element2 | undefined {
  const entries = collections2(model)[ref.collection];
  return entries && Object.hasOwn(entries, ref.id) ? entries[ref.id] : undefined;
}
export function elementName2(element: Element2): string {
  return String(
    element.name || element.label || element.text || element.kind || element.type || 'Element'
  );
}
export function owner2(model: DiagramRevision2, ref: ElementRef): ElementRef {
  const root: ElementRef = { collection: 'diagram', id: model.diagramId };
  const element = getElement2(model, ref);
  if (!element) return root;
  if (typeof element.owner === 'string')
    return { collection: model.diagramType === 'class' ? 'types' : 'entities', id: element.owner };
  if (
    model.diagramType === 'c4' &&
    ref.collection === 'elements' &&
    typeof element.boundary === 'string'
  )
    return { collection: 'boundaries', id: element.boundary };
  for (const { ref: parent, element: p } of elements2(model))
    for (const [field, collection] of [
      ['events', 'events'],
      ['branchOrder', 'branches'],
      ['parameterOrder', 'parameters'],
    ] as const)
      if (
        ref.collection === collection &&
        Array.isArray(p[field]) &&
        (p[field] as string[]).includes(ref.id)
      )
        return parent;
  return root;
}
export function displayChildren2(model: DiagramRevision2, parent: ElementRef): ElementRef[] {
  const roots: Record<DiagramType2, string[]> = {
    graph: ['nodes'],
    flowchart: ['nodes'],
    state: ['states'],
    class: ['types'],
    er: ['entities'],
    sequence: ['participants'],
    c4: ['elements', 'boundaries'],
  };
  const allowed =
    parent.collection === 'diagram'
      ? roots[model.diagramType]
      : parent.collection === 'types'
        ? ['attributes', 'operations']
        : parent.collection === 'entities'
          ? ['attributes']
          : parent.collection === 'boundaries'
            ? ['elements']
            : [];
  return elements2(model)
    .filter(({ ref }) => allowed.includes(ref.collection) && sameRef2(owner2(model, ref), parent))
    .map((e) => e.ref)
    .sort((a, b) =>
      a.collection < b.collection
        ? -1
        : a.collection > b.collection
          ? 1
          : a.id < b.id
            ? -1
            : a.id > b.id
              ? 1
              : 0
    );
}
export function repairOrders2(model: DiagramRevision2) {
  for (const { ref, element } of [
    {
      ref: { collection: 'diagram', id: model.diagramId } as ElementRef,
      element: { presentation: model.presentation } as Element2,
    },
    ...elements2(model),
  ]) {
    if (!element.presentation.order.length) continue;
    const eligible = displayChildren2(model, ref);
    const filtered = element.presentation.order.filter((r) => eligible.some((e) => sameRef2(e, r)));
    element.presentation.order = [
      ...filtered,
      ...eligible.filter((e) => !filtered.some((r) => sameRef2(e, r))),
    ];
  }
}
function require2(valid: unknown, message: string): asserts valid {
  if (!valid) throw new Error(message);
}
export function validate2(model: DiagramRevision2): void {
  validateShape2(model);
  require2(
    model?.schemaVersion === 2 &&
      model.diagramId &&
      Number.isInteger(model.revision) &&
      model.revision > 0,
    'Invalid diagram identity or schema'
  );
  require2(
    model.source?.text != null &&
      model.source.format &&
      model.source.provider &&
      model.source.providerVersion,
    'Incomplete source metadata'
  );
  const maps = collections2(model);
  const all = elements2(model);
  const exists = (collection: string, id: unknown) =>
    typeof id === 'string' && !!maps[collection] && Object.hasOwn(maps[collection]!, id);
  for (const { ref, element: e } of all) {
    require2(
      ref.id.trim() &&
        e?.presentation &&
        e.presentation.properties &&
        Array.isArray(e.presentation.order),
      'Invalid element or presentation'
    );
    require2(
      Object.entries(e.presentation.properties).every(
        ([k, v]) => k.trim() && typeof v === 'string'
      ),
      'Invalid display property'
    );
    if ('from' in e || 'to' in e) {
      const target =
        model.diagramType === 'sequence'
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
      require2(exists(target, e.from) && exists(target, e.to), 'Connection endpoints must exist');
    }
    if ('owner' in e)
      require2(
        exists(model.diagramType === 'class' ? 'types' : 'entities', e.owner),
        'Member owner must exist'
      );
  }
  for (const { ref, element } of [
    {
      ref: { collection: 'diagram', id: model.diagramId } as ElementRef,
      element: { presentation: model.presentation } as Element2,
    },
    ...all,
  ]) {
    const order = element.presentation.order;
    require2(Array.isArray(order), 'Invalid display order');
    if (order.length) {
      const eligible = displayChildren2(model, ref);
      require2(
        order.length === eligible.length &&
          new Set(order.map(refKey2)).size === order.length &&
          order.every((r) => eligible.some((e) => sameRef2(e, r))),
        'Display order must contain each child once'
      );
    }
  }
  if (model.sourceMap) {
    const source = model.sourceMap;
    require2(
      /^sha256:[a-f0-9]{64}$/.test(source.sourceHash) &&
        source.profile.id.trim() &&
        source.profile.version > 0 &&
        source.adapter.id.trim() &&
        source.adapter.version.trim(),
      'Invalid source metadata'
    );
    const targets = new Set<string>();
    const keys = new Set<string>();
    for (const binding of source.bindings) {
      const key = refKey2(binding.element);
      require2(getElement2(model, binding.element) && !targets.has(key), 'Invalid source binding');
      targets.add(key);
      if (binding.key) {
        const named = JSON.stringify(binding.key);
        require2(
          binding.key.length && binding.key.every((part) => part.trim()) && !keys.has(named),
          'Invalid source key'
        );
        keys.add(named);
      }
      if (binding.range)
        require2(
          binding.range.start >= 0 &&
            binding.range.end >= binding.range.start &&
            binding.range.end <= model.source.text.length,
          'Invalid UTF16 source range'
        );
    }
  }
  if (model.diagramType === 'flowchart')
    for (const node of Object.values(model.content.nodes))
      require2(
        ['process', 'terminator', 'decision'].includes(node.kind) && typeof node.name === 'string',
        'Invalid flowchart node'
      );
  if (model.diagramType === 'state') {
    for (const kind of ['initial', 'final'])
      require2(
        Object.values(model.content.states).filter((n) => n.kind === kind).length <= 1,
        'Only one initial/final state is allowed'
      );
    for (const [id, state] of Object.entries(model.content.states)) {
      require2(['state', 'initial', 'final'].includes(state.kind), 'Unsupported state kind');
      if (state.kind !== 'state') require2(state.name === '', 'Pseudo states have no name');
      if (state.kind === 'initial')
        require2(
          !Object.values(model.content.transitions).some((e) => e.to === id) &&
            Object.values(model.content.transitions).filter((e) => e.from === id).length <= 1,
          'Invalid initial state connections'
        );
      if (state.kind === 'final')
        require2(
          !Object.values(model.content.transitions).some((e) => e.from === id),
          'Final states cannot have outgoing transitions'
        );
    }
  }
  if (model.diagramType === 'class') {
    const seen = new Set<string>();
    const names = new Set<string>();
    for (const attr of Object.values(model.content.attributes)) {
      const key = JSON.stringify([attr.owner, attr.name]);
      require2(!names.has(key), 'Duplicate attribute name');
      names.add(key);
    }
    const signatures = new Set<string>();
    for (const op of Object.values(model.content.operations)) {
      const parameterNames = new Set<string>();
      for (const id of op.parameterOrder) {
        require2(exists('parameters', id) && !seen.has(id), 'Invalid parameter ownership');
        seen.add(id);
        const p = model.content.parameters[id]!;
        require2(!parameterNames.has(p.name), 'Duplicate parameter name');
        parameterNames.add(p.name);
      }
      if (op.parameterOrder.every((id) => model.content.parameters[id]!.typeName !== null)) {
        const signature = JSON.stringify([
          op.owner,
          op.name,
          op.parameterOrder.map((id) => model.content.parameters[id]!.typeName),
        ]);
        require2(!signatures.has(signature), 'Duplicate operation signature');
        signatures.add(signature);
      }
    }
    require2(seen.size === Object.keys(model.content.parameters).length, 'Orphan parameters');
  }
  if (model.diagramType === 'er') {
    const names = new Set<string>();
    for (const a of Object.values(model.content.attributes)) {
      const key = JSON.stringify([a.owner, a.name]);
      require2(!names.has(key), 'Duplicate attribute name');
      names.add(key);
      require2(
        new Set(a.keys).size === a.keys.length &&
          a.keys.every((k) => ['primary', 'foreign', 'unique'].includes(k)),
        'Invalid attribute keys'
      );
    }
    for (const r of Object.values(model.content.relations))
      require2(
        [r.fromCardinality, r.toCardinality].every((c) =>
          ['zeroOrOne', 'one', 'zeroOrMore', 'oneOrMore'].includes(c)
        ),
        'Invalid cardinality'
      );
  }
  if (model.diagramType === 'c4') {
    require2(
      Object.keys(model.content.boundaries).length <= (model.content.view === 'context' ? 0 : 1),
      'Only one container boundary is supported'
    );
    for (const e of Object.values(model.content.elements)) {
      const internal = e.kind === 'container' || e.kind === 'database';
      require2(
        ['person', 'system', 'externalSystem', 'container', 'database'].includes(e.kind),
        'Invalid C4 kind'
      );
      require2(
        internal
          ? model.content.view === 'container' && exists('boundaries', e.boundary)
          : e.boundary === null && e.technology === null,
        'Invalid C4 element boundary/technology'
      );
    }
  }
  if (model.diagramType === 'sequence') {
    const visited = new Set<string>();
    const branches = new Set<string>();
    const walk = (ids: string[], depth: number) => {
      require2(depth <= 64, 'Fragment nesting limit');
      const stacks = new Map<string, string[]>();
      for (const id of ids) {
        require2(
          !visited.has(id) && Object.hasOwn(model.content.events, id),
          'Invalid/repeated event'
        );
        visited.add(id);
        const e = model.content.events[id]!;
        if (e.type === 'message')
          require2(['solidArrow', 'dashedArrow'].includes(e.kind), 'Invalid message kind');
        if (e.type === 'activationStart') {
          require2(exists('participants', e.participant), 'Missing activation participant');
          const stack = stacks.get(e.participant) ?? [];
          stack.push(id);
          stacks.set(e.participant, stack);
        }
        if (e.type === 'activationEnd') {
          const start = model.content.events[e.start];
          require2(start?.type === 'activationStart', 'Missing activation start');
          require2(
            stacks.get(start.participant)?.pop() === e.start,
            'Activation must close in the same scope and LIFO order'
          );
        }
        if (e.type === 'fragment') {
          require2(
            e.branchOrder.length === 1 || (e.kind === 'alt' && e.branchOrder.length === 2),
            'Invalid fragment branches'
          );
          e.branchOrder.forEach((bid, index) => {
            const branch = model.content.branches[bid];
            require2(branch && !branches.has(bid), 'Invalid branch ownership');
            branches.add(bid);
            require2(
              branch.kind === (e.kind === 'alt' ? (index === 0 ? 'condition' : 'else') : 'body'),
              'Invalid branch kind'
            );
            walk(branch.events, depth + 1);
          });
        }
      }
      require2(
        [...stacks.values()].every((stack) => !stack.length),
        'Unclosed activation'
      );
    };
    walk(model.content.timeline, 0);
    require2(
      visited.size === Object.keys(model.content.events).length &&
        branches.size === Object.keys(model.content.branches).length,
      'Orphan timeline element'
    );
  }
}
