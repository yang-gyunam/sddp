/**
 * Search Query Store
 * Manages the current search query and filters
 */

import { createStore } from '@sddp/shell/core';
import type { SearchQuery, SearchFilters, SearchSort } from '../types';

interface SearchQueryState {
  query: SearchQuery;
  recentSearches: string[];
}

const initialFilters: SearchFilters = {
  types: [],
  projectIds: [],
  statuses: [],
};

const initialQuery: SearchQuery = {
  text: '',
  filters: initialFilters,
  sort: 'relevance',
  page: 1,
  pageSize: 20,
};

const initialState: SearchQueryState = {
  query: initialQuery,
  recentSearches: [],
};

export const searchQueryStore = createStore<SearchQueryState>(initialState);

// Actions
export function setSearchText(text: string): void {
  searchQueryStore.update((state) => ({
    ...state,
    query: { ...state.query, text, page: 1 },
  }));
}

export function setSearchFilters(filters: Partial<SearchFilters>): void {
  searchQueryStore.update((state) => ({
    ...state,
    query: {
      ...state.query,
      filters: { ...state.query.filters, ...filters },
      page: 1,
    },
  }));
}

export function setSearchSort(sort: SearchSort): void {
  searchQueryStore.update((state) => ({
    ...state,
    query: { ...state.query, sort, page: 1 },
  }));
}

export function setSearchPage(page: number): void {
  searchQueryStore.update((state) => ({
    ...state,
    query: { ...state.query, page },
  }));
}

export function clearFilters(): void {
  searchQueryStore.update((state) => ({
    ...state,
    query: { ...state.query, filters: initialFilters, page: 1 },
  }));
}

export function addRecentSearch(text: string): void {
  if (!text.trim()) return;

  searchQueryStore.update((state) => {
    const recent = [text, ...state.recentSearches.filter((s) => s !== text)].slice(0, 5);
    return { ...state, recentSearches: recent };
  });
}

export function clearRecentSearches(): void {
  searchQueryStore.update((state) => ({
    ...state,
    recentSearches: [],
  }));
}

export function resetSearchQuery(): void {
  searchQueryStore.set(initialState);
}

// Selectors
export function getSearchQuery(): SearchQuery {
  return searchQueryStore.get().query;
}

export function getRecentSearches(): string[] {
  return searchQueryStore.get().recentSearches;
}

export function hasActiveFilters(): boolean {
  const { filters } = searchQueryStore.get().query;
  return (
    filters.types.length > 0 ||
    filters.projectIds.length > 0 ||
    filters.statuses.length > 0 ||
    !!filters.dateRange ||
    !!filters.authorId ||
    !!filters.modifiedById ||
    !!filters.hasAttachments ||
    !!filters.hasRelationships
  );
}

// Standard store exports
export function getSearchQueryState(): SearchQueryState {
  return searchQueryStore.get();
}

export function subscribeSearchQuery(listener: (state: SearchQueryState) => void): () => void {
  return searchQueryStore.subscribe(listener);
}

// Type export
export type { SearchQueryState };
