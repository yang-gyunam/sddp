<!-- Section: MemberDetailPanel — Settings > Project Members -->
<script lang="ts">
  /**
   * MemberDetailPanel
   * Right panel for viewing and inviting project members
   */

  import { untrack } from 'svelte';
  import { formatDateTime, getAuthState } from '@sddp/shell';
  import { Icon, IconButton, Select, Button, SearchField, Spinner } from '@sddp/ui';
  import DetailHeader from '../../../shared/components/idioms/DetailHeader.svelte';
  import type { ProjectMember } from '../../../projects/types/project.types';
  import { PROJECT_ROLES, getRoleLabel, isSystemRole } from '../../../shared/types';
  import { getTenantMembers } from '../../services/UserManagementService';
  import type { TenantMember } from '../../services/UserManagementService';

  const MAX_PROJECT_MEMBERS = 10;

  type PanelMode = 'view' | 'invite';

  interface Props {
    mode: PanelMode;
    member: ProjectMember | null;
    memberCount: number;
    existingMemberIds: string[];
    onClose: () => void;
    onInvited: (userId: string, role: string) => void | Promise<void>;
    onRoleChanged: (userId: string, newRole: string) => void;
    onRemoved: (userId: string) => void;
    onDeactivated: (userId: string) => void;
  }

  let { mode, member, memberCount, existingMemberIds, onClose, onInvited, onRoleChanged, onRemoved, onDeactivated }: Props = $props();

  let loading = $state(false);
  let errors = $state<Record<string, string>>({});

  // Invite form fields
  let inviteForm = $state({ userId: '', userName: '', role: 'Developer' });

  // User search state
  let userSearchQuery = $state('');
  let userSearchResults = $state<TenantMember[]>([]);
  let searching = $state(false);
  let searchDebounceTimer: ReturnType<typeof setTimeout> | null = null;

  const isAtLimit = $derived(memberCount >= MAX_PROJECT_MEMBERS);

  // Reset invite form when mode becomes invite
  $effect(() => {
    if (mode === 'invite') {
      inviteForm = { userId: '', userName: '', role: 'Developer' };
      userSearchQuery = '';
      userSearchResults = [];
      errors = {};
    }
  });

  const roleSelectOptions = PROJECT_ROLES.map((r) => ({ label: r.label, value: r.value }));

  function handleSearchInput(query: string) {
    userSearchQuery = query;
    if (searchDebounceTimer) clearTimeout(searchDebounceTimer);
    if (!query || query.length < 2) {
      userSearchResults = [];
      return;
    }
    searchDebounceTimer = setTimeout(() => {
      untrack(() => searchUsers(query));
    }, 300);
  }

  async function searchUsers(query: string) {
    const tenantId = getAuthState().user?.tenantId;
    if (!tenantId) return;

    searching = true;
    try {
      const results = await getTenantMembers(tenantId, query);
      // Filter out existing project members
      userSearchResults = results.filter((u) => !existingMemberIds.includes(u.id));
    } catch {
      userSearchResults = [];
    } finally {
      searching = false;
    }
  }

  function selectUser(user: TenantMember) {
    inviteForm.userId = user.id;
    inviteForm.userName = user.name;
    userSearchQuery = '';
    userSearchResults = [];
    errors = {};
  }

  function clearSelectedUser() {
    inviteForm.userId = '';
    inviteForm.userName = '';
  }

  function validateInviteForm(): boolean {
    const newErrors: Record<string, string> = {};

    if (!inviteForm.userId) {
      newErrors.user = 'Please select a user';
    }

    errors = newErrors;
    return Object.keys(newErrors).length === 0;
  }

  async function handleInvite() {
    if (!validateInviteForm()) return;

    loading = true;
    try {
      await Promise.resolve(onInvited(inviteForm.userId, inviteForm.role));
    } finally {
      loading = false;
    }
  }

  function handleRoleChange(newRole: string) {
    if (!member) return;
    onRoleChanged(member.userId, newRole);
  }

  function handleDeactivate() {
    if (!member) return;
    if (!confirm(`Deactivate ${member.displayName}? They will lose project access.`)) return;
    onDeactivated(member.userId);
  }

  function handleRemove() {
    if (!member) return;
    if (!confirm(`Remove ${member.displayName} from the project? This action cannot be undone.`)) return;
    onRemoved(member.userId);
  }

  function formatDateStr(dateStr?: string): string {
    if (!dateStr) return 'Never';
    return formatDateTime(dateStr, { month: 'short', year: undefined });
  }
