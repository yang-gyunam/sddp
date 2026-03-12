/**
 * Spec Sidebar Store
 * Manages sidebar UI state for Specs Activity
 */

import { createStore, type Store } from '@sddp/shell/core';
import type {
  SpecSidebarState,
  SpecFilterType,
  ProjectSpecGroup,
  SpecSummary,
} from '../types';

// ============================================
// Initial State
// ============================================

const initialState: SpecSidebarState = {
  searchQuery: '',
  filterType: 'all',
  expandedProjects: new Set(),
  selectedSpecId: null,
  projectGroups: [],
};

// Create the store
const specSidebarStore: Store<SpecSidebarState> =
  createStore<SpecSidebarState>(initialState);

// ============================================
// Actions
// ============================================

/**
 * Set project groups
 */
export function setProjectGroups(groups: ProjectSpecGroup[]): void {
  specSidebarStore.update((state) => ({
    ...state,
    projectGroups: groups,
  }));
}

/**
 * Toggle project expansion
 */
export function toggleProjectExpanded(projectId: string): void {
  specSidebarStore.update((state) => {
    const expandedProjects = new Set(state.expandedProjects);
    if (expandedProjects.has(projectId)) {
      expandedProjects.delete(projectId);
    } else {
      expandedProjects.add(projectId);
    }
    return { ...state, expandedProjects };
  });
}

/**
 * Set selected spec
 */
export function setSelectedSpec(specId: string | null): void {
  specSidebarStore.update((state) => ({
    ...state,
    selectedSpecId: specId,
  }));
}

/**
 * Set search query
 */
export function setSearchQuery(query: string): void {
  specSidebarStore.update((state) => ({
    ...state,
    searchQuery: query,
  }));
}

/**
 * Set filter type
 */
export function setFilterType(filterType: SpecFilterType): void {
  specSidebarStore.update((state) => ({
    ...state,
    filterType,
  }));
}

/**
 * Expand all projects
 */
export function expandAllProjects(): void {
  specSidebarStore.update((state) => ({
    ...state,
    expandedProjects: new Set(state.projectGroups.map((g) => g.projectId)),
  }));
}

/**
 * Collapse all projects
 */
export function collapseAllProjects(): void {
  specSidebarStore.update((state) => ({
    ...state,
    expandedProjects: new Set(),
  }));
}

// ============================================
// Getters
// ============================================

/**
 * Get current state
 */
export function getSpecSidebarState(): SpecSidebarState {
  return specSidebarStore.get();
}

/**
 * Get filtered project groups
 */
export function getFilteredProjectGroups(): ProjectSpecGroup[] {
  const state = specSidebarStore.get();
  const { searchQuery, filterType, projectGroups } = state;

  return projectGroups
    .map((group) => {
      let filteredSpecs = group.specs;

      // Apply search filter
      if (searchQuery) {
        const query = searchQuery.toLowerCase();
        filteredSpecs = filteredSpecs.filter(
          (spec) =>
            spec.code.toLowerCase().includes(query) ||
            spec.title.toLowerCase().includes(query)
        );
      }

      // Apply status filter
      if (filterType !== 'all') {
        filteredSpecs = filteredSpecs.filter((spec) => {
          switch (filterType) {
            case 'draft':
              return spec.status === 'Draft';
            case 'inReview':
              return spec.status === 'InReview';
            case 'approved':
              return spec.status === 'Approved';
            case 'locked':
              return spec.status === 'Locked';
            default:
              return true;
          }
        });
      }

      return {
        ...group,
        specs: filteredSpecs,
        expanded: state.expandedProjects.has(group.projectId),
      };
    })
    .filter((group) => group.specs.length > 0 || !searchQuery);
}

/**
 * Get selected spec
 */
export function getSelectedSpec(): SpecSummary | null {
  const state = specSidebarStore.get();
  if (!state.selectedSpecId) return null;

  for (const group of state.projectGroups) {
    const found = group.specs.find((s) => s.id === state.selectedSpecId);
    if (found) return found;
  }
  return null;
}

/**
 * Get total specs count
 */
export function getTotalSpecsCount(): number {
  const state = specSidebarStore.get();
  return state.projectGroups.reduce((sum, group) => sum + group.totalCount, 0);
}

/**
 * Get counts by status
 */
export function getStatusCounts(): Record<string, number> {
  const state = specSidebarStore.get();
  const counts: Record<string, number> = {
    Draft: 0,
    InReview: 0,
    Approved: 0,
    Locked: 0,
  };

  for (const group of state.projectGroups) {
    for (const spec of group.specs) {
      counts[spec.status] = (counts[spec.status] || 0) + 1;
    }
  }

  return counts;
}

// ============================================
// Subscribe
// ============================================

/**
 * Subscribe to sidebar state changes
 */
export function subscribeSpecSidebar(
  listener: (state: SpecSidebarState, prevState: SpecSidebarState) => void
): () => void {
  return specSidebarStore.subscribe(listener);
}

/**
 * Reset sidebar store
 */
export function resetSpecSidebarStore(): void {
  specSidebarStore.reset();
}

// Export the store
export { specSidebarStore };
