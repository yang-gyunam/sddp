/**
 * Dashboard Service
 * Handles dashboard API calls with fallback to mock data
 */

import { fetchWithAuth } from '../../shared/api';
import { toLocalDateString } from '@sddp/shell/utils';
import type {
  MyDashboard,
  MyStatistics,
  SystemDashboard,
  SystemStatistics,
  ProjectDashboard,
  ActivityLogEntry,
  DashboardWidgets,
  NotificationsData,
  NotificationItem,
} from '../types';

// ============================================
// API Response Types
// ============================================

interface DashboardStatDto {
  total: number;
  secondary: number;
}

interface MyDashboardDto {
  tasks: DashboardStatDto;
  conversations: DashboardStatDto;
  specs: DashboardStatDto;
  requirements: DashboardStatDto;
  glossary: DashboardStatDto;
  artifacts: DashboardStatDto;
  recentActivities: AuditLogEntryDto[];
}

// Split API DTOs
interface MyOverviewDto {
  tasks: DashboardStatDto;
  conversations: DashboardStatDto;
  specs: DashboardStatDto;
  requirements: DashboardStatDto;
  glossary: DashboardStatDto;
  artifacts: DashboardStatDto;
}

interface MyActivitiesDto {
  activities: AuditLogEntryDto[];
  page: number;
  pageSize: number;
  totalCount: number;
}

interface SystemDashboardDto {
  specs: DashboardStatDto;
  requirements: DashboardStatDto;
  conversations: DashboardStatDto;
  users: DashboardStatDto;
  recentActivities: AuditLogEntryDto[];
}

interface SystemStatsDto {
  totalProjects: number;
  totalUsers: number;
  totalSpecs: number;
  totalRequirements: number;
  totalConversations: number;
  specsThisWeek: number;
  requirementsThisWeek: number;
  conversationsThisWeek: number;
}

interface AuditLogsDto {
  logs: AuditLogEntryDto[];
  page: number;
  pageSize: number;
  totalCount: number;
}

interface HealthCheckDto {
  status: string;
  services: ServiceHealthDto[];
}

interface ServiceHealthDto {
  name: string;
  status: string;
  message: string | null;
  responseTimeMs: number | null;
}

interface AuditLogEntryDto {
  id: string;
  actorId: string | null;
  action: string;
  resourceType: string;
  resourceId: string;
  payload: string | null;
  createdAt: string;
}

// Project Dashboard API DTOs
interface ProjectDashboardDto {
  project: ProjectInfoDto;
  statistics: ProjectDashboardStatsDto;
  teamMembers: TeamMemberActivityDto[];
  recentActivities: AuditLogEntryDto[];
  taskProgress: TaskProgressDto;
}

interface ProjectInfoDto {
  id: string;
  name: string;
  ownerId: string;
  ownerName: string;
  status: string;
  memberCount: number;
  createdAt: string;
  lastActivityAt: string | null;
}

interface ProjectDashboardStatsDto {
  conversations: DashboardStatDto;
  requirements: DashboardStatDto;
  specs: DashboardStatDto;
  tasks: DashboardStatDto;
  artifacts: DashboardStatDto;
}

interface TeamMemberActivityDto {
  userId: string;
  userName: string;
  role: string;
  activityCount: number;
}

interface TaskProgressDto {
  toDo: number;
  inProgress: number;
  done: number;
  total: number;
}

// ============================================
// Helpers
// ============================================

function mapAuditToActivity(entry: AuditLogEntryDto): ActivityLogEntry {
  return {
    id: entry.id,
    timestamp: entry.createdAt,
    userId: entry.actorId ?? 'system',
    userName: 'User',
    action: entry.action.toLowerCase(),
    entityType: entry.resourceType.toLowerCase(),
    entityId: entry.resourceId,
    entityTitle: `${entry.resourceType} ${entry.action}`,
  };
}

