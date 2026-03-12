import { fetchWithAuth } from '../../shared/api';
import type {
  EntityMetadata,
  CreateEntityMetadataRequest,
  UpdateEntityMetadataRequest,
} from '../types';

export async function getEntityMetadata(
  tenantId: string,
  projectId: string,
  specId: string
): Promise<EntityMetadata[]> {
  return fetchWithAuth<EntityMetadata[]>(`/specs/${specId}/entities`, {
    tenantId,
    projectId,
  });
}

export async function createEntityMetadata(
  tenantId: string,
  projectId: string,
  specId: string,
  request: CreateEntityMetadataRequest
): Promise<EntityMetadata> {
  return fetchWithAuth<EntityMetadata>(`/specs/${specId}/entities`, {
    method: 'POST',
    body: request,
    tenantId,
    projectId,
  });
}

export async function updateEntityMetadata(
  tenantId: string,
  projectId: string,
  specId: string,
  entityId: string,
  request: UpdateEntityMetadataRequest
): Promise<EntityMetadata> {
  return fetchWithAuth<EntityMetadata>(`/specs/${specId}/entities/${entityId}`, {
    method: 'PUT',
    body: request,
    tenantId,
    projectId,
  });
}

export async function deleteEntityMetadata(
  tenantId: string,
  projectId: string,
  specId: string,
  entityId: string
): Promise<void> {
  await fetchWithAuth<void>(`/specs/${specId}/entities/${entityId}`, {
    method: 'DELETE',
    tenantId,
    projectId,
  });
}

export class EntityMetadataService {
  private tenantId: string = '';
  private projectId: string = '';

  setContext(tenantId: string, projectId: string): void {
    this.tenantId = tenantId;
    this.projectId = projectId;
  }

  async getBySpec(specId: string): Promise<EntityMetadata[]> {
    return getEntityMetadata(this.tenantId, this.projectId, specId);
  }

  async create(specId: string, request: CreateEntityMetadataRequest): Promise<EntityMetadata> {
    return createEntityMetadata(this.tenantId, this.projectId, specId, request);
  }

  async update(
    specId: string,
    entityId: string,
    request: UpdateEntityMetadataRequest
  ): Promise<EntityMetadata> {
    return updateEntityMetadata(this.tenantId, this.projectId, specId, entityId, request);
  }

  async delete(specId: string, entityId: string): Promise<void> {
    return deleteEntityMetadata(this.tenantId, this.projectId, specId, entityId);
  }
}
