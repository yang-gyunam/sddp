/**
 * Task Type Definitions
 * Based on 06-activity-tasks.md specification
 */

import type { UserRef } from '../../shared/types';

// ============================================
// Enums
// ============================================

/**
 * Task status
 */
export type TaskStatus = 'Backlog' | 'ToDo' | 'InProgress' | 'Done' | 'Blocked';

/**
 * Task priority
 */
export type TaskPriority = 'High' | 'Medium' | 'Low';

/**
 * Linked item type
 */
export type LinkedItemType = 'conversation' | 'requirement' | 'spec' | 'artifact';

// ============================================
// DTOs
// ============================================

/**
 * Acceptance criterion
 */
export interface AcceptanceCriterion {
  id: string;
  description: string;
  completed: boolean;
}

/**
 * Linked item
 */
export interface LinkedItem {
  id: string;
  type: LinkedItemType;
  entityId: string;
  entityTitle: string;
  linkedAt: string;
  linkedBy: string;
}

/**
 * Time log entry
 */
export interface TimeLog {
  id: string;
  taskId: string;
  user: UserRef;
  date: string;
  hours: number;
  description: string;
  createdAt: string;
}

/**
 * Full task model
 */
export interface Task {
  id: string;
  tenantId: string;
  projectId?: string;
  projectName?: string;
  categoryId?: string;
  title: string;
  description: string;
  status: TaskStatus;
  priority: TaskPriority;
  assignee: UserRef | null;
  creator: UserRef;
  createdBy: UserRef;
  updatedBy: UserRef;
  estimatedHours: number;
  actualHours: number;
  acceptanceCriteria: AcceptanceCriterion[];
  linkedItems: LinkedItem[];
  timeLogs: TimeLog[];
  createdAt: string;
  updatedAt: string;
  completedAt?: string;
}

/**
 * Task summary for sidebar and cards
 */
export interface TaskSummary {
  id: string;
  projectId?: string;
  projectName?: string;
  categoryId?: string;
  title: string;
  status: TaskStatus;
  priority: TaskPriority;
  assignee: UserRef | null;
  estimatedHours: number;
  actualHours: number;
  linkedItemCount: number;
  sortOrder: number;
  hasOverdueEffort: boolean;
}

/**
 * Project task statistics
 */
export interface ProjectTaskStats {
  projectId: string;
  projectName: string;
  toDoCount: number;
  inProgressCount: number;
  doneCount: number;
  totalCount: number;
  activeCount: number;
}

/**
 * Project task group for sidebar
 */
export interface ProjectTaskGroup {
  projectId: string;
  projectName: string;
  stats: ProjectTaskStats;
  tasks: TaskSummary[];
  expanded: boolean;
}

/**
 * My tasks summary (aggregated across projects)
 */
export interface MyTasksStats {
  toDoCount: number;
  inProgressCount: number;
  doneCount: number;
  blockedCount: number;
  totalCount: number;
}

// ============================================
// Backlog Types
// ============================================

export interface BacklogProjectSummary {
  projectId: string;
  projectName: string;
  isOwner: boolean;
  activeTaskCount: number;
  totalTaskCount: number;
}

export interface BacklogSummary {
  projects: BacklogProjectSummary[];
  totalTasks: number;
  totalProjects: number;
}

export interface BacklogPriorityDistribution {
  priority: TaskPriority;
  count: number;
}

export interface BacklogAssigneeStats {
  assignee: UserRef;
  backlogCount: number;
  toDoCount: number;
  inProgressCount: number;
  doneCount: number;
  blockedCount: number;
  totalCount: number;
}

export interface BacklogStats {
  projectId: string;
  projectName: string;
  isOwner: boolean;
  totalTasks: number;
  backlogCount: number;
  toDoCount: number;
  inProgressCount: number;
  doneCount: number;
  blockedCount: number;
  totalEstimatedHours: number;
  totalActualHours: number;
  priorityDistribution: BacklogPriorityDistribution[];
  assigneeStats: BacklogAssigneeStats[];
}

// ============================================
// Category Types
// ============================================

/**
 * User-defined task category (server-persisted)
 */
export interface TaskCategory {
  id: string;
  name: string;
  icon?: string;
  sortOrder: number;
  createdAt: string;
}

// ============================================
// Filter & Sidebar State
// ============================================

/**
 * Filter type for task sidebar
 */
export type TaskFilterType = 'all' | 'todo' | 'inProgress' | 'done' | 'blocked';

/**
 * Task sidebar state
 */
