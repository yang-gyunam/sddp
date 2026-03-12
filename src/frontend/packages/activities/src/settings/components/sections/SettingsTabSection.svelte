<!-- Section: Settings > SettingsTabSection -->
<script lang="ts">
  /**
   * SettingsTabPage - Settings page content (sidebar handled by SidebarPanels).
   * Renders the active settings page based on settingsNavigationStore.
   */
  import { untrack } from 'svelte';
  import { config as appConfig } from '@sddp/shell';
  import ProfileSettingsPage from '../pages/ProfileSettingsPage.svelte';
  import ProjectGeneralSettingsPage from '../pages/ProjectGeneralSettingsPage.svelte';
  import ProjectMembersSettingsPage from '../pages/ProjectMembersSettingsPage.svelte';
  import ProjectRolesSettingsPage from '../pages/ProjectRolesSettingsPage.svelte';
  import ProjectIntegrationsSettingsPage from '../pages/ProjectIntegrationsSettingsPage.svelte';
  import SystemUsersSettingsPage from '../pages/SystemUsersSettingsPage.svelte';
  import SystemProjectsSettingsPage from '../pages/SystemProjectsSettingsPage.svelte';
  import SystemConfigSettingsPage from '../pages/SystemConfigSettingsPage.svelte';
  import SystemAuditLogsPage from '../pages/SystemAuditLogsPage.svelte';
  import SystemHealthPage from '../pages/SystemHealthPage.svelte';
  import { SystemDashboardPage } from '../../../dashboard/components/pages';
  import { settingsNavigationStore, loadSettingsNavigation, setActiveProjectCategory, setActiveCategory } from '../../stores/settingsNavigation.store';

  let activeCategory = $state('profile');
  let selectedProjectId = $state<string | null>(null);
  const projectIntegrationsEnabled = appConfig.get('enableProjectIntegrations');

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

  $effect(() => {
    if (projectIntegrationsEnabled) return;
    if (activeCategory !== 'project:integrations') return;
    if (selectedProjectId) {
      setActiveProjectCategory(selectedProjectId, 'general');
      return;
    }
    setActiveCategory('profile');
  });
</script>

<div class="settings-tab-content">
  {#if activeCategory === 'profile' || activeCategory.startsWith('profile:')}
    <ProfileSettingsPage />
  {:else if activeCategory === 'project:general'}
    <ProjectGeneralSettingsPage projectId={selectedProjectId ?? ''} />
  {:else if activeCategory === 'project:members'}
    <ProjectMembersSettingsPage projectId={selectedProjectId ?? ''} />
  {:else if activeCategory === 'project:roles'}
    <ProjectRolesSettingsPage projectId={selectedProjectId ?? ''} />
  {:else if activeCategory === 'project:integrations'}
    <ProjectIntegrationsSettingsPage projectId={selectedProjectId ?? ''} />
  {:else if activeCategory === 'system:dashboard'}
    <SystemDashboardPage section="system-stats" />
  {:else if activeCategory === 'system:users'}
    <SystemUsersSettingsPage />
  {:else if activeCategory === 'system:projects'}
    <SystemProjectsSettingsPage />
  {:else if activeCategory === 'system:config'}
    <SystemConfigSettingsPage />
  {:else if activeCategory === 'system:audit'}
    <SystemAuditLogsPage />
  {:else if activeCategory === 'system:health'}
    <SystemHealthPage />
  {:else}
    <ProfileSettingsPage />
  {/if}
</div>

<style>
  .settings-tab-content {
    height: 100%;
    overflow-y: auto;
  }
</style>
