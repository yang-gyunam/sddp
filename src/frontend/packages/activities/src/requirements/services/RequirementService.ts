/**
 * RequirementService - API Client for Requirement Management
 * Handles CRUD operations for requirements
 */

import { fetchWithAuth } from '../../shared/api';
import type {
  Requirement,
  RequirementDetail,
  RequirementPage,
  RequirementTreeNode,
  RequirementSearchResult,
  RequirementVersion,
  RequirementLevel,
  RequirementStatus,
  CreateRequirementRequest,
  UpdateRequirementRequest,
  TransitionStatusRequest,
  LinkConversationRequest,
} from '../types';
import type { FieldAuthor } from '../../shared/types';

// ============================================
// Requirement API
// ============================================

/**
 * Get list of requirements with pagination and filtering
 */
export async function getRequirements(
  tenantId: string,
  projectId: string,
  options?: {
    page?: number;
    pageSize?: number;
    level?: RequirementLevel;
    status?: RequirementStatus;
  }
): Promise<RequirementPage> {
  const params = new URLSearchParams();
  if (options?.page) params.set('page', options.page.toString());
  if (options?.pageSize) params.set('pageSize', options.pageSize.toString());
  if (options?.level) params.set('level', options.level);
  if (options?.status) params.set('status', options.status);

  const queryString = params.toString();
  const endpoint = `/requirements${queryString ? `?${queryString}` : ''}`;

  return fetchWithAuth<RequirementPage>(endpoint, { tenantId, projectId });
}

/**
 * Get requirement by ID (with children)
 */
export async function getRequirementById(
  tenantId: string,
  projectId: string,
  requirementId: string
): Promise<RequirementDetail> {
  return fetchWithAuth<RequirementDetail>(`/requirements/${requirementId}`, {
    tenantId,
    projectId,
  });
}

/**
 * Get requirement by Code
 */
export async function getRequirementByCode(
  tenantId: string,
  projectId: string,
  code: string
): Promise<RequirementDetail> {
  return fetchWithAuth<RequirementDetail>(`/requirements/code/${code}`, {
    tenantId,
    projectId,
  });
}

/**
 * Search requirements by code or title (lightweight, for autocomplete)
 */
export async function searchRequirements(
  tenantId: string,
  projectId: string,
  query: string,
  limit: number = 15
): Promise<RequirementSearchResult[]> {
  return fetchWithAuth<RequirementSearchResult[]>(
    `/requirements/search?q=${encodeURIComponent(query)}&limit=${limit}`,
    { tenantId, projectId }
  );
}

/**
 * Get requirement tree (full hierarchy A→B→C)
 */
export async function getRequirementTree(
  tenantId: string,
  projectId: string
): Promise<RequirementTreeNode[]> {
  return fetchWithAuth<RequirementTreeNode[]>('/requirements/tree', {
    tenantId,
    projectId,
  });
}

/**
 * Get children of a requirement
 */
export async function getRequirementChildren(
  tenantId: string,
  projectId: string,
  parentId: string
): Promise<Requirement[]> {
  return fetchWithAuth<Requirement[]>(`/requirements/${parentId}/children`, {
    tenantId,
    projectId,
  });
}

/**
 * Create a new requirement
 */
export async function createRequirement(
  tenantId: string,
  projectId: string,
  request: CreateRequirementRequest
): Promise<RequirementDetail> {
  return fetchWithAuth<RequirementDetail>('/requirements', {
    method: 'POST',
    body: request,
    tenantId,
    projectId,
  });
}

/**
 * Update a requirement (Draft only)
 */
export async function updateRequirement(
  tenantId: string,
  projectId: string,
  requirementId: string,
  request: UpdateRequirementRequest
): Promise<RequirementDetail> {
  return fetchWithAuth<RequirementDetail>(`/requirements/${requirementId}`, {
    method: 'PUT',
    body: request,
    tenantId,
    projectId,
  });
}

/**
 * Transition requirement status
 */
export async function transitionRequirementStatus(
  tenantId: string,
  projectId: string,
  requirementId: string,
  request: TransitionStatusRequest
): Promise<Requirement> {
  return fetchWithAuth<Requirement>(`/requirements/${requirementId}/status`, {
    method: 'PATCH',
    body: request,
    tenantId,
    projectId,
  });
}

/**
 * Delete a requirement (soft delete)
 */
export async function deleteRequirement(
  tenantId: string,
  projectId: string,
  requirementId: string
): Promise<void> {
  await fetchWithAuth<void>(`/requirements/${requirementId}`, {
    method: 'DELETE',
    tenantId,
    projectId,
  });
}

/**
 * Get version history of a requirement (all versions, newest first)
 */
