<!-- Activity: Settings > Nav: System Users (settings-system-users) -->
<script lang="ts">
  /**
   * System Users Settings Page
   * Admin page for managing all users — SidebarDetailLayout
   * Left sidebar: search + filters + user list
   * Right main: detail/edit/create panel
   */

  import { untrack } from 'svelte';
  import { UserDetailPanel } from '../sections';
  import { toast, getAuthState, subscribeAuth, PageHeader, PageShell, SidebarDetailLayout, Dropdown, RichListItem, getTabState, setTabState, Avatar } from '@sddp/shell';
  import { Icon, Input, IconButton, Button, Spinner } from '@sddp/ui';
  import type { SystemUser } from '../../types';
  import { getUserManagementService } from '../../services';
  import { ALL_ROLES, getRoleLabel } from '../../../shared/types';

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

  const userService = getUserManagementService();

  let users = $state<SystemUser[]>([]);
  let loading = $state(false);
  let loadingMore = $state(false);
  let showLoadingMore = $state(false);
  let error = $state<string | null>(null);
  let searchQuery = $state('');
  let filterRole = $state<string>('all');
  let filterStatus = $state<'all' | 'active' | 'inactive'>('all');

  // Pagination
  const PAGE_SIZE = 20;
  const LOADING_MORE_INDICATOR_DELAY_MS = 180;
  const LOADING_MORE_MIN_LATENCY_MS = 1000;
  let currentPage = $state(1);
  let hasMore = $state(false);
  let loadingMoreIndicatorTimer: ReturnType<typeof setTimeout> | null = null;

  // Tab State Persistence
  interface UsersTabState {
    searchQuery: string;
    filterRole: string;
    filterStatus: 'all' | 'active' | 'inactive';
    selectedUserId: string | null;
    panelMode: PanelMode | null;
  }

  const tabStateKey = $derived(tabId || 'settings-system-users');
  let restoredKey = $state<string | null>(null);
  let isRestored = $state(false);

  $effect(() => {
    if (!tabStateKey || restoredKey === tabStateKey) return;
    const saved = getTabState<UsersTabState>(tabStateKey);
    if (saved) {
      const validRoles = new Set(['all', ...ALL_ROLES.map((r) => r.value)]);
      searchQuery = saved.searchQuery ?? '';
      filterRole = saved.filterRole && validRoles.has(saved.filterRole) ? saved.filterRole : 'all';
      filterStatus = saved.filterStatus ?? 'all';
      selectedUserId = saved.selectedUserId ?? null;
      panelMode = saved.panelMode ?? null;
    }
    restoredKey = tabStateKey;
    isRestored = true;
  });

  $effect(() => {
    if (!tabStateKey || !isRestored) return;
    setTabState<UsersTabState>(tabStateKey, {
      searchQuery,
      filterRole,
      filterStatus,
      selectedUserId,
      panelMode,
    });
  });

  // Panel state
  type PanelMode = 'view' | 'edit' | 'create';
  let selectedUserId = $state<string | null>(null);
  let selectedUser = $state<SystemUser | null>(null);
  let panelMode = $state<PanelMode | null>(null);
  let loadingDetail = $state(false);

  const showPanel = $derived(panelMode !== null);

  // Filter options for dropdown
  const roleFilterOptions: { value: string; label: string; icon: string }[] = [
    { value: 'all', label: 'All Roles', icon: 'users' },
    ...ALL_ROLES.map((r) => ({
      value: r.value,
      label: r.label,
      icon: r.value === 'Admin' ? 'shield' : 'user',
    })),
  ];

  const statusFilterOptions: { value: typeof filterStatus; label: string; icon: string }[] = [
    { value: 'all', label: 'All Status', icon: 'list' },
    { value: 'active', label: 'Active', icon: 'check-circle' },
    { value: 'inactive', label: 'Inactive', icon: 'x-circle' },
  ];

  // Load users when tenantId changes (prevLoadKey guard)
  let prevLoadKey = $state<string | null>(null);
  let prevFilterKey = $state<string | null>(null);
  let searchDebounceTimer: ReturnType<typeof setTimeout> | null = null;

  let latestUsersRequestId = 0;

  function buildFilterKey(): string {
    return `${searchQuery.trim()}|${filterRole}|${filterStatus}`;
  }

  function clearLoadingMoreIndicatorTimer() {
    if (loadingMoreIndicatorTimer) {
      clearTimeout(loadingMoreIndicatorTimer);
      loadingMoreIndicatorTimer = null;
    }
  }

  function wait(ms: number): Promise<void> {
    return new Promise((resolve) => setTimeout(resolve, ms));
  }

  $effect(() => {
    return () => {
      clearLoadingMoreIndicatorTimer();
    };
  });

  $effect(() => {
    if (!isRestored) return;
    if (!effectiveTenantId) return;
    if (effectiveTenantId === prevLoadKey) return;
    prevLoadKey = effectiveTenantId;
    prevFilterKey = buildFilterKey();
    untrack(() => loadUsers());
  });

  // Reload users when filters/search change (debounced for search input)
  $effect(() => {
    if (!isRestored) return;
    if (!effectiveTenantId) return;

    const filterKey = buildFilterKey();
    if (filterKey === prevFilterKey) return;

    prevFilterKey = filterKey;

    if (searchDebounceTimer) {
      clearTimeout(searchDebounceTimer);
    }

    searchDebounceTimer = setTimeout(() => {
      untrack(() => loadUsers());
    }, 250);

    return () => {
      if (searchDebounceTimer) {
        clearTimeout(searchDebounceTimer);
      }
    };
  });

  // Load user detail when selectedUserId changes (prevId guard)
  let prevSelectedUserId = $state<string | null>(null);
  $effect(() => {
    if (!selectedUserId) {
      prevSelectedUserId = null;
      selectedUser = null;
      return;
    }
    if (selectedUserId === prevSelectedUserId) return;
    prevSelectedUserId = selectedUserId;
    untrack(() => loadUserDetail(selectedUserId!));
  });

  async function loadUsers(append = false, targetPage = 1) {
    if (!effectiveTenantId) {
      error = 'Tenant ID is required';
      return;
    }

    const requestId = ++latestUsersRequestId;
    const requestStartedAt = Date.now();

    if (append) {
      loadingMore = true;
      showLoadingMore = false;
      clearLoadingMoreIndicatorTimer();
      loadingMoreIndicatorTimer = setTimeout(() => {
        if (loadingMore && latestUsersRequestId === requestId) {
          showLoadingMore = true;
        }
      }, LOADING_MORE_INDICATOR_DELAY_MS);
    } else {
      loading = true;
      error = null;
    }

    try {
      userService.setContext(effectiveTenantId);
      const result = await userService.getUsers({
        search: searchQuery.trim() || undefined,
        role: filterRole !== 'all' ? filterRole : undefined,
        status: filterStatus !== 'all' ? filterStatus : undefined,
        page: targetPage,
        size: PAGE_SIZE,
      });

      if (requestId !== latestUsersRequestId) return;

      if (append) {
        const elapsed = Date.now() - requestStartedAt;
        if (elapsed < LOADING_MORE_MIN_LATENCY_MS) {
          await wait(LOADING_MORE_MIN_LATENCY_MS - elapsed);
        }
        if (requestId !== latestUsersRequestId) return;
      }

      if (append) {
        users = [...users, ...result.items];
      } else {
        users = result.items;
      }
      currentPage = result.pageNumber;
      hasMore = result.hasNextPage;
    } catch (err) {
      if (requestId !== latestUsersRequestId) return;

      error = err instanceof Error ? err.message : 'Failed to load users';
      toast.error('Failed to load users');
      if (!append) {
        users = [];
        currentPage = 1;
        hasMore = false;
      }
    } finally {
      if (requestId === latestUsersRequestId) {
        loading = false;
        loadingMore = false;
        showLoadingMore = false;
        clearLoadingMoreIndicatorTimer();
      }
    }
  }

  function loadNextPage() {
    if (loading || loadingMore || !hasMore) return;
    untrack(() => loadUsers(true, currentPage + 1));
  }

  function handleListScroll(e: Event) {
    const target = e.target as HTMLElement;
    const threshold = 100;
    if (target.scrollHeight - target.scrollTop - target.clientHeight < threshold) {
      loadNextPage();
    }
  }

  async function loadUserDetail(userId: string) {
    loadingDetail = true;
    try {
      userService.setContext(effectiveTenantId);
      selectedUser = await userService.getUserById(userId);
    } catch {
      toast.error('Failed to load user details');
      selectedUser = null;
    } finally {
      loadingDetail = false;
    }
  }

  function selectUser(user: SystemUser) {
    selectedUserId = user.id;
    panelMode = 'view';
  }

  function openCreate() {
    searchQuery = '';
    selectedUserId = null;
    selectedUser = null;
    panelMode = 'create';
  }

  function openEdit() {
    panelMode = 'edit';
  }

  function closePanel() {
    if (panelMode === 'edit') {
      panelMode = 'view';
      return;
    }
    selectedUserId = null;
    selectedUser = null;
    panelMode = null;
  }

  function handleCreated(user: SystemUser) {
    selectedUserId = user.id;
    selectedUser = user;
    panelMode = 'view';
    untrack(() => loadUsers());
  }

  function handleSaved(user: SystemUser) {
    selectedUser = user;
    panelMode = 'view';
    untrack(() => loadUsers());
  }

  function handleDeactivated(userId: string) {
    if (selectedUser?.id === userId) {
      selectedUser = { ...selectedUser, status: 'inactive' };
    }
    untrack(() => loadUsers());
  }

  function handleActivated(userId: string) {
    if (selectedUser?.id === userId) {
      selectedUser = { ...selectedUser, status: 'active' };
    }
    untrack(() => loadUsers());
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
        <Button variant="secondary" onclick={() => loadUsers()}>Retry</Button>
      </div>
    </div>
  {:else}
  <PageHeader title="Users" {loading}>
    {#snippet actions()}
      <IconButton icon="refresh-cw" title="Refresh" onclick={() => loadUsers()} />
      <IconButton icon="plus" title="New User" onclick={openCreate} />
    {/snippet}
  </PageHeader>
    <SidebarDetailLayout
      showRightPanel={false}
      sidebarWidth={360}
      minSidebarWidth={280}
      maxSidebarWidth={520}
    >
      {#snippet sidebar()}
        <div class="flex flex-col h-full min-h-0 bg-[var(--color-bg-primary)]">
          <!-- Sidebar Header: Search + Actions (GlossarySidebar pattern) -->
          <div class="flex-shrink-0 flex items-center p-2 min-h-12 border-b border-[var(--color-border-primary)]">
            <div class="flex items-center gap-1 w-full">
              <!-- Search Input -->
              <div class="relative flex-1">
                <Icon
                  name="search"
                  size="sm"
                  class="absolute left-2.5 top-1/2 -translate-y-1/2 text-[var(--color-text-tertiary)]"
                />
                <Input
                  type="text"
                  placeholder="Search users..."
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
                  <!-- Role Filter -->
                  <div class="px-3 py-1 text-xs font-semibold text-[var(--color-text-tertiary)] uppercase">
                    Role
                  </div>
                  {#each roleFilterOptions as option (option.value)}
                    <Button
                      variant="unstyled"
                      class="w-full flex items-center gap-2 px-3 py-1.5 text-sm text-left
                             hover:bg-[var(--color-bg-tertiary)] transition-colors"
                      onclick={() => (filterRole = option.value)}
                    >
                      <Icon name={option.icon} size="sm" class="text-[var(--color-text-tertiary)]" />
                      <span class="flex-1">{option.label}</span>
                      {#if filterRole === option.value}
                        <Icon name="check" size="sm" class="text-[var(--color-accent-primary)]" />
                      {/if}
                    </Button>
                  {/each}

                  <div class="my-1 border-t border-[var(--color-border-primary)]"></div>

                  <!-- Status Filter -->
                  <div class="px-3 py-1 text-xs font-semibold text-[var(--color-text-tertiary)] uppercase">
                    Status
                  </div>
                  {#each statusFilterOptions as option (option.value)}
                    <Button
                      variant="unstyled"
                      class="w-full flex items-center gap-2 px-3 py-1.5 text-sm text-left
                             hover:bg-[var(--color-bg-tertiary)] transition-colors"
                      onclick={() => (filterStatus = option.value)}
                    >
                      <Icon name={option.icon} size="sm" class="text-[var(--color-text-tertiary)]" />
                      <span class="flex-1">{option.label}</span>
                      {#if filterStatus === option.value}
                        <Icon name="check" size="sm" class="text-[var(--color-accent-primary)]" />
                      {/if}
                    </Button>
                  {/each}
                </div>
              </Dropdown>
            </div>
          </div>

          <!-- User List -->
          <div class="flex-1 overflow-y-auto pb-1" onscroll={handleListScroll}>
            {#each users as user (user.id)}
              <RichListItem
                selected={selectedUserId === user.id}
                title={user.name}
                description={user.email}
                density="compact"
                onclick={() => selectUser(user)}
              >
                {#snippet leading()}
                  <Avatar name={user.name} size="md" />
                {/snippet}
                {#snippet trailing()}
                  <div class="flex items-center gap-1.5 ml-auto flex-shrink-0">
                    <span class="inline-block px-1.5 py-px rounded text-[0.6875rem]
                                 {user.globalRole === 'Admin'
                                   ? 'bg-purple-500/10 text-purple-500'
                                   : 'bg-[var(--color-bg-tertiary)] text-[var(--color-text-secondary)]'}">
                      {getRoleLabel(user.globalRole)}
                    </span>
                    <span class="w-1.5 h-1.5 rounded-full
                                 {user.status === 'active' ? 'bg-green-500' : 'bg-[var(--color-text-tertiary)]'}"></span>
                  </div>
                {/snippet}
              </RichListItem>
            {:else}
              <div class="p-8 text-center text-sm text-[var(--color-text-tertiary)]">
                <Icon name="user" size="xl" class="mx-auto mb-2 opacity-50" />
                <p>No users found</p>
              </div>
            {/each}
            {#if showLoadingMore}
              <div class="flex justify-center py-3">
                <Spinner size="sm" />
              </div>
            {/if}
          </div>
        </div>
      {/snippet}

      <!-- Main Content: Detail / Edit / Create -->
      {#if showPanel}
        {#if loadingDetail && panelMode === 'view'}
          <div class="flex-1 flex items-center justify-center"><Spinner size="lg" /></div>
        {:else if panelMode === 'create'}
          <UserDetailPanel
            mode="create"
            user={null}
            onClose={closePanel}
            onCreated={handleCreated}
            onSaved={handleSaved}
            onDeactivated={handleDeactivated}
            onActivated={handleActivated}
          />
        {:else if panelMode === 'edit' && selectedUser}
          <UserDetailPanel
            mode="edit"
            user={selectedUser}
            onClose={closePanel}
            onCreated={handleCreated}
            onSaved={handleSaved}
            onDeactivated={handleDeactivated}
            onActivated={handleActivated}
          />
        {:else if panelMode === 'view' && selectedUser}
          <UserDetailPanel
            mode="view"
            user={selectedUser}
            onClose={() => { selectedUserId = null; selectedUser = null; panelMode = null; }}
            onEdit={openEdit}
            onCreated={handleCreated}
            onSaved={handleSaved}
            onDeactivated={handleDeactivated}
            onActivated={handleActivated}
          />
        {/if}
      {:else}
        <div class="flex-1 flex items-center justify-center h-full">
          <div class="flex flex-col items-center gap-2 text-[var(--color-text-tertiary)]">
            <Icon name="user" size="lg" />
            <p class="text-sm">Select a user to view details</p>
            <p class="text-xs">or click + to create a new user</p>
          </div>
        </div>
      {/if}
    </SidebarDetailLayout>
  {/if}
</PageShell>
