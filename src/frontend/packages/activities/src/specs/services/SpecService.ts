/**
 * SpecService - API Client for Spec Management
 * Handles CRUD operations, status transitions, and versioning for specs
 */

import { fetchWithAuth } from '../../shared/api';
import type {
  Spec,
  SpecDetail,
  SpecPage,
  SpecStatus,
  CreateSpecRequest,
  UpdateSpecRequest,
  LinkRequirementRequest,
  RejectRequest,
  SignOff,
  SignOffRequest,
  SignOffSummary,
  FieldAuthor,
  AuditLogEntry,
} from '../types';

// ============================================
// Spec CRUD API
// ============================================

/**
 * Get list of specs with pagination and filtering
 */
export async function getSpecs(
  tenantId: string,
  projectId: string,
  options?: {
    page?: number;
    pageSize?: number;
    status?: SpecStatus;
  }
): Promise<SpecPage> {
  const params = new URLSearchParams();
  if (options?.page) params.set('page', options.page.toString());
  if (options?.pageSize) params.set('pageSize', options.pageSize.toString());
  if (options?.status) params.set('status', options.status);

  const queryString = params.toString();
  const endpoint = `/specs${queryString ? `?${queryString}` : ''}`;

  return fetchWithAuth<SpecPage>(endpoint, { tenantId, projectId });
}

/**
 * Get spec by ID
 */
export async function getSpecById(
  tenantId: string,
  projectId: string,
  specId: string
): Promise<SpecDetail> {
  return fetchWithAuth<SpecDetail>(`/specs/${specId}`, {
    tenantId,
    projectId,
  });
}

/**
 * Search specs by code or title (lightweight, for autocomplete)
 */
export interface SpecSearchResult {
  id: string;
  code: string;
  title: string;
  status: string;
}

export async function searchSpecs(
  tenantId: string,
  projectId: string,
  query: string,
  limit: number = 15
): Promise<SpecSearchResult[]> {
  return fetchWithAuth<SpecSearchResult[]>(
    `/specs/search?q=${encodeURIComponent(query)}&limit=${limit}`,
    { tenantId, projectId }
  );
}

/**
 * Get spec by Code
 */
export async function getSpecByCode(
  tenantId: string,
  projectId: string,
  code: string
): Promise<SpecDetail> {
  return fetchWithAuth<SpecDetail>(`/specs/code/${code}`, {
    tenantId,
    projectId,
  });
}

/**
 * Create a new spec
 */
export async function createSpec(
  tenantId: string,
  projectId: string,
  request: CreateSpecRequest
): Promise<SpecDetail> {
  return fetchWithAuth<SpecDetail>('/specs', {
    method: 'POST',
    body: request,
    tenantId,
    projectId,
  });
}

/**
 * Update a spec (Draft only)
 */
export async function updateSpec(
  tenantId: string,
  projectId: string,
  specId: string,
  request: UpdateSpecRequest
): Promise<SpecDetail> {
  return fetchWithAuth<SpecDetail>(`/specs/${specId}`, {
    method: 'PUT',
    body: request,
    tenantId,
    projectId,
  });
}

/**
 * Delete a spec (soft delete)
 */
export async function deleteSpec(
  tenantId: string,
  projectId: string,
  specId: string
): Promise<void> {
  await fetchWithAuth<void>(`/specs/${specId}`, {
    method: 'DELETE',
    tenantId,
    projectId,
  });
}

/**
 * Activate a spec (restore from deactivated)
 */
export async function activateSpec(
  tenantId: string,
  projectId: string,
  specId: string
): Promise<void> {
  await fetchWithAuth<void>(`/specs/${specId}/activate`, {
    method: 'PUT',
    tenantId,
    projectId,
  });
}

// ============================================
// Status Transitions API
// ============================================

/**
 * Submit spec for review (Draft → InReview)
 */
export async function submitForReview(
  tenantId: string,
  projectId: string,
  specId: string
): Promise<Spec> {
  return fetchWithAuth<Spec>(`/specs/${specId}/submit-review`, {
    method: 'POST',
    tenantId,
    projectId,
  });
}

