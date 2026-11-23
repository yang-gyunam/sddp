import { fetchWithAuth } from '../../shared/api';
import type { ProjectPageType } from '../types';

export type SearchMenu = Exclude<ProjectPageType, 'search'>;
export interface ProjectSearchHit {
  menu: SearchMenu;
  key: string;
  target: string;
  kind: string;
  title: string;
  code: string;
  snippet: string;
  location: string;
  status: string;
  version: string;
  updatedAt: string;
  line?: number | null;
  createdAt?: string | null;
  revision?: string | null;
}
export interface ProjectSearchPage {
  items: ProjectSearchHit[];
  counts: Partial<Record<SearchMenu, number>>;
  nextOffset: number | null;
  nextCursor: string | null;
  terms: string[];
  filesUnavailable: boolean;
}
export interface ProjectSearchOptions {
  q: string;
  mode: 'AND' | 'OR';
  field: 'all' | 'title' | 'body' | 'path';
  menu: SearchMenu | 'all';
  sort: 'relevance' | 'date';
}
export function searchProject(
  projectId: string,
  tenantId: string,
  options: ProjectSearchOptions,
  offset: number,
  signal: AbortSignal,
  cursor?: string | null
) {
  const params = new URLSearchParams({ ...options, offset: String(offset) });
  if (cursor) params.set('cursor', cursor);
  return fetchWithAuth<ProjectSearchPage>(
    `/projects/${encodeURIComponent(projectId)}/search?${params}`,
    { projectId, tenantId, signal }
  );
}
