import { writable } from 'svelte/store';
import { fetchWithAuth } from '../api/fetchWithAuth';

export interface ProjectCapabilities {
  tenantId: string;
  projectId: string;
  permissions: string[];
  canWrite: boolean;
}
interface CapabilityState {
  userId: string;
  data: ProjectCapabilities | null;
}
export const getProjectCapabilities = (projectId: string, tenantId: string) =>
  fetchWithAuth<ProjectCapabilities>(`/projects/${encodeURIComponent(projectId)}/capabilities`, {
    projectId,
    tenantId,
  });

/** Owned by one mounted tab. Never share the currently selected project's authority. */
export function createProjectCapabilityStore(load = getProjectCapabilities) {
  const state = writable<CapabilityState>({ userId: '', data: null });
  let generation = 0;
  return {
    subscribe: state.subscribe,
    clear() {
      generation++;
      state.set({ userId: '', data: null });
    },
    async load(projectId: string, tenantId: string, userId: string) {
      const request = ++generation;
      state.set({ userId, data: null });
      if (!projectId || !tenantId || !userId) return;
      try {
        const data = await load(projectId, tenantId);
        if (request !== generation) return;
        if (data.projectId !== projectId || data.tenantId !== tenantId) return;
        if (
          !Array.isArray(data.permissions) ||
          !data.permissions.every((p) => typeof p === 'string') ||
          typeof data.canWrite !== 'boolean'
        )
          return;
        state.set({ userId, data });
      } catch {
        // A denied/unavailable capability response cannot retain write controls.
        if (request === generation) state.set({ userId, data: null });
      }
    },
  };
}
