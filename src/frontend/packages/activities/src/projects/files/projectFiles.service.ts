import { fetchWithAuth, type FetchOptions } from '../../shared/api';

interface FileOwner {
  id: string;
  name: string | null;
  avatarUrl: string | null;
}

export interface FileEntry {
  name: string;
  path: string;
  kind: 'directory' | 'file' | 'symlink' | 'submodule';
  sizeBytes: number | null;
  owners?: FileOwner[];
  lastChangedBy?: string;
  lastChangedByProfile?: FileOwner;
}
export interface FileTree {
  branch: string;
  commit: string;
  path: string;
  entries: FileEntry[];
}
interface FileTreeOptions {
  includeOwners?: boolean;
  includeLastChangedBy?: boolean;
}
export interface FileSearch {
  branch: string;
  commit: string;
  entries: FileEntry[];
  totalCount: number;
  nextSkip: number | null;
}
export interface FileCommit {
  commit: string;
  author: string;
  authoredAt: string;
  message: string;
  path: string;
  authorProfile?: FileOwner;
}
export interface FileContent {
  branch: string;
  commit: string;
  path: string;
  kind: FileEntry['kind'];
  sizeBytes: number;
  content: string | null;
  isBinary: boolean;
  tooLarge: boolean;
  maxBytes: number;
  lastChange: FileCommit | null;
}
export interface FileHistory {
  commit: string;
  items: FileCommit[];
  nextSkip: number | null;
}
export interface FileLink {
  id: string;
  type: 'Artifact' | 'Task' | 'Spec' | 'Requirement' | 'Conversation';
  title: string;
  code: string | null;
}
export interface FileRelations {
  owners: FileOwner[];
  items: FileLink[];
}

export function createProjectFilesService(
  projectId: string,
  scope: Pick<FetchOptions, 'tenantId' | 'beforeDispatch'> = {}
) {
  const read = <T>(
    action: string,
    path: string,
    revision?: string,
    query: Record<string, string> = {}
  ) => {
    const params = new URLSearchParams({ path, ...query });
    if (revision) params.set('revision', revision);
    return fetchWithAuth<T>(
      `/projects/${encodeURIComponent(projectId)}/files/${action}?${params}`,
      {
        ...scope,
        projectId,
      }
    );
  };
  return {
    search: (query: string, revision?: string, skip = 0) => {
      const params = new URLSearchParams({ q: query, skip: String(skip) });
      if (revision) params.set('revision', revision);
      return fetchWithAuth<FileSearch>(
        `/projects/${encodeURIComponent(projectId)}/files/search?${params}`,
        { ...scope, projectId }
      );
    },
    tree: (path = '', revision?: string, options: FileTreeOptions = {}) => {
      const query: Record<string, string> = {};
      if (options.includeOwners) query.includeOwners = 'true';
      if (options.includeLastChangedBy) query.includeLastChangedBy = 'true';
      return read<FileTree>('tree', path, revision, query);
    },
    content: (path: string, revision?: string) => read<FileContent>('content', path, revision),
    history: (path: string, revision?: string, skip = 0) =>
      read<FileHistory>('history', path, revision, { skip: String(skip) }),
    relations: (path: string) => read<FileRelations>('relations', path),
  };
}
export type ProjectFilesService = ReturnType<typeof createProjectFilesService>;

export function fileLanguage(path: string): string {
  const extension = path.split('.').pop()?.toLowerCase() ?? '';
  const languages: Record<string, string> = {
    cs: 'csharp',
    ts: 'typescript',
    tsx: 'typescript',
    js: 'javascript',
    jsx: 'javascript',
    json: 'json',
    md: 'markdown',
    html: 'html',
    svelte: 'html',
    css: 'css',
    scss: 'scss',
    yml: 'yaml',
    yaml: 'yaml',
    xml: 'xml',
    csproj: 'xml',
    props: 'xml',
    sql: 'sql',
    py: 'python',
    sh: 'shell',
    go: 'go',
    rs: 'rust',
    java: 'java',
    txt: 'plaintext',
  };
  return languages[extension] ?? 'plaintext';
}
export function fileSize(bytes: number): string {
  return bytes < 1024
    ? `${bytes} B`
    : bytes < 1024 * 1024
      ? `${(bytes / 1024).toFixed(1)} KB`
      : `${(bytes / 1024 / 1024).toFixed(1)} MB`;
}
