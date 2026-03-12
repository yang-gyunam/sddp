/**
 * ArtifactService - API Client for Artifact Tracking
 * Handles artifact CRUD, verification, and querying
 */

import { fetchWithAuth } from '../../shared/api';
import type { Artifact, ArtifactSummary } from '../types';

// ============================================
// Types
// ============================================

export interface ArtifactVerifyResult {
  isValid: boolean;
  storedHash: string;
  currentHash: string;
  artifactPath: string;
}

export interface UpsertArtifactRequest {
  specId: string;
  artifactPath: string;
  artifactType: string;
  contentHash: string;
  generatorVersion?: string;
  templateVersion?: string;
  entityName?: string;
  glossaryTermId?: string;
  sourceConversationId?: string;
  sourceRequirementId?: string;
  ownerUserId?: string;
  artifactId?: string;
}

export interface VerifyArtifactRequest {
  artifactId: string;
  currentHash: string;
}

export interface ArtifactPagedResult<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

// ============================================
// Search
// ============================================

export interface ArtifactSearchResult {
  id: string;
  artifactPath: string;
  artifactType: string;
  entityName: string;
}

/**
 * Search artifacts by path or entity name (lightweight, for autocomplete)
 */
export async function searchArtifacts(
  tenantId: string,
  projectId: string,
  query: string,
  limit: number = 15
): Promise<ArtifactSearchResult[]> {
  return fetchWithAuth<ArtifactSearchResult[]>(
    `/artifacts/search?q=${encodeURIComponent(query)}&limit=${limit}`,
    { tenantId, projectId }
  );
}

// ============================================
// Artifact API Functions
// ============================================

/**
 * Get all artifacts for a project
 */
export async function getArtifactsByProject(
  tenantId: string,
  projectId: string
): Promise<ArtifactSummary[]> {
  const items: ArtifactSummary[] = [];
  let pageNumber = 1;

  while (true) {
    const page = await getArtifactsByProjectPage(tenantId, projectId, pageNumber, 50);
    items.push(...page.items);

    if (!page.hasNextPage) {
      break;
    }

    pageNumber += 1;
  }

  return items;
}

export async function getArtifactsByProjectPage(
  tenantId: string,
  projectId: string,
  pageNumber: number = 1,
  pageSize: number = 20
): Promise<ArtifactPagedResult<ArtifactSummary>> {
  const params = new URLSearchParams({
    pageNumber: String(pageNumber),
    pageSize: String(pageSize),
  });
  const endpoint = `/projects/${projectId}/artifacts?${params}`;
  return fetchWithAuth<ArtifactPagedResult<ArtifactSummary>>(endpoint, { tenantId, projectId });
}

/**
 * Get artifacts by spec ID
 */
export async function getArtifactsBySpec(
  tenantId: string,
  projectId: string,
  specId: string
): Promise<Artifact[]> {
  const endpoint = `/artifacts?specId=${encodeURIComponent(specId)}`;
  return fetchWithAuth<Artifact[]>(endpoint, { tenantId, projectId });
}

/**
 * Get artifact by ID
 */
export async function getArtifactById(
  tenantId: string,
  projectId: string,
  artifactId: string
): Promise<Artifact> {
  return fetchWithAuth<Artifact>(`/artifacts/${artifactId}`, { tenantId, projectId });
}

/**
 * Get artifact by file path
 */
export async function getArtifactByPath(
  tenantId: string,
  projectId: string,
  path: string
): Promise<Artifact> {
  const endpoint = `/artifacts/by-path?path=${encodeURIComponent(path)}`;
  return fetchWithAuth<Artifact>(endpoint, { tenantId, projectId });
}

/**
 * Create or update an artifact tracking record
 */
export async function upsertArtifact(
  tenantId: string,
  projectId: string,
  request: UpsertArtifactRequest
): Promise<Artifact> {
  return fetchWithAuth<Artifact>('/artifacts', {
    method: 'POST',
    tenantId,
    projectId,
    body: request,
  });
}

