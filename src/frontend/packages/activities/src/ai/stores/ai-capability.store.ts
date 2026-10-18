import { writable, type Readable } from 'svelte/store';
import {
  AI_FEATURES,
  getAiStatus,
  isAiStatus,
  unavailableAiStatus,
  type AiStatus,
} from '../services/AiStatusService';

export interface AiCapabilityContext {
  userId: string;
  tenantId: string;
  projectId: string | null;
}
export interface AiCapabilityState {
  context: AiCapabilityContext | null;
  phase: 'idle' | 'loading' | 'ready' | 'unavailable';
  data: AiStatus | null;
}

/** Own one instance per UI scope. The global selected project is not split-tab authority. */
export function createAiCapabilityStore(load = getAiStatus) {
  const state = writable<AiCapabilityState>({ context: null, phase: 'idle', data: null });
  let context: AiCapabilityContext | null = null;
  let generation = 0;
  async function refresh() {
    const request = ++generation;
    const requested = context;
    state.set({ context: requested, phase: requested ? 'loading' : 'idle', data: null });
    if (!requested) return;
    let data: AiStatus;
    try {
      const response = await load(
        requested.tenantId,
        requested.projectId ?? undefined,
        requested.userId
      );
      data = isAiStatus(
        response,
        requested.tenantId,
        requested.projectId ?? undefined,
        requested.userId
      )
        ? response
        : unavailableAiStatus(
            requested.tenantId,
            requested.projectId ?? undefined,
            requested.userId
          );
    } catch {
      data = unavailableAiStatus(
        requested.tenantId,
        requested.projectId ?? undefined,
        requested.userId
      );
    }
    if (request !== generation) return;
    state.set({
      context: requested,
      phase: AI_FEATURES.some((key) => data.features[key].state === 'unavailable')
        ? 'unavailable'
        : 'ready',
      data,
    });
  }
  return {
    subscribe: state.subscribe,
    refresh,
    clear() {
      generation++;
      context = null;
      state.set({ context: null, phase: 'idle', data: null });
    },
    async setContext(next: AiCapabilityContext | null) {
      const normalized = next?.userId && next.tenantId ? { ...next } : null;
      if (
        context?.userId === normalized?.userId &&
        context?.tenantId === normalized?.tenantId &&
        context?.projectId === normalized?.projectId
      )
        return;
      context = normalized;
      await refresh();
    },
  };
}
export const aiCapabilityStore = createAiCapabilityStore();
export const resetAiCapability = () => aiCapabilityStore.clear();

type AuthContext = {
  isAuthenticated: boolean;
  sessionStatus?: string;
  user: { id: string; tenantId?: string | null } | null;
};
type ProjectContext = { selectedProjectId?: string | null };

/** Bind once at app mount; invalidate synchronously on logout/account/scope/session changes. */
export function bindAiCapabilityState(
  auth: Readable<AuthContext>,
  projects: Readable<ProjectContext>,
  store = aiCapabilityStore
) {
  let currentAuth: AuthContext | null = null;
  let projectId: string | null = null;
  const update = () => {
    const user = currentAuth?.user;
    const active =
      currentAuth?.isAuthenticated &&
      !['checking', 'anonymous', 'expired', 'unavailable'].includes(
        currentAuth.sessionStatus ?? ''
      );
    void store.setContext(
      active && user?.tenantId ? { userId: user.id, tenantId: user.tenantId, projectId } : null
    );
  };
  const unProject = projects.subscribe((value) => {
    projectId = value.selectedProjectId ?? null;
    update();
  });
  const unAuth = auth.subscribe((value) => {
    currentAuth = value;
    update();
  });
  return () => {
    unAuth();
    unProject();
    store.clear();
  };
}
