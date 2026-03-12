/**
 * Menu Store - Sidebar menu tree management
 *
 * L-3 migration note:
 * This module keeps the public store contract stable while shadow-validating
 * the runes implementation against the legacy writable implementation.
 */

import { derived, writable, type Subscriber, type Unsubscriber } from 'svelte/store';
import { config } from '../core/services/ConfigurationManager';
import type { MenuNode, MenuState } from '../types';
import { createRunesMenuStore } from './menu.store.runes';

export interface MenuStoreLike {
  subscribe: (run: Subscriber<MenuState>) => Unsubscriber;
  setItems: (items: MenuNode[]) => void;
  addItem: (item: MenuNode, parentId?: string) => void;
  updateItem: (id: string, updates: Partial<MenuNode>) => void;
  removeItem: (id: string) => void;
  toggleExpanded: (id: string) => void;
  expand: (id: string) => void;
  collapse: (id: string) => void;
  expandAll: () => void;
  collapseAll: () => void;
  select: (id: string | null) => void;
  setLoading: (loading: boolean) => void;
  setError: (error: string | null) => void;
  reset: () => void;
  get: () => MenuState;
}

type MenuMismatchContext = {
  action: string;
  primaryState: MenuState;
  shadowState: MenuState;
};

const initialState: MenuState = {
  items: [],
  expandedIds: new Set<string>(),
  selectedId: null,
  loading: false,
  error: null,
};

function cloneMenuNode(node: MenuNode): MenuNode {
  return {
    ...node,
    tabConfig: node.tabConfig
      ? {
        ...node.tabConfig,
        createTab: node.tabConfig.createTab,
      }
      : undefined,
    children: node.children.map(cloneMenuNode),
  };
}

function cloneMenuState(state: MenuState): MenuState {
  return {
    ...state,
    items: state.items.map(cloneMenuNode),
    expandedIds: new Set(state.expandedIds),
  };
}

function normalizeMenuNode(node: MenuNode): unknown {
  return {
    id: node.id,
    name: node.name,
    url: node.url ?? null,
    icon: node.icon ?? null,
    order: node.order,
    type: node.type,
    parentId: node.parentId ?? null,
    permissions: [...node.permissions],
    isActive: node.isActive ?? null,
    expanded: node.expanded ?? null,
    hasTabConfig: !!node.tabConfig,
    children: node.children.map(normalizeMenuNode),
  };
}

function normalizeMenuState(state: MenuState): unknown {
  return {
    items: state.items.map(normalizeMenuNode),
    expandedIds: [...state.expandedIds].sort(),
    selectedId: state.selectedId,
    loading: state.loading,
    error: state.error,
  };
}

function areEqual(left: unknown, right: unknown): boolean {
  return JSON.stringify(left) === JSON.stringify(right);
}

function addToParent(nodes: MenuNode[], item: MenuNode, parentId: string): MenuNode[] {
  return nodes.map((node) => {
    if (node.id === parentId) {
      return {
        ...node,
        children: [...node.children, item],
      };
    }

    if (node.children.length > 0) {
      return {
        ...node,
        children: addToParent(node.children, item, parentId),
      };
    }

    return node;
  });
}

function updateNode(nodes: MenuNode[], id: string, updates: Partial<MenuNode>): MenuNode[] {
  return nodes.map((node) => {
    if (node.id === id) {
      return { ...node, ...updates };
    }

    if (node.children.length > 0) {
      return {
        ...node,
        children: updateNode(node.children, id, updates),
      };
    }

    return node;
  });
}

function removeNode(nodes: MenuNode[], id: string): MenuNode[] {
  return nodes
    .filter((node) => node.id !== id)
    .map((node) => ({
      ...node,
      children: removeNode(node.children, id),
    }));
}

function getAllIds(nodes: MenuNode[]): string[] {
  return nodes.flatMap((node) => [node.id, ...getAllIds(node.children)]);
}

function createWritableMenuStore(): MenuStoreLike {
  const { subscribe, set, update } = writable<MenuState>(cloneMenuState(initialState));

  let currentState = cloneMenuState(initialState);
  subscribe((state) => {
    currentState = cloneMenuState(state);
  });

  return {
    subscribe,
    setItems: (items) => {
      update((state) => ({
        ...state,
        items,
        loading: false,
        error: null,
      }));
    },
    addItem: (item, parentId) => {
      update((state) => {
        if (!parentId) {
          return { ...state, items: [...state.items, item] };
        }

        return { ...state, items: addToParent(state.items, item, parentId) };
      });
    },
    updateItem: (id, updates) => {
      update((state) => ({
        ...state,
        items: updateNode(state.items, id, updates),
      }));
    },
    removeItem: (id) => {
      update((state) => ({
        ...state,
        items: removeNode(state.items, id),
      }));
    },
    toggleExpanded: (id) => {
      update((state) => {
        const expandedIds = new Set(state.expandedIds);
        if (expandedIds.has(id)) {
          expandedIds.delete(id);
        } else {
          expandedIds.add(id);
        }
        return { ...state, expandedIds };
      });
    },
    expand: (id) => {
      update((state) => {
        const expandedIds = new Set(state.expandedIds);
        expandedIds.add(id);
        return { ...state, expandedIds };
      });
    },
    collapse: (id) => {
      update((state) => {
        const expandedIds = new Set(state.expandedIds);
        expandedIds.delete(id);
        return { ...state, expandedIds };
      });
    },
    expandAll: () => {
      update((state) => ({
        ...state,
        expandedIds: new Set(getAllIds(state.items)),
      }));
    },
    collapseAll: () => {
      update((state) => ({
        ...state,
        expandedIds: new Set<string>(),
      }));
    },
    select: (id) => {
      update((state) => ({ ...state, selectedId: id }));
    },
    setLoading: (loading) => {
      update((state) => ({ ...state, loading }));
    },
    setError: (error) => {
      update((state) => ({ ...state, error, loading: false }));
    },
    reset: () => {
      set(cloneMenuState(initialState));
    },
    get: () => cloneMenuState(currentState),
  };
}

