/**
 * Project Store
 * user project status
 */
import { writable, get } from 'svelte/store';
import type { ProjectWithBadges, ProjectDetail } from '../types/project.types';

interface ProjectState {
  projects: ProjectWithBadges[];
  projectsLoading: boolean;
  projectsError: string | null;
  expandedProjectIds: Set<string>;
  selectedProjectId: string | null;
  projectDetails: Map<string, ProjectDetail>;
}

const STORAGE_KEY = 'sddp-expanded-projects';

function loadExpandedProjects(): Set<string> {
  try {
    const stored = localStorage.getItem(STORAGE_KEY);
    if (stored) {
      return new Set(JSON.parse(stored));
    }
  } catch {
    // ignore
  }
  return new Set();
}

function saveExpandedProjects(ids: Set<string>) {
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify([...ids]));
  } catch {
    // ignore
  }
}

const initialState: ProjectState = {
  projects: [],
  projectsLoading: false,
  projectsError: null,
  expandedProjectIds: loadExpandedProjects(),
  selectedProjectId: null,
  projectDetails: new Map(),
};

export const projectStore = writable<ProjectState>(initialState);

export function getProjectState(): ProjectState {
  return get(projectStore);
}

export function setProjects(projects: ProjectWithBadges[]) {
  projectStore.update(state => ({
    ...state,
    projects,
    projectsError: null,
  }));
}

export function setProjectsLoading(loading: boolean) {
  projectStore.update(state => ({
    ...state,
    projectsLoading: loading,
  }));
}

export function setProjectsError(error: string | null) {
  projectStore.update(state => ({
    ...state,
    projectsError: error,
    projectsLoading: false,
  }));
}

export function toggleProjectExpanded(projectId: string) {
  projectStore.update(state => {
    const newExpanded = new Set(state.expandedProjectIds);
    if (newExpanded.has(projectId)) {
      newExpanded.delete(projectId);
    } else {
      newExpanded.add(projectId);
    }
    saveExpandedProjects(newExpanded);
    return {
      ...state,
      expandedProjectIds: newExpanded,
    };
  });
}

export function setProjectExpanded(projectId: string, expanded: boolean) {
  projectStore.update(state => {
    const newExpanded = new Set(state.expandedProjectIds);
    if (expanded) {
      newExpanded.add(projectId);
    } else {
      newExpanded.delete(projectId);
    }
    saveExpandedProjects(newExpanded);
    return {
      ...state,
      expandedProjectIds: newExpanded,
    };
  });
}

export function setSelectedProject(projectId: string | null) {
  projectStore.update(state => ({
    ...state,
    selectedProjectId: projectId,
  }));
}

export function setProjectDetail(projectId: string, detail: ProjectDetail) {
  projectStore.update(state => {
    const newDetails = new Map(state.projectDetails);
    newDetails.set(projectId, detail);
    return {
      ...state,
      projectDetails: newDetails,
    };
  });
}

export function subscribeProject(callback: (state: ProjectState) => void) {
  return projectStore.subscribe(callback);
}
