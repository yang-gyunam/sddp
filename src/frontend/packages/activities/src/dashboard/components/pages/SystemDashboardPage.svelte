<!-- Activity: Settings > Nav: System Dashboard (settings-system-dashboard) -->
<script lang="ts">
  /**
   * System Dashboard Page
   * System overview for Admin only - with section-based content
   * Uses split API endpoints for each section
   */

  import { untrack } from 'svelte';
  import { IconButton, CardGrid, Button, Select, DatePicker } from '@sddp/ui';
  import { ENTITY_SVG_ICONS, PageShell, PageHeader, PageBody, formatDateTime, toast, getTabState, setTabState } from '@sddp/shell';
  import { ActivityLog } from '../sections';
  import { AuditLogItem } from '../idioms';
  import { LineChart, BarChart } from '../idioms';
  import { StatCard } from '../../../shared/components/idioms';
  import type { HealthMetric, HealthStatus } from '../../../shared/components/idioms';
  import { getDashboardService } from '../../services';
  import {
    subscribeDashboard,
    setSystemDashboard,
    setDashboardLoading,
    setDashboardError,
  } from '../../stores/dashboard.store';
  import type { SystemDashboard, SystemStatistics, ActivityLogEntry as ActivityLogType } from '../../types';

  type SystemSection = 'system-stats' | 'audit-logs' | 'health';

  interface Props {
    section?: SystemSection;
    tabId?: string;
  }

  let { section: initialSection = 'system-stats', tabId = '' }: Props = $props();
  let section = $state<SystemSection>('system-stats');

  // Tab State Persistence
  interface SystemDashboardTabState {
    section: SystemSection;
    auditTypeFilter: string;
    auditUserFilter: string;
  }

  const tabStateKey = $derived(tabId || 'system-dashboard');
  let restoredKey = $state<string | null>(null);
  let isRestored = $state(false);

  $effect(() => {
    if (!tabStateKey || restoredKey === tabStateKey) return;
    const saved = getTabState<SystemDashboardTabState>(tabStateKey);
    if (saved) {
      section = saved.section ?? initialSection;
      auditTypeFilter = saved.auditTypeFilter ?? '';
      auditUserFilter = saved.auditUserFilter ?? '';
    } else {
      section = initialSection;
    }
    restoredKey = tabStateKey;
    isRestored = true;
  });

  $effect(() => {
    if (!tabStateKey || !isRestored) return;
    setTabState<SystemDashboardTabState>(tabStateKey, {
      section,
      auditTypeFilter,
      auditUserFilter,
    });
  });

  const dashboardService = getDashboardService();

  let systemDashboard = $state<SystemDashboard | null>(null);
  let loading = $state(false);
  let error = $state<string | null>(null);

  // Section-specific data
  let systemStats = $state<SystemStatistics | null>(null);
  let auditLogs = $state<ActivityLogType[]>([]);
  let auditLogsTotal = $state(0);
  let healthData = $state<{ status: string; services: Array<{ name: string; status: string; message: string | null; responseTimeMs: number | null }> } | null>(null);

  // Track which sections have been loaded (cache)
  let loadedSections = $state<Set<SystemSection>>(new Set());

  $effect(() => {
    const unsubscribe = subscribeDashboard((state) => {
      systemDashboard = state.systemDashboard;
      loading = state.loading;
      error = state.error;
    });
    return unsubscribe;
  });

  $effect(() => {
    // Only load if section hasn't been loaded yet
    if (!loadedSections.has(section)) {
      untrack(() => loadSectionData(section));
    }
  });

  async function loadSectionData(currentSection: SystemSection, forceRefresh = false) {
    // Skip if already loaded and not forcing refresh
    if (!forceRefresh && loadedSections.has(currentSection)) {
      return;
    }

    try {
      setDashboardLoading(true);
      setDashboardError(null);

      switch (currentSection) {
        case 'system-stats': {
          // Use split endpoint for system stats
          systemStats = await dashboardService.getSystemStats();
          // Get chart data from full dashboard (reuse if already loaded)
          if (!systemDashboard) {
            const fullData = await dashboardService.getSystemDashboard();
            setSystemDashboard(fullData);
          }
          break;
        }

        case 'audit-logs': {
          // Use split endpoint for audit logs
          const logsResult = await dashboardService.getSystemAuditLogs(1, 50);
          auditLogs = logsResult.logs;
          auditLogsTotal = logsResult.totalCount;
          break;
        }

        case 'health': {
          // Use split endpoint for health check
          healthData = await dashboardService.getSystemHealth();
          break;
        }
      }

      // Mark section as loaded
      loadedSections = new Set([...loadedSections, currentSection]);
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to load dashboard';
      setDashboardError(message);
      toast.error(message);
      console.error('Failed to load System Dashboard section:', err);
    } finally {
      setDashboardLoading(false);
    }
  }

  async function handleRefresh() {
    // Force refresh current section
    await loadSectionData(section, true);
  }

  const periodOptions = [
    { label: 'All Types', value: '' },
    { label: 'Login', value: 'login' },
    { label: 'Create', value: 'create' },
    { label: 'Update', value: 'update' },
    { label: 'Delete', value: 'delete' },
  ];

  const timeRangeOptions = [
    { label: 'All Users', value: '' },
    { label: 'Admin', value: 'admin' },
    { label: 'john.kim', value: 'john.kim' },
    { label: 'sarah.lee', value: 'sarah.lee' },
  ];

  let auditTypeFilter = $state('');
  let auditUserFilter = $state('');
  let auditDateFilter = $state<import('@internationalized/date').DateValue | undefined>(undefined);

  const sectionTitles: Record<SystemSection, string> = {
    'system-stats': 'Dashboard',
    'audit-logs': 'Audit Logs',
    health: 'Health',
  };

  // Transform health API data to StatCard health format
  function getHealthServices(): Array<{
    title: string;
    status: HealthStatus;
    metrics: HealthMetric[];
  }> {
    if (!healthData?.services) {
      return [];
    }
    return healthData.services.map((service) => ({
      title: service.name,
      status: (service.status.toLowerCase() === 'healthy' ? 'healthy' : service.status.toLowerCase() === 'warning' ? 'warning' : 'error') as HealthStatus,
      metrics: [
        { label: 'Status', value: service.status },
        ...(service.responseTimeMs !== null ? [{ label: 'Response Time', value: `${service.responseTimeMs}ms` }] : []),
        ...(service.message ? [{ label: 'Message', value: service.message }] : []),
      ],
    }));
  }

  // Transform audit log data to AuditLogItem format
  function getAuditLogActionType(action: string): 'login' | 'create' | 'update' | 'delete' {
    const lowerAction = action.toLowerCase();
    if (lowerAction.includes('login') || lowerAction.includes('auth')) return 'login';
    if (lowerAction.includes('create') || lowerAction.includes('add')) return 'create';
    if (lowerAction.includes('delete') || lowerAction.includes('remove')) return 'delete';
    return 'update';
  }
