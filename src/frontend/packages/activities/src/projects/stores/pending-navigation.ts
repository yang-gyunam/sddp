/**
 * Pending Entity Navigation
 *
 * One-shot mechanism for cross-type navigation in project pages.
 * When TraceGraphSection navigates to a different entity type page,
 * it stores the target entity ID here. The target page consumes it
 * on mount to auto-select the entity.
 *
 * Flow:
 * 1. setPendingEntityId(entityId) — store target entity ID
 * 2. setSelectedNodePath('projectId/pageType') — switch page
 * 3. Target page: consumePendingEntityId() — get and clear entity ID
 */

let pendingEntityId: string | null = null;

/**
 * Set pending entity ID (called before setSelectedNodePath)
 */
export function setPendingEntityId(entityId: string | null): void {
  pendingEntityId = entityId;
}

/**
 * Consume pending entity ID (one-shot: returns and clears)
 */
export function consumePendingEntityId(): string | null {
  const id = pendingEntityId;
  pendingEntityId = null;
  return id;
}
