<!-- Section: SpecPanel — Specs > spec-{id} (menu-registry) -->
<script lang="ts">
  import { untrack } from 'svelte';
  import { Icon, IconButton, Button } from '@sddp/ui';
  import type { ComboboxOption } from '@sddp/ui';
  import { tabActions, RouterService, toast, getTabState, setTabState } from '@sddp/shell';
  import SpecList from './SpecList.svelte';
  import SpecDetailView from './SpecDetailView.svelte';
  import SpecVersionHistory from '../idioms/SpecVersionHistory.svelte';
  import SpecForm from '../idioms/SpecForm.svelte';
  import RequirementPanel from '../../../requirements/components/sections/RequirementPanel.svelte';
  import {
    subscribeSpec,
    setSpecsPage,
    setSpecsLoading,
    setSpecsError,
    setCurrentSpec,
    setCurrentSpecLoading,
    setCurrentSpecError,
    setVersionHistory,
    setVersionsLoading,
    setVersionsError,
    setStatusFilter,
    clearCurrentSpec,
    updateSpecInList,
    addSpec,
    removeSpec,
    setPage,
  } from '../../stores';
  import type {
    Spec,
    SpecState,
    SpecStatus,
    CreateSpecRequest,
    UpdateSpecRequest,
  } from '../../types';
  import { getSpecService } from '../../services';
  import { getRequirementService } from '../../../requirements/services/RequirementService';
  import { getConversationService } from '../../../conversations/services/ConversationService';

  interface Props {
    tenantId: string;
    projectId: string;
    specId?: string;
    tabId?: string;
    class?: string;
  }

  let { tenantId, projectId, specId, tabId = '', class: className = '' }: Props = $props();

  // State from store
  let storeState: SpecState | null = $state(null);

  // Form states (inline instead of modal)
  let formMode = $state<'create' | 'edit' | null>(null);
  let formLoading = $state(false);
  // Get service
  const specService = getSpecService();

  function ensureSpecContext(): void {
    specService.setContext(tenantId, projectId);
  }

  // Reference search state (Combobox)
  let requirementCandidates = $state<ComboboxOption[]>([]);
  let requirementSearchLoading = $state(false);
  let requirementSearchTimer: ReturnType<typeof setTimeout> | undefined;
  let conversationCandidates = $state<ComboboxOption[]>([]);
  let conversationSearchLoading = $state(false);
  let conversationSearchTimer: ReturnType<typeof setTimeout> | undefined;

  // Tab state persistence
  interface SpecPanelTabState {
    formMode: 'create' | 'edit' | null;
  }
  const tabStateKey = $derived(tabId || `spec-panel-${projectId}`);
  let restoredKey = $state<string | null>(null);
  let isRestored = $state(false);

  $effect(() => {
    if (!tabStateKey || restoredKey === tabStateKey) return;
    const saved = getTabState<SpecPanelTabState>(tabStateKey);
    if (saved) {
      formMode = saved.formMode ?? null;
    }
    restoredKey = tabStateKey;
    isRestored = true;
  });

  $effect(() => {
    if (!tabStateKey || !isRestored) return;
    setTabState<SpecPanelTabState>(tabStateKey, { formMode });
  });

  // Track previous filter/page values to detect actual changes
  let prevStatusFilter: SpecStatus | null = null;
  let prevPage: number = 1;
  let lastSpecId: string | null = null;

  // Subscribe to spec state
  $effect(() => {
    ensureSpecContext();

    const unsubscribe = subscribeSpec((newState: SpecState) => {
      storeState = newState;
    });

    // Load specs on mount
    untrack(() => loadSpecs());

    return () => {
      unsubscribe();
    };
  });

  // Load specific spec if provided (e.g., deep link/tab open)
  $effect(() => {
    if (!specId || specId === lastSpecId) return;
    lastSpecId = specId;
    ensureSpecContext();
    untrack(() => loadSpecById(specId));
  });

  // Reload when filters or page change (not when loading state changes)
  $effect(() => {
    if (storeState) {
      const { statusFilter, page, pageSize } = storeState;

      // Only reload if filters or page actually changed
      const filtersChanged =
        statusFilter !== prevStatusFilter ||
        page !== prevPage;

      if (filtersChanged && storeState.specs.length > 0) {
        prevStatusFilter = statusFilter;
        prevPage = page;
        untrack(() => loadSpecs(page, pageSize, statusFilter));
      }
    }
  });

  async function loadSpecs(
    page: number = 1,
    pageSize: number = 20,
    status: SpecStatus | null = null
  ): Promise<void> {
    setSpecsLoading(true);
    try {
      const result = await specService.getSpecs({
        page,
        pageSize,
        status: status || undefined,
      });
      setSpecsPage(result);
    } catch (error) {
      const message = error instanceof Error ? error.message : 'Failed to load specs';
      setSpecsError(message);
    }
  }

  async function selectSpec(spec: Spec): Promise<void> {
    if (storeState?.currentSpec?.id === spec.id) return;

    setCurrentSpecLoading(true);
    try {
      const detail = await specService.getSpecById(spec.id);
      setCurrentSpec(detail);

      // Load version history
      await loadVersionHistory(spec.id);
    } catch (error) {
      const message = error instanceof Error ? error.message : 'Failed to load spec';
      setCurrentSpecError(message);
    }
  }

  async function loadSpecById(id: string): Promise<void> {
    if (storeState?.currentSpec?.id === id) return;

    setCurrentSpecLoading(true);
    try {
      const detail = await specService.getSpecById(id);
      setCurrentSpec(detail);
      await loadVersionHistory(id);
    } catch (error) {
      const message = error instanceof Error ? error.message : 'Failed to load spec';
      setCurrentSpecError(message);
    }
  }

  async function loadVersionHistory(specId: string): Promise<void> {
    setVersionsLoading(true);
    try {
      const versions = await specService.getVersionHistory(specId);
      setVersionHistory(versions);
    } catch (error) {
      const message = error instanceof Error ? error.message : 'Failed to load version history';
      setVersionsError(message);
    }
  }

  async function handleCreate(data: CreateSpecRequest | UpdateSpecRequest): Promise<void> {
    formLoading = true;
    try {
      const detail = await specService.createSpec(data as CreateSpecRequest);
      // Add to list
      addSpec({
        id: detail.id,
        tenantId: detail.tenantId,
        projectId: detail.projectId,
        code: detail.code,
        title: detail.title,
        description: detail.description,
        decision: detail.decision,
        status: detail.status,
        requirementId: detail.requirementId,
        bornFromConversationId: detail.bornFromConversationId,
        supersedesSpecId: detail.supersedesSpecId,
        version: detail.version,
        lockedAt: detail.lockedAt,
        createdAt: detail.createdAt,
        updatedAt: detail.updatedAt,
      });
      // Select it
      setCurrentSpec(detail);
      formMode = null;
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Failed to create spec');
    } finally {
      formLoading = false;
    }
  }

  async function handleUpdate(data: CreateSpecRequest | UpdateSpecRequest): Promise<void> {
    if (!storeState?.currentSpec) return;

    formLoading = true;
    try {
      const detail = await specService.updateSpec(
        storeState.currentSpec.id,
        data as UpdateSpecRequest
      );
      // Update in list
      updateSpecInList(detail.id, {
        title: detail.title,
        description: detail.description,
        decision: detail.decision,
        updatedAt: detail.updatedAt,
      });
      // Update current
      setCurrentSpec(detail);
      formMode = null;
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Failed to update spec');
    } finally {
      formLoading = false;
    }
  }

  async function handleSubmitForReview(): Promise<void> {
    if (!storeState?.currentSpec) return;

    try {
      const updated = await specService.submitForReview(storeState.currentSpec.id);
      updateSpecInList(updated.id, { status: updated.status });
      const detail = await specService.getSpecById(updated.id);
      setCurrentSpec(detail);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Failed to submit for review');
    }
  }

  async function handleApprove(): Promise<void> {
    if (!storeState?.currentSpec) return;

    try {
      const updated = await specService.approveSpec(storeState.currentSpec.id);
      updateSpecInList(updated.id, { status: updated.status });
      const detail = await specService.getSpecById(updated.id);
      setCurrentSpec(detail);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Failed to approve spec');
    }
  }

  async function handleReject(reason?: string): Promise<void> {
    if (!storeState?.currentSpec) return;

    try {
      const updated = await specService.rejectSpec(storeState.currentSpec.id, { reason });
      updateSpecInList(updated.id, { status: updated.status });
      const detail = await specService.getSpecById(updated.id);
      setCurrentSpec(detail);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Failed to reject spec');
    }
  }

  async function handleLock(): Promise<void> {
    if (!storeState?.currentSpec) return;

    try {
      const updated = await specService.lockSpec(storeState.currentSpec.id);
      updateSpecInList(updated.id, { status: updated.status, lockedAt: updated.lockedAt });
      const detail = await specService.getSpecById(updated.id);
      setCurrentSpec(detail);
      // Reload version history
      await loadVersionHistory(detail.id);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Failed to lock spec');
    }
  }

  async function handleNewVersion(): Promise<void> {
    if (!storeState?.currentSpec) return;

    try {
      const newDetail = await specService.createNewVersion(storeState.currentSpec.id);
      // Add new version to list
      addSpec({
        id: newDetail.id,
        tenantId: newDetail.tenantId,
        projectId: newDetail.projectId,
        code: newDetail.code,
        title: newDetail.title,
        description: newDetail.description,
        decision: newDetail.decision,
        status: newDetail.status,
        requirementId: newDetail.requirementId,
        bornFromConversationId: newDetail.bornFromConversationId,
        supersedesSpecId: newDetail.supersedesSpecId,
        version: newDetail.version,
        lockedAt: newDetail.lockedAt,
        createdAt: newDetail.createdAt,
        updatedAt: newDetail.updatedAt,
      });
      // Select new version
      setCurrentSpec(newDetail);
      // Reload version history
      await loadVersionHistory(newDetail.id);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Failed to create new version');
    }
  }

  async function handleDeactivateSpec(): Promise<void> {
    if (!storeState?.currentSpec) return;
    if (!confirm('Are you sure you want to deactivate this spec?')) return;

    try {
      await specService.deleteSpec(storeState.currentSpec.id);
      removeSpec(storeState.currentSpec.id);
      clearCurrentSpec();
      toast.success('Spec deactivated');
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Failed to deactivate spec');
    }
  }

  function handleStatusFilterChange(status: SpecStatus | null): void {
    setStatusFilter(status);
  }

  async function handleVersionSelect(spec: Spec): Promise<void> {
    await selectSpec(spec);
  }

  function handleRequirementClick(requirementId: string): void {
    tabActions.createTab({
      title: `Requirement`,
      icon: 'clipboard-list',
      dirty: false,
      closable: true,
      component: RequirementPanel,
      props: {
        tenantId,
        projectId,
        requirementId,
      },
      path: `/requirement-${requirementId}`,
    });
  }

  function handleConversationClick(conversationId: string): void {
    RouterService.navigate(`/conversation/${conversationId}`);
  }

  function enterEditMode(): void {
    formMode = 'edit';
    const current = storeState?.currentSpec;

    // Pre-populate requirement option
    if (current?.requirementId && (current?.requirementTitle || current?.requirementCode)) {
      requirementCandidates = [{
        value: current.requirementId,
        label: `${current.requirementCode ?? ''}  ${current.requirementTitle ?? ''}`.trim(),
      }];
    } else {
      requirementCandidates = [];
    }

    // Pre-populate conversation option
    if (current?.bornFromConversationId && current?.bornFromConversationName) {
      const prefix = current.bornFromConversationType === 'Channel' ? '#' : '';
      conversationCandidates = [{
        value: current.bornFromConversationId,
        label: `${prefix}${current.bornFromConversationName}`,
        description: current.bornFromConversationDescription ?? current.bornFromConversationType ?? undefined,
      }];
    } else {
      conversationCandidates = [];
    }
  }

  function enterCreateMode(): void {
    formMode = 'create';
    requirementCandidates = [];
    conversationCandidates = [];
  }

  function handleRequirementSearch(query: string): void {
    if (requirementSearchTimer) clearTimeout(requirementSearchTimer);
    if (!query.trim()) {
      requirementCandidates = [];
      return;
    }
    requirementSearchTimer = setTimeout(async () => {
      requirementSearchLoading = true;
      try {
        const svc = getRequirementService();
        svc.setContext(tenantId, projectId);
        const results = await svc.searchRequirements(query, 15);
        requirementCandidates = results.map((r) => ({
          value: r.id,
          label: `${r.code}  ${r.title}`,
          description: `Level ${r.level}`,
        }));
      } catch {
        requirementCandidates = [];
      } finally {
        requirementSearchLoading = false;
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
</script>

<div class="flex h-full bg-[var(--color-surface-100)] {className}">
  <!-- Spec List (Left sidebar) -->
  <div class="w-80 flex-shrink-0 border-r border-[var(--color-border-primary)] bg-[var(--color-surface-50)] flex flex-col">
    <SpecList
      specs={storeState?.specs || []}
      selectedId={storeState?.currentSpec?.id}
      loading={storeState?.specsLoading}
      statusFilter={storeState?.statusFilter}
      onSelect={selectSpec}
      onCreate={enterCreateMode}
      onStatusFilterChange={handleStatusFilterChange}
      class="flex-1"
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
      <div class="flex items-center justify-between px-4 py-2 border-b border-[var(--color-border-primary)] bg-[var(--color-bg-section)]">
        <div class="flex items-center gap-2">
          <Icon name="file-text" size="sm" class="text-[var(--color-text-tertiary)]" />
          <span class="text-sm font-medium text-[var(--color-text-primary)]">New Spec</span>
        </div>
        <div class="flex items-center gap-1">
          <IconButton icon="check" variant="success" size="sm" title="Create" onclick={() => (document.getElementById('spec-form') as HTMLFormElement | null)?.requestSubmit()} disabled={formLoading} />
          <IconButton icon="x" variant="ghost" size="sm" title="Cancel" onclick={() => (formMode = null)} />
        </div>
      </div>
      <div class="flex-1 overflow-y-auto p-4">
        <SpecForm
          mode="create"
          {tenantId}
          {projectId}
          loading={formLoading}
          {requirementCandidates}
          {conversationCandidates}
          onSearchRequirement={handleRequirementSearch}
          {requirementSearchLoading}
          onSearchConversation={handleConversationSearch}
          {conversationSearchLoading}
          onSubmit={handleCreate}
        />
      </div>
    {:else if formMode === 'edit' && storeState?.currentSpec}
      <!-- Inline Edit Form -->
      <div class="flex items-center justify-between px-4 py-2 border-b border-[var(--color-border-primary)] bg-[var(--color-bg-section)]">
        <div class="flex items-center gap-2">
          <Icon name="file-text" size="sm" class="text-[var(--color-text-tertiary)]" />
          <span class="text-sm font-medium text-[var(--color-text-primary)]">Edit Spec</span>
        </div>
        <div class="flex items-center gap-1">
          <IconButton icon="check" variant="success" size="sm" title="Save" onclick={() => (document.getElementById('spec-form') as HTMLFormElement | null)?.requestSubmit()} disabled={formLoading} />
          <IconButton icon="x" variant="ghost" size="sm" title="Cancel" onclick={() => (formMode = null)} />
        </div>
      </div>
      <div class="flex-1 overflow-y-auto p-4">
        <SpecForm
          mode="edit"
          {tenantId}
          {projectId}
          initialData={{
            code: storeState.currentSpec.code,
            title: storeState.currentSpec.title,
            description: storeState.currentSpec.description,
            decision: storeState.currentSpec.decision,
            context: storeState.currentSpec.context,
            scope: storeState.currentSpec.scope,
            outOfScope: storeState.currentSpec.outOfScope,
            definitions: storeState.currentSpec.definitions,
            acceptanceCriteria: storeState.currentSpec.acceptanceCriteria,
            owners: storeState.currentSpec.owners,
            reviewTrigger: storeState.currentSpec.reviewTrigger,
            requirementId: storeState.currentSpec.requirementId ?? undefined,
            bornFromConversationId: storeState.currentSpec.bornFromConversationId ?? undefined,
          }}
          loading={formLoading}
          {requirementCandidates}
          {conversationCandidates}
          onSearchRequirement={handleRequirementSearch}
          {requirementSearchLoading}
          onSearchConversation={handleConversationSearch}
          {conversationSearchLoading}
          onSubmit={handleUpdate}
        />
      </div>
    {:else if storeState?.currentSpec}
      <!-- Detail View + Version History (side by side) -->
      <div class="flex-1 flex min-w-0 overflow-hidden">
        <div class="flex-1 flex flex-col min-w-0">
          <SpecDetailView
            spec={storeState.currentSpec}
            loading={storeState.currentSpecLoading}
            onEdit={enterEditMode}
            onSubmitForReview={handleSubmitForReview}
            onApprove={handleApprove}
            onReject={handleReject}
            onLock={handleLock}
            onNewVersion={handleNewVersion}
            onRequirementClick={handleRequirementClick}
            onConversationClick={handleConversationClick}
            onDeactivate={handleDeactivateSpec}
            onClose={clearCurrentSpec}
          />
        </div>

        <!-- Version History (Right sidebar) -->
        <div class="w-64 flex-shrink-0 border-l border-[var(--color-border-primary)] bg-[var(--color-surface-50)]">
          <SpecVersionHistory
            versions={storeState.specVersions}
            currentId={storeState.currentSpec?.id}
            loading={storeState.versionsLoading}
            onSelect={handleVersionSelect}
          />
        </div>
      </div>
    {:else}
      <!-- Empty state -->
      <div class="flex-1 flex items-center justify-center">
        <div class="text-center">
          <Icon name="file-code" size="xl" class="mx-auto mb-4 text-[var(--color-text-muted)] opacity-50" />
          <p class="text-sm font-medium text-[var(--color-text-primary)]">Select a spec</p>
          <p class="text-xs text-[var(--color-text-secondary)] mt-1">
            Choose a spec from the sidebar to view details.
          </p>
          <Button variant="primary" class="mt-4" onclick={enterCreateMode}>
            <Icon name="plus" size="sm" class="mr-2" />
            New Spec
          </Button>
        </div>
      </div>
    {/if}
  </div>
</div>

