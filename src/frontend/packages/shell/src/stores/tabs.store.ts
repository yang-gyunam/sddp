/**
 * Tabs Store - Tab and Editor Group Management
 * Simplified version based on app-shell-pro reference
 *
 * L-3 migration note:
 * This module keeps the public store contracts stable while shadow-validating
 * the runes implementation against the legacy writable implementation.
 */

import { writable, derived, type Subscriber, type Unsubscriber } from 'svelte/store';
import { config } from '../core/services/ConfigurationManager';
import type { Tab, EditorGroupData } from '../types';
import {
  clearTabState,
  clearTabStates,
  copyTabState,
  resetTabStateStore,
} from './tab-state.store';
import { createRunesTabsStateStore } from './tabs.store.runes';

export interface TabsState {
  groups: EditorGroupData[];
  activeGroupId: string;
}

export interface TabsIdFactory {
  createGroupId: () => string;
  createTabId: () => string;
  createCopiedTabId: (sourceTabId: string) => string;
}

export interface TabsSideEffects {
  clearTabState: (tabId: string) => void;
  clearTabStates: (tabIds: string[]) => void;
  copyTabState: (sourceTabId: string, targetTabId: string) => void;
  resetTabStateStore: () => void;
}

export interface TabsStateStoreLike {
  subscribe: (run: Subscriber<TabsState>) => Unsubscriber;
  set: (state: TabsState) => void;
  update: (updater: (state: TabsState) => TabsState) => void;
  get: () => TabsState;
}

export interface TabsStoreLike extends TabsStateStoreLike {
  reset: () => void;
}

export interface TabActionsLike {
  createTab: (tabData: Omit<Tab, 'id'>, groupId?: string) => Tab;
  closeTab: (tabId: string, groupId?: string) => void;
  closeOtherTabs: (tabId: string, groupId?: string) => void;
  switchToTab: (tabId: string, groupId?: string) => void;
  setActiveGroup: (groupId: string) => void;
  updateTab: (tabId: string, updates: Partial<Tab>, groupId?: string) => void;
  splitGroup: (direction: 'horizontal' | 'vertical', sourceGroupId: string) => void;
  splitRight: (tabId: string, sourceGroupId: string) => void;
  closeGroup: (groupId: string) => void;
  moveTabToGroup: (tabId: string, targetGroupId: string, sourceGroupId: string, insertIndex?: number) => void;
  reorderTabs: (fromIndex: number, toIndex: number, groupId?: string) => void;
  updateGroupPositions: (updates: Record<string, { x: number; y: number; width: number; height: number }>) => void;
  copyTabToGroup: (tabId: string, targetGroupId: string, sourceGroupId: string, insertIndex?: number) => void;
  openByPath: (path: string, tabData: Omit<Tab, 'id' | 'path'>, groupId?: string) => Tab | null;
}

interface TabsModuleLike {
  store: TabsStoreLike;
  actions: TabActionsLike;
}

type TabsEffectCall =
  | { target: 'tab-state.clearTabState'; args: [string] }
  | { target: 'tab-state.clearTabStates'; args: [string[]] }
  | { target: 'tab-state.copyTabState'; args: [string, string] }
  | { target: 'tab-state.resetTabStateStore'; args: [] };

type TabsMismatchContext = {
  action: string;
  primaryState: TabsState;
  shadowState: TabsState;
  primaryEffects: TabsEffectCall[];
  shadowEffects: TabsEffectCall[];
  primaryResult?: Tab | null;
  shadowResult?: Tab | null;
};

function randomToken(): string {
  return Math.random().toString(36).slice(2, 9);
}

function createDefaultTabsIdFactory(now: () => number = Date.now): TabsIdFactory {
  return {
    createGroupId: () => `group-${now()}-${randomToken()}`,
    createTabId: () => `tab-${now()}-${randomToken()}`,
    createCopiedTabId: (sourceTabId) => `${sourceTabId}-copy-${now()}`,
  };
}

function cloneTab(tab: Tab): Tab {
  return {
    ...tab,
    props: { ...tab.props },
  };
}

function cloneEditorGroup(group: EditorGroupData): EditorGroupData {
  return {
    ...group,
    tabs: group.tabs.map(cloneTab),
    tabHistory: [...group.tabHistory],
    position: { ...group.position },
  };
}

function cloneTabsState(state: TabsState): TabsState {
  return {
    activeGroupId: state.activeGroupId,
    groups: state.groups.map(cloneEditorGroup),
  };
}

function normalizeTab(tab: Tab): unknown {
  return {
    id: tab.id,
    title: tab.title,
    meta: tab.meta ?? null,
    icon: tab.icon ?? null,
    dirty: tab.dirty,
    closable: tab.closable,
    componentType: tab.component == null ? null : typeof tab.component,
    props: tab.props,
    path: tab.path ?? null,
    url: tab.url ?? null,
    menuId: tab.menuId ?? null,
    type: tab.type ?? null,
  };
}

function normalizeTabsState(state: TabsState): unknown {
  return {
    activeGroupId: state.activeGroupId,
    groups: state.groups.map((group) => ({
      id: group.id,
      activeTab: group.activeTab,
      tabHistory: [...group.tabHistory],
      splitDirection: group.splitDirection ?? null,
      position: { ...group.position },
      tabs: group.tabs.map(normalizeTab),
    })),
  };
}

