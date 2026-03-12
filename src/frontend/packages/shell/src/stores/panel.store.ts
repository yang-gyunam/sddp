/**
 * Panel Store - Bottom panel and Status bar management
 *
 * L-3 migration note:
 * This module keeps the public store contracts stable while shadow-validating
 * the runes implementation against the legacy writable implementation.
 */

import { writable, type Subscriber, type Unsubscriber } from 'svelte/store';
import { config } from '../core/services/ConfigurationManager';
import type { BottomPanelState, PanelTab, StatusBarItem, StatusBarState } from '../types';
import { clamp } from '../utils/number.utils';
import { createRunesPanelStore, createRunesStatusBarStore } from './panel.store.runes';

export interface PanelStoreLike {
  subscribe: (run: Subscriber<BottomPanelState>) => Unsubscriber;
  show: () => void;
  hide: () => void;
  toggle: () => void;
  setActiveTab: (tabId: string) => void;
  setHeight: (height: number) => void;
  startResize: () => void;
  endResize: () => void;
  updateTabBadge: (tabId: string, badge: number) => void;
  addTab: (tab: PanelTab) => void;
  removeTab: (tabId: string) => void;
  reset: () => void;
  get: () => BottomPanelState;
}

export interface StatusBarStoreLike {
  subscribe: (run: Subscriber<StatusBarState>) => Unsubscriber;
  addItem: (item: StatusBarItem) => void;
  updateItem: (id: string, updates: Partial<StatusBarItem>) => void;
  removeItem: (id: string) => void;
  setCurrentUser: (user: { name: string; email: string } | null) => void;
  toggle: () => void;
  reset: () => void;
  get: () => StatusBarState;
}

type PanelMismatchContext = {
  action: string;
  primaryState: BottomPanelState;
  shadowState: BottomPanelState;
};

type StatusBarMismatchContext = {
  action: string;
  primaryState: StatusBarState;
  shadowState: StatusBarState;
};

const DEFAULT_PANEL_HEIGHT = 200;
const MIN_PANEL_HEIGHT = 100;
const MAX_PANEL_HEIGHT = 600;

const DEFAULT_TABS: PanelTab[] = [
  { id: 'problems', label: 'Problems', icon: 'alert-circle', badge: 0 },
];

const initialPanelState: BottomPanelState = {
  collapsed: true,
  height: DEFAULT_PANEL_HEIGHT,
  minHeight: MIN_PANEL_HEIGHT,
  maxHeight: MAX_PANEL_HEIGHT,
  activeTab: 'problems',
  tabs: DEFAULT_TABS,
  resizing: false,
};

const initialStatusBarState: StatusBarState = {
  items: [],
  currentUser: null,
  visible: true,
};

function clonePanelTab(tab: PanelTab): PanelTab {
  return {
    ...tab,
    props: tab.props ? { ...tab.props } : undefined,
  };
}

function clonePanelState(state: BottomPanelState): BottomPanelState {
  return {
    ...state,
    tabs: state.tabs.map(clonePanelTab),
  };
}

function cloneStatusBarState(state: StatusBarState): StatusBarState {
  return {
    ...state,
    items: state.items.map((item) => ({ ...item })),
    currentUser: state.currentUser ? { ...state.currentUser } : null,
  };
}

function normalizePanelState(state: BottomPanelState): unknown {
  return {
    collapsed: state.collapsed,
    height: state.height,
    minHeight: state.minHeight,
    maxHeight: state.maxHeight,
    activeTab: state.activeTab,
    resizing: state.resizing,
    tabs: state.tabs.map((tab) => ({
      id: tab.id,
      label: tab.label,
      icon: tab.icon ?? null,
      badge: tab.badge ?? null,
      componentType: tab.component == null ? null : typeof tab.component,
      props: tab.props ?? null,
    })),
  };
}

function normalizeStatusBarState(state: StatusBarState): unknown {
  return {
    visible: state.visible,
    currentUser: state.currentUser,
    items: state.items.map((item) => ({
      id: item.id,
      text: item.text,
      icon: item.icon ?? null,
      tooltip: item.tooltip ?? null,
      priority: item.priority,
      alignment: item.alignment,
      color: item.color ?? null,
      command: item.command ?? null,
      visible: item.visible ?? null,
    })),
  };
}

function areEqual(left: unknown, right: unknown): boolean {
  return JSON.stringify(left) === JSON.stringify(right);
}

