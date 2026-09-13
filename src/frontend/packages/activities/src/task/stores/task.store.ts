/**
 * Task Store
 * Manages task sidebar UI state and task data
 */

import { createStore, type Store } from '@sddp/shell/core';
import type {
  TaskSidebarState,
  ProjectTaskGroup,
  TaskSummary,
  TaskFilterType,
  TaskStatus,
  MyTasksStats,
  BacklogSummary,
  BacklogStats,
} from '../types';

// ============================================
// Initial State
// ============================================

function createInitialState(): TaskSidebarState {
  return {
    searchQuery: '',
    filterType: 'all',
    selectedTaskId: null,
    myTasksStats: {
      toDoCount: 0,
      inProgressCount: 0,
      doneCount: 0,
      blockedCount: 0,
      totalCount: 0,
    },
    projectGroups: [],
    expandedProjects: new Set<string>(),
    viewMode: 'myTasks',
    selectedProjectId: null,
    backlogSummary: null,
    backlogStats: null,
    backlogLoading: false,
  };
}

/** Create an isolated task state store for a single editor tab. */
export function createTaskStoreInstance(): Store<TaskSidebarState> {
  return createStore<TaskSidebarState>(createInitialState());
}

const taskStore: Store<TaskSidebarState> = createTaskStoreInstance();

// ============================================
// Actions
// ============================================

/**
 * Set project groups
 */
export function setTaskProjectGroups(
  groups: ProjectTaskGroup[],
  target: Store<TaskSidebarState> = taskStore
): void {
  target.update((state) => ({
    ...state,
    projectGroups: groups,
  }));
}

/**
 * Set my tasks stats
 */
export function setMyTasksStats(
  stats: MyTasksStats,
  target: Store<TaskSidebarState> = taskStore
): void {
  target.update((state) => ({
    ...state,
    myTasksStats: stats,
  }));
}

/**
 * Toggle project expansion
 */
