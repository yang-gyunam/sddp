/**
 * Snapshot Service
 * API client for project snapshot backup/restore operations
 */

import { fetchWithAuth } from '../../shared/api';
import type { ProjectSnapshot, CreateSnapshotParams } from '../types';

class SnapshotService {
  async getProjectSnapshots(tenantId: string, projectId: string): Promise<ProjectSnapshot[]> {
    const result = await fetchWithAuth<ProjectSnapshot[]>(
      `/projects/${projectId}/snapshots`,
      { tenantId },
    );
    return result;
  }

  async createProjectSnapshot(
    tenantId: string,
    projectId: string,
    params: CreateSnapshotParams,
  ): Promise<ProjectSnapshot> {
    const result = await fetchWithAuth<ProjectSnapshot>(
      `/projects/${projectId}/snapshots`,
      { method: 'POST', tenantId, body: params },
    );
    return result;
  }

  async restoreProjectSnapshot(
    tenantId: string,
    projectId: string,
    snapshotId: string,
  ): Promise<ProjectSnapshot> {
    const result = await fetchWithAuth<ProjectSnapshot>(
      `/projects/${projectId}/snapshots/${snapshotId}/restore`,
      { method: 'POST', tenantId },
    );
    return result;
  }

  async deleteProjectSnapshot(
    tenantId: string,
    projectId: string,
    snapshotId: string,
  ): Promise<void> {
    await fetchWithAuth<void>(
      `/projects/${projectId}/snapshots/${snapshotId}`,
      { method: 'DELETE', tenantId },
    );
  }
}

// Singleton
let instance: SnapshotService | null = null;

export function getSnapshotService(): SnapshotService {
  if (!instance) {
    instance = new SnapshotService();
  }
  return instance;
}

export { SnapshotService };
export default getSnapshotService();
