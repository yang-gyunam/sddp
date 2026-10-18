import type { AiFeature } from '../services/AiStatusService';
import type { AiCapabilityState } from './ai-capability.store';

const messages: Record<string, string> = {
  deployment_disabled:
    'AI execution is disabled on this server. Server configuration and restart are required to enable it.',
  edition_free: 'AI is not included in this edition.',
  module_missing: 'AI module is not installed on this server.',
  not_entitled: 'AI requires a Pro entitlement.',
  system_disabled: 'AI is disabled by the system administrator.',
  project_disabled: 'AI is disabled for this project.',
  project_required: 'Select a project to use AI.',
  permission_denied: 'You do not have permission to run AI analysis.',
  feature_not_available: 'Conversation agents are not available yet.',
  scope_invalid: 'AI is unavailable for this scope.',
};

/** UI hints only; execution and settings APIs still enforce server authorization. */
export function getAiCapabilityView(state: AiCapabilityState, feature: AiFeature = 'ai.analysis') {
  const policy = state.data?.features[feature];
  const observed =
    (state.phase === 'ready' || state.phase === 'unavailable') &&
    !!policy &&
    policy.state !== 'unavailable';
  const runtime = state.data?.distribution;
  const visible = runtime?.edition === 'ai' && runtime.aiInstalled;
  const canConfigure = visible && runtime.canConfigureAi;
  const message =
    state.phase === 'idle' || state.phase === 'loading'
      ? 'Checking AI availability…'
      : !observed
        ? 'AI availability could not be checked. Try again.'
        : policy.canExecute && state.data?.readiness === 'unavailable'
          ? 'AI model is unavailable. Try again.'
          : (messages[policy.reason] ??
            (policy.canExecute ? 'AI is available.' : 'AI is unavailable.'));
  return {
    visible,
    distributionUnavailable: state.phase === 'unavailable' && !runtime,
    message: visible && !runtime.deploymentEnabled ? messages.deployment_disabled! : message,
    canExecute:
      visible &&
      runtime.deploymentEnabled &&
      observed &&
      policy.canExecute &&
      state.data?.readiness !== 'unavailable',
    canRead: visible && runtime.deploymentEnabled && observed && policy.policyEnabled,
    canConfigure,
    canConfigureProject: canConfigure,
    unavailable:
      (!observed && state.phase === 'unavailable') ||
      policy?.state === 'unavailable' ||
      (policy?.canExecute === true && state.data?.readiness === 'unavailable'),
  };
}
