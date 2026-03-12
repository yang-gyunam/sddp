/**
 * Saved Searches Store
 * Manages saved search queries
 */

import { createStore } from '@sddp/shell/core';
import type { SavedSearch } from '../types';

interface SavedSearchesState {
  searches: SavedSearch[];
  loading: boolean;
}

const initialState: SavedSearchesState = {
  searches: [],
  loading: false,
};

export const savedSearchesStore = createStore<SavedSearchesState>(initialState);

// Actions
export function setSavedSearches(searches: SavedSearch[]): void {
  savedSearchesStore.update((state) => ({
    ...state,
    searches,
    loading: false,
  }));
}

export function addSavedSearch(search: SavedSearch): void {
  savedSearchesStore.update((state) => ({
    ...state,
    searches: [search, ...state.searches],
  }));
}

export function removeSavedSearch(id: string): void {
  savedSearchesStore.update((state) => ({
    ...state,
    searches: state.searches.filter((s) => s.id !== id),
  }));
}

export function updateSavedSearch(id: string, updates: Partial<SavedSearch>): void {
  savedSearchesStore.update((state) => ({
    ...state,
    searches: state.searches.map((s) => (s.id === id ? { ...s, ...updates } : s)),
  }));
}

export function setSavedSearchesLoading(loading: boolean): void {
  savedSearchesStore.update((state) => ({
    ...state,
    loading,
  }));
}

// Selectors
export function getSavedSearches(): SavedSearch[] {
  return savedSearchesStore.get().searches;
}

export function getSavedSearchById(id: string): SavedSearch | undefined {
  return savedSearchesStore.get().searches.find((s) => s.id === id);
}
