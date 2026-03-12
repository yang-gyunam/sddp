/**
 * Requirement Sidebar Store
 * Manages sidebar UI state for Requirements Activity
 */

import { createStore, type Store } from '@sddp/shell/core';
import type {
  RequirementSidebarState,
  RequirementFilterType,
  RequirementViewMode,
  ProjectRequirementGroup,
  RequirementSummary,
} from '../types';

// ============================================
// Initial State
// ============================================

const initialState: RequirementSidebarState = {
  searchQuery: '',
  filterType: 'all',
  viewMode: 'tree',
  expandedProjects: new Set(),
  expandedRequirements: new Set(),
  selectedRequirementId: null,
  projectGroups: [],
};

// Create the store
const requirementSidebarStore: Store<RequirementSidebarState> =
  createStore<RequirementSidebarState>(initialState);

// ============================================
// Actions
// ============================================

/**
 * Set project groups
 */
export function setProjectGroups(groups: ProjectRequirementGroup[]): void {
  requirementSidebarStore.update((state) => ({
    ...state,
    projectGroups: groups,
  }));
}

/**
 * Toggle project expansion
 */
export function toggleProjectExpanded(projectId: string): void {
  requirementSidebarStore.update((state) => {
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
 * Toggle requirement expansion (for tree view)
 */
export function toggleRequirementExpanded(requirementId: string): void {
  requirementSidebarStore.update((state) => {
    const expandedRequirements = new Set(state.expandedRequirements);
    if (expandedRequirements.has(requirementId)) {
      expandedRequirements.delete(requirementId);
    } else {
      expandedRequirements.add(requirementId);
    }
    return { ...state, expandedRequirements };
  });
}

/**
 * Set selected requirement
 */
export function setSelectedRequirement(requirementId: string | null): void {
  requirementSidebarStore.update((state) => ({
    ...state,
    selectedRequirementId: requirementId,
  }));
}

/**
 * Set search query
 */
export function setSearchQuery(query: string): void {
  requirementSidebarStore.update((state) => ({
    ...state,
    searchQuery: query,
  }));
}

/**
 * Set filter type
 */
export function setFilterType(filterType: RequirementFilterType): void {
  requirementSidebarStore.update((state) => ({
    ...state,
    filterType,
  }));
}

/**
 * Set view mode
 */
export function setViewMode(viewMode: RequirementViewMode): void {
  requirementSidebarStore.update((state) => ({
    ...state,
    viewMode,
  }));
}

/**
 * Expand all projects
 */
export function expandAllProjects(): void {
  requirementSidebarStore.update((state) => ({
    ...state,
    expandedProjects: new Set(state.projectGroups.map((g) => g.projectId)),
  }));
}

/**
 * Collapse all projects
 */
export function collapseAllProjects(): void {
  requirementSidebarStore.update((state) => ({
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
export function getRequirementSidebarState(): RequirementSidebarState {
  return requirementSidebarStore.get();
}

/**
 * Get filtered project groups
 */
export function getFilteredProjectGroups(): ProjectRequirementGroup[] {
  const state = requirementSidebarStore.get();
  const { searchQuery, filterType, projectGroups } = state;

  return projectGroups
    .map((group) => {
      let filteredRequirements = group.requirements;

      // Apply search filter
      if (searchQuery) {
        const query = searchQuery.toLowerCase();
        filteredRequirements = filteredRequirements.filter(
          (req) =>
            req.code.toLowerCase().includes(query) ||
            req.title.toLowerCase().includes(query)
        );
      }

      // Apply status/level filter
      if (filterType !== 'all') {
        filteredRequirements = filteredRequirements.filter((req) => {
          switch (filterType) {
            case 'draft':
              return req.status === 'Draft';
            case 'inReview':
              return req.status === 'InReview';
            case 'approved':
              return req.status === 'Approved';
            case 'levelA':
              return req.level === 'A';
            case 'levelB':
              return req.level === 'B';
            case 'levelC':
              return req.level === 'C';
            default:
              return true;
          }
        });
      }

      return {
        ...group,
        requirements: filteredRequirements,
        expanded: state.expandedProjects.has(group.projectId),
      };
    })
    .filter((group) => group.requirements.length > 0 || !searchQuery);
}

/**
 * Get selected requirement
 */
export function getSelectedRequirement(): RequirementSummary | null {
  const state = requirementSidebarStore.get();
  if (!state.selectedRequirementId) return null;

  for (const group of state.projectGroups) {
    const found = group.requirements.find(
      (r) => r.id === state.selectedRequirementId
    );
    if (found) return found;
  }
  return null;
}

/**
 * Get total requirements count
 */
export function getTotalRequirementsCount(): number {
  const state = requirementSidebarStore.get();
  return state.projectGroups.reduce((sum, group) => sum + group.totalCount, 0);
}

/**
 * Get counts by status
 */
export function getStatusCounts(): Record<string, number> {
  const state = requirementSidebarStore.get();
  const counts: Record<string, number> = {
    Draft: 0,
    InReview: 0,
    Approved: 0,
    Deprecated: 0,
  };

  for (const group of state.projectGroups) {
    for (const req of group.requirements) {
      counts[req.status] = (counts[req.status] || 0) + 1;
    }
  }

  return counts;
}

/**
 * Get counts by level
 */
export function getLevelCounts(): Record<string, number> {
  const state = requirementSidebarStore.get();
  const counts: Record<string, number> = { A: 0, B: 0, C: 0 };

  for (const group of state.projectGroups) {
    for (const req of group.requirements) {
      counts[req.level] = (counts[req.level] || 0) + 1;
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
export function subscribeRequirementSidebar(
  listener: (state: RequirementSidebarState, prevState: RequirementSidebarState) => void
): () => void {
  return requirementSidebarStore.subscribe(listener);
}

/**
 * Reset sidebar store
 */
export function resetRequirementSidebarStore(): void {
  requirementSidebarStore.reset();
}

// Export the store
export { requirementSidebarStore };
