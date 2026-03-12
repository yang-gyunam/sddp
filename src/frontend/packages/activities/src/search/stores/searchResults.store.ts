/**
 * Search Results Store
 * Manages search results and loading state
 */

import { createStore } from '@sddp/shell/core';
import type { SearchResult } from '../types';

interface SearchResultsState {
  results: SearchResult | null;
  loading: boolean;
  error: string | null;
}

const initialState: SearchResultsState = {
  results: null,
  loading: false,
  error: null,
};

export const searchResultsStore = createStore<SearchResultsState>(initialState);

// Actions
export function setSearchResults(results: SearchResult): void {
  searchResultsStore.update((state) => ({
    ...state,
    results,
    loading: false,
    error: null,
  }));
}

export function appendSearchResults(newResults: SearchResult): void {
  searchResultsStore.update((state) => {
    if (!state.results) {
      return { ...state, results: newResults, loading: false, error: null };
    }

    return {
      ...state,
      results: {
        total: newResults.total,
        results: [...state.results.results, ...newResults.results],
        facets: newResults.facets,
      },
      loading: false,
      error: null,
    };
  });
}

export function setSearchLoading(loading: boolean): void {
  searchResultsStore.update((state) => ({
    ...state,
    loading,
    error: loading ? null : state.error,
  }));
}

export function setSearchError(error: string): void {
  searchResultsStore.update((state) => ({
    ...state,
    loading: false,
    error,
  }));
}

export function clearSearchResults(): void {
  searchResultsStore.set(initialState);
}

// Selectors
export function getSearchResults(): SearchResult | null {
  return searchResultsStore.get().results;
}

export function isSearchLoading(): boolean {
  return searchResultsStore.get().loading;
}

export function getSearchError(): string | null {
  return searchResultsStore.get().error;
}

// Standard store exports
export function getSearchResultsState(): SearchResultsState {
  return searchResultsStore.get();
}

export function subscribeSearchResults(listener: (state: SearchResultsState) => void): () => void {
  return searchResultsStore.subscribe(listener);
}

export function resetSearchResultsStore(): void {
  searchResultsStore.set(initialState);
}

// Type export
export type { SearchResultsState };
