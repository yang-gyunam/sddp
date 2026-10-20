import { SvelteSet } from 'svelte/reactivity';
import type {
  FileCommit,
  FileContent,
  FileEntry,
  FileHistory,
  FileRelations,
  FileSearch,
  ProjectFilesService,
} from './projectFiles.service';

export interface FileSelection {
  path: string;
  revision?: string;
  revisionPath?: string;
}
const message = (error: unknown) =>
  error instanceof Error ? error.message : 'Unable to load repository data.';

/** One instance per editor tab. Async completions are scoped to both repository and selection. */
export class FileViewer {
  branch = $state('');
  tip = $state('');
  directories = $state<Record<string, FileEntry[]>>({});
  expanded = $state<Record<string, boolean>>({ '': true });
  directoryErrors = $state<Record<string, string>>({});
  directoryLoading = $state<Record<string, boolean>>({});
  selection = $state<FileSelection>({ path: '' });
  content = $state<FileContent | null>(null);
  history = $state<FileHistory | null>(null);
  relations = $state<FileRelations | null>(null);
  loading = $state(false);
  contentLoading = $state(false);
  historyLoading = $state(false);
  relationsLoading = $state(false);
  error = $state('');
  contentError = $state('');
  historyError = $state('');
  relationsError = $state('');
  searchQuery = $state('');
  searchResults = $state<FileSearch | null>(null);
  searchLoading = $state(false);
  searchError = $state('');
  private searchGeneration = 0;
  private generation = 0;
  private selectionGeneration = 0;
  private contentGeneration = 0;
  constructor(private readonly api: ProjectFilesService) {}

  async initialize(saved: FileSelection = { path: '' }, expandedPaths: string[] = []) {
    const generation = ++this.generation;
    ++this.selectionGeneration;
    ++this.contentGeneration;
    ++this.searchGeneration;
    this.searchResults = null;
    this.searchLoading = false;
    this.loading = true;
    this.error = '';
    this.tip = '';
    this.directories = {};
    this.expanded = { '': true };
    this.directoryErrors = {};
    this.directoryLoading = {};
    this.content = null;
    this.history = null;
    this.relations = null;
    try {
      const tree = await this.api.tree();
      if (generation !== this.generation) return;
      this.branch = tree.branch;
      this.tip = tree.commit;
      this.directories = { '': tree.entries };
      const folders = new SvelteSet(expandedPaths);
      if (saved.path) {
        const parents = saved.path.split('/');
        parents.pop();
        for (let i = 1; i <= parents.length; i++) folders.add(parents.slice(0, i).join('/'));
      }
      await Promise.all([...folders].filter(Boolean).map((path) => this.toggleDirectory(path)));
      if (generation !== this.generation) return;
      if (saved.path) {
        await this.select(saved);
      } else this.selection = { path: '' };
      if (this.searchQuery.trim()) await this.search();
    } catch (error) {
      if (generation === this.generation) this.error = message(error);
    } finally {
      if (generation === this.generation) this.loading = false;
    }
  }

  async refresh() {
    const expanded = Object.entries(this.expanded)
      .filter(([path, open]) => path && open)
      .map(([path]) => path);
    await this.initialize({ ...this.selection }, expanded);
  }

  setSearchQuery(query: string) {
    this.searchQuery = query;
    ++this.searchGeneration;
    this.searchResults = null;
    this.searchError = '';
    this.searchLoading = !!query.trim();
  }

  async search(more = false) {
    const query = this.searchQuery.trim();
    const commit = this.tip;
    if (!query || !commit) {
      this.searchLoading = false;
      return;
    }
    if (more && (this.searchLoading || this.searchResults?.nextSkip == null)) return;
    const skip = more ? this.searchResults!.nextSkip! : 0;
    const generation = ++this.searchGeneration;
    this.searchLoading = true;
    this.searchError = '';
    try {
      const result = await this.api.search(query, commit, skip);
      if (generation !== this.searchGeneration) return;
      this.searchResults = {
        ...result,
        entries: more
          ? [...(this.searchResults?.entries ?? []), ...result.entries]
          : result.entries,
      };
    } catch (error) {
      if (generation === this.searchGeneration) this.searchError = message(error);
    } finally {
      if (generation === this.searchGeneration) this.searchLoading = false;
    }
  }