function areEqual(left: unknown, right: unknown): boolean {
  return JSON.stringify(left) === JSON.stringify(right);
}

function createPairedTabsIdFactories(now: () => number): {
  primary: TabsIdFactory;
  shadow: TabsIdFactory;
} {
  const groupQueue: string[] = [];
  const tabQueue: string[] = [];
  const copiedQueue: Array<{ sourceTabId: string; value: string }> = [];

  const createFactory = (mode: 'primary' | 'shadow'): TabsIdFactory => ({
    createGroupId: () => {
      if (mode === 'primary') {
        const value = `group-${now()}-${randomToken()}`;
        groupQueue.push(value);
        return value;
      }
      return groupQueue.shift() ?? `group-${now()}-${randomToken()}`;
    },
    createTabId: () => {
      if (mode === 'primary') {
        const value = `tab-${now()}-${randomToken()}`;
        tabQueue.push(value);
        return value;
      }
      return tabQueue.shift() ?? `tab-${now()}-${randomToken()}`;
    },
    createCopiedTabId: (sourceTabId) => {
      if (mode === 'primary') {
        const value = `${sourceTabId}-copy-${now()}`;
        copiedQueue.push({ sourceTabId, value });
        return value;
      }

      const index = copiedQueue.findIndex((entry) => entry.sourceTabId === sourceTabId);
      if (index >= 0) {
        const [entry] = copiedQueue.splice(index, 1);
        return entry?.value ?? `${sourceTabId}-copy-${now()}`;
      }

      return `${sourceTabId}-copy-${now()}`;
    },
  });

  return {
    primary: createFactory('primary'),
    shadow: createFactory('shadow'),
  };
}

function createDefaultGroup(idFactory: TabsIdFactory): EditorGroupData {
  return {
    id: idFactory.createGroupId(),
    tabs: [],
    activeTab: '',
    tabHistory: [],
    position: { x: 0, y: 0, width: 1, height: 1 },
  };
}

function createInitialState(idFactory: TabsIdFactory): TabsState {
  const defaultGroup = createDefaultGroup(idFactory);
  return {
    groups: [defaultGroup],
    activeGroupId: defaultGroup.id,
  };
}

const getTabDedupKey = (tab: Tab): string | null => {
  if (tab.path) return `path:${tab.path}`;
  if (tab.url) return `url:${tab.url}`;
  return null;
};

const setActiveTab = (group: EditorGroupData, newActiveTab: string): EditorGroupData => {
  if (group.activeTab === newActiveTab) return group;
  const previousActiveTab = group.activeTab;
  const newHistory = previousActiveTab
    ? [...group.tabHistory, previousActiveTab]
    : [...group.tabHistory];
  return { ...group, activeTab: newActiveTab, tabHistory: newHistory };
};

const setActiveTabPreserveHistory = (
  group: EditorGroupData,
  newActiveTab: string
): EditorGroupData => {
  if (group.activeTab === newActiveTab) return group;
  return { ...group, activeTab: newActiveTab };
};

const getLastActiveCandidate = (
  group: EditorGroupData,
  tabs: Tab[]
): { tabId: string | null; updatedHistory: string[] } => {
  const tabIds = new Set(tabs.map((tab) => tab.id));
  const history = [...group.tabHistory];
  while (history.length > 0) {
    const candidate = history.pop()!;
    if (tabIds.has(candidate)) {
      return { tabId: candidate, updatedHistory: history };
    }
  }
  return { tabId: null, updatedHistory: [] };
};

const normalizeGroupPositions = (groups: EditorGroupData[]): EditorGroupData[] => {
  if (groups.length === 0) return groups;
  if (groups.length === 1) {
    return [{ ...groups[0]!, position: { x: 0, y: 0, width: 1, height: 1 } }];
  }

  const eps = 0.0001;
  const positions = new Map(groups.map((group) => [group.id, { ...group.position }]));

  const rangesOverlap = (aStart: number, aLen: number, bStart: number, bLen: number) =>
    aStart < bStart + bLen - eps && bStart < aStart + aLen - eps;

  let changed = true;
  let iterations = 0;
  while (changed && iterations < 10) {
    changed = false;
    iterations++;

    for (const group of groups) {
      const pos = positions.get(group.id)!;

      let minY = 0;
      for (const other of groups) {
        if (other.id === group.id) continue;
        const otherPos = positions.get(other.id)!;
        if (rangesOverlap(pos.x, pos.width, otherPos.x, otherPos.width) && otherPos.y + otherPos.height <= pos.y + eps) {
          minY = Math.max(minY, otherPos.y + otherPos.height);
        }
      }
      if (pos.y - minY > eps) {
        const bottom = pos.y + pos.height;
        pos.y = minY;
        pos.height = bottom - minY;
        changed = true;
      }

      let maxBottom = 1;
      for (const other of groups) {
        if (other.id === group.id) continue;
        const otherPos = positions.get(other.id)!;
        if (rangesOverlap(pos.x, pos.width, otherPos.x, otherPos.width) && otherPos.y >= pos.y + pos.height - eps) {
          maxBottom = Math.min(maxBottom, otherPos.y);
        }
      }
      if (maxBottom - (pos.y + pos.height) > eps) {
        pos.height = maxBottom - pos.y;
        changed = true;
      }

      let minX = 0;
      for (const other of groups) {
        if (other.id === group.id) continue;
        const otherPos = positions.get(other.id)!;
        if (rangesOverlap(pos.y, pos.height, otherPos.y, otherPos.height) && otherPos.x + otherPos.width <= pos.x + eps) {
          minX = Math.max(minX, otherPos.x + otherPos.width);
        }
      }
      if (pos.x - minX > eps) {
        const right = pos.x + pos.width;
        pos.x = minX;
        pos.width = right - minX;
        changed = true;
      }

      let maxRight = 1;
      for (const other of groups) {
        if (other.id === group.id) continue;
        const otherPos = positions.get(other.id)!;
        if (rangesOverlap(pos.y, pos.height, otherPos.y, otherPos.height) && otherPos.x >= pos.x + pos.width - eps) {
          maxRight = Math.min(maxRight, otherPos.x);
        }
      }
      if (maxRight - (pos.x + pos.width) > eps) {
        pos.width = maxRight - pos.x;
        changed = true;
      }
    }
  }

  return groups.map((group) => ({
    ...group,
    position: positions.get(group.id) || group.position,
  }));
};

