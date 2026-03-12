<!-- Activity: Settings > Nav: Project Members (settings-project-{id}-members) -->
<script lang="ts">
  /**
   * Project Members Settings Page
   * SidebarDetailLayout — Left sidebar: search + member list, Right: detail/invite panel
   */

  import { untrack } from 'svelte';
  import { MemberDetailPanel } from '../sections';
  import { toast, getAuthState, subscribeAuth, getTabState, setTabState, PageHeader, PageShell, SidebarDetailLayout, Avatar, UserListItem } from '@sddp/shell';
  import { Icon, IconButton, Button, Spinner, SearchField } from '@sddp/ui';
  import type { ProjectMember } from '../../../projects/types/project.types';
  import { getProjectService } from '../../../projects/services/ProjectService';
  import { subscribeConversation, getConversationStoreState } from '../../../conversations/stores';

  interface Props {
    tabId?: string;
    tenantId?: string;
    projectId: string;
    projectName?: string;
  }

  let { tabId = '', tenantId = '', projectId }: Props = $props();

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

  const projectService = getProjectService();

  let members = $state<ProjectMember[]>([]);
  let loading = $state(false);
  let error = $state<string | null>(null);
  let searchQuery = $state('');

  // Panel state
  type PanelMode = 'view' | 'invite';
  let selectedMemberId = $state<string | null>(null);
  let selectedMember = $state<ProjectMember | null>(null);
  let panelMode = $state<PanelMode | null>(null);

  const showPanel = $derived(panelMode !== null);

  const filteredMembers = $derived(
    members.filter((m) => {
      const query = searchQuery.toLowerCase();
      return m.displayName.toLowerCase().includes(query);
    })
  );

  // Tab State Persistence
  interface MembersTabState {
    searchQuery: string;
    selectedMemberId: string | null;
    panelMode: PanelMode | null;
  }

  const tabStateKey = $derived(tabId || `settings-project-${projectId}-members`);
  let restoredKey = $state<string | null>(null);
  let isRestored = $state(false);

  $effect(() => {
    if (!tabStateKey || restoredKey === tabStateKey) return;
    const saved = getTabState<MembersTabState>(tabStateKey);
    if (saved) {
      searchQuery = saved.searchQuery ?? '';
      selectedMemberId = saved.selectedMemberId ?? null;
      panelMode = saved.panelMode ?? null;
    }
    restoredKey = tabStateKey;
    isRestored = true;
  });

  $effect(() => {
    if (!tabStateKey || !isRestored) return;
    setTabState<MembersTabState>(tabStateKey, {
      searchQuery,
      selectedMemberId,
      panelMode,
    });
  });

  // Load members when tenantId or projectId changes (prevLoadKey guard)
  let prevLoadKey = $state<string | null>(null);
  $effect(() => {
    if (!effectiveTenantId || !projectId) return;
    const loadKey = `${effectiveTenantId}:${projectId}`;
    if (loadKey === prevLoadKey) return;
    prevLoadKey = loadKey;
    untrack(() => loadMembers());
  });

  // Sync selectedMember when selectedMemberId changes
  $effect(() => {
    if (!selectedMemberId) {
      selectedMember = null;
      return;
    }
    selectedMember = membersWithPresence.find((m) => m.userId === selectedMemberId) || null;
  });

  // Online users from ConversationHub presence tracking
  let onlineUsers = $state<Set<string>>(getConversationStoreState().onlineUsers);

  $effect(() => {
    const unsubscribe = subscribeConversation((state) => {
      onlineUsers = state.onlineUsers;
    });
    return unsubscribe;
  });

  const membersWithPresence = $derived(
    filteredMembers.map((m) => ({ ...m, isOnline: onlineUsers.has(m.userId) }))
  );

  async function loadMembers() {
    if (!effectiveTenantId || !projectId) {
      error = 'Tenant ID and Project ID are required';
      return;
    }

    loading = true;
    error = null;

    try {
      projectService.setTenantId(effectiveTenantId);
      const projectDetail = await projectService.getProjectById(projectId);
      members = projectDetail.members;
    } catch (err) {
      error = err instanceof Error ? err.message : 'Failed to load members';
      toast.error('Failed to load project members');
      members = [];
    } finally {
      loading = false;
    }
  }

  function selectMember(member: ProjectMember) {
    selectedMemberId = member.userId;
    panelMode = 'view';
  }

  function openInvite() {
    searchQuery = '';
    selectedMemberId = null;
    selectedMember = null;
    panelMode = 'invite';
  }

  function closePanel() {
    selectedMemberId = null;
    selectedMember = null;
    panelMode = null;
  }

  async function handleInvited(userId: string, role: string) {
    if (!effectiveTenantId || !projectId) return;
    try {
      projectService.setTenantId(effectiveTenantId);
      const newMember = await projectService.addProjectMember(projectId, { userId, role });
      members = [...members, newMember];
      closePanel();
      toast.success('Member added successfully');
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to add member';
      toast.error(message);
    }
  }

  function handleRoleChanged(userId: string, newRole: string) {
    members = members.map((m) => (m.userId === userId ? { ...m, role: newRole } : m));
    toast.info('Role change functionality not yet implemented (backend API pending)');
  }

  async function handleDeactivated(userId: string) {
    if (!effectiveTenantId || !projectId) return;
    try {
      projectService.setTenantId(effectiveTenantId);
      await projectService.deactivateProjectMember(projectId, userId);
      members = members.filter((m) => m.userId !== userId);
      closePanel();
      toast.success('Member deactivated');
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to deactivate member';
      toast.error(message);
    }
  }

  async function handleRemoved(userId: string) {
    if (!effectiveTenantId || !projectId) return;
    try {
      projectService.setTenantId(effectiveTenantId);
      await projectService.removeProjectMember(projectId, userId);
      members = members.filter((m) => m.userId !== userId);
      closePanel();
      toast.success('Member removed');
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to remove member';
      toast.error(message);
    }
  }

  function handleSearchChange(query: string) {
    searchQuery = query;
  }