/**
 * Approve spec (InReview → Approved)
 */
export async function approveSpec(
  tenantId: string,
  projectId: string,
  specId: string
): Promise<Spec> {
  return fetchWithAuth<Spec>(`/specs/${specId}/approve`, {
    method: 'POST',
    tenantId,
    projectId,
  });
}

/**
 * Reject spec (InReview → Draft)
 */
export async function rejectSpec(
  tenantId: string,
  projectId: string,
  specId: string,
  request?: RejectRequest
): Promise<Spec> {
  return fetchWithAuth<Spec>(`/specs/${specId}/reject`, {
    method: 'POST',
    body: request || {},
    tenantId,
    projectId,
  });
}

/**
 * Lock spec (Approved → Locked)
 */
export async function lockSpec(
  tenantId: string,
  projectId: string,
  specId: string
): Promise<Spec> {
  return fetchWithAuth<Spec>(`/specs/${specId}/lock`, {
    method: 'POST',
    tenantId,
    projectId,
  });
}

// ============================================
// Sign-Off API
// ============================================

/**
 * Get sign-offs for a spec
 */
export async function getSignOffs(
  tenantId: string,
  projectId: string,
  specId: string
): Promise<SignOff[]> {
  return fetchWithAuth<SignOff[]>(`/specs/${specId}/sign-offs`, {
    tenantId,
    projectId,
  });
}

/**
 * Submit a sign-off decision
 */
export async function submitSignOff(
  tenantId: string,
  projectId: string,
  specId: string,
  request: SignOffRequest
): Promise<SignOff> {
  return fetchWithAuth<SignOff>(`/specs/${specId}/sign-off`, {
    method: 'POST',
    body: request,
    tenantId,
    projectId,
  });
}

/**
 * Get sign-off summary for a spec
 */
export async function getSignOffSummary(
  tenantId: string,
  projectId: string,
  specId: string
): Promise<SignOffSummary> {
  return fetchWithAuth<SignOffSummary>(`/specs/${specId}/sign-off-summary`, {
    tenantId,
    projectId,
  });
}

// ============================================
// Requirement Linking API
// ============================================

/**
 * Link a requirement to a spec
 */
export async function linkRequirement(
  tenantId: string,
  projectId: string,
  specId: string,
  request: LinkRequirementRequest
): Promise<Spec> {
  return fetchWithAuth<Spec>(`/specs/${specId}/link-requirement`, {
    method: 'POST',
    body: request,
    tenantId,
    projectId,
  });
}

/**
 * Unlink a requirement from a spec
 */
export async function unlinkRequirement(
  tenantId: string,
  projectId: string,
  specId: string
): Promise<Spec> {
  return fetchWithAuth<Spec>(`/specs/${specId}/link-requirement`, {
    method: 'DELETE',
    tenantId,
    projectId,
  });
}

// ============================================
// Versioning API
// ============================================

/**
 * Create new version from a locked spec
 */
export async function createNewVersion(
  tenantId: string,
  projectId: string,
  specId: string
): Promise<SpecDetail> {
  return fetchWithAuth<SpecDetail>(`/specs/${specId}/new-version`, {
    method: 'POST',
    tenantId,
    projectId,
  });
}

/**
 * Get version history of a spec
 */
export async function getVersionHistory(
  tenantId: string,
  projectId: string,
  specId: string
): Promise<Spec[]> {
  return fetchWithAuth<Spec[]>(`/specs/${specId}/versions`, {
    tenantId,
    projectId,
  });
}

// ============================================
// Change Tracking API
// ============================================

/**
 * Get field authors (who last modified each field)
 */
export async function getFieldAuthors(
  tenantId: string,
  projectId: string,
  specId: string
): Promise<FieldAuthor[]> {
  return fetchWithAuth<FieldAuthor[]>(`/specs/${specId}/field-authors`, {
    tenantId,
    projectId,
  });
}

/**
 * Get spec timeline (audit log entries)
 */
