<!-- Section: RequirementPanel — Requirements > requirement-{id} (menu-registry) -->
<script lang="ts">
  import { untrack } from 'svelte';
  import { Icon, IconButton, Button } from '@sddp/ui';
  import { RouterService, toast, getTabState, setTabState } from '@sddp/shell';
  import RequirementList from './RequirementList.svelte';
  import RequirementDetailView from './RequirementDetailView.svelte';
  import RequirementForm from '../idioms/RequirementForm.svelte';
  import {
    subscribeRequirement,
    setRequirementsPage,
    setRequirementsLoading,
    setCurrentRequirement,
    setCurrentRequirementLoading,
    setLevelFilter,
    setStatusFilter,
    clearCurrentRequirement,
    updateRequirementInList,
    addRequirement,
    setPage,
  } from '../../stores';
  import type {
    Requirement,
    RequirementState,
    RequirementLevel,
    RequirementStatus,
    CreateRequirementRequest,
    UpdateRequirementRequest,
    RequirementDetail,
  } from '../../types';
  import { getRequirementService } from '../../services';
  import type { ComboboxOption } from '@sddp/ui';
  import { getProjectById } from '../../../projects/services/ProjectService';
  import { getConversationService } from '../../../conversations/services/ConversationService';

  interface Props {
    tenantId: string;
    projectId: string;
    tabId?: string;
    class?: string;
  }

  let { tenantId, projectId, tabId = '', class: className = '' }: Props = $props();

  // State from store
  let storeState: RequirementState | null = $state(null);
  let memberCandidates = $state<ComboboxOption[]>([]);

  // Form states (inline instead of modal)
  let formMode = $state<'create' | 'edit' | null>(null);
  let formLoading = $state(false);
  let conversationCandidates = $state<ComboboxOption[]>([]);
  let conversationSearchLoading = $state(false);
  let conversationSearchTimer: ReturnType<typeof setTimeout> | undefined;
  let parentCandidates = $state<ComboboxOption[]>([]);
  let parentSearchLoading = $state(false);
  let parentSearchTimer: ReturnType<typeof setTimeout> | undefined;

  // Get service
  const requirementService = getRequirementService();

  // Tab state persistence
  interface RequirementPanelTabState {
    formMode: 'create' | 'edit' | null;
  }
  const tabStateKey = $derived(tabId || `requirement-panel-${projectId}`);
  let restoredKey = $state<string | null>(null);
  let isRestored = $state(false);

  $effect(() => {
    if (!tabStateKey || restoredKey === tabStateKey) return;
    const saved = getTabState<RequirementPanelTabState>(tabStateKey);
    if (saved) {
      formMode = saved.formMode ?? null;
    }
    restoredKey = tabStateKey;
    isRestored = true;
  });

  $effect(() => {
    if (!tabStateKey || !isRestored) return;
    setTabState<RequirementPanelTabState>(tabStateKey, { formMode });
  });

  // Track previous filter/page values to detect actual changes
  let prevLevelFilter: RequirementLevel | null = null;
  let prevStatusFilter: RequirementStatus | null = null;
  let prevPage: number = 1;

  // Subscribe to requirement state
  $effect(() => {
    requirementService.setContext(tenantId, projectId);

    const unsubscribe = subscribeRequirement((newState: RequirementState) => {
      storeState = newState;
    });

    // Load requirements on mount
    untrack(() => loadRequirements());

    // Load project members for owner selection
    untrack(() => getProjectById(tenantId, projectId)
      .then((proj) => {
        memberCandidates = (proj.members ?? []).map((m) => ({
          value: m.userId,
          label: m.displayName,
          description: m.role,
        }));
      })
      .catch(() => { /* member list is optional */ }));

    return () => {
      unsubscribe();
    };
  });

  // Reload when filters or page change (not when loading state changes)
  $effect(() => {
    if (storeState) {
      const { levelFilter, statusFilter, page, pageSize } = storeState;

      // Only reload if filters or page actually changed
      const filtersChanged =
        levelFilter !== prevLevelFilter ||
        statusFilter !== prevStatusFilter ||
        page !== prevPage;

      if (filtersChanged && storeState.requirements.length > 0) {
        prevLevelFilter = levelFilter;
        prevStatusFilter = statusFilter;
        prevPage = page;
        untrack(() => loadRequirements(page, pageSize, levelFilter, statusFilter));
      }
    }
  });

  async function loadRequirements(
    page: number = 1,
    pageSize: number = 20,
    level: RequirementLevel | null = null,
    status: RequirementStatus | null = null
  ): Promise<void> {
    setRequirementsLoading(true);
    try {
      const result = await requirementService.getRequirements({
        page,
        pageSize,
        level: level || undefined,
        status: status || undefined,
      });
      setRequirementsPage(result);
    } catch (error) {
      console.error('Failed to load requirements:', error);
      setRequirementsPage({
        items: [],
        totalCount: 0,
        page: 1,
        pageSize: 20,
        totalPages: 0,
      });
    }
  }

  async function selectRequirement(requirement: Requirement): Promise<void> {
    if (storeState?.currentRequirement?.id === requirement.id) return;

    setCurrentRequirementLoading(true);
    try {
      const detail = await requirementService.getRequirementById(requirement.id);
      setCurrentRequirement(detail);
    } catch (error) {
      console.error('Failed to load requirement detail:', error);
      // Fallback: create detail from the list requirement
      const fallbackDetail: RequirementDetail = {
        ...requirement,
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
        createdBy: { id: 'unknown', name: null, avatarUrl: null },
        updatedBy: { id: 'unknown', name: null, avatarUrl: null },
        validFrom: requirement.createdAt,
        validTo: null,
      };
      setCurrentRequirement(fallbackDetail);
    }
  }

  async function handleCreate(data: CreateRequirementRequest | UpdateRequirementRequest): Promise<void> {
    formLoading = true;
    try {
      const { conversationId: convId, ...createData } = data as CreateRequirementRequest;
      const detail = await requirementService.createRequirement(createData);

      // Link conversation if specified (separate API)
      if (convId) {
        await requirementService.linkConversation(detail.id, { conversationId: convId });
      }

      const fullDetail = convId
        ? await requirementService.getRequirementById(detail.id)
        : detail;

      // Add to list
      addRequirement({
        id: fullDetail.id,
        tenantId: fullDetail.tenantId,
        projectId: fullDetail.projectId,
        code: fullDetail.code,
        title: fullDetail.title,
        description: fullDetail.description,
        level: fullDetail.level,
        priority: fullDetail.priority ?? 'Medium',
        status: fullDetail.status,
        parentId: fullDetail.parentId,
        conversationId: fullDetail.conversationId,
        version: fullDetail.version,
        childrenCount: 0,
        createdAt: fullDetail.createdAt,
        updatedAt: fullDetail.updatedAt,
      });
      // Select it
      setCurrentRequirement(fullDetail);
      formMode = null;
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Failed to create requirement');
    } finally {
      formLoading = false;
    }
  }

  async function handleUpdate(data: CreateRequirementRequest | UpdateRequirementRequest): Promise<void> {
    if (!storeState?.currentRequirement) return;

    formLoading = true;
    try {
      const { conversationId: newConvId, ...updateData } = data as UpdateRequirementRequest;
      await requirementService.updateRequirement(
        storeState.currentRequirement.id,
        updateData
      );

      // Handle conversation link/unlink
      const oldConvId = storeState.currentRequirement.conversationId;
      if (newConvId && newConvId !== oldConvId) {
        await requirementService.linkConversation(storeState.currentRequirement.id, { conversationId: newConvId });
      } else if (!newConvId && oldConvId) {
        await requirementService.unlinkConversation(storeState.currentRequirement.id);
      }

      // Reload detail to get full updated data
      const detail = await requirementService.getRequirementById(storeState.currentRequirement.id);
      // Update in list
      updateRequirementInList(detail.id, {
        title: detail.title,
        description: detail.description,
        updatedAt: detail.updatedAt,
      });
      // Update current
      setCurrentRequirement(detail);
      formMode = null;
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Failed to update requirement');
    } finally {
      formLoading = false;
    }
  }

  async function handleStatusChange(newStatus: RequirementStatus): Promise<void> {
    if (!storeState?.currentRequirement) return;

    try {
      const updated = await requirementService.transitionStatus(
        storeState.currentRequirement.id,
        { newStatus }
      );
      // Update in list
      updateRequirementInList(updated.id, { status: updated.status });
      // Reload detail
      const detail = await requirementService.getRequirementById(updated.id);
      setCurrentRequirement(detail);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Failed to change status');
    }
  }

  function enterEditMode(): void {
    formMode = 'edit';
    const current = storeState?.currentRequirement;
    // Pre-populate conversation option so Combobox shows current name
    if (current?.conversationId && current?.conversationName) {
      const prefix = current.conversationType === 'Channel' ? '#' : '';
      conversationCandidates = [{
        value: current.conversationId,
        label: `${prefix}${current.conversationName}`,
        description: current.conversationDescription ?? current.conversationType ?? undefined,
      }];
    } else {
      conversationCandidates = [];
    }
    // Pre-populate parent option so Combobox shows current parent
    if (current?.parentId && (current?.parentTitle || current?.parentCode)) {
      parentCandidates = [{
        value: current.parentId,
        label: `${current.parentCode ?? ''}  ${current.parentTitle ?? ''}`.trim(),
        description: current.parentLevel ? `Level ${current.parentLevel}` : undefined,
      }];
    } else {
      parentCandidates = [];
    }
  }

  function handleParentSearch(query: string): void {
    if (parentSearchTimer) clearTimeout(parentSearchTimer);
    if (!query.trim()) {
      parentCandidates = [];
      return;
    }
    parentSearchTimer = setTimeout(async () => {
      parentSearchLoading = true;
      try {
        const results = await requirementService.searchRequirements(query, 15);
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

  function handleConversationSearch(query: string): void {
    if (conversationSearchTimer) clearTimeout(conversationSearchTimer);
    if (!query.trim()) {
      conversationCandidates = [];
      return;
    }
    conversationSearchTimer = setTimeout(async () => {
      conversationSearchLoading = true;
      try {
        const svc = getConversationService();
        svc.setContext(tenantId, projectId);
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

  function handleLevelFilterChange(level: RequirementLevel | null): void {
    setLevelFilter(level);
  }

  function handleStatusFilterChange(status: RequirementStatus | null): void {
    setStatusFilter(status);
  }

  async function handleParentSelect(parentId: string): Promise<void> {
    setCurrentRequirementLoading(true);
    try {
      const detail = await requirementService.getRequirementById(parentId);
      setCurrentRequirement(detail);
    } catch (error) {
      console.error('Failed to load parent requirement:', error);
    }
  }

  function handleConversationClick(conversationId: string): void {
    RouterService.navigate(`/conversation/${conversationId}`);
  }
</script>

<div class="flex h-full bg-[var(--color-surface-100)] {className}">
  <!-- Requirement List (Left sidebar) -->
  <div class="w-80 flex-shrink-0 border-r border-[var(--color-border-primary)] bg-[var(--color-surface-50)]">
    <RequirementList
      requirements={storeState?.requirements || []}
      selectedId={storeState?.currentRequirement?.id}
      loading={storeState?.requirementsLoading}
      levelFilter={storeState?.levelFilter}
      statusFilter={storeState?.statusFilter}
      onSelect={selectRequirement}
      onCreate={() => (formMode = 'create')}
      onLevelFilterChange={handleLevelFilterChange}
      onStatusFilterChange={handleStatusFilterChange}
    />

    <!-- Pagination -->
    {#if storeState && storeState.totalPages > 1}
      <div class="flex items-center justify-between px-4 py-2 border-t border-[var(--color-border-secondary)]">
        <span class="text-xs text-[var(--color-text-muted)]">
          Page {storeState.page} of {storeState.totalPages}
        </span>
        <div class="flex gap-1">
          <Button
            variant="ghost"
            size="sm"
            disabled={storeState.page <= 1}
            onclick={() => setPage(storeState!.page - 1)}
          >
            <Icon name="chevron-left" size="sm" />
          </Button>
          <Button
            variant="ghost"
            size="sm"
            disabled={storeState.page >= storeState.totalPages}
            onclick={() => setPage(storeState!.page + 1)}
          >
            <Icon name="chevron-right" size="sm" />
          </Button>
        </div>
      </div>
    {/if}
  </div>

  <!-- Main content area -->
  <div class="flex-1 flex flex-col min-w-0">
    {#if formMode === 'create'}
      <!-- Inline Create Form -->
      <div class="flex-1 flex flex-col min-w-0">
        <div class="flex items-center justify-between px-4 py-2 border-b border-[var(--color-border-primary)] bg-[var(--color-bg-section)]">
          <div class="flex items-center gap-2">
            <Icon name="clipboard-list" size="sm" class="text-[var(--color-text-tertiary)]" />
            <span class="text-sm font-medium text-[var(--color-text-primary)]">New Requirement</span>
          </div>
          <div class="flex items-center gap-1">
            <IconButton icon="check" variant="success" size="sm" title="Create" onclick={() => (document.getElementById('requirement-form') as HTMLFormElement | null)?.requestSubmit()} disabled={formLoading} />
            <IconButton icon="x" variant="ghost" size="sm" title="Cancel" onclick={() => (formMode = null)} />
          </div>
        </div>
        <div class="flex-1 overflow-y-auto p-4">
          <RequirementForm
            mode="create"
            loading={formLoading}
            {memberCandidates}
            {conversationCandidates}
            onSearchConversation={handleConversationSearch}
            {conversationSearchLoading}
            onSubmit={handleCreate}
          />
        </div>
      </div>
    {:else if formMode === 'edit' && storeState?.currentRequirement}
      <!-- Inline Edit Form -->
      <div class="flex-1 flex flex-col min-w-0">
        <div class="flex items-center justify-between px-4 py-2 border-b border-[var(--color-border-primary)] bg-[var(--color-bg-section)]">
          <div class="flex items-center gap-2">
            <Icon name="clipboard-list" size="sm" class="text-[var(--color-text-tertiary)]" />
            <span class="text-sm font-medium text-[var(--color-text-primary)]">Edit Requirement</span>
          </div>
          <div class="flex items-center gap-1">
            <IconButton icon="check" variant="success" size="sm" title="Save" onclick={() => (document.getElementById('requirement-form') as HTMLFormElement | null)?.requestSubmit()} disabled={formLoading} />
            <IconButton icon="x" variant="ghost" size="sm" title="Cancel" onclick={() => (formMode = null)} />
          </div>
        </div>
        <div class="flex-1 overflow-y-auto p-4">
          <RequirementForm
            mode="edit"
            initialData={{
              code: storeState.currentRequirement.code,
              title: storeState.currentRequirement.title,
              description: storeState.currentRequirement.description,
              priority: storeState.currentRequirement.priority,
              parentId: storeState.currentRequirement.parentId ?? undefined,
              ownerUserId: storeState.currentRequirement.owner?.id ?? undefined,
              conversationId: storeState.currentRequirement.conversationId ?? undefined,
            }}
            {memberCandidates}
            {parentCandidates}
            onSearchParent={handleParentSearch}
            {parentSearchLoading}
            {conversationCandidates}
            onSearchConversation={handleConversationSearch}
            {conversationSearchLoading}
            loading={formLoading}
            onSubmit={handleUpdate}
          />
        </div>
      </div>
    {:else if storeState?.currentRequirement}
      <RequirementDetailView
        requirement={storeState.currentRequirement}
        loading={storeState.currentRequirementLoading}
        onEdit={enterEditMode}
        onStatusChange={handleStatusChange}
        onChildSelect={selectRequirement}
        onParentSelect={handleParentSelect}
        onConversationClick={handleConversationClick}
        onClose={clearCurrentRequirement}
      />
    {:else}
      <!-- Empty state -->
      <div class="flex-1 flex items-center justify-center">
        <div class="text-center">
          <Icon name="file-text" size="xl" class="mx-auto mb-4 text-[var(--color-text-muted)] opacity-50" />
          <p class="text-sm font-medium text-[var(--color-text-primary)]">Select a requirement</p>
          <p class="text-xs text-[var(--color-text-secondary)] mt-1">
            Choose a requirement from the sidebar to view details.
          </p>
          <Button variant="primary" class="mt-4" onclick={() => (formMode = 'create')}>
            <Icon name="plus" size="sm" class="mr-2" />
            New Requirement
          </Button>
        </div>
      </div>
    {/if}
  </div>
</div>

