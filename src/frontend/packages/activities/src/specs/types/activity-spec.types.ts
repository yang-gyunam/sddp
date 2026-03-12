/**
 * Activity-specific Spec Types
 * Extends spec types for Activity UI
 */

// Re-export from local spec types
export type {
  Spec,
  SpecDetail,
  SpecPage,
  SpecStatus,
  SpecState,
  SignOff,
  SignOffSummary,
  SignOffDecision,
  RoleType,
  CreateSpecRequest,
  UpdateSpecRequest,
} from './spec.types';

export {
  SPEC_STATUS_STYLES,
  SIGN_OFF_DECISION_STYLES,
  ROLE_TYPE_STYLES,
  canTransitionTo,
  isEditable,
  canCreateNewVersion,
  getAvailableActions,
} from './spec.types';

// ============================================
// Activity-specific Types
// ============================================

/**
 * Project-grouped specs for sidebar
 */
export interface ProjectSpecGroup {
  projectId: string;
  projectName: string;
  projectCode: string;
  specs: SpecSummary[];
  totalCount: number;
  expanded: boolean;
}

/**
 * Spec summary for sidebar list
 */
export interface SpecSummary {
  id: string;
  code: string;
  title: string;
  status: 'Draft' | 'InReview' | 'Approved' | 'Locked';
  version: string;
  linkedRequirementCode?: string;
  signOffProgress?: number; // 0-100
}

/**
 * Sidebar filter type
 */
export type SpecFilterType = 'all' | 'draft' | 'inReview' | 'approved' | 'locked';

/**
 * Sidebar state
 */
export interface SpecSidebarState {
  searchQuery: string;
  filterType: SpecFilterType;
  expandedProjects: Set<string>;
  selectedSpecId: string | null;
  projectGroups: ProjectSpecGroup[];
}
