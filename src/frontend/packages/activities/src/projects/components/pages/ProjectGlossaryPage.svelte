<!-- Activity: Projects > Nav: Glossary (project-{id}-glossary) | Screen ID: PRJ-GLO-001 -->
<script lang="ts">
  import { tick, untrack } from 'svelte';
  import { SvelteSet } from 'svelte/reactivity';
  import { Icon, IconButton, Button, CardGrid, Checkbox, Spinner, ResizeHandle } from '@sddp/ui';
  import type { ComboboxOption } from '@sddp/ui';
  import { PageShell, PageHeader, PageBody, SidebarDetailLayout, SIDEBAR_DETAIL_LAYOUT, SurfaceCard, getTabState, setTabState, toast, Dropdown } from '@sddp/shell';
  import DetailHeader from '../../../shared/components/idioms/DetailHeader.svelte';
  import DetailTitle from '../../../shared/components/idioms/DetailTitle.svelte';
  import { StatCard } from '../../../shared/components/idioms';
  import { consumePendingEntityId } from '../../stores';
  import { getAuthState } from '@sddp/shell/auth';
  import type { ProjectDetail } from '../../types';
  import { getProjectService } from '../../services/ProjectService';
  import GlossarySidebar from '../../../glossary/components/sections/GlossarySidebar.svelte';
  import GlossaryForm from '../../../glossary/components/idioms/GlossaryForm.svelte';
  import GlossaryDetail from '../../../glossary/components/sections/GlossaryDetail.svelte';
  import GlossaryVersionDetail from '../../../glossary/components/idioms/GlossaryVersionDetail.svelte';
  import type { GlossaryTerm, GlossaryTermDetail, GlossaryTermVersion, TermCategory, GlossaryTermStatus, CreateGlossaryTermRequest, UpdateGlossaryTermRequest, GlossaryConflictResult, CategoryTermGroup, GlossaryFilterType, TermSummaryItem } from '../../../glossary/types';
  import { TERM_CATEGORY_STYLES, TERM_STATUS_STYLES, TERM_CATEGORIES } from '../../../glossary/types';
  import { createTerm, updateTerm, approveTerm, deprecateTerm, getTermById, detectConflicts, getTermVersionHistory } from '../../../glossary/services';
  import { searchSpecs } from '../../../specs/services/SpecService';
  import { searchConversations } from '../../../conversations/services/ConversationService';
  import { searchRequirements } from '../../../requirements/services/RequirementService';
  import { TraceGraphSection } from '../../../relationship/components/sections';
  import type { PrimaryFlowNode } from '../../../relationship/types';

  interface Props {
    projectId: string;
    projectName?: string;
    project?: ProjectDetail;
    tabId?: string;
    class?: string;
  }

  let { projectId, projectName = '', project, tabId = '', class: className = '' }: Props = $props();

  const pageTitle = 'Glossary';
  const pageMeta = $derived(project?.name || projectName || undefined);

  // State
  let allTerms = $state<GlossaryTerm[]>([]);
  let selectedTermId = $state<string | null>(null);
  let searchQuery = $state('');
  let filterType = $state<GlossaryFilterType>('all');
  let expandedCategories = new SvelteSet<TermCategory>(TERM_CATEGORIES);
  let loading = $state(false);
  let loadingMore = $state(false);
  let termsPageNumber = $state(1);
  let hasMoreTerms = $state(false);
  const TERMS_PAGE_SIZE = 20;
  let latestTermsRequestId = $state(0);
  let error = $state<string | null>(null);
  let viewMode = $state<'detail' | 'create' | 'edit'>('detail');
  let formLoading = $state(false);
  let conflictResult = $state<GlossaryConflictResult | null>(null);
  let conflictLoading = $state(false);
  let editingTerm = $state<GlossaryTermDetail | null>(null);
  let selectedTermDetail = $state<GlossaryTermDetail | null>(null);

  // Spec search for Combobox
  let specCandidates = $state<ComboboxOption[]>([]);
  let specSearchLoading = $state(false);
  let specSearchTimer: ReturnType<typeof setTimeout> | undefined;

  // Conversation search for Combobox
  let conversationCandidates = $state<ComboboxOption[]>([]);
  let conversationSearchLoading = $state(false);
  let conversationSearchTimer: ReturnType<typeof setTimeout> | undefined;

  // Requirement search for Combobox
  let requirementCandidates = $state<ComboboxOption[]>([]);
  let requirementSearchLoading = $state(false);
  let requirementSearchTimer: ReturnType<typeof setTimeout> | undefined;

  // Version history
  let versionHistory = $state<GlossaryTermVersion[]>([]);
  let versionHistoryLoading = $state(false);
  let selectedVersionIds = new SvelteSet<string>();
  let activeVersionTabId = $state<string | null>(null);

  // Member candidates for Owner selection
  let memberCandidates = $state<ComboboxOption[]>([]);

  // Sync from project prop when available
  $effect(() => {
    if (project?.members && project.members.length > 0) {
      memberCandidates = project.members.map((m) => ({
        value: m.userId,
        label: m.displayName,
        description: m.role,
      }));
    }
  });

  async function loadMemberCandidates(): Promise<void> {
    try {
      const authState = getAuthState();
      if (!authState.user?.tenantId) return;
      const { getProjectById } = await import('../../services/ProjectService');
      const detail = await getProjectById(authState.user.tenantId, projectId);
      memberCandidates = (detail.members ?? []).map((m) => ({
        value: m.userId,
        label: m.displayName,
        description: m.role,
      }));
    } catch {
      memberCandidates = [];
    }
  }

  interface ProjectGlossaryTabState {
    [key: string]: unknown;
    searchQuery: string;
    filterType: GlossaryFilterType;
    selectedTermId: string | null;
    expandedCategories: TermCategory[];
  }

  const tabStateKey = $derived(tabId || `project-${projectId}-glossary`);
  let restoredKey = $state<string | null>(null);
  let isRestored = $state(false);

  $effect(() => {
    if (!tabStateKey || restoredKey === tabStateKey) return;
    const saved = getTabState<ProjectGlossaryTabState>(tabStateKey);
    if (saved) {
      searchQuery = saved.searchQuery ?? '';
      filterType = saved.filterType ?? 'all';
      selectedTermId = saved.selectedTermId ?? null;
      if (saved.expandedCategories) {
        expandedCategories = new SvelteSet(saved.expandedCategories);
      }
    }
    restoredKey = tabStateKey;
    isRestored = true;
  });

  // Persist tab state
  $effect(() => {
    if (!tabStateKey || !isRestored) return;
    const state: ProjectGlossaryTabState = {
      searchQuery,
      filterType,
      selectedTermId,
      expandedCategories: Array.from(expandedCategories),
    };
    setTabState<ProjectGlossaryTabState>(tabStateKey, state);
  });

  // Selected term (derived)
  const selectedTerm = $derived(
    allTerms.find((t) => t.id === selectedTermId) ?? null
  );

  // Filtered terms
  const filteredTerms = $derived.by(() => {
    let filtered = allTerms;

    // Filter by status (filterType maps to status)
    if (filterType !== 'all') {
      const statusMap: Record<GlossaryFilterType, GlossaryTermStatus | null> = {
        all: null,
        draft: 'Draft',
        active: 'Active',
        deprecated: 'Deprecated',
      };
      const targetStatus = statusMap[filterType];
      if (targetStatus) {
        filtered = filtered.filter((t) => t.status === targetStatus);
      }
    }

    // Filter by search query
    if (searchQuery.trim()) {
      const query = searchQuery.toLowerCase();
      filtered = filtered.filter(
        (t) =>
          t.term.toLowerCase().includes(query) ||
          t.definition.toLowerCase().includes(query) ||
          (t.synonyms?.toLowerCase().includes(query) ?? false) ||
          (t.abbreviation?.toLowerCase().includes(query) ?? false)
      );
    }

    return filtered;
  });

  // Status counts for sidebar (GlossaryFilterType uses lowercase)
  const statusCounts = $derived<Record<GlossaryFilterType, number>>({
    all: allTerms.length,
    draft: allTerms.filter((t) => t.status === 'Draft').length,
    active: allTerms.filter((t) => t.status === 'Active').length,
    deprecated: allTerms.filter((t) => t.status === 'Deprecated').length,
  });

  // Transform filtered terms into category groups for GlossarySidebar
  const categoryGroups = $derived.by((): CategoryTermGroup[] => {
    const groups: CategoryTermGroup[] = [];

    for (const category of TERM_CATEGORIES) {
      const termsInCategory = filteredTerms.filter((t) => t.category === category);
      if (termsInCategory.length > 0) {
        const summaryItems: TermSummaryItem[] = termsInCategory.map((t) => ({
          id: t.id,
          term: t.term,
          abbreviation: t.abbreviation,
          category: t.category,
          status: t.status,
          version: t.version,
        }));
        groups.push({
          category,
          terms: summaryItems,
          totalCount: termsInCategory.length,
          expanded: expandedCategories.has(category),
        });
      }
    }

    return groups;
  });

  // Version history (exclude current version)
  const pastVersions = $derived(
    versionHistory.filter((v) => v.id !== selectedTerm?.id)
  );
  const selectedVersionList = $derived(
    pastVersions.filter((v) => selectedVersionIds.has(v.id))
  );
  const activeVersion = $derived(
    selectedVersionList.find((v) => v.id === activeVersionTabId) ?? selectedVersionList[0] ?? null
  );

  // Resize state for version comparison panel
  const VERSION_PANEL_MIN = 150;
  const VERSION_PANEL_MAX = 600;
  const VERSION_PANEL_DEFAULT = 280;
  let versionPanelHeight = $state(VERSION_PANEL_DEFAULT);
  let isResizingVersionPanel = $state(false);
  let resizeStartY = 0;
  let resizeStartHeight = 0;

  // NOTE: Primary flow traces upstream lineage via SourceSpecId/SourceConversationId/SourceRequirementId.
  // Future enhancement: "Term Usage" visualization showing where this term is referenced across specs/requirements.

  $effect(() => {
    const pendingId = consumePendingEntityId();
    if (pendingId) {
      selectedTermId = pendingId;
    }
    untrack(() => loadTerms());
  });

  async function loadTerms(reset: boolean = true): Promise<void> {
    if (!reset && (loading || loadingMore || !hasMoreTerms)) return;

    const requestId = ++latestTermsRequestId;
    const nextPageNumber = reset ? 1 : termsPageNumber + 1;

    if (reset) {
      loading = true;
      error = null;
      hasMoreTerms = false;
      termsPageNumber = 1;
    } else {
      loadingMore = true;
    }

    try {
      const authState = getAuthState();
      const tenantId = authState.user?.tenantId;
      if (!tenantId) {
        throw new Error('User not authenticated or missing tenant');
      }
      const service = getProjectService();
      service.setTenantId(tenantId);

      const response = await service.getProjectGlossaryTerms(
        projectId,
        nextPageNumber,
        TERMS_PAGE_SIZE
      );
      if (requestId !== latestTermsRequestId) return;

      const mapped = (response?.items ?? []).map((t) => ({
        id: t.id,
        tenantId: tenantId,
        projectId: projectId,
        term: t.term,
        abbreviation: t.abbreviation ?? null,
        definition: t.definition,
        category: t.category as GlossaryTerm['category'],
        status: t.status as GlossaryTerm['status'],
        version: t.version,
        synonyms: t.synonyms ?? null,
        createdAt: t.createdAt,
        updatedAt: t.updatedAt,
      }));

      allTerms = reset ? mapped : [...allTerms, ...mapped];
      termsPageNumber = response.pageNumber ?? response.page ?? nextPageNumber;
      hasMoreTerms = response.hasNextPage ?? allTerms.length < response.totalCount;
    } catch (err) {
      if (requestId !== latestTermsRequestId) return;
      console.error('Failed to load glossary terms:', err);
      toast.error(err instanceof Error ? err.message : 'Failed to load glossary terms');
      error = err instanceof Error ? err.message : 'Failed to load glossary terms';
      hasMoreTerms = false;
    } finally {
      if (requestId === latestTermsRequestId) {
        loading = false;
        loadingMore = false;
        await tick();
      }
    }
  }

  function handleSearch(query: string) {
    searchQuery = query;
  }

  function handleFilterChange(filter: GlossaryFilterType) {
    filterType = filter;
  }

  function handleLoadMoreTerms(): void {
    loadTerms(false);
  }

  function handleToggleCategory(category: string) {
    const cat = category as TermCategory;
    if (expandedCategories.has(cat)) {
      expandedCategories.delete(cat);
    } else {
      expandedCategories.add(cat);
    }
  }

  async function handleSelectTermById(termId: string) {
    selectedTermId = termId;
    viewMode = 'detail';
    editingTerm = null;
    conflictResult = null;
    selectedVersionIds.clear();
    activeVersionTabId = null;
    await loadTermDetail(termId);
    loadVersionHistory(termId);
  }

  function handleTraceNodeClick(node: PrimaryFlowNode): void {
    const nodeId = node.id ?? node.entityId;
    if (!nodeId) return;
    handleSelectTermById(nodeId);
  }

  async function loadTermDetail(termId: string) {
    try {
      const authState = getAuthState();
      const tenantId = authState.user?.tenantId;
      if (!tenantId) return;
      const detail = await getTermById(tenantId, projectId, termId);
      selectedTermDetail = detail;
    } catch (err) {
      console.error('Failed to load term detail:', err);
      selectedTermDetail = null;
    }
  }

  async function loadVersionHistory(termId: string): Promise<void> {
    versionHistoryLoading = true;
    try {
      const authState = getAuthState();
      if (!authState.user?.tenantId) return;
      versionHistory = await getTermVersionHistory(authState.user.tenantId, projectId, termId);
    } catch {
      versionHistory = [];
    } finally {
      versionHistoryLoading = false;
    }
  }

  function handleVersionToggle(versionId: string): void {
    if (selectedVersionIds.has(versionId)) {
      selectedVersionIds.delete(versionId);
      if (activeVersionTabId === versionId) {
        const remaining = [...selectedVersionIds];
        activeVersionTabId = remaining.length > 0 ? remaining[0]! : null;
      }
    } else {
      selectedVersionIds.add(versionId);
      activeVersionTabId = versionId;
    }
  }

  function handleRemoveVersionTab(versionId: string): void {
    selectedVersionIds.delete(versionId);
    if (activeVersionTabId === versionId) {
      const remaining = [...selectedVersionIds];
      activeVersionTabId = remaining.length > 0 ? remaining[0]! : null;
    }
  }

  function handleResizePointerDown(e: PointerEvent) {
    e.preventDefault();
    isResizingVersionPanel = true;
    resizeStartY = e.clientY;
    resizeStartHeight = versionPanelHeight;

    const target = e.target as HTMLElement;
    target.setPointerCapture(e.pointerId);

    document.addEventListener('pointermove', handleResizePointerMove);
    document.addEventListener('pointerup', handleResizePointerUp);
    document.body.style.cursor = 'row-resize';
    document.body.style.userSelect = 'none';
  }

  function handleResizePointerMove(e: PointerEvent) {
    if (!isResizingVersionPanel) return;
    const delta = resizeStartY - e.clientY;
    versionPanelHeight = Math.min(VERSION_PANEL_MAX, Math.max(VERSION_PANEL_MIN, resizeStartHeight + delta));
  }

  function handleResizePointerUp() {
    isResizingVersionPanel = false;
    document.removeEventListener('pointermove', handleResizePointerMove);
    document.removeEventListener('pointerup', handleResizePointerUp);
    document.body.style.cursor = '';
    document.body.style.userSelect = '';
  }

  function formatShortDate(dateStr: string): string {
    const d = new Date(dateStr);
    return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
  }


  function handleCloseTerm() {
    selectedTermId = null;
    selectedTermDetail = null;
    viewMode = 'detail';
    editingTerm = null;
    conflictResult = null;
    versionHistory = [];
    selectedVersionIds.clear();
    activeVersionTabId = null;
  }

  function handleCreateTerm() {
    selectedTermId = null;
    selectedTermDetail = null;
    viewMode = 'create';
    editingTerm = null;
    conflictResult = null;
    specCandidates = [];
    conversationCandidates = [];
    requirementCandidates = [];
    if (memberCandidates.length === 0) {
      loadMemberCandidates();
    }
  }

  async function handleEditTerm() {
    if (!selectedTerm) return;
    viewMode = 'edit';
    conflictResult = null;
    if (memberCandidates.length === 0) {
      loadMemberCandidates();
    }

    // Pre-populate spec candidates for Combobox
    if (selectedTermDetail?.sourceSpecId && selectedTermDetail?.sourceSpecCode) {
      specCandidates = [{
        value: selectedTermDetail.sourceSpecId,
        label: selectedTermDetail.sourceSpecTitle ?? selectedTermDetail.sourceSpecCode,
        description: selectedTermDetail.sourceSpecCode,
      }];
    } else {
      specCandidates = [];
    }

    // Pre-populate conversation candidates
    if (selectedTermDetail?.sourceConversationId && selectedTermDetail?.sourceConversationName) {
      const prefix = selectedTermDetail.sourceConversationType === 'Channel' ? '#' : '';
      conversationCandidates = [{
        value: selectedTermDetail.sourceConversationId,
        label: `${prefix}${selectedTermDetail.sourceConversationName}`,
        description: selectedTermDetail.sourceConversationType ?? undefined,
      }];
    } else {
      conversationCandidates = [];
    }

    // Pre-populate requirement candidates
    if (selectedTermDetail?.sourceRequirementId && (selectedTermDetail?.sourceRequirementCode || selectedTermDetail?.sourceRequirementTitle)) {
      requirementCandidates = [{
        value: selectedTermDetail.sourceRequirementId,
        label: `${selectedTermDetail.sourceRequirementCode ?? ''}  ${selectedTermDetail.sourceRequirementTitle ?? ''}`.trim(),
        description: selectedTermDetail.sourceRequirementCode ?? undefined,
      }];
    } else {
      requirementCandidates = [];
    }

    // Reuse already-loaded detail if available
    if (selectedTermDetail && selectedTermDetail.id === selectedTerm.id) {
      editingTerm = selectedTermDetail;
      return;
    }

    editingTerm = buildGlossaryDetail(selectedTerm);
    try {
      const authState = getAuthState();
      const tenantId = authState.user?.tenantId;
      if (!tenantId) return;
      formLoading = true;
      const detail = await getTermById(tenantId, projectId, selectedTerm.id);
      editingTerm = detail;
    } catch (err) {
      console.error('Failed to load term detail:', err);
      toast.error(err instanceof Error ? err.message : 'Failed to load term detail');
    } finally {
      formLoading = false;
    }
  }

  async function handleApproveTerm() {
    if (!selectedTerm) return;
    try {
      const authState = getAuthState();
      const tenantId = authState.user?.tenantId;
      if (!tenantId) return;
      const updated = await approveTerm(tenantId, projectId, selectedTerm.id);
      allTerms = allTerms.map((term) =>
        term.id === updated.id
          ? {
              ...term,
              status: updated.status,
              updatedAt: updated.updatedAt,
              version: updated.version,
            }
          : term
      );
      await loadTermDetail(updated.id);
      toast.success('Term approved successfully');
    } catch (err) {
      console.error('Failed to approve term:', err);
      toast.error(err instanceof Error ? err.message : 'Failed to approve term');
    }
  }

  async function handleDeprecateTerm() {
    if (!selectedTerm) return;
    try {
      const authState = getAuthState();
      const tenantId = authState.user?.tenantId;
      if (!tenantId) return;
      const updated = await deprecateTerm(tenantId, projectId, selectedTerm.id);
      allTerms = allTerms.map((term) =>
        term.id === updated.id
          ? {
              ...term,
              status: updated.status,
              updatedAt: updated.updatedAt,
              version: updated.version,
            }
          : term
      );
      await loadTermDetail(updated.id);
      toast.success('Term deprecated successfully');
    } catch (err) {
      console.error('Failed to deprecate term:', err);
      toast.error(err instanceof Error ? err.message : 'Failed to deprecate term');
    }
  }

  function handleCancelForm(): void {
    viewMode = 'detail';
    editingTerm = null;
    conflictResult = null;
  }

  async function handleCreateTermSubmit(data: CreateGlossaryTermRequest | UpdateGlossaryTermRequest): Promise<void> {
    formLoading = true;
    try {
      const authState = getAuthState();
      const tenantId = authState.user?.tenantId;
      if (!tenantId) return;
      const detail = await createTerm(tenantId, projectId, data as CreateGlossaryTermRequest);
      const listItem = toGlossaryListItem(detail);
      allTerms = [listItem, ...allTerms];
      selectedTermDetail = detail;
      selectedTermId = detail.id;
      viewMode = 'detail';
    } catch (err) {
      console.error('Failed to create term:', err);
      toast.error(err instanceof Error ? err.message : 'Failed to create term');
    } finally {
      formLoading = false;
    }
  }

  async function handleUpdateTermSubmit(data: CreateGlossaryTermRequest | UpdateGlossaryTermRequest): Promise<void> {
    if (!selectedTerm) return;
    formLoading = true;
    const previousTermId = selectedTerm.id;
    try {
      const authState = getAuthState();
      const tenantId = authState.user?.tenantId;
      if (!tenantId) return;
      const { term: _term, ...updateData } = data as CreateGlossaryTermRequest;
      const detail = await updateTerm(tenantId, projectId, previousTermId, updateData);
      const listItem = toGlossaryListItem(detail);
      // SCD Type 6: ID may change (new version row), use old ID to find and replace
      allTerms = allTerms.map((t) => (t.id === previousTermId ? listItem : t));
      selectedTermDetail = detail;
      selectedTermId = detail.id;
      viewMode = 'detail';
      editingTerm = null;
      loadVersionHistory(detail.id);
    } catch (err) {
      console.error('Failed to update term:', err);
      toast.error(err instanceof Error ? err.message : 'Failed to update term');
    } finally {
      formLoading = false;
    }
  }

  async function handleCheckConflict(term: string, definition: string): Promise<void> {
    conflictLoading = true;
    try {
      const authState = getAuthState();
      const tenantId = authState.user?.tenantId;
      if (!tenantId) return;
      conflictResult = await detectConflicts(tenantId, projectId, {
        term,
        definition,
        excludeTermId: viewMode === 'edit' ? selectedTermId ?? undefined : undefined,
      });
      if (conflictResult && !conflictResult.hasConflict) {
        toast.info('No conflicts found');
      }
    } catch (err) {
      console.error('Failed to check glossary conflicts:', err);
      toast.error(err instanceof Error ? err.message : 'Failed to check glossary conflicts');
    } finally {
      conflictLoading = false;
    }
  }

  function handleSearchSpec(query: string): void {
    if (specSearchTimer) clearTimeout(specSearchTimer);
    if (!query.trim()) {
      specCandidates = [];
      return;
    }
    specSearchTimer = setTimeout(async () => {
      specSearchLoading = true;
      try {
        const authState = getAuthState();
        const tenantId = authState.user?.tenantId;
        if (!tenantId) return;
        const results = await searchSpecs(tenantId, projectId, query, 15);
        specCandidates = results.map((s) => ({
          value: s.id,
          label: s.title,
          description: `${s.code} · ${s.status}`,
        }));
      } catch {
        specCandidates = [];
      } finally {
        specSearchLoading = false;
      }
    }, 300);
  }

  function handleSearchConversation(query: string): void {
    if (conversationSearchTimer) clearTimeout(conversationSearchTimer);
    if (!query.trim()) {
      conversationCandidates = [];
      return;
    }
    conversationSearchTimer = setTimeout(async () => {
      conversationSearchLoading = true;
      try {
        const authState = getAuthState();
        const tenantId = authState.user?.tenantId;
        if (!tenantId) return;
        const results = await searchConversations(tenantId, projectId, query, 15);
        conversationCandidates = results.map((c) => ({
          value: c.id,
          label: c.conversationType === 'Channel' ? `#${c.name}` : c.name,
          description: c.description ?? c.conversationType,
        }));
      } catch {
        conversationCandidates = [];
      } finally {
        conversationSearchLoading = false;
      }
    }, 300);
  }

  function handleSearchRequirement(query: string): void {
    if (requirementSearchTimer) clearTimeout(requirementSearchTimer);
    if (!query.trim()) {
      requirementCandidates = [];
      return;
    }
    requirementSearchTimer = setTimeout(async () => {
      requirementSearchLoading = true;
      try {
        const authState = getAuthState();
        const tenantId = authState.user?.tenantId;
        if (!tenantId) return;
        const results = await searchRequirements(tenantId, projectId, query, 15);
        requirementCandidates = results.map((r) => ({
          value: r.id,
          label: r.title,
          description: `${r.code} · Level ${r.level}`,
        }));
      } catch {
        requirementCandidates = [];
      } finally {
        requirementSearchLoading = false;
      }
    }, 300);
  }

  function buildGlossaryDetail(term: GlossaryTerm): GlossaryTermDetail {
    return {
      ...term,
      usageExamples: [],
      relatedTermIds: [],
      source: null,
      definedBy: { id: '', name: null, avatarUrl: null },
      approvedBy: null,
      approvedAt: null,
      replacedByTermId: null,
      replacedByTermName: null,
      sourceSpecId: null,
      sourceSpecCode: null,
      sourceSpecTitle: null,
      sourceConversationId: null,
      sourceConversationName: null,
      sourceConversationType: null,
      sourceRequirementId: null,
      sourceRequirementCode: null,
      sourceRequirementTitle: null,
      owner: null,
      createdBy: { id: '', name: null, avatarUrl: null },
      updatedBy: { id: '', name: null, avatarUrl: null },
      validFrom: term.createdAt,
      validTo: null,
    };
  }

  function toGlossaryListItem(detail: GlossaryTermDetail): GlossaryTerm {
    return {
      id: detail.id,
      tenantId: detail.tenantId,
      projectId: detail.projectId,
      term: detail.term,
      definition: detail.definition,
      category: detail.category,
      status: detail.status,
      synonyms: detail.synonyms ?? null,
      abbreviation: detail.abbreviation ?? null,
      version: detail.version,
      createdAt: detail.createdAt,
      updatedAt: detail.updatedAt,
    };
  }