</script>

<PageShell>
  <PageHeader title={sectionTitles[section]} {loading}>
    {#snippet actions()}
      <IconButton icon="refresh-cw" size="sm" variant="ghost" title="Refresh" onclick={handleRefresh} />
    {/snippet}
  </PageHeader>
  <PageBody>
    {#if loading}
      <div class="loading-state">Loading...</div>
    {:else if error}
      <div class="error-state">{error}</div>
    {:else if section === 'system-stats'}
    <!-- System Stats Section -->
    {#if systemStats || systemDashboard}
      {@const stats = systemStats || systemDashboard?.statistics}
      <CardGrid cols={4} gap="md" class="mx-3 mt-2 mb-8">
        <StatCard
          title="Projects"
          value={stats?.projects.total ?? 0}
          subtitle="+{stats?.projects.thisMonth ?? 0} this month"
          icon={ENTITY_SVG_ICONS.projects}
        />
        <StatCard
          title="Users"
          value={stats?.users.total ?? 0}
          subtitle="+{stats?.users.thisMonth ?? 0} this month"
          icon={ENTITY_SVG_ICONS.users}
        />
        <StatCard
          title="Specs"
          value={stats?.specs.total ?? 0}
          subtitle="+{stats?.specs.thisWeek ?? 0} this week"
          icon={ENTITY_SVG_ICONS.specs}
        />
        <StatCard
          title="Tasks"
          value={stats?.tasks.total ?? 0}
          subtitle="+{stats?.tasks.thisWeek ?? 0} this week"
          icon={ENTITY_SVG_ICONS.tasks}
        />
        <StatCard
          title="Conversations"
          value={stats?.conversations.total ?? 0}
          subtitle="+{stats?.conversations.thisWeek ?? 0} this week"
          icon={ENTITY_SVG_ICONS.conversations}
        />
      </CardGrid>

      {#if systemDashboard}
        <div class="chart-section mx-2 my-2">
          <LineChart data={systemDashboard.projectActivity} title="Project Activity (Last 30 days)" />
        </div>

        <div class="two-column-grid mx-2 my-2">
          <div class="card-section">
            <h2>Top Projects by Activity</h2>
            <ul class="project-list">
              {#each systemDashboard.topProjects as project, index (project.projectId)}
                <li class="project-item">
                  <span class="project-rank">{index + 1}.</span>
                  <span class="project-name">{project.projectName}</span>
                  <span class="project-count">({project.activityCount})</span>
                </li>
              {/each}
            </ul>
          </div>

          <div class="card-section">
            <h2>Recent System Activities</h2>
            <ActivityLog activities={systemDashboard.recentActivities} />
          </div>
        </div>

        <div class="chart-section mx-2 my-2">
          <BarChart data={systemDashboard.userDistribution} title="User Activity Distribution" />
        </div>
      {/if}
    {:else}
      <div class="empty-state">No system data available</div>
    {/if}

  {:else if section === 'audit-logs'}
    <!-- Audit Logs Section -->
    <div class="section-content mx-2 my-2">
      <div class="filter-bar">
        <Select options={periodOptions} bind:value={auditTypeFilter} placeholder="All Types" />
        <Select options={timeRangeOptions} bind:value={auditUserFilter} placeholder="All Users" />
        <DatePicker bind:value={auditDateFilter} />
      </div>

      <div class="audit-log-list">
        {#if auditLogs.length > 0}
          {#each auditLogs as log (log.id)}
            <AuditLogItem
              actionType={getAuditLogActionType(log.action)}
              user={log.userName}
              action={log.action}
              meta="{log.entityType} • {formatDateTime(log.timestamp)}"
            />
          {/each}
        {:else}
          <div class="empty-state">No audit logs available</div>
        {/if}
      </div>

      <div class="pagination">
        <Button variant="ghost" disabled>Previous</Button>
        <span>Page 1 of {Math.ceil(auditLogsTotal / 50) || 1}</span>
        <Button variant="ghost" disabled={auditLogsTotal <= 50}>Next</Button>
      </div>
    </div>

  {:else if section === 'health'}
    <!-- Health Check Section -->
    {@const overallStatus = healthData?.status?.toLowerCase() ?? 'unknown'}
    <div class="section-content mx-2 my-2">
      <div class="health-overview">
        <div class="health-status {overallStatus === 'healthy' ? 'healthy' : overallStatus === 'warning' ? 'warning' : 'error'}">
          <span class="status-icon codicon codicon-{overallStatus === 'healthy' ? 'pass-filled' : overallStatus === 'warning' ? 'warning' : 'error'}"></span>
          <span class="status-text">
            {#if overallStatus === 'healthy'}
              All Systems Operational
            {:else if overallStatus === 'warning'}
              Some Services Degraded
            {:else}
              System Issues Detected
            {/if}
          </span>
        </div>
        <div class="last-checked">Last checked: just now</div>
      </div>

      <CardGrid cols={4} gap="md">
        {#each getHealthServices() as service (service.title)}
          <StatCard
            title={service.title}
            status={service.status}
            metrics={service.metrics}
          />
        {:else}
          <div class="empty-state">No health data available</div>
        {/each}
      </CardGrid>
    </div>
  {/if}
  </PageBody>
</PageShell>

<style>
  .loading-state,
  .error-state,
  .empty-state {
    padding: 4rem;
    text-align: center;
    color: var(--color-text-secondary);
  }

  .error-state {
    color: var(--color-danger);
  }

  .chart-section {
    margin-bottom: 2rem;
  }

  .two-column-grid {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
    gap: 1.5rem;
    margin-bottom: 2rem;
  }

  .card-section {
    padding: 1.5rem;
    background: var(--color-bg-secondary);
    border: 1px solid var(--color-border);
    border-radius: 8px;
  }

  .card-section h2 {
    margin: 0 0 1rem;
    font-size: 1.125rem;
    font-weight: 600;
  }

  .project-list {
    list-style: none;
    padding: 0;
    margin: 0;
  }

  .project-item {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    padding: 0.75rem 0;
    border-bottom: 1px solid var(--color-border);
  }

  .project-item:last-child {
    border-bottom: none;
  }

  .project-rank {
    font-weight: 600;
    color: var(--color-text-secondary);
    min-width: 1.5rem;
  }

  .project-name {
    flex: 1;
    color: var(--color-text-primary);
  }

  .project-count {
    color: var(--color-text-tertiary);
    font-size: 0.875rem;
  }

  /* Section Content */
  .section-content {
    padding: 0;
  }

  /* Filter Bar */
  .filter-bar {
    display: flex;
    gap: 1rem;
    margin-bottom: 1.5rem;
    flex-wrap: wrap;
  }

  /* Audit Log List */
  .audit-log-list {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
    margin-bottom: 1.5rem;
  }

  /* Pagination */
  .pagination {
    display: flex;
    justify-content: center;
    align-items: center;
    gap: 1rem;
  }

  .pagination span {
    font-size: 0.875rem;
    color: var(--color-text-secondary);
  }

  /* Health Section */
  .health-overview {
    text-align: center;
    margin-bottom: 2rem;
  }

  .health-status {
    display: inline-flex;
    align-items: center;
    gap: 0.5rem;
    padding: 1rem 2rem;
    border-radius: 8px;
    font-size: 1.25rem;
    font-weight: 600;
  }

  .health-status.healthy {
    background: color-mix(in srgb, var(--color-success-500) 10%, transparent);
    color: var(--color-success-500);
  }

  .health-status.warning {
    background: color-mix(in srgb, var(--color-warning-500) 10%, transparent);
    color: var(--color-warning-500);
  }

  .health-status.error {
    background: color-mix(in srgb, var(--color-danger-500) 10%, transparent);
    color: var(--color-danger-500);
  }

  .last-checked {
    margin-top: 0.5rem;
    font-size: 0.875rem;
    color: var(--color-text-tertiary);
  }

</style>
