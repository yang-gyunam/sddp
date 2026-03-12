/**
 * GlossaryService - API Client for Glossary Management
 * Handles CRUD operations for glossary terms
 */

import { fetchWithAuth } from '../../shared/api';
import type { FieldAuthor } from '../../shared/types';
import type {
  GlossaryTerm,
  GlossaryTermDetail,
  GlossaryTermPage,
  GlossaryTermSummary,
  GlossaryConflictResult,
  GlossaryTermUsage,
  GlossaryTermVersion,
  TermCategory,
  GlossaryTermStatus,
  CreateGlossaryTermRequest,
  UpdateGlossaryTermRequest,
  DeprecateGlossaryTermRequest,
  DetectConflictRequest,
} from '../types';

// ============================================
// Glossary API
// ============================================

/**
 * Get list of glossary terms with pagination and filtering
 * @param projectId - Project ID or null/undefined for tenant-wide (global) query
 */
export async function getTerms(
  tenantId: string,
  projectId: string | null | undefined,
  options?: {
    page?: number;
    pageSize?: number;
    category?: TermCategory;
    status?: GlossaryTermStatus;
  }
): Promise<GlossaryTermPage> {
  const params = new URLSearchParams();
  if (options?.page) params.set('page', options.page.toString());
  if (options?.pageSize) params.set('pageSize', options.pageSize.toString());
  if (options?.category) params.set('category', options.category);
  if (options?.status) params.set('status', options.status);

  const queryString = params.toString();
  const endpoint = `/glossary${queryString ? `?${queryString}` : ''}`;

  return fetchWithAuth<GlossaryTermPage>(endpoint, { tenantId, projectId: projectId ?? undefined });
}

/**
 * Search glossary terms
 */
export async function searchTerms(
  tenantId: string,
  projectId: string,
  query: string,
  options?: {
    page?: number;
    pageSize?: number;
    category?: TermCategory;
    status?: GlossaryTermStatus;
  }
): Promise<GlossaryTermPage> {
  const params = new URLSearchParams({ q: query });
  if (options?.page) params.set('page', options.page.toString());
  if (options?.pageSize) params.set('pageSize', options.pageSize.toString());
  if (options?.category) params.set('category', options.category);
  if (options?.status) params.set('status', options.status);

  return fetchWithAuth<GlossaryTermPage>(`/glossary/search?${params}`, {
    tenantId,
    projectId,
  });
}

/**
 * Autocomplete glossary terms
 */
export async function autocomplete(
  tenantId: string,
  projectId: string,
  prefix: string,
  limit: number = 10
): Promise<GlossaryTermSummary[]> {
  const params = new URLSearchParams({
    prefix,
    limit: limit.toString(),
  });

  return fetchWithAuth<GlossaryTermSummary[]>(
    `/glossary/autocomplete?${params}`,
    { tenantId, projectId }
  );
}

/**
 * Get a single term by ID
 */
export async function getTermById(
  tenantId: string,
  projectId: string,
  termId: string
): Promise<GlossaryTermDetail> {
  return fetchWithAuth<GlossaryTermDetail>(`/glossary/${termId}`, {
    tenantId,
    projectId,
  });
}

/**
 * Get a single term by term name
 */
export async function getTermByTerm(
  tenantId: string,
  projectId: string,
  term: string
): Promise<GlossaryTermDetail> {
  return fetchWithAuth<GlossaryTermDetail>(
    `/glossary/term/${encodeURIComponent(term)}`,
    { tenantId, projectId }
  );
}

/**
 * Create a new term
 */
export async function createTerm(
  tenantId: string,
  projectId: string,
  data: CreateGlossaryTermRequest
): Promise<GlossaryTermDetail> {
  return fetchWithAuth<GlossaryTermDetail>('/glossary', {
    method: 'POST',
    body: data,
    tenantId,
    projectId,
  });
}

/**
 * Update an existing term
 */
