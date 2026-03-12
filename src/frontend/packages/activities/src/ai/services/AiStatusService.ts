/**
 * AI Status Service
 * Checks whether AI features are enabled at system and project level
 */

import { fetchWithAuth } from '../../shared/api';
import { config as appConfig } from '@sddp/shell/core';

export interface AiStatus {
  systemEnabled: boolean;
  projectEnabled: boolean;
  effectiveEnabled: boolean;
}

/**
 * Get AI feature status for the given tenant/project context
 */
export async function getAiStatus(tenantId: string, projectId?: string): Promise<AiStatus> {
  if (!appConfig.get('enableAiFeatures')) {
    return { systemEnabled: false, projectEnabled: false, effectiveEnabled: false };
  }

  try {
    const params = new URLSearchParams();
    if (projectId) params.set('projectId', projectId);
    const query = params.toString();
    const url = query ? `/ai/status?${query}` : '/ai/status';

    const result = await fetchWithAuth<AiStatus>(url, { tenantId });
    if (!result) {
      return { systemEnabled: true, projectEnabled: true, effectiveEnabled: true };
    }
    return result;
  } catch {
    // Default to enabled on error to avoid blocking users
    return { systemEnabled: true, projectEnabled: true, effectiveEnabled: true };
  }
}
