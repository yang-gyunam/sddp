/**
 * Effort Service
 * API operations for effort allocation, worklog, and working day management
 */

import { fetchWithAuth } from '../../shared/api/fetchWithAuth';
import type {
  EffortAllocation,
  EffortAllocationDto,
  Worklog,
  WorklogDto,
  WorkingDay,
  WorkingDayDto,
  MemberEffortSummary,
  MemberOwnershipPage,
  OwnershipFilter,
  DailyEffort,
  AllocationConflict,
} from '../types';

// ============================================
// Effort Allocation API
// ============================================

/**
 * Get effort allocations for a project within a date range
 */
export async function getAllocations(
  projectId: string,
  startDate: string,
  endDate: string,
  userIds?: string[]
): Promise<EffortAllocation[]> {
  const params = new URLSearchParams({
    startDate,
    endDate,
  });
  if (userIds?.length) {
    params.append('userIds', userIds.join(','));
  }

  return fetchWithAuth<EffortAllocation[]>(
    `/projects/${projectId}/effort/allocations?${params.toString()}`,
    { projectId }
  );
}

/**
 * Create or update effort allocation
 */
export async function upsertAllocation(
  projectId: string,
  dto: EffortAllocationDto
): Promise<EffortAllocation> {
  return fetchWithAuth<EffortAllocation>(
    `/projects/${projectId}/effort/allocations`,
    {
      method: 'POST',
      body: dto,
      projectId,
    }
  );
}

/**
 * Bulk update effort allocations
 */
export async function bulkUpsertAllocations(
  projectId: string,
  allocations: EffortAllocationDto[]
): Promise<EffortAllocation[]> {
  return fetchWithAuth<EffortAllocation[]>(
    `/projects/${projectId}/effort/allocations/bulk`,
    {
      method: 'POST',
      body: { allocations },
      projectId,
    }
  );
}

/**
 * Delete effort allocation
 */
export async function deleteAllocation(
  projectId: string,
  allocationId: string
): Promise<void> {
  return fetchWithAuth<void>(
    `/projects/${projectId}/effort/allocations/${allocationId}`,
    {
      method: 'DELETE',
      projectId,
    }
  );
}

// ============================================
// Worklog API
// ============================================

/**
 * Get worklogs for a project within a date range
 */
export async function getWorklogs(
  projectId: string,
  startDate: string,
  endDate: string,
  userIds?: string[]
): Promise<Worklog[]> {
  const params = new URLSearchParams({
    startDate,
    endDate,
  });
  if (userIds?.length) {
    params.append('userIds', userIds.join(','));
  }

  return fetchWithAuth<Worklog[]>(
    `/projects/${projectId}/effort/worklogs?${params.toString()}`,
    { projectId }
  );
}

/**
 * Create worklog entry
 */
export async function createWorklog(
  projectId: string,
  dto: WorklogDto
): Promise<Worklog> {
  return fetchWithAuth<Worklog>(
    `/projects/${projectId}/effort/worklogs`,
    {
      method: 'POST',
      body: dto,
      projectId,
    }
  );
}

/**
 * Update worklog entry
 */
export async function updateWorklog(
  projectId: string,
  worklogId: string,
  dto: Partial<WorklogDto>
): Promise<Worklog> {
  return fetchWithAuth<Worklog>(
    `/projects/${projectId}/effort/worklogs/${worklogId}`,
    {
      method: 'PUT',
      body: dto,
      projectId,
    }
  );
}

/**
 * Delete worklog entry
 */
export async function deleteWorklog(
  projectId: string,
  worklogId: string
): Promise<void> {
  return fetchWithAuth<void>(
    `/projects/${projectId}/effort/worklogs/${worklogId}`,
    {
      method: 'DELETE',
      projectId,
    }
  );
}

// ============================================
// Working Day API
// ============================================

/**
 * Get working day settings for a project
 */
export async function getWorkingDays(
  projectId: string,
  startDate: string,
  endDate: string
): Promise<WorkingDay[]> {
  const params = new URLSearchParams({
    startDate,
    endDate,
  });

  return fetchWithAuth<WorkingDay[]>(
    `/projects/${projectId}/effort/working-days?${params.toString()}`,
    { projectId }
  );
}

/**
 * Set working day configuration
 */