function createWritablePanelStore(initialState: BottomPanelState): PanelStoreLike {
  const { subscribe, set, update } = writable<BottomPanelState>(clonePanelState(initialState));

  let currentState = clonePanelState(initialState);
  subscribe((state) => {
    currentState = clonePanelState(state);
  });

  return {
    subscribe,

    show: () => {
      update((state) => ({ ...state, collapsed: false }));
    },

    hide: () => {
      update((state) => ({ ...state, collapsed: true }));
    },

    toggle: () => {
      update((state) => ({ ...state, collapsed: !state.collapsed }));
    },

    setActiveTab: (tabId: string) => {
      update((state) => ({
        ...state,
        activeTab: tabId,
        collapsed: false,
      }));
    },

    setHeight: (height: number) => {
      update((state) => ({
        ...state,
        height: clamp(height, state.minHeight, state.maxHeight),
      }));
    },

    startResize: () => {
      update((state) => ({ ...state, resizing: true }));
    },

    endResize: () => {
      update((state) => ({ ...state, resizing: false }));
    },

    updateTabBadge: (tabId: string, badge: number) => {
      update((state) => ({
        ...state,
        tabs: state.tabs.map((tab) => (tab.id === tabId ? { ...tab, badge } : tab)),
      }));
    },

    addTab: (tab: PanelTab) => {
      update((state) => ({
        ...state,
        tabs: [...state.tabs, tab],
      }));
    },

    removeTab: (tabId: string) => {
      update((state) => {
        const nextTabs = state.tabs.filter((tab) => tab.id !== tabId);
        return {
          ...state,
          tabs: nextTabs,
          activeTab: state.activeTab === tabId ? nextTabs[0]?.id ?? '' : state.activeTab,
        };
      });
    },

    reset: () => {
      set(clonePanelState(initialState));
    },

    get: () => clonePanelState(currentState),
  };
}

function createWritableStatusBarStore(initialState: StatusBarState): StatusBarStoreLike {
  const { subscribe, set, update } = writable<StatusBarState>(cloneStatusBarState(initialState));

  let currentState = cloneStatusBarState(initialState);
  subscribe((state) => {
    currentState = cloneStatusBarState(state);
  });

  return {
    subscribe,

    addItem: (item: StatusBarItem) => {
      update((state) => {
        const items = state.items.filter((entry) => entry.id !== item.id);
        return {
          ...state,
          items: [...items, { ...item, visible: item.visible ?? true }],
        };
      });
    },

    updateItem: (id: string, updates: Partial<StatusBarItem>) => {
      update((state) => ({
        ...state,
        items: state.items.map((item) => (item.id === id ? { ...item, ...updates } : item)),
      }));
    },

    removeItem: (id: string) => {
      update((state) => ({
        ...state,
        items: state.items.filter((item) => item.id !== id),
      }));
    },

    setCurrentUser: (user: { name: string; email: string } | null) => {
      update((state) => ({ ...state, currentUser: user ? { ...user } : null }));
    },

    toggle: () => {
      update((state) => ({ ...state, visible: !state.visible }));
    },

    reset: () => {
      set(cloneStatusBarState(initialState));
    },

    get: () => cloneStatusBarState(currentState),
  };
}

function createParityAwarePanelStore(
  primary: PanelStoreLike,
  shadow: PanelStoreLike,
  reportMismatch: (context: PanelMismatchContext) => void
): PanelStoreLike {
  const listeners = new Set<Subscriber<BottomPanelState>>();
  let currentState = primary.get();

  primary.subscribe((state) => {
    currentState = clonePanelState(state);
    for (const listener of listeners) {
      listener(clonePanelState(state));
    }
  });

  function subscribe(run: Subscriber<BottomPanelState>): Unsubscriber {
    run(clonePanelState(currentState));
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

    if (!areEqual(normalizePanelState(primaryState), normalizePanelState(shadowState))) {
      reportMismatch({ action, primaryState, shadowState });
    }
  }

  return {
    subscribe,
    show: () => execute('show', primary.show, shadow.show),
    hide: () => execute('hide', primary.hide, shadow.hide),
    toggle: () => execute('toggle', primary.toggle, shadow.toggle),
    setActiveTab: (tabId) => execute('setActiveTab', () => primary.setActiveTab(tabId), () => shadow.setActiveTab(tabId)),
    setHeight: (height) => execute('setHeight', () => primary.setHeight(height), () => shadow.setHeight(height)),
    startResize: () => execute('startResize', primary.startResize, shadow.startResize),
    endResize: () => execute('endResize', primary.endResize, shadow.endResize),
    updateTabBadge: (tabId, badge) => execute('updateTabBadge', () => primary.updateTabBadge(tabId, badge), () => shadow.updateTabBadge(tabId, badge)),
    addTab: (tab) => execute('addTab', () => primary.addTab(tab), () => shadow.addTab(tab)),
    removeTab: (tabId) => execute('removeTab', () => primary.removeTab(tabId), () => shadow.removeTab(tabId)),
    reset: () => execute('reset', primary.reset, shadow.reset),
    get: () => clonePanelState(currentState),
  };
}

