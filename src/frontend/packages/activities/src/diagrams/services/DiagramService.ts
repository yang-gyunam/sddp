import { fetchWithAuth } from '../../shared/api';
import type { DiagramDto, DiagramListDto, DiagramListItemDto } from '../types';

/** Uses the project-scoped, permission-checked title filter for relationship targets. */
export async function searchDiagrams(
  tenantId: string,
  projectId: string,
  query: string,
  limit = 15
): Promise<DiagramListItemDto[]> {
  if (!query.trim()) return [];
  const params = new URLSearchParams({
    search: query.trim(),
    pageSize: String(Math.min(100, Math.max(1, limit))),
  });
  const result = await fetchWithAuth<DiagramListDto>(
    `/v2/projects/${encodeURIComponent(projectId)}/diagrams?${params}`,
    {
      tenantId,
      projectId,
    }
  );
  return result.items;
}

/** Reads summaries without loading the common model for every list row. */
export function listDiagrams(
  tenantId: string,
  projectId: string,
  page = 1,
  search = ''
): Promise<DiagramListDto> {
  const params = new URLSearchParams({ page: String(page), pageSize: '50' });
  if (search.trim()) params.set('search', search.trim());
  return fetchWithAuth<DiagramListDto>(
    `/v2/projects/${encodeURIComponent(projectId)}/diagrams?${params}`,
    { tenantId, projectId }
  );
}

export function getDiagram(tenantId: string, projectId: string, id: string): Promise<DiagramDto> {
  return fetchWithAuth<DiagramDto>(
    `/projects/${encodeURIComponent(projectId)}/diagrams/${encodeURIComponent(id)}`,
    { tenantId, projectId }
  );
}

export function createDiagram(
  tenantId: string,
  projectId: string,
  body: import('../types').CreateDiagramDto,
  beforeDispatch?: () => void
): Promise<DiagramDto> {
  return fetchWithAuth(`/projects/${encodeURIComponent(projectId)}/diagrams`, {
    tenantId,
    projectId,
    method: 'POST',
    body,
    beforeDispatch,
  });
}
export function saveDiagram(
  tenantId: string,
  projectId: string,
  id: string,
  body: import('../types').UpdateDiagramDto,
  beforeDispatch?: () => void
): Promise<DiagramDto> {
  return fetchWithAuth(
    `/projects/${encodeURIComponent(projectId)}/diagrams/${encodeURIComponent(id)}`,
    { tenantId, projectId, method: 'PUT', body, beforeDispatch }
  );
}
export function getDiagramVersions(
  tenantId: string,
  projectId: string,
  id: string
): Promise<import('../types').DiagramVersionListItemDto[]> {
  return fetchWithAuth(
    `/projects/${encodeURIComponent(projectId)}/diagrams/${encodeURIComponent(id)}/versions`,
    { tenantId, projectId }
  );
}
export function getDiagramVersion(
  tenantId: string,
  projectId: string,
  id: string,
  revision: number
): Promise<import('../types').DiagramVersionDto> {
  return fetchWithAuth(
    `/projects/${encodeURIComponent(projectId)}/diagrams/${encodeURIComponent(id)}/versions/${revision}`,
    { tenantId, projectId }
  );
}
export function compareDiagramVersions(
  tenantId: string,
  projectId: string,
  id: string,
  left: number,
  right: number
): Promise<import('../types').DiagramComparisonDto> {
  return fetchWithAuth(
    `/projects/${encodeURIComponent(projectId)}/diagrams/${encodeURIComponent(id)}/compare?${new URLSearchParams({ left: String(left), right: String(right) })}`,
    { tenantId, projectId }
  );
}

export function decodeDiagramYaml(
  tenantId: string,
  projectId: string,
  body: { text: string } | { contentBase64: string },
  beforeDispatch?: () => void
): Promise<{ model: import('../types').DiagramRevision }> {
  return fetchWithAuth(`/projects/${encodeURIComponent(projectId)}/diagrams/yaml/decode`, {
    tenantId,
    projectId,
    method: 'POST',
    body,
    beforeDispatch,
  });
}
export function encodeDiagramYaml(
  tenantId: string,
  projectId: string,
  model: import('../types').DiagramRevision,
  beforeDispatch?: () => void
): Promise<{ text: string }> {
  return fetchWithAuth(`/projects/${encodeURIComponent(projectId)}/diagrams/yaml/encode`, {
    tenantId,
    projectId,
    method: 'POST',
    body: { model },
    beforeDispatch,
  });
}
export function projectDiagram(
  tenantId: string,
  projectId: string,
  origins: import('../types').DiagramOrigin[],
  beforeDispatch?: () => void
): Promise<{ model: import('../types').DiagramRevision }> {
  return fetchWithAuth(`/projects/${encodeURIComponent(projectId)}/diagrams/projection`, {
    tenantId,
    projectId,
    method: 'POST',
    body: { origins },
    beforeDispatch,
  });
}
/** Preserve bytes for the server's BOM-aware strict decoder. */
export async function diagramFileContent(file: File): Promise<{ contentBase64: string }> {
  if (file.size > 2 * 1024 * 1024) throw new Error('Maximum YAML size is 2 MiB.');
  const bytes = new Uint8Array(await file.arrayBuffer());
  let binary = '';
  for (const byte of bytes) binary += String.fromCharCode(byte);
  return { contentBase64: btoa(binary) };
}
