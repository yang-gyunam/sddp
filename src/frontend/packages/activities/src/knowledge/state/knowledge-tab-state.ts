import type { JSONContent } from '@tiptap/core';
import type { KnowledgePageStatus, KnowledgeTemplate } from '../types';

export type KnowledgeTabMode = 'overview' | 'create' | 'read' | 'edit';

/**
 * An edit that exists only in the tab. `revision` is the page revision the edit was written
 * against, so a restored draft can be told apart from one the server has since moved past.
 */
export interface KnowledgeTabDraft {
  pageId: string;
  revision: number;
  title: string;
  slug: string;
  tags: string;
  changeSummary: string;
  content: JSONContent;
}

export interface KnowledgeTabCreateDraft {
  title: string;
  parentId: string;
  template: KnowledgeTemplate;
}

export interface KnowledgeTabState {
  selectedPageId: string | null;
  mode: KnowledgeTabMode;
  query: string;
  statusFilter: KnowledgePageStatus | 'all';
  draft: KnowledgeTabDraft | null;
  createDraft: KnowledgeTabCreateDraft | null;
}

const TAB_MODES: readonly KnowledgeTabMode[] = ['overview', 'create', 'read', 'edit'];
const STATUS_FILTERS: readonly (KnowledgePageStatus | 'all')[] = [
  'all',
  'Draft',
  'Published',
  'Archived',
];

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null;
}

function readDraft(value: unknown): KnowledgeTabDraft | null {
  if (!isRecord(value)) return null;
  const { pageId, revision, title, slug, tags, changeSummary, content } = value;
  if (typeof pageId !== 'string' || pageId.length === 0) return null;
  if (typeof revision !== 'number' || !Number.isFinite(revision)) return null;
  if (!isRecord(content)) return null;

  return {
    pageId,
    revision,
    title: typeof title === 'string' ? title : '',
    slug: typeof slug === 'string' ? slug : '',
    tags: typeof tags === 'string' ? tags : '',
    changeSummary: typeof changeSummary === 'string' ? changeSummary : '',
    content: content as JSONContent,
  };
}

function readCreateDraft(value: unknown): KnowledgeTabCreateDraft | null {
  if (!isRecord(value)) return null;
  const { title, parentId, template } = value;
  if (typeof title !== 'string') return null;

  return {
    title,
    parentId: typeof parentId === 'string' && parentId.length > 0 ? parentId : '__root__',
    template: (typeof template === 'string' ? template : 'blank') as KnowledgeTemplate,
  };
}

/**
 * Tab state outlives the component that wrote it and is only as trustworthy as the last session
 * that ran in this browser. Anything that does not read back as the current shape is dropped
 * rather than fed to the editor.
 */
export function readKnowledgeTabState(value: unknown): KnowledgeTabState | null {
  if (!isRecord(value)) return null;

  const mode = value.mode;
  const statusFilter = value.statusFilter;
  const selectedPageId = value.selectedPageId;

  return {
    selectedPageId: typeof selectedPageId === 'string' && selectedPageId ? selectedPageId : null,
    mode: TAB_MODES.includes(mode as KnowledgeTabMode) ? (mode as KnowledgeTabMode) : 'overview',
    query: typeof value.query === 'string' ? value.query : '',
    statusFilter: STATUS_FILTERS.includes(statusFilter as KnowledgePageStatus | 'all')
      ? (statusFilter as KnowledgePageStatus | 'all')
      : 'all',
    draft: readDraft(value.draft),
    createDraft: readCreateDraft(value.createDraft),
  };
}
