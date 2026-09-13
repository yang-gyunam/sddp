<!-- Activity: Projects > Knowledge | Screen ID: PRJ-KNW-001 -->
<script lang="ts">
  import { onMount, tick } from 'svelte';
  import { SvelteMap, SvelteSet } from 'svelte/reactivity';
  import { generateHTML, mergeAttributes, Node, type Editor, type JSONContent } from '@tiptap/core';
  import StarterKit from '@tiptap/starter-kit';
  import { Table } from '@tiptap/extension-table';
  import { TableRow } from '@tiptap/extension-table-row';
  import { TableCell } from '@tiptap/extension-table-cell';
  import { TableHeader } from '@tiptap/extension-table-header';
  import { Badge, Button, CardGrid, Icon, IconButton, Input, Select, Spinner } from '@sddp/ui';
  import {
    CollapsibleGroup,
    PageBody,
    PageHeader,
    PageShell,
    Dropdown,
    SidebarDetailLayout,
    SIDEBAR_DETAIL_LAYOUT,
    SurfaceCard,
    toast,
  } from '@sddp/shell';
  import { getTabState, registerTabCloseGuard, setTabState } from '@sddp/shell';
  import { subscribeAuth } from '@sddp/shell/auth/stores';
  import { createProjectCapabilityStore } from '../../shared/state/projectCapabilities';
  import DetailHeader from '../../shared/components/idioms/DetailHeader.svelte';
  import DetailTitle from '../../shared/components/idioms/DetailTitle.svelte';
  import { StatCard } from '../../shared/components/idioms';
  import { ApiRequestError } from '../../shared/api/fetchWithAuth';
  import type { ProjectDetail } from '../../projects/types';
  import {
    archiveKnowledgePage,
    createKnowledgePage,
    getKnowledgePage,
    getKnowledgeTree,
    getKnowledgeVersion,
    getKnowledgeVersions,
    moveKnowledgePage,
    publishKnowledgePage,
    restoreKnowledgePage,
    restoreKnowledgeVersion,
    searchKnowledge,
    updateKnowledgePage,
  } from '../services';
  import type {
    FlattenedKnowledgePage,
    KnowledgePage,
    KnowledgePageStatus,
    KnowledgePageTreeItem,
    KnowledgePageVersionSummary,
    KnowledgePageVersion,
    KnowledgeSearchResult,
    KnowledgeTemplate,
  } from '../types';
  import {
    KnowledgeConflictSection,
    KnowledgeEditorSection,
    KnowledgeHistorySection,
    KnowledgeSidebar,
  } from './sections';
  import {
    createKnowledgeWorkspaceState,
    readKnowledgeTabState,
    type KnowledgeTabDraft,
    type KnowledgeTabMode,
    type KnowledgeTabState,
  } from '../state';

  interface Props {
    projectId: string;
    projectName?: string;
    project?: ProjectDetail;
    tabId?: string;
    class?: string;
  }

  let { projectId, projectName = '', project, tabId = '', class: className = '' }: Props = $props();

  const instanceId = $props.id();

  const entityMention = Node.create({
    name: 'sddpEntityMention',
    group: 'inline',
    inline: true,
    atom: true,
    addAttributes() {
      return {
        entityType: {
          default: 'Spec',
          parseHTML: (element) => element.getAttribute('data-sddp-entity-type') ?? 'Spec',
          renderHTML: (attributes) => ({
            'data-sddp-entity-type': attributes.entityType,
          }),
        },
        entityId: {
          default: '',
          parseHTML: (element) => element.getAttribute('data-sddp-entity-id') ?? '',
          renderHTML: (attributes) => ({
            'data-sddp-entity-id': attributes.entityId,
          }),
        },
      };
    },
    parseHTML() {
      return [{ tag: 'span[data-sddp-entity-id]' }];
    },
    renderHTML({ HTMLAttributes }) {
      return [
        'span',
        mergeAttributes(HTMLAttributes, {
          class: 'sddp-entity-mention',
        }),
        `@${HTMLAttributes.entityType}`,
      ];
    },
  });

  const renderingExtensions = [
    StarterKit.configure({ heading: { levels: [1, 2, 3] } }),
    Table.configure({ resizable: true }),
    TableRow,
    TableCell,
    TableHeader,
    entityMention,
  ];

  const templateOptions = [
    { value: 'blank', label: 'Blank page' },
    { value: 'architecture-overview', label: 'Architecture overview' },
    { value: 'onboarding', label: 'Onboarding' },
    { value: 'runbook', label: 'Runbook' },
    { value: 'troubleshooting', label: 'Troubleshooting' },
    { value: 'guideline', label: 'Guideline' },
    { value: 'product-policy', label: 'Product policy' },
  ];

  let tree = $state<KnowledgePageTreeItem[]>([]);
  let selectedPage = $state<KnowledgePage | null>(null);
  let loadingTree = $state(true);
  let loadingPage = $state(false);
  let saving = $state(false);
  let error = $state<string | null>(null);
  let query = $state('');
  let searchResults = $state<KnowledgeSearchResult[] | null>(null);
  let statusFilter = $state<KnowledgePageStatus | 'all'>('all');
  let searchTimer: ReturnType<typeof setTimeout> | undefined;
  let treeRequestGeneration = 0;
  let pageRequestGeneration = 0;
  let searchRequestGeneration = 0;
  let historyRequestGeneration = 0;

  let workspaceState = $state(createKnowledgeWorkspaceState());
  let editorDirty = $state(false);
  let editTitle = $state('');
  let editSlug = $state('');
  let editTags = $state('');
  let changeSummary = $state('');
  let editorHtml = $state('');
  let editorJson = $state<JSONContent>({ type: 'doc', content: [] });
  let editorRef:
    | {
        getEditor(): Editor | null;
        getJSON(): JSONContent | undefined;
        focus(): void;
      }
    | undefined = $state();

  let createTitle = $state('');
  let createParentId = $state('__root__');
  let createTemplate = $state<KnowledgeTemplate>('blank');

  let moving = $state(false);
  let moveParentId = $state('__root__');
  let movePosition = $state(10);

  let isTabStateRestored = false;
  let pendingDraft = $state<KnowledgeTabDraft | null>(null);

  let versions = $state<KnowledgePageVersionSummary[]>([]);
  let historyPageId = $state<string | null>(null);
  let comparedVersion = $state<KnowledgePageVersion | null>(null);
  let comparedVersionHtml = $state('');
  let referenceType = $state('Spec');
  let referenceId = $state('');
  let addingReference = $state(false);

  const pageMeta = $derived(project?.name || projectName || undefined);
  const creating = $derived(workspaceState.primary.kind === 'create');
  const editing = $derived(
    workspaceState.primary.kind === 'page' && workspaceState.primary.mode === 'edit'
  );
  const metadataDirty = $derived(
    selectedPage !== null &&
      (editTitle !== selectedPage.title ||
        editSlug !== selectedPage.slug ||
        JSON.stringify(parseTags(editTags)) !== JSON.stringify(selectedPage.tags) ||
        changeSummary.trim().length > 0)
  );
  const dirty = $derived(editing && (editorDirty || metadataDirty));
  const tabStateKey = $derived(tabId || `knowledge-hub-${instanceId}`);
  const tabMode = $derived<KnowledgeTabMode>(
    workspaceState.primary.kind === 'page'
      ? workspaceState.primary.mode
      : workspaceState.primary.kind
  );
  const showHistory = $derived(workspaceState.history.kind !== 'closed');
  const versionsLoading = $derived(workspaceState.history.kind === 'loading');
  const capabilities = createProjectCapabilityStore();
  let capabilityIdentity = $state({ userId: '', tenantId: '', revision: 0 });
  const projectPermissions = $derived(
    $capabilities.userId === capabilityIdentity.userId &&
      $capabilities.data?.tenantId === capabilityIdentity.tenantId &&
      $capabilities.data?.projectId === projectId &&
      $capabilities.data.canWrite
      ? $capabilities.data.permissions
      : []
  );
  const canCreate = $derived(projectPermissions.includes('knowledge:create'));
  const canUpdate = $derived(projectPermissions.includes('knowledge:update'));
  const canPublish = $derived(projectPermissions.includes('knowledge:publish'));
  const canArchive = $derived(projectPermissions.includes('knowledge:archive'));
  // Equal-valued parent prop updates must not reload permissions and recreate a focused button.
  const capabilityScopeKey = $derived(
    JSON.stringify([
      projectId,
      capabilityIdentity.tenantId,
      capabilityIdentity.userId,
      capabilityIdentity.revision,
    ])
  );
  $effect(() => {
    const [scopedProject, tenantId, userId] = JSON.parse(capabilityScopeKey) as [
      string,
      string,
      string,
      number,
    ];
    void capabilities.load(scopedProject, tenantId, userId);
    return () => capabilities.clear();
  });
  const flattenedTree = $derived(flattenTree(tree));
  const knowledgeCounts = $derived({
    all: flattenedTree.length,
    Draft: flattenedTree.filter((page) => page.status === 'Draft').length,
    Published: flattenedTree.filter((page) => page.status === 'Published').length,
    Archived: flattenedTree.filter((page) => page.status === 'Archived').length,
  });
  const filteredTree = $derived(
    statusFilter === 'all'
      ? flattenedTree
      : flattenedTree.filter((page) => page.status === statusFilter)
  );
  const filteredSearchResults = $derived(
    searchResults === null
      ? null
      : statusFilter === 'all'
        ? searchResults
        : searchResults.filter((page) => page.status === statusFilter)
  );
  const createParentOptions = $derived([
    { value: '__root__', label: 'Top level' },
    ...flattenedTree
      .filter((page) => page.status !== 'Archived')
      .map((page) => ({
        value: page.id,
        label: `${'— '.repeat(page.depth)}${page.title}`,
      })),
  ]);
  const descendantPageIds = $derived(
    selectedPage ? collectDescendantPageIds(tree, selectedPage.id) : new SvelteSet<string>()
  );
  const moveParentOptions = $derived([
    { value: '__root__', label: 'Top level' },
    ...flattenedTree
      .filter(
        (page) =>
          page.id !== selectedPage?.id &&
          !descendantPageIds.has(page.id) &&
          page.status !== 'Archived'
      )
      .map((page) => ({
        value: page.id,
        label: `${'— '.repeat(page.depth)}${page.title}`,
      })),
  ]);

  onMount(() => {
    const unsubscribeAuth = subscribeAuth((state) => {
      capabilities.clear();
      capabilityIdentity = {
        userId: state.isAuthenticated ? (state.user?.id ?? '') : '',
        tenantId: state.isAuthenticated ? (state.user?.tenantId ?? '') : '',
        revision: capabilityIdentity.revision + 1,
      };
    });
    let wasBlurred = false;
    const rememberBlur = () => {
      wasBlurred = true;
    };
    const refreshCapabilities = () => {
      if (!wasBlurred) return;
      wasBlurred = false;
      capabilityIdentity = { ...capabilityIdentity, revision: capabilityIdentity.revision + 1 };
    };
    window.addEventListener('blur', rememberBlur);
    window.addEventListener('focus', refreshCapabilities);
    const restored = restoreTabState();
    void loadTree(restored?.selectedPageId ?? undefined);

    // The shell asks before it closes this tab; unsaved work answers for itself.
    const unregisterCloseGuard = registerTabCloseGuard(tabStateKey, {
      isDirty: () => dirty || (creating && createTitle.trim().length > 0),
      describe: () => (creating ? 'New Knowledge page' : (selectedPage?.title ?? 'Knowledge')),
      save: () => (creating ? createPage() : savePage()),
    });

    return () => {
      unsubscribeAuth();
      window.removeEventListener('blur', rememberBlur);
      window.removeEventListener('focus', refreshCapabilities);
      capabilities.clear();
      unregisterCloseGuard();
      if (searchTimer) clearTimeout(searchTimer);
      treeRequestGeneration += 1;
      pageRequestGeneration += 1;
      searchRequestGeneration += 1;
    };
  });

  /**
   * The shell recreates this component whenever the tab moves between split groups, so anything the
   * user has typed but not saved lives in tab state rather than in component state.
   */
  function restoreTabState(): KnowledgeTabState | null {
    isTabStateRestored = true;
    const saved = tabStateKey ? readKnowledgeTabState(getTabState(tabStateKey)) : null;
    if (!saved) return null;

    statusFilter = saved.statusFilter;
    query = saved.query;
    if (query.trim().length >= 2) void runSearch(query.trim());

    if (saved.mode === 'create') {
      const createDraft = saved.createDraft;
      createTitle = createDraft?.title ?? '';
      createParentId = createDraft?.parentId ?? '__root__';
      createTemplate = createDraft?.template ?? 'blank';
      workspaceState = {
        primary: { kind: 'create' },
        history: { kind: 'closed' },
        conflict: { kind: 'none' },
      };
      return { ...saved, selectedPageId: null };
    }

    pendingDraft = saved.mode === 'edit' ? saved.draft : null;
    return saved;
  }

  $effect(() => {
    if (!tabStateKey || !isTabStateRestored) return;

    // A restored draft is only in `pendingDraft` until the page it belongs to finishes loading.
    // This effect runs during that window with nothing selected, so writing the empty state here
    // would destroy the durable copy before the restore can put it back — and leave nothing behind
    // if the load then fails.
    const parked = pendingDraft;
    const liveDraft =
      selectedPage && editing && dirty
        ? {
            pageId: selectedPage.id,
            revision: selectedPage.revision,
            title: editTitle,
            slug: editSlug,
            tags: editTags,
            changeSummary,
            content: editorJson,
          }
        : null;

    setTabState<KnowledgeTabState>(tabStateKey, {
      selectedPageId: parked?.pageId ?? selectedPage?.id ?? null,
      mode: parked ? 'edit' : tabMode,
      query,
      statusFilter,
      draft: liveDraft ?? parked,
      createDraft: creating
        ? { title: createTitle, parentId: createParentId, template: createTemplate }
        : null,
    });
  });

  /**
   * A restored draft was written against one revision. If the server has moved past it, the draft
   * must not quietly become an edit of the newer revision — that is how a save silently discards
   * someone else's work.
   *
   * Showing the conflict is not enough on its own: `savePage` sends `selectedPage.revision` as
   * `If-Match`, so leaving the freshly fetched page selected would make the server accept the
   * stale content and drop every revision in between. The edited page stays pinned to the
   * revision the draft was written against, exactly as the rejected-save path does, so a save
   * re-conflicts until the user rebases through the conflict UI on purpose.
   */
  function applyRestoredDraft(page: KnowledgePage, draft: KnowledgeTabDraft): void {
    const localHtml = contentToHtml(draft.content);
    const stale = draft.revision !== page.revision;

    if (stale) selectedPage = { ...page, revision: draft.revision };
    editTitle = draft.title;
    editSlug = draft.slug;
    editTags = draft.tags;
    changeSummary = draft.changeSummary;
    editorJson = draft.content;
    editorHtml = localHtml;
    editorDirty = true;
    workspaceState = {
      primary: { kind: 'page', mode: 'edit' },
      history: { kind: 'closed' },
      conflict: stale
        ? {
            kind: 'active',
            serverPage: page,
            serverHtml: contentToHtml(page.content),
            localHtml,
          }
        : { kind: 'none' },
    };
  }

  function flattenTree(items: KnowledgePageTreeItem[]): FlattenedKnowledgePage[] {
    const byParent = new SvelteMap<string | null, KnowledgePageTreeItem[]>();
    const ids = new SvelteSet(items.map((item) => item.id));
    for (const item of items) {
      const parentKey = item.parentId && ids.has(item.parentId) ? item.parentId : null;
      const children = byParent.get(parentKey) ?? [];
      children.push(item);
      byParent.set(parentKey, children);
    }

    for (const children of byParent.values()) {
      children.sort((a, b) => a.position - b.position || a.title.localeCompare(b.title));
    }

    const result: FlattenedKnowledgePage[] = [];
    const append = (parentId: string | null, depth: number) => {
      for (const item of byParent.get(parentId) ?? []) {
        result.push({ ...item, depth });
        append(item.id, depth + 1);
      }
    };
    append(null, 0);
    return result;
  }

  function collectDescendantPageIds(
    items: KnowledgePageTreeItem[],
    ancestorId: string
  ): Set<string> {
    const childrenByParent = new SvelteMap<string, string[]>();
    for (const item of items) {
      if (!item.parentId) continue;
      const childIds = childrenByParent.get(item.parentId) ?? [];
      childIds.push(item.id);
      childrenByParent.set(item.parentId, childIds);
    }

    const descendants = new SvelteSet<string>();
    const pending = [...(childrenByParent.get(ancestorId) ?? [])];
    while (pending.length > 0) {
      const pageId = pending.pop()!;
      if (descendants.has(pageId)) continue;
      descendants.add(pageId);
      pending.push(...(childrenByParent.get(pageId) ?? []));
    }
    return descendants;
  }

  function parseTags(value: string): string[] {
    return value
      .split(',')
      .map((tag) => tag.trim())
      .filter(Boolean);
  }

  function contentToHtml(content: JSONContent): string {
    return generateHTML(content, renderingExtensions);
  }

  function statusVariant(status: KnowledgePageStatus): 'success' | 'warning' | 'secondary' {
    if (status === 'Published') return 'success';
    if (status === 'Draft') return 'warning';
    return 'secondary';
  }

  async function loadTree(preferredPageId?: string): Promise<void> {
    const requestGeneration = ++treeRequestGeneration;
    const selectionGeneration = pageRequestGeneration;
    const requestedProjectId = projectId;
    const targetId = preferredPageId ?? selectedPage?.id;
    loadingTree = true;
    error = null;
    try {
      const loadedTree = await getKnowledgeTree(requestedProjectId, true);
      if (requestGeneration !== treeRequestGeneration || requestedProjectId !== projectId) {
        return;
      }

      tree = loadedTree;
      if (selectionGeneration !== pageRequestGeneration) return;

      if (targetId && loadedTree.some((page) => page.id === targetId)) {
        await selectPage(targetId, true);
      } else {
        invalidatePageRequest();
        selectedPage = null;
        pendingDraft = null;
        // A tree load that finds no page must not throw away a half-written new page form.
        if (workspaceState.primary.kind !== 'create') {
          workspaceState = createKnowledgeWorkspaceState();
        }
        editorDirty = false;
        resetHistoryOwnership();
      }
    } catch (cause) {
      if (requestGeneration === treeRequestGeneration && requestedProjectId === projectId) {
        error = cause instanceof Error ? cause.message : 'Could not load Knowledge pages.';
      }
    } finally {
      if (requestGeneration === treeRequestGeneration && requestedProjectId === projectId) {
        loadingTree = false;
      }
    }
  }

  async function selectPage(pageId: string, force = false): Promise<void> {
    if (!force && dirty && !window.confirm('Discard unsaved changes?')) return;
    const requestGeneration = ++pageRequestGeneration;
    const requestedProjectId = projectId;
    loadingPage = true;
    error = null;
    try {
      const page = await getKnowledgePage(requestedProjectId, pageId, true);
      if (requestGeneration !== pageRequestGeneration || requestedProjectId !== projectId) {
        return;
      }

      selectedPage = page;
      editTitle = page.title;
      editSlug = page.slug;
      editTags = page.tags.join(', ');
      changeSummary = '';
      editorJson = page.content;
      editorHtml = contentToHtml(page.content);
      workspaceState = {
        primary: { kind: 'page', mode: 'read' },
        history: { kind: 'closed' },
        conflict: { kind: 'none' },
      };
      editorDirty = false;
      moving = false;
      resetHistoryOwnership();

      const draft = pendingDraft;
      pendingDraft = null;
      if (draft && draft.pageId === page.id) applyRestoredDraft(page, draft);
    } catch (cause) {
      if (requestGeneration === pageRequestGeneration && requestedProjectId === projectId) {
        error = cause instanceof Error ? cause.message : 'Could not load this page.';
      }
    } finally {
      if (requestGeneration === pageRequestGeneration && requestedProjectId === projectId) {
        loadingPage = false;
      }
    }
  }

  function invalidatePageRequest(): void {
    pageRequestGeneration += 1;
    loadingPage = false;
  }

  /**
   * Revision history belongs to one page. Dropping the owner and bumping the generation makes
   * every in-flight history/compare response for the previous page unusable, so a late response
   * can neither render against the new page nor lend it a revision number to restore.
   */
  function resetHistoryOwnership(): void {
    historyRequestGeneration += 1;
    historyPageId = null;
    versions = [];
    comparedVersion = null;
    comparedVersionHtml = '';
  }

  function ownsHistoryRequest(
    requestGeneration: number,
    requestedProjectId: string,
    requestedPageId: string
  ): boolean {
    return (
      requestGeneration === historyRequestGeneration &&
      requestedProjectId === projectId &&
      selectedPage?.id === requestedPageId
    );
  }

  function beginCreatePage(): void {
    if (!canCreate) return;
    if (dirty && !window.confirm('Discard unsaved changes?')) return;
    invalidatePageRequest();
    createParentId =
      selectedPage && selectedPage.status !== 'Archived' ? selectedPage.id : '__root__';
    createTitle = '';
    createTemplate = 'blank';
    selectedPage = null;
    workspaceState = {
      primary: { kind: 'create' },
      history: { kind: 'closed' },
      conflict: { kind: 'none' },
    };
    editorDirty = false;
    moving = false;
    resetHistoryOwnership();
  }

  function cancelCreatePage(): void {
    workspaceState = createKnowledgeWorkspaceState();
    createTitle = '';
    createParentId = '__root__';
    createTemplate = 'blank';
  }

  function closePage(): void {
    if (dirty && !window.confirm('Discard unsaved changes?')) return;
    invalidatePageRequest();
    selectedPage = null;
    workspaceState = createKnowledgeWorkspaceState();
    editorDirty = false;
    moving = false;
    resetHistoryOwnership();
  }

  function beginEdit(): void {
    if (!canUpdate || !selectedPage || selectedPage.status === 'Archived') return;
    const previousFocus = document.activeElement;
    workspaceState = {
      ...workspaceState,
      primary: { kind: 'page', mode: 'edit' },
      conflict: { kind: 'none' },
    };
    editorDirty = false;
    void tick().then(() => {
      // TipTap commands.focus schedules another animation frame, which can steal
      // focus after the user has already selected the title. Focus the mounted
      // view synchronously after Svelte updates, respecting a newer user choice.
      if (!editing || (document.activeElement !== previousFocus && document.activeElement !== document.body)) return;
      const editor = editorRef?.getEditor();
      if (editor && !editor.isDestroyed) editor.view.focus();
    });
  }

  function cancelEdit(): void {
    if (!selectedPage) return;
    if (dirty && !window.confirm('Discard unsaved changes?')) return;
    editTitle = selectedPage.title;
    editSlug = selectedPage.slug;
    editTags = selectedPage.tags.join(', ');
    editorJson = selectedPage.content;
    editorHtml = contentToHtml(selectedPage.content);
    changeSummary = '';
    workspaceState = {
      ...workspaceState,
      primary: { kind: 'page', mode: 'read' },
      conflict: { kind: 'none' },
    };
    editorDirty = false;
  }

  function handleEditorUpdate(): void {
    if (!editing) return;
    editorJson = editorRef?.getJSON() ?? editorJson;
    editorDirty = true;
  }

  async function savePage(): Promise<boolean> {
    if (!canUpdate || !selectedPage) return false;
    saving = true;
    error = null;
    try {
      const updated = await updateKnowledgePage(projectId, selectedPage.id, selectedPage.revision, {
        title: editTitle,
        slug: editSlug,
        content: editorRef?.getJSON() ?? editorJson,
        tags: parseTags(editTags),
        changeSummary: changeSummary.trim() || null,
      });
      selectedPage = updated;
      editTitle = updated.title;
      editSlug = updated.slug;
      editTags = updated.tags.join(', ');
      changeSummary = '';
      workspaceState = {
        ...workspaceState,
        primary: { kind: 'page', mode: 'read' },
        conflict: { kind: 'none' },
      };
      editorDirty = false;
      editorJson = updated.content;
      editorHtml = contentToHtml(updated.content);
      await loadTree(updated.id);
      toast.success('Knowledge page saved');
      return true;
    } catch (cause) {
      if (cause instanceof ApiRequestError && cause.errorCode === 'KNOWLEDGE_PAGE_EDIT_CONFLICT') {
        await loadEditConflict(cause);
      } else {
        error = cause instanceof Error ? cause.message : 'Could not save this page.';
      }
      toast.error(error ?? 'Could not save this page.');
      return false;
    } finally {
      saving = false;
    }
  }

  async function loadEditConflict(cause: ApiRequestError): Promise<void> {
    error = cause.message;
    const requestGeneration = ++pageRequestGeneration;
    const requestedProjectId = projectId;
    const requestedPageId = selectedPage!.id;
    const localHtml = contentToHtml(editorRef?.getJSON() ?? editorJson);
    try {
      const serverPage = await getKnowledgePage(requestedProjectId, requestedPageId, false);
      if (
        requestGeneration !== pageRequestGeneration ||
        requestedProjectId !== projectId ||
        selectedPage?.id !== requestedPageId
      ) {
        return;
      }
      workspaceState = {
        ...workspaceState,
        conflict: {
          kind: 'active',
          serverPage,
          serverHtml: contentToHtml(serverPage.content),
          localHtml,
        },
      };
    } catch {
      if (
        requestGeneration === pageRequestGeneration &&
        requestedProjectId === projectId &&
        selectedPage?.id === requestedPageId
      ) {
        workspaceState = { ...workspaceState, conflict: { kind: 'none' } };
        error = `${cause.message} The latest server revision could not be loaded.`;
      }
    }
  }

  function useServerConflictVersion(): void {
    const conflict = workspaceState.conflict;
    if (
      conflict.kind !== 'active' ||
      !window.confirm('Discard your local edits and load the server version?')
    ) {
      return;
    }

    selectedPage = conflict.serverPage;
    editTitle = conflict.serverPage.title;
    editSlug = conflict.serverPage.slug;
    editTags = conflict.serverPage.tags.join(', ');
    editorJson = conflict.serverPage.content;
    editorHtml = conflict.serverHtml;
    changeSummary = '';
    editorDirty = false;
    workspaceState = { ...workspaceState, conflict: { kind: 'none' } };
    error = null;
  }

  function rebaseLocalConflictVersion(): void {
    const conflict = workspaceState.conflict;
    if (conflict.kind !== 'active' || conflict.serverPage.status === 'Archived') return;
    selectedPage = conflict.serverPage;
    workspaceState = { ...workspaceState, conflict: { kind: 'none' } };
    error = null;
    editorDirty = true;
    toast.info('Local edits are ready to save on the latest revision');
  }

  async function createPage(): Promise<boolean> {
    if (!canCreate || !createTitle.trim()) return false;
    saving = true;
    error = null;
    try {
      const page = await createKnowledgePage(projectId, {
        title: createTitle.trim(),
        parentId: createParentId === '__root__' ? null : createParentId,
        template: createTemplate,
      });
      createTitle = '';
      createParentId = '__root__';
      createTemplate = 'blank';
      await loadTree(page.id);
      beginEdit();
      toast.success('Draft page created');
      return true;
    } catch (cause) {
      error = cause instanceof Error ? cause.message : 'Could not create the page.';
      toast.error(error);
      return false;
    } finally {
      saving = false;
    }
  }

  async function publishPage(): Promise<void> {
    if (!canPublish || !selectedPage) return;
    saving = true;
    try {
      const page = await publishKnowledgePage(projectId, selectedPage.id, selectedPage.revision);
      await loadTree(page.id);
      toast.success('Knowledge page published');
    } catch (cause) {
      error = cause instanceof Error ? cause.message : 'Could not publish the page.';
      toast.error(error);
    } finally {
      saving = false;
    }
  }

  async function archivePage(): Promise<void> {
    if (!canArchive || !selectedPage) return;
    const includeDescendants = window.confirm(
      'Archive descendant pages too? Choose Cancel to archive only this page.'
    );
    saving = true;
    try {
      const page = await archiveKnowledgePage(
        projectId,
        selectedPage.id,
        selectedPage.revision,
        includeDescendants
      );
      await loadTree(page.id);
      toast.success('Knowledge page archived');
    } catch (cause) {
      error = cause instanceof Error ? cause.message : 'Could not archive the page.';
      toast.error(error);
    } finally {
      saving = false;
    }
  }

  async function restorePage(): Promise<void> {
    if (!canArchive || !selectedPage) return;
    saving = true;
    try {
      const page = await restoreKnowledgePage(projectId, selectedPage.id, selectedPage.revision);
      await loadTree(page.id);
      toast.success('Knowledge page restored');
    } catch (cause) {
      error = cause instanceof Error ? cause.message : 'Could not restore the page.';
      toast.error(error);
    } finally {
      saving = false;
    }
  }

  function beginMove(): void {
    if (!canUpdate || !selectedPage) return;
    moveParentId = selectedPage.parentId ?? '__root__';
    movePosition = selectedPage.position;
    moving = true;
  }

  async function movePage(): Promise<void> {
    if (!canUpdate || !selectedPage) return;
    saving = true;
    try {
      const page = await moveKnowledgePage(
        projectId,
        selectedPage.id,
        selectedPage.revision,
        moveParentId === '__root__' ? null : moveParentId,
        movePosition
      );
      moving = false;
      await loadTree(page.id);
      toast.success('Knowledge page moved');
    } catch (cause) {
      error = cause instanceof Error ? cause.message : 'Could not move the page.';
      toast.error(error);
    } finally {
      saving = false;
    }
  }

  async function toggleHistory(): Promise<void> {
    if (!selectedPage) return;
    if (workspaceState.history.kind !== 'closed') {
      workspaceState = { ...workspaceState, history: { kind: 'closed' } };
      return;
    }
    if (historyPageId === selectedPage.id && versions.length > 0) {
      workspaceState = { ...workspaceState, history: { kind: 'ready' } };
      return;
    }

    const requestGeneration = ++historyRequestGeneration;
    const requestedProjectId = projectId;
    const requestedPageId = selectedPage.id;
    workspaceState = { ...workspaceState, history: { kind: 'loading' } };
    try {
      const loadedVersions = await getKnowledgeVersions(requestedProjectId, requestedPageId, true);
      if (!ownsHistoryRequest(requestGeneration, requestedProjectId, requestedPageId)) return;

      versions = loadedVersions;
      historyPageId = requestedPageId;
      workspaceState = { ...workspaceState, history: { kind: 'ready' } };
    } catch (cause) {
      if (!ownsHistoryRequest(requestGeneration, requestedProjectId, requestedPageId)) return;

      error = cause instanceof Error ? cause.message : 'Could not load page history.';
      workspaceState = { ...workspaceState, history: { kind: 'ready' } };
    }
  }

  async function compareVersion(revision: number): Promise<void> {
    if (!selectedPage || historyPageId !== selectedPage.id) return;

    const requestGeneration = ++historyRequestGeneration;
    const requestedProjectId = projectId;
    const requestedPageId = selectedPage.id;
    workspaceState = { ...workspaceState, history: { kind: 'loading' } };
    try {
      const version = await getKnowledgeVersion(
        requestedProjectId,
        requestedPageId,
        revision,
        true
      );
      if (!ownsHistoryRequest(requestGeneration, requestedProjectId, requestedPageId)) return;

      comparedVersion = version;
      comparedVersionHtml = contentToHtml(version.content);
    } catch (cause) {
      if (!ownsHistoryRequest(requestGeneration, requestedProjectId, requestedPageId)) return;

      error = cause instanceof Error ? cause.message : 'Could not load this revision.';
    } finally {
      if (ownsHistoryRequest(requestGeneration, requestedProjectId, requestedPageId)) {
        workspaceState = { ...workspaceState, history: { kind: 'ready' } };
      }
    }
  }

  function insertReference(): void {
    if (!referenceId.trim()) return;
    const editor = editorRef?.getEditor();
    if (!editor) return;
    editor
      .chain()
      .focus()
      .insertContent({
        type: 'sddpEntityMention',
        attrs: {
          entityType: referenceType,
          entityId: referenceId.trim(),
        },
      })
      .run();
    referenceId = '';
    addingReference = false;
    editorDirty = true;
  }

  async function restoreVersion(revision: number): Promise<void> {
    if (!canUpdate || !selectedPage || historyPageId !== selectedPage.id) return;
    if (!window.confirm(`Restore revision ${revision} as a new revision?`)) return;
    saving = true;
    try {
      const page = await restoreKnowledgeVersion(
        projectId,
        selectedPage.id,
        revision,
        selectedPage.revision
      );
      resetHistoryOwnership();
      workspaceState = { ...workspaceState, history: { kind: 'closed' } };
      await loadTree(page.id);
      toast.success(`Revision ${revision} restored`);
    } catch (cause) {
      error = cause instanceof Error ? cause.message : 'Could not restore this revision.';
      toast.error(error);
    } finally {
      saving = false;
    }
  }

  async function runSearch(requestedQuery: string): Promise<void> {
    const requestGeneration = ++searchRequestGeneration;
    const requestedProjectId = projectId;
    try {
      const results = await searchKnowledge(requestedProjectId, requestedQuery);
      if (
        requestGeneration !== searchRequestGeneration ||
        requestedProjectId !== projectId ||
        requestedQuery !== query.trim()
      ) {
        return;
      }
      searchResults = results;
    } catch (cause) {
      if (
        requestGeneration === searchRequestGeneration &&
        requestedProjectId === projectId &&
        requestedQuery === query.trim()
      ) {
        error = cause instanceof Error ? cause.message : 'Knowledge search failed.';
      }
    }
  }

  function handleSearchInput(event: Event): void {
    query = (event.target as HTMLInputElement).value;
    if (searchTimer) clearTimeout(searchTimer);
    const requestedQuery = query.trim();
    if (requestedQuery.length < 2) {
      searchRequestGeneration += 1;
      searchResults = null;
      return;
    }

    searchTimer = setTimeout(() => void runSearch(requestedQuery), 250);
  }
