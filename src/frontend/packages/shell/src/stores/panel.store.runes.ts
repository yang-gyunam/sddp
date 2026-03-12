import type { Subscriber, Unsubscriber } from 'svelte/store';
import type {
  BottomPanelState,
  PanelTab,
  StatusBarItem,
  StatusBarState,
} from '../types';
import { clamp } from '../utils/number.utils';
import type { PanelStoreLike, StatusBarStoreLike } from './panel.store';

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

function createSubscriberSet<T>(snapshot: () => T): {
  publish: () => void;
  subscribe: (run: Subscriber<T>) => Unsubscriber;
} {
  const subscribers = new Set<Subscriber<T>>();

  return {
    publish: () => {
      const latest = snapshot();
      for (const subscriber of subscribers) {
        subscriber(latest);
      }
    },
    subscribe: (run) => {
      run(snapshot());
      subscribers.add(run);
      return () => {
        subscribers.delete(run);
      };
    },
  };
}

export function createRunesPanelStore(initialState: BottomPanelState): PanelStoreLike {
  let state = clonePanelState(initialState);
  const subscribers = createSubscriberSet(() => clonePanelState(state));

  function commit(nextState: BottomPanelState): void {
    state = clonePanelState(nextState);
    subscribers.publish();
  }

  return {
    subscribe: subscribers.subscribe,

    show: () => {
      commit({ ...state, collapsed: false });
    },

    hide: () => {
      commit({ ...state, collapsed: true });
    },

    toggle: () => {
      commit({ ...state, collapsed: !state.collapsed });
    },

    setActiveTab: (tabId: string) => {
      commit({
        ...state,
        activeTab: tabId,
        collapsed: false,
      });
    },

    setHeight: (height: number) => {
      commit({
        ...state,
        height: clamp(height, state.minHeight, state.maxHeight),
      });
    },

    startResize: () => {
      commit({ ...state, resizing: true });
    },

    endResize: () => {
      commit({ ...state, resizing: false });
    },

    updateTabBadge: (tabId: string, badge: number) => {
      commit({
        ...state,
        tabs: state.tabs.map((tab) => (tab.id === tabId ? { ...tab, badge } : tab)),
      });
    },

    addTab: (tab: PanelTab) => {
      commit({
        ...state,
        tabs: [...state.tabs, tab],
      });
    },

    removeTab: (tabId: string) => {
      const nextTabs = state.tabs.filter((tab) => tab.id !== tabId);
      commit({
        ...state,
        tabs: nextTabs,
        activeTab: state.activeTab === tabId ? nextTabs[0]?.id ?? '' : state.activeTab,
      });
    },

    reset: () => {
      commit(initialState);
    },

    get: () => clonePanelState(state),
  };
}

export function createRunesStatusBarStore(initialState: StatusBarState): StatusBarStoreLike {
  let state = cloneStatusBarState(initialState);
  const subscribers = createSubscriberSet(() => cloneStatusBarState(state));

  function commit(nextState: StatusBarState): void {
    state = cloneStatusBarState(nextState);
    subscribers.publish();
  }

  return {
    subscribe: subscribers.subscribe,

    addItem: (item: StatusBarItem) => {
      const items = state.items.filter((entry) => entry.id !== item.id);
      commit({
        ...state,
        items: [...items, { ...item, visible: item.visible ?? true }],
      });
    },

    updateItem: (id: string, updates: Partial<StatusBarItem>) => {
      commit({
        ...state,
        items: state.items.map((item) => (item.id === id ? { ...item, ...updates } : item)),
      });
    },

    removeItem: (id: string) => {
      commit({
        ...state,
        items: state.items.filter((item) => item.id !== id),
      });
    },

    setCurrentUser: (user: { name: string; email: string } | null) => {
      commit({
        ...state,
        currentUser: user ? { ...user } : null,
      });
    },

    toggle: () => {
      commit({ ...state, visible: !state.visible });
    },

    reset: () => {
      commit(initialState);
    },

    get: () => cloneStatusBarState(state),
  };
}
