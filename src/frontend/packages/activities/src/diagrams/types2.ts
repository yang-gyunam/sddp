import type {
  DiagramRevision,
  DiagramDto,
  DiagramListItemDto,
  DiagramVersionListItemDto,
} from './types';

export type DiagramContent2 =
  | GraphContent2
  | FlowchartContent2
  | SequenceContent2
  | ClassContent2
  | StateContent2
  | ErContent2
  | C4Content2;
export type SequenceEvent2 =
  SequenceMessage2 | SequenceActivationStart2 | SequenceActivationEnd2 | SequenceFragment2;

export interface ElementRef {
  collection:
    | 'diagram'
    | 'nodes'
    | 'edges'
    | 'participants'
    | 'events'
    | 'branches'
    | 'types'
    | 'attributes'
    | 'operations'
    | 'parameters'
    | 'relations'
    | 'states'
    | 'transitions'
    | 'entities'
    | 'elements'
    | 'boundaries';
  id: string;
}

export interface DiagramPresentation2 {
  properties: Record<string, string>;
  order: ElementRef[];
}

export interface DiagramProfile2 {
  id: string;
  version: number;
}

export interface DiagramAdapter2 {
  id: string;
  version: string;
}

export interface DiagramSourceRange2 {
  start: number;
  end: number;
}

export interface DiagramBinding2 {
  element: ElementRef;
  key: string[] | null;
  range: DiagramSourceRange2 | null;
}

export interface DiagramSourceMap2 {
  sourceHash: string;
  profile: DiagramProfile2;
  adapter: DiagramAdapter2;
  bindings: DiagramBinding2[];
}

export interface GraphContent2 {
  nodes: Record<string, GraphNode2>;
  edges: Record<string, GraphEdge2>;
}

export interface GraphNode2 {
  label: string;
  presentation: DiagramPresentation2;
}

export interface GraphEdge2 {
  from: string;
  to: string;
  label: string;
  presentation: DiagramPresentation2;
}

export interface FlowchartContent2 {
  nodes: Record<string, FlowNode2>;
  edges: Record<string, FlowEdge2>;
}

export interface FlowNode2 {
  name: string;
  kind: 'process' | 'terminator' | 'decision';
  presentation: DiagramPresentation2;
}

export interface FlowEdge2 {
  from: string;
  to: string;
  label: string;
  presentation: DiagramPresentation2;
}

export interface SequenceContent2 {
  participants: Record<string, SequenceParticipant2>;
  events: Record<string, SequenceEvent2>;
  branches: Record<string, SequenceBranch2>;
  timeline: string[];
}

export interface SequenceParticipant2 {
  name: string;
  kind: 'actor' | 'participant';
  presentation: DiagramPresentation2;
}

export interface SequenceBranch2 {
  kind: 'condition' | 'else' | 'body';
  label: string;
  events: string[];
  presentation: DiagramPresentation2;
}

export interface SequenceMessage2 {
  type: 'message';
  from: string;
  to: string;
  kind: 'solidArrow' | 'dashedArrow';
  text: string;
  presentation: DiagramPresentation2;
}

export interface SequenceActivationStart2 {
  type: 'activationStart';
  participant: string;
  presentation: DiagramPresentation2;
}

export interface SequenceActivationEnd2 {
  type: 'activationEnd';
  start: string;
  presentation: DiagramPresentation2;
}

export interface SequenceFragment2 {
  type: 'fragment';
  kind: 'alt' | 'opt' | 'loop';
  branchOrder: string[];
  presentation: DiagramPresentation2;
}

export interface ClassContent2 {
  types: Record<string, ClassType2>;
  attributes: Record<string, ClassAttribute2>;
  operations: Record<string, ClassOperation2>;
  parameters: Record<string, ClassParameter2>;
  relations: Record<string, ClassRelation2>;
}

export interface ClassType2 {
  name: string;
  presentation: DiagramPresentation2;
}

export interface ClassAttribute2 {
  owner: string;
  name: string;
  typeName: string;
  visibility: 'public' | 'private' | 'protected' | 'internal' | 'unspecified';
  presentation: DiagramPresentation2;
}

export interface ClassOperation2 {
  owner: string;
  name: string;
  visibility: 'public' | 'private' | 'protected' | 'internal' | 'unspecified';
  returnType: string | null;
  parameterOrder: string[];
  presentation: DiagramPresentation2;
}

export interface ClassParameter2 {
  name: string;
  typeName: string | null;
  presentation: DiagramPresentation2;
}

export interface ClassRelation2 {
  from: string;
  to: string;
  kind:
    'inheritance' | 'composition' | 'aggregation' | 'association' | 'dependency' | 'realization';
  label: string;
  presentation: DiagramPresentation2;
}

export interface StateContent2 {
  states: Record<string, StateNode2>;
  transitions: Record<string, StateTransition2>;
}

export interface StateNode2 {
  name: string;
  kind: 'state' | 'initial' | 'final';
  presentation: DiagramPresentation2;
}

