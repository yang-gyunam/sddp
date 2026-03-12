/**
 * Force Graph Store - Interactive Graph UI State Management
 * Manages config, filters, and selection state for ForceGraph component
 */

import { createStore, type Store } from '@sddp/shell/core';
import type {
  ForceGraphConfig,
  GraphFilterState,
  EntityType,
  RelationType,
} from '../types';
import {
  DEFAULT_FORCE_GRAPH_CONFIG,
  DEFAULT_GRAPH_FILTERS,
} from '../types';

// ============================================
// State
// ============================================

export interface ForceGraphState {
  config: ForceGraphConfig;
  filters: GraphFilterState;
  selectedNodeId: string | null;
  hoveredNodeId: string | null;
}

const initialState: ForceGraphState = {
  config: { ...DEFAULT_FORCE_GRAPH_CONFIG },
  filters: {
    entityTypes: new Set(DEFAULT_GRAPH_FILTERS.entityTypes),
    relationTypes: new Set(DEFAULT_GRAPH_FILTERS.relationTypes),
  },
  selectedNodeId: null,
  hoveredNodeId: null,
};

// ============================================
// Store Instance
// ============================================

let store: Store<ForceGraphState> | null = null;

function getStore(): Store<ForceGraphState> {
  if (!store) {
    store = createStore<ForceGraphState>(initialState);
  }
  return store;
}

// ============================================
// Actions - Config
// ============================================

export function setForceGraphConfig(config: Partial<ForceGraphConfig>): void {
  const s = getStore();
  const current = s.get().config;
  s.update((state) => ({ ...state, config: { ...current, ...config } }));
}

export function resetForceGraphConfig(): void {
  getStore().update((state) => ({ ...state, config: { ...DEFAULT_FORCE_GRAPH_CONFIG } }));
}

// ============================================
// Actions - Filters
// ============================================

export function toggleEntityTypeFilter(entityType: EntityType): void {
  getStore().update((state) => {
    const next = new Set(state.filters.entityTypes);
    if (next.has(entityType)) {
      next.delete(entityType);
    } else {
      next.add(entityType);
    }
    return { ...state, filters: { ...state.filters, entityTypes: next } };
  });
}

export function toggleRelationTypeFilter(relationType: RelationType): void {
  getStore().update((state) => {
    const next = new Set(state.filters.relationTypes);
    if (next.has(relationType)) {
      next.delete(relationType);
    } else {
      next.add(relationType);
    }
    return { ...state, filters: { ...state.filters, relationTypes: next } };
  });
}

export function resetFilters(): void {
  getStore().update((state) => ({
    ...state,
    filters: {
      entityTypes: new Set(DEFAULT_GRAPH_FILTERS.entityTypes),
      relationTypes: new Set(DEFAULT_GRAPH_FILTERS.relationTypes),
    },
  }));
}

// ============================================
// Actions - Selection
// ============================================

export function selectForceGraphNode(nodeId: string | null): void {
  getStore().update((state) => ({ ...state, selectedNodeId: nodeId }));
}

export function setForceGraphHoveredNode(nodeId: string | null): void {
  getStore().update((state) => ({ ...state, hoveredNodeId: nodeId }));
}

// ============================================
// Getters
// ============================================

export function getForceGraphState(): ForceGraphState {
  return getStore().get();
}

export function getForceGraphConfig(): ForceGraphConfig {
  return getStore().get().config;
}

export function getForceGraphFilters(): GraphFilterState {
  return getStore().get().filters;
}

export function getForceGraphSelectedNodeId(): string | null {
  return getStore().get().selectedNodeId;
}

// ============================================
// Subscribe & Reset
// ============================================

export function subscribeForceGraph(listener: (state: ForceGraphState) => void): () => void {
  return getStore().subscribe(listener);
}

export function resetForceGraphStore(): void {
  getStore().reset();
}
