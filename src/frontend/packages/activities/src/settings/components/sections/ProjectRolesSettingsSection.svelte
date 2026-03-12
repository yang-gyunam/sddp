<!-- Section: Settings > ProjectRolesSettingsSection -->
<script lang="ts">
  /**
   * Project Roles Settings Page
   * Read-only view of system roles and their permission assignments
   */

  import { untrack } from 'svelte';
  import { SingleColumnLayout, PageHeader, PageShell, getTabState, setTabState } from '@sddp/shell';
  import { Icon, IconButton, Button, Spinner } from '@sddp/ui';
  import { getRoleService, type Role } from '../../services/RoleService';
  import { SettingGroupHeader } from '../idioms';

  interface Props {
    tenantId?: string;
    projectId: string;
    tabId?: string;
  }

  let { tenantId = '', projectId: _projectId, tabId = '' }: Props = $props();

  // Permission categories derived from actual permission codes (resource:action)
  const permissionCategories = [
    { resource: 'spec', label: 'Specs', actions: ['create', 'read', 'update', 'delete', 'approve', 'lock'] },
    { resource: 'conversation', label: 'Conversations', actions: ['create', 'read', 'post', 'close'] },
    { resource: 'requirement', label: 'Requirements', actions: ['create', 'read', 'update', 'delete'] },
    { resource: 'glossary', label: 'Glossary', actions: ['create', 'read', 'update', 'deprecate'] },
    { resource: 'task', label: 'Tasks', actions: ['create', 'read', 'update', 'delete'] },
    { resource: 'project', label: 'Projects', actions: ['create', 'read', 'update', 'delete'] },
    { resource: 'generation', label: 'Generation', actions: ['execute', 'rollback'] },
    { resource: 'user', label: 'User Management', actions: ['manage'] },
    { resource: 'role', label: 'Role Management', actions: ['assign'] },
    { resource: 'audit', label: 'Audit', actions: ['read'] },
  ];

  const roleService = getRoleService();

  let roles = $state<Role[]>([]);
  let loading = $state(false);
  let error = $state<string | null>(null);

  // Tab State Persistence
  interface RolesTabState {
    roles: Role[];
  }

  const tabStateKey = $derived(tabId || `settings-project-${_projectId}-roles`);
  let restoredKey = $state<string | null>(null);
  let isRestored = $state(false);

  $effect(() => {
    if (!tabStateKey || restoredKey === tabStateKey) return;
    const saved = getTabState<RolesTabState>(tabStateKey);
    if (saved?.roles && saved.roles.length > 0) {
      roles = saved.roles;
    } else {
      untrack(() => loadRoles());
    }
    restoredKey = tabStateKey;
    isRestored = true;
  });

  $effect(() => {
    if (!tabStateKey || !isRestored) return;
    setTabState<RolesTabState>(tabStateKey, {
      roles: [...roles],
    });
  });

  async function loadRoles() {
    if (!tenantId) return;
    loading = true;
    error = null;

    try {
      roleService.setContext(tenantId);
      roles = await roleService.getRoles();
    } catch {
      error = 'Failed to load roles';
      roles = [];
    } finally {
      loading = false;
    }
  }

  function hasPermission(role: Role, resource: string, action: string): boolean {
    return role.permissions.includes(`${resource}:${action}`);
  }
</script>

<PageShell>
  {#if loading}
    <div class="flex-1 flex items-center justify-center pb-28"><Spinner size="lg" /></div>
  {:else if error}
    <div class="flex-1 flex items-center justify-center pb-28">
      <div class="error-state">
        <p>{error}</p>
        <Button variant="secondary" onclick={loadRoles}>Retry</Button>
      </div>
    </div>
  {:else}
  <PageHeader title="Roles" {loading}>
    {#snippet actions()}
      <IconButton icon="refresh-cw" variant="ghost" size="sm" title="Refresh" onclick={loadRoles} />
    {/snippet}
  </PageHeader>

  <SingleColumnLayout>
    <SettingGroupHeader title="Role-Permission Matrix" />
    <p class="section-desc">
      System-defined roles and their permission assignments.
      <span class="read-only-badge">Read-only</span>
    </p>

    {#if roles.length > 0}
      <div class="table-wrapper">
        <table class="permissions-table">
          <thead>
            <tr>
              <th class="col-permission">Permission</th>
              {#each roles as role (role.id)}
                <th class="col-role">
                  <div class="role-name">{role.name}</div>
                </th>
              {/each}
            </tr>
          </thead>
          <tbody>
            {#each permissionCategories as category (category.resource)}
              <tr class="category-row">
                <td class="category-name" colspan={roles.length + 1}>{category.label}</td>
              </tr>
              {#each category.actions as action (action)}
                <tr>
                  <td class="action-name">{action}</td>
                  {#each roles as role (role.id)}
                    <td class="permission-cell">
                      {#if hasPermission(role, category.resource, action)}
                        <Icon name="check" size="xs" />
                      {:else}
                        <span class="no-permission">&mdash;</span>
                      {/if}
                    </td>
                  {/each}
                </tr>
              {/each}
            {/each}
          </tbody>
        </table>
      </div>
    {:else}
      <p class="empty-state">No roles found.</p>
    {/if}
  </SingleColumnLayout>
  {/if}
</PageShell>

<style>
  .error-state {
    padding: 4rem;
    text-align: center;
    color: var(--color-error-600, #dc2626);
  }

  .error-state p {
    margin-bottom: 1rem;
  }

  .section-desc {
    font-size: 0.8125rem;
    color: var(--color-text-secondary, #6b7280);
    margin: 0 0 1rem;
    display: flex;
    align-items: center;
    gap: 0.5rem;
  }

  .read-only-badge {
    padding: 0.0625rem 0.375rem;
    border-radius: 3px;
    font-size: 0.625rem;
    font-weight: 600;
    text-transform: uppercase;
    background: var(--color-bg-tertiary, #f3f4f6);
    color: var(--color-text-tertiary, #9ca3af);
  }

  .table-wrapper {
    overflow-x: auto;
  }

  .permissions-table {
    width: 100%;
    border-collapse: collapse;
    font-size: 0.75rem;
    table-layout: fixed;
  }

  .permissions-table th,
  .permissions-table td {
    padding: 0.3125rem 0.375rem;
    text-align: left;
    border-bottom: 1px solid var(--color-border-primary, #e5e7eb);
  }

  .col-permission {
    font-weight: 500;
    color: var(--color-text-secondary, #6b7280);
    width: 12.5%;
  }

  .col-role {
    text-align: center;
    width: 12.5%;
  }

  .role-name {
    font-weight: 600;
    font-size: 0.6875rem;
    color: var(--color-text-primary, #111);
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
  }

  .category-row {
    background: var(--color-bg-secondary, #f9fafb);
  }

  .category-name {
    font-weight: 600;
    color: var(--color-text-primary, #111);
    text-transform: uppercase;
    font-size: 0.6875rem;
    letter-spacing: 0.05em;
  }

  .action-name {
    padding-left: 1.25rem;
    color: var(--color-text-secondary, #6b7280);
  }

  .permission-cell {
    text-align: center;
    color: var(--color-success-500, #22c55e);
  }

  .no-permission {
    color: var(--color-text-quaternary, #d1d5db);
    font-size: 0.75rem;
  }

  .empty-state {
    font-size: 0.8125rem;
    color: var(--color-text-tertiary, #9ca3af);
  }

</style>
