/**
 * RelationshipService - API Client for Relationship Management
 * Handles CRUD operations, graph traversal, and validation for relationships
 */

import { fetchWithAuth } from '../../shared/api';
import { searchSpecs } from '../../specs/services/SpecService';
import { searchRequirements } from '../../requirements/services/RequirementService';
import { searchConversations } from '../../conversations/services/ConversationService';
import { autocomplete as glossaryAutocomplete } from '../../glossary/services/GlossaryService';
import { searchArtifacts } from '../../artifact/services/ArtifactService';
import { searchTasks } from '../../task/services/TaskService';
import type {
  EntityType,
  RelationType,
  Relationship,
  RelationshipListData,
  RelationshipGraphData,
  RelationshipValidationResult,
  SpecDiffResult,
  CreateRelationshipRequest,
  ValidateRelationshipRequest,
  DecisionImpactData,
  PrimaryFlowNodeDto,
} from '../types';

// ============================================
// Relationship CRUD API
// ============================================

/**
 * Create a new relationship
 */
export async function createRelationship(
  tenantId: string,
  projectId: string,
  request: CreateRelationshipRequest
): Promise<Relationship> {
  return fetchWithAuth<Relationship>('/relationships', {
    method: 'POST',
    body: request,
    tenantId,
    projectId,
  });
}

/**
 * Get relationship by ID
 */
export async function getRelationshipById(
  tenantId: string,
  projectId: string,
  relationshipId: string
): Promise<Relationship> {
  return fetchWithAuth<Relationship>(`/relationships/${relationshipId}`, {
    tenantId,
    projectId,
  });
}

/**
 * Get relationships for an entity (incoming + outgoing)
 */
export async function getRelationshipsByEntity(
  tenantId: string,
  projectId: string,
  entityType: EntityType,
  entityId: string,
  options?: {
    typeFilter?: RelationType;
    includeInvalidated?: boolean;
  }
): Promise<RelationshipListData> {
  const params = new URLSearchParams();
  if (options?.typeFilter) params.set('typeFilter', options.typeFilter);
  if (options?.includeInvalidated) params.set('includeInvalidated', 'true');

  const queryString = params.toString();
  const endpoint = `/relationships/entity/${entityType}/${entityId}${queryString ? `?${queryString}` : ''}`;

  return fetchWithAuth<RelationshipListData>(endpoint, { tenantId, projectId });
}

/**
 * Get relationship graph (BFS traversal)
 */
export async function getRelationshipGraph(
  tenantId: string,
  projectId: string,
  entityType: EntityType,
  entityId: string,
  options?: {
    maxDepth?: number;
    typeFilter?: RelationType[];
  }
): Promise<RelationshipGraphData> {
  const params = new URLSearchParams();
  if (options?.maxDepth) params.set('maxDepth', options.maxDepth.toString());
  if (options?.typeFilter && options.typeFilter.length > 0) {
    params.set('typeFilter', options.typeFilter.join(','));
  }

  const queryString = params.toString();
  const endpoint = `/relationships/graph/${entityType}/${entityId}${queryString ? `?${queryString}` : ''}`;

  return fetchWithAuth<RelationshipGraphData>(endpoint, { tenantId, projectId });
}

/**
 * Validate a relationship (check for circular references)
 */
export async function validateRelationship(
  tenantId: string,
  projectId: string,
  request: ValidateRelationshipRequest
): Promise<RelationshipValidationResult> {
  return fetchWithAuth<RelationshipValidationResult>('/relationships/validate', {
    method: 'POST',
    body: request,
    tenantId,
    projectId,
  });
}

/**
 * Invalidate a relationship (soft delete)
 */
export async function invalidateRelationship(
  tenantId: string,
  projectId: string,
  relationshipId: string
): Promise<void> {
  await fetchWithAuth<{ message: string }>(`/relationships/${relationshipId}`, {
    method: 'DELETE',
    tenantId,
    projectId,
  });
}

// ============================================
// Decision Impact API
// ============================================

/**
 * Get impact data for a decision message (relationships created from it)
 */
export async function getDecisionImpact(
  tenantId: string,
  projectId: string,
  messageId: string
): Promise<DecisionImpactData> {
  return fetchWithAuth<DecisionImpactData>(`/relationships/decision/${messageId}`, {
    tenantId,
    projectId,
  });
}

// ============================================
// Primary Flow API
// ============================================

/**
 * Get primary flow nodes for an entity (FK-based vertical chain)
 * Conversation → Requirement → Spec → GlossaryTerm / Artifact
 */
export async function getPrimaryFlow(
  tenantId: string,
  projectId: string,
  entityType: string,
  entityId: string
): Promise<PrimaryFlowNodeDto[]> {
  const params = new URLSearchParams({ entityType, entityId });
  return fetchWithAuth<PrimaryFlowNodeDto[]>(
    `/projects/${projectId}/primary-flow?${params.toString()}`,
    { tenantId }
  );
}

// ============================================
// Version Diff API
// ============================================

/**
 * Compare two spec versions
 */
export async function compareSpecVersions(
  tenantId: string,
  projectId: string,
  specId: string,
  compareToId: string
): Promise<SpecDiffResult> {
  return fetchWithAuth<SpecDiffResult>(`/specs/${specId}/diff/${compareToId}`, {
    tenantId,
    projectId,
  });
}

// ============================================
// Entity Search (unified cross-type search)
// ============================================

export interface EntitySearchResult {
  id: string;
  entityType: EntityType;
  label: string;
  code?: string;
}

