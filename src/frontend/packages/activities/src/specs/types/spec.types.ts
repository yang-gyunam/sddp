/**
 * Spec Management Type Definitions
 * Based on REQ-01 requirements (2.3 Spec Management)
 */

import type { UserRef } from '../../shared/types';

// ============================================
// Enums (matching backend)
// ============================================

/**
 * Spec status - follows state machine:
 * Draft → InReview → Approved → Locked
 */
export type SpecStatus = 'Draft' | 'InReview' | 'Approved' | 'Locked';

// ============================================
// DTOs (matching backend response)
// ============================================

/**
 * Spec summary (list view)
 */
export interface Spec {
  id: string;
  tenantId: string;
  projectId: string;
  code: string;
  title: string;
  description: string;
  decision: string;
  status: SpecStatus;
  requirementId: string | null;
  bornFromConversationId: string | null;
  supersedesSpecId: string | null;
  version: string;
  lockedAt: string | null;
  createdAt: string;
  updatedAt: string;
}

/**
 * Spec detail (extended information)
 */
export interface SpecDetail extends Spec {
  context: string;
  scope: string;
  outOfScope: string;
  definitions: string;
  acceptanceCriteria: string;
  owners: string;
  reviewTrigger: string;
  requirementCode: string | null;
  requirementTitle: string | null;
  bornFromConversationName: string | null;
  bornFromConversationType: string | null;
  bornFromConversationDescription: string | null;
  createdBy: UserRef;
  updatedBy: UserRef;
  validFrom: string;
  validTo: string | null;
}

/**
 * Paginated specs response
 */
