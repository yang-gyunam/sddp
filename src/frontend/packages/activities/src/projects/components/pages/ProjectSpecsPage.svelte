<!-- Activity: Projects > Nav: Specs (project-{id}-specs) | Screen ID: PRJ-SPEC-001 -->
<script lang="ts">
  import { tick, untrack } from 'svelte';
  import { SvelteSet } from 'svelte/reactivity';
  import { Icon, Input, IconButton, Button, CardGrid, Checkbox, Spinner, ResizeHandle } from '@sddp/ui';
  import type { ComboboxOption } from '@sddp/ui';
  import { PageShell, PageHeader, PageBody, SidebarDetailLayout, Dropdown, RichListItem, SIDEBAR_DETAIL_LAYOUT, SurfaceCard, getTabState, setTabState, toast, formatDateWithOptions } from '@sddp/shell';
  import DetailHeader from '../../../shared/components/idioms/DetailHeader.svelte';
  import DetailTitle from '../../../shared/components/idioms/DetailTitle.svelte';
  import { StatCard } from '../../../shared/components/idioms';
  import { getAuthState } from '@sddp/shell/auth';
  import type { ProjectDetail } from '../../types';
  import { getProjectService, getProjectById } from '../../services/ProjectService';
  import { setSelectedNodePath, consumePendingEntityId, setPendingEntityId } from '../../stores';
  import { getSpecById, deleteSpec, submitForReview, approveSpec, lockSpec, updateSpec, createSpec, createNewVersion, getSignOffSummary, submitSignOff, getVersionHistory } from '../../../specs/services';
  import SpecDetailView from '../../../specs/components/sections/SpecDetailView.svelte';
  import SpecForm from '../../../specs/components/idioms/SpecForm.svelte';
  import SpecVersionDetail from '../../../specs/components/idioms/SpecVersionDetail.svelte';
  import type { Spec, SpecDetail, SpecStatus, CreateSpecRequest, UpdateSpecRequest, SignOffSummary, SignOffRequest } from '../../../specs/types';
  import { SPEC_STATUS_STYLES } from '../../../specs/types';
  import { TraceGraphSection } from '../../../relationship/components/sections';
  import type { PrimaryFlowNode } from '../../../relationship/types';
  import type { EntityMetadata, CreateEntityMetadataRequest, UpdateEntityMetadataRequest } from '../../../entities/types';
  import { getEntityMetadataService } from '../../../entities/services';

  interface Props {
    projectId: string;
    projectName?: string;
    project?: ProjectDetail;
    tabId?: string;
    class?: string;
  }

  let { projectId, projectName = '', project, tabId = '', class: className = '' }: Props = $props();

  const pageTitle = 'Specs';
  const pageMeta = $derived(project?.name || projectName || undefined);

  // State
  let allSpecs = $state<Spec[]>([]);
  let selectedSpecId = $state<string | null>(null);
  let selectedSpec = $state<SpecDetail | null>(null);
  let searchQuery = $state('');
  let statusFilter = $state<SpecStatus | 'all'>('all');
  let loading = $state(false);
  let loadingMore = $state(false);
  let specsPageNumber = $state(1);
  let hasMoreSpecs = $state(false);
  const SPECS_PAGE_SIZE = 20;
  let latestSpecsRequestId = $state(0);
  let detailLoading = $state(false);
  let viewMode = $state<'detail' | 'create' | 'edit'>('detail');
  let formLoading = $state(false);
  let error = $state<string | null>(null);
  let signOffSummary = $state<SignOffSummary | null>(null);
  let versionHistory = $state<Spec[]>([]);
  let versionHistoryLoading = $state(false);
  let selectedVersionIds = new SvelteSet<string>();
  let activeVersionTabId = $state<string | null>(null);

  // Resize state for version comparison panel
  const VERSION_PANEL_MIN = 150;
  const VERSION_PANEL_MAX = 600;
  const VERSION_PANEL_DEFAULT = 280;
  let versionPanelHeight = $state(VERSION_PANEL_DEFAULT);
  let isResizingVersionPanel = $state(false);
  let resizeStartY = 0;
  let resizeStartHeight = 0;

  let memberCandidates = $state<ComboboxOption[]>([]);

  let entitySchemas = $state<EntityMetadata[]>([]);
  let entitySchemasLoading = $state(false);
  let entitySchemasError = $state<string | null>(null);
  let currentUserId = $state<string | null>(null);

  interface ProjectSpecsTabState {
    searchQuery: string;
    statusFilter: SpecStatus | 'all';
    selectedSpecId: string | null;
  }

  const tabStateKey = $derived(tabId || `project-${projectId}-specs`);
  let restoredKey = $state<string | null>(null);
  let isRestored = $state(false);

  $effect(() => {
    if (!tabStateKey || restoredKey === tabStateKey) return;
    const saved = getTabState<ProjectSpecsTabState>(tabStateKey);
    if (saved) {
      searchQuery = saved.searchQuery ?? '';
      statusFilter = saved.statusFilter ?? 'all';
      selectedSpecId = saved.selectedSpecId ?? null;
    }
    restoredKey = tabStateKey;
    isRestored = true;
  });

  // Persist tab state
  $effect(() => {
    if (!tabStateKey || !isRestored) return;
    const state: ProjectSpecsTabState = {
      searchQuery,
      statusFilter,
      selectedSpecId,
    };
    setTabState<ProjectSpecsTabState>(tabStateKey, state);
  });

  // Filtered specs
  const filteredSpecs = $derived.by(() => {
    let filtered = allSpecs;

    // Filter by status
    if (statusFilter !== 'all') {
      filtered = filtered.filter((s) => s.status === statusFilter);
    }

    // Filter by search query
    if (searchQuery.trim()) {
      const query = searchQuery.toLowerCase();
      filtered = filtered.filter(
        (s) =>
          s.title.toLowerCase().includes(query) ||
          s.code.toLowerCase().includes(query)
      );
    }

    return filtered;
  });

  function mapSpecDetailToSpec(detail: SpecDetail): Spec {
    return {
      id: detail.id,
      tenantId: detail.tenantId,
      projectId: detail.projectId,
      code: detail.code,
      title: detail.title,
      description: detail.description ?? '',
      decision: detail.decision ?? '',
      status: detail.status,
      requirementId: detail.requirementId ?? null,
      bornFromConversationId: detail.bornFromConversationId ?? null,
      supersedesSpecId: detail.supersedesSpecId ?? null,
      version: detail.version,
      lockedAt: detail.lockedAt ?? null,
      createdAt: detail.createdAt,
      updatedAt: detail.updatedAt,
    };
  }

  // Status counts for sidebar
  const statusCounts = $derived({
    all: allSpecs.length,
    Draft: allSpecs.filter((s) => s.status === 'Draft').length,
    InReview: allSpecs.filter((s) => s.status === 'InReview').length,
    Approved: allSpecs.filter((s) => s.status === 'Approved').length,
    Locked: allSpecs.filter((s) => s.status === 'Locked').length,
  });

  const pastVersions = $derived(
    versionHistory.filter((v) => v.id !== selectedSpec?.id)
  );
  const selectedVersionList = $derived(
    pastVersions.filter((v) => selectedVersionIds.has(v.id))
  );
  const activeVersion = $derived(
    selectedVersionList.find((v) => v.id === activeVersionTabId) ?? selectedVersionList[0] ?? null
  );

  function formatShortDate(dateStr: string): string {
    try {
      return formatDateWithOptions(dateStr, { month: 'short', day: 'numeric', year: '2-digit' });
    } catch {
      return dateStr;
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

  $effect(() => {
    // Check for cross-type navigation (e.g., from Requirements → Specs)
    const pendingId = consumePendingEntityId();
    if (pendingId) {
      selectedSpecId = pendingId;
    }
    untrack(() => loadSpecs());
  });

  async function loadSpecs(reset: boolean = true): Promise<void> {
    if (!reset && (loading || loadingMore || !hasMoreSpecs)) return;

    const requestId = ++latestSpecsRequestId;
    const nextPageNumber = reset ? 1 : specsPageNumber + 1;

    if (reset) {
      loading = true;
      error = null;
      hasMoreSpecs = false;
      specsPageNumber = 1;
    } else {
      loadingMore = true;
    }

    try {
      const authState = getAuthState();
      if (!authState.user?.tenantId) {
        throw new Error('User not authenticated or missing tenant');
      }

      currentUserId = authState.user.id ?? null;
      const service = getProjectService();
      service.setTenantId(authState.user.tenantId);

      const response = await service.getProjectSpecs(
        projectId,
        nextPageNumber,
        SPECS_PAGE_SIZE
      );
      if (requestId !== latestSpecsRequestId) return;

      const mapped = response.items.map((s) => ({
        id: s.id,
        tenantId: authState.user!.tenantId!,
        projectId: projectId,
        code: s.code,
        title: s.title,
        description: s.description || '',
        decision: s.decision || '',
        status: s.status as SpecStatus,
        requirementId: s.requirementId || null,
        bornFromConversationId: null,
        supersedesSpecId: null,
        version: s.version,
        lockedAt: null,
        createdAt: s.createdAt,
        updatedAt: s.updatedAt,
      }));

      allSpecs = reset ? mapped : [...allSpecs, ...mapped];
      specsPageNumber = response.pageNumber ?? response.page ?? nextPageNumber;
      hasMoreSpecs = response.hasNextPage ?? allSpecs.length < response.totalCount;

      if (reset && selectedSpecId && selectedSpec?.id !== selectedSpecId) {
        await loadSpecDetail(selectedSpecId);
      }
    } catch (err) {
      if (requestId !== latestSpecsRequestId) return;
      console.error('Failed to load specs:', err);
      const message = err instanceof Error ? err.message : 'Failed to load specs';
      error = message;
      toast.error(message);
      hasMoreSpecs = false;
    } finally {
      if (requestId === latestSpecsRequestId) {
        loading = false;
        loadingMore = false;
        await tick();
      }
    }
  }

  async function loadSpecDetail(specId: string) {
    detailLoading = true;
    try {
      const authState = getAuthState();
      if (!authState.user?.tenantId) return;

      const detail = await getSpecById(authState.user.tenantId, projectId, specId);
      selectedSpec = detail;
    } catch (err) {
      console.error('Failed to load spec detail:', err);
      toast.error(err instanceof Error ? err.message : 'Failed to load spec detail');
      selectedSpec = null;
    } finally {
      detailLoading = false;
    }
  }

  async function loadSignOffSummary(specId: string): Promise<SignOffSummary | null> {
    try {
      const authState = getAuthState();
      if (!authState.user?.tenantId) return null;
      const summary = await getSignOffSummary(authState.user.tenantId, projectId, specId);
      signOffSummary = summary;
      return summary;
    } catch (err) {
      console.error('Failed to load sign-off summary:', err);
      toast.error(err instanceof Error ? err.message : 'Failed to load sign-off summary');
      signOffSummary = null;
      return null;
    } finally {
      // no-op
    }
  }

  async function loadVersionHistory(specId: string): Promise<void> {
    versionHistoryLoading = true;
    try {
      const authState = getAuthState();
      if (!authState.user?.tenantId) return;
      versionHistory = await getVersionHistory(authState.user.tenantId, projectId, specId);
    } catch (err) {
      console.error('Failed to load version history:', err);
      toast.error(err instanceof Error ? err.message : 'Failed to load version history');
      versionHistory = [];
    } finally {
      versionHistoryLoading = false;
    }
  }

  async function loadEntitySchemas(): Promise<void> {
    if (!selectedSpec?.id) return;
    entitySchemasLoading = true;
    entitySchemasError = null;
    try {
      const authState = getAuthState();
      if (!authState.user?.tenantId) return;
      const service = getEntityMetadataService();
      service.setContext(authState.user.tenantId, projectId);
      entitySchemas = await service.getBySpec(selectedSpec.id);
    } catch (err) {
      console.error('Failed to load entity schemas:', err);
      const message = err instanceof Error ? err.message : 'Failed to load entity schemas';
      entitySchemasError = message;
      toast.error(message);
      entitySchemas = [];
    } finally {
      entitySchemasLoading = false;
    }
  }

  async function handleCreateEntitySchema(request: CreateEntityMetadataRequest): Promise<void> {
    if (!selectedSpec?.id) return;
    try {
      const authState = getAuthState();
      if (!authState.user?.tenantId) return;
      const service = getEntityMetadataService();
      service.setContext(authState.user.tenantId, projectId);
      await service.create(selectedSpec.id, request);
      await loadEntitySchemas();
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to create entity schema';
      entitySchemasError = message;
      toast.error(message);
    }
  }

  async function handleUpdateEntitySchema(
    entityId: string,
    request: UpdateEntityMetadataRequest
  ): Promise<void> {
    if (!selectedSpec?.id) return;
    try {
      const authState = getAuthState();
      if (!authState.user?.tenantId) return;
      const service = getEntityMetadataService();
      service.setContext(authState.user.tenantId, projectId);
      await service.update(selectedSpec.id, entityId, request);
      await loadEntitySchemas();
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to update entity schema';
      entitySchemasError = message;
      toast.error(message);
    }
  }

  async function handleDeleteEntitySchema(entityId: string): Promise<void> {
    if (!selectedSpec?.id) return;
    try {
      const authState = getAuthState();
      if (!authState.user?.tenantId) return;
      const service = getEntityMetadataService();
      service.setContext(authState.user.tenantId, projectId);
      await service.delete(selectedSpec.id, entityId);
      await loadEntitySchemas();
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to delete entity schema';
      entitySchemasError = message;
      toast.error(message);
    }
  }

  let prevSelectedSpecId = $state<string | null>(null);
  $effect(() => {
    if (!selectedSpec?.id) {
      prevSelectedSpecId = null;
      signOffSummary = null;
      versionHistory = [];
      selectedVersionIds.clear();
      activeVersionTabId = null;
      entitySchemas = [];
      entitySchemasError = null;
      return;
    }
    if (selectedSpec.id === prevSelectedSpecId) return;
    prevSelectedSpecId = selectedSpec.id;
    selectedVersionIds.clear();
    activeVersionTabId = null;
    const specId = selectedSpec.id;
    entitySchemas = [];
    entitySchemasError = null;
    untrack(() => loadSignOffSummary(specId));
    untrack(() => loadVersionHistory(specId));
    untrack(() => loadEntitySchemas());
  });

  function handleSelectSpec(spec: Spec) {
    selectedSpecId = spec.id;
    viewMode = 'detail';
    loadSpecDetail(spec.id);
  }

  function handleSpecListScroll(e: Event): void {
    if (loading || loadingMore || !hasMoreSpecs) return;

    const target = e.currentTarget as HTMLElement;
    const nearBottom = target.scrollHeight - target.scrollTop - target.clientHeight <= 100;
    if (nearBottom) {
      loadSpecs(false);
    }
  }

  function handleTraceNodeClick(node: PrimaryFlowNode): void {
    const nodeId = node.id ?? node.entityId;
    if (!nodeId) return;
    selectedSpecId = nodeId;
    loadSpecDetail(nodeId);
  }

  function handleCloseSpec(): void {
    selectedSpecId = null;
    selectedSpec = null;
    viewMode = 'detail';
  }

  async function handleDeactivateSpec(): Promise<void> {
    if (!selectedSpec) return;
    if (!confirm('Are you sure you want to deactivate this spec?')) return;
    const authState = getAuthState();
    if (!authState.user?.tenantId) return;
    try {
      await deleteSpec(authState.user.tenantId, projectId, selectedSpec.id);
      allSpecs = allSpecs.filter((s) => s.id !== selectedSpec!.id);
      handleCloseSpec();
      toast.success('Spec deactivated');
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Failed to deactivate spec');
    }
  }

  async function loadMemberCandidates(): Promise<void> {
    try {
      const authState = getAuthState();
      if (!authState.user?.tenantId) return;
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

  function handleEditSpec(): void {
    if (!selectedSpec) return;
    viewMode = 'edit';
    if (memberCandidates.length === 0) {
      loadMemberCandidates();
    }
  }

  function handleCreateSpec() {
    selectedSpecId = null;
    selectedSpec = null;
    viewMode = 'create';
    if (memberCandidates.length === 0) {
      loadMemberCandidates();
    }
  }

  async function handleUpdateSpec(data: UpdateSpecRequest): Promise<void> {
    if (!selectedSpec) return;
    formLoading = true;
    try {
      const authState = getAuthState();
      if (!authState.user?.tenantId) return;

      const detail = await updateSpec(authState.user.tenantId, projectId, selectedSpec.id, data);
      selectedSpec = detail;
      allSpecs = allSpecs.map((spec) =>
        spec.id === detail.id
          ? {
              ...spec,
              title: detail.title,
              status: detail.status,
              updatedAt: detail.updatedAt,
              version: detail.version,
            }
          : spec
      );
      viewMode = 'detail';
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Failed to update spec');
    } finally {
      formLoading = false;
    }
  }

  function handleCancelForm(): void {
    if (viewMode === 'edit') {
      viewMode = 'detail';
      return;
    }
    viewMode = 'detail';
  }

  async function handleCreateSpecSubmit(data: CreateSpecRequest): Promise<void> {
    formLoading = true;
    try {
      const authState = getAuthState();
      if (!authState.user?.tenantId) return;

      const detail = await createSpec(authState.user.tenantId, projectId, data);
      const newSpec = mapSpecDetailToSpec(detail);

      allSpecs = [newSpec, ...allSpecs];
      selectedSpecId = detail.id;
      selectedSpec = detail;
      viewMode = 'detail';
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Failed to create spec');
    } finally {
      formLoading = false;
    }
  }

  async function handleTransition(targetStatus: SpecStatus) {
    if (!selectedSpec) return;

    try {
      const authState = getAuthState();
      if (!authState.user?.tenantId) return;

      const tenantId = authState.user.tenantId;

      // Call appropriate API based on target status
      switch (targetStatus) {
        case 'InReview':
          await submitForReview(tenantId, projectId, selectedSpec.id);
          break;
        case 'Approved':
          await approveSpec(tenantId, projectId, selectedSpec.id);
          break;
        case 'Locked':
          await lockSpec(tenantId, projectId, selectedSpec.id);
          break;
        default:
          console.warn('Unsupported transition to:', targetStatus);
          return;
      }

      // Reload spec detail and list
      await Promise.all([
        loadSpecDetail(selectedSpec.id),
        loadSpecs(),
      ]);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Failed to transition spec');
    }
  }

  async function handleSignOff(request: SignOffRequest): Promise<void> {
    if (!selectedSpec) return;
    try {
      const authState = getAuthState();
      if (!authState.user?.tenantId) return;
      await submitSignOff(authState.user.tenantId, projectId, selectedSpec.id, request);
      await loadSignOffSummary(selectedSpec.id);
      await loadSpecDetail(selectedSpec.id);
      await loadSpecs();
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Failed to submit sign-off');
    }
  }

  async function handleCreateNewVersion(): Promise<void> {
    if (!selectedSpec) return;
    try {
      const authState = getAuthState();
      if (!authState.user?.tenantId) return;
      const detail = await createNewVersion(authState.user.tenantId, projectId, selectedSpec.id);
      const newSpec = mapSpecDetailToSpec(detail);
      allSpecs = [newSpec, ...allSpecs];
      selectedSpecId = detail.id;
      selectedSpec = detail;
      viewMode = 'detail';
      toast.success('New version created');
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Failed to create new version');
    }
  }

  function handleRequirementClick(requirementId: string) {
    setPendingEntityId(requirementId);
    setSelectedNodePath(`${projectId}/requirements`);
  }

  function handleConversationClick(conversationId: string) {
    setPendingEntityId(conversationId);
    setSelectedNodePath(`${projectId}/conversations`);
  }
</script>

<PageShell class={className}>
  <PageHeader title={pageTitle} meta={pageMeta} {loading}>
    {#snippet actions()}
      <IconButton
        icon="refresh-cw"
        size="sm"
        variant="ghost"
        title="Refresh specs"
        onclick={() => loadSpecs()}
      />
      <IconButton
        icon="plus"
        size="sm"
        variant="ghost"
        title="New Spec"
        onclick={handleCreateSpec}
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
        <Button variant="secondary" size="sm" onclick={() => loadSpecs()} class="mt-2">
          Retry
        </Button>
      </div>
    {:else}
      <SidebarDetailLayout
        showRightPanel={!!selectedSpec && viewMode === 'detail'}
        sidebarWidth={SIDEBAR_DETAIL_LAYOUT.sidebarWidth}
        minSidebarWidth={SIDEBAR_DETAIL_LAYOUT.minSidebarWidth}
        maxSidebarWidth={SIDEBAR_DETAIL_LAYOUT.maxSidebarWidth}
        rightPanelWidth={SIDEBAR_DETAIL_LAYOUT.rightPanelWidth}
        minRightPanelWidth={SIDEBAR_DETAIL_LAYOUT.minRightPanelWidth}
        maxRightPanelWidth={SIDEBAR_DETAIL_LAYOUT.maxRightPanelWidth}
      >
        {#snippet sidebar()}
          <div class="flex flex-col h-full bg-[var(--color-bg-primary)]">
            <!-- Header with Search and Actions -->
            <div class="flex-shrink-0 flex items-center p-2 min-h-12 border-b border-[var(--color-border-primary)]">
              <div class="flex items-center gap-1 w-full">
                <div class="relative flex-1">
                  <Icon
                    name="search"
                    size="sm"
                    class="absolute left-2.5 top-1/2 -translate-y-1/2 text-gray-400"
                  />
                  <Input
                    type="text"
                    placeholder="Search specs..."
                    value={searchQuery}
                    oninput={(e) => searchQuery = (e.target as HTMLInputElement).value}
                    variant="flat"
                    class="pl-8 w-full"
                    size="sm"
                  />
                </div>
                <!-- Filter Dropdown -->
                <Dropdown position="bottom-right">
                  {#snippet trigger()}
                    <IconButton icon="more-vertical" size="sm" variant="ghost" title="Filter options" />
                  {/snippet}
                  <div class="py-1 min-w-[180px]">
                    <div class="px-3 py-1 text-xs font-semibold text-[var(--color-text-tertiary)] uppercase">
                      Status
                    </div>
                    <Button
                      variant="unstyled"
                      class="w-full flex items-center gap-2 px-3 py-1.5 text-sm text-left transition-colors
                        {statusFilter === 'all'
                          ? 'bg-[var(--color-accent-primary)]/10 text-[var(--color-accent-primary)]'
                          : 'text-[var(--color-text-secondary)] hover:bg-[var(--color-bg-tertiary)]'}"
                      onclick={() => statusFilter = 'all'}
                    >
                      <Icon name="layers" size="sm" />
                      <span class="flex-1">All</span>
                      {#if statusFilter === 'all'}
                        <Icon name="check" size="sm" class="text-[var(--color-accent-primary)]" />
                      {/if}
                      <span class="text-xs opacity-70">({statusCounts.all})</span>
                    </Button>
                    {#each (['Draft', 'InReview', 'Approved', 'Locked'] as SpecStatus[]) as status (status)}
                      {@const style = SPEC_STATUS_STYLES[status]}
                      <Button
                        variant="unstyled"
                        class="w-full flex items-center gap-2 px-3 py-1.5 text-sm text-left transition-colors
                          {statusFilter === status
                            ? 'bg-[var(--color-accent-primary)]/10 text-[var(--color-accent-primary)]'
                            : 'text-[var(--color-text-secondary)] hover:bg-[var(--color-bg-tertiary)]'}"
                        onclick={() => statusFilter = status}
                      >
                        <Icon name={style.icon} size="sm" class={style.color} />
                        <span class="flex-1">{style.label}</span>
                        {#if statusFilter === status}
                          <Icon name="check" size="sm" class="text-[var(--color-accent-primary)]" />
                        {/if}
                        <span class="text-xs opacity-70">({statusCounts[status]})</span>
                      </Button>
                    {/each}
                  </div>
                </Dropdown>
              </div>
            </div>

            <!-- List Header -->
            <div class="flex items-center gap-2 px-3 py-1.5 border-b border-[var(--color-border-primary)] bg-[var(--color-bg-secondary)]">
              <span class="w-4"></span>
              <span class="flex-1 text-xs font-medium text-[var(--color-text-tertiary)] uppercase">Title</span>
              <span class="w-24 text-xs font-medium text-[var(--color-text-tertiary)] uppercase text-right">Code</span>
              <span class="w-8 text-xs font-medium text-[var(--color-text-tertiary)] uppercase text-right">Ver</span>
            </div>

            <!-- Spec List -->
            <div class="flex-1 overflow-y-auto pb-1" onscroll={handleSpecListScroll}>
              {#each filteredSpecs as spec (spec.id)}
                {@const style = SPEC_STATUS_STYLES[spec.status]}
                <RichListItem
                  selected={selectedSpecId === spec.id}
                  title={spec.title}
                  leadingIcon={style.icon}
                  leadingIconClass={style.textColor}
                  onclick={() => handleSelectSpec(spec)}
                >
                  {#snippet trailing()}
                    <span class="ml-auto w-24 text-right flex-shrink-0 text-xs font-mono text-[var(--color-text-tertiary)]">
                      {spec.code}
                    </span>
                    <span class="w-8 text-right flex-shrink-0 text-xs text-[var(--color-text-tertiary)]">
                      v{spec.version}
                    </span>
                  {/snippet}
                  {#snippet badges()}
                    <span class="px-1.5 py-0.5 text-[0.625rem] rounded {style.bgColor} {style.textColor}">
                      {style.label}
                    </span>
                  {/snippet}
                </RichListItem>
              {/each}

              {#if filteredSpecs.length === 0}
                <div class="p-4 text-center text-sm text-[var(--color-text-tertiary)]">
                  <Icon name="inbox" size="lg" class="mx-auto mb-2 opacity-50" />
                  <p>No specs found</p>
                </div>
              {/if}

              {#if loadingMore}
                <div class="flex items-center justify-center py-3">
                  <Spinner size="sm" />
                </div>
              {/if}
            </div>

          </div>
        {/snippet}

        {#snippet rightPanel()}
          {#if selectedSpec}
            <div class="flex flex-col h-full">
              <TraceGraphSection
                entityType="Spec"
                entityId={selectedSpec.id}
                entityCode={selectedSpec.code}
                {projectId}
                onNodeClick={handleTraceNodeClick}
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
                            <span class="text-xs font-medium flex-shrink-0 {SPEC_STATUS_STYLES[ver.status].textColor}">
                              {SPEC_STATUS_STYLES[ver.status].label}
                            </span>
                            <span class="ml-auto text-xs text-[var(--color-text-muted)] flex-shrink-0">
                              {formatShortDate(ver.createdAt)}
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
            </div>
          {/if}
        {/snippet}

        {#snippet header()}
          {#if viewMode === 'create'}
            <DetailHeader>
              <h2 class="text-sm font-medium text-[var(--color-text-primary)]">New Spec</h2>
              {#snippet actions()}
                <IconButton icon="check" variant="success" size="sm" title="Create" onclick={() => (document.getElementById('spec-form') as HTMLFormElement | null)?.requestSubmit()} disabled={formLoading} />
                <IconButton icon="x" variant="ghost" size="sm" title="Cancel" onclick={handleCancelForm} />
              {/snippet}
            </DetailHeader>
          {:else if viewMode === 'edit' && selectedSpec}
            <DetailHeader>
              <h2 class="text-sm font-medium text-[var(--color-text-primary)]">Edit Spec</h2>
              {#snippet actions()}
                <IconButton icon="check" variant="success" size="sm" title="Save" onclick={() => (document.getElementById('spec-form') as HTMLFormElement | null)?.requestSubmit()} disabled={formLoading} />
                <IconButton icon="x" variant="ghost" size="sm" title="Cancel" onclick={handleCancelForm} />
              {/snippet}
            </DetailHeader>
          {:else if selectedSpec}
            <DetailHeader>
              {#snippet leading()}
                <Icon name="file-text" size="md" class="text-[var(--color-info-600)]" />
              {/snippet}
              <DetailTitle title={selectedSpec.title} code={selectedSpec.code} />
              {#snippet actions()}
                {#if selectedSpec!.status === 'Draft'}
                  <IconButton icon="edit" variant="ghost" size="sm" title="Edit" onclick={handleEditSpec} />
                {/if}
                {#if selectedSpec!.status !== 'Locked'}
                  <Dropdown position="bottom-right">
                    {#snippet trigger()}
                      <IconButton icon="more-vertical" variant="ghost" size="sm" title="More actions" />
                    {/snippet}
                    <div class="py-1 min-w-[160px]">
                      <Button
                        variant="unstyled"
                        onclick={handleDeactivateSpec}
                        class="w-full text-left px-3 py-1.5 text-xs font-medium hover:bg-[var(--color-surface-200)] transition-colors text-[var(--color-warning-600)] flex items-center gap-2"
                      >
                        <Icon name="lock" size="xs" />
                        Deactivate
                      </Button>
                    </div>
                  </Dropdown>
                {/if}
                <IconButton icon="x" variant="ghost" size="sm" title="Close" onclick={handleCloseSpec} />
              {/snippet}
            </DetailHeader>
          {:else}
            <DetailHeader>
              <DetailTitle title="Overview" />
            </DetailHeader>
          {/if}
        {/snippet}

        <!-- Main Content: Spec Detail -->
        {#if viewMode === 'create'}
          <div class="flex-1 min-h-0 overflow-y-auto p-4">
              <SpecForm
                mode="create"
                tenantId={getAuthState().user?.tenantId ?? ''}
                {projectId}
                loading={formLoading}
                {memberCandidates}
                onSubmit={handleCreateSpecSubmit}
              />
          </div>
        {:else if viewMode === 'edit' && selectedSpec}
          <div class="flex-1 min-h-0 overflow-y-auto p-4">
              <SpecForm
                mode="edit"
                tenantId={getAuthState().user?.tenantId ?? ''}
                {projectId}
                loading={formLoading}
                {memberCandidates}
                initialData={{
                  code: selectedSpec.code,
                  title: selectedSpec.title,
                  description: selectedSpec.description,
                  decision: selectedSpec.decision,
                  context: selectedSpec.context,
                  scope: selectedSpec.scope,
                  outOfScope: selectedSpec.outOfScope,
                  definitions: selectedSpec.definitions,
                  acceptanceCriteria: selectedSpec.acceptanceCriteria,
                  owners: selectedSpec.owners,
                  reviewTrigger: selectedSpec.reviewTrigger,
                  requirementId: selectedSpec.requirementId ?? undefined,
                  bornFromConversationId: selectedSpec.bornFromConversationId ?? undefined,
                }}
                onSubmit={handleUpdateSpec}
              />
          </div>
        {:else if detailLoading}
          <div class="flex-1 flex items-center justify-center">
            <Spinner size="lg" />
          </div>
        {:else if selectedSpec}
          <div class="flex flex-col flex-1 min-h-0">
            <SpecDetailView
              spec={selectedSpec}
              showHeader={false}
              onEdit={handleEditSpec}
              onClose={handleCloseSpec}
              onTransition={handleTransition}
              onNewVersion={handleCreateNewVersion}
              onSignOff={handleSignOff}
              signOffSummary={signOffSummary ?? undefined}
              currentUserId={currentUserId ?? undefined}
              entitySchemas={entitySchemas}
              entitySchemasLoading={entitySchemasLoading}
              entitySchemasError={entitySchemasError}
              onLoadEntitySchemas={loadEntitySchemas}
              onCreateEntitySchema={handleCreateEntitySchema}
              onUpdateEntitySchema={handleUpdateEntitySchema}
              onDeleteEntitySchema={handleDeleteEntitySchema}
              onRequirementClick={handleRequirementClick}
              onConversationClick={handleConversationClick}
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
                    <SpecVersionDetail version={activeVersion} />
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
                <StatCard title="Total Specs" value={statusCounts.all} icon="file-text" iconColor="blue-500" />
                <StatCard title="Draft" value={statusCounts.Draft} icon="edit" iconColor="gray-500" />
                <StatCard title="In Review" value={statusCounts.InReview} icon="eye" iconColor="yellow-500" />
                <StatCard title="Approved/Locked" value={statusCounts.Approved + statusCounts.Locked} icon="check-circle" iconColor="green-500" />
              </CardGrid>

              <!-- Quick Guide -->
              <SurfaceCard padding="lg">
                <h3 class="text-sm font-semibold text-[var(--color-text-primary)] mb-3">About Specs</h3>
                <div class="space-y-2 text-sm text-[var(--color-text-secondary)]">
                  <p>
                    Specs define the technical decisions and requirements for your project.
                    They follow a workflow: Draft → In Review → Approved → Locked.
                  </p>
                  <div class="flex items-start gap-2 mt-3">
                    <Icon name="edit" size="sm" class="text-gray-500 mt-0.5 flex-shrink-0" />
                    <span><strong>Draft</strong> - Work in progress, editable</span>
                  </div>
                  <div class="flex items-start gap-2">
                    <Icon name="eye" size="sm" class="text-yellow-500 mt-0.5 flex-shrink-0" />
                    <span><strong>In Review</strong> - Under team review for sign-offs</span>
                  </div>
                  <div class="flex items-start gap-2">
                    <Icon name="check-circle" size="sm" class="text-green-500 mt-0.5 flex-shrink-0" />
                    <span><strong>Approved</strong> - Accepted, ready for implementation</span>
                  </div>
                  <div class="flex items-start gap-2">
                    <Icon name="lock" size="sm" class="text-purple-500 mt-0.5 flex-shrink-0" />
                    <span><strong>Locked</strong> - Immutable, used for artifact generation</span>
                  </div>
                </div>
              </SurfaceCard>

              <!-- Empty state message -->
              {#if allSpecs.length === 0}
                <div class="text-center py-8">
                  <Icon name="file-text" size="xl" class="mx-auto mb-4 text-[var(--color-text-tertiary)] opacity-50" />
                  <h3 class="text-lg font-medium text-[var(--color-text-primary)] mb-2">No specs yet</h3>
                  <p class="text-sm text-[var(--color-text-secondary)] mb-4">
                    Start documenting your project decisions by creating specs.
                  </p>
                  <Button variant="primary" size="md" onclick={handleCreateSpec}>
                    <Icon name="plus" size="sm" />
                    Create First Spec
                  </Button>
                </div>
              {:else}
                <div class="text-center py-6">
                  <Icon name="mouse-pointer" size="lg" class="mx-auto mb-2 text-[var(--color-text-tertiary)] opacity-70" />
                  <p class="text-sm font-medium text-[var(--color-text-primary)]">Select a spec</p>
                  <p class="text-xs text-[var(--color-text-secondary)] mt-1">
                    Choose a spec from the sidebar to view details.
                  </p>
                </div>
              {/if}
          </div>
        {/if}
      </SidebarDetailLayout>
    {/if}
  </PageBody>
</PageShell>