/**
 * Verify an artifact's content hash
 */
export async function verifyArtifact(
  tenantId: string,
  projectId: string,
  request: VerifyArtifactRequest
): Promise<ArtifactVerifyResult> {
  return fetchWithAuth<ArtifactVerifyResult>('/artifacts/verify', {
    method: 'POST',
    tenantId,
    projectId,
    body: request,
  });
}

/**
 * Regenerate an artifact from its source spec
 */
export async function regenerateArtifact(
  tenantId: string,
  projectId: string,
  artifactId: string
): Promise<RegenerateResult> {
  return fetchWithAuth<RegenerateResult>(`/artifacts/${artifactId}/regenerate`, {
    method: 'POST',
    tenantId,
    projectId,
  });
}

/**
 * Deactivate an artifact (soft delete)
 */
export async function deactivateArtifact(
  tenantId: string,
  projectId: string,
  artifactId: string
): Promise<void> {
  await fetchWithAuth<void>(`/artifacts/${artifactId}`, {
    method: 'DELETE',
    tenantId,
    projectId,
  });
}

/**
 * Activate an artifact (restore from deactivated)
 */
export async function activateArtifact(
  tenantId: string,
  projectId: string,
  artifactId: string
): Promise<void> {
  await fetchWithAuth<void>(`/artifacts/${artifactId}/activate`, {
    method: 'PUT',
    tenantId,
    projectId,
  });
}

// ============================================
// Types (Regenerate)
// ============================================

export interface RegenerateResult {
  artifactId: string;
  specId: string;
  artifactPath: string;
  newContentHash: string;
  previousContentHash: string;
  regeneratedAt: string;
}

// ============================================
// Service Class (Singleton)
// ============================================

export class ArtifactService {
  private tenantId = '';
  private projectId = '';

  setContext(tenantId: string, projectId: string): void {
    this.tenantId = tenantId;
    this.projectId = projectId;
  }

  getArtifactsByProject(): Promise<ArtifactSummary[]> {
    return getArtifactsByProject(this.tenantId, this.projectId);
  }

  getArtifactsByProjectPage(
    pageNumber: number = 1,
    pageSize: number = 20
  ): Promise<ArtifactPagedResult<ArtifactSummary>> {
    return getArtifactsByProjectPage(this.tenantId, this.projectId, pageNumber, pageSize);
  }

  getArtifactsBySpec(specId: string): Promise<Artifact[]> {
    return getArtifactsBySpec(this.tenantId, this.projectId, specId);
  }

  getArtifactById(artifactId: string): Promise<Artifact> {
    return getArtifactById(this.tenantId, this.projectId, artifactId);
  }

  getArtifactByPath(path: string): Promise<Artifact> {
    return getArtifactByPath(this.tenantId, this.projectId, path);
  }

  upsertArtifact(request: UpsertArtifactRequest): Promise<Artifact> {
    return upsertArtifact(this.tenantId, this.projectId, request);
  }

  verifyArtifact(request: VerifyArtifactRequest): Promise<ArtifactVerifyResult> {
    return verifyArtifact(this.tenantId, this.projectId, request);
  }

  regenerateArtifact(artifactId: string): Promise<RegenerateResult> {
    return regenerateArtifact(this.tenantId, this.projectId, artifactId);
  }

  deactivateArtifact(artifactId: string): Promise<void> {
    return deactivateArtifact(this.tenantId, this.projectId, artifactId);
  }

  activateArtifact(artifactId: string): Promise<void> {
    return activateArtifact(this.tenantId, this.projectId, artifactId);
  }
}

// ============================================
// Singleton Management
// ============================================

let instance: ArtifactService | null = null;

export function getArtifactService(): ArtifactService {
  if (!instance) {
    instance = new ArtifactService();
  }
  return instance;
}

export function resetArtifactService(): void {
  instance = null;
}
