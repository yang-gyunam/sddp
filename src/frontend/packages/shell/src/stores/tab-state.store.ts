/**
 * Tab State Store
 * Persists per-tab UI state across tab moves/splits
 *
 * L-3 migration note:
 * This module keeps the public store contract stable while shadow-validating
 * the runes implementation against the legacy writable implementation.
 */

import { writable, type Subscriber, type Unsubscriber } from 'svelte/store';
import { config } from '../core/services/ConfigurationManager';
import { createRunesTabStateStore } from './tab-state.store.runes';

export type TabStateMap = Record<string, Record<string, unknown>>;

export interface TabStateStoreLike {
  subscribe: (run: Subscriber<TabStateMap>) => Unsubscriber;
  get: () => TabStateMap;
  reset: () => void;
  getState: <T extends object>(tabId: string) => T | undefined;
  setState: <T extends object>(tabId: string, state: T) => void;
  updateState: <T extends object>(tabId: string, patch: Partial<T>) => void;
  clearState: (tabId: string) => void;
  clearStates: (tabIds: string[]) => void;
  copyState: (sourceTabId: string, targetTabId: string) => void;
}

type TabStateMismatchContext = {
  action: string;
  primaryState: TabStateMap;
  shadowState: TabStateMap;
};

const initialState: TabStateMap = {};

function cloneTabStateMap(map: TabStateMap): TabStateMap {
  return Object.fromEntries(
    Object.entries(map).map(([tabId, state]) => [tabId, { ...state }])
  );
}

function normalizeTabStateMap(map: TabStateMap): unknown {
  return Object.fromEntries(
    Object.entries(map)
      .sort(([left], [right]) => left.localeCompare(right))
      .map(([tabId, state]) => [tabId, { ...state }])
  );
}

function areEqual(left: unknown, right: unknown): boolean {
  return JSON.stringify(left) === JSON.stringify(right);
}

function createWritableTabStateStore(initialMap: TabStateMap): TabStateStoreLike {
  const { subscribe, update, set } = writable<TabStateMap>(cloneTabStateMap(initialMap));

  let currentState = cloneTabStateMap(initialMap);
  subscribe((state) => {
    currentState = cloneTabStateMap(state);
  });

  return {
    subscribe,
    get: () => cloneTabStateMap(currentState),
    reset: () => set({}),
    getState: <T extends object>(tabId: string): T | undefined => {
      if (!tabId) {
        return undefined;
      }

      const state = currentState[tabId];
      return state ? ({ ...state } as T) : undefined;
    },
    setState: <T extends object>(tabId: string, state: T): void => {
      if (!tabId) {
        return;
      }

      update((map) => ({
        ...map,
        [tabId]: { ...state } as Record<string, unknown>,
      }));
    },
    updateState: <T extends object>(tabId: string, patch: Partial<T>): void => {
      if (!tabId) {
        return;
      }

      update((map) => ({
        ...map,
        [tabId]: {
          ...(map[tabId] ?? {}),
          ...patch,
        } as Record<string, unknown>,
      }));
    },
    clearState: (tabId: string): void => {
      if (!tabId) {
        return;
      }

      update((map) => {
        if (!map[tabId]) {
          return map;
        }

        const nextMap = { ...map };
        delete nextMap[tabId];
        return nextMap;
      });
    },
    clearStates: (tabIds: string[]): void => {
      if (!tabIds.length) {
        return;
      }

      update((map) => {
        const nextMap = { ...map };
        tabIds.forEach((id) => {
          delete nextMap[id];
        });
        return nextMap;
      });
    },
    copyState: (sourceTabId: string, targetTabId: string): void => {
      if (!sourceTabId || !targetTabId) {
        return;
      }

      update((map) => {
        const source = map[sourceTabId];
        if (!source) {
          return map;
        }

        return {
          ...map,
          [targetTabId]: { ...source },
        };
      });
    },
  };
}

