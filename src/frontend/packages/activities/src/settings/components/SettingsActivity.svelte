<script lang="ts">
  /**
   * Settings Activity
   * Main activity component for settings functionality
   */

  import { untrack } from 'svelte';
  import { config as appConfig, getAuthState, subscribeAuth, hasRole, output } from '@sddp/shell';
  import { SettingsSidebar } from './sections';
  import {
    // User Settings
    ProfileSettingsPage,
    // Project Settings
    ProjectGeneralSettingsPage,
    ProjectMembersSettingsPage,
    ProjectRolesSettingsPage,
    ProjectIntegrationsSettingsPage,
    // System Settings
    SystemUsersSettingsPage,
    SystemProjectsSettingsPage,
    SystemConfigSettingsPage,
    SystemAuditLogsPage,
    SystemHealthPage,
  } from './pages';
  import { SystemDashboardPage } from '../../dashboard/components/pages';
  import { settingsNavigationStore, loadSettingsNavigation, setActiveCategory, setActiveProjectCategory } from '../stores/settingsNavigation.store';
  import { getProjects } from '../../projects';
  import { canAccessSettingsProject, getVisibleSettingsProjects } from '../utils';

  interface Props {
    activityId?: string;
  }

  let { activityId = 'settings' }: Props = $props();

  // Get tenantId from auth store
  let tenantId = $state(getAuthState().user?.tenantId || '');
  let currentUserId = $state(getAuthState().user?.id || '');

  $effect(() => {
    const unsubscribe = subscribeAuth((state) => {
      tenantId = state.user?.tenantId || '';
      currentUserId = state.user?.id || '';
    });
    return unsubscribe;
  });

  // Check admin role from auth store
  const isAdmin = $derived(hasRole('Admin'));
  const isProductOwner = $derived(
    hasRole('ProductOwner') || hasRole('PRODUCT_OWNER')
  );

  // Load owned projects from API
  let ownedProjects = $state<Array<{ id: string; name: string }>>([]);
  let ownedProjectsLoaded = $state(false);

  // Load owned projects (prevLoadKey guard)
  let prevLoadKey = $state<string | null>(null);
  $effect(() => {
    if (!tenantId) {
      ownedProjects = [];
      ownedProjectsLoaded = true;
      prevLoadKey = null;
      return;
    }

    const loadKey = `${tenantId}:${currentUserId}:${isAdmin ? 'admin' : 'user'}:${isProductOwner ? 'po' : 'non-po'}`;
    if (loadKey === prevLoadKey) return;
    prevLoadKey = loadKey;
    ownedProjectsLoaded = false;
    untrack(() => loadOwnedProjects());
  });

  async function loadOwnedProjects() {
    try {
      const projects = await getProjects(tenantId);
      ownedProjects = getVisibleSettingsProjects({
        projects,
        currentUserId,
        isAdmin,
        isProductOwner,
      });
    } catch {
      output.error('Settings', 'Failed to load projects');
      ownedProjects = [];
    } finally {
      ownedProjectsLoaded = true;
    }
  }

  let activeCategory = $state('profile');
  let selectedProjectId = $state<string | null>(null);

  $effect(() => {
    const unsubscribe = settingsNavigationStore.subscribe((state) => {
      activeCategory = state.activeCategory;
      selectedProjectId = state.selectedProjectId;
    });
    return unsubscribe;
  });

  $effect(() => {
    untrack(() => loadSettingsNavigation());
  });

  // Parse category to determine section and sub-category
  const categoryParts = $derived(activeCategory.split(':'));
  const section = $derived(categoryParts[0]);
  const subCategory = $derived(categoryParts[1] || '');
  const projectIntegrationsEnabled = appConfig.get('enableProjectIntegrations');

  // Guard against stale or unauthorized project selection from persisted navigation state.
  $effect(() => {
    if (!ownedProjectsLoaded) return;
    if (section !== 'project') return;
    if (canAccessSettingsProject(ownedProjects, selectedProjectId)) return;
    setActiveCategory('profile');
  });

  $effect(() => {
    if (projectIntegrationsEnabled) return;
    if (section !== 'project' || subCategory !== 'integrations') return;
    if (selectedProjectId) {
      setActiveProjectCategory(selectedProjectId, 'general');
      return;
    }
    setActiveCategory('profile');
  });
</script>

<div class="settings-activity" data-activity-id={activityId}>
  <div class="settings-sidebar-container">
    <SettingsSidebar {isAdmin} {ownedProjects} />
  </div>

  <div class="settings-main">
    <!-- User Settings -->
    {#if activeCategory === 'profile' || activeCategory.startsWith('profile:')}
      <ProfileSettingsPage />

    <!-- Project Settings -->
    {:else if section === 'project' && selectedProjectId}
      {#if subCategory === 'general'}
        <ProjectGeneralSettingsPage {tenantId} projectId={selectedProjectId} />
      {:else if subCategory === 'members'}
        <ProjectMembersSettingsPage {tenantId} projectId={selectedProjectId} projectName="Project Alpha" />
      {:else if subCategory === 'roles'}
        <ProjectRolesSettingsPage {tenantId} projectId={selectedProjectId} />
      {:else if subCategory === 'integrations'}
        <ProjectIntegrationsSettingsPage {tenantId} projectId={selectedProjectId} />
      {:else}
        <ProjectGeneralSettingsPage {tenantId} projectId={selectedProjectId} />
      {/if}

    <!-- System Settings (Admin) -->
    {:else if activeCategory === 'system:users'}
      <SystemUsersSettingsPage {tenantId} />
    {:else if activeCategory === 'system:projects'}
      <SystemProjectsSettingsPage {tenantId} />
    {:else if activeCategory === 'system:config'}
      <SystemConfigSettingsPage />
    {:else if activeCategory === 'system:audit'}
      <SystemAuditLogsPage {tenantId} />
    {:else if activeCategory === 'system:health'}
      <SystemHealthPage />
    {:else if activeCategory === 'system:dashboard'}
      <SystemDashboardPage section="system-stats" />

    <!-- Fallback -->
    {:else}
      <div class="placeholder-page">
        <h1>Settings</h1>
        <p>Category: {activeCategory} - Select a category from the sidebar</p>
      </div>
    {/if}
  </div>
</div>

<style>
  .settings-activity {
    display: flex;
    height: 100%;
    width: 100%;
    overflow: hidden;
  }

  .settings-sidebar-container {
    width: 250px;
    flex-shrink: 0;
    border-right: 1px solid var(--border-color);
    overflow-y: auto;
  }

  .settings-main {
    flex: 1;
    overflow-y: auto;
  }

  .placeholder-page {
    padding: 2rem;
  }

  .placeholder-page h1 {
    margin: 0 0 1rem;
    font-size: 1.5rem;
  }

  .placeholder-page p {
    color: var(--text-tertiary);
    font-style: italic;
  }
</style>
