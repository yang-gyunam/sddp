/**
 * Requirement Store - Requirement Management State Management
 * Manages requirements list, current requirement, and filters
 */

import { createStore, type Store } from '@sddp/shell/core';
import type {
  Requirement,
  RequirementDetail,
  RequirementState,
  RequirementLevel,
  RequirementStatus,
  RequirementPage,
} from '../types';

// ============================================
// Initial State
// ============================================

const initialState: RequirementState = {
  // Requirements list
  requirements: [],
  totalCount: 0,
  page: 1,
  pageSize: 20,
  totalPages: 0,
  requirementsLoading: false,
  requirementsError: null,

  // Filters
  levelFilter: null,
  statusFilter: null,

  // Current requirement
  currentRequirement: null,
  currentRequirementLoading: false,
  currentRequirementError: null,

  // Children
  children: [],
  childrenLoading: false,
  childrenError: null,
};

// Create the store
const requirementStore: Store<RequirementState> = createStore<RequirementState>(initialState);

// ============================================
// Requirements List Actions
// ============================================

/**
 * Set requirements page data
 */
export function setRequirementsPage(page: RequirementPage): void {
  requirementStore.update((state) => ({
    ...state,
    requirements: page.items,
    totalCount: page.totalCount,
    page: page.page,
    pageSize: page.pageSize,
    totalPages: page.totalPages,
    requirementsLoading: false,
    requirementsError: null,
  }));
}

/**
 * Set requirements loading state
 */
export function setRequirementsLoading(loading: boolean): void {
  requirementStore.update((state) => ({
    ...state,
    requirementsLoading: loading,
  }));
}

/**
 * Set requirements error
 */
export function setRequirementsError(error: string | null): void {
  requirementStore.update((state) => ({
    ...state,
    requirementsError: error,
    requirementsLoading: false,
  }));
}

/**
 * Add a new requirement to the list
 */
export function addRequirement(requirement: Requirement): void {
  requirementStore.update((state) => ({
    ...state,
    requirements: [requirement, ...state.requirements],
    totalCount: state.totalCount + 1,
  }));
}

/**
 * Update a requirement in the list
 */
export function updateRequirementInList(
  requirementId: string,
  updates: Partial<Requirement>
): void {
  requirementStore.update((state) => ({
    ...state,
    requirements: state.requirements.map((r) =>
      r.id === requirementId ? { ...r, ...updates } : r
    ),
  }));
}

/**
 * Remove a requirement from the list
 */
export function removeRequirement(requirementId: string): void {
  requirementStore.update((state) => ({
    ...state,
    requirements: state.requirements.filter((r) => r.id !== requirementId),
    totalCount: Math.max(0, state.totalCount - 1),
  }));
}

// ============================================
// Filters Actions
// ============================================

/**
 * Set level filter
 */
export function setLevelFilter(level: RequirementLevel | null): void {
  requirementStore.update((state) => ({
    ...state,
    levelFilter: level,
    page: 1, // Reset to first page on filter change
  }));
}

/**
 * Set status filter
 */
export function setStatusFilter(status: RequirementStatus | null): void {
  requirementStore.update((state) => ({
    ...state,
    statusFilter: status,
    page: 1, // Reset to first page on filter change
  }));
}

/**
 * Clear all filters
 */
export function clearFilters(): void {
  requirementStore.update((state) => ({
    ...state,
    levelFilter: null,
    statusFilter: null,
    page: 1,
  }));
}

/**
 * Set page number
 */
export function setPage(page: number): void {
  requirementStore.update((state) => ({
    ...state,
    page,
  }));
}

/**
 * Set page size
 */
export function setPageSize(pageSize: number): void {
  requirementStore.update((state) => ({
    ...state,
    pageSize,
    page: 1, // Reset to first page on page size change
  }));
}

// ============================================
// Current Requirement Actions
// ============================================

/**
 * Set current requirement (detail view)
 */
export function setCurrentRequirement(requirement: RequirementDetail | null): void {
  requirementStore.update((state) => ({
    ...state,
    currentRequirement: requirement,
    currentRequirementLoading: false,
    currentRequirementError: null,
    // Set children from detail response
    children: requirement?.children || [],
  }));
}

