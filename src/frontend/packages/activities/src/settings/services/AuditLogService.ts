/**
 * AuditLogService - API Client for Audit Log Management
 * Handles audit log listing and detail retrieval for admin settings
 */

import { fetchWithAuth } from '../../shared/api';
import type { AuditLog } from '../types';

// ============================================
// Types
// ============================================

export interface GetAuditLogsParams {
  userId?: string;
  action?: string;
  resourceType?: string;
  startDate?: string;
  endDate?: string;
  page?: number;
  size?: number;
}

export interface PagedAuditLogs {
  items: AuditLog[];
  totalCount: number;
}

// ============================================
// Audit Log API Functions
// ============================================

/**
 * Get list of audit logs with filtering
 */
export async function getAuditLogs(
  tenantId: string,
  params?: GetAuditLogsParams
): Promise<PagedAuditLogs> {
  const searchParams = new URLSearchParams();
  if (params?.userId) searchParams.set('userId', params.userId);
  if (params?.action) searchParams.set('action', params.action);
  if (params?.resourceType) searchParams.set('resourceType', params.resourceType);
  if (params?.startDate) searchParams.set('startDate', params.startDate);
  if (params?.endDate) searchParams.set('endDate', params.endDate);
  if (params?.page) searchParams.set('pageNumber', params.page.toString());
  if (params?.size) searchParams.set('pageSize', params.size.toString());

  const queryString = searchParams.toString();
  const endpoint = `/audit-logs${queryString ? `?${queryString}` : ''}`;

  const result = await fetchWithAuth<{ items: AuditLog[]; totalCount: number }>(endpoint, {
    tenantId,
  });
  return { items: result.items ?? [], totalCount: result.totalCount ?? 0 };
}

/**
 * Get audit log by ID
 */
export async function getAuditLogById(
  tenantId: string,
  logId: string
): Promise<AuditLog> {
  return fetchWithAuth<AuditLog>(`/audit-logs/${logId}`, { tenantId });
}

// ============================================
// Singleton Service Class
// ============================================

export class AuditLogService {
  private tenantId = '';

  setContext(tenantId: string): void {
    this.tenantId = tenantId;
  }

  getAuditLogs(params?: GetAuditLogsParams): Promise<PagedAuditLogs> {
    return getAuditLogs(this.tenantId, params);
  }

  getAuditLogById(logId: string): Promise<AuditLog> {
    return getAuditLogById(this.tenantId, logId);
  }
}

// ============================================
// Singleton Management
// ============================================

let instance: AuditLogService | null = null;

export function getAuditLogService(): AuditLogService {
  if (!instance) {
    instance = new AuditLogService();
  }
  return instance;
}

export function resetAuditLogService(): void {
  instance = null;
}
