/**
 * Activity-specific Glossary Type Definitions
 * Extends glossary types for Activity-level UI
 */

// Re-export from local glossary types
export type {
  GlossaryTerm,
  GlossaryTermDetail,
  GlossaryTermSummary,
  GlossaryTermPage,
  GlossaryTermUsage,
  GlossaryTermUsageItem,
  GlossaryConflictResult,
  GlossaryConflict,
  TermCategory,
  GlossaryTermStatus,
  TermCategoryStyle,
  TermStatusStyle,
  CreateGlossaryTermRequest,
  UpdateGlossaryTermRequest,
  DeprecateGlossaryTermRequest,
} from './glossary.types';

export {
  TERM_CATEGORY_STYLES,
  TERM_STATUS_STYLES,
  getCategoryLabel,
  getStatusLabel,
  isStatusEditable,
  isStatusUsable,
  canTransitionTo,
  parseSynonyms,
  formatSynonyms,
} from './glossary.types';

// ============================================
// Activity-specific Types
// ============================================

import type { GlossaryTermStatus, TermCategory } from './glossary.types';

/**
 * Term summary for sidebar display
 */
export interface TermSummaryItem {
  id: string;
  term: string;
  abbreviation: string | null;
  category: TermCategory;
  status: GlossaryTermStatus;
  version: string;
  usageCount?: number;
}

/**
 * Category-based term group for sidebar
 */
export interface CategoryTermGroup {
  category: TermCategory;
  terms: TermSummaryItem[];
  totalCount: number;
  expanded: boolean;
}

/**
 * Filter type for glossary sidebar
 */
export type GlossaryFilterType = 'all' | 'draft' | 'active' | 'deprecated';

/**
 * Sidebar state for Glossary Activity
 */
export interface GlossarySidebarState {
  searchQuery: string;
  filterType: GlossaryFilterType;
  expandedCategories: Set<TermCategory>;
  selectedTermId: string | null;
  categoryGroups: CategoryTermGroup[];
}
