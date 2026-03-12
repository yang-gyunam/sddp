/**
 * Artifact Type Definitions
 * Based on backend ArtifactType enum and ArtifactTrackingDto
 */

import type { UserRef } from '../../shared/types';

// ============================================
// Enums
// ============================================

/**
 * Artifact type - matches backend ArtifactType enum
 */
export type ArtifactType =
  | 'Entity'
  | 'CreateDto'
  | 'UpdateDto'
  | 'ResponseDto'
  | 'Validator'
  | 'Repository'
  | 'DapperReadRepository'
  | 'Command'
  | 'Query'
  | 'UnitTest'
  | 'Endpoints'
  | 'Service'
  | 'Migration'
  | 'EntityJson'
  | 'UiComponent'
  | 'Documentation'
  | 'Configuration';

export const ARTIFACT_TYPES: ArtifactType[] = [
  'Entity', 'CreateDto', 'UpdateDto', 'ResponseDto',
  'Validator', 'Repository', 'DapperReadRepository',
  'Command', 'Query', 'UnitTest', 'Endpoints',
  'Service', 'Migration', 'EntityJson', 'UiComponent',
  'Documentation', 'Configuration',
];

/**
 * Artifact verification status
 */
export type ArtifactStatus = 'Valid' | 'Modified' | 'Missing' | 'Unverified';

// ============================================
// DTOs
// ============================================

/**
 * Artifact tracking item
 */
export interface Artifact {
  id: string;
  tenantId: string;
  projectId: string;
  specId: string;
  specCode?: string;
  specTitle?: string;
  artifactPath: string;
  artifactType: ArtifactType;
  contentHash: string;
  generatorVersion: string;
  templateVersion: string;
  entityName: string;
  glossaryTermId?: string;
  sourceConversationId?: string;
  sourceRequirementId?: string;
  ownerUserId?: string;
  createdAt: string;
  updatedAt: string;
}

/**
 * Artifact with verification result
 */
export interface ArtifactDetail extends Artifact {
  status: ArtifactStatus;
  currentHash?: string;
  fileSize?: number;
  lineCount?: number;
}

/**
 * Artifact summary for sidebar
 */
export interface ArtifactSummary {
  id: string;
  artifactPath: string;
  artifactType: ArtifactType;
  entityName: string;
  specCode?: string;
  specTitle?: string;
  glossaryTermId?: string;
  glossaryTermName?: string;
  sourceConversationId?: string;
  sourceConversationName?: string;
  sourceRequirementId?: string;
  sourceRequirementCode?: string;
  owner: UserRef | null;
  createdBy: UserRef | null;
  updatedBy: UserRef | null;
  status: ArtifactStatus;
  generatorVersion?: string;
  templateVersion?: string;
  contentHash?: string;
  isValid?: boolean;
  createdAt?: string;
  updatedAt: string;
}

/**
 * Group artifacts by type
 */
export interface ArtifactTypeGroup {
  type: ArtifactType;
  artifacts: ArtifactSummary[];
  totalCount: number;
  expanded: boolean;
}

/**
 * Filter type for artifact sidebar
 */
export type ArtifactFilterType = 'all' | 'valid' | 'modified' | 'missing';

/**
 * Sidebar state for Artifact Activity
 */
export interface ArtifactSidebarState {
  searchQuery: string;
  filterType: ArtifactFilterType;
  expandedTypes: Set<ArtifactType>;
  selectedArtifactId: string | null;
  typeGroups: ArtifactTypeGroup[];
}

// ============================================
// Style Definitions
// ============================================

export interface ArtifactTypeStyle {
  color: string;
  bgColor: string;
  borderColor: string;
  label: string;
  icon: string;
}

export interface ArtifactStatusStyle {
  color: string;
  bgColor: string;
  borderColor: string;
  label: string;
  icon: string;
}

/**
 * Artifact type styles
 */