/**
 * Search entities across all types (Spec, Requirement, Conversation, GlossaryTerm, Artifact)
 * Used by RelationshipLinkSearch for creating new relationships.
 */
export async function searchEntities(
  tenantId: string,
  projectId: string,
  query: string,
  entityTypeFilter?: EntityType | 'All',
  limit: number = 15
): Promise<EntitySearchResult[]> {
  if (!query.trim()) return [];

  const filterType = entityTypeFilter === 'All' ? undefined : entityTypeFilter;
  const results: EntitySearchResult[] = [];

  const searches: Promise<void>[] = [];

  if (!filterType || filterType === 'Spec') {
    searches.push(
      searchSpecs(tenantId, projectId, query, limit)
        .then((items) =>
          items.forEach((s) =>
            results.push({ id: s.id, entityType: 'Spec', label: s.title, code: s.code })
          )
        )
        .catch((err) => console.warn('[searchEntities] Spec search failed:', err))
    );
  }
  if (!filterType || filterType === 'Requirement') {
    searches.push(
      searchRequirements(tenantId, projectId, query, limit)
        .then((items) =>
          items.forEach((r) =>
            results.push({ id: r.id, entityType: 'Requirement', label: r.title, code: r.code })
          )
        )
        .catch((err) => console.warn('[searchEntities] Requirement search failed:', err))
    );
  }
  if (!filterType || filterType === 'Conversation') {
    searches.push(
      searchConversations(tenantId, projectId, query, limit)
        .then((items) =>
          items.forEach((c) =>
            results.push({ id: c.id, entityType: 'Conversation', label: c.name })
          )
        )
        .catch((err) => console.warn('[searchEntities] Conversation search failed:', err))
    );
  }
  if (!filterType || filterType === 'GlossaryTerm') {
    searches.push(
      glossaryAutocomplete(tenantId, projectId, query, limit)
        .then((items) =>
          items.forEach((g) =>
            results.push({ id: g.id, entityType: 'GlossaryTerm', label: g.term })
          )
        )
        .catch((err) => console.warn('[searchEntities] GlossaryTerm search failed:', err))
    );
  }
  if (!filterType || filterType === 'Artifact') {
    searches.push(
      searchArtifacts(tenantId, projectId, query, limit)
        .then((items) =>
          items.forEach((a) =>
            results.push({
              id: a.id,
              entityType: 'Artifact',
              label: a.entityName || a.artifactPath,
            })
          )
        )
        .catch((err) => console.warn('[searchEntities] Artifact search failed:', err))
    );
  }
  if (!filterType || filterType === 'Task') {
    searches.push(
      searchTasks(tenantId, projectId, query, limit)
        .then((items) =>
          items.forEach((t) =>
            results.push({
              id: t.id,
              entityType: 'Task',
              label: t.title,
            })
          )
        )
        .catch((err) => console.warn('[searchEntities] Task search failed:', err))
    );
  }

  await Promise.allSettled(searches);
  return results.slice(0, limit);
}

// ============================================
// Singleton Service Class
// ============================================

/**
 * RelationshipService class for dependency injection
 */
export class RelationshipService {
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

  // Relationship CRUD
  async createRelationship(request: CreateRelationshipRequest): Promise<Relationship> {
    return createRelationship(this.tenantId, this.projectId, request);
  }

  async getRelationshipById(relationshipId: string): Promise<Relationship> {
    return getRelationshipById(this.tenantId, this.projectId, relationshipId);
  }

  async getRelationshipsByEntity(
    entityType: EntityType,
    entityId: string,
    options?: {
      typeFilter?: RelationType;
      includeInvalidated?: boolean;
    }
  ): Promise<RelationshipListData> {
    return getRelationshipsByEntity(this.tenantId, this.projectId, entityType, entityId, options);
  }

  async getRelationshipGraph(
    entityType: EntityType,
    entityId: string,
    options?: {
      maxDepth?: number;
      typeFilter?: RelationType[];
    }
  ): Promise<RelationshipGraphData> {
    return getRelationshipGraph(this.tenantId, this.projectId, entityType, entityId, options);
  }

  async validateRelationship(
    request: ValidateRelationshipRequest
  ): Promise<RelationshipValidationResult> {
    return validateRelationship(this.tenantId, this.projectId, request);
  }

  async invalidateRelationship(relationshipId: string): Promise<void> {
    return invalidateRelationship(this.tenantId, this.projectId, relationshipId);
  }

  // Primary flow
  async getPrimaryFlow(entityType: string, entityId: string): Promise<PrimaryFlowNodeDto[]> {
    return getPrimaryFlow(this.tenantId, this.projectId, entityType, entityId);
  }

  // Decision impact
  async getDecisionImpact(messageId: string): Promise<DecisionImpactData> {
    return getDecisionImpact(this.tenantId, this.projectId, messageId);
  }

  // Version diff
  async compareSpecVersions(specId: string, compareToId: string): Promise<SpecDiffResult> {
    return compareSpecVersions(this.tenantId, this.projectId, specId, compareToId);
  }
}

// Singleton instance
let relationshipServiceInstance: RelationshipService | null = null;

/**
 * Get the singleton RelationshipService instance
 */
export function getRelationshipService(): RelationshipService {
  if (!relationshipServiceInstance) {
    relationshipServiceInstance = new RelationshipService();
  }
  return relationshipServiceInstance;
}

/**
 * Reset the singleton instance (for testing)
 */
export function resetRelationshipService(): void {
  relationshipServiceInstance = null;
}
