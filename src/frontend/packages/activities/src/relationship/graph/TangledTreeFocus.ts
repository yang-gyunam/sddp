import type { EntityType } from '../types';

export const TRACEABILITY_DIMMED_OPACITY = 0.15;

/**
 * What the stats bar is focusing. Cross-links are not an entity type — they are the
 * relationships that jump between branches — so they sit alongside the entity types
 * rather than inside them.
 */
export type TraceabilityFocus = EntityType | 'CrossLink';

/** Sentinel the cross-link count button focuses on. */
export const CROSS_LINK_FOCUS = 'CrossLink' as const;

/**
 * Entity focus matches by type; cross-link focus matches any node taking part in a
 * cross-link, whatever its type.
 */
export function nodeMatchesFocus(
  entityType: EntityType,
  focus: TraceabilityFocus,
  hasCrossLink: boolean
): boolean {
  return focus === CROSS_LINK_FOCUS ? hasCrossLink : entityType === focus;
}

/**
 * Combines the persistent focus with the temporary hover focus.
 * Either focus may dim a node, while matching both keeps it fully visible.
 */
export function resolveTraceabilityNodeOpacity(
  entityType: EntityType,
  focus: TraceabilityFocus | null,
  isConnectedToHoveredNode: boolean | null,
  hasCrossLink = false
): number {
  if (focus !== null && !nodeMatchesFocus(entityType, focus, hasCrossLink)) {
    return TRACEABILITY_DIMMED_OPACITY;
  }

  if (isConnectedToHoveredNode === false) {
    return TRACEABILITY_DIMMED_OPACITY;
  }

  return 1;
}
