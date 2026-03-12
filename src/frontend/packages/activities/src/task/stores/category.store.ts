/**
 * Category Store
 * Manages user-defined task categories (server-persisted via API)
 */

import { createStore, type Store } from '@sddp/shell/core';
import type { TaskCategory } from '../types';
import {
  getCategories,
  createCategory,
  deleteCategory,
  updateCategory,
} from '../services/TaskService';

// ============================================
// State
// ============================================

interface CategoryState {
  categories: TaskCategory[];
  tenantId: string | null;
}

const initialState: CategoryState = {
  categories: [],
  tenantId: null,
};

const categoryStore: Store<CategoryState> = createStore<CategoryState>(initialState);

// ============================================
// Actions
// ============================================

/**
 * Load categories from API for a tenant
 */
export async function loadCategories(tenantId: string): Promise<TaskCategory[]> {
  try {
    const categories = await getCategories(tenantId);
    categoryStore.set({ categories, tenantId });
    return categories;
  } catch {
    categoryStore.set({ categories: [], tenantId });
    return [];
  }
}

/**
 * Add a new category via API
 */
export async function addCategory(
  tenantId: string,
  name: string
): Promise<TaskCategory | null> {
  try {
    const category = await createCategory(tenantId, name);
    categoryStore.update((state) => ({
      ...state,
      categories: [...state.categories, category],
      tenantId,
    }));
    return category;
  } catch {
    return null;
  }
}

/**
 * Remove a category via API
 */
export async function removeCategory(tenantId: string, categoryId: string): Promise<void> {
  try {
    await deleteCategory(tenantId, categoryId);
    categoryStore.update((state) => ({
      ...state,
      categories: state.categories.filter((c) => c.id !== categoryId),
    }));
  } catch {
    // API error — state unchanged
  }
}

/**
 * Rename a category via API
 */
export async function renameCategory(
  tenantId: string,
  categoryId: string,
  newName: string
): Promise<void> {
  try {
    const updated = await updateCategory(tenantId, categoryId, { name: newName });
    categoryStore.update((state) => ({
      ...state,
      categories: state.categories.map((c) => (c.id === categoryId ? updated : c)),
    }));
  } catch {
    // API error — state unchanged
  }
}

// ============================================
// Selectors
// ============================================

/**
 * Get current category state
 */
export function getCategoryState(): CategoryState {
  return categoryStore.get();
}

/**
 * Subscribe to category state changes
 */
export function subscribeCategoryStore(
  callback: (state: CategoryState) => void
): () => void {
  return categoryStore.subscribe(callback);
}
