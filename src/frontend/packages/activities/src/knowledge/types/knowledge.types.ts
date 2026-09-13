import type { JSONContent } from '@tiptap/core';

export type KnowledgePageStatus = 'Draft' | 'Published' | 'Archived';

export interface KnowledgePageTreeItem {
  id: string;
  parentId: string | null;
  title: string;
  slug: string;
  status: KnowledgePageStatus;
  position: number;
  revision: number;
  hasChildren: boolean;
  updatedBy: string;
  updatedAt: string;
}

export interface KnowledgePage {
  id: string;
  tenantId: string;
  projectId: string;
  parentId: string | null;
  title: string;
  slug: string;
  content: JSONContent;
  tags: string[];
  status: KnowledgePageStatus;
  position: number;
  revision: number;
  createdBy: string;
  updatedBy: string;
  publishedBy: string | null;
  publishedAt: string | null;
  archivedAt: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface KnowledgePageVersionSummary {
  id: string;
  revision: number;
  title: string;
  status: KnowledgePageStatus;
  changeSummary: string | null;
  createdBy: string;
  createdAt: string;
}

export interface KnowledgePageVersion extends KnowledgePageVersionSummary {
  pageId: string;
  slug: string;
  content: JSONContent;
  tags: string[];
}

export interface KnowledgeSearchResult {
  id: string;
  parentId: string | null;
  title: string;
  slug: string;
  summary: string;
  status: KnowledgePageStatus;
  tags: string[];
  revision: number;
  updatedBy: string;
  updatedAt: string;
}

export interface CreateKnowledgePageRequest {
  title: string;
  parentId?: string | null;
  template?: string | null;
}

export interface UpdateKnowledgePageRequest {
  title: string;
  slug: string;
  content: JSONContent;
  tags: string[];
  changeSummary?: string | null;
}

export type KnowledgeTemplate =
  | 'blank'
  | 'architecture-overview'
  | 'onboarding'
  | 'runbook'
  | 'troubleshooting'
  | 'guideline'
  | 'product-policy';

export interface FlattenedKnowledgePage extends KnowledgePageTreeItem {
  depth: number;
}
