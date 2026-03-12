/**
 * Relationship Management Type Definitions
 * Based on REQ-04.4 (8 relationship types) and REQ-10.3 (relationship entity)
 */

import type { UserRef } from '../../shared/types';

// ============================================
// Enums (matching backend)
// ============================================

/**
 * Entity types that can participate in relationships
 */
export type EntityType = 'Spec' | 'Requirement' | 'Conversation' | 'GlossaryTerm' | 'Artifact' | 'Task';

/**
 * 8 relationship types
 */
export type RelationType =
  | 'Supersedes'
  | 'EvolvesFrom'
  | 'Extends'
  | 'ConflictsWith'
  | 'DependsOn'
  | 'Implements'
  | 'Replaces'
  | 'Affects';

/**
 * Diff change types for version comparison
 */
export type DiffChangeType = 'Unchanged' | 'Added' | 'Removed' | 'Modified';

// ============================================
// DTOs (matching backend response)
// ============================================

/**
 * Relationship entity
 */
export interface Relationship {
  id: string;
  tenantId: string;
  projectId: string;
  fromEntityType: EntityType;
  fromEntityId: string;
  fromEntityLabel: string | null;
  fromEntityCode: string | null;
  toEntityType: EntityType;
  toEntityId: string;
  toEntityLabel: string | null;
  toEntityCode: string | null;
  type: RelationType;
  typeLabel: string;
  reason: string | null;
  sourceSpecId: string | null;
  sourceDecisionId: string | null;
  validFrom: string;
  validTo: string | null;
  isCurrent: boolean;
  createdBy: UserRef;
  createdAt: string;
}

/**
 * Relationship list (incoming + outgoing)
 */
export interface RelationshipListData {
  incoming: Relationship[];
  outgoing: Relationship[];
  totalCount: number;
}

/**
 * Graph node for visualization
 */
export interface GraphNode {
  id: string;
  entityType: EntityType;
  label: string;
  status: string | null;
  depth: number;
}

/**
 * Graph edge for visualization
 */
export interface GraphEdge {
  id: string;
  sourceId: string;
  targetId: string;
  type: RelationType;
  typeLabel: string;
  strength?: number;
}

/**
 * Relationship graph response
 */
export interface RelationshipGraphData {
  nodes: GraphNode[];
  edges: GraphEdge[];
  maxDepth: number;
  rootEntityType: EntityType;
  rootEntityId: string;
}

/**
 * Relationship validation result
 */
export interface RelationshipValidationResult {
  isValid: boolean;
  hasCircularReference: boolean;
  circularPath: string[];
  existingRelationship: Relationship | null;
  message: string | null;
}

// ============================================
// Version Diff Types
// ============================================

/**
 * Field diff for version comparison
 */
export interface FieldDiff {
  fieldName: string;
  fieldLabel: string;
  oldValue: string | null;
  newValue: string | null;
  changeType: DiffChangeType;
}

/**
 * JSON diff operation (JSON Patch-like)
 */
export interface JsonDiffOperation {
  op: 'add' | 'remove' | 'replace' | 'move';
  path: string;
  valueJson?: string | null;
  from?: string | null;
}

/**
 * JSON diff result
 */
export interface JsonDiffResult {
  operations: JsonDiffOperation[];
}

/**
 * Spec diff result
 */
export interface SpecDiffResult {
  spec1Id: string;
  spec1Version: string;
  spec2Id: string;
  spec2Version: string;
  changes: FieldDiff[];
  addedCount: number;
  removedCount: number;
  modifiedCount: number;
  jsonDiff?: JsonDiffResult | null;
}

// ============================================
// Decision Impact Types
// ============================================

/**
 * Decision impact response - relationships created from a decision message
 */
export interface DecisionImpactData {
  totalCount: number;
  items: DecisionImpactItem[];
}

/**
 * Single impacted entity from a decision
 */
export interface DecisionImpactItem {
  entityType: EntityType;
  entityId: string;
  label: string;
  relationType: RelationType;
}

// ============================================
// Request DTOs
// ============================================

export interface CreateRelationshipRequest {
  fromEntityType: EntityType;
  fromEntityId: string;
  toEntityType: EntityType;
  toEntityId: string;
  type: RelationType;
  strength?: number;
  reason?: string;
  sourceSpecId?: string;
  sourceDecisionId?: string;
}

