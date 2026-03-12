import type { Subscriber, Unsubscriber } from 'svelte/store';
import type { TabStateMap, TabStateStoreLike } from './tab-state.store';

function cloneTabStateMap(map: TabStateMap): TabStateMap {
  return Object.fromEntries(
    Object.entries(map).map(([tabId, state]) => [tabId, { ...state }])
  );
}

export function createRunesTabStateStore(initialState: TabStateMap): TabStateStoreLike {
  let state = cloneTabStateMap(initialState);
  const subscribers = new Set<Subscriber<TabStateMap>>();

  function snapshot(): TabStateMap {
    return cloneTabStateMap(state);
  }

  function commit(nextState: TabStateMap): void {
    state = cloneTabStateMap(nextState);
    const latest = snapshot();
    for (const subscriber of subscribers) {
      subscriber(latest);
    }
  }

  return {
    subscribe: (run: Subscriber<TabStateMap>): Unsubscriber => {
      run(snapshot());
      subscribers.add(run);
      return () => {
        subscribers.delete(run);
      };
    },
    get: () => snapshot(),
    reset: () => {
      commit({});
    },
    getState: <T extends object>(tabId: string): T | undefined => {
      if (!tabId) {
        return undefined;
      }

      const value = state[tabId];
      return value ? ({ ...value } as T) : undefined;
    },
    setState: <T extends object>(tabId: string, nextState: T): void => {
      if (!tabId) {
        return;
      }

      commit({
        ...state,
        [tabId]: { ...nextState } as Record<string, unknown>,
      });
    },
    updateState: <T extends object>(tabId: string, patch: Partial<T>): void => {
      if (!tabId) {
        return;
      }

      commit({
        ...state,
        [tabId]: {
          ...(state[tabId] ?? {}),
          ...patch,
        } as Record<string, unknown>,
      });
    },
    clearState: (tabId: string): void => {
      if (!tabId || !state[tabId]) {
        return;
      }

      const nextState = { ...state };
      delete nextState[tabId];
      commit(nextState);
    },
    clearStates: (tabIds: string[]): void => {
      if (!tabIds.length) {
        return;
      }

      const nextState = { ...state };
      tabIds.forEach((id) => {
        delete nextState[id];
      });
      commit(nextState);
    },
    copyState: (sourceTabId: string, targetTabId: string): void => {
      if (!sourceTabId || !targetTabId) {
        return;
      }

      const source = state[sourceTabId];
      if (!source) {
        return;
      }

      commit({
        ...state,
        [targetTabId]: { ...source },
      });
    },
  };
}