function createWritableTabsStateStore(initialState: TabsState): TabsStateStoreLike {
  const { subscribe, set, update } = writable<TabsState>(cloneTabsState(initialState));

  let currentState = cloneTabsState(initialState);
  subscribe((state) => {
    currentState = cloneTabsState(state);
  });

  return {
    subscribe,
    set: (state) => set(cloneTabsState(state)),
    update: (updater) => {
      update((state) => cloneTabsState(updater(cloneTabsState(state))));
    },
    get: () => cloneTabsState(currentState),
  };
}

function createRecordingTabStateEffects(
  mode: 'active' | 'shadow',
  effectLog: TabsEffectCall[]
): TabsSideEffects {
  return {
    clearTabState: (tabId) => {
      effectLog.push({ target: 'tab-state.clearTabState', args: [tabId] });
      if (mode === 'active') {
        clearTabState(tabId);
      }
    },
    clearTabStates: (tabIds) => {
      effectLog.push({ target: 'tab-state.clearTabStates', args: [[...tabIds]] });
      if (mode === 'active') {
        clearTabStates(tabIds);
      }
    },
    copyTabState: (sourceTabId, targetTabId) => {
      effectLog.push({ target: 'tab-state.copyTabState', args: [sourceTabId, targetTabId] });
      if (mode === 'active') {
        copyTabState(sourceTabId, targetTabId);
      }
    },
    resetTabStateStore: () => {
      effectLog.push({ target: 'tab-state.resetTabStateStore', args: [] });
      if (mode === 'active') {
        resetTabStateStore();
      }
    },
  };
}

