/**
 * Project Types
 * API UI status
 */

import { ENTITY_SVG_ICONS } from '@sddp/shell/core';
import type { TabConfig } from '@sddp/shell/types';

export interface Project {
  id: string;
  tenantId: string;
  code: string;
  name: string;
  description?: string;
  ownerId: string;
  ownerName: string;
  status: 'planning' | 'active' | 'concluded' | 'archived';
  createdAt: string;
  updatedAt: string;
}

export interface UpdateProjectParams {
  name: string;
  description?: string;
}

export type ProjectStatus = 'planning' | 'active' | 'concluded' | 'archived';

export const PROJECT_STATUS_CONFIG: Record<ProjectStatus, { label: string; color: string; icon: string; description: string }> = {
  planning: { label: 'Planning', color: 'text-[var(--color-info-500)]', icon: 'edit', description: 'Project is being set up' },
  active: { label: 'Active', color: 'text-[var(--color-success-500)]', icon: 'play', description: 'Project is in active development' },
  concluded: { label: 'Concluded', color: 'text-[var(--color-warning-500)]', icon: 'pause', description: 'Project development is concluded' },
  archived: { label: 'Archived', color: 'text-[var(--color-neutral-500)]', icon: 'archive', description: 'Project is archived (read-only)' },
};

export interface ProjectDataResetResult {
  projectId: string;
  snapshotId: string;
  totalRowsDeleted: number;
  deletedTableCounts: Record<string, number>;
}

export interface TenantDataResetResult {
  tenantId: string;
  projectsReset: number;
  snapshotsCreated: number;
  totalRowsDeleted: number;
}

export interface ProjectStatistics {
  conversations: { total: number; secondary: number };
  requirements: { total: number; secondary: number };
  specs: { total: number; secondary: number };
  artifacts: { total: number; secondary: number };
  tasks: { total: number; secondary: number };
  glossary: { total: number; secondary: number };
  effort: { total: number; secondary: number };
}

export interface ProjectMember {
  userId: string;
  personId: string;
  displayName: string;
  role: string;
  avatarUrl?: string;
  lastActivityAt?: string;
  isOnline: boolean;
}

export interface ProjectDetail extends Project {
  statistics: ProjectStatistics;
  members: ProjectMember[];
}

export interface ProjectWithBadges extends Project {
  badges: {
    conversations: number;
    requirements: number;
    specs: number;
    tasks: number;
  };
}

export type ProjectPageType = 'dashboard' | 'conversations' | 'requirements' | 'specs' | 'tasks' | 'glossary' | 'artifacts' | 'effort' | 'traceability';

export interface ProjectTreeNode {
  id: string;
  type: ProjectPageType;
  label: string;
  icon: string;
  badge?: number;
  path?: string;
  tabConfig?: TabConfig;
}

export interface ProjectSidebarBadges {
  conversations: number;
  requirements: number;
  specs: number;
  tasks: number;
}

export interface ProjectsState {
  projects: Project[];
  projectsLoading: boolean;
  projectsError: string | null;
  currentProject: ProjectDetail | null;
  currentProjectLoading: boolean;
  currentProjectError: string | null;
  expandedProjects: Set<string>;
  selectedNodePath: string | null;
  searchQuery: string;
}

export const PROJECT_TREE_NODES: ProjectTreeNode[] = [
  { id: 'dashboard', type: 'dashboard', label: 'Dashboard', icon: ENTITY_SVG_ICONS.dashboard },
  { id: 'conversations', type: 'conversations', label: 'Conversations', icon: ENTITY_SVG_ICONS.conversations },
  { id: 'requirements', type: 'requirements', label: 'Requirements', icon: ENTITY_SVG_ICONS.requirements },
  { id: 'specs', type: 'specs', label: 'Specs', icon: ENTITY_SVG_ICONS.specs },
  { id: 'glossary', type: 'glossary', label: 'Glossary', icon: ENTITY_SVG_ICONS.glossary },
  { id: 'artifacts', type: 'artifacts', label: 'Artifacts', icon: ENTITY_SVG_ICONS.artifacts },
  { id: 'tasks', type: 'tasks', label: 'Tasks', icon: ENTITY_SVG_ICONS.tasks },
  { id: 'effort', type: 'effort', label: 'Effort', icon: ENTITY_SVG_ICONS.effort },
  { id: 'traceability', type: 'traceability', label: 'Traceability', icon: 'type-hierarchy' },
];

