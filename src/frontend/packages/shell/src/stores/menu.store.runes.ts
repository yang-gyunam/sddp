import type { Subscriber, Unsubscriber } from 'svelte/store';
import type { MenuNode, MenuState } from '../types';
import type { MenuStoreLike } from './menu.store';

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

export function createRunesMenuStore(initialState: MenuState): MenuStoreLike {
  let state = cloneMenuState(initialState);
  const subscribers = new Set<Subscriber<MenuState>>();

  function snapshot(): MenuState {
    return cloneMenuState(state);
  }

  function commit(nextState: MenuState): void {
    state = cloneMenuState(nextState);
    const latest = snapshot();
    for (const subscriber of subscribers) {
      subscriber(latest);
    }
  }

  return {
    subscribe: (run: Subscriber<MenuState>): Unsubscriber => {
      run(snapshot());
      subscribers.add(run);
      return () => {
        subscribers.delete(run);
      };
    },
    setItems: (items) => {
      commit({
        ...state,
        items,
        loading: false,
        error: null,
      });
    },
    addItem: (item, parentId) => {
      if (!parentId) {
        commit({ ...state, items: [...state.items, item] });
        return;
      }

      commit({ ...state, items: addToParent(state.items, item, parentId) });
    },
    updateItem: (id, updates) => {
      commit({
        ...state,
        items: updateNode(state.items, id, updates),
      });
    },
    removeItem: (id) => {
      commit({
        ...state,
        items: removeNode(state.items, id),
      });
    },
    toggleExpanded: (id) => {
      const expandedIds = new Set(state.expandedIds);
      if (expandedIds.has(id)) {
        expandedIds.delete(id);
      } else {
        expandedIds.add(id);
      }

      commit({ ...state, expandedIds });
    },
    expand: (id) => {
      const expandedIds = new Set(state.expandedIds);
      expandedIds.add(id);
      commit({ ...state, expandedIds });
    },
    collapse: (id) => {
      const expandedIds = new Set(state.expandedIds);
      expandedIds.delete(id);
      commit({ ...state, expandedIds });
    },
    expandAll: () => {
      commit({
        ...state,
        expandedIds: new Set(getAllIds(state.items)),
      });
    },
    collapseAll: () => {
      commit({
        ...state,
        expandedIds: new Set<string>(),
      });
    },
    select: (id) => {
      commit({ ...state, selectedId: id });
    },
    setLoading: (loading) => {
      commit({ ...state, loading });
    },
    setError: (error) => {
      commit({ ...state, error, loading: false });
    },
    reset: () => {
      commit(initialState);
    },
    get: () => snapshot(),
  };
}
