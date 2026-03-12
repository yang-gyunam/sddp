<!-- Activity: Projects > Nav: Requirements (project-{id}-requirements) | Screen ID: PRJ-REQ-001 -->
<script lang="ts">
  import { untrack } from 'svelte';
  import { Icon, Input, IconButton, Button, CardGrid, Checkbox, Spinner, ResizeHandle } from '@sddp/ui';
  import { PageShell, PageHeader, PageBody, SidebarDetailLayout, Dropdown, RichListItem, SIDEBAR_DETAIL_LAYOUT, SurfaceCard, PriorityBadge, getTabState, setTabState, RouterService, toast, formatDateWithOptions } from '@sddp/shell';
  import DetailHeader from '../../../shared/components/idioms/DetailHeader.svelte';
  import DetailTitle from '../../../shared/components/idioms/DetailTitle.svelte';
  import { StatCard } from '../../../shared/components/idioms';
  import { getAuthState } from '@sddp/shell/auth';
  import type { ProjectDetail } from '../../types';
  import { getProjectService, getProjectById } from '../../services/ProjectService';
  import { setSelectedNodePath, consumePendingEntityId, setPendingEntityId } from '../../stores';
  import RequirementDetailView from '../../../requirements/components/sections/RequirementDetailView.svelte';
  import RequirementForm from '../../../requirements/components/idioms/RequirementForm.svelte';
  import RequirementLevelBadge from '../../../requirements/components/idioms/RequirementLevelBadge.svelte';
  import type { Requirement, RequirementDetail, RequirementLevel, RequirementStatus, CreateRequirementRequest, UpdateRequirementRequest } from '../../../requirements/types';
  import type { ComboboxOption } from '@sddp/ui';
  import { REQUIREMENT_LEVEL_STYLES, REQUIREMENT_STATUS_STYLES, VALID_STATUS_TRANSITIONS, isEditable } from '../../../requirements/types';
  import { getRequirementService } from '../../../requirements/services';
  import { getConversationService } from '../../../conversations/services/ConversationService';
  import SpecForm from '../../../specs/components/idioms/SpecForm.svelte';
  import type { CreateSpecRequest } from '../../../specs/types';
  import { createSpec } from '../../../specs/services';
  import { SvelteSet } from 'svelte/reactivity';
  import { TraceGraphSection } from '../../../relationship/components/sections';
  import type { PrimaryFlowNode } from '../../../relationship/types';
  import type { RequirementVersion } from '../../../requirements/types';
  import RequirementVersionDetail from '../../../requirements/components/idioms/RequirementVersionDetail.svelte';

  interface Props {
    projectId: string;
    projectName?: string;
    project?: ProjectDetail;
    tabId?: string;
    class?: string;
  }

  let { projectId, projectName = '', project, tabId = '', class: className = '' }: Props = $props();

  const pageTitle = 'Requirements';
  const pageMeta = $derived(project?.name || projectName || undefined);

  // State
  let requirements = $state<Requirement[]>([]);
  let selectedRequirementId = $state<string | null>(null);
  let selectedRequirement = $state<RequirementDetail | null>(null);
  let loading = $state(false);
  let loadingMore = $state(false);
  let requirementsPageNumber = $state(1);
  let hasMoreRequirements = $state(false);
  const REQUIREMENTS_PAGE_SIZE = 20;
  let latestRequirementsRequestId = $state(0);
  let detailLoading = $state(false);
  let viewMode = $state<'detail' | 'create' | 'edit' | 'promote'>('detail');
  let formLoading = $state(false);
  let error = $state<string | null>(null);
  let parentCandidates = $state<ComboboxOption[]>([]);
  let memberCandidates = $state<ComboboxOption[]>([]);
  let searchQuery = $state('');
  let levelFilter = $state<RequirementLevel | 'all'>('all');
  let statusFilter = $state<RequirementStatus | 'all'>('all');
  let conversationCandidates = $state<ComboboxOption[]>([]);
  let conversationSearchLoading = $state(false);
  let conversationSearchTimer: ReturnType<typeof setTimeout> | undefined;

  // Version history state
  let versionHistory = $state<RequirementVersion[]>([]);
  let versionHistoryLoading = $state(false);
  let selectedVersionIds = new SvelteSet<string>();
  let activeVersionTabId = $state<string | null>(null);
  let prevVersionHistoryReqId = $state<string | null>(null);

  // Resize state for version comparison panel
  const VERSION_PANEL_MIN = 150;
  const VERSION_PANEL_MAX = 600;
  const VERSION_PANEL_DEFAULT = 280;
  let versionPanelHeight = $state(VERSION_PANEL_DEFAULT);
  let isResizingVersionPanel = $state(false);
  let resizeStartY = 0;
  let resizeStartHeight = 0;

  interface ProjectRequirementsTabState {
    searchQuery: string;
    levelFilter: RequirementLevel | 'all';
    statusFilter: RequirementStatus | 'all';
    selectedRequirementId: string | null;
  }

  const tabStateKey = $derived(tabId || `project-${projectId}-requirements`);
  let restoredKey = $state<string | null>(null);
  let isRestored = $state(false);

  $effect(() => {
    if (!tabStateKey || restoredKey === tabStateKey) return;
    const saved = getTabState<ProjectRequirementsTabState>(tabStateKey);
    if (saved) {
      searchQuery = saved.searchQuery ?? '';
      levelFilter = saved.levelFilter ?? 'all';
      statusFilter = saved.statusFilter ?? 'all';
      selectedRequirementId = saved.selectedRequirementId ?? null;
    }
    restoredKey = tabStateKey;
    isRestored = true;
  });

  // Persist tab state
  $effect(() => {
    if (!tabStateKey || !isRestored) return;
    const state: ProjectRequirementsTabState = {
      searchQuery,
      levelFilter,
      statusFilter,
      selectedRequirementId,
    };
    setTabState<ProjectRequirementsTabState>(tabStateKey, state);
  });

  // Load requirements for this project
  $effect(() => {
    // Check for cross-type navigation (e.g., from Specs → Requirements)
    const pendingId = consumePendingEntityId();
    if (pendingId) {
      selectedRequirementId = pendingId;
    }
    untrack(() => loadRequirements());
  });

  async function loadRequirements(reset: boolean = true): Promise<void> {
    if (!reset && (loading || loadingMore || !hasMoreRequirements)) return;

    const requestId = ++latestRequirementsRequestId;
    const nextPageNumber = reset ? 1 : requirementsPageNumber + 1;

    if (reset) {
      loading = true;
      error = null;
      hasMoreRequirements = false;
      requirementsPageNumber = 1;
    } else {
      loadingMore = true;
    }

    try {
      const authState = getAuthState();
      if (!authState.user?.tenantId) {
        throw new Error('User not authenticated or missing tenant');
      }
      const service = getProjectService();
      service.setTenantId(authState.user.tenantId);

      const tenantId = authState.user.tenantId;
      const response = await service.getProjectRequirements(
        projectId,
        nextPageNumber,
        REQUIREMENTS_PAGE_SIZE
      );
      if (requestId !== latestRequirementsRequestId) return;

      const mapped = response.items.map((r) => ({
        id: r.id,
        tenantId,
        projectId,
        code: r.code,
        title: r.title,
        level: r.level,
        priority: r.priority ?? 'Medium',
        status: r.status,
        description: '',
        parentId: null,
        childrenCount: r.childrenCount,
        conversationId: null,
        version: '1',
        createdAt: r.createdAt,
        updatedAt: r.updatedAt,
      })) as Requirement[];

      requirements = reset ? mapped : [...requirements, ...mapped];
      requirementsPageNumber = response.pageNumber ?? response.page ?? nextPageNumber;
      hasMoreRequirements = response.hasNextPage ?? requirements.length < response.totalCount;

      if (reset && selectedRequirementId && selectedRequirement?.id !== selectedRequirementId) {
        await loadRequirementDetail(selectedRequirementId);
      }
    } catch (err) {
      if (requestId !== latestRequirementsRequestId) return;
      const message = err instanceof Error ? err.message : 'Failed to load requirements';
      console.error('Failed to load requirements:', err);
      error = message;
      hasMoreRequirements = false;
    } finally {
      if (requestId === latestRequirementsRequestId) {
        loading = false;
        loadingMore = false;
      }
    }
  }

  async function loadRequirementDetail(requirementId: string) {
    detailLoading = true;
    try {
      const authState = getAuthState();
      if (!authState.user?.tenantId) {
        throw new Error('User not authenticated');
      }
      const requirementService = getRequirementService();
      requirementService.setContext(authState.user.tenantId, projectId);

      const detail = await requirementService.getRequirementById(requirementId);
      selectedRequirement = detail;
    } catch (err) {
      console.error('Failed to load requirement detail:', err);
      // Fallback: create detail from requirement list item
      const req = requirements.find((r) => r.id === requirementId);
      if (req) {
        selectedRequirement = {
          ...req,
          priority: req.priority ?? 'Medium',
          parentCode: null,
          parentTitle: null,
          parentLevel: null,
          conversationName: null,
          conversationDescription: null,
          conversationType: null,
          ancestors: [],
          siblings: [],
          children: [],
          linkedSpecs: [],
          owner: null,
          createdBy: { id: '', name: null, avatarUrl: null },
          updatedBy: { id: '', name: null, avatarUrl: null },
          validFrom: req.createdAt,
          validTo: null,
        };
      }
    } finally {
      detailLoading = false;
    }
  }

  function handleSelectRequirement(req: Requirement) {
    selectedRequirementId = req.id;
    viewMode = 'detail';
    loadRequirementDetail(req.id);
  }

  function handleRequirementListScroll(e: Event): void {
    if (loading || loadingMore || !hasMoreRequirements) return;

    const target = e.currentTarget as HTMLElement;
    const nearBottom = target.scrollHeight - target.scrollTop - target.clientHeight <= 100;
    if (nearBottom) {
      loadRequirements(false);
    }
  }

  function handleTraceNodeClick(node: PrimaryFlowNode): void {
    if (!node.id) return;
    selectedRequirementId = node.id;
    viewMode = 'detail';
    loadRequirementDetail(node.id);
  }

  function handleCloseRequirement(): void {
    selectedRequirementId = null;
    selectedRequirement = null;
    viewMode = 'detail';
  }

  function handleEditRequirement(): void {
    if (!selectedRequirement) return;
    viewMode = 'edit';
    if (memberCandidates.length === 0) {
      loadMemberCandidates();
    }
    // Pre-populate conversation option so Combobox shows current name
    if (selectedRequirement.conversationId && selectedRequirement.conversationName) {
      const prefix = selectedRequirement.conversationType === 'Channel' ? '#' : '';
      conversationCandidates = [{
        value: selectedRequirement.conversationId,
        label: `${prefix}${selectedRequirement.conversationName}`,
        description: selectedRequirement.conversationDescription ?? selectedRequirement.conversationType ?? undefined,
      }];
    } else {
      conversationCandidates = [];
    }
    // Pre-populate parent option so Combobox shows current parent
    if (selectedRequirement.parentId && (selectedRequirement.parentTitle || selectedRequirement.parentCode)) {
      parentCandidates = [{
        value: selectedRequirement.parentId,
        label: `${selectedRequirement.parentCode ?? ''}  ${selectedRequirement.parentTitle ?? ''}`.trim(),
        description: selectedRequirement.parentLevel ? `Level ${selectedRequirement.parentLevel}` : undefined,
      }];
    } else {
      parentCandidates = [];
    }
  }

  function handlePromoteRequirement(): void {
    if (!selectedRequirement) return;
    viewMode = 'promote';
  }

  async function handleStatusChange(newStatus: RequirementStatus): Promise<void> {
    if (!selectedRequirement) return;
    try {
      const authState = getAuthState();
      if (!authState.user?.tenantId) return;
      const requirementService = getRequirementService();
      requirementService.setContext(authState.user.tenantId, projectId);

      await requirementService.transitionStatus(selectedRequirement.id, { newStatus });
      // Reload detail to reflect updated status
      await loadRequirementDetail(selectedRequirement.id);
      // Update list
      requirements = requirements.map((req) =>
        req.id === selectedRequirement!.id ? { ...req, status: newStatus } : req
      );
      // Refresh version history
      prevVersionHistoryReqId = null;
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Failed to update status');
    }
  }

  function handleConversationSearch(query: string): void {
    if (conversationSearchTimer) clearTimeout(conversationSearchTimer);
    if (!query.trim()) {
      conversationCandidates = [];
      return;
    }
    conversationSearchTimer = setTimeout(async () => {
      conversationSearchLoading = true;
      try {
        const authState = getAuthState();
        if (!authState.user?.tenantId) return;
        const svc = getConversationService();
        svc.setContext(authState.user.tenantId, projectId);
        const results = await svc.searchConversations(query, 15);
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

  async function handleUpdateRequirement(data: UpdateRequirementRequest): Promise<void> {
    if (!selectedRequirement) return;
    formLoading = true;
    try {
      const authState = getAuthState();
      if (!authState.user?.tenantId) return;
      const requirementService = getRequirementService();
      requirementService.setContext(authState.user.tenantId, projectId);

      const { conversationId: newConvId, ...updateData } = data;
      await requirementService.updateRequirement(selectedRequirement.id, updateData);

      // Handle conversation link/unlink
      const oldConvId = selectedRequirement.conversationId;
      if (newConvId && newConvId !== oldConvId) {
        await requirementService.linkConversation(selectedRequirement.id, { conversationId: newConvId });
      } else if (!newConvId && oldConvId) {
        await requirementService.unlinkConversation(selectedRequirement.id);
      }

      // Reload detail to get full updated data
      const detail = await requirementService.getRequirementById(selectedRequirement.id);
      selectedRequirement = detail;
      requirements = requirements.map((req) =>
        req.id === detail.id
          ? { ...req, title: detail.title, description: detail.description, updatedAt: detail.updatedAt }
          : req
      );
      viewMode = 'detail';
      // Refresh version history
      prevVersionHistoryReqId = null;
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Failed to update requirement');
    } finally {
      formLoading = false;
    }
  }

  let parentSearchLoading = $state(false);
  let parentSearchTimer: ReturnType<typeof setTimeout> | undefined;

  function handleParentSearch(query: string): void {
    if (parentSearchTimer) clearTimeout(parentSearchTimer);
    if (!query.trim()) {
      parentCandidates = [];
      return;
    }
    parentSearchTimer = setTimeout(async () => {
      parentSearchLoading = true;
      try {
        const authState = getAuthState();
        if (!authState.user?.tenantId) return;
        const svc = getRequirementService();
        svc.setContext(authState.user.tenantId, projectId);
        const results = await svc.searchRequirements(query, 15);
        parentCandidates = results.map((r) => ({
          value: r.id,
          label: `${r.code}  ${r.title}`,
          description: `Level ${r.level}`,
        }));
      } catch {
        parentCandidates = [];
      } finally {
        parentSearchLoading = false;
      }
    }, 300);
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

  function handleCreateRequirement() {
    selectedRequirementId = null;
    selectedRequirement = null;
    viewMode = 'create';
    parentCandidates = [];
    loadMemberCandidates();
  }

  function handleCancelForm(): void {
    if (viewMode === 'edit') {
      viewMode = 'detail';
      return;
    }
    viewMode = 'detail';
  }

  async function handlePromoteSpecSubmit(data: CreateSpecRequest): Promise<void> {
    if (!selectedRequirement) return;
    formLoading = true;
    try {
      const authState = getAuthState();
      if (!authState.user?.tenantId) return;

      const detail = await createSpec(authState.user.tenantId, projectId, data);
      viewMode = 'detail';

      // Navigate to the specs page and select the newly created spec
      setPendingEntityId(detail.id);
      setSelectedNodePath(`${projectId}/specs`);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Failed to promote to spec');
    } finally {
      formLoading = false;
    }
  }

  async function handleCreateRequirementSubmit(data: CreateRequirementRequest): Promise<void> {
    formLoading = true;
    try {
      const authState = getAuthState();
      if (!authState.user?.tenantId) return;
      const requirementService = getRequirementService();
      requirementService.setContext(authState.user.tenantId, projectId);

      const { conversationId: convId, ...createData } = data;
      const detail = await requirementService.createRequirement(createData);

      // Link conversation if specified (separate API)
      if (convId) {
        await requirementService.linkConversation(detail.id, { conversationId: convId });
      }

      // Reload detail to get full data (including conversation info)
      const fullDetail = convId
        ? await requirementService.getRequirementById(detail.id)
        : detail;

      const newRequirement: Requirement = {
        id: fullDetail.id,
        tenantId: fullDetail.tenantId,
        projectId: fullDetail.projectId,
        code: fullDetail.code,
        title: fullDetail.title,
        level: fullDetail.level,
        priority: fullDetail.priority ?? 'Medium',
        status: fullDetail.status,
        description: fullDetail.description ?? '',
        parentId: fullDetail.parentId ?? null,
        childrenCount: fullDetail.children?.length ?? 0,
        conversationId: fullDetail.conversationId ?? null,
        version: fullDetail.version,
        createdAt: fullDetail.createdAt,
        updatedAt: fullDetail.updatedAt,
      };

      requirements = [newRequirement, ...requirements];
      selectedRequirementId = fullDetail.id;
      selectedRequirement = fullDetail;
      viewMode = 'detail';
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Failed to create requirement');
    } finally {
      formLoading = false;
    }
  }

  // Filter requirements
  const filteredRequirements = $derived(
    requirements.filter((r) => {
      if (searchQuery && !r.title.toLowerCase().includes(searchQuery.toLowerCase()) && !r.code.toLowerCase().includes(searchQuery.toLowerCase())) {
        return false;
      }
      if (levelFilter !== 'all' && r.level !== levelFilter) return false;
      if (statusFilter !== 'all' && r.status !== statusFilter) return false;
      return true;
    })
  );

  // Status counts for filter badges
  const statusCounts = $derived({
    all: requirements.length,
    Draft: requirements.filter((r) => r.status === 'Draft').length,
    InReview: requirements.filter((r) => r.status === 'InReview').length,
    Approved: requirements.filter((r) => r.status === 'Approved').length,
    Deprecated: requirements.filter((r) => r.status === 'Deprecated').length,
  });

  // Level counts
  const levelCounts = $derived({
    all: requirements.length,
    A: requirements.filter((r) => r.level === 'A').length,
    B: requirements.filter((r) => r.level === 'B').length,
    C: requirements.filter((r) => r.level === 'C').length,
  });

  function handleChildSelect(child: Requirement) {
    handleSelectRequirement(child);
  }

  function handleParentSelect(parentId: string) {
    selectedRequirementId = parentId;
    viewMode = 'detail';
    loadRequirementDetail(parentId);
  }

  // Version history — derived values
  const pastVersions = $derived(
    selectedRequirement
      ? versionHistory.filter((v) => v.id !== selectedRequirement!.id)
      : versionHistory
  );
  const selectedVersionList = $derived(
    pastVersions.filter((v) => selectedVersionIds.has(v.id))
  );
  const activeVersion = $derived(
    selectedVersionList.find((v) => v.id === activeVersionTabId) ?? selectedVersionList[0] ?? null
  );

  // Load version history when selected requirement changes
  $effect(() => {
    const reqId = selectedRequirement?.id ?? null;
    if (!reqId || reqId === prevVersionHistoryReqId) return;
    prevVersionHistoryReqId = reqId;
    selectedVersionIds.clear();
    activeVersionTabId = null;
    untrack(() => loadVersionHistory(reqId));
  });

  async function loadVersionHistory(requirementId: string): Promise<void> {
    versionHistoryLoading = true;
    try {
      const authState = getAuthState();
      if (!authState.user?.tenantId) return;
      const svc = getRequirementService();
      svc.setContext(authState.user.tenantId, projectId);
      versionHistory = await svc.getVersionHistory(requirementId);
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

  function formatShortDate(dateStr: string): string {
    return formatDateWithOptions(dateStr, { month: 'short', day: 'numeric' });
  }

  function handleConversationClick(conversationId: string) {
    RouterService.navigate(`/conversation/${conversationId}`);
  }

  // Resize handlers for version comparison panel
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

  function handleResizePointerUp(e: PointerEvent) {
    isResizingVersionPanel = false;
    const target = e.target as HTMLElement;
    if (target.hasPointerCapture?.(e.pointerId)) {
      target.releasePointerCapture(e.pointerId);
    }
    document.removeEventListener('pointermove', handleResizePointerMove);
    document.removeEventListener('pointerup', handleResizePointerUp);
    document.body.style.cursor = '';
    document.body.style.userSelect = '';
  }
</script>

<PageShell class={className}>
  <PageHeader title={pageTitle} meta={pageMeta} {loading}>
    {#snippet actions()}
      <IconButton
        icon="refresh-cw"
        size="sm"
        variant="ghost"
        title="Refresh requirements"
        onclick={() => loadRequirements()}
      />
      <IconButton
        icon="plus"
        size="sm"
        variant="ghost"
        title="New Requirement"
        onclick={handleCreateRequirement}
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
        <Button variant="secondary" size="sm" onclick={() => loadRequirements()} class="mt-2">
          Retry
        </Button>
      </div>
    {:else}
      <SidebarDetailLayout
        showRightPanel={!!selectedRequirement && viewMode === 'detail'}
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
                    placeholder="Search requirements..."
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
                    <!-- Level Filter -->
                    <div class="px-3 py-1 text-xs font-semibold text-[var(--color-text-tertiary)] uppercase">
                      Level
                    </div>
                    <Button
                      variant="unstyled"
                      class="w-full flex items-center gap-2 px-3 py-1.5 text-sm text-left transition-colors
                        {levelFilter === 'all'
                          ? 'bg-[var(--color-accent-primary)]/10 text-[var(--color-accent-primary)]'
                          : 'text-[var(--color-text-secondary)] hover:bg-[var(--color-bg-tertiary)]'}"
                      onclick={() => levelFilter = 'all'}
                    >
                      <Icon name="layers" size="sm" />
                      <span class="flex-1">All Levels</span>
                      {#if levelFilter === 'all'}
                        <Icon name="check" size="sm" class="text-[var(--color-accent-primary)]" />
                      {/if}
                      <span class="text-xs opacity-70">({levelCounts.all})</span>
                    </Button>
                    {#each (['A', 'B', 'C'] as RequirementLevel[]) as level (level)}
                      {@const lvlStyle = REQUIREMENT_LEVEL_STYLES[level]}
                      <Button
                        variant="unstyled"
                        class="w-full flex items-center gap-2 px-3 py-1.5 text-sm text-left transition-colors
                          {levelFilter === level
                            ? 'bg-[var(--color-accent-primary)]/10 text-[var(--color-accent-primary)]'
                            : 'text-[var(--color-text-secondary)] hover:bg-[var(--color-bg-tertiary)]'}"
                        onclick={() => levelFilter = level}
                      >
                        <span class="w-5 h-5 flex items-center justify-center text-xs font-bold rounded {lvlStyle.bgColor} {lvlStyle.textColor}">
                          {level}
                        </span>
                        <span class="flex-1">Level {level}</span>
                        {#if levelFilter === level}
                          <Icon name="check" size="sm" class="text-[var(--color-accent-primary)]" />
                        {/if}
                        <span class="text-xs opacity-70">({levelCounts[level]})</span>
                      </Button>
                    {/each}

                    <div class="my-1 border-t border-[var(--color-border-primary)]"></div>

                    <!-- Status Filter -->
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
                      <span class="flex-1">All Status</span>
                      {#if statusFilter === 'all'}
                        <Icon name="check" size="sm" class="text-[var(--color-accent-primary)]" />
                      {/if}
                      <span class="text-xs opacity-70">({statusCounts.all})</span>
                    </Button>
                    {#each (['Draft', 'InReview', 'Approved', 'Deprecated'] as RequirementStatus[]) as status (status)}
                      {@const style = REQUIREMENT_STATUS_STYLES[status]}
                      <Button
                        variant="unstyled"
                        class="w-full flex items-center gap-2 px-3 py-1.5 text-sm text-left transition-colors
                          {statusFilter === status
                            ? 'bg-[var(--color-accent-primary)]/10 text-[var(--color-accent-primary)]'
                            : 'text-[var(--color-text-secondary)] hover:bg-[var(--color-bg-tertiary)]'}"
                        onclick={() => statusFilter = status}
                      >
                        <Icon name={style.icon} size="sm" class={style.textColor} />
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

            <!-- List Header (matches RichListItem layout: leading + flex-1 content + meta) -->
            <div class="flex items-center gap-2 px-3 py-1.5 border-b border-[var(--color-border-primary)] bg-[var(--color-bg-secondary)]">
              <span class="flex-shrink-0 w-6 text-xs font-medium text-[var(--color-text-tertiary)] uppercase text-center">Lv</span>
              <span class="flex-1 text-xs font-medium text-[var(--color-text-tertiary)] uppercase">Title</span>
              <span class="flex-shrink-0 text-xs font-medium text-[var(--color-text-tertiary)] uppercase">Code</span>
            </div>

            <!-- Requirement List -->
            <div class="flex-1 overflow-y-auto pb-1" onscroll={handleRequirementListScroll}>
              {#each filteredRequirements as req (req.id)}
                {@const statusStyle = REQUIREMENT_STATUS_STYLES[req.status]}
                <RichListItem
                  selected={selectedRequirementId === req.id}
                  title={req.title}
                  meta={req.code}
                  onclick={() => handleSelectRequirement(req)}
                >
                  {#snippet leading()}
                    <RequirementLevelBadge level={req.level} size="sm" />
                  {/snippet}
                  {#snippet badges()}
                    <PriorityBadge priority={req.priority ?? 'Medium'} size="sm" />
                    <span class="px-1.5 py-0.5 text-[0.625rem] rounded {statusStyle.bgColor} {statusStyle.textColor}">
                      {statusStyle.label}
                    </span>
                    {#if req.childrenCount > 0}
                      <span class="text-xs text-[var(--color-text-tertiary)] flex items-center gap-0.5">
                        <Icon name="git-branch" size="xs" />
                        {req.childrenCount}
                      </span>
                    {/if}
                  {/snippet}
                </RichListItem>
              {/each}

              {#if filteredRequirements.length === 0}
                <div class="p-4 text-center text-sm text-[var(--color-text-tertiary)]">
                  <Icon name="inbox" size="lg" class="mx-auto mb-2 opacity-50" />
                  <p>No requirements found</p>
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
          {#if selectedRequirement}
            <div class="flex flex-col h-full">
              <TraceGraphSection
                entityType="Requirement"
                entityId={selectedRequirement.id}
                entityCode={selectedRequirement.code}
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
                            <span class="text-xs font-medium flex-shrink-0 {REQUIREMENT_STATUS_STYLES[ver.status].textColor}">
                              {REQUIREMENT_STATUS_STYLES[ver.status].label}
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
            </div>
          {/if}
        {/snippet}

        {#snippet header()}
          {#if viewMode === 'create'}
            <DetailHeader>
              <h2 class="text-sm font-medium text-[var(--color-text-primary)]">New Requirement</h2>
              {#snippet actions()}
                <IconButton icon="check" variant="success" size="sm" title="Create" onclick={() => (document.getElementById('requirement-form') as HTMLFormElement | null)?.requestSubmit()} disabled={formLoading} />
                <IconButton icon="x" variant="ghost" size="sm" title="Cancel" onclick={handleCancelForm} />
              {/snippet}
            </DetailHeader>
          {:else if viewMode === 'promote' && selectedRequirement}
            <DetailHeader>
              <h2 class="text-sm font-medium text-[var(--color-text-primary)]">Promote to Spec</h2>
              {#snippet actions()}
                <IconButton icon="check" variant="success" size="sm" title="Create" onclick={() => (document.getElementById('spec-form') as HTMLFormElement | null)?.requestSubmit()} disabled={formLoading} />
                <IconButton icon="x" variant="ghost" size="sm" title="Cancel" onclick={handleCancelForm} />
              {/snippet}
            </DetailHeader>
          {:else if viewMode === 'edit' && selectedRequirement}
            <DetailHeader>
              <h2 class="text-sm font-medium text-[var(--color-text-primary)]">Edit Requirement</h2>
              {#snippet actions()}
                <IconButton icon="check" variant="success" size="sm" title="Save" onclick={() => (document.getElementById('requirement-form') as HTMLFormElement | null)?.requestSubmit()} disabled={formLoading} />
                <IconButton icon="x" variant="ghost" size="sm" title="Cancel" onclick={handleCancelForm} />
              {/snippet}
            </DetailHeader>
          {:else if selectedRequirement}
            <DetailHeader>
              {#snippet leading()}
                <RequirementLevelBadge level={selectedRequirement!.level} size="md" />
              {/snippet}
              <DetailTitle title={selectedRequirement!.title} code={selectedRequirement!.code} />
              {#snippet actions()}
                {@const transitions = VALID_STATUS_TRANSITIONS[selectedRequirement!.status]}
                {#if isEditable(selectedRequirement!.status)}
                  <IconButton icon="edit" variant="ghost" size="sm" title="Edit" onclick={handleEditRequirement} />
                {/if}
                {#if transitions.length > 0 || selectedRequirement!.status === 'Approved'}
                  <Dropdown position="bottom-right">
                    {#snippet trigger()}
                      <IconButton icon="more-vertical" variant="ghost" size="sm" title="More actions" />
                    {/snippet}
                    <div class="py-1 min-w-[160px]">
                      {#if transitions.length > 0}
                        <div class="px-3 py-1 text-[0.625rem] font-semibold uppercase tracking-wider text-[var(--color-text-muted)]">
                          Transition to
                        </div>
                        {#each transitions as status (status)}
                          <Button
                            variant="unstyled"
                            onclick={() => handleStatusChange(status)}
                            class="w-full text-left px-3 py-1.5 text-xs font-medium hover:bg-[var(--color-surface-200)] transition-colors {REQUIREMENT_STATUS_STYLES[status].textColor}"
                          >
                            {REQUIREMENT_STATUS_STYLES[status].label}
                          </Button>
                        {/each}
                      {/if}
                      {#if selectedRequirement!.status === 'Approved' && selectedRequirement!.linkedSpecs.length === 0}
                        {#if transitions.length > 0}
                          <div class="my-1 border-t border-[var(--color-border-secondary)]"></div>
                        {/if}
                        <Button
                          variant="unstyled"
                          onclick={handlePromoteRequirement}
                          class="w-full text-left px-3 py-1.5 text-xs font-medium hover:bg-[var(--color-surface-200)] transition-colors text-[var(--color-text-primary)] flex items-center gap-2"
                        >
                          <Icon name="file-signature" size="xs" />
                          Promote to Spec
                        </Button>
                      {/if}
                    </div>
                  </Dropdown>
                {/if}
                <IconButton icon="x" variant="ghost" size="sm" title="Close" onclick={handleCloseRequirement} />
              {/snippet}
            </DetailHeader>
          {:else}
            <DetailHeader>
              <DetailTitle title="Overview" />
            </DetailHeader>
          {/if}
        {/snippet}

        <!-- Main Content: Requirement Detail -->
        {#if viewMode === 'create'}
          <div class="flex-1 min-h-0 overflow-y-auto p-4">
              <RequirementForm
                mode="create"
                loading={formLoading}
                {parentCandidates}
                {memberCandidates}
                {conversationCandidates}
                onSearchParent={handleParentSearch}
                {parentSearchLoading}
                onSearchConversation={handleConversationSearch}
                {conversationSearchLoading}
                onSubmit={handleCreateRequirementSubmit}
              />
          </div>
        {:else if viewMode === 'promote' && selectedRequirement}
          <div class="flex-1 min-h-0 overflow-y-auto p-4 space-y-4">
              <div class="rounded-lg border border-[var(--color-border-secondary)] bg-[var(--color-surface-100)] p-3 text-xs text-[var(--color-text-secondary)]">
                This spec will link back to requirement <span class="font-mono">{selectedRequirement.code}</span>.
              </div>
              <SpecForm
                mode="create"
                tenantId={getAuthState().user?.tenantId ?? ''}
                {projectId}
                loading={formLoading}
                initialData={{
                  code: `SPEC-${selectedRequirement.code}`,
                  title: selectedRequirement.title,
                  description: selectedRequirement.description,
                  decision: '',
                  context: '',
                  scope: '',
                  outOfScope: '',
                  definitions: '',
                  acceptanceCriteria: '',
                  owners: '',
                  reviewTrigger: '',
                  requirementId: selectedRequirement.id,
                  bornFromConversationId: selectedRequirement.conversationId ?? undefined,
                }}
                onSubmit={handlePromoteSpecSubmit}
              />
          </div>
        {:else if viewMode === 'edit' && selectedRequirement}
          <div class="flex-1 min-h-0 overflow-y-auto p-4">
              <RequirementForm
                mode="edit"
                loading={formLoading}
                {memberCandidates}
                {parentCandidates}
                onSearchParent={handleParentSearch}
                {parentSearchLoading}
                {conversationCandidates}
                onSearchConversation={handleConversationSearch}
                {conversationSearchLoading}
                initialData={{
                  code: selectedRequirement.code,
                  title: selectedRequirement.title,
                  description: selectedRequirement.description,
                  priority: selectedRequirement.priority,
                  parentId: selectedRequirement.parentId ?? undefined,
                  ownerUserId: selectedRequirement.owner?.id ?? undefined,
                  conversationId: selectedRequirement.conversationId ?? undefined,
                }}
                onSubmit={handleUpdateRequirement}
              />
          </div>
        {:else if detailLoading}
          <div class="flex-1 flex items-center justify-center">
            <Spinner size="lg" />
          </div>
        {:else if selectedRequirement}
          <div class="flex flex-col flex-1 min-h-0">
            <div class="flex-1 overflow-y-auto min-h-0">
              <RequirementDetailView
                requirement={selectedRequirement}
                showHeader={false}
                onEdit={handleEditRequirement}
                onPromoteToSpec={handlePromoteRequirement}
                onClose={handleCloseRequirement}
                onChildSelect={handleChildSelect}
                onParentSelect={handleParentSelect}
                onConversationClick={handleConversationClick}
              />
            </div>

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
                    <RequirementVersionDetail version={activeVersion} />
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
                <StatCard title="Total Requirements" value={requirements.length} />
                <StatCard title="Level A" value={levelCounts.A} valueColor="text-red-600" />
                <StatCard title="Level B" value={levelCounts.B} valueColor="text-yellow-600" />
                <StatCard title="Level C" value={levelCounts.C} valueColor="text-blue-600" />
              </CardGrid>

              <!-- Status Breakdown -->
              <SurfaceCard padding="lg">
                <h3 class="text-sm font-medium text-[var(--color-text-primary)] mb-3">Status Breakdown</h3>
                <div class="space-y-2">
                  {#each (['Draft', 'InReview', 'Approved', 'Deprecated'] as RequirementStatus[]) as status (status)}
                    {@const style = REQUIREMENT_STATUS_STYLES[status]}
                    {@const count = statusCounts[status]}
                    {@const percentage = requirements.length > 0 ? (count / requirements.length) * 100 : 0}
                    <div class="flex items-center gap-3">
                      <div class="w-20 text-xs {style.textColor}">{style.label}</div>
                      <div class="flex-1 h-2 bg-[var(--color-bg-tertiary)] rounded-full overflow-hidden">
                        <div
                          class="h-full {style.barColor}"
                          style="width: {percentage}%"
                        ></div>
                      </div>
                      <div class="w-8 text-xs text-right text-[var(--color-text-tertiary)]">{count}</div>
                    </div>
                  {/each}
                </div>
              </SurfaceCard>

              <div class="text-center py-6">
                <Icon name="mouse-pointer" size="lg" class="mx-auto mb-2 text-[var(--color-text-tertiary)] opacity-70" />
                <p class="text-sm font-medium text-[var(--color-text-primary)]">Select a requirement</p>
                <p class="text-xs text-[var(--color-text-secondary)] mt-1">
                  Choose a requirement from the sidebar to view details.
                </p>
              </div>
          </div>
        {/if}
      </SidebarDetailLayout>
    {/if}
  </PageBody>
</PageShell>