function createTabsModule(
  stateStore: TabsStateStoreLike,
  effects: TabsSideEffects,
  idFactory: TabsIdFactory
): TabsModuleLike {
  const store: TabsStoreLike = {
    subscribe: stateStore.subscribe,
    set: (state) => {
      stateStore.set(cloneTabsState(state));
    },
    update: (updater) => {
      stateStore.update((state) => updater(cloneTabsState(state)));
    },
    get: () => stateStore.get(),
    reset: () => {
      effects.resetTabStateStore();
      stateStore.set(createInitialState(idFactory));
    },
  };

  const actions: TabActionsLike = {
    createTab: (tabData, groupId) => {
      const newTab: Tab = {
        ...tabData,
        id: idFactory.createTabId(),
      };

      store.update((state) => {
        const targetGroupId = groupId || state.activeGroupId;
        const groups = state.groups.map((group) => {
          if (group.id !== targetGroupId) {
            return group;
          }

          const existingTab = tabData.path
            ? group.tabs.find((tab) => tab.path === tabData.path)
            : null;

          if (existingTab) {
            const shouldUpdate =
              (tabData.component && !existingTab.component) || tabData.props;
            if (shouldUpdate) {
              const updatedTabs = group.tabs.map((tab) =>
                tab.id === existingTab.id
                  ? {
                      ...tab,
                      component: tabData.component ?? tab.component,
                      props: tabData.props ?? tab.props,
                      title: tabData.title ?? tab.title,
                    }
                  : tab
              );
              return setActiveTab({ ...group, tabs: updatedTabs }, existingTab.id);
            }
            return setActiveTab(group, existingTab.id);
          }

          return setActiveTab({ ...group, tabs: [...group.tabs, newTab] }, newTab.id);
        });

        return { ...state, groups, activeGroupId: targetGroupId };
      });

      return newTab;
    },
    closeTab: (tabId, groupId) => {
      let didClose = false;
      store.update((state) => {
        const groups = state.groups.map((group) => {
          if (groupId && group.id !== groupId) return group;

          const tabIndex = group.tabs.findIndex((tab) => tab.id === tabId);
          if (tabIndex === -1) return group;

          const tab = group.tabs[tabIndex];
          if (!tab || tab.closable === false) return group;
          didClose = true;

          const newTabs = group.tabs.filter((tabEntry) => tabEntry.id !== tabId);

          if (group.activeTab === tabId) {
            const cleanHistory = group.tabHistory.filter((id) => id !== tabId);
            const groupWithCleanHistory = { ...group, tabs: newTabs, tabHistory: cleanHistory };

            if (newTabs.length > 0) {
              const { tabId: lastActive, updatedHistory } = getLastActiveCandidate(groupWithCleanHistory, newTabs);
              if (lastActive) {
                return { ...groupWithCleanHistory, activeTab: lastActive, tabHistory: updatedHistory };
              }

              const newIndex = Math.min(tabIndex, newTabs.length - 1);
              const fallbackTab = newTabs[newIndex]?.id ?? '';
              return { ...groupWithCleanHistory, activeTab: fallbackTab, tabHistory: [] };
            }

            return { ...groupWithCleanHistory, activeTab: '' };
          }

          return { ...group, tabs: newTabs, tabHistory: group.tabHistory.filter((id) => id !== tabId) };
        });

        const nonEmptyGroups = groups.filter((group) => group.tabs.length > 0);
        const remainingGroups = nonEmptyGroups.length > 0 ? nonEmptyGroups : [createDefaultGroup(idFactory)];
        const filteredGroups =
          remainingGroups.length < state.groups.length
            ? normalizeGroupPositions(remainingGroups)
            : remainingGroups;

        let newActiveGroupId = state.activeGroupId;
        if (!filteredGroups.some((group) => group.id === state.activeGroupId)) {
          newActiveGroupId = filteredGroups[0]!.id;
        }

        return { ...state, groups: filteredGroups, activeGroupId: newActiveGroupId };
      });

      if (didClose) {
        effects.clearTabState(tabId);
      }
    },
    closeOtherTabs: (tabId, groupId) => {
      const removedTabIds: string[] = [];
      store.update((state) => {
        const groups = state.groups.map((group) => {
          if (groupId && group.id !== groupId) return group;

          const newTabs = group.tabs.filter((tab) => tab.id === tabId || tab.closable === false);
          group.tabs.forEach((tab) => {
            if (!newTabs.some((newTab) => newTab.id === tab.id)) {
              removedTabIds.push(tab.id);
            }
          });
          const nextActiveTab = newTabs.find((tab) => tab.id === tabId)?.id || newTabs[0]?.id || '';
          return { ...group, tabs: newTabs, activeTab: nextActiveTab, tabHistory: [] };
        });

        return { ...state, groups };
      });

      if (removedTabIds.length > 0) {
        effects.clearTabStates(removedTabIds);
      }
    },
    switchToTab: (tabId, groupId) => {
      store.update((state) => {
        const groups = state.groups.map((group) => {
          if (groupId && group.id !== groupId) return group;

          const tab = group.tabs.find((entry) => entry.id === tabId);
          if (!tab) return group;

          return setActiveTab(group, tabId);
        });

        const newActiveGroupId = groupId || state.activeGroupId;
        return { ...state, groups, activeGroupId: newActiveGroupId };
      });
    },
    setActiveGroup: (groupId) => {
      store.update((state) => {
        const group = state.groups.find((entry) => entry.id === groupId);
        if (!group) return state;
        if (state.activeGroupId === groupId) return state;
        return { ...state, activeGroupId: groupId };
      });
    },
    updateTab: (tabId, updates, groupId) => {
      store.update((state) => ({
        ...state,
        groups: state.groups.map((group) => {
          if (groupId && group.id !== groupId) return group;
          return {
            ...group,
            tabs: group.tabs.map((tab) => (tab.id === tabId ? { ...tab, ...updates } : tab)),
          };
        }),
      }));
    },
    splitGroup: (direction, sourceGroupId) => {
      let sourceTabId: string | null = null;
      let newTabId: string | null = null;

      store.update((state) => {
        const sourceGroup = state.groups.find((group) => group.id === sourceGroupId);
        if (!sourceGroup || sourceGroup.tabs.length === 0) return state;

        const newGroupId = idFactory.createGroupId();
        const activeTab = sourceGroup.tabs.find((tab) => tab.id === sourceGroup.activeTab);
        if (!activeTab) return state;

        sourceTabId = activeTab.id;
        newTabId = idFactory.createCopiedTabId(activeTab.id);

        const newGroup: EditorGroupData = {
          id: newGroupId,
          tabs: [{ ...activeTab, id: newTabId }],
          activeTab: newTabId,
          tabHistory: [],
          splitDirection: direction,
          position:
            direction === 'horizontal'
              ? {
                  x: sourceGroup.position.x + sourceGroup.position.width / 2,
                  y: sourceGroup.position.y,
                  width: sourceGroup.position.width / 2,
                  height: sourceGroup.position.height,
                }
              : {
                  x: sourceGroup.position.x,
                  y: sourceGroup.position.y + sourceGroup.position.height / 2,
                  width: sourceGroup.position.width,
                  height: sourceGroup.position.height / 2,
                },
        };

        const updatedSourceGroup = {
          ...sourceGroup,
          position:
            direction === 'horizontal'
              ? { ...sourceGroup.position, width: sourceGroup.position.width / 2 }
              : { ...sourceGroup.position, height: sourceGroup.position.height / 2 },
        };

        const groups = state.groups.map((group) =>
          group.id === sourceGroupId ? updatedSourceGroup : group
        );

        return {
          ...state,
          groups: [...groups, newGroup],
          activeGroupId: newGroupId,
        };
      });

      if (sourceTabId && newTabId) {
        effects.copyTabState(sourceTabId, newTabId);
      }
    },
    splitRight: (tabId, sourceGroupId) => {
      let newTabId: string | null = null;

      store.update((state) => {
        const sourceGroup = state.groups.find((group) => group.id === sourceGroupId);
        if (!sourceGroup) return state;

        const tab = sourceGroup.tabs.find((entry) => entry.id === tabId);
        if (!tab) return state;

        const newGroupId = idFactory.createGroupId();
        newTabId = idFactory.createCopiedTabId(tab.id);

        const newGroup: EditorGroupData = {
          id: newGroupId,
          tabs: [{ ...tab, id: newTabId }],
          activeTab: newTabId,
          tabHistory: [],
          splitDirection: 'horizontal',
          position: {
            x: sourceGroup.position.x + sourceGroup.position.width / 2,
            y: sourceGroup.position.y,
            width: sourceGroup.position.width / 2,
            height: sourceGroup.position.height,
          },
        };

        const updatedSourceGroup = {
          ...sourceGroup,
          position: { ...sourceGroup.position, width: sourceGroup.position.width / 2 },
        };

        const groups = state.groups.map((group) =>
          group.id === sourceGroupId ? updatedSourceGroup : group
        );

        return {
          ...state,
          groups: [...groups, newGroup],
          activeGroupId: newGroupId,
        };
      });

      if (newTabId) {
        effects.copyTabState(tabId, newTabId);
      }
    },
    closeGroup: (groupId) => {
      let removedIds: string[] = [];
      store.update((state) => {
        if (state.groups.length <= 1) return state;

        const closingGroup = state.groups.find((group) => group.id === groupId);
        if (closingGroup) {
          removedIds = closingGroup.tabs.map((tab) => tab.id);
        }

        const remaining = state.groups.filter((group) => group.id !== groupId);
        const groups = normalizeGroupPositions(remaining);
        const newActiveGroupId =
          state.activeGroupId === groupId ? (groups[0]?.id ?? state.activeGroupId) : state.activeGroupId;

        return { ...state, groups, activeGroupId: newActiveGroupId };
      });

      if (removedIds.length > 0) {
        effects.clearTabStates(removedIds);
      }
    },
    moveTabToGroup: (tabId, targetGroupId, sourceGroupId, insertIndex) => {
      let removedDuplicateIds: string[] = [];
      store.update((state) => {
        const sourceGroup = state.groups.find((group) => group.id === sourceGroupId);
        const targetGroup = state.groups.find((group) => group.id === targetGroupId);

        if (!sourceGroup || !targetGroup) return state;
        if (sourceGroupId === targetGroupId) return state;

        const tab = sourceGroup.tabs.find((entry) => entry.id === tabId);
        if (!tab) return state;

        const dedupKey = getTabDedupKey(tab);
        const targetTabs = targetGroup.tabs;
        const duplicateIndexes = dedupKey
          ? targetTabs.reduce<number[]>((acc, entry, index) => {
              if (getTabDedupKey(entry) === dedupKey) acc.push(index);
              return acc;
            }, [])
          : [];
        const hasDuplicate = duplicateIndexes.length > 0;
        removedDuplicateIds = hasDuplicate
          ? targetTabs.filter((entry) => getTabDedupKey(entry) === dedupKey).map((entry) => entry.id)
          : [];

        const newSourceTabs = sourceGroup.tabs.filter((entry) => entry.id !== tabId);
        let newSourceActiveTab = sourceGroup.activeTab;
        let newSourceHistory = sourceGroup.tabHistory.filter((id) => id !== tabId);
        if (sourceGroup.activeTab === tabId) {
          const cleanHistory = sourceGroup.tabHistory.filter((id) => id !== tabId);
          const { tabId: lastActive, updatedHistory } = getLastActiveCandidate(
            { ...sourceGroup, tabHistory: cleanHistory },
            newSourceTabs
          );
          newSourceActiveTab = lastActive ?? (newSourceTabs[0]?.id || '');
          newSourceHistory = updatedHistory;
        }

        const groups = state.groups.map((group) => {
          if (group.id === sourceGroupId) {
            const updatedGroup = { ...group, tabs: newSourceTabs, tabHistory: newSourceHistory };
            if (sourceGroup.activeTab === tabId) {
              return setActiveTabPreserveHistory(updatedGroup, newSourceActiveTab);
            }
            return updatedGroup;
          }

          if (group.id === targetGroupId) {
            const filteredTargetTabs = hasDuplicate
              ? group.tabs.filter((entry) => getTabDedupKey(entry) !== dedupKey)
              : [...group.tabs];

            let adjustedInsertIndex = insertIndex;
            if (hasDuplicate && insertIndex !== undefined) {
              const removedBefore = duplicateIndexes.filter((index) => index < insertIndex).length;
              adjustedInsertIndex = insertIndex - removedBefore;
            }

            const resolvedInsertIndex =
              insertIndex !== undefined
                ? Math.max(0, Math.min(adjustedInsertIndex ?? 0, filteredTargetTabs.length))
                : hasDuplicate
                  ? Math.min(duplicateIndexes[0] ?? filteredTargetTabs.length, filteredTargetTabs.length)
                  : filteredTargetTabs.length;

            const newTargetTabs = [...filteredTargetTabs];
            newTargetTabs.splice(resolvedInsertIndex, 0, tab);
            return setActiveTab({ ...group, tabs: newTargetTabs }, tab.id);
          }

          return group;
        });

        const nonEmptyGroups = groups.filter((group) => group.tabs.length > 0);
        const remainingGroups = nonEmptyGroups.length > 0 ? nonEmptyGroups : [createDefaultGroup(idFactory)];
        const filteredGroups =
          remainingGroups.length < state.groups.length
            ? normalizeGroupPositions(remainingGroups)
            : remainingGroups;

        const newActiveGroupId = filteredGroups.some((group) => group.id === targetGroupId)
          ? targetGroupId
          : filteredGroups[0]!.id;

        return {
          ...state,
          groups: filteredGroups,
          activeGroupId: newActiveGroupId,
        };
      });

      if (removedDuplicateIds.length > 0) {
        effects.clearTabStates(removedDuplicateIds);
      }
    },
    reorderTabs: (fromIndex, toIndex, groupId) => {
      store.update((state) => {
        const targetGroupId = groupId || state.activeGroupId;
        const groups = state.groups.map((group) => {
          if (group.id !== targetGroupId) return group;

          const tabs = [...group.tabs];
          if (fromIndex < 0 || fromIndex >= tabs.length) return group;
          if (toIndex < 0 || toIndex >= tabs.length) return group;
          if (fromIndex === toIndex) return group;

          const [movedTab] = tabs.splice(fromIndex, 1);
          if (!movedTab) return group;
          tabs.splice(toIndex, 0, movedTab);

          return setActiveTab({ ...group, tabs }, movedTab.id);
        });

        return { ...state, groups };
      });
    },
    updateGroupPositions: (updates) => {
      store.update((state) => ({
        ...state,
        groups: state.groups.map((group) => {
          const newPos = updates[group.id];
          return newPos ? { ...group, position: newPos } : group;
        }),
      }));
    },
    copyTabToGroup: (tabId, targetGroupId, sourceGroupId, insertIndex) => {
      let newTabId: string | null = null;
      store.update((state) => {
        const sourceGroup = state.groups.find((group) => group.id === sourceGroupId);
        const targetGroup = state.groups.find((group) => group.id === targetGroupId);

        if (!sourceGroup || !targetGroup) return state;
        if (sourceGroupId === targetGroupId) return state;

        const tab = sourceGroup.tabs.find((entry) => entry.id === tabId);
        if (!tab) return state;

        const dedupKey = getTabDedupKey(tab);
        const existingTab = dedupKey
          ? targetGroup.tabs.find((entry) => getTabDedupKey(entry) === dedupKey)
          : null;

        if (existingTab) {
          const groups = state.groups.map((group) =>
            group.id === targetGroupId ? setActiveTab(group, existingTab.id) : group
          );
          return { ...state, groups, activeGroupId: targetGroupId };
        }

        newTabId = idFactory.createCopiedTabId(tab.id);
        const newTab: Tab = { ...tab, id: newTabId };

        const newTargetTabs = [...targetGroup.tabs];
        if (insertIndex !== undefined && insertIndex >= 0 && insertIndex <= newTargetTabs.length) {
          newTargetTabs.splice(insertIndex, 0, newTab);
        } else {
          newTargetTabs.push(newTab);
        }

        const groups = state.groups.map((group) => {
          if (group.id === targetGroupId) {
            return setActiveTab({ ...group, tabs: newTargetTabs }, newTab.id);
          }
          return group;
        });

        return { ...state, groups, activeGroupId: targetGroupId };
      });

      if (newTabId) {
        effects.copyTabState(tabId, newTabId);
      }
    },
    openByPath: (path, tabData, groupId) => {
      const state = store.get();
      const targetGroupId = groupId || state.activeGroupId;
      const group = state.groups.find((entry) => entry.id === targetGroupId);

      if (!group) return null;

      const existingTab = group.tabs.find((tab) => tab.path === path);
      if (existingTab) {
        actions.switchToTab(existingTab.id, targetGroupId);
        return existingTab;
      }

      return actions.createTab({ ...tabData, path }, targetGroupId);
    },
  };

  return { store, actions };
}