function createParityAwareStatusBarStore(
  primary: StatusBarStoreLike,
  shadow: StatusBarStoreLike,
  reportMismatch: (context: StatusBarMismatchContext) => void
): StatusBarStoreLike {
  const listeners = new Set<Subscriber<StatusBarState>>();
  let currentState = primary.get();

  primary.subscribe((state) => {
    currentState = cloneStatusBarState(state);
    for (const listener of listeners) {
      listener(cloneStatusBarState(state));
    }
  });

  function subscribe(run: Subscriber<StatusBarState>): Unsubscriber {
    run(cloneStatusBarState(currentState));
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

    if (!areEqual(normalizeStatusBarState(primaryState), normalizeStatusBarState(shadowState))) {
      reportMismatch({ action, primaryState, shadowState });
    }
  }

  return {
    subscribe,
    addItem: (item) => execute('addItem', () => primary.addItem(item), () => shadow.addItem(item)),
    updateItem: (id, updates) => execute('updateItem', () => primary.updateItem(id, updates), () => shadow.updateItem(id, updates)),
    removeItem: (id) => execute('removeItem', () => primary.removeItem(id), () => shadow.removeItem(id)),
    setCurrentUser: (user) => execute('setCurrentUser', () => primary.setCurrentUser(user), () => shadow.setCurrentUser(user)),
    toggle: () => execute('toggle', primary.toggle, shadow.toggle),
    reset: () => execute('reset', primary.reset, shadow.reset),
    get: () => cloneStatusBarState(currentState),
  };
}

function createPanelStores(): {
  panel: PanelStoreLike;
  statusBar: StatusBarStoreLike;
} {
  const enableRunesPanelStore = config.isRunesPanelStoreEnabled();
  const enableShadowParity = config.isRunesStoreShadowParityEnabled();

  const legacyPanelStore = createWritablePanelStore(initialPanelState);
  const legacyStatusBarStore = createWritableStatusBarStore(initialStatusBarState);

  const runesPanelStore = createRunesPanelStore(initialPanelState);
  const runesStatusBarStore = createRunesStatusBarStore(initialStatusBarState);

  const primaryPanelStore = enableRunesPanelStore ? runesPanelStore : legacyPanelStore;
  const shadowPanelStore = enableRunesPanelStore ? legacyPanelStore : runesPanelStore;
  const primaryStatusBarStore = enableRunesPanelStore ? runesStatusBarStore : legacyStatusBarStore;
  const shadowStatusBarStore = enableRunesPanelStore ? legacyStatusBarStore : runesStatusBarStore;

  return {
    panel: enableShadowParity
      ? createParityAwarePanelStore(primaryPanelStore, shadowPanelStore, ({ action, primaryState, shadowState }) => {
        console.error('[panel.store parity mismatch]', {
          action,
          primaryState: normalizePanelState(primaryState),
          shadowState: normalizePanelState(shadowState),
        });
      })
      : primaryPanelStore,
    statusBar: enableShadowParity
      ? createParityAwareStatusBarStore(primaryStatusBarStore, shadowStatusBarStore, ({ action, primaryState, shadowState }) => {
        console.error('[statusBar.store parity mismatch]', {
          action,
          primaryState: normalizeStatusBarState(primaryState),
          shadowState: normalizeStatusBarState(shadowState),
        });
      })
      : primaryStatusBarStore,
  };
}

const stores = createPanelStores();

export const __internal = {
  clonePanelState,
  cloneStatusBarState,
  normalizePanelState,
  normalizeStatusBarState,
  createWritablePanelStore,
  createWritableStatusBarStore,
  createParityAwarePanelStore,
  createParityAwareStatusBarStore,
  createRunesPanelStore,
  createRunesStatusBarStore,
  initialPanelState: clonePanelState(initialPanelState),
  initialStatusBarState: cloneStatusBarState(initialStatusBarState),
};

export const panel = stores.panel;
export const $panel = initialPanelState;

export const statusBar = stores.statusBar;
export const $statusBar = initialStatusBarState;