export async function setWorkingDay(
  projectId: string,
  dto: WorkingDayDto
): Promise<WorkingDay> {
  return fetchWithAuth<WorkingDay>(
    `/projects/${projectId}/effort/working-days`,
    {
      method: 'POST',
      body: dto,
      projectId,
    }
  );
}

/**
 * Bulk set working days (e.g., set all weekends as offdays)
 */
export async function bulkSetWorkingDays(
  projectId: string,
  workingDays: WorkingDayDto[]
): Promise<WorkingDay[]> {
  return fetchWithAuth<WorkingDay[]>(
    `/projects/${projectId}/effort/working-days/bulk`,
    {
      method: 'POST',
      body: { workingDays },
      projectId,
    }
  );
}

// ============================================
// Summary & Aggregation API
// ============================================

/**
 * Get effort summary for all project members
 */
export async function getMembersSummary(
  projectId: string,
  startDate: string,
  endDate: string
): Promise<MemberEffortSummary[]> {
  const params = new URLSearchParams({
    startDate,
    endDate,
  });

  return fetchWithAuth<MemberEffortSummary[]>(
    `/projects/${projectId}/effort/summary?${params.toString()}`,
    { projectId }
  );
}

/**
 * Get daily effort data for a specific member
 */
export async function getMemberDailyEffort(
  projectId: string,
  userId: string,
  startDate: string,
  endDate: string
): Promise<DailyEffort[]> {
  const params = new URLSearchParams({
    startDate,
    endDate,
  });

  return fetchWithAuth<DailyEffort[]>(
    `/projects/${projectId}/effort/users/${userId}/daily?${params.toString()}`,
    { projectId }
  );
}

/**
 * Get owned deliverables for a specific member
 */
export async function getMemberOwnership(
  projectId: string,
  userId: string,
  options?: {
    type?: OwnershipFilter;
    query?: string;
    page?: number;
    pageSize?: number;
  }
): Promise<MemberOwnershipPage> {
  const params = new URLSearchParams();
  if (options?.type && options.type !== 'all') {
    params.set('type', options.type);
  }
  if (options?.query) {
    params.set('q', options.query);
  }
  if (options?.page) {
    params.set('page', String(options.page));
  }
  if (options?.pageSize) {
    params.set('pageSize', String(options.pageSize));
  }

  const query = params.toString();
  const url = query
    ? `/projects/${projectId}/effort/users/${userId}/ownership?${query}`
    : `/projects/${projectId}/effort/users/${userId}/ownership`;

  return fetchWithAuth<MemberOwnershipPage>(url, { projectId });
}

/**
 * Get allocation conflicts (users allocated to multiple projects)
 */
export async function getConflicts(
  projectId: string,
  startDate: string,
  endDate: string
): Promise<AllocationConflict[]> {
  const params = new URLSearchParams({
    startDate,
    endDate,
  });

  return fetchWithAuth<AllocationConflict[]>(
    `/projects/${projectId}/effort/conflicts?${params.toString()}`,
    { projectId }
  );
}

// ============================================
// Utility Functions
// ============================================

/**
 * Calculate week boundaries (Monday to Sunday)
 */
export function getWeekBoundaries(date: Date = new Date()): { start: string; end: string } {
  const d = new Date(date);
  const day = d.getDay();
  const diff = d.getDate() - day + (day === 0 ? -6 : 1); // Adjust when day is Sunday

  const monday = new Date(d.setDate(diff));
  const sunday = new Date(d.setDate(d.getDate() + 6));

  return {
    start: formatDate(monday),
    end: formatDate(sunday),
  };
}

/**
 * Calculate month boundaries (first to last day)
 */
export function getMonthBoundaries(date: Date = new Date()): { start: string; end: string } {
  const d = new Date(date);
  const first = new Date(d.getFullYear(), d.getMonth(), 1);
  const last = new Date(d.getFullYear(), d.getMonth() + 1, 0);

  return {
    start: formatDate(first),
    end: formatDate(last),
  };
}

/**
 * Format date to YYYY-MM-DD (local timezone)
 * @see toLocalDateString in @sddp/shell for the shared utility
 */
export function formatDate(date: Date): string {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
}

/**
 * Parse YYYY-MM-DD string to Date
 */
export function parseDate(dateStr: string): Date {
  return new Date(dateStr + 'T00:00:00');
}

/**
 * Get array of dates between start and end (inclusive)
 */
