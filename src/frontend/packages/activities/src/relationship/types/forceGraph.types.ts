/**
 * Force-Directed Graph Type Definitions
 * D3.js force simulation types for interactive relationship visualization
 */

import type { GraphNode, GraphEdge, EntityType, RelationType } from './relationship.types';
import type { SimulationNodeDatum, SimulationLinkDatum } from 'd3-force';

// ============================================
// Simulation Types (D3 extensions)
// ============================================

/**
 * Graph node extended with D3 simulation properties
 */
export interface SimulationNode extends GraphNode, SimulationNodeDatum {
  x: number;
  y: number;
  fx: number | null;
  fy: number | null;
  vx: number;
  vy: number;
}

/**
 * Graph edge extended with D3 simulation link properties
 */
export interface SimulationLink extends SimulationLinkDatum<SimulationNode> {
  id: string;
  source: SimulationNode;
  target: SimulationNode;
  type: RelationType;
  typeLabel: string;
}

// ============================================
// Layout Configuration
// ============================================

/**
 * Available graph layout modes
 */
export type GraphLayoutMode = 'force' | 'tree' | 'radial';

/**
 * Force simulation configuration parameters
 */
export interface ForceGraphConfig {
  chargeStrength: number;
  linkDistance: number;
  collideRadius: number;
  centerStrength: number;
  alphaDecay: number;
  velocityDecay: number;
}

/**
 * Default force graph configuration
 */
export const DEFAULT_FORCE_GRAPH_CONFIG: ForceGraphConfig = {
  chargeStrength: -300,
  linkDistance: 120,
  collideRadius: 40,
  centerStrength: 0.1,
  alphaDecay: 0.0228,
  velocityDecay: 0.4,
};

// ============================================
// Filter State
// ============================================

/**
 * Graph filter state for showing/hiding node and edge types
 */
export interface GraphFilterState {
  entityTypes: Set<EntityType>;
  relationTypes: Set<RelationType>;
}

/**
 * Default graph filters (all enabled)
 */
export const DEFAULT_GRAPH_FILTERS: GraphFilterState = {
  entityTypes: new Set<EntityType>(['Spec', 'Requirement', 'Conversation', 'GlossaryTerm']),
  relationTypes: new Set<RelationType>([
    'Supersedes',
    'EvolvesFrom',
    'Extends',
    'ConflictsWith',
    'DependsOn',
    'Implements',
    'Replaces',
    'Affects',
  ]),
};

// ============================================
// Edge Style Mapping
// ============================================

export type EdgeLineStyle = 'solid' | 'dashed' | 'dotted';

/**
 * Edge dash array for SVG stroke-dasharray
 */
export const EDGE_LINE_STYLES: Record<RelationType, EdgeLineStyle> = {
  DependsOn: 'solid',
  Implements: 'solid',
  Extends: 'solid',
  ConflictsWith: 'dashed',
  Replaces: 'dashed',
  Supersedes: 'dashed',
  EvolvesFrom: 'dotted',
  Affects: 'dotted',
};

/**
 * SVG stroke-dasharray values for edge styles
 */
export const EDGE_DASH_ARRAYS: Record<EdgeLineStyle, string> = {
  solid: 'none',
  dashed: '8 4',
  dotted: '3 3',
};

// ============================================
// Node Color Map (hex values for SVG)
// ============================================

export const NODE_COLORS: Record<EntityType, string> = {
  Spec: '#3b82f6',
  Requirement: '#22c55e',
  Conversation: '#a855f7',
  GlossaryTerm: '#f59e0b',
  Artifact: '#06b6d4',
  Task: '#14b8a6',
};

export const EDGE_COLORS: Record<RelationType, string> = {
  Supersedes: '#3b82f6',
  EvolvesFrom: '#6366f1',
  Extends: '#22c55e',
  ConflictsWith: '#ef4444',
  DependsOn: '#f59e0b',
  Implements: '#10b981',
  Replaces: '#a855f7',
  Affects: '#6b7280',
};

// ============================================
// Utility: Convert graph data to simulation data
// ============================================

/**
 * Convert GraphNode[] to SimulationNode[]
 */
export function toSimulationNodes(nodes: GraphNode[]): SimulationNode[] {
  return nodes.map((node) => ({
    ...node,
    x: 0,
    y: 0,
    fx: null,
    fy: null,
    vx: 0,
    vy: 0,
  }));
}

/**
 * Convert GraphEdge[] to SimulationLink[] (source/target resolved after simulation init)
 */
export function toSimulationLinks(
  edges: GraphEdge[],
  nodes: SimulationNode[]
): SimulationLink[] {
  const nodeMap = new Map(nodes.map((n) => [n.id, n]));
  return edges
    .filter((e) => nodeMap.has(e.sourceId) && nodeMap.has(e.targetId))
    .map((edge) => ({
      id: edge.id,
      source: nodeMap.get(edge.sourceId)!,
      target: nodeMap.get(edge.targetId)!,
      type: edge.type,
      typeLabel: edge.typeLabel,
    }));
}
