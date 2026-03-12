<script lang="ts">
  /**
   * Dashboard Activity
   * Main activity component for dashboard functionality
   */

  import { untrack } from 'svelte';
  import { getAuthState, subscribeAuth, hasRole, toast } from '@sddp/shell';
  import { DashboardSidebar } from './sections';
  import { MyDashboardPage, SystemDashboardPage } from './pages';
  import { subscribeDashboardView, loadDashboardView } from '../stores/dashboardView.store';
  import { getProjectService } from '../../projects/services';
  import type { DashboardView } from '../types';
  import type { Project } from '../../projects/types';

  interface Props {
    activityId?: string;
  }

  let { activityId = 'dashboard' }: Props = $props();

  // Auth state
  let authState = $state(getAuthState());
  let isAdmin = $derived(hasRole('Admin'));
  let tenantId = $derived(authState.user?.tenantId || '');

  // Projects state
  let myProjects = $state<Array<{ id: string; name: string }>>([]);
  let projectsLoading = $state(false);

  // Dashboard view state
  let currentView = $state<DashboardView>('my');

  // Subscribe to auth state changes
  $effect(() => {
    const unsubscribe = subscribeAuth((state) => {
      authState = state;
    });
    return unsubscribe;
  });

  // Subscribe to dashboard view changes
  $effect(() => {
    const unsubscribe = subscribeDashboardView((state) => {
      currentView = state.currentView;
    });
    return unsubscribe;
  });

  // Load projects when tenantId changes
  $effect(() => {
    if (tenantId) {
      untrack(() => loadProjects());
    }
  });

  async function loadProjects() {
    if (!tenantId) return;

    projectsLoading = true;
    try {
      const projectService = getProjectService();
      projectService.setTenantId(tenantId);
      const projects: Project[] = await projectService.getProjects();
      myProjects = projects.map((p) => ({ id: p.id, name: p.name }));
    } catch (err) {
      console.error('Failed to load projects:', err);
      toast.error(err instanceof Error ? err.message : 'Failed to load projects');
      myProjects = [];
    } finally {
      projectsLoading = false;
    }
  }

  $effect(() => {
    untrack(() => loadDashboardView());
  });
</script>

<div class="dashboard-activity" data-activity-id={activityId}>
  <div class="dashboard-sidebar-container">
    <DashboardSidebar {isAdmin} {myProjects} loading={projectsLoading} />
  </div>

  <div class="dashboard-main">
    {#if currentView === 'my'}
      <MyDashboardPage />
    {:else if currentView === 'system'}
      <SystemDashboardPage />
    {:else if currentView === 'project'}
      <div class="placeholder-page">
        <h1>Project Dashboard</h1>
        <p>Select a project from the sidebar to view its dashboard.</p>
      </div>
    {/if}
  </div>
</div>

<style>
  .dashboard-activity {
    display: flex;
    height: 100%;
    width: 100%;
    overflow: hidden;
  }

  .dashboard-sidebar-container {
    width: 250px;
    flex-shrink: 0;
    border-right: 1px solid var(--border-color);
    overflow-y: auto;
  }

  .dashboard-main {
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