  async selectSearchResult(path: string) {
    await this.select({ path });
  }

  async retryDirectory(path: string) {
    if (this.directoryLoading[path]) return;
    delete this.directories[path];
    this.expanded[path] = false;
    await this.toggleDirectory(path);
  }

  async toggleDirectory(path: string) {
    this.expanded[path] = !this.expanded[path];
    if (!this.expanded[path] || this.directories[path] || this.directoryLoading[path]) return;
    const generation = this.generation;
    this.directoryLoading[path] = true;
    delete this.directoryErrors[path];
    try {
      const tree = await this.api.tree(path, this.tip);
      if (generation === this.generation) this.directories[path] = tree.entries;
    } catch (error) {
      if (generation === this.generation) this.directoryErrors[path] = message(error);
    } finally {
      if (generation === this.generation) this.directoryLoading[path] = false;
    }
  }

  async select(selection: FileSelection) {
    const generation = ++this.selectionGeneration;
    this.selection = { ...selection };
    this.history = null;
    this.relations = null;
    this.historyError = '';
    this.relationsError = '';
    this.historyLoading = true;
    this.relationsLoading = true;
    await Promise.all([
      this.showVersion(
        selection.revision
          ? { commit: selection.revision, path: selection.revisionPath ?? selection.path }
          : null
      ),
      this.loadHistory(generation),
      this.loadRelations(generation),
      this.revealParents(selection.path),
    ]);
  }

  private async revealParents(path: string) {
    // URL restoration selects a file after initialization, without saved tab state.
    // Reveal its current path just as opening it through the tree would do.
    const segments = path.split('/');
    segments.pop();
    const parents = segments.map((_, index) => segments.slice(0, index + 1).join('/'));
    await Promise.all(
      parents
        .filter((parent) => !this.expanded[parent])
        .map((parent) => this.toggleDirectory(parent))
    );
  }

  async showVersion(version: Pick<FileCommit, 'commit' | 'path'> | null) {
    const generation = ++this.contentGeneration;
    this.selection = {
      path: this.selection.path,
      revision: version?.commit,
      revisionPath: version?.path,
    };
    this.content = null;
    this.contentError = '';
    this.contentLoading = true;
    try {
      const content = await this.api.content(
        version?.path ?? this.selection.path,
        version?.commit ?? this.tip
      );
      if (generation === this.contentGeneration) this.content = content;
    } catch (error) {
      if (generation === this.contentGeneration) this.contentError = message(error);
    } finally {
      if (generation === this.contentGeneration) this.contentLoading = false;
    }
  }

  async loadHistory(generation = this.selectionGeneration, more = false) {
    this.historyLoading = true;
    this.historyError = '';
    try {
      const history = await this.api.history(
        this.selection.path,
        this.tip,
        more ? (this.history?.nextSkip ?? 0) : 0
      );
      if (generation === this.selectionGeneration)
        this.history = {
          ...history,
          items: more ? [...(this.history?.items ?? []), ...history.items] : history.items,
        };
    } catch (error) {
      if (generation === this.selectionGeneration) this.historyError = message(error);
    } finally {
      if (generation === this.selectionGeneration) this.historyLoading = false;
    }
  }

  async loadRelations(generation = this.selectionGeneration) {
    this.relationsLoading = true;
    this.relationsError = '';
    try {
      const relations = await this.api.relations(this.selection.path);
      if (generation === this.selectionGeneration) this.relations = relations;
    } catch (error) {
      if (generation === this.selectionGeneration) this.relationsError = message(error);
    } finally {
      if (generation === this.selectionGeneration) this.relationsLoading = false;
    }
  }

  dispose() {
    ++this.generation;
    ++this.selectionGeneration;
    ++this.contentGeneration;
    ++this.searchGeneration;
  }
}