export const PROJECT_PAGE_CONFIG: Record<ProjectPageType, { label: string; icon: string }> = {
  dashboard: { label: 'Dashboard', icon: ENTITY_SVG_ICONS.dashboard },
  conversations: { label: 'Conversations', icon: ENTITY_SVG_ICONS.conversations },
  requirements: { label: 'Requirements', icon: ENTITY_SVG_ICONS.requirements },
  specs: { label: 'Specs', icon: ENTITY_SVG_ICONS.specs },
  tasks: { label: 'Tasks', icon: ENTITY_SVG_ICONS.tasks },
  glossary: { label: 'Glossary', icon: ENTITY_SVG_ICONS.glossary },
  artifacts: { label: 'Artifacts', icon: ENTITY_SVG_ICONS.artifacts },
  effort: { label: 'Effort', icon: ENTITY_SVG_ICONS.effort },
  traceability: { label: 'Traceability', icon: 'type-hierarchy' },
};

/**
 * Generate tree nodes for a project's sidebar navigation
 */
export function generateProjectTreeNodes(
  project: Project,
  badges?: ProjectSidebarBadges,
): ProjectTreeNode[] {
  return PROJECT_TREE_NODES.map((node) => ({
    ...node,
    id: `${project.id}/${node.id}`,
    path: `/${project.id}/${node.type}`,
    badge: badges ? (badges as unknown as Record<string, number>)[node.type] : undefined,
  }));
}

/**
 * Get badge counts for a project (returns zeros if not loaded)
 */
export function getProjectBadges(_projectId: string): ProjectSidebarBadges {
  return { conversations: 0, requirements: 0, specs: 0, tasks: 0 };
}

// ============================================
// Ownership Types (Treemap)
// ============================================

export interface OwnershipItem {
  entityType: string;
  entityId: string;
  entityName: string;
  ownerUserId: string | null;
  ownerName: string | null;
}

export interface ProjectOwnership {
  items: OwnershipItem[];
}

// ============================================
// Timeline Types
// ============================================

export type TimelineEventType = 'created' | 'updated' | 'approved' | 'rejected' | 'commented' | 'locked' | 'signed-off' | 'create' | 'update' | 'delete' | 'comment' | 'approve' | 'reject' | 'transition';

export type TimelineEntityType = 'spec' | 'requirement' | 'conversation' | 'glossary' | 'task' | 'artifact';

export interface TimelineEvent {
  id: string;
  projectId?: string;
  type: TimelineEventType;
  entityType: TimelineEntityType;
  entityId: string;
  entityName?: string;
  entityTitle?: string;
  actorId: string;
  actorName: string;
  timestamp: string;
  description?: string;
  metadata?: Record<string, unknown>;
}

export interface TimelineFilter {
  period: 'minutes-10' | 'hour-1' | 'today' | 'week' | 'all';
  entityType?: TimelineEntityType;
  entityTypes?: TimelineEntityType[];
  eventType?: TimelineEventType;
  types?: TimelineEventType[];
  actorIds?: string[];
}

export interface TimelineState {
  events: TimelineEvent[];
  loading: boolean;
  error: string | null;
  filter: TimelineFilter;
  expanded: boolean;
}

export const TIMELINE_EVENT_TYPE_STYLES: Record<
  TimelineEventType,
  { color: string; icon: string; label: string }
> = {
  created: { color: 'text-[var(--color-success-500)]', icon: 'plus-circle', label: 'created' },
  updated: { color: 'text-[var(--color-info-500)]', icon: 'edit', label: 'updated' },
  approved: { color: 'text-[var(--color-teal-500)]', icon: 'check-circle', label: 'approved' },
  rejected: { color: 'text-[var(--color-error-500)]', icon: 'x-circle', label: 'rejected' },
  commented: { color: 'text-[var(--color-warning-500)]', icon: 'message-circle', label: 'commented on' },
  locked: { color: 'text-[var(--color-purple-500)]', icon: 'lock', label: 'locked' },
  'signed-off': { color: 'text-[var(--color-cyan-500)]', icon: 'file-check', label: 'signed off' },
  create: { color: 'text-[var(--color-success-500)]', icon: 'plus-circle', label: 'created' },
  update: { color: 'text-[var(--color-info-500)]', icon: 'edit', label: 'updated' },
  delete: { color: 'text-[var(--color-error-500)]', icon: 'trash-2', label: 'deleted' },
  comment: { color: 'text-[var(--color-warning-500)]', icon: 'message-circle', label: 'commented on' },
  approve: { color: 'text-[var(--color-teal-500)]', icon: 'check-circle', label: 'approved' },
  reject: { color: 'text-[var(--color-error-500)]', icon: 'x-circle', label: 'rejected' },
  transition: { color: 'text-[var(--color-purple-500)]', icon: 'arrow-right', label: 'transitioned' },
};

export const ENTITY_TYPE_ICONS: Record<TimelineEntityType, string> = {
  spec: 'file-code',
  requirement: 'file-text',
  conversation: 'message-square',
  glossary: 'book-open',
  task: 'check-square',
  artifact: 'package',
};

