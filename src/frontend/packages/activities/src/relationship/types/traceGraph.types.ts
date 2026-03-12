/**
 * Trace Graph Type Definitions
 * For Right Panel trace visualization components
 */

// Import from local relationship.types (moved from @sddp/features/relationship)
import type { RelationType } from './relationship.types';

// ============================================
// Primary Flow Types ()
// ============================================

/**
 * Primary flow types in the trace graph
 * Represents the main lineage path of an entity
 */
export type PrimaryFlowType =
  | 'bornFrom' // Conversation → Requirement
  | 'parentRequirement' // Requirement → Spec
  | 'generatedFrom'; // Spec → Artifact

/**
 * Primary flow node in trace graph
 */
export interface PrimaryFlowNode {
  id?: string;
  entityId?: string;
  entityType: 'Conversation' | 'Requirement' | 'Spec' | 'GlossaryTerm' | 'Artifact' | 'Task' | 'SiblingGroup';
  label: string;
  code?: string;
  flowType?: PrimaryFlowType | null; // null for root node
  depth?: number;
  isCurrent: boolean;
  metadata?: Record<string, unknown>;
}

/**
 * Primary flow connection
 */
export interface PrimaryFlowEdge {
  from: string;
  to: string;
  flowType: PrimaryFlowType;
  label: string;
}

/**
 * Primary Flow API response DTO (matches backend PrimaryFlowNodeDto)
 */
export interface PrimaryFlowNodeDto {
  id: string;
  entityType: string;
  label: string;
  code: string | null;
  depth: number;
  isCurrent: boolean;
}

// ============================================
// Trace Graph Configuration
// ============================================

/**
 * Trace graph display configuration
 */
export interface TraceGraphConfig {
  /** Show primary flow (Conversation → Requirement → Spec → Artifact) */
  showPrimaryFlow: boolean;
  /** Show relationship connections */
  showRelationships: boolean;
  /** Show deprecated/invalidated relationships */
  showDeprecated: boolean;
  /** Maximum depth for relationship traversal */
  maxDepth: number;
  /** Expanded sections */
  expandedSections: Set<string>;
}

/**
 * Default trace graph configuration
 */
export const DEFAULT_TRACE_GRAPH_CONFIG: TraceGraphConfig = {
  showPrimaryFlow: true,
  showRelationships: true,
  showDeprecated: false,
  maxDepth: 3,
  expandedSections: new Set(['primaryFlow', 'relationships']),
};

// ============================================
// Trace Graph State
// ============================================

/**
 * Trace graph UI state
 */
export interface TraceGraphState {
  /** Current configuration */
  config: TraceGraphConfig;
  /** Selected node ID */
  selectedNodeId: string | null;
  /** Hovered node ID */
  hoveredNodeId: string | null;
  /** Primary flow data */
  primaryFlow: PrimaryFlowNode[];
  /** Loading state */
  loading: boolean;
  /** Error message */
  error: string | null;
}

/**
 * Initial trace graph state
 */
export const INITIAL_TRACE_GRAPH_STATE: TraceGraphState = {
  config: DEFAULT_TRACE_GRAPH_CONFIG,
  selectedNodeId: null,
  hoveredNodeId: null,
  primaryFlow: [],
  loading: false,
  error: null,
};

// ============================================
// Related Items Types
// ============================================

/**
 * Related item for list display
 */
export interface RelatedItem {
  id: string;
  entityType: 'Conversation' | 'Requirement' | 'Spec' | 'GlossaryTerm' | 'Artifact' | 'Task';
  code: string;
  title: string;
  relationshipType: RelationType;
  direction: 'incoming' | 'outgoing';
  createdAt: string;
  isDeprecated: boolean;
}

/**
 * Related items grouped by type
 */
export interface GroupedRelatedItems {
  conversations: RelatedItem[];
  requirements: RelatedItem[];
  specs: RelatedItem[];
  glossaryTerms: RelatedItem[];
  artifacts: RelatedItem[];
}

// ============================================
// Primary Flow Icons & Styles
// ============================================

export interface PrimaryFlowStyle {
  icon: string;
  color: string;
  bgColor: string;
  label: string;
}

export const PRIMARY_FLOW_STYLES: Record<PrimaryFlowType, PrimaryFlowStyle> = {
  bornFrom: {
    icon: 'message-circle',
    color: 'text-purple-600 dark:text-purple-400',
    bgColor: 'bg-purple-50 dark:bg-purple-950',
    label: 'Born From',
  },
  parentRequirement: {
    icon: 'clipboard-list',
    color: 'text-green-600 dark:text-green-400',
    bgColor: 'bg-green-50 dark:bg-green-950',
    label: 'Parent Requirement',
  },
  generatedFrom: {
    icon: 'file-code',
    color: 'text-blue-600 dark:text-blue-400',
    bgColor: 'bg-blue-50 dark:bg-blue-950',
    label: 'Generated From',
  },
};

// ============================================
// Utility Functions
// ============================================

/**
 * Get entity type icon name
 */
export function getEntityTypeIcon(
  entityType: 'Conversation' | 'Requirement' | 'Spec' | 'GlossaryTerm' | 'Artifact' | 'Task'
): string {
  const icons: Record<string, string> = {
    Conversation: 'message-circle',
    Requirement: 'clipboard-list',
    Spec: 'file-text',
    GlossaryTerm: 'book',
    Artifact: 'code',
    Task: 'clipboard-list',
  };
  return icons[entityType] || 'file';
}

/**
 * Group related items by entity type
 */
export function groupRelatedItems(items: RelatedItem[]): GroupedRelatedItems {
  return {
    conversations: items.filter((i) => i.entityType === 'Conversation'),
    requirements: items.filter((i) => i.entityType === 'Requirement'),
    specs: items.filter((i) => i.entityType === 'Spec'),
    glossaryTerms: items.filter((i) => i.entityType === 'GlossaryTerm'),
    artifacts: items.filter((i) => i.entityType === 'Artifact'),
  };
}

/**
 * Sort related items by relationship type priority
 */
export function sortRelatedItems(items: RelatedItem[]): RelatedItem[] {
  const priority: Record<string, number> = {
    DependsOn: 1,
    Implements: 2,
    Extends: 3,
    Supersedes: 4,
    EvolvesFrom: 5,
    ConflictsWith: 6,
    Replaces: 7,
    Affects: 8,
  };

  return [...items].sort((a, b) => {
    const aPriority = priority[a.relationshipType] || 99;
    const bPriority = priority[b.relationshipType] || 99;
    return aPriority - bPriority;
  });
}
