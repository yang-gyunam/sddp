/**
 * Glossary Sidebar Store
 * Manages sidebar UI state for Glossary Activity
 */

import { createStore, type Store } from '@sddp/shell/core';
import type {
  GlossarySidebarState,
  CategoryTermGroup,
  TermSummaryItem,
  GlossaryFilterType,
} from '../types';
import { TERM_CATEGORIES } from '../types/glossary.types';
import type { TermCategory } from '../types/glossary.types';

// ============================================
// Initial State
// ============================================

const initialState: GlossarySidebarState = {
  searchQuery: '',
  filterType: 'all',
  expandedCategories: new Set<TermCategory>(),
  selectedTermId: null,
  categoryGroups: [],
};

// Create the store
const glossarySidebarStore: Store<GlossarySidebarState> =
  createStore<GlossarySidebarState>(initialState);

// ============================================
// Actions
// ============================================

/**
 * Set category groups
 */
export function setCategoryGroups(groups: CategoryTermGroup[]): void {
  glossarySidebarStore.update((state) => ({
    ...state,
    categoryGroups: groups,
  }));
}

/**
 * Toggle category expansion
 */
export function toggleCategoryExpanded(category: TermCategory): void {
  glossarySidebarStore.update((state) => {
    const newExpanded = new Set(state.expandedCategories);
    if (newExpanded.has(category)) {
      newExpanded.delete(category);
    } else {
      newExpanded.add(category);
    }
    return {
      ...state,
      expandedCategories: newExpanded,
    };
  });
}

/**
 * Set selected term
 */
export function setSelectedTerm(termId: string | null): void {
  glossarySidebarStore.update((state) => ({
    ...state,
    selectedTermId: termId,
  }));
}

/**
 * Set search query
 */
export function setSidebarSearchQuery(query: string): void {
  glossarySidebarStore.update((state) => ({
    ...state,
    searchQuery: query,
  }));
}

/**
 * Set filter type
 */
export function setFilterType(filterType: GlossaryFilterType): void {
  glossarySidebarStore.update((state) => ({
    ...state,
    filterType,
  }));
}

/**
 * Clear all filters
 */
export function clearSidebarFilters(): void {
  glossarySidebarStore.update((state) => ({
    ...state,
    searchQuery: '',
    filterType: 'all',
  }));
}

/**
 * Expand all categories
 */
export function expandAllCategories(): void {
  glossarySidebarStore.update((state) => ({
    ...state,
    expandedCategories: new Set<TermCategory>(TERM_CATEGORIES),
  }));
}

/**
 * Collapse all categories
 */
export function collapseAllCategories(): void {
  glossarySidebarStore.update((state) => ({
    ...state,
    expandedCategories: new Set<TermCategory>(),
  }));
}

/**
 * Reset sidebar to initial state
 */
export function resetGlossarySidebar(): void {
  glossarySidebarStore.set(initialState);
}

// ============================================
// Selectors / Getters
// ============================================

/**
 * Get current state
 */
export function getGlossarySidebarState(): GlossarySidebarState {
  return glossarySidebarStore.get();
}

/**
 * Subscribe to store changes
 */
export function subscribeGlossarySidebar(
  callback: (state: GlossarySidebarState) => void
): () => void {
  return glossarySidebarStore.subscribe(callback);
}

/**
 * Get filtered category groups based on current filters
 */
export function getFilteredCategoryGroups(): CategoryTermGroup[] {
  const state = glossarySidebarStore.get();
  const { categoryGroups, searchQuery, filterType } = state;

  return categoryGroups
    .map((group) => {
      let filteredTerms = group.terms;

      // Apply status filter
      if (filterType !== 'all') {
        const statusMap: Record<GlossaryFilterType, string> = {
          all: '',
          draft: 'Draft',
          active: 'Active',
          deprecated: 'Deprecated',
        };
        const targetStatus = statusMap[filterType];
        filteredTerms = filteredTerms.filter((t) => t.status === targetStatus);
      }

      // Apply search filter
      if (searchQuery.trim()) {
        const query = searchQuery.toLowerCase();
        filteredTerms = filteredTerms.filter(
          (t) =>
            t.term.toLowerCase().includes(query) ||
            (t.abbreviation && t.abbreviation.toLowerCase().includes(query))
        );
      }

      return {
        ...group,
        terms: filteredTerms,
        expanded: state.expandedCategories.has(group.category),
      };
    })
    .filter((group) => group.terms.length > 0);
}

/**
 * Get selected term
 */
export function getSelectedTerm(): TermSummaryItem | null {
  const state = glossarySidebarStore.get();
  if (!state.selectedTermId) return null;

  for (const group of state.categoryGroups) {
    const term = group.terms.find((t) => t.id === state.selectedTermId);
    if (term) return term;
  }
  return null;
}

/**
 * Get status counts
 */
export function getStatusCounts(): Record<GlossaryFilterType, number> {
  const state = glossarySidebarStore.get();
  const counts: Record<GlossaryFilterType, number> = {
    all: 0,
    draft: 0,
    active: 0,
    deprecated: 0,
  };

  for (const group of state.categoryGroups) {
    for (const term of group.terms) {
      counts.all++;
      if (term.status === 'Draft') counts.draft++;
      if (term.status === 'Active') counts.active++;
      if (term.status === 'Deprecated') counts.deprecated++;
    }
  }

  return counts;
}

/**
 * Get total terms count
 */
export function getTotalTermsCount(): number {
  const state = glossarySidebarStore.get();
  return state.categoryGroups.reduce((sum, g) => sum + g.terms.length, 0);
}

// Export store for direct access
export { glossarySidebarStore };
