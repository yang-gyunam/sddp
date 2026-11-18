import { fetchWithAuth } from '../../shared/api';
import type { DiagramOrigin, DiagramVersionListItemDto } from '../types';
import type {
  DiagramRevision2,
  DiagramV2Dto,
  DiagramV2ComparisonDto,
  DiagramV2VersionDto,
  DiagramSourcePreviewDto,
  DiagramIdentityPreview2,
} from '../types2';
const route = (project: string, tail = '') =>
  `/v2/projects/${encodeURIComponent(project)}/diagrams${tail}`;
export function getDiagram2(
  tenantId: string,
  projectId: string,
  id: string
): Promise<DiagramV2Dto> {
  return fetchWithAuth(route(projectId, `/${encodeURIComponent(id)}`), { tenantId, projectId });
}
export function createDiagram2(
  tenantId: string,
  projectId: string,
  body: {
    title: string;
    kind: 'authored' | 'derived';
    model: DiagramRevision2;
    changeSummary?: string;
  },
  beforeDispatch?: () => void
): Promise<DiagramV2Dto> {
  return fetchWithAuth(route(projectId), {
    tenantId,
    projectId,
    method: 'POST',
    body,
    beforeDispatch,
  });
}
export function saveDiagram2(
  tenantId: string,
  projectId: string,
  id: string,
  body: {
    title: string;
    expectedRevision: number;
    model: DiagramRevision2;
    changeSummary?: string;
  },
  beforeDispatch?: () => void
): Promise<DiagramV2Dto> {
  return fetchWithAuth(route(projectId, `/${encodeURIComponent(id)}`), {
    tenantId,
    projectId,
    method: 'PUT',
    body,
    beforeDispatch,
  });
}
export function getDiagramVersions2(
  tenantId: string,
  projectId: string,
  id: string
): Promise<DiagramVersionListItemDto[]> {
  return fetchWithAuth(route(projectId, `/${encodeURIComponent(id)}/versions`), {
    tenantId,
    projectId,
  });
}
export function getDiagramVersion2(
  tenantId: string,
  projectId: string,
  id: string,
  revision: number
): Promise<DiagramV2VersionDto> {
  return fetchWithAuth(route(projectId, `/${encodeURIComponent(id)}/versions/${revision}`), {
    tenantId,
    projectId,
  });
}
export function compareDiagramVersions2(
  tenantId: string,
  projectId: string,
  id: string,
  left: number,
  right: number
): Promise<DiagramV2ComparisonDto> {
  return fetchWithAuth(
    route(
      projectId,
      `/${encodeURIComponent(id)}/compare?${new URLSearchParams({ left: String(left), right: String(right) })}`
    ),
    { tenantId, projectId }
  );
}
export function decodeDiagramYaml2(
  tenantId: string,
  projectId: string,
  body: { text: string } | { contentBase64: string },
  beforeDispatch?: () => void
): Promise<{ model: DiagramRevision2; inputSchemaVersion: 1 | 2 }> {
  return fetchWithAuth(route(projectId, '/yaml/decode'), {
    tenantId,
    projectId,
    method: 'POST',
    body,
    beforeDispatch,
  });
}
export function encodeDiagramYaml2(
  tenantId: string,
  projectId: string,
  model: DiagramRevision2,
  beforeDispatch?: () => void
): Promise<{ text: string }> {
  return fetchWithAuth(route(projectId, '/yaml/encode'), {
    tenantId,
    projectId,
    method: 'POST',
    body: { model },
    beforeDispatch,
  });
}
export function projectDiagram2(
  tenantId: string,
  projectId: string,
  origins: DiagramOrigin[],
  beforeDispatch?: () => void
): Promise<{ model: DiagramRevision2 }> {
  return fetchWithAuth(route(projectId, '/projection'), {
    tenantId,
    projectId,
    method: 'POST',
    body: { origins },
    beforeDispatch,
  });
}
export function previewDiagramSource2(
  tenantId: string,
  projectId: string,
  body: DiagramSourcePreviewDto,
  beforeDispatch?: () => void
): Promise<DiagramIdentityPreview2> {
  return fetchWithAuth(route(projectId, '/source/preview'), {
    tenantId,
    projectId,
    method: 'POST',
    body,
    beforeDispatch,
  });
}

export function listDiagramParsers2(
  tenantId: string,
  projectId: string
): Promise<import('../types2').DiagramParserCapability2[]> {
  return fetchWithAuth(route(projectId, '/parsers'), { tenantId, projectId });
}