export async function getSpecTimeline(
  tenantId: string,
  projectId: string,
  specId: string,
  limit: number = 50
): Promise<AuditLogEntry[]> {
  const params = new URLSearchParams();
  params.set('limit', limit.toString());

  return fetchWithAuth<AuditLogEntry[]>(`/specs/${specId}/timeline?${params.toString()}`, {
    tenantId,
    projectId,
  });
}

// ============================================
// Singleton Service Class
// ============================================

/**
 * SpecService class for dependency injection
 */
export class SpecService {
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

  // CRUD operations
  async getSpecs(options?: {
    page?: number;
    pageSize?: number;
    status?: SpecStatus;
  }): Promise<SpecPage> {
    return getSpecs(this.tenantId, this.projectId, options);
  }

  async getSpecById(specId: string): Promise<SpecDetail> {
    return getSpecById(this.tenantId, this.projectId, specId);
  }

  async getSpecByCode(code: string): Promise<SpecDetail> {
    return getSpecByCode(this.tenantId, this.projectId, code);
  }

  async createSpec(request: CreateSpecRequest): Promise<SpecDetail> {
    return createSpec(this.tenantId, this.projectId, request);
  }

  async updateSpec(specId: string, request: UpdateSpecRequest): Promise<SpecDetail> {
    return updateSpec(this.tenantId, this.projectId, specId, request);
  }

  async deleteSpec(specId: string): Promise<void> {
    return deleteSpec(this.tenantId, this.projectId, specId);
  }

  async activateSpec(specId: string): Promise<void> {
    return activateSpec(this.tenantId, this.projectId, specId);
  }

  // Status transitions
  async submitForReview(specId: string): Promise<Spec> {
    return submitForReview(this.tenantId, this.projectId, specId);
  }

  async approveSpec(specId: string): Promise<Spec> {
    return approveSpec(this.tenantId, this.projectId, specId);
  }

  async rejectSpec(specId: string, request?: RejectRequest): Promise<Spec> {
    return rejectSpec(this.tenantId, this.projectId, specId, request);
  }

  async lockSpec(specId: string): Promise<Spec> {
    return lockSpec(this.tenantId, this.projectId, specId);
  }

  // Sign-Off
  async getSignOffs(specId: string): Promise<SignOff[]> {
    return getSignOffs(this.tenantId, this.projectId, specId);
  }

  async submitSignOff(specId: string, request: SignOffRequest): Promise<SignOff> {
    return submitSignOff(this.tenantId, this.projectId, specId, request);
  }

  async getSignOffSummary(specId: string): Promise<SignOffSummary> {
    return getSignOffSummary(this.tenantId, this.projectId, specId);
  }

  // Requirement linking
  async linkRequirement(specId: string, request: LinkRequirementRequest): Promise<Spec> {
    return linkRequirement(this.tenantId, this.projectId, specId, request);
  }

  async unlinkRequirement(specId: string): Promise<Spec> {
    return unlinkRequirement(this.tenantId, this.projectId, specId);
  }

  // Versioning
  async createNewVersion(specId: string): Promise<SpecDetail> {
    return createNewVersion(this.tenantId, this.projectId, specId);
  }

  async getVersionHistory(specId: string): Promise<Spec[]> {
    return getVersionHistory(this.tenantId, this.projectId, specId);
  }

  // Change Tracking
  async getFieldAuthors(specId: string): Promise<FieldAuthor[]> {
    return getFieldAuthors(this.tenantId, this.projectId, specId);
  }

  async getSpecTimeline(specId: string, limit?: number): Promise<AuditLogEntry[]> {
    return getSpecTimeline(this.tenantId, this.projectId, specId, limit);
  }
}

// Singleton instance
let specServiceInstance: SpecService | null = null;

/**
 * Get the singleton SpecService instance
 */
export function getSpecService(): SpecService {
  if (!specServiceInstance) {
    specServiceInstance = new SpecService();
  }
  return specServiceInstance;
}

/**
 * Reset the singleton instance (for testing)
 */
export function resetSpecService(): void {
  specServiceInstance = null;
}
