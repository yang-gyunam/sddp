/**
 * Glossary Store - Glossary Management State Management
 * Manages glossary terms list, current term, and filters
 */

import { createStore, type Store } from '@sddp/shell/core';
import type {
  GlossaryTerm,
  GlossaryTermDetail,
  GlossaryTermSummary,
  GlossaryTermPage,
  GlossaryConflictResult,
  GlossaryTermUsage,
  TermCategory,
  GlossaryTermStatus,
} from '../types';

// ============================================
// State Interface
// ============================================

export interface GlossaryState {
  // Terms list
  terms: GlossaryTerm[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  termsLoading: boolean;
  termsError: string | null;

  // Filters
  categoryFilter: TermCategory | null;
  statusFilter: GlossaryTermStatus | null;
  searchQuery: string;

  // Current term
  currentTerm: GlossaryTermDetail | null;
  currentTermLoading: boolean;
  currentTermError: string | null;

  // Autocomplete
  autocompleteSuggestions: GlossaryTermSummary[];
  autocompleteLoading: boolean;

  // Conflict detection
  conflictResult: GlossaryConflictResult | null;
  conflictLoading: boolean;

  // Usage info
  termUsage: GlossaryTermUsage | null;
  usageLoading: boolean;
}

// ============================================
// Initial State
// ============================================

const initialState: GlossaryState = {
  // Terms list
  terms: [],
  totalCount: 0,
  page: 1,
  pageSize: 20,
  totalPages: 0,
  termsLoading: false,
  termsError: null,

  // Filters
  categoryFilter: null,
  statusFilter: null,
  searchQuery: '',

  // Current term
  currentTerm: null,
  currentTermLoading: false,
  currentTermError: null,

  // Autocomplete
  autocompleteSuggestions: [],
  autocompleteLoading: false,

  // Conflict detection
  conflictResult: null,
  conflictLoading: false,

  // Usage info
  termUsage: null,
  usageLoading: false,
};

// Create the store
const glossaryStore: Store<GlossaryState> = createStore<GlossaryState>(initialState);

// ============================================
// Terms List Actions
// ============================================

/**
 * Set terms page data
 */
export function setTermsPage(page: GlossaryTermPage): void {
  glossaryStore.update((state) => ({
    ...state,
    terms: page.items,
    totalCount: page.totalCount,
    page: page.page,
    pageSize: page.pageSize,
    totalPages: page.totalPages,
    termsLoading: false,
    termsError: null,
  }));
}

/**
 * Set terms loading state
 */
export function setTermsLoading(loading: boolean): void {
  glossaryStore.update((state) => ({
    ...state,
    termsLoading: loading,
  }));
}

/**
 * Set terms error
 */
export function setTermsError(error: string | null): void {
  glossaryStore.update((state) => ({
    ...state,
    termsError: error,
    termsLoading: false,
  }));
}

/**
 * Add a new term to the list
 */
export function addTerm(term: GlossaryTerm): void {
  glossaryStore.update((state) => ({
    ...state,
    terms: [term, ...state.terms],
    totalCount: state.totalCount + 1,
  }));
}

/**
 * Update a term in the list
 */
export function updateTermInList(updatedTerm: GlossaryTerm): void {
  glossaryStore.update((state) => ({
    ...state,
    terms: state.terms.map((t) => (t.id === updatedTerm.id ? updatedTerm : t)),
  }));
}

/**
 * Remove a term from the list
 */
export function removeTerm(termId: string): void {
  glossaryStore.update((state) => ({
    ...state,
    terms: state.terms.filter((t) => t.id !== termId),
    totalCount: state.totalCount - 1,
  }));
}

// ============================================
// Filter Actions
// ============================================

/**
 * Set category filter
 */
export function setCategoryFilter(category: TermCategory | null): void {
  glossaryStore.update((state) => ({
    ...state,
    categoryFilter: category,
    page: 1, // Reset to first page
  }));
}

/**
 * Set status filter
 */
export function setStatusFilter(status: GlossaryTermStatus | null): void {
  glossaryStore.update((state) => ({
    ...state,
    statusFilter: status,
    page: 1,
  }));
}

/**
 * Set search query
 */
export function setSearchQuery(query: string): void {
  glossaryStore.update((state) => ({
    ...state,
    searchQuery: query,
    page: 1,
  }));
}

/**
 * Set page number
 */
export function setPage(page: number): void {
  glossaryStore.update((state) => ({
    ...state,
    page,
  }));
}

/**
 * Clear all filters
 */
export function clearFilters(): void {
  glossaryStore.update((state) => ({
    ...state,
    categoryFilter: null,
    statusFilter: null,
    searchQuery: '',
    page: 1,
  }));
}

// ============================================
// Current Term Actions
// ============================================

/**
 * Set current term
 */
export function setCurrentTerm(term: GlossaryTermDetail | null): void {
  glossaryStore.update((state) => ({
    ...state,
    currentTerm: term,
    currentTermLoading: false,
    currentTermError: null,
  }));
}

/**
 * Set current term loading
 */
export function setCurrentTermLoading(loading: boolean): void {
  glossaryStore.update((state) => ({
    ...state,
    currentTermLoading: loading,
  }));
}

/**
 * Set current term error
 */
export function setCurrentTermError(error: string | null): void {
  glossaryStore.update((state) => ({
    ...state,
    currentTermError: error,
    currentTermLoading: false,
  }));
}

/**
 * Clear current term
 */
export function clearCurrentTerm(): void {
  glossaryStore.update((state) => ({
    ...state,
    currentTerm: null,
    currentTermLoading: false,
    currentTermError: null,
  }));
}

// ============================================
// Autocomplete Actions
// ============================================

/**
 * Set autocomplete suggestions
 */
export function setAutocompleteSuggestions(suggestions: GlossaryTermSummary[]): void {
  glossaryStore.update((state) => ({
    ...state,
    autocompleteSuggestions: suggestions,
    autocompleteLoading: false,
  }));
}

/**
 * Set autocomplete loading
 */
export function setAutocompleteLoading(loading: boolean): void {
  glossaryStore.update((state) => ({
    ...state,
    autocompleteLoading: loading,
  }));
}

/**
 * Clear autocomplete
 */
export function clearAutocomplete(): void {
  glossaryStore.update((state) => ({
    ...state,
    autocompleteSuggestions: [],
    autocompleteLoading: false,
  }));
}

// ============================================
// Conflict Detection Actions
// ============================================

/**
 * Set conflict result
 */
export function setConflictResult(result: GlossaryConflictResult | null): void {
  glossaryStore.update((state) => ({
    ...state,
    conflictResult: result,
    conflictLoading: false,
  }));
}

/**
 * Set conflict loading
 */
export function setConflictLoading(loading: boolean): void {
  glossaryStore.update((state) => ({
    ...state,
    conflictLoading: loading,
  }));
}

/**
 * Clear conflict result
 */
export function clearConflictResult(): void {
  glossaryStore.update((state) => ({
    ...state,
    conflictResult: null,
    conflictLoading: false,
  }));
}

// ============================================
// Usage Actions
// ============================================

/**
 * Set term usage
 */
export function setTermUsage(usage: GlossaryTermUsage | null): void {
  glossaryStore.update((state) => ({
    ...state,
    termUsage: usage,
    usageLoading: false,
  }));
}

/**
 * Set usage loading
 */
export function setUsageLoading(loading: boolean): void {
  glossaryStore.update((state) => ({
    ...state,
    usageLoading: loading,
  }));
}

/**
 * Clear usage
 */
export function clearUsage(): void {
  glossaryStore.update((state) => ({
    ...state,
    termUsage: null,
    usageLoading: false,
  }));
}

// ============================================
// Reset
// ============================================

/**
 * Reset the entire store to initial state
 */
export function resetGlossaryStore(): void {
  glossaryStore.set(initialState);
}

// ============================================
// Selectors
// ============================================

/**
 * Get current state
 */
export function getGlossaryState(): GlossaryState {
  return glossaryStore.get();
}

/**
 * Subscribe to store changes
 */
export function subscribeToGlossary(
  callback: (state: GlossaryState) => void
): () => void {
  return glossaryStore.subscribe(callback);
}

// Export the store for direct access if needed
export { glossaryStore };