export interface ValidateRelationshipRequest {
  fromEntityType: string;
  fromEntityId: string;
  toEntityType: string;
  toEntityId: string;
  type: string;
}

// ============================================
// Store State
// ============================================

export interface RelationshipState {
  // Current entity relationships
  relationships: RelationshipListData | null;
  relationshipsLoading: boolean;
  relationshipsError: string | null;

  // Graph
  graph: RelationshipGraphData | null;
  graphLoading: boolean;
  graphError: string | null;
  graphDepth: number;

  // Diff
  diff: SpecDiffResult | null;
  diffLoading: boolean;
  diffError: string | null;

  // Context
  currentEntityType: EntityType | null;
  currentEntityId: string | null;
}

// ============================================
// UI Types
// ============================================

export interface RelationTypeStyle {
  color: string;
  bgColor: string;
  borderColor: string;
  label: string;
  icon: string;
  description: string;
}

/**
 * Relationship type styles for UI rendering
 */
export const RELATION_TYPE_STYLES: Record<RelationType, RelationTypeStyle> = {
  Supersedes: {
    color: 'text-[var(--color-info-600)] dark:text-[var(--color-info-400)]',
    bgColor: 'bg-[var(--color-info-500)]/10',
    borderColor: 'border-[var(--color-info-500)]/20',
    label: 'Supersedes',
    icon: 'arrow-up-circle',
    description: 'This entity supersedes (replaces with improvements) the target',
  },
  EvolvesFrom: {
    color: 'text-[var(--color-indigo-600)] dark:text-[var(--color-indigo-400)]',
    bgColor: 'bg-[var(--color-indigo-500)]/10',
    borderColor: 'border-[var(--color-indigo-500)]/20',
    label: 'Evolves From',
    icon: 'git-branch',
    description: 'This entity evolved from the target',
  },
  Extends: {
    color: 'text-[var(--color-success-600)] dark:text-[var(--color-success-400)]',
    bgColor: 'bg-[var(--color-success-500)]/10',
    borderColor: 'border-[var(--color-success-500)]/20',
    label: 'Extends',
    icon: 'plus-circle',
    description: 'This entity extends the target with additional capabilities',
  },
  ConflictsWith: {
    color: 'text-[var(--color-error-600)] dark:text-[var(--color-error-400)]',
    bgColor: 'bg-[var(--color-error-500)]/10',
    borderColor: 'border-[var(--color-error-500)]/20',
    label: 'Conflicts With',
    icon: 'alert-triangle',
    description: 'This entity conflicts with the target',
  },
  DependsOn: {
    color: 'text-[var(--color-warning-600)] dark:text-[var(--color-warning-400)]',
    bgColor: 'bg-[var(--color-warning-500)]/10',
    borderColor: 'border-[var(--color-warning-500)]/20',
    label: 'Depends On',
    icon: 'link',
    description: 'This entity depends on the target',
  },
  Implements: {
    color: 'text-[var(--color-teal-600)] dark:text-[var(--color-teal-400)]',
    bgColor: 'bg-[var(--color-teal-500)]/10',
    borderColor: 'border-[var(--color-teal-500)]/20',
    label: 'Implements',
    icon: 'check-square',
    description: 'This entity implements the target',
  },
  Replaces: {
    color: 'text-[var(--color-purple-600)] dark:text-[var(--color-purple-400)]',
    bgColor: 'bg-[var(--color-purple-500)]/10',
    borderColor: 'border-[var(--color-purple-500)]/20',
    label: 'Replaces',
    icon: 'refresh-cw',
    description: 'This entity replaces the target completely',
  },
  Affects: {
    color: 'text-[var(--color-neutral-600)] dark:text-[var(--color-neutral-400)]',
    bgColor: 'bg-[var(--color-neutral-500)]/10',
    borderColor: 'border-[var(--color-neutral-500)]/20',
    label: 'Affects',
    icon: 'arrow-right',
    description: 'This entity affects the target',
  },
};

export interface EntityTypeStyle {
  color: string;
  bgColor: string;
  borderColor: string;
  label: string;
  icon: string;
}

/**
 * Entity type styles for UI rendering
 */