function mapMyDashboard(dto: MyDashboardDto): MyDashboard {
  const activities = dto.recentActivities?.map((a) => mapAuditToActivity(a)) ?? [];
  const now = new Date();
  const dates = Array.from({ length: 30 }, (_, i) => {
    const date = new Date(now);
    date.setDate(date.getDate() - (29 - i));
    return toLocalDateString(date);
  });

  return {
    statistics: {
      tasks: { total: dto.tasks?.total ?? 0, toDo: dto.tasks?.secondary ?? 0 },
      conversations: { total: dto.conversations?.total ?? 0, active: dto.conversations?.secondary ?? 0 },
      specs: { total: dto.specs?.total ?? 0, inReview: dto.specs?.secondary ?? 0 },
      requirements: { total: dto.requirements?.total ?? 0, draft: dto.requirements?.secondary ?? 0 },
      glossary: { total: dto.glossary?.total ?? 0, draft: dto.glossary?.secondary ?? 0 },
      artifacts: { total: dto.artifacts?.total ?? 0, recent: dto.artifacts?.secondary ?? 0 },
      effort: { total: 40, used: 0 },
    },
    myActivity: dates.map((date) => ({
      date,
      tasksCompleted: 0,
      specsCreated: 0,
      comments: 0,
    })),
    myProjects: [],
    recentActivities: activities,
    taskStatus: { 'To Do': dto.tasks.secondary, 'In Progress': 0, Done: 0 },
    effortThisWeek: [
      { day: 'Mon', hours: 0 },
      { day: 'Tue', hours: 0 },
      { day: 'Wed', hours: 0 },
      { day: 'Thu', hours: 0 },
      { day: 'Fri', hours: 0 },
    ],
    upcomingDeadlines: [],
  };
}

function mapProjectDashboard(dto: ProjectDashboardDto): ProjectDashboard {
  const activities = dto.recentActivities?.map((a) => mapAuditToActivity(a)) ?? [];

  return {
    project: {
      id: dto.project.id,
      name: dto.project.name,
      ownerId: dto.project.ownerId,
      ownerName: dto.project.ownerName,
      status: dto.project.status === 'active' ? 'Active' : 'Archived',
      memberCount: dto.project.memberCount,
      createdAt: dto.project.createdAt,
      lastActivityAt: dto.project.lastActivityAt ?? dto.project.createdAt,
    },
    statistics: {
      conversations: { total: dto.statistics.conversations?.total ?? 0, active: dto.statistics.conversations?.secondary ?? 0 },
      requirements: { total: dto.statistics.requirements?.total ?? 0, draft: dto.statistics.requirements?.secondary ?? 0 },
      specs: { total: dto.statistics.specs?.total ?? 0, inReview: dto.statistics.specs?.secondary ?? 0 },
      tasks: { total: dto.statistics.tasks?.total ?? 0, inProgress: dto.statistics.tasks?.secondary ?? 0 },
      artifacts: { total: dto.statistics.artifacts?.total ?? 0, recent: dto.statistics.artifacts?.secondary ?? 0 },
    },
    progress: [],
    teamMembers: dto.teamMembers?.map((m) => ({
      userId: m.userId,
      userName: m.userName,
      role: m.role,
      activityCount: m.activityCount,
    })) ?? [],
    recentActivities: activities,
    requirementStatus: {},
    taskProgress: {
      toDo: dto.taskProgress?.toDo ?? 0,
      inProgress: dto.taskProgress?.inProgress ?? 0,
      done: dto.taskProgress?.done ?? 0,
      total: dto.taskProgress?.total ?? 0,
    },
    specQualityGates: [],
  };
}

function mapSystemDashboard(dto: SystemDashboardDto): SystemDashboard {
  const activities = dto.recentActivities?.map((a) => mapAuditToActivity(a)) ?? [];
  const now = new Date();
  const dates = Array.from({ length: 30 }, (_, i) => {
    const date = new Date(now);
    date.setDate(date.getDate() - (29 - i));
    return toLocalDateString(date);
  });

  return {
    statistics: {
      projects: { total: 0, thisMonth: 0 },
      users: { total: dto.users?.total ?? 0, thisMonth: dto.users?.secondary ?? 0 },
      specs: { total: dto.specs?.total ?? 0, thisWeek: dto.specs?.secondary ?? 0 },
      tasks: { total: 0, thisWeek: 0 },
      conversations: { total: dto.conversations?.total ?? 0, thisWeek: dto.conversations?.secondary ?? 0 },
    },
    projectActivity: dates.map((date) => ({
      date,
      specsCreated: 0,
      tasksCompleted: 0,
      conversations: 0,
    })),
    topProjects: [],
    recentActivities: activities,
    userDistribution: [],
    specStatus: {
      Draft: 0,
      InReview: 0,
      Approved: 0,
      Locked: 0,
    },
    taskStatus: {
      'To Do': 0,
      'In Progress': 0,
      Done: 0,
    },
  };
}

// ============================================
// Standalone API Functions
// ============================================

