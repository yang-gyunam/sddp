import type { GitConnections } from './GitConnectionService';
import { fetchWithAuth } from '../../shared/api';

export interface RepositoryDraft {
  gitConnectionId?: string | null;
  repoUrl: string;
  repoBranch: string;
  artifactRootPath: string;
  syncIntervalMinutes: number;
}
export interface ProjectRepositorySettings extends RepositoryDraft {
  visibility?: 'public' | 'private' | null;
  version: number;
  checkedAt: string | null;
  confirmedAt: string | null;
  relationshipCount: number;
  canConfirm: boolean;
  isReadOnly: boolean;
}

export function createProjectRepositoryService(tenantId: string, projectId: string) {
  const path = `/projects/${encodeURIComponent(projectId)}/repository`;
  const scope = { tenantId, projectId };
  return {
    connections: () => fetchWithAuth<GitConnections>(`${path}/connections`, scope),
    get: () => fetchWithAuth<ProjectRepositorySettings>(path, scope),
    save: (draft: RepositoryDraft, expectedVersion: number) =>
      fetchWithAuth<ProjectRepositorySettings>(path, {
        ...scope,
        method: 'PUT',
        body: { ...draft, expectedVersion },
      }),
    check: (expectedVersion: number) =>
      fetchWithAuth<ProjectRepositorySettings>(`${path}/check`, {
        ...scope,
        method: 'POST',
        body: { expectedVersion },
      }),
    confirm: (expectedVersion: number) =>
      fetchWithAuth<ProjectRepositorySettings>(`${path}/confirm`, {
        ...scope,
        method: 'POST',
        body: { expectedVersion },
      }),
  };
}

export const repositoryDraft = (settings: RepositoryDraft): RepositoryDraft => ({
  gitConnectionId: settings.gitConnectionId ?? null,
  repoUrl: settings.repoUrl,
  repoBranch: settings.repoBranch,
  artifactRootPath: settings.artifactRootPath,
  syncIntervalMinutes: settings.syncIntervalMinutes,
});
