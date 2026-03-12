/**
 * Timeline Actions
 * Mediates between Timeline store and TimelineService
 */

import { getTimelineService } from './services';
import type { AuditLogEntry, AuditLogDto } from './services';
import {
  setTimelineEvents,
  setTimelineLoading,
  setTimelineError,
} from './stores';
import type { TimelineEvent, TimelineEventType, TimelineEntityType } from './types';
import type { ActivityLogEntry } from '../dashboard/types';

// ============================================
// Action → TimelineEventType mapping
// ============================================

const ACTION_TO_EVENT_TYPE: Record<string, TimelineEventType> = {
  Created: 'create',
  Updated: 'update',
  Deleted: 'delete',
  Commented: 'comment',
  Approved: 'approve',
  Rejected: 'reject',
  Submitted: 'transition',
  Locked: 'transition',
  Unlocked: 'transition',
  Reviewed: 'transition',
  Assigned: 'update',
  Unassigned: 'update',
  // lowercase keys (DB)
  created: 'create',
  updated: 'update',
  deleted: 'delete',
  commented: 'comment',
  approved: 'approve',
  rejected: 'reject',
  submitted: 'transition',
  locked: 'transition',
  unlocked: 'transition',
  reviewed: 'transition',
  assigned: 'update',
  unassigned: 'update',
  create: 'create',
  update: 'update',
  delete: 'delete',
};

// ============================================
// ResourceType → EntityType mapping
// ============================================

const RESOURCE_TO_ENTITY_TYPE: Record<string, TimelineEntityType> = {
  Spec: 'spec',
  Conversation: 'conversation',
 Discussion: 'conversation', // Legacy: AuditLog 
  Requirement: 'requirement',
  GlossaryTerm: 'glossary',
  Artifact: 'artifact',
  Task: 'task',
  // lowercase keys (DB)
  spec: 'spec',
  conversation: 'conversation',
  discussion: 'conversation',
  requirement: 'requirement',
  glossaryTerm: 'glossary',
  glossary_term: 'glossary',
  artifact: 'artifact',
  task: 'task',
};

// ============================================
// Transform
// ============================================

function mapAuditLogToTimelineEvent(entry: AuditLogEntry, projectId: string): TimelineEvent {
  const eventType = ACTION_TO_EVENT_TYPE[entry.action] ?? 'update';
  const entityType = RESOURCE_TO_ENTITY_TYPE[entry.resourceType] ?? 'spec';

  // Parse payload for additional metadata
  let metadata: Record<string, unknown> | undefined;
  let actorName = 'System';
  let entityTitle = entry.resourceType;

  if (entry.payload) {
    try {
      const parsed = JSON.parse(entry.payload);
      metadata = parsed;
      if (parsed.actorName) actorName = parsed.actorName;
      if (parsed.entityTitle) entityTitle = parsed.entityTitle;
    } catch {
      // Ignore parse errors
    }
  }

  return {
    id: entry.id,
    projectId,
    type: eventType,
    entityType,
    entityId: entry.resourceId,
    entityTitle,
    actorId: entry.actorId ?? 'system',
    actorName,
    description: `${entry.action} ${entry.resourceType}`,
    timestamp: entry.createdAt,
    metadata,
  };
}

// ============================================
// Transform: AuditLogDto (with userName) → TimelineEvent
// ============================================

export function mapAuditLogDtoToTimelineEvent(dto: AuditLogDto, projectId: string): TimelineEvent {
  const eventType = ACTION_TO_EVENT_TYPE[dto.action] ?? 'update';
  const entityType = RESOURCE_TO_ENTITY_TYPE[dto.resourceType] ?? 'spec';

  let entityTitle = dto.resourceType;
  if (dto.details) {
    const title = dto.details['entityTitle'] ?? dto.details['title'] ?? dto.details['Title'];
    if (typeof title === 'string') entityTitle = title;
  }

  return {
    id: dto.id,
    projectId,
    type: eventType,
    entityType,
    entityId: dto.resourceId,
    entityTitle,
    actorId: dto.userId || 'system',
    actorName: dto.userName || 'System',
    description: `${dto.action} ${dto.resourceType}`,
    timestamp: dto.timestamp,
    metadata: dto.details ?? undefined,
  };
}