function createParityAwareTabsModule(
  primary: TabsModuleLike,
  shadow: TabsModuleLike,
  primaryEffectsLog: TabsEffectCall[],
  shadowEffectsLog: TabsEffectCall[],
  reportMismatch: (context: TabsMismatchContext) => void
): TabsModuleLike {
  const listeners = new Set<Subscriber<TabsState>>();
  let currentState = primary.store.get();

  primary.store.subscribe((state) => {
    currentState = cloneTabsState(state);
    for (const listener of listeners) {
      listener(cloneTabsState(state));
    }
  });

  function compare(action: string, primaryResult?: Tab | null, shadowResult?: Tab | null): void {
    const primaryState = primary.store.get();
    const shadowState = shadow.store.get();

    if (
      !areEqual(normalizeTabsState(primaryState), normalizeTabsState(shadowState)) ||
      !areEqual(primaryEffectsLog, shadowEffectsLog) ||
      !areEqual(primaryResult ? normalizeTab(primaryResult) : primaryResult ?? null, shadowResult ? normalizeTab(shadowResult) : shadowResult ?? null)
    ) {
      reportMismatch({
        action,
        primaryState,
        shadowState,
        primaryEffects: [...primaryEffectsLog],
        shadowEffects: [...shadowEffectsLog],
        primaryResult,
        shadowResult,
      });
    }
  }

  const store: TabsStoreLike = {
    subscribe: (run) => {
      run(cloneTabsState(currentState));
      listeners.add(run);
      return () => {
        listeners.delete(run);
      };
    },
    set: (state) => {
      const nextState = cloneTabsState(state);
      primary.store.set(nextState);
      shadow.store.set(nextState);
      compare('set');
    },
    update: (updater) => {
      const nextState = updater(primary.store.get());
      primary.store.set(nextState);
      shadow.store.set(nextState);
      compare('update');
    },
    get: () => cloneTabsState(currentState),
    reset: () => {
      primary.store.reset();
      shadow.store.reset();
      compare('reset');
    },
  };

  const actions: TabActionsLike = {
    createTab: (tabData, groupId) => {
      const primaryResult = primary.actions.createTab(tabData, groupId);
      const shadowResult = shadow.actions.createTab(tabData, groupId);
      compare('createTab', primaryResult, shadowResult);
      return primaryResult;
    },
    closeTab: (tabId, groupId) => {
      primary.actions.closeTab(tabId, groupId);
      shadow.actions.closeTab(tabId, groupId);
      compare('closeTab');
    },
    closeOtherTabs: (tabId, groupId) => {
      primary.actions.closeOtherTabs(tabId, groupId);
      shadow.actions.closeOtherTabs(tabId, groupId);
      compare('closeOtherTabs');
    },
    switchToTab: (tabId, groupId) => {
      primary.actions.switchToTab(tabId, groupId);
      shadow.actions.switchToTab(tabId, groupId);
      compare('switchToTab');
    },
    setActiveGroup: (groupId) => {
      primary.actions.setActiveGroup(groupId);
      shadow.actions.setActiveGroup(groupId);
      compare('setActiveGroup');
    },
    updateTab: (tabId, updates, groupId) => {
      primary.actions.updateTab(tabId, updates, groupId);
      shadow.actions.updateTab(tabId, updates, groupId);
      compare('updateTab');
    },
    splitGroup: (direction, sourceGroupId) => {
      primary.actions.splitGroup(direction, sourceGroupId);
      shadow.actions.splitGroup(direction, sourceGroupId);
      compare('splitGroup');
    },
    splitRight: (tabId, sourceGroupId) => {
      primary.actions.splitRight(tabId, sourceGroupId);
      shadow.actions.splitRight(tabId, sourceGroupId);
      compare('splitRight');
    },
    closeGroup: (groupId) => {
      primary.actions.closeGroup(groupId);
      shadow.actions.closeGroup(groupId);
      compare('closeGroup');
    },
    moveTabToGroup: (tabId, targetGroupId, sourceGroupId, insertIndex) => {
      primary.actions.moveTabToGroup(tabId, targetGroupId, sourceGroupId, insertIndex);
      shadow.actions.moveTabToGroup(tabId, targetGroupId, sourceGroupId, insertIndex);
      compare('moveTabToGroup');
    },
    reorderTabs: (fromIndex, toIndex, groupId) => {
      primary.actions.reorderTabs(fromIndex, toIndex, groupId);
      shadow.actions.reorderTabs(fromIndex, toIndex, groupId);
      compare('reorderTabs');
    },
    updateGroupPositions: (updates) => {
      primary.actions.updateGroupPositions(updates);
      shadow.actions.updateGroupPositions(updates);
      compare('updateGroupPositions');
    },
    copyTabToGroup: (tabId, targetGroupId, sourceGroupId, insertIndex) => {
      primary.actions.copyTabToGroup(tabId, targetGroupId, sourceGroupId, insertIndex);
      shadow.actions.copyTabToGroup(tabId, targetGroupId, sourceGroupId, insertIndex);
      compare('copyTabToGroup');
    },
    openByPath: (path, tabData, groupId) => {
      const primaryResult = primary.actions.openByPath(path, tabData, groupId);
      const shadowResult = shadow.actions.openByPath(path, tabData, groupId);
      compare('openByPath', primaryResult, shadowResult);
      return primaryResult;
    },
  };

  return { store, actions };
}

