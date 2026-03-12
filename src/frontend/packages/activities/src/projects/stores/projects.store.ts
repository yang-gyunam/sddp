/**
 * Projects Store - Project State Management
 * Manages projects list, current project, and sidebar state
 */

import { createStore, type Store } from '@sddp/shell/core';
import type { Project, ProjectDetail, ProjectsState, ProjectSidebarBadges } from '../types';
import { getProjectBadges } from '../types';

// ============================================
// Initial State
// ============================================

const initialState: ProjectsState = {
  // Projects list
  projects: [],
  projectsLoading: false,
  projectsError: null,

  // Current project
  currentProject: null,
  currentProjectLoading: false,
  currentProjectError: null,

  // Sidebar state
  expandedProjects: new Set<string>(),
  selectedNodePath: null,

  // Search/filter
  searchQuery: '',
};

// Create the store
const projectsStore: Store<ProjectsState> = createStore<ProjectsState>(initialState);

// ============================================
// Projects List Actions
// ============================================

/**
 * Set projects list
 */
export function setProjects(projects: Project[]): void {
  projectsStore.update((state) => ({
    ...state,
    projects,
    projectsLoading: false,
    projectsError: null,
  }));
}

/**
 * Set projects loading state
 */
export function setProjectsLoading(loading: boolean): void {
  projectsStore.update((state) => ({
    ...state,
    projectsLoading: loading,
  }));
}

/**
 * Set projects error
 */
export function setProjectsError(error: string | null): void {
  projectsStore.update((state) => ({
    ...state,
    projectsError: error,
    projectsLoading: false,
  }));
}

/**
 * Add a new project to the list
 */
export function addProject(project: Project): void {
  projectsStore.update((state) => ({
    ...state,
    projects: [project, ...state.projects],
  }));
}

/**
 * Update a project in the list
 */
export function updateProjectInList(projectId: string, updates: Partial<Project>): void {
  projectsStore.update((state) => ({
    ...state,
    projects: state.projects.map((p) => (p.id === projectId ? { ...p, ...updates } : p)),
  }));
}

/**
 * Remove a project from the list
 */
export function removeProject(projectId: string): void {
  projectsStore.update((state) => ({
    ...state,
    projects: state.projects.filter((p) => p.id !== projectId),
  }));
}

// ============================================
// Current Project Actions
// ============================================

/**
 * Set current project (detail view)
 */
export function setCurrentProject(project: ProjectDetail | null): void {
  projectsStore.update((state) => ({
    ...state,
    currentProject: project,
    currentProjectLoading: false,
    currentProjectError: null,
  }));
}

/**
 * Set current project loading state
 */
export function setCurrentProjectLoading(loading: boolean): void {
  projectsStore.update((state) => ({
    ...state,
    currentProjectLoading: loading,
  }));
}

/**
 * Set current project error
 */
export function setCurrentProjectError(error: string | null): void {
  projectsStore.update((state) => ({
    ...state,
    currentProjectError: error,
    currentProjectLoading: false,
  }));
}

/**
 * Clear current project
 */
export function clearCurrentProject(): void {
  projectsStore.update((state) => ({
    ...state,
    currentProject: null,
    currentProjectLoading: false,
    currentProjectError: null,
  }));
}

/**
 * Update current project
 */
export function updateCurrentProject(updates: Partial<ProjectDetail>): void {
  projectsStore.update((state) => ({
    ...state,
    currentProject: state.currentProject ? { ...state.currentProject, ...updates } : null,
  }));
}

// ============================================
// Sidebar State Actions
// ============================================

/**
 * Toggle project expanded state
 */
export function toggleProjectExpanded(projectId: string): void {
  projectsStore.update((state) => {
    const newExpanded = new Set(state.expandedProjects);
    if (newExpanded.has(projectId)) {
      newExpanded.delete(projectId);
    } else {
      newExpanded.add(projectId);
    }
    return {
      ...state,
      expandedProjects: newExpanded,
    };
  });
}

/**
 * Set project expanded state
 */
export function setProjectExpanded(projectId: string, expanded: boolean): void {
  projectsStore.update((state) => {
    const newExpanded = new Set(state.expandedProjects);
    if (expanded) {
      newExpanded.add(projectId);
    } else {
      newExpanded.delete(projectId);
    }
    return {
      ...state,
      expandedProjects: newExpanded,
    };
  });
}

