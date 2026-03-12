/**
 * TaskService - API Client for Task Management
 * Handles CRUD operations for tasks, status transitions, and time logs
 */

import { fetchWithAuth } from '../../shared/api';
import type { UserRef } from '../../shared/types';
import type {
  Task,
  TaskSummary,
  TaskStatus,
  TaskPriority,
  TaskCategory,
  BacklogSummary,
  BacklogStats,
} from '../types';

// ============================================
// API Response Types (matching backend DTOs)
// ============================================

export interface TaskItemDto {
  id: string;
  tenantId: string;
  projectId: string | null;
  title: string;
  description: string;
  status: TaskStatus;
  priority: TaskPriority;
  assignee: UserRef | null;
  creator: UserRef;
  estimatedHours: number;
  actualHours: number;
  linkedItemCount: number;
  sortOrder: number;
  categoryId: string | null;
  createdAt: string;
  updatedAt: string;
  completedAt: string | null;
}

export interface TaskItemDetailDto extends TaskItemDto {
  acceptanceCriteria: Array<{
    id: string;
    description: string;
    completed: boolean;
  }>;
  linkedItems: Array<{
    id: string;
    type: string;
    entityId: string;
    entityTitle: string;
    linkedBy: string;
    linkedAt: string;
  }>;
  timeLogs: Array<{
    id: string;
    taskId: string;
    user: UserRef;
    date: string;
    hours: number;
    description: string;
    createdAt: string;
  }>;
  createdBy: UserRef;
  updatedBy: UserRef;
}

