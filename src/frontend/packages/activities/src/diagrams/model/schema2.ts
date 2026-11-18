// Closed SDDP record shapes; semantic/ownership checks live in model2.ts.
const records: Record<string, Record<string, string>> = {
  ElementRef: {
    collection: 'string',
    id: 'string',
  },
  DiagramPresentation2: {
    properties: 'Dictionary<string, string>',
    order: 'List<ElementRef>',
  },
  DiagramProfile2: {
    id: 'string',
    version: 'int',
  },
  DiagramAdapter2: {
    id: 'string',
    version: 'string',
  },
  DiagramSourceRange2: {
    start: 'int',
    end: 'int',
  },
  DiagramBinding2: {
    element: 'ElementRef',
    key: 'List<string>?',
    range: 'DiagramSourceRange2?',
  },
  DiagramSourceMap2: {
    sourceHash: 'string',
    profile: 'DiagramProfile2',
    adapter: 'DiagramAdapter2',
    bindings: 'List<DiagramBinding2>',
  },
  DiagramRevision2: {
    schemaVersion: 'int',
    diagramId: 'string',
    revision: 'int',
    diagramType: 'string',
    source: 'DiagramSource',
    sourceState: 'string',
    sourceMap: 'DiagramSourceMap2?',
    presentation: 'DiagramPresentation2',
    content: 'DiagramContent2',
  },
  GraphContent2: {
    nodes: 'Dictionary<string, GraphNode2>',
    edges: 'Dictionary<string, GraphEdge2>',
  },
  GraphNode2: {
    label: 'string',
    presentation: 'DiagramPresentation2',
  },
  GraphEdge2: {
    from: 'string',
    to: 'string',
    label: 'string',
    presentation: 'DiagramPresentation2',
  },
  FlowchartContent2: {
    nodes: 'Dictionary<string, FlowNode2>',
    edges: 'Dictionary<string, FlowEdge2>',
  },
  FlowNode2: {
    name: 'string',
    kind: 'string',
    presentation: 'DiagramPresentation2',
  },
  FlowEdge2: {
    from: 'string',
    to: 'string',
    label: 'string',
    presentation: 'DiagramPresentation2',
  },
  SequenceContent2: {
    participants: 'Dictionary<string, SequenceParticipant2>',
    events: 'Dictionary<string, SequenceEvent2>',
    branches: 'Dictionary<string, SequenceBranch2>',
    timeline: 'List<string>',
  },
  SequenceParticipant2: {
    name: 'string',
    kind: 'string',
    presentation: 'DiagramPresentation2',
  },
  SequenceBranch2: {
    kind: 'string',
    label: 'string',
    events: 'List<string>',
    presentation: 'DiagramPresentation2',
  },
  SequenceMessage2: {
    from: 'string',
    to: 'string',
    kind: 'string',
    text: 'string',
    presentation: 'DiagramPresentation2',
    type: 'string',
  },
  SequenceActivationStart2: {
    participant: 'string',
    presentation: 'DiagramPresentation2',
    type: 'string',
  },
  SequenceActivationEnd2: {
    start: 'string',
    presentation: 'DiagramPresentation2',
    type: 'string',
  },
  SequenceFragment2: {
    kind: 'string',
    branchOrder: 'List<string>',
    presentation: 'DiagramPresentation2',
    type: 'string',
  },
  ClassContent2: {
    types: 'Dictionary<string, ClassType2>',
    attributes: 'Dictionary<string, ClassAttribute2>',
    operations: 'Dictionary<string, ClassOperation2>',
    parameters: 'Dictionary<string, ClassParameter2>',
    relations: 'Dictionary<string, ClassRelation2>',
  },
  ClassType2: {
    name: 'string',
    presentation: 'DiagramPresentation2',
  },
  ClassAttribute2: {
    owner: 'string',
    name: 'string',
    typeName: 'string',
    visibility: 'string',
    presentation: 'DiagramPresentation2',
  },
  ClassOperation2: {
    owner: 'string',
    name: 'string',
    visibility: 'string',
    returnType: 'string?',
    parameterOrder: 'List<string>',
    presentation: 'DiagramPresentation2',
  },
  ClassParameter2: {
    name: 'string',
    typeName: 'string?',
    presentation: 'DiagramPresentation2',
  },
  ClassRelation2: {
    from: 'string',
    to: 'string',
    kind: 'string',
    label: 'string',
    presentation: 'DiagramPresentation2',
  },
  StateContent2: {
    states: 'Dictionary<string, StateNode2>',
    transitions: 'Dictionary<string, StateTransition2>',
  },
  StateNode2: {
    name: 'string',
    kind: 'string',
    presentation: 'DiagramPresentation2',
  },
  StateTransition2: {
    from: 'string',
    to: 'string',
    event: 'string',
    presentation: 'DiagramPresentation2',
  },
  ErContent2: {
    entities: 'Dictionary<string, ErEntity2>',
    attributes: 'Dictionary<string, ErAttribute2>',
    relations: 'Dictionary<string, ErRelation2>',
  },
  ErEntity2: {
    name: 'string',
    presentation: 'DiagramPresentation2',
  },
  ErAttribute2: {
    owner: 'string',
    name: 'string',
    typeName: 'string',
    keys: 'List<string>',
    presentation: 'DiagramPresentation2',
  },
  ErRelation2: {
    from: 'string',
    to: 'string',
    label: 'string',
    fromCardinality: 'string',
    toCardinality: 'string',
    identifying: 'bool',
    presentation: 'DiagramPresentation2',
  },
  C4Content2: {
    view: 'string',
    elements: 'Dictionary<string, C4Element2>',
    boundaries: 'Dictionary<string, C4Boundary2>',
    relations: 'Dictionary<string, C4Relation2>',
  },
  C4Element2: {
    name: 'string',
    kind: 'string',
    description: 'string?',
    technology: 'string?',
    boundary: 'string?',
    presentation: 'DiagramPresentation2',
  },
  C4Boundary2: {
    name: 'string',
    presentation: 'DiagramPresentation2',
  },
  C4Relation2: {
    from: 'string',
    to: 'string',
    label: 'string',
    technology: 'string?',
    presentation: 'DiagramPresentation2',
  },
  DiagramSource: {
    format: 'string',
    provider: 'string',
    providerVersion: 'string',
    text: 'string',
  },
};
const content: Record<string, string> = {
  graph: 'GraphContent2',
  flowchart: 'FlowchartContent2',
  sequence: 'SequenceContent2',
  class: 'ClassContent2',
  state: 'StateContent2',
  er: 'ErContent2',
  c4: 'C4Content2',
};
const events: Record<string, string> = {
  message: 'SequenceMessage2',
  activationStart: 'SequenceActivationStart2',
  activationEnd: 'SequenceActivationEnd2',
  fragment: 'SequenceFragment2',
};
const allowed: Record<string, string[]> = {
  'ElementRef.collection': [
    'diagram',
    'nodes',
    'edges',
    'participants',
    'events',
    'branches',
    'types',
    'attributes',
    'operations',
    'parameters',
    'relations',
    'states',
    'transitions',
    'entities',
    'elements',
    'boundaries',
  ],
  'FlowNode2.kind': ['process', 'terminator', 'decision'],
  'SequenceParticipant2.kind': ['actor', 'participant'],
  'SequenceBranch2.kind': ['condition', 'else', 'body'],
  'SequenceMessage2.kind': ['solidArrow', 'dashedArrow'],
  'SequenceFragment2.kind': ['alt', 'opt', 'loop'],
  'ClassAttribute2.visibility': ['public', 'private', 'protected', 'internal', 'unspecified'],
  'ClassOperation2.visibility': ['public', 'private', 'protected', 'internal', 'unspecified'],
  'ClassRelation2.kind': [
    'inheritance',
    'composition',
    'aggregation',
    'association',
    'dependency',
    'realization',
  ],
  'StateNode2.kind': ['state', 'initial', 'final'],
  'C4Content2.view': ['context', 'container'],
  'C4Element2.kind': ['person', 'system', 'externalSystem', 'container', 'database'],
  'DiagramRevision2.sourceState': ['aligned', 'stale', 'unknown', 'notApplicable'],
  'ErRelation2.fromCardinality': ['zeroOrOne', 'one', 'zeroOrMore', 'oneOrMore'],
  'ErRelation2.toCardinality': ['zeroOrOne', 'one', 'zeroOrMore', 'oneOrMore'],
};
export function validateShape2(input: unknown): void {
  const visit = (value: unknown, type: string, depth: number, root: Record<string, unknown>) => {
    if (depth > 64) throw new Error('Model nesting limit');
    if (type.endsWith('?')) {
      if (value === null) return;
      type = type.slice(0, -1);
    }
    if (type === 'string') {
      if (typeof value !== 'string') throw new Error('Expected string');
      return;
    }
    if (type === 'int') {
      if (!Number.isInteger(value)) throw new Error('Expected integer');
      return;
    }
    if (type === 'bool') {
      if (typeof value !== 'boolean') throw new Error('Expected boolean');
      return;
    }
    if (type.startsWith('List<')) {
      if (!Array.isArray(value)) throw new Error('Expected list');
      for (const item of value) visit(item, type.slice(5, -1), depth + 1, root);
      return;
    }
    if (value === null || typeof value !== 'object' || Array.isArray(value))
      throw new Error('Expected record');
    const object = value as Record<string, unknown>;
    if (type.startsWith('Dictionary<string, ')) {
      for (const [key, item] of Object.entries(object)) {
        if (!key.trim()) throw new Error('Empty map key');
        visit(item, type.slice(19, -1), depth + 1, root);
      }
      return;
    }
    if (type === 'DiagramContent2') type = content[String(root.diagramType)] ?? '';
    if (type === 'SequenceEvent2') type = events[String(object.type)] ?? '';
    const shape = records[type];
    if (!shape) throw new Error('Unsupported diagram record');
    for (const key of Object.keys(shape)) {
      if (!Object.hasOwn(object, key)) throw new Error('Missing ' + key);
      if (allowed[type + '.' + key] && !allowed[type + '.' + key]!.includes(String(object[key])))
        throw new Error('Unsupported ' + key);
      visit(object[key], shape[key]!, depth + 1, root);
    }
    if (Object.keys(object).some((key) => !Object.hasOwn(shape, key)))
      throw new Error('Unknown semantic field');
  };
  if (input === null || typeof input !== 'object') throw new Error('Expected model');
  visit(input, 'DiagramRevision2', 0, input as Record<string, unknown>);
}
