/** Versioned, provider-independent diagram model shared with the backend. */
export interface DiagramRevision {
  schemaVersion: number;
  diagramId: string;
  revision: number;
  source: { format: string; provider: string; providerVersion: string; text: string };
  presentation: Record<string, string>;
  nodes: Record<string, { label: string; presentation: Record<string, string> }>;
  edges: Record<
    string,
    { from: string; to: string; label: string; presentation: Record<string, string> }
  >;
}

export interface CreateDiagramDto {
  title: string;
  kind: 'authored' | 'derived';
  model: DiagramRevision;
  changeSummary?: string | null;
}

export interface UpdateDiagramDto {
  title: string;
  expectedRevision: number;
  model: DiagramRevision;
  changeSummary?: string | null;
}

export interface DiagramDto {
  id: string;
  title: string;
  kind: 'authored' | 'derived';
  revision: number;
  model: DiagramRevision;
  createdBy: string;
  updatedBy: string;
  createdAt: string;
  updatedAt: string;
}

export type DiagramListItemDto = Pick<
  DiagramDto,
  'id' | 'title' | 'kind' | 'revision' | 'updatedAt'
>;
export interface DiagramListDto {
  items: DiagramListItemDto[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface DiagramVersionListItemDto {
  revision: number;
  title: string;
  changeSummary: string | null;
  createdBy: string;
  createdAt: string;
}
export interface DiagramVersionDto extends DiagramVersionListItemDto {
  model: DiagramRevision;
}

export interface DiagramChange {
  target: 'diagram' | 'node' | 'edge';
  id: string;
  field: string | null;
  kind: 'added' | 'removed' | 'modified';
  category: 'semantic' | 'presentation';
  before: string | DiagramRevision['nodes'][string] | DiagramRevision['edges'][string] | null;
  after: string | DiagramRevision['nodes'][string] | DiagramRevision['edges'][string] | null;
}

export interface DiagramComparisonDto {
  diagramId: string;
  leftRevision: number;
  rightRevision: number;
  changes: DiagramChange[];
}

/** Existing relationship enum values, without recursive Diagram projections. */
export interface DiagramOrigin {
  type: number;
  id: string;
}