function createParityAwareMenuStore(
  primary: MenuStoreLike,
  shadow: MenuStoreLike,
  reportMismatch: (context: MenuMismatchContext) => void
): MenuStoreLike {
  const listeners = new Set<Subscriber<MenuState>>();
  let currentState = primary.get();

  primary.subscribe((state) => {
    currentState = cloneMenuState(state);
    for (const listener of listeners) {
      listener(cloneMenuState(state));
    }
  });

  function subscribe(run: Subscriber<MenuState>): Unsubscriber {
    run(cloneMenuState(currentState));
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

    if (!areEqual(normalizeMenuState(primaryState), normalizeMenuState(shadowState))) {
      reportMismatch({ action, primaryState, shadowState });
    }
  }

  return {
    subscribe,
    setItems: (items) => execute('setItems', () => primary.setItems(items), () => shadow.setItems(items)),
    addItem: (item, parentId) => execute('addItem', () => primary.addItem(item, parentId), () => shadow.addItem(item, parentId)),
    updateItem: (id, updates) => execute('updateItem', () => primary.updateItem(id, updates), () => shadow.updateItem(id, updates)),
    removeItem: (id) => execute('removeItem', () => primary.removeItem(id), () => shadow.removeItem(id)),
    toggleExpanded: (id) => execute('toggleExpanded', () => primary.toggleExpanded(id), () => shadow.toggleExpanded(id)),
    expand: (id) => execute('expand', () => primary.expand(id), () => shadow.expand(id)),
    collapse: (id) => execute('collapse', () => primary.collapse(id), () => shadow.collapse(id)),
    expandAll: () => execute('expandAll', primary.expandAll, shadow.expandAll),
    collapseAll: () => execute('collapseAll', primary.collapseAll, shadow.collapseAll),
    select: (id) => execute('select', () => primary.select(id), () => shadow.select(id)),
    setLoading: (loading) => execute('setLoading', () => primary.setLoading(loading), () => shadow.setLoading(loading)),
    setError: (error) => execute('setError', () => primary.setError(error), () => shadow.setError(error)),
    reset: () => execute('reset', primary.reset, shadow.reset),
    get: () => cloneMenuState(currentState),
  };
}

function createStore(): MenuStoreLike {
  const enableRunesMenuStore = config.isRunesMenuStoreEnabled();
  const enableShadowParity = config.isRunesStoreShadowParityEnabled();

  const writableStore = createWritableMenuStore();
  const runesStore = createRunesMenuStore(initialState);

  const primaryStore = enableRunesMenuStore ? runesStore : writableStore;
  const shadowStore = enableRunesMenuStore ? writableStore : runesStore;

  if (!enableShadowParity) {
    return primaryStore;
  }

  return createParityAwareMenuStore(primaryStore, shadowStore, ({ action, primaryState, shadowState }) => {
    console.error('[menu.store parity mismatch]', {
      action,
      primaryState: normalizeMenuState(primaryState),
      shadowState: normalizeMenuState(shadowState),
    });
  });
}

export const __internal = {
  cloneMenuNode,
  cloneMenuState,
  normalizeMenuState,
  createWritableMenuStore,
  createParityAwareMenuStore,
  createRunesMenuStore,
};

export const menuStore = createStore();

export const flatMenuItems = derived(menuStore, ($state) => {
  const flatten = (nodes: MenuNode[]): MenuNode[] =>
    nodes.flatMap((node) => [node, ...flatten(node.children)]);
  return flatten($state.items);
});

export const selectedMenuItem = derived(menuStore, ($state) => {
  const findNode = (nodes: MenuNode[], id: string): MenuNode | null => {
    for (const node of nodes) {
      if (node.id === id) {
        return node;
      }

      const found = findNode(node.children, id);
      if (found) {
        return found;
      }
    }

    return null;
  };

  return $state.selectedId ? findNode($state.items, $state.selectedId) : null;
});

export const isExpanded = (id: string) =>
  derived(menuStore, ($state) => $state.expandedIds.has(id));

export const menuLoading = derived(menuStore, ($state) => $state.loading);

export const menuError = derived(menuStore, ($state) => $state.error);