/**
 * Set current requirement loading state
 */
export function setCurrentRequirementLoading(loading: boolean): void {
  requirementStore.update((state) => ({
    ...state,
    currentRequirementLoading: loading,
  }));
}

/**
 * Set current requirement error
 */
export function setCurrentRequirementError(error: string | null): void {
  requirementStore.update((state) => ({
    ...state,
    currentRequirementError: error,
    currentRequirementLoading: false,
  }));
}

/**
 * Clear current requirement
 */
export function clearCurrentRequirement(): void {
  requirementStore.update((state) => ({
    ...state,
    currentRequirement: null,
    currentRequirementLoading: false,
    currentRequirementError: null,
    children: [],
    childrenLoading: false,
    childrenError: null,
  }));
}

/**
 * Update current requirement
 */
export function updateCurrentRequirement(updates: Partial<RequirementDetail>): void {
  requirementStore.update((state) => ({
    ...state,
    currentRequirement: state.currentRequirement
      ? { ...state.currentRequirement, ...updates }
      : null,
  }));
}

// ============================================
// Children Actions
// ============================================

/**
 * Set children requirements
 */
export function setChildren(children: Requirement[]): void {
  requirementStore.update((state) => ({
    ...state,
    children,
    childrenLoading: false,
    childrenError: null,
  }));
}

/**
 * Set children loading state
 */
export function setChildrenLoading(loading: boolean): void {
  requirementStore.update((state) => ({
    ...state,
    childrenLoading: loading,
  }));
}

/**
 * Set children error
 */
export function setChildrenError(error: string | null): void {
  requirementStore.update((state) => ({
    ...state,
    childrenError: error,
    childrenLoading: false,
  }));
}

/**
 * Add a child requirement
 */
export function addChild(child: Requirement): void {
  requirementStore.update((state) => ({
    ...state,
    children: [...state.children, child],
    // Update parent's children count
    currentRequirement: state.currentRequirement
      ? {
          ...state.currentRequirement,
          children: [...(state.currentRequirement.children || []), child],
        }
      : null,
  }));
}

/**
 * Remove a child requirement
 */
export function removeChild(childId: string): void {
  requirementStore.update((state) => ({
    ...state,
    children: state.children.filter((c) => c.id !== childId),
    currentRequirement: state.currentRequirement
      ? {
          ...state.currentRequirement,
          children: state.currentRequirement.children.filter((c) => c.id !== childId),
        }
      : null,
  }));
}

// ============================================
// Getters
// ============================================

/**
 * Get current state
 */
export function getRequirementState(): RequirementState {
  return requirementStore.get();
}

/**
 * Get requirements list
 */
export function getRequirementsList(): Requirement[] {
  return requirementStore.get().requirements;
}

/**
 * Get current requirement
 */
export function getCurrentRequirement(): RequirementDetail | null {
  return requirementStore.get().currentRequirement;
}

/**
 * Get children
 */
export function getChildren(): Requirement[] {
  return requirementStore.get().children;
}

/**
 * Get current filters
 */
export function getFilters(): {
  level: RequirementLevel | null;
  status: RequirementStatus | null;
} {
  const state = requirementStore.get();
  return {
    level: state.levelFilter,
    status: state.statusFilter,
  };
}

/**
 * Get pagination info
 */
export function getPagination(): {
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
} {
  const state = requirementStore.get();
  return {
    page: state.page,
    pageSize: state.pageSize,
    totalCount: state.totalCount,
    totalPages: state.totalPages,
  };
}

/**
 * Check if requirement is editable (Draft status only)
 */
export function isCurrentRequirementEditable(): boolean {
  const current = requirementStore.get().currentRequirement;
  return current?.status === 'Draft';
}

// ============================================
// Subscribe
// ============================================

/**
 * Subscribe to requirement state changes
 */
export function subscribeRequirement(
  listener: (state: RequirementState, prevState: RequirementState) => void
): () => void {
  return requirementStore.subscribe(listener);
}

/**
 * Reset requirement store
 */
export function resetRequirementStore(): void {
  requirementStore.reset();
}

// Export the store for direct access
export { requirementStore };

// Re-export types for convenience
export type { RequirementState } from '../types';
