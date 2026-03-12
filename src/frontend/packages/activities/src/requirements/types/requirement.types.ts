/**
 * Requirement Management Type Definitions
 * Based on REQ-03 requirements (2.2 Requirement Management)
 */

import type { UserRef } from '../../shared/types';

// ============================================
// Enums (matching backend)
// ============================================

export type RequirementLevel = 'A' | 'B' | 'C';

export type RequirementPriority = 'Low' | 'Medium' | 'High' | 'Urgent';

export type RequirementStatus = 'Draft' | 'InReview' | 'Approved' | 'Deprecated';

// ============================================
// DTOs (matching backend response)
// ============================================

/**
 * Requirement summary (list view)
 */
export interface Requirement {
  id: string;
  tenantId: string;
  projectId: string;
  code: string;
  title: string;
  description: string;
  level: RequirementLevel;
  priority: RequirementPriority;
  status: RequirementStatus;
  parentId: string | null;
  conversationId: string | null;
  bornFromConversationId?: string | null;
  version: string;
  childrenCount: number;
  acceptanceCriteria?: string;
  createdAt: string;
  updatedAt: string;
}

/**
 * Requirement detail (with children)
 */
export type ConversationType = 'Channel' | 'Forum' | 'DirectMessage';

export interface RequirementAncestor {
  id: string;
  code: string;
  title: string;
  level: RequirementLevel;
}

export interface LinkedSpec {
  id: string;
  code: string;
  title: string;
  status: string;
}

export interface RequirementDetail extends Requirement {
  parentCode: string | null;
  parentTitle: string | null;
  parentLevel: RequirementLevel | null;
  conversationName: string | null;
  conversationDescription: string | null;
  conversationType: ConversationType | null;
  ancestors: RequirementAncestor[];
  siblings: RequirementAncestor[];
  children: Requirement[];
  linkedSpecs: LinkedSpec[];
  owner: UserRef | null;
  createdBy: UserRef;
  updatedBy: UserRef;
  validFrom: string;
  validTo: string | null;
}

/**
 * Paginated requirements response
 */
