import { fetchWithAuth } from '../../shared/api';

export const AI_FEATURES = ['ai.analysis', 'ai.search', 'ai.conversation'] as const;
export type AiFeature = (typeof AI_FEATURES)[number];
export interface AiFeatureStatus {
  state: 'enabled' | 'disabled' | 'unavailable';
  entitled: boolean | null;
  policyEnabled: boolean;
  canExecute: boolean;
  reason: string;
}
export interface AiStatus {
  distribution?: {
    contractVersion: 1;
    edition: 'free' | 'ai';
    aiInstalled: boolean;
    deploymentEnabled: boolean;
    canConfigureAi: boolean;
  };
  contractVersion: 1;
  scope: { tenantId: string; projectId: string | null; userId: string | null };
  deploymentEnabled: boolean;
  readiness: 'unknown' | 'ready' | 'unavailable';
  features: Record<AiFeature, AiFeatureStatus>;
  systemEnabled: boolean;
  projectEnabled: boolean;
  effectiveEnabled: boolean;
}

export function unavailableAiStatus(
  tenantId: string,
  projectId?: string,
  userId?: string
): AiStatus {
  const denied: AiFeatureStatus = {
    state: 'unavailable',
    entitled: null,
    policyEnabled: false,
    canExecute: false,
    reason: 'status_unavailable',
  };
  return {
    contractVersion: 1,
    scope: { tenantId, projectId: projectId ?? null, userId: userId ?? null },
    deploymentEnabled: false,
    readiness: 'unknown',
    systemEnabled: false,
    projectEnabled: false,
    effectiveEnabled: false,
    features: {
      'ai.analysis': { ...denied },
      'ai.search': { ...denied },
      'ai.conversation': { ...denied },
    },
  };
}

/** Validate the wire contract before any capability can enable a control. */
export function isAiStatus(
  value: unknown,
  tenantId: string,
  projectId?: string,
  userId?: string
): value is AiStatus {
  if (!value || typeof value !== 'object') return false;
  const data = value as AiStatus;
  if (
    data.contractVersion !== 1 ||
    data.scope?.tenantId !== tenantId ||
    data.scope?.projectId !== (projectId ?? null) ||
    typeof data.scope?.userId !== 'string' ||
    (userId !== undefined && data.scope.userId !== userId)
  )
    return false;
  if (
    ![data.deploymentEnabled, data.systemEnabled, data.projectEnabled, data.effectiveEnabled].every(
      (v) => typeof v === 'boolean'
    ) ||
    !['unknown', 'ready', 'unavailable'].includes(data.readiness)
  )
    return false;
  if (
    !AI_FEATURES.every((key) => {
      const feature = data.features?.[key];
      return (
        feature &&
        ['enabled', 'disabled', 'unavailable'].includes(feature.state) &&
        (typeof feature.entitled === 'boolean' || feature.entitled === null) &&
        typeof feature.policyEnabled === 'boolean' &&
        typeof feature.canExecute === 'boolean' &&
        typeof feature.reason === 'string' &&
        feature.reason.length > 0 &&
        feature.policyEnabled === (feature.state === 'enabled') &&
        (!feature.policyEnabled ||
          (data.deploymentEnabled && feature.entitled === true && projectId !== undefined)) &&
        (!feature.canExecute || feature.policyEnabled)
      );
    })
  )
    return false;
  return data.effectiveEnabled === data.features['ai.analysis'].policyEnabled;
}

/** Runtime authority comes from the server, independently of Vite feature flags. */
export async function getAiStatus(
  tenantId: string,
  projectId?: string,
  userId?: string
): Promise<AiStatus> {
  try {
    const runtime = await fetchWithAuth<unknown>('/runtime/capabilities', { tenantId, projectId });
    if (!isRuntimeCapabilities(runtime)) return unavailableAiStatus(tenantId, projectId, userId);
    if (!runtime.aiInstalled) {
      const status = unavailableAiStatus(tenantId, projectId, userId);
      status.distribution = runtime;
      for (const key of AI_FEATURES)
        status.features[key] = {
          state: 'disabled',
          entitled: false,
          policyEnabled: false,
          canExecute: false,
          reason: runtime.edition === 'free' ? 'edition_free' : 'module_missing',
        };
      return status;
    }
    try {
      const result = await fetchWithAuth<unknown>('/ai/status', { tenantId, projectId });
      if (
        isAiStatus(result, tenantId, projectId, userId) &&
        result.deploymentEnabled === runtime.deploymentEnabled
      )
        return { ...result, distribution: runtime };
    } catch {
      /* Keep installed edition and settings permission on execution-status failure. */
    }
    return { ...unavailableAiStatus(tenantId, projectId, userId), distribution: runtime };
  } catch {
    return unavailableAiStatus(tenantId, projectId, userId);
  }
}

export function isRuntimeCapabilities(
  value: unknown
): value is NonNullable<AiStatus['distribution']> {
  if (!value || typeof value !== 'object') return false;
  const item = value as Record<string, unknown>;
  return (
    item.contractVersion === 1 &&
    ['free', 'ai'].includes(item.edition as string) &&
    typeof item.aiInstalled === 'boolean' &&
    typeof item.deploymentEnabled === 'boolean' &&
    typeof item.canConfigureAi === 'boolean' &&
    (item.edition === 'ai' || !item.aiInstalled) &&
    (item.aiInstalled || (!item.deploymentEnabled && !item.canConfigureAi))
  );
}