export interface TaskItemPageDto {
  items: TaskItemDto[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface TaskTimeLogDto {
  id: string;
  taskId: string;
  user: UserRef;
  date: string;
  hours: number;
  description: string;
  createdAt: string;
}

export interface MyTaskStatsDto {
  toDoCount: number;
  inProgressCount: number;
  doneCount: number;
  blockedCount: number;
  totalCount: number;
}

export interface CreateTaskRequest {
  title: string;
  description: string;
  priority: TaskPriority;
  status?: TaskStatus;
  assigneeId?: string;
  estimatedHours?: number;
  categoryId?: string;
}

export interface CreateTimeLogRequest {
  date: string;
  hours: number;
  description: string;
}

// ============================================
// Task API
// ============================================

/**
 * Get list of tasks with pagination and filtering
 */
export async function getTasks(
  tenantId: string,
  options?: {
    projectId?: string;
    page?: number;
    pageSize?: number;
    status?: TaskStatus;
    priority?: TaskPriority;
    myTasksOnly?: boolean;
    categoryId?: string;
  }
): Promise<TaskItemPageDto> {
  const params = new URLSearchParams();
  if (options?.page) params.set('page', options.page.toString());
  if (options?.pageSize) params.set('pageSize', options.pageSize.toString());
  if (options?.status) params.set('status', options.status);
  if (options?.priority) params.set('priority', options.priority);
  if (options?.myTasksOnly) params.set('myTasksOnly', 'true');
  if (options?.categoryId) params.set('categoryId', options.categoryId);

  const queryString = params.toString();
  const endpoint = `/tasks${queryString ? `?${queryString}` : ''}`;

  return fetchWithAuth<TaskItemPageDto>(endpoint, {
    tenantId,
    projectId: options?.projectId,
  });
}

/**
 * Search tasks by title
 */
export async function searchTasks(
  tenantId: string,
  projectId: string,
  query: string,
  limit: number = 15
): Promise<Array<{ id: string; title: string; status: string }>> {
  const params = new URLSearchParams({ q: query, limit: limit.toString() });
  return fetchWithAuth<Array<{ id: string; title: string; status: string }>>(
    `/tasks/search?${params.toString()}`,
    { tenantId, projectId }
  );
}

/**
 * Get task detail by ID
 */
export async function getTaskById(
  tenantId: string,
  taskId: string
): Promise<TaskItemDetailDto> {
  return fetchWithAuth<TaskItemDetailDto>(`/tasks/${taskId}`, {
    tenantId,
  });
}

/**
 * Create a new task
 */
export async function createTask(
  tenantId: string,
  projectId: string | undefined,
  request: CreateTaskRequest
): Promise<TaskItemDetailDto> {
  return fetchWithAuth<TaskItemDetailDto>('/tasks', {
    method: 'POST',
    body: request,
    tenantId,
    projectId,
  });
}

/**
 * Update task status
 */
export async function updateTaskStatus(
  tenantId: string,
  taskId: string,
  newStatus: TaskStatus
): Promise<TaskItemDto> {
  return fetchWithAuth<TaskItemDto>(`/tasks/${taskId}/status`, {
    method: 'PUT',
    body: { newStatus },
    tenantId,
  });
}

/**
 * Add time log to a task
 */
export async function addTimeLog(
  tenantId: string,
  taskId: string,
  request: CreateTimeLogRequest
): Promise<TaskTimeLogDto> {
  return fetchWithAuth<TaskTimeLogDto>(`/tasks/${taskId}/time-logs`, {
    method: 'POST',
    body: request,
    tenantId,
  });
}

/**
 * Get my task statistics
 */
export async function getMyTaskStats(
  tenantId: string
): Promise<MyTaskStatsDto> {
  return fetchWithAuth<MyTaskStatsDto>('/tasks/my-stats', {
    tenantId,
  });
}

// ============================================
// Backlog API
// ============================================

/**
 * Get backlog summary (sidebar project list with task counts)
 */
export async function getBacklogSummary(
  tenantId: string
): Promise<BacklogSummary> {
  return fetchWithAuth<BacklogSummary>('/tasks/backlog/summary', {
    tenantId,
  });
}

/**
 * Get backlog stats for a specific project
 */
export async function getBacklogStats(
  tenantId: string,
  projectId: string
): Promise<BacklogStats> {
  return fetchWithAuth<BacklogStats>('/tasks/backlog/stats', {
    tenantId,
    projectId,
  });
}

// ============================================
// Category API
// ============================================

/**
 * Get task categories for the current user
 */
export async function getCategories(tenantId: string): Promise<TaskCategory[]> {
  return fetchWithAuth<TaskCategory[]>('/task-categories', { tenantId });
}

/**
 * Create a new task category
 */
export async function createCategory(
  tenantId: string,
  name: string,
  icon?: string,
  sortOrder: number = 0
): Promise<TaskCategory> {
  return fetchWithAuth<TaskCategory>('/task-categories', {
    method: 'POST',
    body: { name, icon, sortOrder },
    tenantId,
  });
}

/**
 * Update a task category
 */
export async function updateCategory(
  tenantId: string,
  categoryId: string,
  data: { name?: string; icon?: string; sortOrder?: number }
): Promise<TaskCategory> {
  return fetchWithAuth<TaskCategory>(`/task-categories/${categoryId}`, {
    method: 'PUT',
    body: data,
    tenantId,
  });
}

/**
 * Delete a task category
 */
export async function deleteCategory(tenantId: string, categoryId: string): Promise<void> {
  return fetchWithAuth<void>(`/task-categories/${categoryId}`, {
    method: 'DELETE',
    tenantId,
  });
}

/**
 * Reorder task categories
 */
export async function reorderCategories(
  tenantId: string,
  items: Array<{ id: string; sortOrder: number }>
): Promise<void> {
  return fetchWithAuth<void>('/task-categories/reorder', {
    method: 'PUT',
    body: { items },
    tenantId,
  });
}

// ============================================
// DTO → Domain Model Mappers
// ============================================

/**
 * Map TaskItemDto to TaskSummary (for sidebar/cards)
 */
export function mapToTaskSummary(dto: TaskItemDto, projectName: string = ''): TaskSummary {
  return {
    id: dto.id,
    projectId: dto.projectId ?? undefined,
    projectName,
    categoryId: dto.categoryId ?? undefined,
    title: dto.title,
    status: dto.status,
    priority: dto.priority,
    assignee: dto.assignee,
    estimatedHours: dto.estimatedHours,
    actualHours: dto.actualHours,
    linkedItemCount: dto.linkedItemCount,
    sortOrder: dto.sortOrder,
    hasOverdueEffort: dto.actualHours > dto.estimatedHours && dto.estimatedHours > 0,
  };
}

/**
 * Map TaskItemDetailDto to Task (full model)
 */
export function mapToTask(dto: TaskItemDetailDto, projectName: string = ''): Task {
  return {
    id: dto.id,
    tenantId: dto.tenantId,
    projectId: dto.projectId ?? undefined,
    projectName,
    categoryId: dto.categoryId ?? undefined,
    title: dto.title,
    description: dto.description,
    status: dto.status,
    priority: dto.priority,
    assignee: dto.assignee,
    creator: dto.creator,
    createdBy: dto.createdBy,
    updatedBy: dto.updatedBy,
    estimatedHours: dto.estimatedHours,
    actualHours: dto.actualHours,
    acceptanceCriteria: dto.acceptanceCriteria,
    linkedItems: dto.linkedItems.map((l) => ({
      id: l.id,
      type: l.type as Task['linkedItems'][0]['type'],
      entityId: l.entityId,
      entityTitle: l.entityTitle,
      linkedAt: l.linkedAt,
      linkedBy: l.linkedBy,
    })),
    timeLogs: dto.timeLogs.map((t) => ({
      id: t.id,
      taskId: t.taskId,
      user: t.user,
      date: t.date,
      hours: t.hours,
      description: t.description,
      createdAt: t.createdAt,
    })),
    createdAt: dto.createdAt,
    updatedAt: dto.updatedAt,
    completedAt: dto.completedAt ?? undefined,
  };
}

// ============================================
// Update Task
// ============================================

export interface UpdateTaskRequest {
  title?: string;
  description?: string;
  priority?: TaskPriority;
  assigneeId?: string | null;
  estimatedHours?: number;
  categoryId?: string | null;
}

/**
 * Update an existing task
 */
export async function updateTask(
  tenantId: string,
  taskId: string,
  request: UpdateTaskRequest
): Promise<TaskItemDetailDto> {
  return fetchWithAuth<TaskItemDetailDto>(`/tasks/${taskId}`, {
    method: 'PUT',
    body: request,
    tenantId,
  });
}

// ============================================
// Linked Items API
// ============================================

export interface CreateLinkedItemRequest {
  linkedType: 'conversation' | 'requirement' | 'spec' | 'artifact';
  linkedEntityId: string;
}

export interface TaskLinkedItemDto {
  id: string;
  type: string;
  entityId: string;
  entityTitle: string;
  linkedBy: string;
  linkedAt: string;
}

/**
 * Add a linked item to a task
 */
export async function addLinkedItem(
  tenantId: string,
  taskId: string,
  request: CreateLinkedItemRequest
): Promise<TaskLinkedItemDto> {
  return fetchWithAuth<TaskLinkedItemDto>(`/tasks/${taskId}/linked-items`, {
    method: 'POST',
    body: request,
    tenantId,
  });
}

/**
 * Delete a task (soft delete)
 */
export async function deleteTask(
  tenantId: string,
  taskId: string,
  projectId?: string
): Promise<void> {
  await fetchWithAuth<void>(`/tasks/${taskId}`, {
    method: 'DELETE',
    tenantId,
    projectId,
  });
}

/**
 * Remove a linked item from a task
 */
export async function removeLinkedItem(
  tenantId: string,
  taskId: string,
  linkedItemId: string
): Promise<void> {
  return fetchWithAuth<void>(`/tasks/${taskId}/linked-items/${linkedItemId}`, {
    method: 'DELETE',
    tenantId,
  });
}

// ============================================
// Position API (Kanban drag & drop)
// ============================================

export interface UpdatePositionRequest {
  newStatus: TaskStatus;
  newPosition: number;
}

/**
 * Update task position (Kanban drag & drop)
 */
export async function updateTaskPosition(
  tenantId: string,
  taskId: string,
  request: UpdatePositionRequest
): Promise<TaskItemDto> {
  return fetchWithAuth<TaskItemDto>(`/tasks/${taskId}/position`, {
    method: 'PUT',
    body: request,
    tenantId,
  });
}

// ============================================
// Service Class (for dependency injection)
// ============================================

/**
 * TaskService class with tenant/project context
 */
export class TaskServiceClass {
  private tenantId: string = '';
  private projectId?: string;

  setContext(tenantId: string, projectId?: string): void {
    this.tenantId = tenantId;
    this.projectId = projectId;
  }

  getTasks(options?: { page?: number; pageSize?: number; status?: TaskStatus; priority?: TaskPriority; myTasksOnly?: boolean; }): Promise<TaskItemPageDto> {
    return getTasks(this.tenantId, { ...options, projectId: this.projectId });
  }

  getTaskById(taskId: string): Promise<TaskItemDetailDto> {
    return getTaskById(this.tenantId, taskId);
  }

  createTask(request: CreateTaskRequest): Promise<TaskItemDetailDto> {
    return createTask(this.tenantId, this.projectId || '', request);
  }

  updateTask(taskId: string, request: UpdateTaskRequest): Promise<TaskItemDetailDto> {
    return updateTask(this.tenantId, taskId, request);
  }

  updateTaskStatus(taskId: string, newStatus: TaskStatus): Promise<TaskItemDto> {
    return updateTaskStatus(this.tenantId, taskId, newStatus);
  }

  addTimeLog(taskId: string, request: CreateTimeLogRequest): Promise<TaskTimeLogDto> {
    return addTimeLog(this.tenantId, taskId, request);
  }

  getMyTaskStats(): Promise<MyTaskStatsDto> {
    return getMyTaskStats(this.tenantId);
  }

  addLinkedItem(taskId: string, request: CreateLinkedItemRequest): Promise<TaskLinkedItemDto> {
    return addLinkedItem(this.tenantId, taskId, request);
  }

  removeLinkedItem(taskId: string, linkedItemId: string): Promise<void> {
    return removeLinkedItem(this.tenantId, taskId, linkedItemId);
  }

  deleteTask(taskId: string): Promise<void> {
    return deleteTask(this.tenantId, taskId, this.projectId);
  }

  updateTaskPosition(taskId: string, request: UpdatePositionRequest): Promise<TaskItemDto> {
    return updateTaskPosition(this.tenantId, taskId, request);
  }

  getBacklogSummary(): Promise<BacklogSummary> {
    return getBacklogSummary(this.tenantId);
  }

  getBacklogStats(projectId: string): Promise<BacklogStats> {
    return getBacklogStats(this.tenantId, projectId);
  }
}

// Singleton instance
let taskServiceInstance: TaskServiceClass | null = null;

/**
 * Get the singleton TaskService instance
 */
export function getTaskService(): TaskServiceClass {
  if (!taskServiceInstance) {
    taskServiceInstance = new TaskServiceClass();
  }
  return taskServiceInstance;
}

/**
 * Reset the singleton instance (for testing/logout)
 */
export function resetTaskService(): void {
  taskServiceInstance = null;
}