/**
 * Set selected node path
 */
export function setSelectedNodePath(path: string | null): void {
  projectsStore.update((state) => ({
    ...state,
    selectedNodePath: path,
  }));
}

/**
 * Expand all projects
 */
export function expandAllProjects(): void {
  projectsStore.update((state) => ({
    ...state,
    expandedProjects: new Set(state.projects.map((p) => p.id)),
  }));
}

/**
 * Collapse all projects
 */
export function collapseAllProjects(): void {
  projectsStore.update((state) => ({
    ...state,
    expandedProjects: new Set<string>(),
  }));
}

// ============================================
// Search/Filter Actions
// ============================================

/**
 * Set search query
 */
export function setSearchQuery(query: string): void {
  projectsStore.update((state) => ({
    ...state,
    searchQuery: query,
  }));
}

/**
 * Clear search query
 */
export function clearSearchQuery(): void {
  projectsStore.update((state) => ({
    ...state,
    searchQuery: '',
  }));
}

// ============================================
// Getters
// ============================================

/**
 * Get current state
 */
export function getProjectsState(): ProjectsState {
  return projectsStore.get();
}

/**
 * Get projects list
 */
export function getProjects(): Project[] {
  return projectsStore.get().projects;
}

/**
 * Get filtered projects (by search query)
 */
export function getFilteredProjects(): Project[] {
  const state = projectsStore.get();
  if (!state.searchQuery) {
    return state.projects;
  }
  const query = state.searchQuery.toLowerCase();
  return state.projects.filter(
    (p) => p.name.toLowerCase().includes(query) || p.code.toLowerCase().includes(query)
  );
}

/**
 * Get current project
 */
export function getCurrentProject(): ProjectDetail | null {
  return projectsStore.get().currentProject;
}

/**
 * Get expanded projects set
 */
export function getExpandedProjects(): Set<string> {
  return projectsStore.get().expandedProjects;
}

/**
 * Get selected node path
 */
export function getSelectedNodePath(): string | null {
  return projectsStore.get().selectedNodePath;
}

/**
 * Check if project is expanded
 */
export function isProjectExpanded(projectId: string): boolean {
  return projectsStore.get().expandedProjects.has(projectId);
}

/**
 * Get project badges (mock)
 */
export function getProjectBadgesFromStore(projectId: string): ProjectSidebarBadges {
  return getProjectBadges(projectId);
}

/**
 * Load project detail from API
 */
export async function loadProjectDetail(projectId: string, tenantId?: string): Promise<void> {
  setCurrentProjectLoading(true);
  try {
    if (!tenantId) throw new Error('No tenant context');
    const { getProjectById } = await import('../services/ProjectService');
    const detail = await getProjectById(tenantId, projectId);
    setCurrentProject(detail);
  } catch (err) {
    console.warn('loadProjectDetail failed, using fallback:', err);
    // Fallback: construct minimal detail from list data
    const project = getProjects().find((p) => p.id === projectId);
    if (project) {
      setCurrentProject({
        ...project,
        statistics: {
          conversations: { total: 0, secondary: 0 },
          requirements: { total: 0, secondary: 0 },
          specs: { total: 0, secondary: 0 },
          artifacts: { total: 0, secondary: 0 },
          tasks: { total: 0, secondary: 0 },
          glossary: { total: 0, secondary: 0 },
          effort: { total: 0, secondary: 0 },
        },
        members: [],
      });
    } else {
      setCurrentProjectError('Project not found');
    }
  }
}

// ============================================
// Subscribe
// ============================================

/**
 * Subscribe to projects state changes
 */
export function subscribeProjects(
  listener: (state: ProjectsState, prevState: ProjectsState) => void
): () => void {
  return projectsStore.subscribe(listener);
}

/**
 * Reset projects store
 */
export function resetProjectsStore(): void {
  projectsStore.reset();
}

// Export the store for direct access
export { projectsStore };

// Re-export pending navigation (standalone, no store deps)
export { setPendingEntityId, consumePendingEntityId } from './pending-navigation';

// Re-export types for convenience
export type { ProjectsState } from '../types';
