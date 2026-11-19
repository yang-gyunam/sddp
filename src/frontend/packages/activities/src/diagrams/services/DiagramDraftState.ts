import { getTabState, setTabState } from '@sddp/shell';

export function diagramDraftKey(tabId: string | undefined, projectId: string, diagramId: string) {
  return tabId || `diagram-${projectId}-${diagramId}`;
}

export function diagramDraftScope(
  tenantId: string,
  userId: string,
  projectId: string,
  diagramId: string
) {
  return JSON.stringify([tenantId, userId, projectId, diagramId]);
}

export function clearDiagramDraft(key: string, scope: string): void {
  const state = getTabState<{
    diagramWorkspace?: { scope: string };
    diagramWorkspace2?: { scope: string };
  }>(key);
  if (!state) return;
  const clearLegacy = state.diagramWorkspace?.scope === scope;
  const clearCurrent = state.diagramWorkspace2?.scope === scope;
  if (!clearLegacy && !clearCurrent) return;
  if (clearLegacy) delete state.diagramWorkspace;
  if (clearCurrent) delete state.diagramWorkspace2;
  setTabState(key, state);
}
