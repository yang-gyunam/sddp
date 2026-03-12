/**
 * Relationship Actions
 * Mediates between Relationship stores and RelationshipService
 */

import { getRelationshipService } from './services';
import {
  setRelationships,
  setRelationshipsLoading,
  setRelationshipsError,
  setGraph,
  setGraphLoading,
  setGraphError,
  setDiff,
  setDiffLoading,
  setDiffError,
} from './stores';
import type { EntityType } from './types';

/**
 * Load relationships for an entity
 */
export async function loadRelationships(
  tenantId: string,
  projectId: string,
  entityType: EntityType,
  entityId: string
): Promise<void> {
  setRelationshipsLoading(true);
  setRelationshipsError(null);

  try {
    const service = getRelationshipService();
    service.setContext(tenantId, projectId);

    const data = await service.getRelationshipsByEntity(entityType, entityId);
    setRelationships(data);
  } catch (error) {
    const message = error instanceof Error ? error.message : 'Failed to load relationships';
    console.warn('Relationship API failed:', message);
    setRelationshipsError(message);
  } finally {
    setRelationshipsLoading(false);
  }
}

/**
 * Load relationship graph for an entity
 */
export async function loadRelationshipGraph(
  tenantId: string,
  projectId: string,
  entityType: EntityType,
  entityId: string,
  depth: number = 3
): Promise<void> {
  setGraphLoading(true);
  setGraphError(null);

  try {
    const service = getRelationshipService();
    service.setContext(tenantId, projectId);

    const data = await service.getRelationshipGraph(entityType, entityId, { maxDepth: depth });
    setGraph(data);
  } catch (error) {
    const message = error instanceof Error ? error.message : 'Failed to load graph';
    console.warn('Relationship graph API failed:', message);
    setGraphError(message);
  } finally {
    setGraphLoading(false);
  }
}

/**
 * Compare two spec versions
 */
export async function compareSpecVersions(
  tenantId: string,
  projectId: string,
  specId1: string,
  specId2: string
): Promise<void> {
  setDiffLoading(true);
  setDiffError(null);

  try {
    const service = getRelationshipService();
    service.setContext(tenantId, projectId);

    const data = await service.compareSpecVersions(specId1, specId2);
    setDiff(data);
  } catch (error) {
    const message = error instanceof Error ? error.message : 'Failed to compare versions';
    console.warn('Spec diff API failed:', message);
    setDiffError(message);
  } finally {
    setDiffLoading(false);
  }
}
