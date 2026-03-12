/**
 * Glossary Management Type Definitions
 * Based on REQ-12.1 (Glossary Management) and REQ-12.2 (Search)
 */

import type { UserRef } from '../../shared/types';

// ============================================
// Enums (matching backend)
// ============================================

/**
 * Term categories
 */
export type TermCategory = 'Technical' | 'Business' | 'Abbreviation' | 'Domain' | 'Architecture' | 'Infrastructure' | 'Security';

/**
 * Array of all term categories (for iteration)
 */
export const TERM_CATEGORIES: TermCategory[] = ['Technical', 'Business', 'Abbreviation', 'Domain', 'Architecture', 'Infrastructure', 'Security'];

/**
 * Glossary term status
 */
export type GlossaryTermStatus = 'Draft' | 'Active' | 'Deprecated';

// ============================================
// DTOs (matching backend response)
// ============================================

/**
 * Glossary term (list item)
 */
export interface GlossaryTerm {
  id: string;
  tenantId: string;
  projectId: string;
  term: string;
  definition: string;
  category: TermCategory;
  status: GlossaryTermStatus;
  synonyms: string | null;
  abbreviation: string | null;
  version: string;
  createdAt: string;
  updatedAt: string;
}

/**
 * Glossary term detail
 */
export interface GlossaryTermDetail extends GlossaryTerm {
  usageExamples: string[];
  relatedTermIds: string[];
  source: string | null;
  definedBy: UserRef;
  approvedBy: UserRef | null;
  approvedAt: string | null;
  replacedByTermId: string | null;
  replacedByTermName: string | null;
  sourceSpecId: string | null;
  sourceSpecCode: string | null;
  sourceSpecTitle: string | null;
  sourceConversationId: string | null;
  sourceConversationName: string | null;
  sourceConversationType: string | null;
  sourceRequirementId: string | null;
  sourceRequirementCode: string | null;
  sourceRequirementTitle: string | null;
  owner: UserRef | null;
  createdBy: UserRef;
  updatedBy: UserRef;
  validFrom: string;
  validTo: string | null;
}

/**
 * Glossary term summary (autocomplete)
 */
export interface GlossaryTermSummary {
  id: string;
  term: string;
  definition: string;
  category: TermCategory;
  abbreviation?: string | null;
  status: GlossaryTermStatus;
}

/**
 * Glossary term suggestion (alias for autocomplete)
 */
export type GlossaryTermSuggestion = GlossaryTermSummary;

/**
 * Paged response for glossary terms
 */