export async function getMyDashboard(tenantId?: string): Promise<MyDashboard> {
  const dto = await fetchWithAuth<MyDashboardDto>('/dashboard/my', tenantId ? { tenantId } : undefined);
  return mapMyDashboard(dto);
}

export async function getSystemDashboard(tenantId?: string): Promise<SystemDashboard> {
  const dto = await fetchWithAuth<SystemDashboardDto>('/dashboard/system', tenantId ? { tenantId } : undefined);
  return mapSystemDashboard(dto);
}

export async function getProjectDashboard(projectId: string, tenantId?: string): Promise<ProjectDashboard> {
  const dto = await fetchWithAuth<ProjectDashboardDto>(
    `/dashboard/projects/${projectId}`,
    tenantId ? { tenantId } : undefined
  );
  return mapProjectDashboard(dto);
}

export async function getActivities(params?: {
  projectId?: string;
  userId?: string;
  limit?: number;
  offset?: number;
}, tenantId?: string): Promise<ActivityLogEntry[]> {
  // Route to existing API based on params
  if (params?.userId) {
    const result = await getMyActivities(tenantId, 1, params.limit ?? 20);
    return result.activities;
  }
  const result = await getSystemAuditLogs(tenantId, 1, params?.limit ?? 50);
  return result.logs;
}

export async function refreshDashboard(
  view: 'my' | 'system' | 'project',
  projectId?: string,
  tenantId?: string
): Promise<void> {
  if (view === 'my') {
    await getMyDashboard(tenantId);
  } else if (view === 'system') {
    await getSystemDashboard(tenantId);
  } else if (view === 'project' && projectId) {
    await getProjectDashboard(projectId, tenantId);
  }
}

export async function getMyDashboardWidgets(tenantId?: string): Promise<DashboardWidgets> {
  const responseData = await fetchWithAuth<DashboardWidgets>('/dashboard/my/widgets', tenantId ? { tenantId } : undefined);
  const data = (responseData as unknown as { data?: DashboardWidgets }).data || responseData;
  return data;
}

export async function getMyOverview(tenantId?: string): Promise<MyStatistics> {
  const dto = await fetchWithAuth<MyOverviewDto>('/dashboard/my/overview', tenantId ? { tenantId } : undefined);
  return {
    tasks: { total: dto.tasks?.total ?? 0, toDo: dto.tasks?.secondary ?? 0 },
    conversations: { total: dto.conversations?.total ?? 0, active: dto.conversations?.secondary ?? 0 },
    specs: { total: dto.specs?.total ?? 0, inReview: dto.specs?.secondary ?? 0 },
    requirements: { total: dto.requirements?.total ?? 0, draft: dto.requirements?.secondary ?? 0 },
    glossary: { total: dto.glossary?.total ?? 0, draft: dto.glossary?.secondary ?? 0 },
    artifacts: { total: dto.artifacts?.total ?? 0, recent: dto.artifacts?.secondary ?? 0 },
    effort: { total: 40, used: 0 },
  };
}

export async function getMyActivities(
  tenantId?: string,
  page = 1,
  pageSize = 20
): Promise<{ activities: ActivityLogEntry[]; totalCount: number }> {
  const dto = await fetchWithAuth<MyActivitiesDto>(
    `/dashboard/my/activities?page=${page}&pageSize=${pageSize}`,
    tenantId ? { tenantId } : undefined
  );
  return {
    activities: dto.activities?.map((a) => mapAuditToActivity(a)) ?? [],
    totalCount: dto.totalCount ?? 0,
  };
}

export async function getSystemStats(tenantId?: string): Promise<SystemStatistics> {
  const dto = await fetchWithAuth<SystemStatsDto>('/dashboard/system/stats', tenantId ? { tenantId } : undefined);
  return {
    projects: { total: dto.totalProjects ?? 0, thisMonth: 0 },
    users: { total: dto.totalUsers ?? 0, thisMonth: 0 },
    specs: { total: dto.totalSpecs ?? 0, thisWeek: dto.specsThisWeek ?? 0 },
    tasks: { total: 0, thisWeek: 0 },
    conversations: { total: dto.totalConversations ?? 0, thisWeek: dto.conversationsThisWeek ?? 0 },
  };
}

export async function getSystemAuditLogs(
  tenantId?: string,
  page = 1,
  pageSize = 50
): Promise<{ logs: ActivityLogEntry[]; totalCount: number }> {
  const dto = await fetchWithAuth<AuditLogsDto>(
    `/dashboard/system/audit-logs?page=${page}&pageSize=${pageSize}`,
    tenantId ? { tenantId } : undefined
  );
  return {
    logs: dto.logs?.map((a) => mapAuditToActivity(a)) ?? [],
    totalCount: dto.totalCount ?? 0,
  };
}

