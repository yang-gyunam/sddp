/**
 * Trace Graph Store - UI State Management for Trace Graph Section
 * Manages trace graph configuration, selection, and primary flow data
 */

import { createStore, type Store } from '@sddp/shell/core';
import { clamp } from '@sddp/shell/utils';
import type {
  TraceGraphState,
  TraceGraphConfig,
  PrimaryFlowNode,
} from '../types';
import {
  INITIAL_TRACE_GRAPH_STATE,
  DEFAULT_TRACE_GRAPH_CONFIG,
} from '../types';

// Create the store
const traceGraphStore: Store<TraceGraphState> = createStore<TraceGraphState>(
  INITIAL_TRACE_GRAPH_STATE
);

// ============================================
// Configuration Actions
// ============================================

/**
 * Update trace graph configuration
 */
export function setTraceGraphConfig(config: Partial<TraceGraphConfig>): void {
  traceGraphStore.update((state) => ({
    ...state,
    config: {
      ...state.config,
      ...config,
    },
  }));
}

/**
 * Toggle primary flow visibility
 */
export function togglePrimaryFlow(): void {
  traceGraphStore.update((state) => ({
    ...state,
    config: {
      ...state.config,
      showPrimaryFlow: !state.config.showPrimaryFlow,
    },
  }));
}

/**
 * Toggle relationships visibility
 */
export function toggleRelationships(): void {
  traceGraphStore.update((state) => ({
    ...state,
    config: {
      ...state.config,
      showRelationships: !state.config.showRelationships,
    },
  }));
}

/**
 * Toggle deprecated visibility
 */
export function toggleDeprecated(): void {
  traceGraphStore.update((state) => ({
    ...state,
    config: {
      ...state.config,
      showDeprecated: !state.config.showDeprecated,
    },
  }));
}

/**
 * Set max depth
 */
export function setMaxDepth(depth: number): void {
  traceGraphStore.update((state) => ({
    ...state,
    config: {
      ...state.config,
      maxDepth: clamp(depth, 1, 10),
    },
  }));
}

/**
 * Toggle section expansion
 */
export function toggleSection(sectionId: string): void {
  traceGraphStore.update((state) => {
    const expandedSections = new Set(state.config.expandedSections);
    if (expandedSections.has(sectionId)) {
      expandedSections.delete(sectionId);
    } else {
      expandedSections.add(sectionId);
    }
    return {
      ...state,
      config: {
        ...state.config,
        expandedSections,
      },
    };
  });
}

/**
 * Reset configuration to defaults
 */
export function resetTraceGraphConfig(): void {
  traceGraphStore.update((state) => ({
    ...state,
    config: { ...DEFAULT_TRACE_GRAPH_CONFIG },
  }));
}

// ============================================
// Selection Actions
// ============================================

/**
 * Select a node
 */
export function selectNode(nodeId: string | null): void {
  traceGraphStore.update((state) => ({
    ...state,
    selectedNodeId: nodeId,
  }));
}

/**
 * Set hovered node
 */
export function setHoveredNode(nodeId: string | null): void {
  traceGraphStore.update((state) => ({
    ...state,
    hoveredNodeId: nodeId,
  }));
}

/**
 * Clear selection
 */
export function clearSelection(): void {
  traceGraphStore.update((state) => ({
    ...state,
    selectedNodeId: null,
    hoveredNodeId: null,
  }));
}

// ============================================
// Primary Flow Actions
// ============================================

/**
 * Set primary flow data
 */
export function setPrimaryFlow(nodes: PrimaryFlowNode[]): void {
  traceGraphStore.update((state) => ({
    ...state,
    primaryFlow: nodes,
    loading: false,
    error: null,
  }));
}

/**
 * Set loading state
 */
export function setTraceGraphLoading(loading: boolean): void {
  traceGraphStore.update((state) => ({
    ...state,
    loading,
  }));
}

/**
 * Set error
 */
export function setTraceGraphError(error: string | null): void {
  traceGraphStore.update((state) => ({
    ...state,
    error,
    loading: false,
  }));
}

/**
 * Clear primary flow data
 */
export function clearPrimaryFlow(): void {
  traceGraphStore.update((state) => ({
    ...state,
    primaryFlow: [],
    loading: false,
    error: null,
  }));
}

// ============================================
// Getters
// ============================================

/**
 * Get current state
 */
export function getTraceGraphState(): TraceGraphState {
  return traceGraphStore.get();
}

/**
 * Get configuration
 */
export function getTraceGraphConfig(): TraceGraphConfig {
  return traceGraphStore.get().config;
}

/**
 * Get selected node ID
 */
export function getSelectedNodeId(): string | null {
  return traceGraphStore.get().selectedNodeId;
}

/**
 * Get hovered node ID
 */
export function getHoveredNodeId(): string | null {
  return traceGraphStore.get().hoveredNodeId;
}

/**
 * Get primary flow nodes from store
 */
export function getPrimaryFlowNodes(): PrimaryFlowNode[] {
  return traceGraphStore.get().primaryFlow;
}

/**
 * Check if section is expanded
 */
export function isSectionExpanded(sectionId: string): boolean {
  return traceGraphStore.get().config.expandedSections.has(sectionId);
}

/**
 * Check if loading
 */
export function isTraceGraphLoading(): boolean {
  return traceGraphStore.get().loading;
}

// ============================================
// Subscribe
// ============================================

/**
 * Subscribe to trace graph state changes
 */
export function subscribeTraceGraph(
  listener: (state: TraceGraphState, prevState: TraceGraphState) => void
): () => void {
  return traceGraphStore.subscribe(listener);
}

/**
 * Reset trace graph store
 */
export function resetTraceGraphStore(): void {
  traceGraphStore.reset();
}

// Export the store for direct access
export { traceGraphStore };

// Re-export types
export type { TraceGraphState, TraceGraphConfig } from '../types';