function createModule(): TabsModuleLike {
  const enableRunesTabsStore = config.isRunesTabsStoreEnabled();
  const enableShadowParity = config.isRunesStoreShadowParityEnabled();
  const idFactories = createPairedTabsIdFactories(Date.now);
  const primaryEffectsLog: TabsEffectCall[] = [];
  const shadowEffectsLog: TabsEffectCall[] = [];
  const primaryInitialState = createInitialState(idFactories.primary);
  const shadowInitialState = createInitialState(idFactories.shadow);

  const writableIdFactory = enableRunesTabsStore ? idFactories.shadow : idFactories.primary;
  const runesIdFactory = enableRunesTabsStore ? idFactories.primary : idFactories.shadow;

  const writableModule = createTabsModule(
    createWritableTabsStateStore(enableRunesTabsStore ? shadowInitialState : primaryInitialState),
    createRecordingTabStateEffects(enableRunesTabsStore ? 'shadow' : 'active', enableRunesTabsStore ? shadowEffectsLog : primaryEffectsLog),
    writableIdFactory
  );
  const runesModule = createTabsModule(
    createRunesTabsStateStore(enableRunesTabsStore ? primaryInitialState : shadowInitialState),
    createRecordingTabStateEffects(enableRunesTabsStore ? 'active' : 'shadow', enableRunesTabsStore ? primaryEffectsLog : shadowEffectsLog),
    runesIdFactory
  );

  const primaryModule = enableRunesTabsStore ? runesModule : writableModule;
  const shadowModule = enableRunesTabsStore ? writableModule : runesModule;

  if (!enableShadowParity) {
    return primaryModule;
  }

  return createParityAwareTabsModule(primaryModule, shadowModule, primaryEffectsLog, shadowEffectsLog, (context) => {
    console.error('[tabs.store] parity mismatch', {
      action: context.action,
      primaryState: normalizeTabsState(context.primaryState),
      shadowState: normalizeTabsState(context.shadowState),
      primaryEffects: context.primaryEffects,
      shadowEffects: context.shadowEffects,
      primaryResult: context.primaryResult ? normalizeTab(context.primaryResult) : context.primaryResult ?? null,
      shadowResult: context.shadowResult ? normalizeTab(context.shadowResult) : context.shadowResult ?? null,
    });
  });
}