export function toggleTaskProjectExpanded(projectId: string): void {
  taskStore.update((state) => {
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
 * Set selected task
 */
export function setSelectedTask(
  taskId: string | null,
  target: Store<TaskSidebarState> = taskStore
): void {
  target.update((state) => ({
    ...state,
    selectedTaskId: taskId,
  }));
}

/**
 * Set search query
 */
export function setTaskSearchQuery(
  query: string,
  target: Store<TaskSidebarState> = taskStore
): void {
  target.update((state) => ({
    ...state,
    searchQuery: query,
  }));
}

/**
 * Set filter type
 */
export function setTaskFilterType(
  filterType: TaskFilterType,
  target: Store<TaskSidebarState> = taskStore
): void {
  target.update((state) => ({
    ...state,
    filterType,
  }));
}

/**
 * Set view mode
 */
export function setTaskViewMode(
  mode: 'myTasks' | 'backlog',
  target: Store<TaskSidebarState> = taskStore
): void {
  target.update((state) => ({
    ...state,
    viewMode: mode,
  }));
}

/**
 * Set selected project
 */
export function setSelectedProject(projectId: string | null): void {
  taskStore.update((state) => ({
    ...state,
    selectedProjectId: projectId,
    viewMode: projectId ? 'backlog' : 'myTasks',
  }));
}

/**
 * Select a backlog project (switches to backlog view)
 */
export function selectBacklogProject(
  projectId: string,
  target: Store<TaskSidebarState> = taskStore
): void {
  target.update((state) => ({
    ...state,
    selectedProjectId: projectId,
    viewMode: 'backlog',
  }));
}

/**
 * Set backlog summary
 */
export function setBacklogSummary(
  summary: BacklogSummary | null,
  target: Store<TaskSidebarState> = taskStore
): void {
  target.update((state) => ({
    ...state,
    backlogSummary: summary,
  }));
}

/**
 * Set backlog stats
 */
export function setBacklogStats(
  stats: BacklogStats | null,
  target: Store<TaskSidebarState> = taskStore
): void {
  target.update((state) => ({
    ...state,
    backlogStats: stats,
  }));
}

/**
 * Set backlog loading
 */
export function setBacklogLoading(
  loading: boolean,
  target: Store<TaskSidebarState> = taskStore
): void {
  target.update((state) => ({
    ...state,
    backlogLoading: loading,
  }));
}

/**
 * Clear all filters
 */
export function clearTaskFilters(): void {
  taskStore.update((state) => ({
    ...state,
    searchQuery: '',
    filterType: 'all',
  }));
}

/**
 * Reset store to initial state
 */
export function resetTaskStore(): void {
  taskStore.set(createInitialState());
}

// ============================================
// Selectors / Getters
// ============================================

/**
 * Get current state
 */
export function getTaskState(): TaskSidebarState {
  return taskStore.get();
}

/**
 * Subscribe to store changes
 */
export function subscribeTask(
  callback: (state: TaskSidebarState) => void,
  target: Store<TaskSidebarState> = taskStore
): () => void {
  return target.subscribe(callback);
}

/** Actions bound to one task tab store. */
export function createTaskStoreScope() {
  const store = createTaskStoreInstance();

  return {
    getState: () => store.get(),
    subscribe: (callback: (state: TaskSidebarState) => void) => subscribeTask(callback, store),
    setSelectedTask: (taskId: string | null) => setSelectedTask(taskId, store),
    setTaskSearchQuery: (query: string) => setTaskSearchQuery(query, store),
    setTaskFilterType: (filterType: TaskFilterType) => setTaskFilterType(filterType, store),
    setTaskViewMode: (mode: 'myTasks' | 'backlog') => setTaskViewMode(mode, store),
    selectBacklogProject: (projectId: string) => selectBacklogProject(projectId, store),
    setBacklogSummary: (summary: BacklogSummary | null) => setBacklogSummary(summary, store),
    setBacklogStats: (stats: BacklogStats | null) => setBacklogStats(stats, store),
    setBacklogLoading: (loading: boolean) => setBacklogLoading(loading, store),
    setMyTasksStats: (stats: MyTasksStats) => setMyTasksStats(stats, store),
    setTaskProjectGroups: (groups: ProjectTaskGroup[]) => setTaskProjectGroups(groups, store),
  };
}

/**
 * Get filtered project groups
 */
export function getFilteredTaskProjectGroups(): ProjectTaskGroup[] {
  const state = taskStore.get();
  const { projectGroups, searchQuery, filterType } = state;

  return projectGroups
    .map((group) => {
      let filteredTasks = group.tasks;

      // Apply status filter
      if (filterType !== 'all') {
        const statusMap: Record<TaskFilterType, TaskStatus | null> = {
          all: null,
          todo: 'ToDo',
          inProgress: 'InProgress',
          done: 'Done',
          blocked: 'Blocked',
        };
        const targetStatus = statusMap[filterType];
        if (targetStatus) {
          filteredTasks = filteredTasks.filter((t) => t.status === targetStatus);
        }
      }

      // Apply search filter
      if (searchQuery.trim()) {
        const query = searchQuery.toLowerCase();
        filteredTasks = filteredTasks.filter(
          (t) =>
            t.title.toLowerCase().includes(query) ||
            (t.assignee?.name ?? '').toLowerCase().includes(query)
        );
      }

      return {
        ...group,
        tasks: filteredTasks,
        expanded: state.expandedProjects.has(group.projectId),
      };
    })
    .filter((group) => group.tasks.length > 0);
}

/**
 * Get all tasks (flattened)
 */
export function getAllTasks(): TaskSummary[] {
  const state = taskStore.get();
  return state.projectGroups.flatMap((g) => g.tasks);
}

/**
 * Get tasks by status
 */
export function getTasksByStatus(status: TaskStatus): TaskSummary[] {
  return getAllTasks().filter((t) => t.status === status);
}

/**
 * Get selected task
 */
export function getSelectedTask(): TaskSummary | null {
  const state = taskStore.get();
  if (!state.selectedTaskId) return null;

  for (const group of state.projectGroups) {
    const task = group.tasks.find((t) => t.id === state.selectedTaskId);
    if (task) return task;
  }
  return null;
}

/**
 * Get status filter counts
 */
export function getTaskFilterCounts(): Record<TaskFilterType, number> {
  const state = taskStore.get();
  return {
    all: state.myTasksStats.totalCount,
    todo: state.myTasksStats.toDoCount,
    inProgress: state.myTasksStats.inProgressCount,
    done: state.myTasksStats.doneCount,
    blocked: state.myTasksStats.blockedCount,
  };
}

// Export store
export { taskStore };