function createParityAwareTabStateStore(
  primary: TabStateStoreLike,
  shadow: TabStateStoreLike,
  reportMismatch: (context: TabStateMismatchContext) => void
): TabStateStoreLike {
  const listeners = new Set<Subscriber<TabStateMap>>();
  let currentState = primary.get();

  primary.subscribe((state) => {
    currentState = cloneTabStateMap(state);
    for (const listener of listeners) {
      listener(cloneTabStateMap(state));
    }
  });

  function subscribe(run: Subscriber<TabStateMap>): Unsubscriber {
    run(cloneTabStateMap(currentState));
    listeners.add(run);
    return () => {
      listeners.delete(run);
    };
  }

  function execute(action: string, invokePrimary: () => void, invokeShadow: () => void): void {
    invokePrimary();
    invokeShadow();

    const primaryState = primary.get();
    const shadowState = shadow.get();

    if (!areEqual(normalizeTabStateMap(primaryState), normalizeTabStateMap(shadowState))) {
      reportMismatch({ action, primaryState, shadowState });
    }
  }

  return {
    subscribe,
    get: () => cloneTabStateMap(currentState),
    reset: () => execute('reset', primary.reset, shadow.reset),
    getState: <T extends object>(tabId: string): T | undefined => {
      if (!tabId) {
        return undefined;
      }

      const state = currentState[tabId];
      return state ? ({ ...state } as T) : undefined;
    },
    setState: <T extends object>(tabId: string, state: T): void => {
      execute('setState', () => primary.setState(tabId, state), () => shadow.setState(tabId, state));
    },
    updateState: <T extends object>(tabId: string, patch: Partial<T>): void => {
      execute('updateState', () => primary.updateState<T>(tabId, patch), () => shadow.updateState<T>(tabId, patch));
    },
    clearState: (tabId: string): void => {
      execute('clearState', () => primary.clearState(tabId), () => shadow.clearState(tabId));
    },
    clearStates: (tabIds: string[]): void => {
      execute('clearStates', () => primary.clearStates(tabIds), () => shadow.clearStates(tabIds));
    },
    copyState: (sourceTabId: string, targetTabId: string): void => {
      execute('copyState', () => primary.copyState(sourceTabId, targetTabId), () => shadow.copyState(sourceTabId, targetTabId));
    },
  };
}

function createStore(): TabStateStoreLike {
  const enableRunesTabStateStore = config.isRunesTabStateStoreEnabled();
  const enableShadowParity = config.isRunesStoreShadowParityEnabled();

  const legacyStore = createWritableTabStateStore(initialState);
  const runesStore = createRunesTabStateStore(initialState);

  const primaryStore = enableRunesTabStateStore ? runesStore : legacyStore;
  const shadowStore = enableRunesTabStateStore ? legacyStore : runesStore;

  if (!enableShadowParity) {
    return primaryStore;
  }

  return createParityAwareTabStateStore(primaryStore, shadowStore, ({ action, primaryState, shadowState }) => {
    console.error('[tab-state.store parity mismatch]', {
      action,
      primaryState: normalizeTabStateMap(primaryState),
      shadowState: normalizeTabStateMap(shadowState),
    });
  });
}

export const __internal = {
  cloneTabStateMap,
  normalizeTabStateMap,
  createWritableTabStateStore,
  createParityAwareTabStateStore,
  createRunesTabStateStore,
};

export const tabStateStore = createStore();

export const getTabState = <T extends object>(tabId: string): T | undefined =>
  tabStateStore.getState<T>(tabId);

export const setTabState = <T extends object>(tabId: string, state: T): void =>
  tabStateStore.setState(tabId, state);

export const updateTabState = <T extends object>(
  tabId: string,
  patch: Partial<T>
): void => tabStateStore.updateState<T>(tabId, patch);

export const clearTabState = (tabId: string): void => tabStateStore.clearState(tabId);

export const clearTabStates = (tabIds: string[]): void => tabStateStore.clearStates(tabIds);

export const copyTabState = (sourceTabId: string, targetTabId: string): void =>
  tabStateStore.copyState(sourceTabId, targetTabId);

export const resetTabStateStore = (): void => tabStateStore.reset();