</script>

<PageShell class={className}>
  <PageHeader title="Knowledge" meta={pageMeta} loading={loadingTree || loadingPage || saving}>
    {#snippet actions()}
      <IconButton
        icon="refresh-cw"
        size="sm"
        variant="ghost"
        title="Refresh Knowledge"
        onclick={() => void loadTree(selectedPage?.id)}
      />
      {#if canCreate}
        <IconButton
          icon="plus"
          size="sm"
          variant="ghost"
          title="New Page"
          onclick={beginCreatePage}
        />
      {/if}
    {/snippet}
  </PageHeader>

  <PageBody noPadding scrollable={false}>
    <SidebarDetailLayout
      sidebarWidth={SIDEBAR_DETAIL_LAYOUT.sidebarWidth}
      minSidebarWidth={SIDEBAR_DETAIL_LAYOUT.minSidebarWidth}
      maxSidebarWidth={SIDEBAR_DETAIL_LAYOUT.maxSidebarWidth}
    >
      {#snippet sidebar()}
        <KnowledgeSidebar
          {query}
          {statusFilter}
          counts={knowledgeCounts}
          loading={loadingTree}
          searchResults={filteredSearchResults}
          pages={filteredTree}
          selectedPageId={selectedPage?.id}
          onSearchInput={handleSearchInput}
          onStatusFilterChange={(status) => (statusFilter = status)}
          onSelectPage={(pageId) => void selectPage(pageId)}
        />
      {/snippet}

      {#snippet header()}
        {#if creating}
          <DetailHeader>
            <DetailTitle title="New Page" />
            {#snippet actions()}
              <IconButton
                icon="check"
                variant="success"
                size="sm"
                title="Create"
                disabled={!canCreate || !createTitle.trim()}
                loading={saving}
                onclick={createPage}
              />
              <IconButton
                icon="x"
                variant="ghost"
                size="sm"
                title="Cancel"
                onclick={cancelCreatePage}
              />
            {/snippet}
          </DetailHeader>
        {:else if selectedPage}
          <DetailHeader>
            {#snippet leading()}
              <Icon name="file-text" size="md" class="text-[var(--color-info-600)]" />
            {/snippet}
            {#if editing}
              <DetailTitle title="Edit Page" code={'r' + selectedPage.revision} />
            {:else}
              <DetailTitle title={selectedPage.title} code={'r' + selectedPage.revision}>
                <Badge variant={statusVariant(selectedPage.status)} size="sm">
                  {selectedPage.status}
                </Badge>
              </DetailTitle>
            {/if}
            {#snippet actions()}
              {#if editing}
                <IconButton
                  icon="check"
                  variant="success"
                  size="sm"
                  title="Save page"
                  disabled={!canUpdate || !dirty}
                  loading={saving}
                  onclick={savePage}
                />
                <IconButton
                  icon="x"
                  variant="ghost"
                  size="sm"
                  title="Cancel editing"
                  onclick={cancelEdit}
                />
              {:else}
                {#if selectedPage!.status !== 'Archived' && canUpdate}
                  <IconButton
                    icon="edit"
                    variant="ghost"
                    size="sm"
                    title="Edit page"
                    onclick={beginEdit}
                  />
                {/if}

                <Dropdown position="bottom-right">
                  {#snippet trigger({ toggle, open })}
                    <IconButton
                      onclick={toggle}
                      ariaHasPopup
                      ariaExpanded={open}
                      icon="more-vertical"
                      variant="ghost"
                      size="sm"
                      title="More page actions"
                    />
                  {/snippet}
                  <div class="min-w-[180px] py-1">
                    {#if (selectedPage!.status === 'Draft' && canPublish) || (selectedPage!.status === 'Archived' && canArchive)}
                      <div
                        class="px-3 py-1 text-[0.625rem] font-semibold uppercase tracking-wider text-[var(--color-text-muted)]"
                      >
                        Status
                      </div>
                      {#if selectedPage!.status === 'Draft' && canPublish}
                        <Button
                          variant="unstyled"
                          class="flex w-full items-center gap-2 px-3 py-1.5 text-left text-xs font-medium text-[var(--color-success-600)] transition-colors hover:bg-[var(--color-bg-tertiary)]"
                          disabled={saving}
                          onclick={publishPage}
                        >
                          <Icon name="check" size="xs" />
                          Publish
                        </Button>
                      {/if}
                      {#if selectedPage!.status === 'Archived' && canArchive}
                        <Button
                          variant="unstyled"
                          class="flex w-full items-center gap-2 px-3 py-1.5 text-left text-xs font-medium text-[var(--color-success-600)] transition-colors hover:bg-[var(--color-bg-tertiary)]"
                          disabled={saving}
                          onclick={restorePage}
                        >
                          <Icon name="refresh-cw" size="xs" />
                          Restore page
                        </Button>
                      {/if}
                      <div class="my-1 border-t border-[var(--color-border-secondary)]"></div>
                    {/if}
                    {#if selectedPage!.status !== 'Archived' && canUpdate}
                      <Button
                        variant="unstyled"
                        class="flex w-full items-center gap-2 px-3 py-1.5 text-left text-xs font-medium text-[var(--color-text-secondary)] transition-colors hover:bg-[var(--color-bg-tertiary)]"
                        onclick={beginMove}
                      >
                        <Icon name="git-branch" size="xs" />
                        Move page
                      </Button>
                    {/if}
                    <Button
                      variant="unstyled"
                      class="flex w-full items-center gap-2 px-3 py-1.5 text-left text-xs font-medium text-[var(--color-text-secondary)] transition-colors hover:bg-[var(--color-bg-tertiary)]"
                      onclick={toggleHistory}
                    >
                      <Icon name="history" size="xs" />
                      Revision history
                    </Button>
                    {#if selectedPage!.status !== 'Archived' && canArchive}
                      <Button
                        variant="unstyled"
                        class="flex w-full items-center gap-2 px-3 py-1.5 text-left text-xs font-medium text-[var(--color-error-600)] transition-colors hover:bg-[var(--color-bg-tertiary)]"
                        onclick={archivePage}
                      >
                        <Icon name="archive" size="xs" />
                        Archive page
                      </Button>
                    {/if}
                  </div>
                </Dropdown>
                <IconButton icon="x" variant="ghost" size="sm" title="Close" onclick={closePage} />
              {/if}
            {/snippet}
          </DetailHeader>
        {:else}
          <DetailHeader>
            <DetailTitle title="Overview" />
          </DetailHeader>
        {/if}
      {/snippet}

      <main class="flex min-h-0 flex-1 flex-col bg-[var(--color-bg-primary)]">
        {#if error}
          <div
            class="m-4 mb-0 flex flex-shrink-0 items-start gap-2 rounded border border-[var(--color-error-500)]/30 bg-[var(--color-error-500)]/10 p-3 text-sm text-[var(--color-error-600)]"
          >
            <Icon name="alert-circle" size="sm" />
            <span class="flex-1">{error}</span>
            <IconButton icon="x" size="sm" title="Dismiss" onclick={() => (error = null)} />
          </div>
        {/if}

        {#if loadingPage}
          <div class="flex flex-1 items-center justify-center"><Spinner size="lg" /></div>
        {:else if creating}
          <div class="flex-1 min-h-0 overflow-y-auto p-4">
            <div class="space-y-5">
              <CollapsibleGroup title="Basic Information" variant="plain" expanded>
                <div class="flex max-w-3xl flex-col gap-4 pl-5 pt-2 pb-3">
                  <Select
                    bind:value={createParentId}
                    options={createParentOptions}
                    label="Parent"
                  />
                  <Select bind:value={createTemplate} options={templateOptions} label="Template" />
                  <Input
                    id={'knowledge-create-title-' + projectId}
                    bind:value={createTitle}
                    label="Title"
                    hint="Enter a clear, unique page title"
                    maxlength={200}
                    required
                    onkeydown={(event) => {
                      if (event.key === 'Enter' && createTitle.trim()) void createPage();
                    }}
                  />
                </div>
              </CollapsibleGroup>
            </div>
          </div>
        {:else if selectedPage}
          <article class="flex-1 min-h-0 overflow-y-auto">
            <div class="mx-auto max-w-5xl p-4">
              {#if workspaceState.conflict.kind === 'active'}
                <KnowledgeConflictSection
                  conflict={workspaceState.conflict}
                  extensions={[entityMention]}
                  onLoadServer={useServerConflictVersion}
                  onApplyMine={rebaseLocalConflictVersion}
                />
              {/if}

              <KnowledgeEditorSection
                page={selectedPage}
                {editing}
                {dirty}
                bind:editTitle
                bind:editSlug
                bind:editTags
                bind:changeSummary
                {editorHtml}
                bind:editorRef
                {moving}
                bind:moveParentId
                bind:movePosition
                {moveParentOptions}
                bind:addingReference
                bind:referenceType
                bind:referenceId
                extensions={[entityMention]}
                onEditorUpdate={handleEditorUpdate}
                onCancelMove={() => (moving = false)}
                onMove={() => void movePage()}
                onInsertReference={insertReference}
              />

              {#if showHistory}
                <KnowledgeHistorySection
                  page={selectedPage}
                  {versions}
                  loading={versionsLoading}
                  {comparedVersion}
                  {comparedVersionHtml}
                  currentVersionHtml={editorHtml}
                  canRestore={canUpdate}
                  extensions={[entityMention]}
                  onCompare={(revision) => void compareVersion(revision)}
                  onRestore={(revision) => void restoreVersion(revision)}
                />
              {/if}
            </div>
          </article>
        {:else}
          <div class="flex-1 min-h-0 overflow-y-auto p-3 space-y-3">
            <CardGrid cols={4} gap="md">
              <StatCard
                title="Total Pages"
                value={knowledgeCounts.all}
                icon="file-text"
                iconColor="blue-500"
              />
              <StatCard
                title="Draft"
                value={knowledgeCounts.Draft}
                icon="edit"
                iconColor="gray-500"
              />
              <StatCard
                title="Published"
                value={knowledgeCounts.Published}
                icon="check-circle"
                iconColor="green-500"
              />
              <StatCard
                title="Archived"
                value={knowledgeCounts.Archived}
                icon="archive"
                iconColor="purple-500"
              />
            </CardGrid>

            <SurfaceCard padding="lg">
              <h3 class="mb-3 text-sm font-semibold text-[var(--color-text-primary)]">
                About Knowledge
              </h3>
              <div class="space-y-2 text-sm text-[var(--color-text-secondary)]">
                <p>
                  Knowledge is the project wiki for architecture, onboarding, runbooks, and team
                  decisions.
                </p>
                <div class="flex items-start gap-2">
                  <Icon name="file-text" size="sm" class="mt-0.5 flex-shrink-0 text-gray-500" />
                  <span><strong>Draft</strong> - Work in progress, editable</span>
                </div>
                <div class="flex items-start gap-2">
                  <Icon name="check-circle" size="sm" class="mt-0.5 flex-shrink-0 text-green-500" />
                  <span><strong>Published</strong> - Shared project knowledge</span>
                </div>
                <div class="flex items-start gap-2">
                  <Icon name="history" size="sm" class="mt-0.5 flex-shrink-0 text-blue-500" />
                  <span><strong>Revisioned</strong> - Every saved change remains traceable</span>
                </div>
              </div>
            </SurfaceCard>

            {#if flattenedTree.length === 0}
              <div class="py-8 text-center">
                <Icon
                  name="book-open"
                  size="xl"
                  class="mx-auto mb-4 text-[var(--color-text-tertiary)] opacity-50"
                />
                <h3 class="mb-2 text-lg font-medium text-[var(--color-text-primary)]">
                  Start your project Knowledge space
                </h3>
                <p class="mb-4 text-sm text-[var(--color-text-secondary)]">
                  Create the first page for your project wiki.
                </p>
                {#if canCreate}
                  <Button variant="primary" onclick={beginCreatePage}>Create first page</Button>
                {/if}
              </div>
            {/if}
          </div>
        {/if}
      </main>
    </SidebarDetailLayout>
  </PageBody>
</PageShell>
