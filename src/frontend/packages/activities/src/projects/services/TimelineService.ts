/**
 * TimelineService - API Client for Timeline (Audit Log)
 * Fetches timeline events from the backend TimelineController
 */

import { fetchWithAuth } from '../../shared/api';

// ============================================
// Types (matching backend AuditLogEntry DTO)
// ============================================

export interface AuditLogEntry {
  id: string;
  actorId: string | null;
  action: string;
  resourceType: string;
  resourceId: string;
  payload: string | null;
  createdAt: string;
}

export interface AuditLogDto {
  id: string;
  timestamp: string;
  userId: string;
  userName: string;
  action: string;
  resourceType: string;
  resourceId: string;
  ipAddress: string;
  details: Record<string, unknown> | null;
}

// ============================================
// API Functions
// ============================================

/**
 * Get timeline events by resource
 */
export async function getTimelineByResource(
  tenantId: string,
  resourceType: string,
  resourceId: string,
  projectId?: string
): Promise<AuditLogEntry[]> {
  const endpoint = `/timeline/by-resource/${encodeURIComponent(resourceType)}/${encodeURIComponent(resourceId)}`;
  return fetchWithAuth<AuditLogEntry[]>(endpoint, { tenantId, projectId });
}

/**
 * Get project-scoped timeline (all users, all actions)
 * @param actorId - optional actor filter (for Recent Activity)
 * @param startDate - optional start date ISO string (for Recent Activity)
 */
export async function getTimelineByProject(
  tenantId: string,
  projectId?: string,
  limit: number = 1000,
  actorId?: string,
  startDate?: string
): Promise<AuditLogDto[]> {
  const params = new URLSearchParams({ limit: String(limit) });
  if (actorId) params.set('actorId', actorId);
  if (startDate) params.set('startDate', startDate);
  const endpoint = `/timeline/by-project?${params.toString()}`;
  return fetchWithAuth<AuditLogDto[]>(endpoint, { tenantId, projectId });
}

/**
 * Get timeline events by actor (user)
 */
export async function getTimelineByActor(
  tenantId: string,
  actorId: string,
  projectId?: string
): Promise<AuditLogEntry[]> {
  const endpoint = `/timeline/by-actor/${encodeURIComponent(actorId)}`;
  return fetchWithAuth<AuditLogEntry[]>(endpoint, { tenantId, projectId });
}

// ============================================
// Service Class (Singleton)
// ============================================

export class TimelineService {
  private tenantId = '';
  private projectId?: string;

  setContext(tenantId: string, projectId?: string): void {
    this.tenantId = tenantId;
    this.projectId = projectId;
  }

  getByResource(resourceType: string, resourceId: string): Promise<AuditLogEntry[]> {
    return getTimelineByResource(this.tenantId, resourceType, resourceId, this.projectId);
  }

  getByActor(actorId: string): Promise<AuditLogEntry[]> {
    return getTimelineByActor(this.tenantId, actorId, this.projectId);
  }

  getByProject(limit?: number, actorId?: string, startDate?: string): Promise<AuditLogDto[]> {
    return getTimelineByProject(this.tenantId, this.projectId, limit, actorId, startDate);
  }
}

// ============================================
// Singleton Management
// ============================================

let instance: TimelineService | null = null;

export function getTimelineService(): TimelineService {
  if (!instance) {
    instance = new TimelineService();
  }
  return instance;
}

export function resetTimelineService(): void {
  instance = null;
}
