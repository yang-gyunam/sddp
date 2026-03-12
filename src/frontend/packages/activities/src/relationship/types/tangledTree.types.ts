/**
 * Tangled Tree Traceability Map Type Definitions
 * Types for the hierarchical traceability visualization with cross-links.
 * Backend DTO mapping: TraceabilityMapDto, TraceabilityNodeDto, TraceabilityCrossLinkDto
 */

import type { EntityType, RelationType } from './relationship.types';

// ============================================
// Backend DTO Types (API Response)
// ============================================

/**
 * Node from backend TraceabilityNodeDto.
 * Represents a single entity in the traceability hierarchy.
 */
export interface TraceabilityNode {
  id: string;
  entityType: EntityType;
  label: string;
  code: string | null;
  status: string | null;
  layer: number; // 0=Conversation, 1=Requirement, 2=Spec, 3=Task/Artifact
  parentId: string | null;
}

/**
 * Cross-link from backend TraceabilityCrossLinkDto.
 * Represents a non-hierarchical relationship between nodes.
 */
export interface TraceabilityCrossLink {
  id: string;
  sourceId: string;
  targetId: string;
  type: RelationType;
  typeLabel: string;
}

/**
 * Stats from backend TraceabilityMapStatsDto.
 * Summary counts for the traceability map.
 */
export interface TraceabilityMapStats {
  conversationCount: number;
  requirementCount: number;
  specCount: number;
  taskCount: number;
  artifactCount: number;
  crossLinkCount: number;
}

/**
 * Full API response from GET /api/projects/{projectId}/traceability-map
 */
export interface TraceabilityMapData {
  nodes: TraceabilityNode[];
  crossLinks: TraceabilityCrossLink[];
  stats: TraceabilityMapStats;
}

// ============================================
// Layout Engine Output Types
// ============================================

/**
 * A node after layout calculation with x/y coordinates.
 */
export interface PositionedNode {
  id: string;
  entityType: EntityType;
  label: string;
  code: string | null;
  status: string | null;
  layer: number;
  x: number;
  y: number;
  /** Height contributed by bundles passing through this node (metro lines) */
  bundleHeight: number;
}

/**
 * A hierarchical (parent-child) edge with pre-computed SVG path.
 */
export interface HierarchyEdge {
  sourceId: string;
  targetId: string;
  path: string; // SVG path d attribute
}

/**
 * A cross-link path with pre-computed curved SVG path.
 */
export interface CrossLinkPath {
  id: string;
  sourceId: string;
  targetId: string;
  type: RelationType;
  typeLabel: string;
  path: string; // SVG path d attribute (curved)
}

/**
 * Complete layout result from the tangled tree layout engine.
 */
export interface TangledTreeLayoutResult {
  nodes: PositionedNode[];
  hierarchyEdges: HierarchyEdge[];
  crossLinkPaths: CrossLinkPath[];
  /** Metro-style bundled edges (when bundled layout is active, hierarchyEdges is empty) */
  bundledEdges: BundledEdge[];
  /** Layout bounding box */
  layoutWidth: number;
  layoutHeight: number;
}

// ============================================
// Bundled Layout Types (Nitaku-style)
// ============================================

/** Configuration for the bundled tangled tree layout */
export interface TangledTreeLayoutConfig {
  padding: number;
  nodeHeight: number;
  nodeWidth: number;
  bundleWidth: number;
  levelYPadding: number;
  metroD: number;
  minFamilyHeight: number;
  /** Arc corner radius */
  c: number;
}

/** Default config — tuned for longer labels (Korean + code strings) */
export const DEFAULT_TANGLED_TREE_CONFIG: TangledTreeLayoutConfig = {
  padding: 40,
  nodeHeight: 32,
  nodeWidth: 220,
  bundleWidth: 14,
  levelYPadding: 24,
  metroD: 4,
  minFamilyHeight: 32,
  c: 16,
};

/** A bundled edge path — metro-style SVG path */
export interface BundledEdge {
  sourceId: string;
  targetId: string;
  path: string;
  bundleId: string;
  color: string;
}

// ============================================
// Detail Panel Types
// ============================================

/** An entity opened in the in-page detail panel */
export interface DetailPanelEntity {
  id: string;
  entityType: EntityType;
  label: string;
}

// ============================================
// Filter State
// ============================================

/**
 * UI filter state for the tangled tree visualization.
 */
export interface TangledTreeFilterState {
  entityTypes: Set<EntityType>;
  showCrossLinks: boolean;
  showLabels: boolean;
  orientation: 'horizontal' | 'vertical';
}