export interface StateTransition2 {
  from: string;
  to: string;
  event: string;
  presentation: DiagramPresentation2;
}

export interface ErContent2 {
  entities: Record<string, ErEntity2>;
  attributes: Record<string, ErAttribute2>;
  relations: Record<string, ErRelation2>;
}

export interface ErEntity2 {
  name: string;
  presentation: DiagramPresentation2;
}

export interface ErAttribute2 {
  owner: string;
  name: string;
  typeName: string;
  keys: ('primary' | 'foreign' | 'unique')[];
  presentation: DiagramPresentation2;
}

export interface ErRelation2 {
  from: string;
  to: string;
  label: string;
  fromCardinality: 'zeroOrOne' | 'one' | 'zeroOrMore' | 'oneOrMore';
  toCardinality: 'zeroOrOne' | 'one' | 'zeroOrMore' | 'oneOrMore';
  identifying: boolean;
  presentation: DiagramPresentation2;
}

export interface C4Content2 {
  view: 'context' | 'container';
  elements: Record<string, C4Element2>;
  boundaries: Record<string, C4Boundary2>;
  relations: Record<string, C4Relation2>;
}

export interface C4Element2 {
  name: string;
  kind: 'person' | 'system' | 'externalSystem' | 'container' | 'database';
  description: string | null;
  technology: string | null;
  boundary: string | null;
  presentation: DiagramPresentation2;
}

export interface C4Boundary2 {
  name: string;
  presentation: DiagramPresentation2;
}

export interface C4Relation2 {
  from: string;
  to: string;
  label: string;
  technology: string | null;
  presentation: DiagramPresentation2;
}

export interface DiagramEnvelope2 {
  schemaVersion: 2;
  diagramId: string;
  revision: number;
  source: DiagramRevision['source'];
  sourceState: 'aligned' | 'stale' | 'unknown' | 'notApplicable';
  sourceMap: DiagramSourceMap2 | null;
  presentation: DiagramPresentation2;
}
export type DiagramRevision2 = DiagramEnvelope2 &
  (
    | { diagramType: 'graph'; content: GraphContent2 }
    | { diagramType: 'flowchart'; content: FlowchartContent2 }
    | { diagramType: 'sequence'; content: SequenceContent2 }
    | { diagramType: 'class'; content: ClassContent2 }
    | { diagramType: 'state'; content: StateContent2 }
    | { diagramType: 'er'; content: ErContent2 }
    | { diagramType: 'c4'; content: C4Content2 }
  );
export type DiagramType2 = DiagramRevision2['diagramType'];

export type DiagramV2Dto = Omit<DiagramDto, 'model'> & {
  storedSchemaVersion: 1 | 2;
  model: DiagramRevision2;
};
export type DiagramV2ListItemDto = DiagramListItemDto & {
  storedSchemaVersion: 1 | 2;
  diagramType: DiagramType2;
};
export type DiagramV2VersionDto = DiagramVersionListItemDto & {
  storedSchemaVersion: 1 | 2;
  model: DiagramRevision2;
};
export interface DiagramChange2 {
  target: ElementRef;
  field: string[] | null;
  kind: 'added' | 'removed' | 'modified';
  category: 'semantic' | 'presentation';
  before: unknown;
  after: unknown;
}
export interface DiagramProvenanceChange2 {
  field: string[];
  before: unknown;
  after: unknown;
}
export interface DiagramChangeSet2 {
  changes: DiagramChange2[];
  provenanceChanges: DiagramProvenanceChange2[];
}
export type DiagramV2ComparisonDto = DiagramChangeSet2 & {
  diagramId: string;
  leftRevision: number;
  rightRevision: number;
};
export interface DiagramDiagnostic2 {
  code: string;
  severity: 'error' | 'warning' | 'info';
  message: string;
  sourceRange: DiagramSourceRange2 | null;
  modelPath: string[] | null;
  target: ElementRef | null;
}
export interface DiagramIdentityMapping2 {
  candidateKey: string;
  target: ElementRef | null;
}
export interface DiagramIdentityResolution2 {
  fingerprint: string;
  mappings: DiagramIdentityMapping2[];
  removed: ElementRef[];
}
export interface DiagramIdentityItem2 {
  candidateKey: string;
  collection: ElementRef['collection'];
  label: string;
  reason: string;
  target: ElementRef | null;
  candidates: ElementRef[];
}
export interface DiagramIdentityPreview2 {
  fingerprint: string;
  candidate: DiagramRevision2 | null;
  diagnostics: DiagramDiagnostic2[];
  identityReport: DiagramIdentityItem2[];
  removed: ElementRef[];
  changes: DiagramChangeSet2 | null;
}
export interface DiagramSourcePreviewDto {
  text: string;
  profile?: DiagramProfile2 | null;
  adapterId?: string;
  diagramId?: string;
  expectedRevision?: number;
  baseline?: DiagramRevision2;
  resolution?: DiagramIdentityResolution2;
}

export interface DiagramParserCapability2 {
  id: string;
  version: string;
  profiles: DiagramProfile2[];
  generatesEditedSource: boolean;
}
