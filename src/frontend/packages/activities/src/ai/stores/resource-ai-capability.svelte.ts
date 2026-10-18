import { untrack } from 'svelte';
import { fromStore, writable } from 'svelte/store';
import { authStore } from '@sddp/shell/auth/stores';
import {
  aiCapabilityStore,
  createAiCapabilityStore,
  type AiCapabilityState,
  type AiCapabilityContext,
} from './ai-capability.store';
import { getAiCapabilityView } from './ai-capability-view';
import type { AiFeature } from '../services/AiStatusService';

const revision = writable(0);
export function refreshAiCapabilities() {
  revision.update((value) => value + 1);
  void aiCapabilityStore.refresh();
}

/** Call during component initialization. Each resource owns its request generations. */
export function createResourceAiCapability(
  scope: () => { tenantId: string; projectId?: string | null }
) {
  const auth = fromStore(authStore);
  const changes = fromStore(revision);
  const store = createAiCapabilityStore();
  const snapshot = fromStore(store);
  const contextKey = $derived.by(() => {
    const { tenantId, projectId } = scope();
    const user = auth.current.user;
    return auth.current.isAuthenticated &&
      auth.current.sessionStatus === 'active' &&
      user?.id &&
      user.tenantId === tenantId &&
      tenantId
      ? JSON.stringify({ userId: user.id, tenantId, projectId: projectId || null })
      : '';
  });
  const context = $derived<AiCapabilityContext | null>(contextKey ? JSON.parse(contextKey) : null);
  $effect(() => {
    void changes.current;
    const next = context;
    untrack(() => void store.setContext(next));
    return () => store.clear();
  });
  const state = $derived.by<AiCapabilityState>(() => {
    const value = snapshot.current;
    if (
      !context ||
      value.context?.userId !== context.userId ||
      value.context?.tenantId !== context.tenantId ||
      value.context?.projectId !== context.projectId
    )
      return { context, phase: context ? 'loading' : 'idle', data: null };
    return value;
  });
  return {
    get state() {
      return state;
    },
    view(feature: AiFeature = 'ai.analysis') {
      return getAiCapabilityView(state, feature);
    },
    refresh: refreshAiCapabilities,
  };
}
