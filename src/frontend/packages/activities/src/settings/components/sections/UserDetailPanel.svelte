<!-- Section: UserDetailPanel — Settings > System Users -->
<script lang="ts">
  /**
   * UserDetailPanel
   * Right panel for viewing, editing, and creating users
   */

  import { formatDateTime, toast } from '@sddp/shell';
  import { Icon, Input, IconButton, Select } from '@sddp/ui';
  import DetailHeader from '../../../shared/components/idioms/DetailHeader.svelte';
  import type { SystemUser, CreateUserParams } from '../../types';
  import { getUserManagementService } from '../../services';
  import { ALL_ROLES, getRoleLabel } from '../../../shared/types';

  type PanelMode = 'view' | 'edit' | 'create';

  interface Props {
    mode: PanelMode;
    user: SystemUser | null;
    onClose: () => void;
    onEdit?: () => void;
    onCreated: (user: SystemUser) => void;
    onSaved: (user: SystemUser) => void;
    onDeactivated: (userId: string) => void;
    onActivated: (userId: string) => void;
    onDeleted?: (userId: string) => void;
  }

  let { mode, user, onClose, onEdit, onCreated, onSaved, onDeactivated, onActivated, onDeleted }: Props = $props();

  const userService = getUserManagementService();

  const roleOptions = ALL_ROLES.map((r) => ({ label: r.label, value: r.value }));

  let loading = $state(false);
  let errors = $state<Record<string, string>>({});

  // Create form fields
  let createForm = $state<CreateUserParams>({
    username: '',
    email: '',
    displayName: '',
    password: '',
  });

  // Edit form fields
  let editForm = $state<{ name: string; email: string; globalRole: string }>({
    name: '',
    email: '',
    globalRole: 'Developer',
  });

  // Reset edit form when user changes
  $effect(() => {
    if (user && mode === 'edit') {
      editForm = {
        name: user.name,
        email: user.email,
        globalRole: user.globalRole,
      };
    }
  });

  // Reset create form when mode becomes create
  $effect(() => {
    if (mode === 'create') {
      createForm = { username: '', email: '', displayName: '', password: '' };
      errors = {};
    }
  });

  function validateCreateForm(): boolean {
    const newErrors: Record<string, string> = {};

    if (!createForm.username || createForm.username.length < 3) {
      newErrors.username = 'Username must be at least 3 characters';
    }
    if (!createForm.email || !createForm.email.includes('@')) {
      newErrors.email = 'Valid email is required';
    }
    if (!createForm.displayName || createForm.displayName.length < 2) {
      newErrors.displayName = 'Display name must be at least 2 characters';
    }
    if (!createForm.password || createForm.password.length < 6) {
      newErrors.password = 'Password must be at least 6 characters';
    }

    errors = newErrors;
    return Object.keys(newErrors).length === 0;
  }

  async function handleCreate() {
    if (!validateCreateForm()) return;

    loading = true;
    try {
      const created = await userService.createUser(createForm);
      toast.success(`User "${created.name}" created successfully`);
      onCreated(created);
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to create user';
      toast.error(message);
    } finally {
      loading = false;
    }
  }

  async function handleSave() {
    if (!user) return;

    loading = true;
    try {
      const updated = await userService.updateUser(user.id, {
        displayName: editForm.name,
        email: editForm.email,
        globalRole: editForm.globalRole,
      });
      toast.success(`User "${updated.name}" updated`);
      onSaved(updated);
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to update user';
      toast.error(message);
    } finally {
      loading = false;
    }
  }

  async function handleDeactivate() {
    if (!user) return;
    if (!confirm(`Are you sure you want to deactivate ${user.name}?`)) return;

    loading = true;
    try {
      await userService.deactivateUser(user.id);
      toast.success(`User "${user.name}" deactivated`);
      onDeactivated(user.id);
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to deactivate user';
      toast.error(message);
    } finally {
      loading = false;
    }
  }

  async function handleActivate() {
    if (!user) return;
    if (!confirm(`Are you sure you want to activate ${user.name}?`)) return;

    loading = true;
    try {
      await userService.activateUser(user.id);
      toast.success(`User "${user.name}" activated`);
      onActivated(user.id);
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to activate user';
      toast.error(message);
    } finally {
      loading = false;
    }
  }

  async function handleResetPassword() {
    if (!user) return;
    if (!confirm(`Reset password for ${user.name}? A temporary password will be generated.`)) return;

    loading = true;
    try {
      const result = await userService.resetPassword(user.id);
      toast.success(`Temporary password: ${result.temporaryPassword}`);
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to reset password';
      toast.error(message);
    } finally {
      loading = false;
    }
  }

  async function handleDelete() {
    if (!user || !onDeleted) return;
    if (!confirm(`Are you sure you want to delete ${user.name}? This action cannot be undone.`)) return;

    loading = true;
    try {
      await onDeleted(user.id);
      toast.info('Delete functionality not yet implemented (backend API pending)');
    } finally {
      loading = false;
    }
  }

  function formatDateStr(dateStr?: string): string {
    if (!dateStr) return 'Never';
    return formatDateTime(dateStr, { month: 'short', year: undefined });
  }
