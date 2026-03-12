/**
 * Dashboard View Store
 * Manages current dashboard view selection
 */

import { createStore } from '@sddp/shell/core';
import type { DashboardView } from '../types';

interface DashboardViewState {
  currentView: DashboardView;
  selectedProjectId: string | null;
}

const initialState: DashboardViewState = {
  currentView: 'my',
  selectedProjectId: null,
};

export const dashboardViewStore = createStore<DashboardViewState>(initialState);

// Actions
export function setDashboardView(view: DashboardView, projectId?: string): void {
  dashboardViewStore.update((state) => ({
    ...state,
    currentView: view,
    selectedProjectId: view === 'project' ? projectId || null : null,
  }));

  // Save to localStorage
  if (typeof window !== 'undefined') {
    localStorage.setItem('dashboard-view', view);
    if (projectId) {
      localStorage.setItem('dashboard-project-id', projectId);
    }
  }
}

export function loadDashboardView(): void {
  if (typeof window !== 'undefined') {
    const savedView = localStorage.getItem('dashboard-view') as DashboardView;
    const savedProjectId = localStorage.getItem('dashboard-project-id');

    if (savedView) {
      dashboardViewStore.update((state) => ({
        ...state,
        currentView: savedView,
        selectedProjectId: savedView === 'project' ? savedProjectId : null,
      }));
    }
  }
}

// Selectors
export function getCurrentView(): DashboardView {
  return dashboardViewStore.get().currentView;
}

export function getSelectedProjectId(): string | null {
  return dashboardViewStore.get().selectedProjectId;
}

export function getDashboardViewState(): DashboardViewState {
  return dashboardViewStore.get();
}

export function subscribeDashboardView(
  listener: (state: DashboardViewState, prevState: DashboardViewState) => void
): () => void {
  return dashboardViewStore.subscribe(listener);
}

export function resetDashboardViewStore(): void {
  dashboardViewStore.reset();
  if (typeof window !== 'undefined') {
    localStorage.removeItem('dashboard-view');
    localStorage.removeItem('dashboard-project-id');
  }
}

export type { DashboardViewState };
