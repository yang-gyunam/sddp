/**
 * Settings Navigation Store
 * Manages settings category navigation
 */

import { createStore } from '@sddp/shell/core';

interface SettingsNavigationState {
  activeCategory: string;
  selectedProjectId: string | null;
  expandedCategories: Set<string>;
}

const initialState: SettingsNavigationState = {
  activeCategory: 'profile',
  selectedProjectId: null,
  expandedCategories: new Set(),
};

export const settingsNavigationStore = createStore<SettingsNavigationState>(initialState);

// Actions
export function setActiveCategory(categoryId: string): void {
  settingsNavigationStore.update((state) => ({
    ...state,
    activeCategory: categoryId,
    // Clear project selection when navigating away from project section
    selectedProjectId: categoryId.startsWith('project:') ? state.selectedProjectId : null,
  }));

  // Save to localStorage
  if (typeof window !== 'undefined') {
    localStorage.setItem('settings-active-category', categoryId);
  }
}

export function setActiveProjectCategory(projectId: string, categoryId: string): void {
  settingsNavigationStore.update((state) => ({
    ...state,
    activeCategory: `project:${categoryId}`,
    selectedProjectId: projectId,
  }));

  if (typeof window !== 'undefined') {
    localStorage.setItem('settings-active-category', `project:${categoryId}`);
    localStorage.setItem('settings-selected-project-id', projectId);
  }
}

export function toggleCategoryExpanded(categoryId: string): void {
  settingsNavigationStore.update((state) => {
    const expanded = new Set(state.expandedCategories);
    if (expanded.has(categoryId)) {
      expanded.delete(categoryId);
    } else {
      expanded.add(categoryId);
    }
    return { ...state, expandedCategories: expanded };
  });
}

export function loadSettingsNavigation(): void {
  if (typeof window !== 'undefined') {
    const savedCategory = localStorage.getItem('settings-active-category');
    const savedProjectId = localStorage.getItem('settings-selected-project-id');
    if (savedCategory) {
      settingsNavigationStore.update((state) => ({
        ...state,
        activeCategory: savedCategory,
        selectedProjectId: savedCategory.startsWith('project:') ? (savedProjectId || null) : null,
      }));
    }
  }
}

// Selectors
export function getActiveCategory(): string {
  return settingsNavigationStore.get().activeCategory;
}

export function isCategoryExpanded(categoryId: string): boolean {
  return settingsNavigationStore.get().expandedCategories.has(categoryId);
}

// Standard store exports
export function getSettingsNavigationState(): SettingsNavigationState {
  return settingsNavigationStore.get();
}

export function subscribeSettingsNavigation(listener: (state: SettingsNavigationState) => void): () => void {
  return settingsNavigationStore.subscribe(listener);
}

export function resetSettingsNavigationStore(): void {
  settingsNavigationStore.set(initialState);
}

// Type export
export type { SettingsNavigationState };