export interface GlossaryTermPage {
  items: GlossaryTerm[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

/**
 * Conflict detection result
 */
export interface GlossaryConflictResult {
  hasConflict: boolean;
  conflicts: GlossaryConflict[];
}

/**
 * Individual conflict
 */
export interface GlossaryConflict {
  termId: string;
  term: string;
  conflictType: 'SameTerm' | 'Synonym' | 'Abbreviation' | 'SimilarDefinition';
  message: string;
}

/**
 * Term usage information
 */
export interface GlossaryTermUsage {
  termId: string;
  term: string;
  usageCount: number;
  usages: GlossaryTermUsageItem[];
}

/**
 * Individual usage item
 */
export interface GlossaryTermUsageItem {
  entityType: 'Spec' | 'Requirement' | 'Conversation';
  entityId: string;
  entityTitle: string;
  fieldName: string;
}

/**
 * Glossary term version (for version history)
 */
export interface GlossaryTermVersion {
  id: string;
  term: string;
  definition: string;
  category: TermCategory;
  status: GlossaryTermStatus;
  synonyms: string | null;
  abbreviation: string | null;
  version: string;
  definedBy: UserRef | null;
  updatedBy: UserRef | null;
  validFrom: string;
  validTo: string | null;
  createdAt: string;
  updatedAt: string;
}

// ============================================
// Request DTOs
// ============================================

/**
 * Create term request
 */
export interface CreateGlossaryTermRequest {
  term: string;
  definition: string;
  category: TermCategory;
  source?: string;
  synonyms?: string;
  abbreviation?: string;
  usageExamples?: string[];
  relatedTermIds?: string[];
  sourceSpecId?: string;
  sourceConversationId?: string;
  sourceRequirementId?: string;
  ownerUserId?: string;
}

/**
 * Update term request
 */
export interface UpdateGlossaryTermRequest {
  definition?: string;
  category?: TermCategory;
  source?: string;
  synonyms?: string;
  abbreviation?: string;
  usageExamples?: string[];
  relatedTermIds?: string[];
  sourceSpecId?: string;
  sourceConversationId?: string;
  sourceRequirementId?: string;
  ownerUserId?: string;
}

/**
 * Deprecate term request
 */
export interface DeprecateGlossaryTermRequest {
  replacementTermId?: string;
}

/**
 * Conflict detection request
 */
export interface DetectConflictRequest {
  term: string;
  definition?: string;
  excludeTermId?: string;
}

// ============================================
// UI Style Definitions
// ============================================

export interface TermCategoryStyle {
  color: string;
  bgColor: string;
  borderColor: string;
  label: string;
  icon: string;
}

export interface TermStatusStyle {
  color: string;
  bgColor: string;
  borderColor: string;
  label: string;
  icon: string;
}

/**
 * Category style mappings
 */
export const TERM_CATEGORY_STYLES: Record<TermCategory, TermCategoryStyle> = {
  Technical: {
    color: 'text-[var(--color-info-600)] dark:text-[var(--color-info-400)]',
    bgColor: 'bg-[var(--color-info-500)]/10',
    borderColor: 'border-[var(--color-info-500)]/20',
    label: 'Technical',
    icon: 'code',
  },
  Business: {
    color: 'text-[var(--color-success-600)] dark:text-[var(--color-success-400)]',
    bgColor: 'bg-[var(--color-success-500)]/10',
    borderColor: 'border-[var(--color-success-500)]/20',
    label: 'Business',
    icon: 'briefcase',
  },
  Abbreviation: {
    color: 'text-[var(--color-purple-600)] dark:text-[var(--color-purple-400)]',
    bgColor: 'bg-[var(--color-purple-500)]/10',
    borderColor: 'border-[var(--color-purple-500)]/20',
    label: 'Abbreviation',
    icon: 'hash',
  },
  Domain: {
    color: 'text-[var(--color-amber-600)] dark:text-[var(--color-amber-400)]',
    bgColor: 'bg-[var(--color-amber-500)]/10',
    borderColor: 'border-[var(--color-amber-500)]/20',
    label: 'Domain',
    icon: 'layers',
  },
  Architecture: {
    color: 'text-[var(--color-indigo-600)] dark:text-[var(--color-indigo-400)]',
    bgColor: 'bg-[var(--color-indigo-500)]/10',
    borderColor: 'border-[var(--color-indigo-500)]/20',
    label: 'Architecture',
    icon: 'layout',
  },
  Infrastructure: {
    color: 'text-[var(--color-slate-600)] dark:text-[var(--color-slate-400)]',
    bgColor: 'bg-[var(--color-slate-500)]/10',
    borderColor: 'border-[var(--color-slate-500)]/20',
    label: 'Infrastructure',
    icon: 'server',
  },
  Security: {
    color: 'text-[var(--color-error-600)] dark:text-[var(--color-error-400)]',
    bgColor: 'bg-[var(--color-error-500)]/10',
    borderColor: 'border-[var(--color-error-500)]/20',
    label: 'Security',
    icon: 'shield',
  },
};

/**
 * Status style mappings
 */
export const TERM_STATUS_STYLES: Record<GlossaryTermStatus, TermStatusStyle> = {
  Draft: {
    color: 'text-[var(--color-neutral-600)] dark:text-[var(--color-neutral-400)]',
    bgColor: 'bg-[var(--color-neutral-500)]/10',
    borderColor: 'border-[var(--color-neutral-500)]/20',
    label: 'Draft',
    icon: 'file-text',
  },
  Active: {
    color: 'text-[var(--color-success-600)] dark:text-[var(--color-success-400)]',
    bgColor: 'bg-[var(--color-success-500)]/10',
    borderColor: 'border-[var(--color-success-500)]/20',
    label: 'Active',
    icon: 'check-circle',
  },
  Deprecated: {
    color: 'text-[var(--color-error-600)] dark:text-[var(--color-error-400)]',
    bgColor: 'bg-[var(--color-error-500)]/10',
    borderColor: 'border-[var(--color-error-500)]/20',
    label: 'Deprecated',
    icon: 'x-circle',
  },
};

// ============================================
// Utility Functions
// ============================================

/**
 * Get display label for category
 */
export function getCategoryLabel(category: TermCategory): string {
  return TERM_CATEGORY_STYLES[category]?.label ?? category;
}

/**
 * Get display label for status
 */
export function getStatusLabel(status: GlossaryTermStatus): string {
  return TERM_STATUS_STYLES[status]?.label ?? status;
}

/**
 * Check if status is editable
 */
export function isStatusEditable(status: GlossaryTermStatus): boolean {
  return status === 'Draft';
}

/**
 * Check if status is usable (can be referenced in Spec)
 */
export function isStatusUsable(status: GlossaryTermStatus): boolean {
  return status === 'Active';
}

/**
 * Check if transition is valid
 */
export function canTransitionTo(
  current: GlossaryTermStatus,
  target: GlossaryTermStatus
): boolean {
  const validTransitions: Record<GlossaryTermStatus, GlossaryTermStatus[]> = {
    Draft: ['Active', 'Deprecated'],
    Active: ['Draft', 'Deprecated'],
    Deprecated: [],
  };
  return validTransitions[current]?.includes(target) ?? false;
}

/**
 * Parse synonyms string to array
 */
export function parseSynonyms(synonyms: string | null): string[] {
  if (!synonyms) return [];
  return synonyms
    .split(',')
    .map((s) => s.trim())
    .filter((s) => s.length > 0);
}

/**
 * Format synonyms array to string
 */
export function formatSynonyms(synonyms: string[]): string {
  return synonyms.join(', ');
}
