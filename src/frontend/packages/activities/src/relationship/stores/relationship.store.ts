/**
 * Relationship Store - Relationship Management State Management
 * Manages relationships, graph, and diff state
 */

import { createStore, type Store } from '@sddp/shell/core';
import { clamp } from '@sddp/shell/utils';
import type {
  EntityType,
  Relationship,
  RelationshipListData,
  RelationshipGraphData,
  SpecDiffResult,
  RelationshipState,
} from '../types';

// ============================================
// Initial State
// ============================================

const initialState: RelationshipState = {
  // Relationships
  relationships: null,
  relationshipsLoading: false,
  relationshipsError: null,

  // Graph
  graph: null,
  graphLoading: false,
  graphError: null,
  graphDepth: 3,

  // Diff
  diff: null,
  diffLoading: false,
  diffError: null,

  // Context
  currentEntityType: null,
  currentEntityId: null,
};

// Create the store
const relationshipStore: Store<RelationshipState> = createStore<RelationshipState>(initialState);

// ============================================
// Context Actions
// ============================================

/**
 * Set current entity context
 */
export function setCurrentEntity(entityType: EntityType, entityId: string): void {
  relationshipStore.update((state) => ({
    ...state,
    currentEntityType: entityType,
    currentEntityId: entityId,
  }));
}

/**
 * Clear current entity context
 */
export function clearCurrentEntity(): void {
  relationshipStore.update((state) => ({
    ...state,
    currentEntityType: null,
    currentEntityId: null,
    relationships: null,
    graph: null,
    diff: null,
  }));
}

// ============================================
// Relationships Actions
// ============================================

/**
 * Set relationships
 */
export function setRelationships(relationships: RelationshipListData): void {
  relationshipStore.update((state) => ({
    ...state,
    relationships,
    relationshipsLoading: false,
    relationshipsError: null,
  }));
}

/**
 * Set relationships loading state
 */
export function setRelationshipsLoading(loading: boolean): void {
  relationshipStore.update((state) => ({
    ...state,
    relationshipsLoading: loading,
  }));
}

/**
 * Set relationships error
 */
export function setRelationshipsError(error: string | null): void {
  relationshipStore.update((state) => ({
    ...state,
    relationshipsError: error,
    relationshipsLoading: false,
  }));
}

/**
 * Add a new relationship to the list
 */
export function addRelationship(
  relationship: Relationship,
  direction: 'incoming' | 'outgoing'
): void {
  relationshipStore.update((state) => {
    if (!state.relationships) {
      return {
        ...state,
        relationships: {
          incoming: direction === 'incoming' ? [relationship] : [],
          outgoing: direction === 'outgoing' ? [relationship] : [],
          totalCount: 1,
        },
      };
    }

    return {
      ...state,
      relationships: {
        ...state.relationships,
        [direction]: [...state.relationships[direction], relationship],
        totalCount: state.relationships.totalCount + 1,
      },
    };
  });
}

/**
 * Remove a relationship from the list
 */
export function removeRelationship(relationshipId: string): void {
  relationshipStore.update((state) => {
    if (!state.relationships) return state;

    const newIncoming = state.relationships.incoming.filter((r) => r.id !== relationshipId);
    const newOutgoing = state.relationships.outgoing.filter((r) => r.id !== relationshipId);
    const removedCount =
      state.relationships.incoming.length +
      state.relationships.outgoing.length -
      newIncoming.length -
      newOutgoing.length;

    return {
      ...state,
      relationships: {
        incoming: newIncoming,
        outgoing: newOutgoing,
        totalCount: Math.max(0, state.relationships.totalCount - removedCount),
      },
    };
  });
}

/**
 * Clear relationships
 */
export function clearRelationships(): void {
  relationshipStore.update((state) => ({
    ...state,
    relationships: null,
    relationshipsLoading: false,
    relationshipsError: null,
  }));
}

// ============================================
// Graph Actions
// ============================================

/**
 * Set relationship graph
 */
export function setGraph(graph: RelationshipGraphData): void {
  relationshipStore.update((state) => ({
    ...state,
    graph,
    graphLoading: false,
    graphError: null,
  }));
}

/**
 * Set graph loading state
 */
export function setGraphLoading(loading: boolean): void {
  relationshipStore.update((state) => ({
    ...state,
    graphLoading: loading,
  }));
}

/**
 * Set graph error
 */
export function setGraphError(error: string | null): void {
  relationshipStore.update((state) => ({
    ...state,
    graphError: error,
    graphLoading: false,
  }));
}

/**
 * Set graph depth
 */
export function setGraphDepth(depth: number): void {
  relationshipStore.update((state) => ({
    ...state,
    graphDepth: clamp(depth, 1, 10),
  }));
}

/**
 * Clear graph
 */
export function clearGraph(): void {
  relationshipStore.update((state) => ({
    ...state,
    graph: null,
    graphLoading: false,
    graphError: null,
  }));
}

// ============================================
// Diff Actions
// ============================================

/**
 * Set diff result
 */
export function setDiff(diff: SpecDiffResult): void {
  relationshipStore.update((state) => ({
    ...state,
    diff,
    diffLoading: false,
    diffError: null,
  }));
}

/**
 * Set diff loading state
 */
export function setDiffLoading(loading: boolean): void {
  relationshipStore.update((state) => ({
    ...state,
    diffLoading: loading,
  }));
}

/**
 * Set diff error
 */
export function setDiffError(error: string | null): void {
  relationshipStore.update((state) => ({
    ...state,
    diffError: error,
    diffLoading: false,
  }));
}

/**
 * Clear diff
 */
export function clearDiff(): void {
  relationshipStore.update((state) => ({
    ...state,
    diff: null,
    diffLoading: false,
    diffError: null,
  }));
}

// ============================================
// Getters
// ============================================

/**
 * Get current state
 */
export function getRelationshipState(): RelationshipState {
  return relationshipStore.get();
}

/**
 * Get relationships
 */
export function getRelationships(): RelationshipListData | null {
  return relationshipStore.get().relationships;
}

/**
 * Get graph
 */
export function getGraph(): RelationshipGraphData | null {
  return relationshipStore.get().graph;
}

/**
 * Get diff
 */
export function getDiff(): SpecDiffResult | null {
  return relationshipStore.get().diff;
}

/**
 * Get current entity context
 */
export function getCurrentEntity(): { type: EntityType | null; id: string | null } {
  const state = relationshipStore.get();
  return {
    type: state.currentEntityType,
    id: state.currentEntityId,
  };
}

/**
 * Get graph depth
 */
export function getGraphDepth(): number {
  return relationshipStore.get().graphDepth;
}

/**
 * Check if relationships are loading
 */
export function isRelationshipsLoading(): boolean {
  return relationshipStore.get().relationshipsLoading;
}

/**
 * Check if graph is loading
 */
export function isGraphLoading(): boolean {
  return relationshipStore.get().graphLoading;
}

/**
 * Check if diff is loading
 */
export function isDiffLoading(): boolean {
  return relationshipStore.get().diffLoading;
}

// ============================================
// Subscribe
// ============================================

/**
 * Subscribe to relationship state changes
 */
export function subscribeRelationship(
  listener: (state: RelationshipState, prevState: RelationshipState) => void
): () => void {
  return relationshipStore.subscribe(listener);
}

/**
 * Reset relationship store
 */
export function resetRelationshipStore(): void {
  relationshipStore.reset();
}

// Export the store for direct access
export { relationshipStore };

// Re-export types for convenience
export type { RelationshipState } from '../types';