export async function getSystemHealth(tenantId?: string): Promise<HealthCheckDto> {
  try {
    return await fetchWithAuth<HealthCheckDto>('/dashboard/system/health', tenantId ? { tenantId } : undefined);
  } catch (error) {
    console.warn('System Health API failed:', error);
    return {
      status: 'Unknown',
      services: [
        { name: 'API Server', status: 'Unknown', message: 'Could not check', responseTimeMs: null },
      ],
    };
  }
}

// ============================================
// Notification API Functions
// ============================================

interface MyNotificationsDto {
  notifications: NotificationItem[];
  unreadCount: number;
}

export async function getMyNotifications(
  tenantId?: string,
  page = 1,
  pageSize = 20
): Promise<NotificationsData> {
  const dto = await fetchWithAuth<MyNotificationsDto>(
    `/dashboard/my/notifications?page=${page}&pageSize=${pageSize}`,
    tenantId ? { tenantId } : undefined
  );
  return {
    notifications: dto.notifications ?? [],
    unreadCount: dto.unreadCount ?? 0,
  };
}

export async function markNotificationRead(
  id: string,
  tenantId?: string
): Promise<void> {
  await fetchWithAuth(`/dashboard/my/notifications/${id}/read`, {
    method: 'PATCH',
    ...(tenantId ? { tenantId } : {}),
  });
}

export async function markAllNotificationsRead(tenantId?: string): Promise<void> {
  await fetchWithAuth('/dashboard/my/notifications/read-all', {
    method: 'PATCH',
    ...(tenantId ? { tenantId } : {}),
  });
}

// ============================================
// Service Class (Singleton)
// ============================================

export class DashboardService {
  private tenantId = '';
  private projectId = '';

  setContext(tenantId: string, projectId?: string): void {
    this.tenantId = tenantId;
    this.projectId = projectId ?? '';
  }

  getMyDashboard(): Promise<MyDashboard> {
    return getMyDashboard(this.tenantId);
  }

  getSystemDashboard(): Promise<SystemDashboard> {
    return getSystemDashboard(this.tenantId);
  }

  getProjectDashboard(projectId: string = this.projectId): Promise<ProjectDashboard> {
    return getProjectDashboard(projectId, this.tenantId);
  }

  getActivities(params?: {
    projectId?: string;
    userId?: string;
    limit?: number;
    offset?: number;
  }): Promise<ActivityLogEntry[]> {
    return getActivities(params, this.tenantId);
  }

  refresh(view: 'my' | 'system' | 'project', projectId?: string): Promise<void> {
    return refreshDashboard(view, projectId ?? this.projectId, this.tenantId);
  }

  getMyOverview(): Promise<MyStatistics> {
    return getMyOverview(this.tenantId);
  }

  getMyDashboardWidgets(): Promise<DashboardWidgets> {
    return getMyDashboardWidgets(this.tenantId);
  }

  getMyActivities(page = 1, pageSize = 20): Promise<{ activities: ActivityLogEntry[]; totalCount: number }> {
    return getMyActivities(this.tenantId, page, pageSize);
  }

  getSystemStats(): Promise<SystemStatistics> {
    return getSystemStats(this.tenantId);
  }

  getSystemAuditLogs(
    page = 1,
    pageSize = 50
  ): Promise<{ logs: ActivityLogEntry[]; totalCount: number }> {
    return getSystemAuditLogs(this.tenantId, page, pageSize);
  }

  getSystemHealth(): Promise<HealthCheckDto> {
    return getSystemHealth(this.tenantId);
  }

  getMyNotifications(page = 1, pageSize = 20): Promise<NotificationsData> {
    return getMyNotifications(this.tenantId, page, pageSize);
  }

  markNotificationRead(id: string): Promise<void> {
    return markNotificationRead(id, this.tenantId);
  }

  markAllNotificationsRead(): Promise<void> {
    return markAllNotificationsRead(this.tenantId);
  }
}

// ============================================
// Singleton Management
// ============================================

let instance: DashboardService | null = null;

export function getDashboardService(): DashboardService {
  if (!instance) {
    instance = new DashboardService();
  }
  return instance;
}

export function resetDashboardService(): void {
  instance = null;
}
