/**
 * Effort Management Types
 * Types for project effort tracking and worklog management
 */

/**
 * Effort allocation for a user on a specific date
 */
export interface EffortAllocation {
  id: string;
  userId: string;
  projectId: string;
  date: string; // YYYY-MM-DD format
  allocatedHours: number; // 0-24
  createdAt: string;
  updatedAt: string;
  createdBy: string;
  updatedBy: string;
}

/**
 * Create/Update DTO for effort allocation
 */
export interface EffortAllocationDto {
  userId: string;
  projectId: string;
  date: string;
  allocatedHours: number;
}

/**
 * Worklog entry - actual time spent by a user
 */
export interface Worklog {
  id: string;
  userId: string;
  projectId: string;
  date: string; // YYYY-MM-DD format
  spentHours: number; // 0-24
  note?: string;
  taskId?: string; // Optional link to a task
  createdAt: string;
  updatedAt: string;
}

/**
 * Create/Update DTO for worklog
 */
export interface WorklogDto {
  userId: string;
  projectId: string;
  date: string;
  spentHours: number;
  note?: string;
  taskId?: string;
}

/**
 * Working day configuration for a project
 */
export interface WorkingDay {
  id: string;
  projectId: string;
  date: string; // YYYY-MM-DD format
  type: WorkingDayType;
  note?: string;
  createdAt: string;
  updatedAt: string;
}

export type WorkingDayType = 'workday' | 'offday' | 'holiday' | 'exception';

/**
 * Create/Update DTO for working day
 */
export interface WorkingDayDto {
  projectId: string;
  date: string;
  type: WorkingDayType;
  note?: string;
}

/**
 * Project member with effort summary
 */
export interface MemberEffortSummary {
  userId: string;
  userName: string;
  userEmail: string;
  avatarUrl?: string;
  role: string;
  totalAllocated: number; // Total allocated hours for the period
  totalSpent: number; // Total spent hours for the period
  remaining: number; // totalAllocated - totalSpent
  utilizationRate: number; // (totalSpent / totalAllocated) * 100
  requirementsCreated: number;
  specsCreated: number;
  glossaryTermsCreated: number;
  artifactsCreated: number;
}

/**
 * Ownership filter options
 */
export type OwnershipFilter = 'all' | 'requirements' | 'specs' | 'glossary' | 'artifacts';

export type OwnershipItemType = 'requirements' | 'specs' | 'glossary' | 'artifacts';

/**
 * Owned deliverables for a member (paged)
 */
export interface MemberOwnershipPage {
  userId: string;
  filter: OwnershipFilter;
  query?: string | null;
  page: number;
  pageSize: number;
  totalCount: number;
  counts: MemberOwnershipCounts;
  items: MemberOwnershipItem[];
}

export interface MemberOwnershipCounts {
  requirements: number;
  specs: number;
  glossaryTerms: number;
  artifacts: number;
  total: number;
}

export interface MemberOwnershipItem {
  id: string;
  type: OwnershipItemType;
  title: string;
  subtitle?: string | null;
  artifactPath?: string | null;
  specId?: string | null;
  specCode?: string | null;
  updatedAt: string;
}

/**
 * Daily effort data for a member
 */
export interface DailyEffort {
  date: string;
  allocatedHours: number;
  spentHours: number;
  isWorkingDay: boolean;
  workingDayType: WorkingDayType;
  worklogs: Worklog[];
  hasConflict: boolean; // True if allocated to multiple projects
  conflictingProjects?: string[];
}

/**
 * Week view data structure
 */
export interface WeekData {
  weekStart: string; // Monday date
  weekEnd: string; // Sunday date
  days: DailyEffort[];
  totalAllocated: number;
  totalSpent: number;
}

/**
 * Conflict information when a user is allocated to multiple projects
 */
export interface AllocationConflict {
  userId: string;
  userName: string;
  date: string;
  totalAllocated: number; // Total hours across all projects
  projects: {
    projectId: string;
    projectName: string;
    allocatedHours: number;
  }[];
}

/**
 * Effort page state
 */
export interface EffortState {
  projectId: string;
  selectedMemberId: string | null;
  dateRange: {
    start: string;
    end: string;
  };
  viewMode: 'week' | 'month';
  members: MemberEffortSummary[];
  ownership: MemberOwnershipPage | null;
  ownershipLoading: boolean;
  ownershipError: string | null;
  memberDailyEffort: DailyEffort[] | null;
  weekData: WeekData | null;
  conflicts: AllocationConflict[];
  workingDays: WorkingDay[];
  isLoading: boolean;
  error: string | null;
}

/**
 * Effort filter options
 */
export interface EffortFilter {
  projectId: string;
  startDate: string;
  endDate: string;
  userIds?: string[];
}

/**
 * Heatmap cell data for activity visualization
 */
export interface HeatmapCell {
  date: string;
  allocated: number;
  spent: number;
  utilization: number; // 0-100
}

/**
 * Heatmap week data (5 weekdays)
 */
export interface HeatmapWeek {
  weekStart: string;
  days: (HeatmapCell | null)[]; // 5 days (Mon-Fri), null for future/missing
}
