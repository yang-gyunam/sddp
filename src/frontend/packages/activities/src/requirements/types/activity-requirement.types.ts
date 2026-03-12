/**
 * Activity-specific Requirement Types
 * Extends requirement types for Activity UI
 */

// Re-export from local requirement types
export type {
  Requirement,
  RequirementDetail,
  RequirementPage,
  RequirementLevel,
  RequirementPriority,
  RequirementStatus,
  RequirementState,
  RequirementLevelStyle,
  RequirementPriorityStyle,
  RequirementStatusStyle,
  CreateRequirementRequest,
  UpdateRequirementRequest,
} from './requirement.types';

export {
  REQUIREMENT_LEVEL_STYLES,
  REQUIREMENT_PRIORITY_STYLES,
  REQUIREMENT_STATUS_STYLES,
  VALID_STATUS_TRANSITIONS,
  canTransitionTo,
  isEditable,
} from './requirement.types';

// ============================================
// Activity-specific Types
// ============================================

/**
 * Project-grouped requirements for sidebar
 */
export interface ProjectRequirementGroup {
  projectId: string;
  projectName: string;
  projectCode: string;
  requirements: RequirementSummary[];
  totalCount: number;
  expanded: boolean;
}

/**
 * Requirement summary for sidebar list
 */
export interface RequirementSummary {
  id: string;
  code: string;
  title: string;
  level: 'A' | 'B' | 'C';
  status: 'Draft' | 'InReview' | 'Approved' | 'Deprecated';
  childrenCount: number;
  hasChildren: boolean;
}

/**
 * Sidebar filter type
 */
export type RequirementFilterType = 'all' | 'draft' | 'inReview' | 'approved' | 'levelA' | 'levelB' | 'levelC';

/**
 * Sidebar view mode
 */
export type RequirementViewMode = 'tree' | 'flat';

/**
 * Sidebar state
 */
export interface RequirementSidebarState {
  searchQuery: string;
  filterType: RequirementFilterType;
  viewMode: RequirementViewMode;
  expandedProjects: Set<string>;
  expandedRequirements: Set<string>;
  selectedRequirementId: string | null;
  projectGroups: ProjectRequirementGroup[];
}

