<!-- Activity: Settings > Nav: System Audit Logs (settings-system-audit) -->
<script lang="ts">
  /**
   * System Audit Logs Page
   * SidebarDetailLayout — Left sidebar: search + filters + log list, Right: detail panel
   * Supports infinite scroll pagination
   */

  import { untrack } from 'svelte';
  import { AuditLogDetailPanel } from '../sections';
  import { formatDateTime, toast, getAuthState, subscribeAuth, getTabState, setTabState, PageHeader, PageShell, SidebarDetailLayout, Dropdown, ListItem } from '@sddp/shell';
  import { Icon, Input, IconButton, Button, Spinner } from '@sddp/ui';
  import type { AuditLog } from '../../types';
  import { getAuditLogService } from '../../services';
  import { getAuditActionColor } from '../../../shared/constants/semanticColors';

  interface Props {
    tenantId?: string;
    tabId?: string;
  }

  let { tenantId = '', tabId = '' }: Props = $props();

  // If tenantId is not passed as prop, get from auth store
  let effectiveTenantId = $state('');

  $effect(() => {
    if (!tenantId) {
      effectiveTenantId = getAuthState().user?.tenantId || '';
      const unsubscribe = subscribeAuth((state) => {
        effectiveTenantId = state.user?.tenantId || '';
      });
      return unsubscribe;
    } else {
      effectiveTenantId = tenantId;
    }
  });

  const auditLogService = getAuditLogService();

  const PAGE_SIZE = 50;

  let logs = $state<AuditLog[]>([]);
  let loading = $state(false);
  let loadingMore = $state(false);
  let error = $state<string | null>(null);
  let searchQuery = $state('');
  let filterAction = $state<string>('all');
  let filterResource = $state<string>('all');
  let currentPage = $state(1);
  let totalCount = $state(0);

  const hasMore = $derived(logs.length < totalCount);

  // Panel state
  let selectedLogId = $state<string | null>(null);
  let selectedLog = $state<AuditLog | null>(null);

  const showPanel = $derived(selectedLog !== null);

  const actionFilterOptions: { value: string; label: string }[] = [
    { value: 'all', label: 'All Actions' },
    { value: 'create', label: 'Create' },
    { value: 'update', label: 'Update' },
    { value: 'delete', label: 'Delete' },
    { value: 'login', label: 'Login' },
    { value: 'logout', label: 'Logout' },
    { value: 'refresh_token', label: 'Refresh Token' },
    { value: 'activate', label: 'Activate' },
    { value: 'deactivate', label: 'Deactivate' },
    { value: 'approve', label: 'Approve' },
    { value: 'reject', label: 'Reject' },
    { value: 'set_value', label: 'Set Value' },
    { value: 'set_group', label: 'Set Group' },
    { value: 'save', label: 'Save' },
    { value: 'delete_value', label: 'Delete Value' },
    { value: 'reset', label: 'Reset' },
  ];

  const resourceFilterOptions: { value: string; label: string }[] = [
    { value: 'all', label: 'All Resources' },
    { value: 'auth', label: 'Auth' },
    { value: 'user', label: 'User' },
    { value: 'project', label: 'Project' },
    { value: 'spec', label: 'Spec' },
    { value: 'requirement', label: 'Requirement' },
    { value: 'conversation', label: 'Conversation' },
    { value: 'glossary', label: 'Glossary' },
    { value: 'system_config', label: 'System Config' },
  ];

  const filteredLogs = $derived(
    logs.filter((log) => {
      const query = searchQuery.toLowerCase();
      const matchesSearch =
        log.userName.toLowerCase().includes(query) ||
        log.action.toLowerCase().includes(query) ||
        log.resourceType.toLowerCase().includes(query);
      const matchesAction = filterAction === 'all' || log.action.toLowerCase() === filterAction;
      const matchesResource = filterResource === 'all' || log.resourceType.toLowerCase() === filterResource;
      return matchesSearch && matchesAction && matchesResource;
    })
  );

  // Tab State Persistence
  interface AuditLogsTabState {
    searchQuery: string;
    filterAction: string;
    filterResource: string;
    selectedLogId: string | null;
  }

  const tabStateKey = $derived(tabId || 'settings-system-audit');
  let restoredKey = $state<string | null>(null);
  let isRestored = $state(false);

  $effect(() => {
    if (!tabStateKey || restoredKey === tabStateKey) return;
    const saved = getTabState<AuditLogsTabState>(tabStateKey);
    if (saved) {
      searchQuery = saved.searchQuery ?? '';
      filterAction = saved.filterAction ?? 'all';
      filterResource = saved.filterResource ?? 'all';
      selectedLogId = saved.selectedLogId ?? null;
    }
    restoredKey = tabStateKey;
    isRestored = true;
  });

  $effect(() => {
    if (!tabStateKey || !isRestored) return;
    setTabState<AuditLogsTabState>(tabStateKey, {
      searchQuery,
      filterAction,
      filterResource,
      selectedLogId,
    });
  });

  // Load logs when tenantId changes (prevLoadKey guard)
  let prevLoadKey = $state<string | null>(null);
  $effect(() => {
    if (!effectiveTenantId) return;
    if (effectiveTenantId === prevLoadKey) return;
    prevLoadKey = effectiveTenantId;
    untrack(() => loadLogs());
  });

  // Sync selectedLog when selectedLogId changes
  $effect(() => {
    if (!selectedLogId) {
      selectedLog = null;
      return;
    }
    selectedLog = logs.find((l) => l.id === selectedLogId) || null;
  });

  /** Load first page (resets list) */
  async function loadLogs() {
    if (!effectiveTenantId) {
      error = 'Tenant ID is required';
      return;
    }

    loading = true;
    error = null;
    currentPage = 1;

    try {
      auditLogService.setContext(effectiveTenantId);
      const result = await auditLogService.getAuditLogs({
        action: filterAction !== 'all' ? filterAction : undefined,
        resourceType: filterResource !== 'all' ? filterResource : undefined,
        page: 1,
        size: PAGE_SIZE,
      });
      logs = result.items;
      totalCount = result.totalCount;
    } catch (err) {
      error = err instanceof Error ? err.message : 'Failed to load audit logs';
      toast.error('Failed to load audit logs');
      logs = [];
      totalCount = 0;
    } finally {
      loading = false;
    }
  }

  /** Load next page (appends to list) */
  async function loadMore() {
    if (loadingMore || !hasMore) return;

    loadingMore = true;
    const nextPage = currentPage + 1;

    try {
      auditLogService.setContext(effectiveTenantId);
      const result = await auditLogService.getAuditLogs({
        action: filterAction !== 'all' ? filterAction : undefined,
        resourceType: filterResource !== 'all' ? filterResource : undefined,
        page: nextPage,
        size: PAGE_SIZE,
      });
      logs = [...logs, ...result.items];
      totalCount = result.totalCount;
      currentPage = nextPage;
    } catch {
      toast.error('Failed to load more logs');
    } finally {
      loadingMore = false;
    }
  }

  /** Scroll handler for infinite scroll */
  function handleListScroll(e: Event) {
    const target = e.target as HTMLElement;
    const nearBottom = target.scrollHeight - target.scrollTop - target.clientHeight < 100;
    if (nearBottom && hasMore && !loadingMore) {
      loadMore();
    }
  }

  function selectLog(log: AuditLog) {
    selectedLogId = log.id;
  }

  function closePanel() {
    selectedLogId = null;
    selectedLog = null;
  }

  function formatDateStr(dateStr: string): string {
    return formatDateTime(dateStr, { month: 'short', year: undefined });
  }

  function getActionColor(action: string): string {
    return getAuditActionColor(action);
  }

  function handleSearchInput(e: Event) {
    const target = e.target as HTMLInputElement;
    searchQuery = target.value;
  }