export async function getRequirementVersionHistory(
  tenantId: string,
  projectId: string,
  requirementId: string
): Promise<RequirementVersion[]> {
  return fetchWithAuth<RequirementVersion[]>(`/requirements/${requirementId}/versions`, {
    tenantId,
    projectId,
  });
}

/**
 * Link a conversation to a requirement
 */
export async function linkConversation(
  tenantId: string,
  projectId: string,
  requirementId: string,
  request: LinkConversationRequest
): Promise<Requirement> {
  return fetchWithAuth<Requirement>(
    `/requirements/${requirementId}/link-conversation`,
    {
      method: 'POST',
      body: request,
      tenantId,
      projectId,
    }
  );
}

/**
 * Unlink a conversation from a requirement
 */
export async function unlinkConversation(
  tenantId: string,
  projectId: string,
  requirementId: string
): Promise<Requirement> {
  return fetchWithAuth<Requirement>(
    `/requirements/${requirementId}/link-conversation`,
    {
      method: 'DELETE',
      tenantId,
      projectId,
    }
  );
}

/**
 * Get field authors (who last modified each field)
 */
export async function getFieldAuthors(
  tenantId: string,
  projectId: string,
  requirementId: string
): Promise<FieldAuthor[]> {
  return fetchWithAuth<FieldAuthor[]>(`/requirements/${requirementId}/field-authors`, {
    tenantId,
    projectId,
  });
}

// ============================================
// Singleton Service Class
// ============================================

/**
 * RequirementService class for dependency injection
 */
export class RequirementService {
  private tenantId: string = '';
  private projectId: string = '';

  /**
   * Set tenant context
   */
  setContext(tenantId: string, projectId: string): void {
    this.tenantId = tenantId;
    this.projectId = projectId;
  }

  /**
   * Get tenant ID
   */
  getTenantId(): string {
    return this.tenantId;
  }

  /**
   * Get project ID
   */
  getProjectId(): string {
    return this.projectId;
  }

  // Requirements
  async getRequirements(options?: {
    page?: number;
    pageSize?: number;
    level?: RequirementLevel;
    status?: RequirementStatus;
  }): Promise<RequirementPage> {
    return getRequirements(this.tenantId, this.projectId, options);
  }

  async getRequirementById(requirementId: string): Promise<RequirementDetail> {
    return getRequirementById(this.tenantId, this.projectId, requirementId);
  }

  async getRequirementByCode(code: string): Promise<RequirementDetail> {
    return getRequirementByCode(this.tenantId, this.projectId, code);
  }

  async searchRequirements(query: string, limit: number = 15): Promise<RequirementSearchResult[]> {
    return searchRequirements(this.tenantId, this.projectId, query, limit);
  }

  async getRequirementTree(): Promise<RequirementTreeNode[]> {
    return getRequirementTree(this.tenantId, this.projectId);
  }

  async getRequirementChildren(parentId: string): Promise<Requirement[]> {
    return getRequirementChildren(this.tenantId, this.projectId, parentId);
  }

  async createRequirement(
    request: CreateRequirementRequest
  ): Promise<RequirementDetail> {
    return createRequirement(this.tenantId, this.projectId, request);
  }

  async updateRequirement(
    requirementId: string,
    request: UpdateRequirementRequest
  ): Promise<RequirementDetail> {
    return updateRequirement(
      this.tenantId,
      this.projectId,
      requirementId,
      request
    );
  }

  async transitionStatus(
    requirementId: string,
    request: TransitionStatusRequest
  ): Promise<Requirement> {
    return transitionRequirementStatus(
      this.tenantId,
      this.projectId,
      requirementId,
      request
    );
  }

  async deleteRequirement(requirementId: string): Promise<void> {
    return deleteRequirement(this.tenantId, this.projectId, requirementId);
  }

  async getVersionHistory(requirementId: string): Promise<RequirementVersion[]> {
    return getRequirementVersionHistory(this.tenantId, this.projectId, requirementId);
  }

  async linkConversation(
    requirementId: string,
    request: LinkConversationRequest
  ): Promise<Requirement> {
    return linkConversation(
      this.tenantId,
      this.projectId,
      requirementId,
      request
    );
  }

  async unlinkConversation(requirementId: string): Promise<Requirement> {
    return unlinkConversation(this.tenantId, this.projectId, requirementId);
  }
}

// Singleton instance
let requirementServiceInstance: RequirementService | null = null;

/**
 * Get the singleton RequirementService instance
 */
export function getRequirementService(): RequirementService {
  if (!requirementServiceInstance) {
    requirementServiceInstance = new RequirementService();
  }
  return requirementServiceInstance;
}

/**
 * Reset the singleton instance (for testing)
 */
export function resetRequirementService(): void {
  requirementServiceInstance = null;
}