export async function updateTerm(
  tenantId: string,
  projectId: string,
  termId: string,
  data: UpdateGlossaryTermRequest
): Promise<GlossaryTermDetail> {
  return fetchWithAuth<GlossaryTermDetail>(`/glossary/${termId}`, {
    method: 'PUT',
    body: data,
    tenantId,
    projectId,
  });
}

/**
 * Approve a term (Draft -> Active)
 */
export async function approveTerm(
  tenantId: string,
  projectId: string,
  termId: string
): Promise<GlossaryTerm> {
  return fetchWithAuth<GlossaryTerm>(`/glossary/${termId}/approve`, {
    method: 'POST',
    tenantId,
    projectId,
  });
}

/**
 * Deprecate a term
 */
export async function deprecateTerm(
  tenantId: string,
  projectId: string,
  termId: string,
  data?: DeprecateGlossaryTermRequest
): Promise<GlossaryTerm> {
  return fetchWithAuth<GlossaryTerm>(`/glossary/${termId}/deprecate`, {
    method: 'POST',
    body: data || {},
    tenantId,
    projectId,
  });
}

/**
 * Reactivate a deprecated term (Deprecated -> Active)
 */
export async function reactivateTerm(
  tenantId: string,
  projectId: string,
  termId: string
): Promise<GlossaryTerm> {
  return fetchWithAuth<GlossaryTerm>(`/glossary/${termId}/reactivate`, {
    method: 'POST',
    tenantId,
    projectId,
  });
}

/**
 * Delete a term (soft delete)
 */
export async function deleteTerm(
  tenantId: string,
  projectId: string,
  termId: string
): Promise<void> {
  await fetchWithAuth<void>(`/glossary/${termId}`, {
    method: 'DELETE',
    tenantId,
    projectId,
  });
}

/**
 * Detect conflicts for a term
 */
export async function detectConflicts(
  tenantId: string,
  projectId: string,
  data: DetectConflictRequest
): Promise<GlossaryConflictResult> {
  return fetchWithAuth<GlossaryConflictResult>('/glossary/detect-conflicts', {
    method: 'POST',
    body: data,
    tenantId,
    projectId,
  });
}

/**
 * Get term usage information
 */
export async function getTermUsage(
  tenantId: string,
  projectId: string,
  termId: string
): Promise<GlossaryTermUsage> {
  return fetchWithAuth<GlossaryTermUsage>(`/glossary/${termId}/usage`, {
    tenantId,
    projectId,
  });
}

/**
 * Get term version history
 */
export async function getTermVersionHistory(
  tenantId: string,
  projectId: string,
  termId: string
): Promise<GlossaryTermVersion[]> {
  return fetchWithAuth<GlossaryTermVersion[]>(`/glossary/${termId}/versions`, {
    tenantId,
    projectId,
  });
}

/**
 * Add a related term
 */
export async function addRelatedTerm(
  tenantId: string,
  projectId: string,
  termId: string,
  relatedTermId: string
): Promise<GlossaryTerm> {
  return fetchWithAuth<GlossaryTerm>(`/glossary/${termId}/related-terms`, {
    method: 'POST',
    body: { relatedTermId },
    tenantId,
    projectId,
  });
}

/**
 * Add a usage example
 */
export async function addUsageExample(
  tenantId: string,
  projectId: string,
  termId: string,
  example: string
): Promise<GlossaryTerm> {
  return fetchWithAuth<GlossaryTerm>(`/glossary/${termId}/examples`, {
    method: 'POST',
    body: { example },
    tenantId,
    projectId,
  });
}

/**
 * Get field authors (who last modified each field)
 */
export async function getFieldAuthors(
  tenantId: string,
  projectId: string,
  termId: string
): Promise<FieldAuthor[]> {
  return fetchWithAuth<FieldAuthor[]>(`/glossary/${termId}/field-authors`, {
    tenantId,
    projectId,
  });
}

// ============================================
// Service Class (for dependency injection)
// ============================================

/**
 * GlossaryService class with tenant/project context
 */
export class GlossaryServiceClass {
  private tenantId: string = '';
  private projectId: string = '';