export const ENTITY_TYPE_STYLES: Record<EntityType, EntityTypeStyle> = {
  Spec: {
    color: 'text-[var(--color-info-600)] dark:text-[var(--color-info-400)]',
    bgColor: 'bg-[var(--color-info-500)]/10',
    borderColor: 'border-[var(--color-info-500)]/20',
    label: 'Spec',
    icon: 'file-text',
  },
  Requirement: {
    color: 'text-[var(--color-success-600)] dark:text-[var(--color-success-400)]',
    bgColor: 'bg-[var(--color-success-500)]/10',
    borderColor: 'border-[var(--color-success-500)]/20',
    label: 'Requirement',
    icon: 'clipboard-list',
  },
  Conversation: {
    color: 'text-[var(--color-purple-600)] dark:text-[var(--color-purple-400)]',
    bgColor: 'bg-[var(--color-purple-500)]/10',
    borderColor: 'border-[var(--color-purple-500)]/20',
    label: 'Conversation',
    icon: 'message-circle',
  },
  GlossaryTerm: {
    color: 'text-[var(--color-warning-600)] dark:text-[var(--color-warning-400)]',
    bgColor: 'bg-[var(--color-warning-500)]/10',
    borderColor: 'border-[var(--color-warning-500)]/20',
    label: 'Glossary Term',
    icon: 'book',
  },
  Artifact: {
    color: 'text-[var(--color-cyan-600)] dark:text-[var(--color-cyan-400)]',
    bgColor: 'bg-[var(--color-cyan-500)]/10',
    borderColor: 'border-[var(--color-cyan-500)]/20',
    label: 'Artifact',
    icon: 'code',
  },
  Task: {
    color: 'text-[var(--color-teal-600)] dark:text-[var(--color-teal-400)]',
    bgColor: 'bg-[var(--color-teal-500)]/10',
    borderColor: 'border-[var(--color-teal-500)]/20',
    label: 'Task',
    icon: 'clipboard-list',
  },
};

export interface DiffChangeTypeStyle {
  color: string;
  bgColor: string;
  borderColor: string;
  label: string;
  icon: string;
}

/**
 * Diff change type styles for UI rendering
 */
export const DIFF_CHANGE_TYPE_STYLES: Record<DiffChangeType, DiffChangeTypeStyle> = {
  Unchanged: {
    color: 'text-[var(--color-neutral-600)] dark:text-[var(--color-neutral-400)]',
    bgColor: 'bg-[var(--color-neutral-500)]/10',
    borderColor: 'border-[var(--color-neutral-500)]/20',
    label: 'Unchanged',
    icon: 'minus',
  },
  Added: {
    color: 'text-[var(--color-success-600)] dark:text-[var(--color-success-400)]',
    bgColor: 'bg-[var(--color-success-500)]/10',
    borderColor: 'border-[var(--color-success-500)]/20',
    label: 'Added',
    icon: 'plus',
  },
  Removed: {
    color: 'text-[var(--color-error-600)] dark:text-[var(--color-error-400)]',
    bgColor: 'bg-[var(--color-error-500)]/10',
    borderColor: 'border-[var(--color-error-500)]/20',
    label: 'Removed',
    icon: 'x',
  },
  Modified: {
    color: 'text-[var(--color-warning-600)] dark:text-[var(--color-warning-400)]',
    bgColor: 'bg-[var(--color-warning-500)]/10',
    borderColor: 'border-[var(--color-warning-500)]/20',
    label: 'Modified',
    icon: 'edit-2',
  },
};

// ============================================
// Utility Functions
// ============================================

/**
 * Get available relationship types for a given entity type pair
 */
export function getAvailableRelationTypes(
  _fromType: EntityType,
  _toType: EntityType
): RelationType[] {
  // All types are available between any entity types
  return [
    'Supersedes',
    'EvolvesFrom',
    'Extends',
    'ConflictsWith',
    'DependsOn',
    'Implements',
    'Replaces',
    'Affects',
  ];
}

/**
 * Get inverse relationship type (for bidirectional display)
 */
export function getInverseRelationType(type: RelationType): string {
  const inverses: Record<RelationType, string> = {
    Supersedes: 'Superseded by',
    EvolvesFrom: 'Evolved to',
    Extends: 'Extended by',
    ConflictsWith: 'Conflicts with',
    DependsOn: 'Depended on by',
    Implements: 'Implemented by',
    Replaces: 'Replaced by',
    Affects: 'Affected by',
  };
  return inverses[type];
}

/**
 * Check if relationship is still valid (not invalidated)
 */
export function isRelationshipValid(relationship: Relationship): boolean {
  return relationship.validTo === null;
}
