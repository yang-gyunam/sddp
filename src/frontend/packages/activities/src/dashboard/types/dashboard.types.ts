/**
 * Dashboard Types
 * Type definitions for Dashboard Activity
 */

// Dashboard View Type
export type DashboardView = 'my' | 'system' | 'project';

// Activity Log
export interface ActivityLogEntry {
  id: string;
  timestamp: string;
  userId: string;
  userName: string;
  action: string;
  entityType: string;
  entityId: string;
  entityTitle: string;
  projectId?: string;
  projectName?: string;
}

// Status Distribution
export interface StatusDistribution {
  [status: string]: number;
}

// ============================================================================
// My Dashboard
// ============================================================================

export interface MyDashboard {
  statistics: MyStatistics;
  myActivity: MyActivityData[];
  myProjects: MyProjectSummary[];
  recentActivities: ActivityLogEntry[];
  taskStatus: StatusDistribution;
  effortThisWeek: EffortData[];
  upcomingDeadlines: DeadlineItem[];
}

export interface MyStatistics {
  tasks: { total: number; toDo: number; inProgress?: number; done?: number };
  conversations: { total: number; active: number };
  specs: { total: number; inReview: number };
  requirements: { total: number; draft: number };
  glossary: { total: number; draft: number };
  artifacts: { total: number; recent: number };
  effort: { total: number; used: number };
}

export interface MyActivityData {
  date: string;
  tasksCompleted: number;
  specsCreated: number;
  comments: number;
}

export interface MyProjectSummary {
  projectId: string;
  projectName: string;
  myTasks: number;
  myConversations: number;
}

export interface EffortData {
  day: string;
  hours: number;
}

export interface DeadlineItem {
  entityType: 'task' | 'spec';
  entityId: string;
  title: string;
  projectName: string;
  dueDate: string;
  daysLeft: number;
}

// ============================================================================
// System Dashboard
// ============================================================================

export interface SystemDashboard {
  statistics: SystemStatistics;
  projectActivity: ActivityData[];
  topProjects: ProjectRanking[];
  recentActivities: ActivityLogEntry[];
  userDistribution: UserDistributionData[];
  specStatus: StatusDistribution;
  taskStatus: StatusDistribution;
}

export interface SystemStatistics {
  projects: { total: number; thisMonth: number };
  users: { total: number; thisMonth: number };
  specs: { total: number; thisWeek: number };
  tasks: { total: number; thisWeek: number };
  conversations: { total: number; thisWeek: number };
}

export interface ActivityData {
  date: string;
  specsCreated: number;
  tasksCompleted: number;
  conversations: number;
}

export interface ProjectRanking {
  projectId: string;
  projectName: string;
  activityCount: number;
}

export interface UserDistributionData {
  projectId: string;
  projectName: string;
  activeUsers: number;
}

// ============================================================================
// Project Dashboard
// ============================================================================

export interface ProjectDashboard {
  project: ProjectInfo;
  statistics: ProjectStatistics;
  progress: ProgressData[];
  teamMembers: TeamMemberActivity[];
  recentActivities: ActivityLogEntry[];
  requirementStatus: StatusDistribution;
  taskProgress: TaskProgressData;
  specQualityGates: SpecQualityGate[];
}

export interface ProjectInfo {
  id: string;
  name: string;
  ownerId: string;
  ownerName: string;
  status: 'Active' | 'Archived';
  memberCount: number;
  createdAt: string;
  lastActivityAt: string;
}

export interface ProjectStatistics {
  conversations: { total: number; active: number };
  requirements: { total: number; draft: number };
  specs: { total: number; inReview: number };
  tasks: { total: number; inProgress: number };
  artifacts: { total: number; recent: number };
}

export interface ProgressData {
  date: string;
  requirements: number;
  specs: number;
  tasks: number;
}

export interface TeamMemberActivity {
  userId: string;
  userName: string;
  role: string;
  activityCount: number;
}

export interface TaskProgressData {
  toDo: number;
  inProgress: number;
  done: number;
  total: number;
}

export interface SpecQualityGate {
  specId: string;
  specTitle: string;
  status: string;
  gateStatus: 'passed' | 'warning' | 'error';
  gateMessage: string;
  updatedAt: string;
}

// ============================================================================
// Dashboard Phase 1 Widget Types
// ============================================================================

export interface SpecHealthRadar {
  draft: number;
  inReview: number;
  approved: number;
  locked: number;
  total: number;
}

export interface MySignOffQueue {
  pendingCount: number;
  items: PendingSignOffItem[];
}

export interface PendingSignOffItem {
  signOffId: string;
  specId: string;
  specCode: string;
  specTitle: string;
  projectId: string;
  projectName: string;
  requestedAt: string;
  waitingDays: number;
  urgency: 'normal' | 'urgent' | 'overdue';
}

export interface ContributionHeatmap {
  days: DayContribution[];
  totalContributions: number;
}

export interface DayContribution {
  date: string;
  count: number;
  specsCreated: number;
  comments: number;
  signOffs: number;
  tasksCompleted: number;
}

export interface ProjectSpotlight {
  projectId: string | null;
  projectName: string | null;
  changeCount: number;
  teamActivityCount: number;
  teamMemberCount: number;
  recentChanges: SpotlightActivity[];
}

export interface SpotlightActivity {
  action: string;
  resourceType: string;
  timestamp: string;
}

export interface DueDateTimeline {
  tasks: TimelineTask[];
  overdueCount: number;
  upcomingCount: number;
}

export interface TimelineTask {
  taskId: string;
  title: string;
  status: string;
  priority: string;
  dueDate: string | null;
  projectName: string | null;
  daysFromToday: number;
  isOverdue: boolean;
}

export interface MyEffortTracker {
  totalHoursThisWeek: number;
  targetHoursPerWeek: number;
  dailyBreakdown: DailyEffortSummary[];
  taskDistribution: TaskEffortDistribution[];
}

export interface DailyEffortSummary {
  date: string;
  dayLabel: string;
  hours: number;
}

export interface TaskEffortDistribution {
  taskId: string;
  taskTitle: string;
  hours: number;
  percentage: number;
}

export interface DashboardWidgets {
  specHealth: SpecHealthRadar;
  signOffQueue: MySignOffQueue;
  contributionHeatmap: ContributionHeatmap;
  projectSpotlight: ProjectSpotlight | null;
  dueDateTimeline: DueDateTimeline;
  effortTracker: MyEffortTracker;
}

// ============================================================================
// Notification Types
// ============================================================================

export interface NotificationItem {
  id: string;
  type: string;
  title: string;
  message: string;
  isRead: boolean;
  entityType?: string;
  entityId?: string;
  actorName?: string;
  createdAt: string;
}

export interface NotificationsData {
  notifications: NotificationItem[];
  unreadCount: number;
}