</script>

<PageShell class={className}>
  <PageHeader title={pageTitle} meta={pageMeta} {loading}>
    {#snippet actions()}
      <IconButton
        icon="refresh-cw"
        size="sm"
        variant="ghost"
        title="Refresh terms"
        onclick={() => loadTerms()}
      />
      <IconButton
        icon="plus"
        size="sm"
        variant="ghost"
        title="New Term"
        onclick={handleCreateTerm}
      />
    {/snippet}
  </PageHeader>

  <PageBody class="p-0">
    {#if loading}
      <div class="flex-1 flex items-center justify-center">
        <!-- <Spinner size="lg" /> -->
      </div>
    {:else if error}
      <div class="flex flex-col items-center justify-center h-full text-red-500">
        <Icon name="alert-circle" size="lg" class="mb-2" />
        <p class="text-sm">{error}</p>
        <Button variant="secondary" size="sm" onclick={() => loadTerms()} class="mt-2">
          Retry
        </Button>
      </div>
    {:else}
      <SidebarDetailLayout
        showRightPanel={!!selectedTerm && viewMode === 'detail'}
        sidebarWidth={SIDEBAR_DETAIL_LAYOUT.sidebarWidth}
        minSidebarWidth={SIDEBAR_DETAIL_LAYOUT.minSidebarWidth}
        maxSidebarWidth={SIDEBAR_DETAIL_LAYOUT.maxSidebarWidth}
      >
        {#snippet sidebar()}
          <GlossarySidebar
            {categoryGroups}
            {selectedTermId}
            {searchQuery}
            {filterType}
            {statusCounts}
            onSearch={handleSearch}
            onFilterChange={handleFilterChange}
            onToggleCategory={handleToggleCategory}
            onSelectTerm={handleSelectTermById}
            hasMore={hasMoreTerms}
            loadingMore={loadingMore}
            onLoadMore={handleLoadMoreTerms}
            class="h-full"
          />
        {/snippet}

        {#snippet rightPanel()}
          {#if selectedTerm}
            <TraceGraphSection
              entityType="GlossaryTerm"
              entityId={selectedTerm.id}
              entityCode={selectedTerm.term}
              {projectId}
              onNodeClick={handleTraceNodeClick}
              class="h-full"
            >
              {#snippet revisionHistory()}
                <div class="px-4 pt-1 pb-3">
                  {#if versionHistoryLoading}
                    <div class="flex items-center gap-2 py-2 text-sm text-[var(--color-text-muted)]">
                      <Spinner size="sm" />
                      <span>Loading...</span>
                    </div>
                  {:else if pastVersions.length > 0}
                    <div class="space-y-0.5 max-h-48 overflow-y-auto">
                      {#each pastVersions as ver (ver.id)}
                        <label
                          class="flex items-center gap-2 px-2 py-1.5 rounded cursor-pointer transition-colors
                                 {selectedVersionIds.has(ver.id)
                                   ? 'bg-[var(--color-accent-primary)]/10'
                                   : 'hover:bg-[var(--color-bg-tertiary)]'}"
                        >
                          <Checkbox
                            unstyled
                            checked={selectedVersionIds.has(ver.id)}
                            onchange={() => handleVersionToggle(ver.id)}
                            class="w-3.5 h-3.5 rounded border-[var(--color-border-secondary)] accent-[var(--color-accent-primary)]"
                          />
                          <span class="font-mono text-sm text-[var(--color-accent-primary)] flex-shrink-0">
                            v{ver.version}
                          </span>
                          <span class="text-xs font-medium flex-shrink-0 {TERM_STATUS_STYLES[ver.status].color}">
                            {ver.status}
                          </span>
                          <span class="ml-auto text-xs text-[var(--color-text-muted)] flex-shrink-0">
                            {formatShortDate(ver.validFrom)}
                          </span>
                        </label>
                      {/each}
                    </div>
                  {:else}
                    <p class="text-sm text-[var(--color-text-tertiary)] text-center py-3">
                      No previous versions
                    </p>
                  {/if}
                </div>
              {/snippet}
            </TraceGraphSection>
          {/if}
        {/snippet}

        {#snippet header()}
          {#if viewMode === 'create'}
            <DetailHeader style="--detail-header-padding: 0.25rem 1rem;">
              <h2 class="text-sm font-medium text-[var(--color-text-primary)]">New Term</h2>
              {#snippet actions()}
                <IconButton icon="check" variant="success" size="sm" title="Create" onclick={() => (document.getElementById('glossary-form') as HTMLFormElement | null)?.requestSubmit()} disabled={formLoading} />
                <IconButton icon="x" variant="ghost" size="sm" title="Cancel" onclick={handleCancelForm} />
              {/snippet}
            </DetailHeader>
          {:else if viewMode === 'edit' && selectedTerm}
            <DetailHeader style="--detail-header-padding: 0.25rem 1rem;">
              <h2 class="text-sm font-medium text-[var(--color-text-primary)]">Edit Term</h2>
              {#snippet actions()}
                <IconButton icon="check" variant="success" size="sm" title="Save" onclick={() => (document.getElementById('glossary-form') as HTMLFormElement | null)?.requestSubmit()} disabled={formLoading} />
                <IconButton icon="x" variant="ghost" size="sm" title="Cancel" onclick={handleCancelForm} />
              {/snippet}
            </DetailHeader>
          {:else if selectedTerm}
            {@const catStyle = TERM_CATEGORY_STYLES[selectedTerm.category]}
            <DetailHeader style="--detail-header-padding: 0.25rem 1rem;">
              {#snippet leading()}
                <div class="flex items-center justify-center w-8 h-8 rounded-lg {catStyle.bgColor} {catStyle.borderColor} border">
                  <Icon name={catStyle.icon} size="sm" class={catStyle.color} />
                </div>
              {/snippet}
              <DetailTitle title={selectedTerm.term} code={selectedTerm.abbreviation || undefined} />
              {#snippet actions()}
                <IconButton icon="edit" variant="ghost" size="sm" title="Edit" onclick={handleEditTerm} />
                {#if selectedTerm.status === 'Draft' || selectedTerm.status === 'Active'}
                  <Dropdown position="bottom-right">
                    {#snippet trigger()}
                      <IconButton icon="more-vertical" variant="ghost" size="sm" title="More actions" />
                    {/snippet}
                    <div class="py-1 min-w-[160px]">
                      {#if selectedTerm.status === 'Draft'}
                        <Button
                          variant="unstyled"
                          onclick={handleApproveTerm}
                          class="w-full text-left px-3 py-1.5 text-xs font-medium hover:bg-[var(--color-surface-200)] transition-colors text-[var(--color-success-600)]"
                        >
                          Approve
                        </Button>
                      {/if}
                      {#if selectedTerm.status === 'Active'}
                        <Button
                          variant="unstyled"
                          onclick={handleDeprecateTerm}
                          class="w-full text-left px-3 py-1.5 text-xs font-medium hover:bg-[var(--color-surface-200)] transition-colors text-[var(--color-error-600)]"
                        >
                          Deprecate
                        </Button>
                      {/if}
                    </div>
                  </Dropdown>
                {/if}
                <IconButton icon="x" variant="ghost" size="sm" title="Close" onclick={handleCloseTerm} />
              {/snippet}
            </DetailHeader>
          {:else}
            <DetailHeader style="--detail-header-padding: 0.25rem 1rem;">
              <DetailTitle title="Overview" />
            </DetailHeader>
          {/if}
        {/snippet}

        <!-- Main Content: Term Detail -->
        {#if viewMode === 'create'}
          <div class="flex-1 min-h-0 overflow-y-auto p-4">
              <GlossaryForm
                loading={formLoading}
                {conflictLoading}
                conflictResult={conflictResult}
                onCheckConflict={handleCheckConflict}
                onSubmit={handleCreateTermSubmit}
                {specCandidates}
                {specSearchLoading}
                {memberCandidates}
                {conversationCandidates}
                {conversationSearchLoading}
                {requirementCandidates}
                {requirementSearchLoading}
                onSearchSpec={handleSearchSpec}
                onSearchConversation={handleSearchConversation}
                onSearchRequirement={handleSearchRequirement}
              />
          </div>
        {:else if viewMode === 'edit' && selectedTerm}
          <div class="flex-1 min-h-0 overflow-y-auto p-4">
              <GlossaryForm
                term={editingTerm ?? buildGlossaryDetail(selectedTerm)}
                loading={formLoading}
                {conflictLoading}
                conflictResult={conflictResult}
                onCheckConflict={handleCheckConflict}
                onSubmit={handleUpdateTermSubmit}
                {specCandidates}
                {specSearchLoading}
                {memberCandidates}
                {conversationCandidates}
                {conversationSearchLoading}
                {requirementCandidates}
                {requirementSearchLoading}
                onSearchSpec={handleSearchSpec}
                onSearchConversation={handleSearchConversation}
                onSearchRequirement={handleSearchRequirement}
              />
          </div>
        {:else if selectedTerm && selectedTermDetail}
          <div class="flex flex-col flex-1 min-h-0">
            <GlossaryDetail
              term={selectedTermDetail}
              showHeader={false}
              readonly
              onTermSelect={(termId) => handleSelectTermById(termId)}
              class="flex-1 min-h-0"
            />

            <!-- Version history tabs (shown when versions are selected) -->
            {#if selectedVersionList.length > 0}
              <div class="flex-shrink-0 flex flex-col" style="height: {versionPanelHeight}px">
                <!-- Resize handle -->
                <ResizeHandle orientation="horizontal" onpointerdown={handleResizePointerDown} isResizing={isResizingVersionPanel} ariaLabel="Resize version panel" />
                <!-- Label + Tab bar -->
                <div class="flex items-center gap-2 px-3 py-1.5 bg-[var(--color-bg-secondary)] border-b border-[var(--color-border-secondary)] flex-shrink-0">
                  <Icon name="history" size="xs" class="text-[var(--color-text-tertiary)]" />
                  <span class="text-[0.6875rem] font-semibold uppercase tracking-wider text-[var(--color-text-tertiary)]">Revision Compare</span>
                </div>
                <div class="flex items-center border-b border-[var(--color-border-secondary)] bg-[var(--color-surface-50)] px-1 flex-shrink-0">
                  {#each selectedVersionList as ver (ver.id)}
                    <Button
                      variant="unstyled"
                      onclick={() => (activeVersionTabId = ver.id)}
                      class="group flex items-center gap-1 px-3 py-1.5 text-xs font-medium transition-colors border-b-2
                             {activeVersionTabId === ver.id || (!activeVersionTabId && ver.id === selectedVersionList[0]?.id)
                               ? 'border-[var(--color-accent-primary)] text-[var(--color-text-primary)]'
                               : 'border-transparent text-[var(--color-text-muted)] hover:text-[var(--color-text-secondary)]'}"
                    >
                      <span class="font-mono">v{ver.version}</span>
                      <Button
                        variant="unstyled"
                        tabindex={-1}
                        aria-label="Close version tab"
                        onclick={(e: MouseEvent) => { e.stopPropagation(); handleRemoveVersionTab(ver.id); }}
                        class="ml-1 w-4 h-4 flex items-center justify-center rounded opacity-0 group-hover:opacity-100 hover:bg-[var(--color-surface-200)] transition-opacity text-[var(--color-text-muted)] cursor-pointer"
                        title="Close tab"
                      >
                        <Icon name="x" size="xs" />
                      </Button>
                    </Button>
                  {/each}
                </div>
                <!-- Tab content -->
                <div class="flex-1 overflow-y-auto p-4">
                  {#if activeVersion}
                    <GlossaryVersionDetail version={activeVersion} />
                  {/if}
                </div>
              </div>
            {/if}
          </div>
        {:else}
          <!-- Empty state: Summary dashboard -->
          <div class="flex-1 min-h-0 overflow-y-auto p-3 space-y-3">
              <!-- Summary Cards -->
              <CardGrid cols={4} gap="md">
                <StatCard title="Total Terms" value={statusCounts.all} icon="book-open" iconColor="blue-500" />
                <StatCard title="Draft" value={statusCounts.draft} icon="file-text" iconColor="gray-500" />
                <StatCard title="Active" value={statusCounts.active} icon="check-circle" iconColor="green-500" />
                <StatCard title="Deprecated" value={statusCounts.deprecated} icon="x-circle" iconColor="red-500" />
              </CardGrid>

              <!-- Quick Guide -->
              <SurfaceCard padding="lg">
                <h3 class="text-sm font-semibold text-[var(--color-text-primary)] mb-3">About Glossary</h3>
                <div class="space-y-2 text-sm text-[var(--color-text-secondary)]">
                  <p>
                    The glossary defines standard terms and definitions for your project.
                    Consistent terminology helps team members communicate effectively.
                  </p>
                  <div class="flex items-start gap-2 mt-3">
                    <Icon name="file-text" size="sm" class="text-gray-500 mt-0.5 flex-shrink-0" />
                    <span><strong>Draft</strong> - Term under review, not yet approved</span>
                  </div>
                  <div class="flex items-start gap-2">
                    <Icon name="check-circle" size="sm" class="text-green-500 mt-0.5 flex-shrink-0" />
                    <span><strong>Active</strong> - Approved term, use in specs and requirements</span>
                  </div>
                  <div class="flex items-start gap-2">
                    <Icon name="x-circle" size="sm" class="text-red-500 mt-0.5 flex-shrink-0" />
                    <span><strong>Deprecated</strong> - Outdated term, avoid using</span>
                  </div>
                </div>
              </SurfaceCard>

              <!-- Category Distribution -->
              <SurfaceCard padding="lg">
                <h3 class="text-sm font-semibold text-[var(--color-text-primary)] mb-3">Categories</h3>
                <div class="flex flex-wrap gap-2">
                  {#each TERM_CATEGORIES as category (category)}
                    {@const catStyle = TERM_CATEGORY_STYLES[category]}
                    {@const count = allTerms.filter(t => t.category === category).length}
                    <div class="px-3 py-1.5 rounded-full text-xs {catStyle.bgColor} {catStyle.color} border {catStyle.borderColor} {count === 0 ? 'opacity-40' : ''}">
                      <Icon name={catStyle.icon} size="xs" class="inline mr-1" />
                      {catStyle.label}: {count}
                    </div>
                  {/each}
                </div>
              </SurfaceCard>

              <!-- Empty state message -->
              {#if allTerms.length === 0}
                <div class="text-center py-8">
                  <Icon name="book-open" size="xl" class="mx-auto mb-4 text-[var(--color-text-tertiary)] opacity-50" />
                  <h3 class="text-lg font-medium text-[var(--color-text-primary)] mb-2">No terms yet</h3>
                  <p class="text-sm text-[var(--color-text-secondary)] mb-4">
                    Start building your project glossary by adding terms.
                  </p>
                  <Button variant="primary" size="md" onclick={handleCreateTerm}>
                    <Icon name="plus" size="sm" />
                    Add First Term
                  </Button>
                </div>
              {:else}
                <div class="text-center py-6">
                  <Icon name="mouse-pointer" size="lg" class="mx-auto mb-2 text-[var(--color-text-tertiary)] opacity-70" />
                  <p class="text-sm font-medium text-[var(--color-text-primary)]">Select a term</p>
                  <p class="text-xs text-[var(--color-text-secondary)] mt-1">
                    Choose a term from the sidebar to view details.
                  </p>
                </div>
              {/if}
          </div>
        {/if}
      </SidebarDetailLayout>
    {/if}
  </PageBody>
</PageShell>
