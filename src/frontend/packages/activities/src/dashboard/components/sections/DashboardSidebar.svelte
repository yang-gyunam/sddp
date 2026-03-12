<!-- Section: DashboardSidebar -->
<script lang="ts">
  /**
   * Dashboard Sidebar
   * Dashboard type selection
   */

  import { Button, Icon } from '@sddp/ui';
  import { dashboardViewStore, setDashboardView } from '../../stores/dashboardView.store';
  import type { DashboardView } from '../../types';

  interface Props {
    isAdmin?: boolean;
    myProjects?: Array<{ id: string; name: string }>;
    loading?: boolean;
  }
  let { isAdmin = false, myProjects = [], loading = false }: Props = $props();

  let currentView = $state<DashboardView>('my');
  let selectedProjectId = $state<string | null | undefined>(undefined);

  $effect(() => {
    const unsubscribe = dashboardViewStore.subscribe((state) => {
      currentView = state.currentView;
      selectedProjectId = state.selectedProjectId;
    });
    return unsubscribe;
  });

  function selectView(view: DashboardView, projectId?: string) {
    setDashboardView(view, projectId);
  }
</script>

<div class="p-4 h-full overflow-y-auto">
  <div class="mb-6">
    <div class="text-xs font-semibold uppercase tracking-wide text-[var(--color-text-secondary)] mb-2">
      DASHBOARDS
    </div>
    <Button
      variant="unstyled"
      class="w-full flex items-center gap-2 px-2 py-2 text-sm text-left transition-colors rounded border
        {currentView === 'my'
          ? 'bg-[var(--color-accent-primary)]/10 border-[var(--color-accent-primary)]/30 text-[var(--color-text-primary)]'
          : 'border-transparent hover:bg-[var(--color-bg-tertiary)] text-[var(--color-text-secondary)]'}"
      onclick={() => selectView('my')}
    >
      <Icon name="layout-dashboard" size="sm" class="flex-shrink-0" />
      <span class="flex-1">My Dashboard</span>
    </Button>

    {#if isAdmin}
      <Button
        variant="unstyled"
        class="w-full flex items-center gap-2 px-2 py-2 text-sm text-left transition-colors rounded border
          {currentView === 'system'
            ? 'bg-[var(--color-accent-primary)]/10 border-[var(--color-accent-primary)]/30 text-[var(--color-text-primary)]'
            : 'border-transparent hover:bg-[var(--color-bg-tertiary)] text-[var(--color-text-secondary)]'}"
        onclick={() => selectView('system')}
      >
        <Icon name="monitor" size="sm" class="flex-shrink-0" />
        <span class="flex-1">System Overview</span>
      </Button>
    {/if}
  </div>

  <div class="mb-6">
    <div class="text-xs font-semibold uppercase tracking-wide text-[var(--color-text-secondary)] mb-2">
      PROJECTS
    </div>
    <div class="text-sm font-medium text-[var(--color-text-secondary)] my-2">▼ My Projects</div>
    {#if loading}
      <div class="pl-6 pr-2 py-2 text-sm text-[var(--color-text-tertiary)]">
        Loading projects...
      </div>
    {:else if myProjects.length === 0}
      <div class="pl-6 pr-2 py-2 text-sm text-[var(--color-text-tertiary)] italic">
        No projects found
      </div>
    {:else}
      {#each myProjects as project (project.id)}
        <Button
          variant="unstyled"
          class="w-full flex items-center gap-2 pl-6 pr-2 py-2 text-sm text-left transition-colors rounded border
            {currentView === 'project' && selectedProjectId === project.id
              ? 'bg-[var(--color-accent-primary)]/10 border-[var(--color-accent-primary)]/30 text-[var(--color-text-primary)]'
              : 'border-transparent hover:bg-[var(--color-bg-tertiary)] text-[var(--color-text-secondary)]'}"
          onclick={() => selectView('project', project.id)}
        >
          <span class="flex-1">{project.name}</span>
        </Button>
      {/each}
    {/if}
  </div>
</div>