</script>

<div class="panel">
  {#if mode === 'view' && member}
    <DetailHeader>
      {#snippet leading()}
        <Icon name="user" size="md" class="text-[var(--color-accent-primary)]" />
      {/snippet}
      <span class="text-sm font-semibold text-[var(--color-text-primary)]">Member Details</span>
      {#snippet actions()}
        {#if !isSystemRole(member.role)}
          <IconButton icon="lock" variant="warn" onclick={handleDeactivate} disabled={loading} title="Deactivate member" />
          <IconButton icon="trash" variant="danger" onclick={handleRemove} disabled={loading} title="Remove member" />
        {/if}
        <IconButton icon="x" onclick={onClose} title="Close" />
      {/snippet}
    </DetailHeader>
  {:else if mode === 'invite'}
    <DetailHeader>
      {#snippet leading()}
        <Icon name="person-add" size="md" class="text-[var(--color-accent-primary)]" />
      {/snippet}
      <span class="text-sm font-semibold text-[var(--color-text-primary)]">Add Member</span>
      {#snippet actions()}
        <IconButton icon="check" variant="success" onclick={handleInvite} disabled={loading || isAtLimit || !inviteForm.userId} title="Add member" />
        <IconButton icon="x" onclick={onClose} title="Cancel" />
      {/snippet}
    </DetailHeader>
  {/if}

  <div class="panel-body">
    {#if mode === 'invite'}
      <!-- Invite Mode -->
      {#if isAtLimit}
        <div class="limit-warning">
          <Icon name="alert-triangle" size="sm" />
          <span>Maximum {MAX_PROJECT_MEMBERS} members reached</span>
        </div>
      {/if}

      <div class="form-section">
        {#if inviteForm.userId}
          <!-- Selected User -->
          <div class="selected-user">
            <div class="selected-user-info">
              <Icon name="user" size="sm" class="text-[var(--color-accent-primary)]" />
              <span class="text-sm font-medium text-[var(--color-text-primary)]">{inviteForm.userName}</span>
            </div>
            <IconButton icon="x" variant="ghost" onclick={clearSelectedUser} title="Clear selection" />
          </div>
        {:else}
          <!-- User Search -->
          <div class="user-search">
            <SearchField
              bind:value={userSearchQuery}
              placeholder="Search users by name or email..."
              debounceMs={0}
              onSearch={handleSearchInput}
              size="sm"
            />
            {#if errors.user}
              <span class="text-xs text-[var(--color-error-600)] mt-1">{errors.user}</span>
            {/if}
            {#if searching}
              <div class="search-loading"><Spinner size="sm" /></div>
            {:else if userSearchResults.length > 0}
              <div class="user-results">
                {#each userSearchResults as user (user.id)}
                  <Button
                    variant="unstyled"
                    fullWidth
                    class="text-left p-2 rounded-lg transition-colors hover:bg-[var(--color-bg-tertiary)]"
                    onclick={() => selectUser(user)}
                  >
                    <span class="text-sm text-[var(--color-text-primary)]">{user.name}</span>
                    <span class="text-xs text-[var(--color-text-tertiary)] ml-1">{user.email}</span>
                  </Button>
                {/each}
              </div>
            {:else if userSearchQuery.length >= 2}
              <div class="text-xs text-[var(--color-text-tertiary)] mt-2 text-center">No users found</div>
            {/if}
          </div>
        {/if}

        <Select label="Role" bind:value={inviteForm.role} disabled={loading || isAtLimit} options={roleSelectOptions} />
      </div>

    {:else if mode === 'view' && member}
      <!-- View Mode -->
      <div class="member-header">
        <div class="member-avatar">{member.displayName.charAt(0).toUpperCase()}</div>
        <div class="member-info">
          <h4 class="member-name">{member.displayName}</h4>
          <div class="member-status-row">
            <span class="online-dot" class:online={member.isOnline}></span>
            <span class="status-text">{member.isOnline ? 'Online' : 'Offline'}</span>
          </div>
        </div>
        <span class="role-badge" class:system={isSystemRole(member.role)}>{getRoleLabel(member.role)}</span>
      </div>

      <div class="detail-grid">
        <div class="detail-item">
          <span class="detail-label">Role</span>
          <span class="detail-value">{getRoleLabel(member.role)}</span>
        </div>
        <div class="detail-item">
          <span class="detail-label">Last Activity</span>
          <span class="detail-value">{formatDateStr(member.lastActivityAt)}</span>
        </div>
      </div>

      <!-- Role Change (system roles like Admin cannot be reassigned) -->
      {#if !isSystemRole(member.role)}
        <div class="role-section">
          <h4 class="section-title">Change Role</h4>
          <div class="role-options">
            {#each PROJECT_ROLES as role (role.value)}
              <Button
                variant="unstyled"
                class="role-option {member.role === role.value ? 'active' : ''}"
                onclick={() => handleRoleChange(role.value)}
              >
                {role.label}
              </Button>
            {/each}
          </div>
        </div>
      {/if}
    {/if}
  </div>
</div>

<style>
  @import '../../styles/detail-panel.css';
  @import '../../styles/detail-panel-form.css';

  /* Limit Warning */
  .limit-warning {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    padding: 0.5rem 0.75rem;
    border-radius: 6px;
    background: var(--color-warning-700-10, rgba(180, 83, 9, 0.1));
    color: var(--color-warning-700, #b45309);
    font-size: 0.75rem;
    margin-bottom: 0.75rem;
  }

  /* User Search */
  .user-search {
    display: flex;
    flex-direction: column;
  }

  .search-loading {
    display: flex;
    justify-content: center;
    padding: 0.5rem;
  }

  .user-results {
    max-height: 160px;
    overflow-y: auto;
    border: 1px solid var(--color-border-primary, #e5e7eb);
    border-radius: 6px;
    margin-top: 0.25rem;
  }

  /* Selected User */
  .selected-user {
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding: 0.5rem 0.75rem;
    border: 1px solid var(--color-accent-primary, #3b82f6);
    border-radius: 6px;
    background: var(--color-accent-primary-10, rgba(59, 130, 246, 0.05));
  }

  .selected-user-info {
    display: flex;
    align-items: center;
    gap: 0.5rem;
  }

  /* Member Header (View Mode) */
  .member-header {
    display: flex;
    align-items: center;
    gap: 0.75rem;
  }

  .member-avatar {
    width: 40px;
    height: 40px;
    border-radius: 50%;
    background: var(--color-accent-primary, #3b82f6);
    color: white;
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: 1rem;
    font-weight: 600;
    flex-shrink: 0;
  }

  .member-info {
    flex: 1;
    min-width: 0;
  }

  .member-name {
    margin: 0;
    font-size: 1rem;
    font-weight: 600;
    color: var(--color-text-primary, #111);
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
  }

  .member-status-row {
    display: flex;
    align-items: center;
    gap: 0.375rem;
    margin-top: 0.125rem;
  }

  .online-dot {
    width: 6px;
    height: 6px;
    border-radius: 50%;
    background: var(--color-text-tertiary, #9ca3af);
    flex-shrink: 0;
  }

  .online-dot.online {
    background: var(--color-success-500, #22c55e);
  }

  .status-text {
    font-size: 0.75rem;
    color: var(--color-text-secondary, #6b7280);
  }

  /* Badge */
  .role-badge {
    padding: 0.125rem 0.5rem;
    border-radius: 4px;
    font-size: 0.6875rem;
    font-weight: 500;
    text-transform: uppercase;
    background: var(--color-bg-tertiary, #f3f4f6);
    color: var(--color-text-secondary, #6b7280);
    flex-shrink: 0;
  }

  .role-badge.system {
    background: var(--color-warning-700-10, rgba(180, 83, 9, 0.1));
    color: var(--color-warning-700, #b45309);
  }

  /* Role Section */
  .role-section {
    border-top: 1px solid var(--color-border-primary, #e5e7eb);
    padding-top: 1rem;
  }

  .role-options {
    display: flex;
    flex-wrap: wrap;
    gap: 0.375rem;
  }

  :global(.role-option) {
    padding: 0.25rem 0.75rem;
    border: 1px solid var(--color-border-primary, #e5e7eb);
    border-radius: 4px;
    background: var(--color-bg-primary, #fff);
    color: var(--color-text-primary, #111);
    font-size: 0.75rem;
    cursor: pointer;
  }

  :global(.role-option:hover:not(:disabled)) {
    background: var(--color-bg-tertiary, #f3f4f6);
  }

  :global(.role-option.active) {
    background: var(--color-accent-primary, #3b82f6);
    border-color: var(--color-accent-primary, #3b82f6);
    color: white;
  }

  :global(.role-option:disabled) {
    opacity: 0.4;
    cursor: not-allowed;
  }
</style>