export interface RequirementPage {
  items: Requirement[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

/**
 * Requirement tree node (recursive hierarchy)
 */
export interface RequirementTreeNode {
  id: string;
  code: string;
  title: string;
  level: RequirementLevel;
  priority: RequirementPriority;
  status: RequirementStatus;
  parentId: string | null;
  childrenCount: number;
  children: RequirementTreeNode[];
}

/**
 * Lightweight requirement result for search/autocomplete
 */
export interface RequirementSearchResult {
  id: string;
  code: string;
  title: string;
  level: RequirementLevel;
}

/**
 * Requirement version history item (all versions of a requirement)
 */
export interface RequirementVersion {
  id: string;
  code: string;
  title: string;
  description: string;
  level: RequirementLevel;
  priority: RequirementPriority;
  status: RequirementStatus;
  parentId: string | null;
  conversationId: string | null;
  version: string;
  owner: UserRef | null;
  createdBy: UserRef | null;
  updatedBy: UserRef | null;
  validFrom: string;
  validTo: string | null;
  createdAt: string;
  updatedAt: string;
}

// ============================================
// Request DTOs
// ============================================

export interface CreateRequirementRequest {
  code: string;
  title: string;
  description: string;
  priority?: RequirementPriority;
  parentId?: string;
  ownerUserId?: string;
  /** Linked conversation (handled via separate link API after creation) */
  conversationId?: string;
}

export interface UpdateRequirementRequest {
  title: string;
  description: string;
  priority?: RequirementPriority;
  parentId?: string;
  ownerUserId?: string;
  conversationId?: string;
}

export interface TransitionStatusRequest {
  newStatus: RequirementStatus;
}

export interface LinkConversationRequest {
  conversationId: string;
}

// ============================================
// Store State
// ============================================

export interface RequirementState {
  // List of requirements
  requirements: Requirement[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  requirementsLoading: boolean;
  requirementsError: string | null;

  // Filters
  levelFilter: RequirementLevel | null;
  statusFilter: RequirementStatus | null;

  // Current requirement
  currentRequirement: RequirementDetail | null;
  currentRequirementLoading: boolean;
  currentRequirementError: string | null;

  // Children
  children: Requirement[];
  childrenLoading: boolean;
  childrenError: string | null;
}

// ============================================
// UI Types
// ============================================

export interface RequirementLevelStyle {
  bgColor: string;
  borderColor: string;
  textColor: string;
  label: string;
}

export interface RequirementStatusStyle {
  bgColor: string;
  borderColor: string;
  textColor: string;
  barColor: string;
  icon: string;
  label: string;
}

export const REQUIREMENT_LEVEL_STYLES: Record<RequirementLevel, RequirementLevelStyle> = {
  A: {
    bgColor: '',
    borderColor: 'border-[var(--color-border-secondary)]',
    textColor: 'text-[var(--color-text-tertiary)]',
    label: 'Level A',
  },
  B: {
    bgColor: '',
    borderColor: 'border-[var(--color-border-secondary)]',
    textColor: 'text-[var(--color-text-tertiary)]',
    label: 'Level B',
  },
  C: {
    bgColor: '',
    borderColor: 'border-[var(--color-border-secondary)]',
    textColor: 'text-[var(--color-text-tertiary)]',
    label: 'Level C',
  },
};

export interface RequirementPriorityStyle {
  bgColor: string;
  borderColor: string;
  textColor: string;
  icon: string;
  label: string;
}

export const REQUIREMENT_PRIORITY_STYLES: Record<RequirementPriority, RequirementPriorityStyle> = {
  Low: {
    bgColor: 'bg-[var(--color-info-500)]/10',
    borderColor: 'border-[var(--color-info-500)]/20',
    textColor: 'text-[var(--color-info-600)] dark:text-[var(--color-info-400)]',
    icon: 'arrow-down',
    label: 'Low',
  },
  Medium: {
    bgColor: 'bg-[var(--color-warning-500)]/10',
    borderColor: 'border-[var(--color-warning-500)]/20',
    textColor: 'text-[var(--color-warning-600)] dark:text-[var(--color-warning-400)]',
    icon: 'minus',
    label: 'Medium',
  },
  High: {
    bgColor: 'bg-[var(--color-error-500)]/10',
    borderColor: 'border-[var(--color-error-500)]/20',
    textColor: 'text-[var(--color-error-600)] dark:text-[var(--color-error-400)]',
    icon: 'arrow-up',
    label: 'High',
  },
  Urgent: {
    bgColor: 'bg-[var(--color-error-600)]/15',
    borderColor: 'border-[var(--color-error-600)]/30',
    textColor: 'text-[var(--color-error-700)] dark:text-[var(--color-error-300)]',
    icon: 'alert-triangle',
    label: 'Urgent',
  },
};

export const REQUIREMENT_STATUS_STYLES: Record<RequirementStatus, RequirementStatusStyle> = {
  Draft: {
    bgColor: 'bg-[var(--color-neutral-500)]/10',
    borderColor: 'border-[var(--color-neutral-500)]/20',
    textColor: 'text-[var(--color-neutral-600)] dark:text-[var(--color-neutral-400)]',
    barColor: 'bg-[var(--color-neutral-500)]',
    icon: 'edit',
    label: 'Draft',
  },
  InReview: {
    bgColor: 'bg-[var(--color-warning-500)]/10',
    borderColor: 'border-[var(--color-warning-500)]/20',
    textColor: 'text-[var(--color-warning-600)] dark:text-[var(--color-warning-400)]',
    barColor: 'bg-[var(--color-warning-500)]',
    icon: 'eye',
    label: 'In Review',
  },
  Approved: {
    bgColor: 'bg-[var(--color-success-500)]/10',
    borderColor: 'border-[var(--color-success-500)]/20',
    textColor: 'text-[var(--color-success-600)] dark:text-[var(--color-success-400)]',
    barColor: 'bg-[var(--color-success-500)]',
    icon: 'check-circle',
    label: 'Approved',
  },
  Deprecated: {
    bgColor: 'bg-[var(--color-error-500)]/10',
    borderColor: 'border-[var(--color-error-500)]/20',
    textColor: 'text-[var(--color-error-600)] dark:text-[var(--color-error-400)]',
    barColor: 'bg-[var(--color-error-500)]',
    icon: 'archive',
    label: 'Deprecated',
  },
};

/**
 * Valid status transitions
 */
export const VALID_STATUS_TRANSITIONS: Record<RequirementStatus, RequirementStatus[]> = {
  Draft: ['InReview', 'Deprecated'],
  InReview: ['Approved', 'Draft', 'Deprecated'],
  Approved: ['Deprecated'],
  Deprecated: [],
};

/**
 * Check if a status transition is valid
 */
export function canTransitionTo(
  current: RequirementStatus,
  target: RequirementStatus
): boolean {
  return VALID_STATUS_TRANSITIONS[current].includes(target);
}

/**
 * Check if a requirement is editable (Draft only)
 */
export function isEditable(status: RequirementStatus): boolean {
  return status === 'Draft';
}