const moduleInstance = createModule();

export const tabsStore = moduleInstance.store;

export const editorGroups = derived(tabsStore, ($state) => $state.groups);

export const activeGroup = derived(tabsStore, ($state) =>
  $state.groups.find((group) => group.id === $state.activeGroupId) || $state.groups[0]
);

export const activeGroupId = derived(tabsStore, ($state) => $state.activeGroupId);

export const globalActiveTab = derived(tabsStore, ($state) => {
  const group = $state.groups.find((entry) => entry.id === $state.activeGroupId);
  if (!group) return null;
  return group.tabs.find((tab) => tab.id === group.activeTab) || null;
});

export const allTabs = derived(tabsStore, ($state) =>
  $state.groups.flatMap((group) => group.tabs)
);

export const tabActions = moduleInstance.actions;

export function setupTabKeyboardShortcuts(): () => void {
  const handleKeyDown = (event: KeyboardEvent) => {
    const state = tabsStore.get();
    const group = state.groups.find((entry) => entry.id === state.activeGroupId);

    if (event.ctrlKey && event.key === 'w') {
      event.preventDefault();
      if (group?.activeTab) {
        tabActions.closeTab(group.activeTab, group.id);
      }
      return;
    }

    if (event.ctrlKey && event.key === 'Tab' && !event.shiftKey) {
      event.preventDefault();
      if (group && group.tabs.length > 1) {
        const currentIndex = group.tabs.findIndex((tab) => tab.id === group.activeTab);
        const nextIndex = (currentIndex + 1) % group.tabs.length;
        const nextTab = group.tabs[nextIndex];
        if (nextTab) {
          tabActions.switchToTab(nextTab.id, group.id);
        }
      }
      return;
    }

    if (event.ctrlKey && event.key === 'Tab' && event.shiftKey) {
      event.preventDefault();
      if (group && group.tabs.length > 1) {
        const currentIndex = group.tabs.findIndex((tab) => tab.id === group.activeTab);
        const prevIndex = (currentIndex - 1 + group.tabs.length) % group.tabs.length;
        const prevTab = group.tabs[prevIndex];
        if (prevTab) {
          tabActions.switchToTab(prevTab.id, group.id);
        }
      }
      return;
    }

    const isTabBarFocused = document.activeElement?.closest('[data-tab-bar]');
    if (!isTabBarFocused || !group || group.tabs.length === 0) return;

    const currentIndex = group.tabs.findIndex((tab) => tab.id === group.activeTab);

    if (event.key === 'ArrowLeft') {
      event.preventDefault();
      if (currentIndex > 0) {
        const prevTab = group.tabs[currentIndex - 1];
        if (prevTab) tabActions.switchToTab(prevTab.id, group.id);
      }
      return;
    }

    if (event.key === 'ArrowRight') {
      event.preventDefault();
      if (currentIndex < group.tabs.length - 1) {
        const nextTab = group.tabs[currentIndex + 1];
        if (nextTab) tabActions.switchToTab(nextTab.id, group.id);
      }
      return;
    }

    if (event.key === 'Home') {
      event.preventDefault();
      const firstTab = group.tabs[0];
      if (firstTab) tabActions.switchToTab(firstTab.id, group.id);
      return;
    }

    if (event.key === 'End') {
      event.preventDefault();
      const lastTab = group.tabs[group.tabs.length - 1];
      if (lastTab) tabActions.switchToTab(lastTab.id, group.id);
      return;
    }

    if (event.key === 'Delete') {
      event.preventDefault();
      if (group.activeTab) {
        tabActions.closeTab(group.activeTab, group.id);
      }
    }
  };

  document.addEventListener('keydown', handleKeyDown);
  return () => document.removeEventListener('keydown', handleKeyDown);
}

export const __internal = {
  cloneTab,
  cloneEditorGroup,
  cloneTabsState,
  normalizeTab,
  normalizeTabsState,
  createDefaultTabsIdFactory,
  createPairedTabsIdFactories,
  createDefaultGroup,
  createInitialState,
  createWritableTabsStateStore,
  createRecordingTabStateEffects,
  createTabsModule,
  createParityAwareTabsModule,
  createRunesTabsStateStore,
};