</script>

<div class="panel">
  {#if mode === 'view' && user}
    <DetailHeader>
      {#snippet leading()}
        <Icon name="user" size="md" class="text-[var(--color-accent-primary)]" />
      {/snippet}
      <span class="text-sm font-semibold text-[var(--color-text-primary)]">User Details</span>
      {#snippet actions()}
        {#if !user.isBuiltIn && user.status === 'active'}
          <IconButton icon="key" variant="warn" onclick={handleResetPassword} disabled={loading} title="Reset password" />
          <IconButton icon="lock" variant="warn" onclick={handleDeactivate} disabled={loading} title="Deactivate user" />
        {/if}
        {#if !user.isBuiltIn && user.status === 'inactive'}
          <IconButton icon="unlock" variant="success" onclick={handleActivate} disabled={loading} title="Activate user" />
        {/if}
        {#if !user.isBuiltIn && onDeleted}
          <IconButton icon="trash" variant="danger" onclick={handleDelete} disabled={loading} title="Delete user" />
        {/if}
        {#if onEdit}
          <IconButton icon="edit" onclick={onEdit} title="Edit user" />
        {/if}
        <IconButton icon="x" onclick={onClose} title="Close" />
      {/snippet}
    </DetailHeader>
  {:else if mode === 'create'}
    <DetailHeader>
      {#snippet leading()}
        <Icon name="user-plus" size="md" class="text-[var(--color-accent-primary)]" />
      {/snippet}
      <span class="text-sm font-semibold text-[var(--color-text-primary)]">Create User</span>
      {#snippet actions()}
        <IconButton icon="check" variant="success" onclick={handleCreate} disabled={loading} title="Create user" />
        <IconButton icon="x" onclick={onClose} title="Cancel" />
      {/snippet}
    </DetailHeader>
  {:else if mode === 'edit'}
    <DetailHeader>
      {#snippet leading()}
        <Icon name="edit" size="md" class="text-[var(--color-accent-primary)]" />
      {/snippet}
      <span class="text-sm font-semibold text-[var(--color-text-primary)]">Edit User</span>
      {#snippet actions()}
        <IconButton icon="check" variant="success" onclick={handleSave} disabled={loading} title="Save changes" />
        <IconButton icon="x" onclick={onClose} title="Cancel" />
      {/snippet}
    </DetailHeader>
  {/if}

  <div class="panel-body">
    {#if mode === 'create'}
      <!-- Create Mode -->
      <div class="form-section">
        <Input
          label="Username"
          placeholder="e.g. john.doe"
          bind:value={createForm.username}
          disabled={loading}
          autocomplete="off"
          error={errors.username}
        />
        <Input
          type="email"
          label="Email"
          placeholder="e.g. john@example.com"
          bind:value={createForm.email}
          disabled={loading}
          autocomplete="off"
          error={errors.email}
        />
        <Input
          label="Display Name"
          placeholder="e.g. John Doe"
          bind:value={createForm.displayName}
          disabled={loading}
          autocomplete="off"
          error={errors.displayName}
        />
        <Input
          type="password"
          label="Password"
          placeholder="Min 6 characters"
          bind:value={createForm.password}
          disabled={loading}
          autocomplete="new-password"
          error={errors.password}
        />
      </div>

    {:else if mode === 'edit' && user}
      <!-- Edit Mode -->
      <div class="form-section">
        <Input
          label="Display Name"
          bind:value={editForm.name}
          disabled={loading}
          autocomplete="off"
        />
        <Input
          type="email"
          label="Email"
          bind:value={editForm.email}
          disabled={loading}
          autocomplete="off"
        />
        <Select label="Global Role" bind:value={editForm.globalRole} disabled={loading} options={roleOptions} />
      </div>

    {:else if mode === 'view' && user}
      <!-- View Mode -->
      <div class="user-header">
        <div class="user-avatar">{user.name.charAt(0).toUpperCase()}</div>
        <div class="user-info">
          <h4 class="user-name">{user.name}</h4>
          <p class="user-email">{user.email}</p>
        </div>
        <span class="status-badge" class:active={user.status === 'active'}>
          {user.status}
        </span>
      </div>

      <div class="detail-grid">
        <div class="detail-item">
          <span class="detail-label">Username</span>
          <span class="detail-value">{user.username}</span>
        </div>
        <div class="detail-item">
          <span class="detail-label">Global Role</span>
          <span class="detail-value">
            <span class="role-badge" class:admin={user.globalRole === 'Admin'}>
              {getRoleLabel(user.globalRole)}
            </span>
          </span>
        </div>
        <div class="detail-item">
          <span class="detail-label">Created</span>
          <span class="detail-value">{formatDateStr(user.createdAt)}</span>
        </div>
        <div class="detail-item">
          <span class="detail-label">Last Login</span>
          <span class="detail-value">{formatDateStr(user.lastLoginAt)}</span>
        </div>
      </div>

      {#if user.projects.length > 0}
        <div class="projects-section">
          <h4 class="section-title">Project Memberships</h4>
          <div class="projects-list">
            {#each user.projects as project (project.projectId + '-' + project.role)}
              <div class="project-item">
                <span class="project-name">{project.projectName}</span>
                <span class="project-role">{project.role}</span>
              </div>
            {/each}
          </div>
        </div>
      {:else}
        <div class="projects-section">
          <h4 class="section-title">Project Memberships</h4>
          <p class="empty-text">No project memberships</p>
        </div>
      {/if}

    {/if}
  </div>
</div>

<style>
  @import '../../styles/detail-panel.css';
  @import '../../styles/detail-panel-form.css';

  /* User Header (View Mode) */
  .user-header {
    display: flex;
    align-items: center;
    gap: 0.75rem;
  }

  .user-avatar {
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

  .user-info {
    flex: 1;
    min-width: 0;
  }

  .user-name {
    margin: 0;
    font-size: 1rem;
    font-weight: 600;
    color: var(--color-text-primary, #111);
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
  }

  .user-email {
    margin: 0.125rem 0 0;
    font-size: 0.8125rem;
    color: var(--color-text-secondary, #6b7280);
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
  }

  /* Badges */
  .status-badge {
    padding: 0.125rem 0.5rem;
    border-radius: 4px;
    font-size: 0.6875rem;
    font-weight: 500;
    text-transform: uppercase;
    background: var(--color-bg-tertiary, #f3f4f6);
    color: var(--color-text-secondary, #6b7280);
    flex-shrink: 0;
  }

  .status-badge.active {
    background: var(--color-success-10);
    color: var(--color-success-500);
  }

  .role-badge {
    display: inline-block;
    padding: 0.125rem 0.375rem;
    border-radius: 4px;
    background: var(--color-bg-tertiary, #f3f4f6);
    font-size: 0.8125rem;
    text-transform: capitalize;
  }

  .role-badge.admin {
    background: var(--color-purple-10);
    color: var(--color-purple-500);
  }

  /* Projects */
  .projects-section {
    border-top: 1px solid var(--color-border-primary, #e5e7eb);
    padding-top: 1rem;
  }

  .projects-list {
    display: flex;
    flex-direction: column;
    gap: 0.375rem;
  }

  .project-item {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 0.5rem 0.625rem;
    background: var(--color-bg-secondary, #f9fafb);
    border-radius: 4px;
    font-size: 0.8125rem;
  }

  .project-name {
    color: var(--color-text-primary, #111);
  }

  .project-role {
    font-size: 0.75rem;
    color: var(--color-text-secondary, #6b7280);
  }

  .empty-text {
    margin: 0;
    font-size: 0.8125rem;
    color: var(--color-text-tertiary, #9ca3af);
  }

  /* Action button hover colors */
  :global(.action-danger:hover) {
    color: var(--color-error-500);
    background: var(--color-error-10);
  }

  :global(.action-warn:hover) {
    color: var(--color-orange-500);
    background: var(--color-orange-10);
  }

  :global(.action-success:hover) {
    color: var(--color-success-500);
    background: var(--color-success-10);
  }
</style>