export interface TaskSidebarState {
  searchQuery: string;
  filterType: TaskFilterType;
  selectedTaskId: string | null;
  myTasksStats: MyTasksStats;
  projectGroups: ProjectTaskGroup[];
  expandedProjects: Set<string>;
  viewMode: 'myTasks' | 'backlog';
  selectedProjectId: string | null;
  backlogSummary: BacklogSummary | null;
  backlogStats: BacklogStats | null;
  backlogLoading: boolean;
}

// ============================================
// Style Definitions
// ============================================

export interface TaskStatusStyle {
  color: string;
  bgColor: string;
  borderColor: string;
  label: string;
  icon: string;
}

export interface TaskPriorityStyle {
  color: string;
  bgColor: string;
  borderColor: string;
  label: string;
  icon: string;
}

/**
 * Task status styles
 */
export const TASK_STATUS_STYLES: Record<TaskStatus, TaskStatusStyle> = {
  Backlog: {
    color: 'text-[var(--color-neutral-500)] dark:text-[var(--color-neutral-400)]',
    bgColor: 'bg-[var(--color-neutral-400)]/10',
    borderColor: 'border-[var(--color-neutral-400)]/25',
    label: 'Backlog',
    icon: 'archive',
  },
  ToDo: {
    color: 'text-[var(--color-neutral-600)] dark:text-[var(--color-neutral-400)]',
    bgColor: 'bg-[var(--color-neutral-500)]/10',
    borderColor: 'border-[var(--color-neutral-500)]/20',
    label: 'To Do',
    icon: 'circle',
  },
  InProgress: {
    color: 'text-[var(--color-info-600)] dark:text-[var(--color-info-400)]',
    bgColor: 'bg-[var(--color-info-500)]/10',
    borderColor: 'border-[var(--color-info-500)]/20',
    label: 'In Progress',
    icon: 'loader',
  },
  Done: {
    color: 'text-[var(--color-success-600)] dark:text-[var(--color-success-400)]',
    bgColor: 'bg-[var(--color-success-500)]/10',
    borderColor: 'border-[var(--color-success-500)]/20',
    label: 'Done',
    icon: 'check-circle',
  },
  Blocked: {
    color: 'text-[var(--color-error-600)] dark:text-[var(--color-error-400)]',
    bgColor: 'bg-[var(--color-error-500)]/10',
    borderColor: 'border-[var(--color-error-500)]/20',
    label: 'Blocked',
    icon: 'x-circle',
  },
};

/**
 * Task priority styles
 */
export const TASK_PRIORITY_STYLES: Record<TaskPriority, TaskPriorityStyle> = {
  High: {
    color: 'text-[var(--color-error-600)] dark:text-[var(--color-error-400)]',
    bgColor: 'bg-[var(--color-error-500)]/10',
    borderColor: 'border-[var(--color-error-500)]/20',
    label: 'High',
    icon: 'arrow-up',
  },
  Medium: {
    color: 'text-[var(--color-warning-600)] dark:text-[var(--color-warning-400)]',
    bgColor: 'bg-[var(--color-warning-500)]/10',
    borderColor: 'border-[var(--color-warning-500)]/20',
    label: 'Medium',
    icon: 'minus',
  },
  Low: {
    color: 'text-[var(--color-neutral-600)] dark:text-[var(--color-neutral-400)]',
    bgColor: 'bg-[var(--color-neutral-500)]/10',
    borderColor: 'border-[var(--color-neutral-500)]/20',
    label: 'Low',
    icon: 'arrow-down',
  },
};

/**
 * Linked item type icons
 */
export const LINKED_ITEM_ICONS: Record<LinkedItemType, string> = {
  conversation: 'message-circle',
  requirement: 'clipboard-list',
  spec: 'file-text',
  artifact: 'package',
};

// ============================================
// Utility Functions
// ============================================

export function getStatusLabel(status: TaskStatus): string {
  return TASK_STATUS_STYLES[status]?.label ?? status;
}

export function getPriorityLabel(priority: TaskPriority): string {
  return TASK_PRIORITY_STYLES[priority]?.label ?? priority;
}

export function calculateProgress(actual: number, estimated: number): number {
  if (estimated <= 0) return 0;
  return Math.min(Math.round((actual / estimated) * 100), 100);
}

export function getRemainingHours(actual: number, estimated: number): number {
  return Math.max(estimated - actual, 0);
}

export function isOverdue(actual: number, estimated: number): boolean {
  return actual > estimated;
}