export function getDateRange(startDate: string, endDate: string): string[] {
  const dates: string[] = [];
  const current = parseDate(startDate);
  const end = parseDate(endDate);

  while (current <= end) {
    dates.push(formatDate(current));
    current.setDate(current.getDate() + 1);
  }

  return dates;
}

/**
 * Check if a date is a weekend (Saturday or Sunday)
 */
export function isWeekend(date: Date | string): boolean {
  const d = typeof date === 'string' ? parseDate(date) : date;
  const day = d.getDay();
  return day === 0 || day === 6;
}

// ============================================
// Service Class (for dependency injection)
// ============================================

/**
 * EffortService class with project context
 */
export class EffortServiceClass {
  private projectId: string = '';

  setContext(projectId: string): void {
    this.projectId = projectId;
  }

  getAllocations(startDate: string, endDate: string, userIds?: string[]): Promise<EffortAllocation[]> {
    return getAllocations(this.projectId, startDate, endDate, userIds);
  }

  upsertAllocation(dto: EffortAllocationDto): Promise<EffortAllocation> {
    return upsertAllocation(this.projectId, dto);
  }

  bulkUpsertAllocations(allocations: EffortAllocationDto[]): Promise<EffortAllocation[]> {
    return bulkUpsertAllocations(this.projectId, allocations);
  }

  deleteAllocation(allocationId: string): Promise<void> {
    return deleteAllocation(this.projectId, allocationId);
  }

  getWorklogs(startDate: string, endDate: string, userIds?: string[]): Promise<Worklog[]> {
    return getWorklogs(this.projectId, startDate, endDate, userIds);
  }

  createWorklog(dto: WorklogDto): Promise<Worklog> {
    return createWorklog(this.projectId, dto);
  }

  updateWorklog(worklogId: string, dto: Partial<WorklogDto>): Promise<Worklog> {
    return updateWorklog(this.projectId, worklogId, dto);
  }

  deleteWorklog(worklogId: string): Promise<void> {
    return deleteWorklog(this.projectId, worklogId);
  }

  getWorkingDays(startDate: string, endDate: string): Promise<WorkingDay[]> {
    return getWorkingDays(this.projectId, startDate, endDate);
  }

  setWorkingDay(dto: WorkingDayDto): Promise<WorkingDay> {
    return setWorkingDay(this.projectId, dto);
  }

  bulkSetWorkingDays(workingDays: WorkingDayDto[]): Promise<WorkingDay[]> {
    return bulkSetWorkingDays(this.projectId, workingDays);
  }

  getMembersSummary(startDate: string, endDate: string): Promise<MemberEffortSummary[]> {
    return getMembersSummary(this.projectId, startDate, endDate);
  }

  getMemberDailyEffort(userId: string, startDate: string, endDate: string): Promise<DailyEffort[]> {
    return getMemberDailyEffort(this.projectId, userId, startDate, endDate);
  }

  getMemberOwnership(userId: string, options?: { type?: OwnershipFilter; query?: string; page?: number; pageSize?: number; }): Promise<MemberOwnershipPage> {
    return getMemberOwnership(this.projectId, userId, options);
  }

  getConflicts(startDate: string, endDate: string): Promise<AllocationConflict[]> {
    return getConflicts(this.projectId, startDate, endDate);
  }
}

// Singleton instance
let effortServiceInstance: EffortServiceClass | null = null;

/**
 * Get the singleton EffortService instance
 */
export function getEffortService(): EffortServiceClass {
  if (!effortServiceInstance) {
    effortServiceInstance = new EffortServiceClass();
  }
  return effortServiceInstance;
}

/**
 * Reset the singleton instance (for testing/logout)
 */
export function resetEffortService(): void {
  effortServiceInstance = null;
}

// Export all functions as a service object for convenience (backward compatibility)
export const EffortService = {
  // Allocations
  getAllocations,
  upsertAllocation,
  bulkUpsertAllocations,
  deleteAllocation,

  // Worklogs
  getWorklogs,
  createWorklog,
  updateWorklog,
  deleteWorklog,

  // Working Days
  getWorkingDays,
  setWorkingDay,
  bulkSetWorkingDays,

  // Summary
  getMembersSummary,
  getMemberDailyEffort,
  getMemberOwnership,
  getConflicts,

  // Utilities
  getWeekBoundaries,
  getMonthBoundaries,
  formatDate,
  parseDate,
  getDateRange,
  isWeekend,
};