</script>

<PageShell>
  {#if loading}
    <div class="flex-1 flex items-center justify-center pb-28"><Spinner size="lg" /></div>
  {:else if error}
    <div class="flex-1 flex items-center justify-center pb-28">
      <div class="p-12 text-center text-sm text-[var(--color-error-600)]">
        <p class="mb-4">{error}</p>
        <Button variant="secondary" onclick={loadMembers}>Retry</Button>
      </div>
    </div>
  {:else}
  <PageHeader title="Members ({members.length}/10)" {loading}>
    {#snippet actions()}
      <IconButton icon="refresh-cw" title="Refresh" onclick={loadMembers} />
      <IconButton icon="plus" title="Invite Member" onclick={openInvite} />
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
          <!-- Sidebar Header: Search -->
          <div class="flex-shrink-0 p-2 min-h-12 border-b border-[var(--color-border-primary)]">
            <SearchField
              bind:value={searchQuery}
              placeholder="Search members..."
              debounceMs={0}
              onSearch={handleSearchChange}
              size="sm"
            />
          </div>

          <!-- Member List -->
          <div class="flex-1 overflow-y-auto pb-1">
            {#each membersWithPresence as member (member.userId)}
              <UserListItem
                user={{ displayName: member.displayName, email: member.role }}
                selected={selectedMemberId === member.userId}
                onclick={() => selectMember(member)}
                density="compact"
              >
                {#snippet leading()}
                  <Avatar name={member.displayName} size="md" />
                {/snippet}
                {#snippet trailing()}
                  <div class="flex items-center gap-2 flex-shrink-0">
                    {#if member.role === 'Admin'}
                      <span class="inline-block px-1.5 py-px rounded text-[0.6875rem] bg-amber-500/10 text-amber-600">
                        Admin
                      </span>
                    {:else if member.role === 'ProductOwner'}
                      <span class="inline-block px-1.5 py-px rounded text-[0.6875rem] bg-purple-500/10 text-purple-500">
                        PO
                      </span>
                    {/if}
                    <span class="w-1.5 h-1.5 rounded-full flex-shrink-0
                                 {member.isOnline ? 'bg-green-500' : 'bg-[var(--color-text-tertiary)]'}"></span>
                  </div>
                {/snippet}
              </UserListItem>
            {:else}
              <div class="p-8 text-center text-sm text-[var(--color-text-tertiary)]">
                <Icon name="circle-user-round" size="xl" class="mx-auto mb-2 opacity-50" />
                <p>No members found</p>
              </div>
            {/each}
          </div>
        </div>
      {/snippet}

      <!-- Main Content: Detail / Invite -->
      {#if showPanel}
        {#if panelMode === 'invite'}
          <MemberDetailPanel
            mode="invite"
            member={null}
            memberCount={members.length}
            existingMemberIds={members.map((m) => m.userId)}
            onClose={closePanel}
            onInvited={handleInvited}
            onRoleChanged={handleRoleChanged}
            onRemoved={handleRemoved}
            onDeactivated={handleDeactivated}
          />
        {:else if panelMode === 'view' && selectedMember}
          <MemberDetailPanel
            mode="view"
            member={selectedMember}
            memberCount={members.length}
            existingMemberIds={members.map((m) => m.userId)}
            onClose={closePanel}
            onInvited={handleInvited}
            onRoleChanged={handleRoleChanged}
            onRemoved={handleRemoved}
            onDeactivated={handleDeactivated}
          />
        {/if}
      {:else}
        <div class="flex-1 flex items-center justify-center h-full">
          <div class="flex flex-col items-center gap-2 text-[var(--color-text-tertiary)]">
            <Icon name="circle-user-round" size="lg" />
            <p class="text-sm">Select a member to view details</p>
            <p class="text-xs">or click + to invite a new member</p>
          </div>
        </div>
      {/if}
    </SidebarDetailLayout>
  {/if}
</PageShell>

