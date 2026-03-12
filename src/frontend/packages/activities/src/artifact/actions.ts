/**
 * Artifact Actions
 * Mediates between Artifact stores and ArtifactService
 */

import { getArtifactService } from './services';
import { setTypeGroups } from './stores';
import type { ArtifactTypeGroup, ArtifactSummary, ArtifactType } from './types';

/**
 * Load artifacts for a spec and group by type for sidebar display
 */
export async function loadArtifactsSidebar(
  tenantId: string,
  projectId: string,
  specId: string
): Promise<void> {
  try {
    const service = getArtifactService();
    service.setContext(tenantId, projectId);

    const artifacts = await service.getArtifactsBySpec(specId);

    // Transform Artifact[] → ArtifactTypeGroup[]
    const groupMap = new Map<ArtifactType, ArtifactSummary[]>();

    for (const artifact of artifacts) {
      const type = artifact.artifactType;
      if (!groupMap.has(type)) {
        groupMap.set(type, []);
      }
      groupMap.get(type)!.push({
        id: artifact.id,
        artifactPath: artifact.artifactPath,
        artifactType: artifact.artifactType,
        entityName: artifact.entityName,
        specCode: artifact.specCode,
        status: 'Unverified', // Status requires verification call
        updatedAt: artifact.updatedAt,
        owner: null,
        createdBy: null,
        updatedBy: null,
      });
    }

    const groups: ArtifactTypeGroup[] = Array.from(groupMap.entries()).map(
      ([type, artifacts]) => ({
        type,
        artifacts,
        totalCount: artifacts.length,
        expanded: true,
      })
    );

    setTypeGroups(groups);
  } catch (error) {
    const message = error instanceof Error ? error.message : 'Failed to load artifacts';
    console.error('Artifact API failed:', message);
    setTypeGroups([]);
  }
}