  setContext(tenantId: string, projectId?: string): void {
    this.tenantId = tenantId;
    this.projectId = projectId || '';
  }

  getTerms(options?: { page?: number; pageSize?: number; category?: TermCategory; status?: GlossaryTermStatus; }): Promise<GlossaryTermPage> {
    return getTerms(this.tenantId, this.projectId || null, options);
  }

  searchTerms(query: string, options?: { page?: number; pageSize?: number; category?: TermCategory; status?: GlossaryTermStatus; }): Promise<GlossaryTermPage> {
    return searchTerms(this.tenantId, this.projectId, query, options);
  }

  autocomplete(prefix: string, limit?: number): Promise<GlossaryTermSummary[]> {
    return autocomplete(this.tenantId, this.projectId, prefix, limit);
  }

  getTermById(termId: string): Promise<GlossaryTermDetail> {
    return getTermById(this.tenantId, this.projectId, termId);
  }

  getTermByTerm(term: string): Promise<GlossaryTermDetail> {
    return getTermByTerm(this.tenantId, this.projectId, term);
  }

  createTerm(data: CreateGlossaryTermRequest): Promise<GlossaryTermDetail> {
    return createTerm(this.tenantId, this.projectId, data);
  }

  updateTerm(termId: string, data: UpdateGlossaryTermRequest): Promise<GlossaryTermDetail> {
    return updateTerm(this.tenantId, this.projectId, termId, data);
  }

  approveTerm(termId: string): Promise<GlossaryTerm> {
    return approveTerm(this.tenantId, this.projectId, termId);
  }

  deprecateTerm(termId: string, data?: DeprecateGlossaryTermRequest): Promise<GlossaryTerm> {
    return deprecateTerm(this.tenantId, this.projectId, termId, data);
  }

  reactivateTerm(termId: string): Promise<GlossaryTerm> {
    return reactivateTerm(this.tenantId, this.projectId, termId);
  }

  deleteTerm(termId: string): Promise<void> {
    return deleteTerm(this.tenantId, this.projectId, termId);
  }

  detectConflicts(data: DetectConflictRequest): Promise<GlossaryConflictResult> {
    return detectConflicts(this.tenantId, this.projectId, data);
  }

  getTermUsage(termId: string): Promise<GlossaryTermUsage> {
    return getTermUsage(this.tenantId, this.projectId, termId);
  }

  addRelatedTerm(termId: string, relatedTermId: string): Promise<GlossaryTerm> {
    return addRelatedTerm(this.tenantId, this.projectId, termId, relatedTermId);
  }

  addUsageExample(termId: string, example: string): Promise<GlossaryTerm> {
    return addUsageExample(this.tenantId, this.projectId, termId, example);
  }

  getTermVersionHistory(termId: string): Promise<GlossaryTermVersion[]> {
    return getTermVersionHistory(this.tenantId, this.projectId, termId);
  }

  getFieldAuthors(termId: string): Promise<FieldAuthor[]> {
    return getFieldAuthors(this.tenantId, this.projectId, termId);
  }
}

// Singleton instance
let glossaryServiceInstance: GlossaryServiceClass | null = null;

/**
 * Get the singleton GlossaryService instance
 */
export function getGlossaryService(): GlossaryServiceClass {
  if (!glossaryServiceInstance) {
    glossaryServiceInstance = new GlossaryServiceClass();
  }
  return glossaryServiceInstance;
}

/**
 * Reset the singleton instance (for testing/logout)
 */
export function resetGlossaryService(): void {
  glossaryServiceInstance = null;
}

// ============================================
// Export as service object (backward compatibility)
// ============================================

export const GlossaryService = {
  getTerms,
  searchTerms,
  autocomplete,
  getTermById,
  getTermByTerm,
  createTerm,
  updateTerm,
  approveTerm,
  deprecateTerm,
  reactivateTerm,
  deleteTerm,
  detectConflicts,
  getTermUsage,
  getTermVersionHistory,
  addRelatedTerm,
  addUsageExample,
  getFieldAuthors,
};

export default GlossaryService;