export const ARTIFACT_TYPE_STYLES: Record<ArtifactType, ArtifactTypeStyle> = {
  Entity: {
    color: 'text-[var(--color-info-600)] dark:text-[var(--color-info-400)]',
    bgColor: 'bg-[var(--color-info-500)]/10',
    borderColor: 'border-[var(--color-info-500)]/20',
    label: 'Entity',
    icon: 'box',
  },
  CreateDto: {
    color: 'text-[var(--color-success-600)] dark:text-[var(--color-success-400)]',
    bgColor: 'bg-[var(--color-success-500)]/10',
    borderColor: 'border-[var(--color-success-500)]/20',
    label: 'Create DTO',
    icon: 'plus-circle',
  },
  UpdateDto: {
    color: 'text-[var(--color-amber-600)] dark:text-[var(--color-amber-400)]',
    bgColor: 'bg-[var(--color-amber-500)]/10',
    borderColor: 'border-[var(--color-amber-500)]/20',
    label: 'Update DTO',
    icon: 'edit',
  },
  ResponseDto: {
    color: 'text-[var(--color-purple-600)] dark:text-[var(--color-purple-400)]',
    bgColor: 'bg-[var(--color-purple-500)]/10',
    borderColor: 'border-[var(--color-purple-500)]/20',
    label: 'Response DTO',
    icon: 'arrow-right',
  },
  Validator: {
    color: 'text-[var(--color-error-600)] dark:text-[var(--color-error-400)]',
    bgColor: 'bg-[var(--color-error-500)]/10',
    borderColor: 'border-[var(--color-error-500)]/20',
    label: 'Validator',
    icon: 'shield',
  },
  Repository: {
    color: 'text-[var(--color-indigo-600)] dark:text-[var(--color-indigo-400)]',
    bgColor: 'bg-[var(--color-indigo-500)]/10',
    borderColor: 'border-[var(--color-indigo-500)]/20',
    label: 'Repository',
    icon: 'database',
  },
  DapperReadRepository: {
    color: 'text-[var(--color-cyan-600)] dark:text-[var(--color-cyan-400)]',
    bgColor: 'bg-[var(--color-cyan-500)]/10',
    borderColor: 'border-[var(--color-cyan-500)]/20',
    label: 'Dapper Repo',
    icon: 'zap',
  },
  Command: {
    color: 'text-[var(--color-orange-600)] dark:text-[var(--color-orange-400)]',
    bgColor: 'bg-[var(--color-orange-500)]/10',
    borderColor: 'border-[var(--color-orange-500)]/20',
    label: 'Command',
    icon: 'terminal',
  },
  Query: {
    color: 'text-[var(--color-teal-600)] dark:text-[var(--color-teal-400)]',
    bgColor: 'bg-[var(--color-teal-500)]/10',
    borderColor: 'border-[var(--color-teal-500)]/20',
    label: 'Query',
    icon: 'search',
  },
  UnitTest: {
    color: 'text-[var(--color-lime-600)] dark:text-[var(--color-lime-400)]',
    bgColor: 'bg-[var(--color-lime-500)]/10',
    borderColor: 'border-[var(--color-lime-500)]/20',
    label: 'Unit Test',
    icon: 'check-square',
  },
  Endpoints: {
    color: 'text-[var(--color-pink-600)] dark:text-[var(--color-pink-400)]',
    bgColor: 'bg-[var(--color-pink-500)]/10',
    borderColor: 'border-[var(--color-pink-500)]/20',
    label: 'Endpoints',
    icon: 'globe',
  },
  Service: {
    color: 'text-[var(--color-violet-600)] dark:text-[var(--color-violet-400)]',
    bgColor: 'bg-[var(--color-violet-500)]/10',
    borderColor: 'border-[var(--color-violet-500)]/20',
    label: 'Service',
    icon: 'settings',
  },
  Migration: {
    color: 'text-[var(--color-neutral-600)] dark:text-[var(--color-neutral-400)]',
    bgColor: 'bg-[var(--color-neutral-500)]/10',
    borderColor: 'border-[var(--color-neutral-500)]/20',
    label: 'Migration',
    icon: 'git-branch',
  },
  EntityJson: {
    color: 'text-[var(--color-yellow-600)] dark:text-[var(--color-yellow-400)]',
    bgColor: 'bg-[var(--color-yellow-500)]/10',
    borderColor: 'border-[var(--color-yellow-500)]/20',
    label: 'Entity JSON',
    icon: 'file-json',
  },
  UiComponent: {
    color: 'text-[var(--color-rose-600)] dark:text-[var(--color-rose-400)]',
    bgColor: 'bg-[var(--color-rose-500)]/10',
    borderColor: 'border-[var(--color-rose-500)]/20',
    label: 'UI Component',
    icon: 'layout',
  },
  Documentation: {
    color: 'text-[var(--color-sky-600)] dark:text-[var(--color-sky-400)]',
    bgColor: 'bg-[var(--color-sky-500)]/10',
    borderColor: 'border-[var(--color-sky-500)]/20',
    label: 'Documentation',
    icon: 'file-text',
  },
  Configuration: {
    color: 'text-[var(--color-slate-600)] dark:text-[var(--color-slate-400)]',
    bgColor: 'bg-[var(--color-slate-500)]/10',
    borderColor: 'border-[var(--color-slate-500)]/20',
    label: 'Configuration',
    icon: 'sliders',
  },
};

/**
 * Artifact status styles
 */
export const ARTIFACT_STATUS_STYLES: Record<ArtifactStatus, ArtifactStatusStyle> = {
  Valid: {
    color: 'text-[var(--color-success-600)] dark:text-[var(--color-success-400)]',
    bgColor: 'bg-[var(--color-success-500)]/10',
    borderColor: 'border-[var(--color-success-500)]/20',
    label: 'Valid',
    icon: 'check-circle',
  },
  Modified: {
    color: 'text-[var(--color-warning-600)] dark:text-[var(--color-warning-400)]',
    bgColor: 'bg-[var(--color-warning-500)]/10',
    borderColor: 'border-[var(--color-warning-500)]/20',
    label: 'Modified',
    icon: 'alert-triangle',
  },
  Missing: {
    color: 'text-[var(--color-error-600)] dark:text-[var(--color-error-400)]',
    bgColor: 'bg-[var(--color-error-500)]/10',
    borderColor: 'border-[var(--color-error-500)]/20',
    label: 'Missing',
    icon: 'x-circle',
  },
  Unverified: {
    color: 'text-[var(--color-neutral-600)] dark:text-[var(--color-neutral-400)]',
    bgColor: 'bg-[var(--color-neutral-500)]/10',
    borderColor: 'border-[var(--color-neutral-500)]/20',
    label: 'Unverified',
    icon: 'help-circle',
  },
};

// ============================================
// Utility Functions
// ============================================

export function getTypeLabel(type: ArtifactType): string {
  return ARTIFACT_TYPE_STYLES[type]?.label ?? type;
}

export function getStatusLabel(status: ArtifactStatus): string {
  return ARTIFACT_STATUS_STYLES[status]?.label ?? status;
}

export function getFileExtension(path: string): string {
  const parts = path.split('.');
  return parts.length > 1 ? parts[parts.length - 1]! : '';
}

export function getFileName(path: string): string {
  const parts = path.split('/');
  return parts[parts.length - 1]!;
}

