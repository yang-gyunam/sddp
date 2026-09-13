import { fetchWithAuth } from '../../shared/api/fetchWithAuth';
import type {
  CreateKnowledgePageRequest,
  KnowledgePage,
  KnowledgePageTreeItem,
  KnowledgePageVersion,
  KnowledgePageVersionSummary,
  KnowledgeSearchResult,
  UpdateKnowledgePageRequest,
} from '../types';

function basePath(projectId: string): string {
  return `/projects/${encodeURIComponent(projectId)}/knowledge`;
}

export function getKnowledgeTree(
  projectId: string,
  includeArchived = false
): Promise<KnowledgePageTreeItem[]> {
  return fetchWithAuth(`${basePath(projectId)}/tree?includeArchived=${includeArchived}`, {
    projectId,
  });
}

export function getKnowledgePage(
  projectId: string,
  pageId: string,
  includeArchived = false
): Promise<KnowledgePage> {
  return fetchWithAuth(
    `${basePath(projectId)}/pages/${encodeURIComponent(pageId)}?includeArchived=${includeArchived}`,
    { projectId }
  );
}

export function createKnowledgePage(
  projectId: string,
  request: CreateKnowledgePageRequest
): Promise<KnowledgePage> {
  return fetchWithAuth(`${basePath(projectId)}/pages`, {
    method: 'POST',
    body: request,
    projectId,
  });
}

export function updateKnowledgePage(
  projectId: string,
  pageId: string,
  revision: number,
  request: UpdateKnowledgePageRequest
): Promise<KnowledgePage> {
  return fetchWithAuth(`${basePath(projectId)}/pages/${encodeURIComponent(pageId)}`, {
    method: 'PUT',
    body: request,
    projectId,
    headers: { 'If-Match': `"${revision}"` },
  });
}

export function publishKnowledgePage(
  projectId: string,
  pageId: string,
  revision: number
): Promise<KnowledgePage> {
  return fetchWithAuth(`${basePath(projectId)}/pages/${encodeURIComponent(pageId)}/publish`, {
    method: 'POST',
    projectId,
    headers: { 'If-Match': `"${revision}"` },
  });
}

export function moveKnowledgePage(
  projectId: string,
  pageId: string,
  revision: number,
  parentId: string | null,
  position: number
): Promise<KnowledgePage> {
  return fetchWithAuth(`${basePath(projectId)}/pages/${encodeURIComponent(pageId)}/move`, {
    method: 'POST',
    body: { parentId, position },
    projectId,
    headers: { 'If-Match': `"${revision}"` },
  });
}

export function archiveKnowledgePage(
  projectId: string,
  pageId: string,
  revision: number,
  includeDescendants: boolean
): Promise<KnowledgePage> {
  return fetchWithAuth(`${basePath(projectId)}/pages/${encodeURIComponent(pageId)}/archive`, {
    method: 'POST',
    body: { includeDescendants },
    projectId,
    headers: { 'If-Match': `"${revision}"` },
  });
}

export function restoreKnowledgePage(
  projectId: string,
  pageId: string,
  revision: number
): Promise<KnowledgePage> {
  return fetchWithAuth(`${basePath(projectId)}/pages/${encodeURIComponent(pageId)}/restore`, {
    method: 'POST',
    projectId,
    headers: { 'If-Match': `"${revision}"` },
  });
}

export function getKnowledgeVersions(
  projectId: string,
  pageId: string,
  includeArchived = false
): Promise<KnowledgePageVersionSummary[]> {
  return fetchWithAuth(
    `${basePath(projectId)}/pages/${encodeURIComponent(pageId)}/versions?includeArchived=${includeArchived}`,
    { projectId }
  );
}

export function getKnowledgeVersion(
  projectId: string,
  pageId: string,
  revision: number,
  includeArchived = false
): Promise<KnowledgePageVersion> {
  return fetchWithAuth(
    `${basePath(projectId)}/pages/${encodeURIComponent(pageId)}/versions/${revision}?includeArchived=${includeArchived}`,
    { projectId }
  );
}

export function restoreKnowledgeVersion(
  projectId: string,
  pageId: string,
  revision: number,
  expectedRevision: number
): Promise<KnowledgePage> {
  return fetchWithAuth(
    `${basePath(projectId)}/pages/${encodeURIComponent(pageId)}/versions/${revision}/restore`,
    {
      method: 'POST',
      projectId,
      headers: { 'If-Match': `"${expectedRevision}"` },
    }
  );
}

export function searchKnowledge(
  projectId: string,
  query: string
): Promise<KnowledgeSearchResult[]> {
  const params = new URLSearchParams({ q: query, limit: '50' });
  return fetchWithAuth(`${basePath(projectId)}/search?${params.toString()}`, { projectId });
}
