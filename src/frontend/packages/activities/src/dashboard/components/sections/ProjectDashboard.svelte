<!-- Section: ProjectDashboard -->
<!--
  ProjectDashboard Section Component
  Renders project-level dashboard with statistics, team, and activity
-->
<script lang="ts">
  import { Button } from '@sddp/ui';
  import { ActivityLog } from '../sections';
  import { TeamMemberList } from '../sections';
  import { StatCard } from '../../../shared/components/idioms';
  import { ProgressBar } from '../idioms';
  import { PieChart } from '../idioms';
  import { getDashboardService } from '../../services';
  import type * as DashboardTypes from '../../types';

  interface Props {
    projectId: string;
    class?: string;
  }

  let { projectId, class: className = '' }: Props = $props();

  const dashboardService = getDashboardService();

  let dashboard: DashboardTypes.ProjectDashboard | null = $state(null);
  let loading = $state(true);
  let error = $state<string | null>(null);

  async function loadDashboard() {
    loading = true;
    error = null;
    try {
      dashboard = await dashboardService.getProjectDashboard(projectId);
    } catch (e) {
      error = e instanceof Error ? e.message : 'Failed to load dashboard';
    } finally {
      loading = false;
    }
  }

  let prevProjectId = $state<string | null>(null);
  $effect(() => {
    if (!projectId) {
      prevProjectId = null;
      return;
    }
    if (projectId === prevProjectId) return;
    prevProjectId = projectId;
    loadDashboard();
  });

  const taskCompletionPercent = $derived.by((): number => {
    const d = dashboard;
    if (!d?.taskProgress) return 0;
    return Math.round((d.taskProgress.done / Math.max(d.taskProgress.total, 1)) * 100);
  });

  const requirementStatusEntries = $derived.by((): [string, number][] => {
    const d = dashboard;
    if (!d?.requirementStatus) return [];
    return Object.entries(d.requirementStatus) as [string, number][];
  });
</script>

<div class="project-dashboard {className}">
  {#if loading}
    <div class="loading-state">
      <p class="text-secondary">Loading project dashboard...</p>
    </div>
  {:else if error}
    <div class="error-state">
      <p class="text-danger">{error}</p>
      <Button variant="secondary" onclick={loadDashboard}>Retry</Button>
    </div>
  {:else if dashboard}
    <!-- Project Header -->
    <div class="project-header">
      <div class="project-info">
        <h2 class="project-name">{dashboard.project.name}</h2>
        <span class="project-status" class:active={dashboard.project.status === 'Active'}>
          {dashboard.project.status}
        </span>
      </div>
      <div class="project-meta">
        <span class="meta-item">Owner: {dashboard.project.ownerName}</span>
        <span class="meta-item">Members: {dashboard.project.memberCount}</span>
      </div>
    </div>

    <!-- Statistics Grid -->
    <div class="statistics-grid">
      <StatCard
        title="Conversations"
        value={dashboard.statistics.conversations.total}
        subtitle="{dashboard.statistics.conversations.active} active"
      />
      <StatCard
        title="Requirements"
        value={dashboard.statistics.requirements.total}
        subtitle="{dashboard.statistics.requirements.draft} draft"
      />
      <StatCard
        title="Specs"
        value={dashboard.statistics.specs.total}
        subtitle="{dashboard.statistics.specs.inReview} in review"
      />
      <StatCard
        title="Tasks"
        value={dashboard.statistics.tasks.total}
        subtitle="{dashboard.statistics.tasks.inProgress} in progress"
      />
      <StatCard
        title="Artifacts"
        value={dashboard.statistics.artifacts.total}
        subtitle="{dashboard.statistics.artifacts.recent} recent"
      />
    </div>

    <!-- Task Progress -->
    {#if dashboard.taskProgress}
      <div class="section">
        <h3 class="section-title">Task Progress</h3>
        <div class="task-progress">
          <ProgressBar value={taskCompletionPercent} max={100} />
          <div class="task-counts">
            <span class="count-item">To Do: {dashboard.taskProgress.toDo}</span>
            <span class="count-item">In Progress: {dashboard.taskProgress.inProgress}</span>
            <span class="count-item">Done: {dashboard.taskProgress.done}</span>
          </div>
        </div>
      </div>
    {/if}

    <!-- Requirement Status Distribution -->
    {#if requirementStatusEntries.length > 0}
      <div class="section">
        <h3 class="section-title">Requirement Status</h3>
        <PieChart
          data={requirementStatusEntries.map(([label, value]) => ({ label, value }))}
        />
      </div>
    {/if}

    <!-- Team Members -->
    {#if dashboard.teamMembers.length > 0}
      <div class="section">
        <h3 class="section-title">Team Activity</h3>
        <TeamMemberList members={dashboard.teamMembers} />
      </div>
    {/if}

    <!-- Recent Activities -->
    {#if dashboard.recentActivities.length > 0}
      <div class="section">
        <h3 class="section-title">Recent Activity</h3>
        <ActivityLog activities={dashboard.recentActivities} />
      </div>
    {/if}
  {/if}
</div>

<style>
  .project-dashboard {
    display: flex;
    flex-direction: column;
    gap: 1.5rem;
    padding: 1.5rem;
  }

  .loading-state,
  .error-state {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    min-height: 200px;
    gap: 1rem;
  }

  .text-secondary {
    color: var(--text-secondary);
  }

  .text-danger {
    color: var(--color-danger, #ef4444);
  }

  .project-header {
    display: flex;
    justify-content: space-between;
    align-items: flex-start;
    flex-wrap: wrap;
    gap: 1rem;
  }

  .project-info {
    display: flex;
    align-items: center;
    gap: 0.75rem;
  }

  .project-name {
    font-size: 1.5rem;
    font-weight: 700;
    color: var(--text-primary);
    margin: 0;
  }

  .project-status {
    padding: 0.25rem 0.75rem;
    border-radius: 9999px;
    font-size: 0.75rem;
    font-weight: 600;
    background: var(--bg-tertiary);
    color: var(--text-secondary);
  }

  .project-status.active {
    background: var(--color-success-bg, #ecfdf5);
    color: var(--color-success, #059669);
  }

  .project-meta {
    display: flex;
    gap: 1rem;
  }

  .meta-item {
    font-size: 0.875rem;
    color: var(--text-tertiary);
  }

  .statistics-grid {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(180px, 1fr));
    gap: 1rem;
  }

  .section {
    display: flex;
    flex-direction: column;
    gap: 0.75rem;
  }

  .section-title {
    font-size: 0.875rem;
    font-weight: 600;
    color: var(--text-secondary);
    text-transform: uppercase;
    letter-spacing: 0.05em;
    margin: 0;
  }

  .task-progress {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
  }

  .task-counts {
    display: flex;
    gap: 1rem;
    flex-wrap: wrap;
  }

  .count-item {
    font-size: 0.8125rem;
    color: var(--text-tertiary);
  }
</style>
