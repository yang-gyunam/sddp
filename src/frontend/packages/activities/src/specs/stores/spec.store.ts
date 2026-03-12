/**
 * Spec Store - Spec Management State Management
 * Manages specs list, current spec, filters, and version history
 */

import { createStore, type Store } from '@sddp/shell/core';
import type { Spec, SpecDetail, SpecState, SpecStatus, SpecPage } from '../types';

// ============================================
// Initial State
// ============================================

const initialState: SpecState = {
  // Specs list
  specs: [],
  totalCount: 0,
  page: 1,
  pageSize: 20,
  totalPages: 0,
  specsLoading: false,
  specsError: null,

  // Filters
  statusFilter: null,
  codeSearch: null,

  // Current spec
  currentSpec: null,
  currentSpecLoading: false,
  currentSpecError: null,

  // Version history
  specVersions: [],
  versionsLoading: false,
  versionsError: null,
};

// Create the store
const specStore: Store<SpecState> = createStore<SpecState>(initialState);

// ============================================
// Specs List Actions
// ============================================

/**
 * Set specs page data
 */
export function setSpecsPage(page: SpecPage): void {
  specStore.update((state) => ({
    ...state,
    specs: page.items,
    totalCount: page.totalCount,
    page: page.page,
    pageSize: page.pageSize,
    totalPages: page.totalPages,
    specsLoading: false,
    specsError: null,
  }));
}

/**
 * Set specs loading state
 */
export function setSpecsLoading(loading: boolean): void {
  specStore.update((state) => ({
    ...state,
    specsLoading: loading,
  }));
}

/**
 * Set specs error
 */
export function setSpecsError(error: string | null): void {
  specStore.update((state) => ({
    ...state,
    specsError: error,
    specsLoading: false,
  }));
}

/**
 * Add a new spec to the list
 */
export function addSpec(spec: Spec): void {
  specStore.update((state) => ({
    ...state,
    specs: [spec, ...state.specs],
    totalCount: state.totalCount + 1,
  }));
}

/**
 * Update a spec in the list
 */
export function updateSpecInList(specId: string, updates: Partial<Spec>): void {
  specStore.update((state) => ({
    ...state,
    specs: state.specs.map((s) => (s.id === specId ? { ...s, ...updates } : s)),
  }));
}

/**
 * Remove a spec from the list
 */
export function removeSpec(specId: string): void {
  specStore.update((state) => ({
    ...state,
    specs: state.specs.filter((s) => s.id !== specId),
    totalCount: Math.max(0, state.totalCount - 1),
  }));
}

// ============================================
// Filters Actions
// ============================================

/**
 * Set status filter
 */
export function setStatusFilter(status: SpecStatus | null): void {
  specStore.update((state) => ({
    ...state,
    statusFilter: status,
    page: 1, // Reset to first page on filter change
  }));
}

/**
 * Set code search filter
 */
export function setCodeSearch(code: string | null): void {
  specStore.update((state) => ({
    ...state,
    codeSearch: code,
    page: 1, // Reset to first page on search change
  }));
}

/**
 * Clear all filters
 */
export function clearFilters(): void {
  specStore.update((state) => ({
    ...state,
    statusFilter: null,
    codeSearch: null,
    page: 1,
  }));
}

/**
 * Set page number
 */
export function setPage(page: number): void {
  specStore.update((state) => ({
    ...state,
    page,
  }));
}

/**
 * Set page size
 */
export function setPageSize(pageSize: number): void {
  specStore.update((state) => ({
    ...state,
    pageSize,
    page: 1, // Reset to first page on page size change
  }));
}

// ============================================
// Current Spec Actions
// ============================================

/**
 * Set current spec (detail view)
 */
export function setCurrentSpec(spec: SpecDetail | null): void {
  specStore.update((state) => ({
    ...state,
    currentSpec: spec,
    currentSpecLoading: false,
    currentSpecError: null,
  }));
}

/**
 * Set current spec loading state
 */
export function setCurrentSpecLoading(loading: boolean): void {
  specStore.update((state) => ({
    ...state,
    currentSpecLoading: loading,
  }));
}

/**
 * Set current spec error
 */
export function setCurrentSpecError(error: string | null): void {
  specStore.update((state) => ({
    ...state,
    currentSpecError: error,
    currentSpecLoading: false,
  }));
}

/**
 * Clear current spec
 */
export function clearCurrentSpec(): void {
  specStore.update((state) => ({
    ...state,
    currentSpec: null,
    currentSpecLoading: false,
    currentSpecError: null,
    specVersions: [],
    versionsLoading: false,
    versionsError: null,
  }));
}

/**
 * Update current spec
 */
export function updateCurrentSpec(updates: Partial<SpecDetail>): void {
  specStore.update((state) => ({
    ...state,
    currentSpec: state.currentSpec ? { ...state.currentSpec, ...updates } : null,
  }));
}

// ============================================
// Version History Actions
// ============================================

/**
 * Set version history
 */
export function setVersionHistory(versions: Spec[]): void {
  specStore.update((state) => ({
    ...state,
    specVersions: versions,
    versionsLoading: false,
    versionsError: null,
  }));
}

/**
 * Set versions loading state
 */
export function setVersionsLoading(loading: boolean): void {
  specStore.update((state) => ({
    ...state,
    versionsLoading: loading,
  }));
}

/**
 * Set versions error
 */
export function setVersionsError(error: string | null): void {
  specStore.update((state) => ({
    ...state,
    versionsError: error,
    versionsLoading: false,
  }));
}

/**
 * Clear version history
 */
export function clearVersionHistory(): void {
  specStore.update((state) => ({
    ...state,
    specVersions: [],
    versionsLoading: false,
    versionsError: null,
  }));
}

// ============================================
// Getters
// ============================================

/**
 * Get current state
 */
export function getSpecState(): SpecState {
  return specStore.get();
}

/**
 * Get specs list
 */
export function getSpecsList(): Spec[] {
  return specStore.get().specs;
}

/**
 * Get current spec
 */
export function getCurrentSpec(): SpecDetail | null {
  return specStore.get().currentSpec;
}

/**
 * Get version history from store
 */
export function getSpecVersions(): Spec[] {
  return specStore.get().specVersions;
}

/**
 * Get current filters
 */
export function getFilters(): {
  status: SpecStatus | null;
  codeSearch: string | null;
} {
  const state = specStore.get();
  return {
    status: state.statusFilter,
    codeSearch: state.codeSearch,
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
  const state = specStore.get();
  return {
    page: state.page,
    pageSize: state.pageSize,
    totalCount: state.totalCount,
    totalPages: state.totalPages,
  };
}

/**
 * Check if spec is editable (Draft status only)
 */
export function isCurrentSpecEditable(): boolean {
  const current = specStore.get().currentSpec;
  return current?.status === 'Draft';
}

/**
 * Check if current spec can create new version (Locked status only)
 */
export function canCurrentSpecCreateNewVersion(): boolean {
  const current = specStore.get().currentSpec;
  return current?.status === 'Locked';
}

// ============================================
// Subscribe
// ============================================

/**
 * Subscribe to spec state changes
 */
export function subscribeSpec(
  listener: (state: SpecState, prevState: SpecState) => void
): () => void {
  return specStore.subscribe(listener);
}

/**
 * Reset spec store
 */
export function resetSpecStore(): void {
  specStore.reset();
}

// Export the store for direct access
export { specStore };

// Re-export types for convenience
export type { SpecState } from '../types';