export interface SpecPage {
  items: Spec[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

// ============================================
// Request DTOs
// ============================================

export interface CreateSpecRequest {
  code: string;
  title: string;
  description: string;
  decision: string;
  context?: string;
  scope?: string;
  outOfScope?: string;
  definitions?: string;
  acceptanceCriteria?: string;
  owners?: string;
  reviewTrigger?: string;
  requirementId?: string;
  bornFromConversationId?: string;
}

export interface UpdateSpecRequest {
  title: string;
  description: string;
  decision: string;
  context: string;
  scope: string;
  outOfScope: string;
  definitions: string;
  acceptanceCriteria: string;
  owners: string;
  reviewTrigger: string;
  requirementId?: string;
  bornFromConversationId?: string;
}

export interface TransitionStatusRequest {
  newStatus: SpecStatus;
}

export interface LinkRequirementRequest {
  requirementId: string;
}

export interface RejectRequest {
  reason?: string;
}

// ============================================
// Store State
// ============================================

export interface SpecState {
  // List of specs
  specs: Spec[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  specsLoading: boolean;
  specsError: string | null;

  // Filters
  statusFilter: SpecStatus | null;
  codeSearch: string | null;

  // Current spec
  currentSpec: SpecDetail | null;
  currentSpecLoading: boolean;
  currentSpecError: string | null;

  // Version history
  specVersions: Spec[];
  versionsLoading: boolean;
  versionsError: string | null;
}

// ============================================
// UI Types
// ============================================

export interface SpecStatusStyle {
  color?: string;
  bgColor: string;
  borderColor: string;
  textColor: string;
  icon: string;
  label: string;
}

export const SPEC_STATUS_STYLES: Record<SpecStatus, SpecStatusStyle> = {
  Draft: {
    bgColor: 'bg-[var(--color-neutral-500)]/10',
    borderColor: 'border-[var(--color-neutral-500)]/20',
    textColor: 'text-[var(--color-neutral-600)] dark:text-[var(--color-neutral-400)]',
    icon: 'edit',
    label: 'Draft',
  },
  InReview: {
    bgColor: 'bg-[var(--color-warning-500)]/10',
    borderColor: 'border-[var(--color-warning-500)]/20',
    textColor: 'text-[var(--color-warning-600)] dark:text-[var(--color-warning-400)]',
    icon: 'eye',
    label: 'In Review',
  },
  Approved: {
    bgColor: 'bg-[var(--color-success-500)]/10',
    borderColor: 'border-[var(--color-success-500)]/20',
    textColor: 'text-[var(--color-success-600)] dark:text-[var(--color-success-400)]',
    icon: 'check-circle',
    label: 'Approved',
  },
  Locked: {
    bgColor: 'bg-[var(--color-info-500)]/10',
    borderColor: 'border-[var(--color-info-500)]/20',
    textColor: 'text-[var(--color-info-600)] dark:text-[var(--color-info-400)]',
    icon: 'lock',
    label: 'Locked',
  },
};

/**
 * Valid status transitions
 * Draft → InReview → Approved → Locked
 * InReview can go back to Draft (rejection)
 */
export const VALID_STATUS_TRANSITIONS: Record<SpecStatus, SpecStatus[]> = {
  Draft: ['InReview'],
  InReview: ['Approved', 'Draft'],
  Approved: ['Locked'],
  Locked: [],
};

/**
 * Check if a status transition is valid
 */
export function canTransitionTo(current: SpecStatus, target: SpecStatus): boolean {
  return VALID_STATUS_TRANSITIONS[current].includes(target);
}

/**
 * Check if a spec is editable (Draft only)
 */
export function isEditable(status: SpecStatus): boolean {
  return status === 'Draft';
}

/**
 * Check if a spec can create a new version (Locked only)
 */
export function canCreateNewVersion(status: SpecStatus): boolean {
  return status === 'Locked';
}

/**
 * Get available actions for a spec based on its status
 */
export function getAvailableActions(
  status: SpecStatus
): { action: string; label: string; variant: 'primary' | 'secondary' | 'danger' }[] {
  switch (status) {
    case 'Draft':
      return [{ action: 'submitForReview', label: 'Submit for Review', variant: 'primary' }];
    case 'InReview':
      return [
        { action: 'approve', label: 'Approve', variant: 'primary' },
        { action: 'reject', label: 'Reject', variant: 'danger' },
      ];
    case 'Approved':
      return [{ action: 'lock', label: 'Lock', variant: 'primary' }];
    case 'Locked':
      return [{ action: 'newVersion', label: 'Create New Version', variant: 'secondary' }];
    default:
      return [];
  }
}

// ============================================
// Sign-off Types (REQ-04.3: Sign-off)
// ============================================

/**
 * Sign-off decision types
 */
export type SignOffDecision = 'Pending' | 'Approved' | 'Rejected' | 'Conditional';

/**
 * Role type for sign-off
 */
export type RoleType = 'Owner' | 'Reviewer' | 'Approver' | 'Stakeholder';

/**
 * Sign-off entry (individual reviewer's sign-off)
 */
export interface SignOff {
  id: string;
  specId: string;
  stakeholder: UserRef;
  role: RoleType;
  decision: SignOffDecision;
  conditions?: string | null;
  comments?: string | null;
  signedAt?: string | null;
  createdAt: string;
  updatedAt: string;
}

/**
 * Sign-off request DTO
 */
export interface SignOffRequest {
  decision: SignOffDecision;
  conditions?: string;
  comments?: string;
}

/**
 * Sign-off summary for a spec
 */
export interface SignOffSummary {
  specId: string;
  totalCount: number;
  pendingCount: number;
  approvedCount: number;
  rejectedCount: number;
  conditionalCount: number;
  signOffs: SignOff[];
}

export interface SpecSummaryResult {
  summary: string;
  fromCache: boolean;
  modelUsed?: string | null;
  generatedAt: string;
}

/**
 * Sign-off decision styles
 */
export interface SignOffDecisionStyle {
  bgColor: string;
  borderColor: string;
  textColor: string;
  icon: string;
  label: string;
}

export const SIGN_OFF_DECISION_STYLES: Record<SignOffDecision, SignOffDecisionStyle> = {
  Pending: {
    bgColor: 'bg-[var(--color-neutral-500)]/10',
    borderColor: 'border-[var(--color-neutral-500)]/20',
    textColor: 'text-[var(--color-neutral-600)] dark:text-[var(--color-neutral-400)]',
    icon: 'clock',
    label: 'Pending',
  },
  Approved: {
    bgColor: 'bg-[var(--color-success-500)]/10',
    borderColor: 'border-[var(--color-success-500)]/20',
    textColor: 'text-[var(--color-success-600)] dark:text-[var(--color-success-400)]',
    icon: 'check-circle',
    label: 'Approved',
  },
  Rejected: {
    bgColor: 'bg-[var(--color-error-500)]/10',
    borderColor: 'border-[var(--color-error-500)]/20',
    textColor: 'text-[var(--color-error-600)] dark:text-[var(--color-error-400)]',
    icon: 'x-circle',
    label: 'Rejected',
  },
  Conditional: {
    bgColor: 'bg-[var(--color-warning-500)]/10',
    borderColor: 'border-[var(--color-warning-500)]/20',
    textColor: 'text-[var(--color-warning-600)] dark:text-[var(--color-warning-400)]',
    icon: 'alert-circle',
    label: 'Conditional',
  },
};

/**
 * Role type styles
 */
export const ROLE_TYPE_STYLES: Record<RoleType, { label: string; icon: string }> = {
  Owner: { label: 'Owner', icon: 'crown' },
  Reviewer: { label: 'Reviewer', icon: 'eye' },
  Approver: { label: 'Approver', icon: 'check-square' },
  Stakeholder: { label: 'Stakeholder', icon: 'user' },
};

// ============================================
// Change Tracking Types (3.6.2: Item Level Change Tracking)
// ============================================

/**
 * Field author information - who last modified each field
 */
export interface FieldAuthor {
  fieldName: string;
  userId: string;
  userName: string;
  timestamp: string;
}

/**
 * Audit log entry for timeline display
 */
export interface AuditLogEntry {
  id: string;
  timestamp: string;
  userId: string;
  userName: string;
  action: string;
  resourceType: string;
  resourceId: string;
  ipAddress: string;
  details?: Record<string, unknown>;
}
