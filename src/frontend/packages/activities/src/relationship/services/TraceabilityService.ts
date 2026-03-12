/**
 * TraceabilityService - API Client for Traceability Map
 * Fetches the hierarchical traceability map (tangled tree) for a project.
 */

import { fetchWithAuth } from '../../shared/api';
import type { TraceabilityMapData } from '../types';

// ============================================
// Traceability Map API
// ============================================

/**
 * Get the traceability map for a project.
 * Returns hierarchical nodes (Conversation → Requirement → Spec → Task/Artifact)
 * with cross-links representing non-hierarchical relationships.
 *
 * Endpoint: GET /api/projects/{projectId}/traceability-map
 */
export async function getTraceabilityMap(
  tenantId: string,
  projectId: string
): Promise<TraceabilityMapData> {
  return fetchWithAuth<TraceabilityMapData>(
    `/projects/${projectId}/traceability-map`,
    { tenantId }
  );
}
