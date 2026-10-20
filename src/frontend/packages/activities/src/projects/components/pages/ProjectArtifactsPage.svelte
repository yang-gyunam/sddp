<!-- Activity: Projects > Nav: Artifacts (project-{id}-artifacts) | Screen ID: PRJ-ART-001 -->
<script lang="ts">
  import ProjectListLayout from '../../../shared/components/sections/ProjectListLayout.svelte';
  import { useDetailRoute } from '../../routing/useDetailRoute.svelte';
  import { untrack } from 'svelte';
  import { SvelteSet } from 'svelte/reactivity';
  import { Icon, IconButton, Button, CardGrid, Spinner } from '@sddp/ui';
  import type { ComboboxOption } from '@sddp/ui';
  import {
    createConfirmation,
    config as appConfig,
    PageShell,
    PageHeader,
    PageBody,
    SidebarDetailLayout,
    Dropdown,
    authStore,
    SIDEBAR_DETAIL_LAYOUT,
    SurfaceCard,
    getTabState,
    setTabState,
    toast,
    RouterService,
  } from '@sddp/shell';
  import { ApiRequestError } from '../../../shared/api/fetchWithAuth';
  let repositoryRequired = $state(false);

  import DetailHeader from '../../../shared/components/idioms/DetailHeader.svelte';
  import DetailTitle from '../../../shared/components/idioms/DetailTitle.svelte';
  import { StatCard } from '../../../shared/components/idioms';
  import { consumePendingEntityId } from '../../stores';
  import type { ProjectDetail } from '../../types';
  // Import from activities/artifact (same package, use relative path)
  import ArtifactSidebar from '../../../artifact/components/sections/ArtifactSidebar.svelte';
  import ArtifactContent from '../../../artifact/components/sections/ArtifactContent.svelte';
  import ArtifactReviewPanel from '../../../artifact/components/sections/ArtifactReviewPanel.svelte';
  import ArtifactForm from '../../../artifact/components/idioms/ArtifactForm.svelte';
  import ArtifactTypeIcon from '../../../artifact/components/idioms/ArtifactTypeIcon.svelte';
  import {
    getArtifactsByProjectPage,
    getArtifactById,
    regenerateArtifact,
    verifyArtifact,
    upsertArtifact,
    deactivateArtifact,
  } from '../../../artifact/services/ArtifactService';
  import type {
    UpsertArtifactRequest,
    ArtifactStatusCounts,
  } from '../../../artifact/services/ArtifactService';
  import { searchSpecs } from '../../../specs/services/SpecService';
  import { searchTerms } from '../../../glossary/services/GlossaryService';
  import { searchConversations } from '../../../conversations/services/ConversationService';
  import { searchRequirements } from '../../../requirements/services/RequirementService';
  import { getAuthState } from '@sddp/shell/auth';
  import type {
    ArtifactTypeGroup,
    ArtifactFilterType,
    ArtifactSummary,
  } from '../../../artifact/types';
  import { getFileName } from '../../../artifact/types';
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

  const pageTitle = 'Artifacts';
  const pageMeta = $derived(project?.name || projectName || undefined);

  // State
  let allArtifacts = $state<ArtifactSummary[]>([]);
  let expandedTypes = new SvelteSet<string>();
  let selectedArtifactId = $state<string | null>(null);
  let searchQuery = $state('');
  let filterType = $state<ArtifactFilterType>('all');
  let loading = $state(false);
  let loadingMore = $state(false);
  let artifactsPageNumber = $state(1);
  let hasMoreArtifacts = $state(false);
  const ARTIFACTS_PAGE_SIZE = 20;
  let latestArtifactsRequestId = $state(0);
  let viewMode = $state<'detail' | 'create' | 'edit'>('detail');
  let formLoading = $state(false);
  let editArtifactSpecId = $state('');
  let editingArtifactId = $state<string | null>(null);
  let editRequestGeneration = $state(0);
  const artifactMaintenanceEnabled = appConfig.get('enableArtifactMaintenance');

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

  // Conversation search for Combobox
  let conversationCandidates = $state<ComboboxOption[]>([]);
  let conversationSearchLoading = $state(false);
  let conversationSearchTimer: ReturnType<typeof setTimeout> | undefined;

  // Requirement search for Combobox
  let requirementCandidates = $state<ComboboxOption[]>([]);
  let requirementSearchLoading = $state(false);
  let requirementSearchTimer: ReturnType<typeof setTimeout> | undefined;

  // Spec search for Combobox
  let specCandidates = $state<ComboboxOption[]>([]);
  let specSearchLoading = $state(false);
  let specSearchTimer: ReturnType<typeof setTimeout> | undefined;

  // Glossary search for Combobox
  let glossaryCandidates = $state<ComboboxOption[]>([]);
  let glossarySearchLoading = $state(false);
  let glossarySearchTimer: ReturnType<typeof setTimeout> | undefined;

  interface ProjectArtifactsTabState {
    searchQuery: string;
    filterType: ArtifactFilterType;
    selectedArtifactId: string | null;
    expandedTypes: string[];
  }

  const tabStateKey = $derived(tabId || `project-${projectId}-artifacts`);

  let reviewOpen = $state(false);
  const reviewTenantId = $derived(authStore.get().user?.tenantId || '');
  let restoredKey = $state<string | null>(null);
  let isRestored = $state(false);
  let hasExpandedState = $state(false);

  $effect(() => {
    if (!tabStateKey || restoredKey === tabStateKey) return;
    const saved = getTabState<ProjectArtifactsTabState>(tabStateKey);
    if (saved) {
      searchQuery = saved.searchQuery ?? '';
      filterType = saved.filterType ?? 'all';
      selectedArtifactId = saved.selectedArtifactId ?? null;
      expandedTypes = new SvelteSet(saved.expandedTypes ?? []);
      hasExpandedState = saved.expandedTypes !== undefined;
    }
    restoredKey = tabStateKey;
    isRestored = true;
  });

  // Persist tab state
  $effect(() => {
    if (!tabStateKey || !isRestored) return;
    const state: ProjectArtifactsTabState = {
      searchQuery,
      filterType,
      selectedArtifactId,
      expandedTypes: Array.from(expandedTypes),
    };
    setTabState<ProjectArtifactsTabState>(tabStateKey, state);
  });

  // Selected artifact (derived)
  const selectedArtifact = $derived(allArtifacts.find((a) => a.id === selectedArtifactId) ?? null);

  // Initial data for edit form
  /** The artifact the edit form is bound to; falls back to the selection outside edit mode. */
  const editingArtifact = $derived(
    editingArtifactId
      ? (allArtifacts.find((a) => a.id === editingArtifactId) ?? null)
      : selectedArtifact
  );

  const editInitialData = $derived.by(() => {
    const selectedArtifact = editingArtifact;
    if (!selectedArtifact) return undefined;
    return {
      specId: editArtifactSpecId,
      artifactPath: selectedArtifact.artifactPath,
      artifactType: selectedArtifact.artifactType,
      entityName: selectedArtifact.entityName,
      glossaryTermId: selectedArtifact.glossaryTermId ?? '',
      sourceConversationId: selectedArtifact.sourceConversationId ?? '',
      sourceRequirementId: selectedArtifact.sourceRequirementId ?? '',
      ownerUserId: selectedArtifact.owner?.id ?? '',
    };
  });

  // Status counts (derived from all artifacts)
  /** Project-wide totals from the server, or null until the first page lands. */
  let serverStatusCounts = $state<ArtifactStatusCounts | null>(null);
  let projectArtifactTotal = $state(0);

  const statusCounts = $derived.by(() => ({
    all: projectArtifactTotal,
    valid: serverStatusCounts?.valid ?? 0,
    modified: serverStatusCounts?.modified ?? 0,
    missing: serverStatusCounts?.missing ?? 0,
  }));

  // Filtered and grouped artifacts
  /** The status the server filters on, or undefined for "all". */
  const activeStatusFilter = $derived(
    filterType === 'all'
      ? undefined
      : ({ valid: 'Valid', modified: 'Modified', missing: 'Missing' } as const)[filterType]
  );

  const filteredTypeGroups = $derived.by(() => {
    const grouped = allArtifacts.reduce(
      (acc, artifact) => {
        if (!acc[artifact.artifactType]) {
          acc[artifact.artifactType] = [];
        }
        acc[artifact.artifactType]!.push(artifact);
        return acc;
      },
      {} as Record<string, ArtifactSummary[]>
    );

    return Object.entries(grouped).map(([type, artifacts]) => ({
      type: type as ArtifactTypeGroup['type'],
      artifacts,
      totalCount: artifacts.length,
      expanded: expandedTypes.has(type),
    }));
  });

  // Load artifacts for this project
  $effect(() => {
    const pendingId = consumePendingEntityId();
    if (pendingId) {
      selectedArtifactId = pendingId;
    }
    untrack(() => loadArtifacts());
  });

  async function loadArtifacts(reset: boolean = true): Promise<void> {
    if (!reset && (loading || loadingMore || !hasMoreArtifacts)) return;

    const tenantId = authStore.get().user?.tenantId || '';
    if (!tenantId) {
      toast.error('No tenant context');
      return;
    }

    const requestId = ++latestArtifactsRequestId;
    const nextPageNumber = reset ? 1 : artifactsPageNumber + 1;

    if (reset) {
      loading = true;
      hasMoreArtifacts = false;
      artifactsPageNumber = 1;
    } else {
      loadingMore = true;
    }

    try {
      const page = await getArtifactsByProjectPage(
        tenantId,
        projectId,
        nextPageNumber,
        ARTIFACTS_PAGE_SIZE,
        { status: activeStatusFilter, q: searchQuery }
      );
      if (requestId !== latestArtifactsRequestId) return;

      allArtifacts = reset ? page.items : [...allArtifacts, ...page.items];
      artifactsPageNumber = page.pageNumber ?? nextPageNumber;
      hasMoreArtifacts = page.hasNextPage ?? allArtifacts.length < page.totalCount;
      projectArtifactTotal = page.statusCounts
        ? page.statusCounts.valid +
          page.statusCounts.modified +
          page.statusCounts.missing +
          page.statusCounts.unverified
        : page.totalCount;
      if (page.statusCounts) serverStatusCounts = page.statusCounts;

      if (!hasExpandedState) {
        expandedTypes = new SvelteSet(allArtifacts.map((artifact) => artifact.artifactType));
      }
    } catch (err) {
      if (requestId !== latestArtifactsRequestId) return;
      console.error('Failed to load artifacts:', err);
      toast.error(err instanceof Error ? err.message : 'Failed to load artifacts');
      hasMoreArtifacts = false;
    } finally {
      if (requestId === latestArtifactsRequestId) {
        loading = false;
        loadingMore = false;
      }
    }
  }

  function handleLoadMoreArtifacts(): void {
    loadArtifacts(false);
  }

  function handleSearch(query: string) {
    searchQuery = query;
    // The server owns filtering now, so a new term is a new request rather than a re-filter of
    // whatever happened to be loaded.
    loadArtifacts(true);
  }

  function handleFilterChange(filter: ArtifactFilterType) {
    filterType = filter;
    loadArtifacts(true);
  }

  function handleToggleType(type: string) {
    if (expandedTypes.has(type)) {
      expandedTypes.delete(type);
    } else {
      expandedTypes.add(type);
    }
  }

  function handleSelectArtifact(artifactId: string) {
    selectedArtifactId = artifactId;
    viewMode = 'detail';
  }

  function handleTraceNodeClick(node: PrimaryFlowNode): void {
    const nodeId = node.id ?? node.entityId;
    if (!nodeId) return;
    selectedArtifactId = nodeId;
    viewMode = 'detail';
  }

  function handleCloseArtifact(): void {
    selectedArtifactId = null;
  }

  async function handleDeactivateArtifact(): Promise<void> {
    if (!selectedArtifact) return;
    if (
      !(await confirmAction('Are you sure you want to deactivate this artifact?', {
        title: 'Deactivate artifact',
        confirmLabel: 'Deactivate',
        variant: 'danger',
      }))
    )
      return;
    const tenantId = authStore.get().user?.tenantId || '';
    if (!tenantId) {
      toast.error('No tenant context');
      return;
    }
    try {
      await deactivateArtifact(tenantId, projectId, selectedArtifact.id);
      selectedArtifactId = null;
      toast.success('Artifact deactivated');
      await loadArtifacts();
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Failed to deactivate artifact');
    }
  }

  async function handleRegenerate() {
    if (!artifactMaintenanceEnabled) {
      toast.info('Artifact maintenance actions are coming soon.');
      return;
    }

    if (!selectedArtifact) return;

    const tenantId = authStore.get().user?.tenantId || '';
    if (!tenantId) {
      toast.error('No tenant context');
      return;
    }

    try {
      toast.info('Regenerating artifact...');
      await regenerateArtifact(tenantId, projectId, selectedArtifact.id);
      toast.success('Artifact regenerated successfully');
      // Reload artifacts to get updated status
      await loadArtifacts();
    } catch (err) {
      console.error('Failed to regenerate artifact:', err);
      toast.error('Failed to regenerate artifact');
    }
  }

  async function handleVerifyAll() {
    if (!artifactMaintenanceEnabled) {
      toast.info('Artifact maintenance actions are coming soon.');
      return;
    }

    const tenantId = authStore.get().user?.tenantId || '';
    if (!tenantId) {
      toast.error('No tenant context');
      return;
    }

    const artifactsToVerify = allArtifacts.filter((a) => a.status !== 'Missing');
    if (artifactsToVerify.length === 0) {
      toast.info('No artifacts to verify');
      return;
    }

    toast.info(`Verifying ${artifactsToVerify.length} artifacts...`);

    let verified = 0;
    let modified = 0;
    let missing = 0;
    let failed = 0;

    for (const artifact of artifactsToVerify) {
      try {
        const result = await verifyArtifact(tenantId, projectId, {
          artifactId: artifact.id,
        });

        if (result.isValid) {
          verified++;
        } else if (result.fileExists === false) {
          missing++;
        } else {
          modified++;
        }
      } catch {
        failed++;
      }
    }

    if (modified === 0 && missing === 0 && failed === 0) {
      toast.success(`All ${verified} artifacts match their recorded hash`);
    } else {
      const parts = [`${verified} verified`];
      if (modified > 0) parts.push(`${modified} modified`);
      if (missing > 0) parts.push(`${missing} missing`);
      if (failed > 0) parts.push(`${failed} could not be checked`);
      toast.warning(parts.join(', '));
    }

    // Reload artifacts to get updated status
    await loadArtifacts();
  }

  function handleCreateArtifact() {
    selectedArtifactId = null;
    viewMode = 'create';
    conversationCandidates = [];
    requirementCandidates = [];
    specCandidates = [];
    glossaryCandidates = [];
    if (memberCandidates.length === 0) loadMemberCandidates();
  }

  async function handleEditArtifact() {
    if (!selectedArtifact) return;
    const authState = getAuthState();
    const tenantId = authState.user?.tenantId;
    if (!tenantId) return;

    // Everything below belongs to this artifact, whatever the selection does meanwhile.
    const targetArtifact = selectedArtifact;
    const requestId = ++editRequestGeneration;

    try {
      // Fetch full artifact detail to get specId
      const fullArtifact = await getArtifactById(tenantId, projectId, targetArtifact.id);
      if (requestId !== editRequestGeneration) return;

      editingArtifactId = targetArtifact.id;
      editArtifactSpecId = fullArtifact.specId;

      // Pre-populate Combobox candidates with existing references
      specCandidates = targetArtifact.specCode
        ? [{ value: fullArtifact.specId, label: targetArtifact.specCode }]
        : [];
      glossaryCandidates =
        targetArtifact.glossaryTermId && targetArtifact.glossaryTermName
          ? [{ value: targetArtifact.glossaryTermId, label: targetArtifact.glossaryTermName }]
          : [];
      conversationCandidates =
        targetArtifact.sourceConversationId && targetArtifact.sourceConversationName
          ? [
              {
                value: targetArtifact.sourceConversationId,
                label: targetArtifact.sourceConversationName,
              },
            ]
          : [];
      requirementCandidates =
        targetArtifact.sourceRequirementId && targetArtifact.sourceRequirementCode
          ? [
              {
                value: targetArtifact.sourceRequirementId,
                label: targetArtifact.sourceRequirementCode,
              },
            ]
          : [];

      // Pre-populate owner candidate so Combobox displays immediately
      if (targetArtifact.owner?.id && targetArtifact.owner?.name) {
        if (!memberCandidates.some((m) => m.value === targetArtifact.owner!.id)) {
          memberCandidates = [
            { value: targetArtifact.owner.id, label: targetArtifact.owner.name },
            ...memberCandidates,
          ];
        }
      }
      if (memberCandidates.length <= 1) loadMemberCandidates();
      viewMode = 'edit';
    } catch (err) {
      console.error('Failed to load artifact for edit:', err);
      toast.error('Failed to load artifact details');
    }
  }

  function handleCancelForm() {
    // Release the edit target too, or the next form would inherit it.
    editingArtifactId = null;
    editRequestGeneration++;
    viewMode = 'detail';
  }

  async function handleCreateArtifactSubmit(data: UpsertArtifactRequest) {
    formLoading = true;
    repositoryRequired = false;
    try {
      const authState = getAuthState();
      const tenantId = authState.user?.tenantId;
      if (!tenantId) return;
      await upsertArtifact(tenantId, projectId, data);
      viewMode = 'detail';
      await loadArtifacts();
      toast.success('Artifact created successfully');
    } catch (err) {
      repositoryRequired =
        err instanceof ApiRequestError && err.errorCode === 'REPOSITORY_CONFIRMATION_REQUIRED';
      if (!repositoryRequired) console.error('Failed to create artifact:', err);
      toast.error(err instanceof Error ? err.message : 'Failed to create artifact');
    } finally {
      formLoading = false;
    }
  }

  async function handleEditArtifactSubmit(data: UpsertArtifactRequest) {
    formLoading = true;
    repositoryRequired = false;
    try {
      const authState = getAuthState();
      const tenantId = authState.user?.tenantId;
      if (!tenantId) return;
      if (!editingArtifactId) {
        toast.error('Lost track of which artifact was being edited. Reopen it and try again.');
        return;
      }

      await upsertArtifact(tenantId, projectId, {
        ...data,
        artifactId: editingArtifactId,
      });
      editingArtifactId = null;
      viewMode = 'detail';
      await loadArtifacts();
      toast.success('Artifact updated successfully');
    } catch (err) {
      repositoryRequired =
        err instanceof ApiRequestError && err.errorCode === 'REPOSITORY_CONFIRMATION_REQUIRED';
      if (!repositoryRequired) console.error('Failed to update artifact:', err);
      toast.error(err instanceof Error ? err.message : 'Failed to update artifact');
    } finally {
      formLoading = false;
    }
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

  function handleSearchGlossary(query: string): void {
    if (glossarySearchTimer) clearTimeout(glossarySearchTimer);
    if (!query.trim()) {
      glossaryCandidates = [];
      return;
    }
    glossarySearchTimer = setTimeout(async () => {
      glossarySearchLoading = true;
      try {
        const authState = getAuthState();
        const tenantId = authState.user?.tenantId;
        if (!tenantId) return;
        const result = await searchTerms(tenantId, projectId, query, { pageSize: 15 });
        glossaryCandidates = result.items.map((t) => ({
          value: t.id,
          label: t.term,
          description: `${t.category} · ${t.status}`,
        }));
      } catch {
        glossaryCandidates = [];
      } finally {
        glossarySearchLoading = false;
      }
    }, 300);
  }
  useDetailRoute({
    tabId: () => tabId,
    path: () => `/project/${projectId}/artifacts`,
    selection: () => selectedArtifactId,
    ready: () => !loading,
    restore: async (id) => {
      selectedArtifactId = id;
      viewMode = 'detail';
      while (
        id &&
        selectedArtifactId === id &&
        !allArtifacts.some((a) => a.id === id) &&
        hasMoreArtifacts
      ) {
        await loadArtifacts(false);
      }
    },
  });
  const { confirmAction } = createConfirmation(() => `${projectId}/${selectedArtifact?.id}`);
</script>

<PageShell class={className}>
  <PageHeader title={pageTitle} meta={pageMeta} {loading}>
    {#snippet actions()}
      <IconButton
        icon="refresh-cw"
        size="sm"
        variant="ghost"
        title="Refresh artifacts"
        onclick={() => loadArtifacts()}
      />
      <IconButton
        icon="plus"
        size="sm"
        variant="ghost"
        title="New Artifact"
        onclick={handleCreateArtifact}
      />
    {/snippet}
  </PageHeader>

  <PageBody class="p-0">
    {#if loading}
      <div
        class="flex-1 flex items-center justify-center"
        role="status"
        aria-label="Loading artifacts"
      >
        <Spinner size="lg" />
      </div>
    {:else}
      <SidebarDetailLayout
        showRightPanel={!!selectedArtifact && (viewMode === 'detail' || viewMode === 'edit')}
        sidebarWidth={SIDEBAR_DETAIL_LAYOUT.sidebarWidth}
        minSidebarWidth={SIDEBAR_DETAIL_LAYOUT.minSidebarWidth}
        maxSidebarWidth={SIDEBAR_DETAIL_LAYOUT.maxSidebarWidth}
        rightPanelWidth={SIDEBAR_DETAIL_LAYOUT.rightPanelWidth}
        minRightPanelWidth={SIDEBAR_DETAIL_LAYOUT.minRightPanelWidth}
        maxRightPanelWidth={SIDEBAR_DETAIL_LAYOUT.maxRightPanelWidth}
      >
        {#snippet sidebar()}
          <ProjectListLayout lines={1}>
            <ArtifactSidebar
              typeGroups={filteredTypeGroups}
              {selectedArtifactId}
              {searchQuery}
              {filterType}
              {statusCounts}
              onSearch={handleSearch}
              onFilterChange={handleFilterChange}
              onToggleType={handleToggleType}
              onSelectArtifact={handleSelectArtifact}
              onRegenerate={artifactMaintenanceEnabled ? handleRegenerate : undefined}
              onVerifyAll={artifactMaintenanceEnabled ? handleVerifyAll : undefined}
              hasMore={hasMoreArtifacts}
              {loadingMore}
              onLoadMore={handleLoadMoreArtifacts}
              class="h-full"
            />
          </ProjectListLayout>
        {/snippet}

        {#snippet rightPanel()}
          {#if selectedArtifact}
            <TraceGraphSection
              entityType="Artifact"
              entityId={selectedArtifact.id}
              entityCode={getFileName(selectedArtifact.artifactPath)}
              {projectId}
              onNodeClick={handleTraceNodeClick}
              class="h-full"
            >
              {#snippet revisionHistory()}
                <div class="px-4 pt-1 pb-3">
                  <p class="text-sm text-[var(--color-text-tertiary)] text-center py-3">
                    No previous versions
                  </p>
                </div>
              {/snippet}
            </TraceGraphSection>
          {/if}
        {/snippet}

        {#snippet header()}
          {#if viewMode === 'create'}
            <DetailHeader style="--detail-header-padding: 0.25rem 1rem;">
              <h2 class="text-sm font-medium text-[var(--color-text-primary)]">New Artifact</h2>
              {#snippet actions()}
                <IconButton
                  icon="check"
                  variant="success"
                  size="sm"
                  title="Create"
                  onclick={() =>
                    (
                      document.getElementById('artifact-form') as HTMLFormElement | null
                    )?.requestSubmit()}
                  disabled={formLoading}
                />
                <IconButton
                  icon="x"
                  variant="ghost"
                  size="sm"
                  title="Cancel"
                  onclick={handleCancelForm}
                />
              {/snippet}
            </DetailHeader>
          {:else if viewMode === 'edit' && selectedArtifact}
            <DetailHeader>
              {#snippet leading()}
                <ArtifactTypeIcon type={selectedArtifact.artifactType} size={20} />
              {/snippet}
              <DetailTitle title="Edit Artifact">
                <span class="text-xs text-[var(--color-text-tertiary)] truncate min-w-0">
                  {selectedArtifact.artifactPath}
                </span>
              </DetailTitle>
              {#snippet actions()}
                <IconButton
                  icon="check"
                  variant="success"
                  size="sm"
                  title="Save"
                  onclick={() =>
                    (
                      document.getElementById('artifact-form') as HTMLFormElement | null
                    )?.requestSubmit()}
                  disabled={formLoading}
                />
                <IconButton
                  icon="x"
                  variant="ghost"
                  size="sm"
                  title="Cancel"
                  onclick={handleCancelForm}
                />
              {/snippet}
            </DetailHeader>
          {:else if selectedArtifact}
            <DetailHeader>
              {#snippet leading()}
                <ArtifactTypeIcon type={selectedArtifact.artifactType} size={20} />
              {/snippet}
              <DetailTitle title={getFileName(selectedArtifact.artifactPath)}>
                <span class="text-xs text-[var(--color-text-tertiary)] truncate min-w-0">
                  {selectedArtifact.artifactPath}
                </span>
              </DetailTitle>
              {#snippet actions()}
                <IconButton
                  icon="code"
                  variant="ghost"
                  size="sm"
                  title="View Source"
                  onclick={() => (reviewOpen = true)}
                />
                <IconButton
                  icon="edit"
                  variant="ghost"
                  size="sm"
                  title="Edit"
                  onclick={handleEditArtifact}
                />
                <Dropdown position="bottom-right">
                  {#snippet trigger({ toggle, open })}
                    <IconButton
                      onclick={toggle}
                      ariaHasPopup
                      ariaExpanded={open}
                      icon="more-vertical"
                      variant="ghost"
                      size="sm"
                      title="More actions"
                    />
                  {/snippet}
                  <div class="py-1 min-w-[160px]">
                    {#if artifactMaintenanceEnabled}
                      <Button
                        variant="unstyled"
                        onclick={handleRegenerate}
                        class="w-full text-left px-3 py-1.5 text-xs font-medium hover:bg-[var(--color-surface-200)] transition-colors text-[var(--color-text-primary)] flex items-center gap-2"
                      >
                        <Icon name="refresh-cw" size="xs" />
                        Regenerate
                      </Button>
                      <div class="my-1 border-t border-[var(--color-border-secondary)]"></div>
                    {/if}
                    <Button
                      variant="unstyled"
                      onclick={handleDeactivateArtifact}
                      class="w-full text-left px-3 py-1.5 text-xs font-medium hover:bg-[var(--color-surface-200)] transition-colors text-[var(--color-warning-600)] flex items-center gap-2"
                    >
                      <Icon name="lock" size="xs" />
                      Deactivate
                    </Button>
                  </div>
                </Dropdown>
                <IconButton
                  icon="x"
                  variant="ghost"
                  size="sm"
                  title="Close"
                  onclick={handleCloseArtifact}
                />
              {/snippet}
            </DetailHeader>
          {:else}
            <DetailHeader style="--detail-header-padding: 0.25rem 1rem;">
              <DetailTitle title="Overview" />
            </DetailHeader>
          {/if}
        {/snippet}

        <!-- Main Content -->
        {#if viewMode === 'create'}
          <div class="flex-1 min-h-0 overflow-y-auto p-4">
            {#if repositoryRequired}
              <div role="alert" class="mb-4 text-sm text-[var(--color-text-secondary)]">
                <p>Confirm the Git repository before saving file relationships.</p>
                <Button
                  variant="secondary"
                  size="sm"
                  onclick={() => RouterService.navigate(`/settings/project/${projectId}/general`)}
                  >Open repository settings</Button
                >
              </div>
            {/if}
            <ArtifactForm
              loading={formLoading}
              {memberCandidates}
              {conversationCandidates}
              {conversationSearchLoading}
              {requirementCandidates}
              {requirementSearchLoading}
              {specCandidates}
              {specSearchLoading}
              {glossaryCandidates}
              {glossarySearchLoading}
              onSearchConversation={handleSearchConversation}
              onSearchRequirement={handleSearchRequirement}
              onSearchSpec={handleSearchSpec}
              onSearchGlossary={handleSearchGlossary}
              onSubmit={handleCreateArtifactSubmit}
            />
          </div>
        {:else if viewMode === 'edit' && selectedArtifact}
          <div class="flex-1 min-h-0 overflow-y-auto p-4">
            {#if repositoryRequired}
              <div role="alert" class="mb-4 text-sm text-[var(--color-text-secondary)]">
                <p>Confirm the Git repository before saving file relationships.</p>
                <Button
                  variant="secondary"
                  size="sm"
                  onclick={() => RouterService.navigate(`/settings/project/${projectId}/general`)}
                  >Open repository settings</Button
                >
              </div>
            {/if}
            <ArtifactForm
              loading={formLoading}
              initialData={editInitialData}
              {memberCandidates}
              {conversationCandidates}
              {conversationSearchLoading}
              {requirementCandidates}
              {requirementSearchLoading}
              {specCandidates}
              {specSearchLoading}
              {glossaryCandidates}
              {glossarySearchLoading}
              onSearchConversation={handleSearchConversation}
              onSearchRequirement={handleSearchRequirement}
              onSearchSpec={handleSearchSpec}
              onSearchGlossary={handleSearchGlossary}
              onSubmit={handleEditArtifactSubmit}
            />
          </div>
        {:else if selectedArtifact}
          <!-- showHeader={false}: this page draws its own header, so ArtifactContent's
               internal action bar — View Source included — never renders. That button
               belongs in the header above. -->
          <ArtifactContent
            artifact={selectedArtifact}
            showHeader={false}
            onRegenerate={artifactMaintenanceEnabled ? handleRegenerate : undefined}
            onDeactivate={handleDeactivateArtifact}
          />
        {:else}
          <!-- Empty state: Summary dashboard -->
          <div class="flex-1 min-h-0 overflow-y-auto p-3 space-y-3">
            <!-- Summary Cards -->
            <CardGrid cols={4} gap="md">
              <StatCard
                title="Total Artifacts"
                value={statusCounts.all}
                icon="layers"
                iconColor="blue-500"
              />
              <StatCard
                title="Valid"
                value={statusCounts.valid}
                icon="check-circle"
                iconColor="green-500"
              />
              <StatCard
                title="Modified"
                value={statusCounts.modified}
                icon="alert-triangle"
                iconColor="amber-500"
              />
              <StatCard
                title="Missing"
                value={statusCounts.missing}
                icon="x-circle"
                iconColor="red-500"
              />
            </CardGrid>

            <!-- Quick Guide -->
            <SurfaceCard padding="lg">
              <h3 class="surface-card__title mb-3">Quick Guide</h3>
              <div class="space-y-2 text-sm text-[var(--color-text-secondary)]">
                <div class="flex items-start gap-2">
                  <Icon name="check-circle" size="sm" class="text-green-500 mt-0.5 flex-shrink-0" />
                  <span><strong>Valid</strong> - Artifact matches the spec, no action needed</span>
                </div>
                <div class="flex items-start gap-2">
                  <Icon
                    name="alert-triangle"
                    size="sm"
                    class="text-amber-500 mt-0.5 flex-shrink-0"
                  />
                  <span
                    ><strong>Modified</strong> - File changed after generation, verify or regenerate</span
                  >
                </div>
                <div class="flex items-start gap-2">
                  <Icon name="x-circle" size="sm" class="text-red-500 mt-0.5 flex-shrink-0" />
                  <span
                    ><strong>Missing</strong> - Expected file not found, regenerate required</span
                  >
                </div>
              </div>
            </SurfaceCard>

            <!-- Type Distribution -->
            {#if allArtifacts.length > 0}
              <SurfaceCard padding="lg">
                <h3 class="surface-card__title mb-3">Artifact Types</h3>
                <div class="flex flex-wrap gap-2">
                  {#each Object.entries(allArtifacts.reduce((acc, a) => {
                        acc[a.artifactType] = (acc[a.artifactType] || 0) + 1;
                        return acc;
                      }, {} as Record<string, number>)) as [type, count] (type)}
                    <div
                      class="px-3 py-1.5 rounded-full text-xs bg-[var(--color-bg-tertiary)] text-[var(--color-text-secondary)]"
                    >
                      {type}: {count}
                    </div>
                  {/each}
                </div>
              </SurfaceCard>
            {/if}

            <div class="text-center py-6">
              <Icon
                name="mouse-pointer"
                size="lg"
                class="mx-auto mb-2 text-[var(--color-text-tertiary)] opacity-70"
              />
              <p class="text-sm font-medium text-[var(--color-text-primary)]">Select an artifact</p>
              <p class="text-xs text-[var(--color-text-secondary)] mt-1">
                Choose an artifact from the sidebar to view details.
              </p>
            </div>
          </div>
        {/if}
      </SidebarDetailLayout>
    {/if}
  </PageBody>
</PageShell>

<ArtifactReviewPanel
  open={reviewOpen}
  artifactId={selectedArtifact?.id ?? null}
  tenantId={reviewTenantId}
  {projectId}
  onClose={() => (reviewOpen = false)}
/>