// ============================================
// Transform: AuditLogDto → ActivityLogEntry (for Recent Activity)
// ============================================

export function mapAuditLogDtoToActivityLogEntry(dto: AuditLogDto, projectName: string = ''): ActivityLogEntry {
  const entityType = RESOURCE_TO_ENTITY_TYPE[dto.resourceType] ?? 'spec';

  let entityTitle = dto.resourceType;
  if (dto.details) {
    const title = dto.details['entityTitle'] ?? dto.details['title'] ?? dto.details['Title'];
    if (typeof title === 'string') entityTitle = title;
  }

  return {
    id: dto.id,
    timestamp: dto.timestamp,
    userId: dto.userId,
    userName: dto.userName || 'System',
    action: dto.action,
    entityType,
    entityId: dto.resourceId,
    entityTitle,
    projectId: '',
    projectName,
  };
}

// ============================================
// Actions
// ============================================

/**
 * Load timeline events by project (all users, all actions)
 * tenantId only — shows all users' events across accessible projects
 */
export async function loadTimelineByProject(
  tenantId: string,
  projectId?: string,
  limit: number = 1000
): Promise<void> {
  setTimelineLoading(true);
  setTimelineError(null);

  try {
    const service = getTimelineService();
    service.setContext(tenantId, projectId);

    const entries = await service.getByProject(limit);
    const events = entries.map((e) => mapAuditLogDtoToTimelineEvent(e, projectId ?? ''));
    setTimelineEvents(events);
  } catch (error) {
    const message = error instanceof Error ? error.message : 'Failed to load timeline';
    console.error('Timeline API failed:', message);
    setTimelineEvents([]);
    setTimelineError(message);
  } finally {
    setTimelineLoading(false);
  }
}

/**
 * Load recent activity for the current user (today only)
 * Used by ProjectDashboardPage
 */
export async function loadRecentActivity(
  tenantId: string,
  actorId: string,
  projectId?: string
): Promise<AuditLogDto[]> {
  try {
    const service = getTimelineService();
    service.setContext(tenantId, projectId);

    const today = new Date();
    today.setHours(0, 0, 0, 0);
    const startDate = today.toISOString();

    return await service.getByProject(undefined, actorId, startDate);
  } catch (error) {
    console.error('Recent activity API failed:', error);
    return [];
  }
}

/**
 * Load timeline events by resource
 */
export async function loadTimelineByResource(
  tenantId: string,
  projectId: string,
  resourceType: string,
  resourceId: string
): Promise<void> {
  setTimelineLoading(true);
  setTimelineError(null);

  try {
    const service = getTimelineService();
    service.setContext(tenantId, projectId);

    const entries = await service.getByResource(resourceType, resourceId);
    const events = entries.map((e) => mapAuditLogToTimelineEvent(e, projectId));
    setTimelineEvents(events);
  } catch (error) {
    const message = error instanceof Error ? error.message : 'Failed to load timeline';
    console.error('Timeline API failed:', message);
    setTimelineEvents([]);
    setTimelineError(message);
  } finally {
    setTimelineLoading(false);
  }
}

/**
 * Load timeline events by actor (user)
 */
export async function loadTimelineByActor(
  tenantId: string,
  projectId: string,
  actorId: string
): Promise<void> {
  setTimelineLoading(true);
  setTimelineError(null);

  try {
    const service = getTimelineService();
    service.setContext(tenantId, projectId);

    const entries = await service.getByActor(actorId);
    const events = entries.map((e) => mapAuditLogToTimelineEvent(e, projectId));
    setTimelineEvents(events);
  } catch (error) {
    const message = error instanceof Error ? error.message : 'Failed to load timeline';
    console.error('Timeline API failed:', message);
    setTimelineEvents([]);
    setTimelineError(message);
  } finally {
    setTimelineLoading(false);
  }
}

/**
 * Refresh timeline (re-fetch from API)
 */
export async function refreshTimelineFromApi(
  tenantId: string,
  projectId?: string,
  actorId?: string
): Promise<void> {
  if (actorId && projectId) {
    return loadTimelineByActor(tenantId, projectId, actorId);
  }
  return loadTimelineByProject(tenantId, projectId);
}
