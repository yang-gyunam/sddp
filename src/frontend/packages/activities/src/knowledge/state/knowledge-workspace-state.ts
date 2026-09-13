import type { KnowledgePage } from '../types';

export type KnowledgePrimaryState =
  { kind: 'overview' } | { kind: 'create' } | { kind: 'page'; mode: 'read' | 'edit' };

export type KnowledgeHistoryState = { kind: 'closed' } | { kind: 'loading' } | { kind: 'ready' };

export type KnowledgeConflictState =
  | { kind: 'none' }
  | {
      kind: 'active';
      serverPage: KnowledgePage;
      serverHtml: string;
      localHtml: string;
    };

export interface KnowledgeWorkspaceState {
  primary: KnowledgePrimaryState;
  history: KnowledgeHistoryState;
  conflict: KnowledgeConflictState;
}

export function createKnowledgeWorkspaceState(): KnowledgeWorkspaceState {
  return {
    primary: { kind: 'overview' },
    history: { kind: 'closed' },
    conflict: { kind: 'none' },
  };
}