</script>

<PageShell>
  {#if loading}
    <div class="flex-1 flex items-center justify-center pb-28"><Spinner size="lg" /></div>
  {:else if error}
    <div class="flex-1 flex items-center justify-center pb-28">
      <div class="p-12 text-center text-sm text-[var(--color-error-600)]">
        <p class="mb-4">{error}</p>
        <Button variant="secondary" onclick={loadLogs}>Retry</Button>
      </div>
    </div>
  {:else}
  <PageHeader title="Audit Logs" {loading}>
    {#snippet actions()}
      <IconButton icon="refresh-cw" title="Refresh" onclick={loadLogs} />
    {/snippet}
  </PageHeader>
    <SidebarDetailLayout
      showRightPanel={false}
      sidebarWidth={420}
      minSidebarWidth={320}
      maxSidebarWidth={600}
    >
      {#snippet sidebar()}
        <div class="flex flex-col h-full min-h-0 bg-[var(--color-bg-primary)]">
          <!-- Sidebar Header: Search + Filters -->
          <div class="flex-shrink-0 flex items-center p-2 min-h-12 border-b border-[var(--color-border-primary)]">
            <div class="flex items-center gap-1 w-full">
              <div class="relative flex-1">
                <Icon
                  name="search"
                  size="sm"
                  class="absolute left-2.5 top-1/2 -translate-y-1/2 text-[var(--color-text-tertiary)]"
                />
                <Input
                  type="text"
                  placeholder="Search logs..."
                  value={searchQuery}
                  oninput={handleSearchInput}
                  autocomplete="off"
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
                  <!-- Action Filter -->
                  <div class="px-3 py-1 text-xs font-semibold text-[var(--color-text-tertiary)] uppercase">
                    Action
                  </div>
                  {#each actionFilterOptions as option (option.value)}
                    <Button
                      variant="unstyled"
                      class="w-full flex items-center gap-2 px-3 py-1.5 text-sm text-left
                             hover:bg-[var(--color-bg-tertiary)] transition-colors"
                      onclick={() => { filterAction = option.value; loadLogs(); }}
                    >
                      <span class="flex-1">{option.label}</span>
                      {#if filterAction === option.value}
                        <Icon name="check" size="sm" class="text-[var(--color-accent-primary)]" />
                      {/if}
                    </Button>
                  {/each}

                  <div class="my-1 border-t border-[var(--color-border-primary)]"></div>

                  <!-- Resource Filter -->
                  <div class="px-3 py-1 text-xs font-semibold text-[var(--color-text-tertiary)] uppercase">
                    Resource
                  </div>
                  {#each resourceFilterOptions as option (option.value)}
                    <Button
                      variant="unstyled"
                      class="w-full flex items-center gap-2 px-3 py-1.5 text-sm text-left
                             hover:bg-[var(--color-bg-tertiary)] transition-colors"
                      onclick={() => { filterResource = option.value; loadLogs(); }}
                    >
                      <span class="flex-1">{option.label}</span>
                      {#if filterResource === option.value}
                        <Icon name="check" size="sm" class="text-[var(--color-accent-primary)]" />
                      {/if}
                    </Button>
                  {/each}
                </div>
              </Dropdown>
            </div>
          </div>

          <!-- Log List (infinite scroll) -->
          <div class="flex-1 overflow-y-auto pb-1" onscroll={handleListScroll}>
            {#each filteredLogs as log (log.id)}
              <ListItem selected={selectedLogId === log.id}
                onclick={() => selectLog(log)} class="gap-2.5">
                <div class="flex-1 min-w-0 flex flex-col gap-0.5">
                  <div class="flex items-center gap-2">
                    <span
                      class="inline-block px-1.5 py-px rounded text-[0.6875rem] font-medium capitalize"
                      style="background: {getActionColor(log.action)}15; color: {getActionColor(log.action)}"
                    >
                      {log.action}
                    </span>
                    <span class="text-[0.8125rem] font-medium text-[var(--color-text-primary)] truncate">
                      {log.userName}
                    </span>
                  </div>
                  <div class="flex items-center gap-2 text-xs text-[var(--color-text-secondary)] pl-1.5">
                    <span class="capitalize">{log.resourceType}</span>
                    <span class="flex-1"></span>
                    <span class="text-[var(--color-text-tertiary)] flex-shrink-0">{formatDateStr(log.timestamp)}</span>
                  </div>
                </div>
              </ListItem>
            {:else}
              <div class="p-8 text-center text-sm text-[var(--color-text-tertiary)]">
                <Icon name="file-text" size="xl" class="mx-auto mb-2 opacity-50" />
                <p>No logs found</p>
              </div>
            {/each}

            <!-- Loading more indicator -->
            {#if loadingMore}
              <div class="py-3 flex justify-center">
                <Spinner size="sm" />
              </div>
            {/if}
          </div>

          <!-- Footer: Count -->
          <div class="flex-shrink-0 px-3 py-1.5 border-t border-[var(--color-border-primary)] text-xs text-[var(--color-text-tertiary)]">
            {filteredLogs.length} of {totalCount} entries
            {#if hasMore}
              <span class="text-[var(--color-text-quaternary)]">&middot; scroll for more</span>
            {/if}
          </div>
        </div>
      {/snippet}

      <!-- Main Content: Detail Panel -->
      {#if showPanel && selectedLog}
        <AuditLogDetailPanel
          log={selectedLog}
          onClose={closePanel}
        />
      {:else}
        <div class="flex-1 flex items-center justify-center h-full">
          <div class="flex flex-col items-center gap-2 text-[var(--color-text-tertiary)]">
            <Icon name="file-text" size="lg" />
            <p class="text-sm">Select a log entry to view details</p>
          </div>
        </div>
      {/if}
    </SidebarDetailLayout>
  {/if}
</PageShell>
