/**
 * Artifact Store
 * Manages artifact sidebar UI state
 */

import { createStore, type Store } from '@sddp/shell/core';
import type {
  ArtifactSidebarState,
  ArtifactTypeGroup,
  ArtifactSummary,
  ArtifactFilterType,
  ArtifactType,
} from '../types';

// ============================================
// Initial State
// ============================================

const initialState: ArtifactSidebarState = {
  searchQuery: '',
  filterType: 'all',
  expandedTypes: new Set<ArtifactType>(),
  selectedArtifactId: null,
  typeGroups: [],
};

// Create the store
const artifactStore: Store<ArtifactSidebarState> =
  createStore<ArtifactSidebarState>(initialState);

// ============================================
// Actions
// ============================================

/**
 * Set type groups
 */
export function setTypeGroups(groups: ArtifactTypeGroup[]): void {
  artifactStore.update((state) => ({
    ...state,
    typeGroups: groups,
  }));
}

/**
 * Remove an artifact from all groups
 */
export function removeArtifactFromGroups(artifactId: string): void {
  artifactStore.update((state) => {
    const nextGroups = state.typeGroups
      .map((group) => {
        const filteredArtifacts = group.artifacts.filter((artifact) => artifact.id !== artifactId);
        if (filteredArtifacts.length === group.artifacts.length) {
          return group;
        }

        return {
          ...group,
          artifacts: filteredArtifacts,
          totalCount: filteredArtifacts.length,
        };
      })
      .filter((group) => group.artifacts.length > 0);

    const remainingTypes = new Set(nextGroups.map((group) => group.type));
    const nextExpandedTypes = new Set(
      [...state.expandedTypes].filter((type) => remainingTypes.has(type))
    );

    return {
      ...state,
      typeGroups: nextGroups,
      expandedTypes: nextExpandedTypes,
      selectedArtifactId: state.selectedArtifactId === artifactId ? null : state.selectedArtifactId,
    };
  });
}

/**
 * Toggle type expansion
 */
export function toggleTypeExpanded(type: ArtifactType): void {
  artifactStore.update((state) => {
    const newExpanded = new Set(state.expandedTypes);
    if (newExpanded.has(type)) {
      newExpanded.delete(type);
    } else {
      newExpanded.add(type);
    }
    return {
      ...state,
      expandedTypes: newExpanded,
    };
  });
}

/**
 * Set selected artifact
 */
export function setSelectedArtifact(artifactId: string | null): void {
  artifactStore.update((state) => ({
    ...state,
    selectedArtifactId: artifactId,
  }));
}

/**
 * Set search query
 */
export function setArtifactSearchQuery(query: string): void {
  artifactStore.update((state) => ({
    ...state,
    searchQuery: query,
  }));
}

/**
 * Set filter type
 */
export function setArtifactFilterType(filterType: ArtifactFilterType): void {
  artifactStore.update((state) => ({
    ...state,
    filterType,
  }));
}

/**
 * Clear all filters
 */
export function clearArtifactFilters(): void {
  artifactStore.update((state) => ({
    ...state,
    searchQuery: '',
    filterType: 'all',
  }));
}

/**
 * Expand all types
 */
export function expandAllTypes(): void {
  artifactStore.update((state) => {
    const allTypes = state.typeGroups.map((g) => g.type);
    return {
      ...state,
      expandedTypes: new Set<ArtifactType>(allTypes),
    };
  });
}

/**
 * Collapse all types
 */
export function collapseAllTypes(): void {
  artifactStore.update((state) => ({
    ...state,
    expandedTypes: new Set<ArtifactType>(),
  }));
}

/**
 * Reset store to initial state
 */
export function resetArtifactStore(): void {
  artifactStore.set(initialState);
}

// ============================================
// Selectors / Getters
// ============================================

/**
 * Get current state
 */
export function getArtifactState(): ArtifactSidebarState {
  return artifactStore.get();
}

/**
 * Subscribe to store changes
 */
export function subscribeArtifact(
  callback: (state: ArtifactSidebarState) => void
): () => void {
  return artifactStore.subscribe(callback);
}

/**
 * Get filtered type groups
 */
export function getFilteredTypeGroups(): ArtifactTypeGroup[] {
  const state = artifactStore.get();
  const { typeGroups, searchQuery, filterType } = state;

  return typeGroups
    .map((group) => {
      let filteredArtifacts = group.artifacts;

      // Apply status filter
      if (filterType !== 'all') {
        const statusMap: Record<ArtifactFilterType, string> = {
          all: '',
          valid: 'Valid',
          modified: 'Modified',
          missing: 'Missing',
        };
        const targetStatus = statusMap[filterType];
        filteredArtifacts = filteredArtifacts.filter((a) => a.status === targetStatus);
      }

      // Apply search filter
      if (searchQuery.trim()) {
        const query = searchQuery.toLowerCase();
        filteredArtifacts = filteredArtifacts.filter(
          (a) =>
            a.entityName.toLowerCase().includes(query) ||
            a.artifactPath.toLowerCase().includes(query) ||
            (a.specCode && a.specCode.toLowerCase().includes(query))
        );
      }

      return {
        ...group,
        artifacts: filteredArtifacts,
        expanded: state.expandedTypes.has(group.type),
      };
    })
    .filter((group) => group.artifacts.length > 0);
}

/**
 * Get selected artifact
 */
export function getSelectedArtifact(): ArtifactSummary | null {
  const state = artifactStore.get();
  if (!state.selectedArtifactId) return null;

  for (const group of state.typeGroups) {
    const artifact = group.artifacts.find((a) => a.id === state.selectedArtifactId);
    if (artifact) return artifact;
  }
  return null;
}

/**
 * Get status counts
 */
export function getArtifactStatusCounts(): Record<ArtifactFilterType, number> {
  const state = artifactStore.get();
  const counts: Record<ArtifactFilterType, number> = {
    all: 0,
    valid: 0,
    modified: 0,
    missing: 0,
  };

  for (const group of state.typeGroups) {
    for (const artifact of group.artifacts) {
      counts.all++;
      if (artifact.status === 'Valid') counts.valid++;
      if (artifact.status === 'Modified') counts.modified++;
      if (artifact.status === 'Missing') counts.missing++;
    }
  }

  return counts;
}

/**
 * Get total artifacts count
 */
export function getTotalArtifactsCount(): number {
  const state = artifactStore.get();
  return state.typeGroups.reduce((sum, g) => sum + g.artifacts.length, 0);
}

// Export store
export { artifactStore };
