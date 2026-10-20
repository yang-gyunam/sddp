import { fetchWithAuth } from '../../shared/api';
export interface GitConnection {
  id: string;
  projectId: string;
  name: string;
  provider: 'GitHub' | 'Gitea';
  serverUrl: string;
  username: string;
  hasToken: boolean;
  version: number;
  checkedAt: string | null;
}
export interface GitConnections {
  allowsPrivateRepositories: boolean;
  items: GitConnection[];
}
export interface GitConnectionInput {
  projectId: string;
  name: string;
  provider: string;
  serverUrl: string;
  username: string;
  token?: string;
}
export function createGitConnectionService(tenantId: string) {
  const path = '/admin/git-connections';
  return {
    list: () => fetchWithAuth<GitConnections>(path, { tenantId }),
    create: (body: GitConnectionInput) =>
      fetchWithAuth<GitConnection>(path, { tenantId, method: 'POST', body }),
    update: (
      id: string,
      body: {
        name: string;
        username: string;
        token?: string;
        removeToken: boolean;
        expectedVersion: number;
      }
    ) =>
      fetchWithAuth<GitConnection>(`${path}/${encodeURIComponent(id)}`, {
        tenantId,
        method: 'PUT',
        body,
      }),
    remove: (id: string, version: number) =>
      fetchWithAuth<void>(`${path}/${encodeURIComponent(id)}?expectedVersion=${version}`, {
        tenantId,
        method: 'DELETE',
      }),
  };
}
