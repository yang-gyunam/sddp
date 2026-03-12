<!-- Section: Settings > SystemHealthSection -->
<script lang="ts">
  /**
   * System Health Page
   * Displays health status of all system services
   */

  import { untrack } from 'svelte';
  import { CardGrid, IconButton, Button, Spinner } from '@sddp/ui';
  import { PageShell, PageHeader, PageBody, formatDateTime, getTabState, setTabState } from '@sddp/shell';
  import { StatCard } from '../../../shared/components/idioms';
  import type { HealthStatus, HealthMetric } from '../../../shared/components/idioms';
  import { getDashboardService } from '../../../dashboard/services/DashboardService';

  interface Props {
    tabId?: string;
  }

  let { tabId = '' }: Props = $props();

  interface ServiceHealth {
    name: string;
    status: string;
    message: string | null;
    responseTimeMs: number | null;
  }

  interface HealthData {
    status: string;
    services: ServiceHealth[];
  }

  interface HealthTabState {
    healthData: HealthData | null;
    lastCheckedAt: string | null;
  }

  const dashboardService = getDashboardService();

  const tabStateKey = $derived(tabId || 'settings-system-health');
  let restoredKey = $state<string | null>(null);
  let isRestored = $state(false);

  let healthData = $state<HealthData | null>(null);
  let loading = $state(false);
  let error = $state<string | null>(null);
  let lastCheckedAt = $state<string | null>(null);

  // Restore tab state or load fresh data
  $effect(() => {
    if (!tabStateKey || restoredKey === tabStateKey) return;
    const saved = getTabState<HealthTabState>(tabStateKey);
    if (saved?.healthData) {
      healthData = saved.healthData;
      lastCheckedAt = saved.lastCheckedAt ?? null;
    } else {
      untrack(() => loadHealth());
    }
    restoredKey = tabStateKey;
    isRestored = true;
  });

  // Save tab state on changes
  $effect(() => {
    if (!tabStateKey || !isRestored) return;
    setTabState<HealthTabState>(tabStateKey, {
      healthData: healthData ? JSON.parse(JSON.stringify(healthData)) : null,
      lastCheckedAt,
    });
  });

  async function loadHealth() {
    loading = true;
    error = null;

    try {
      healthData = await dashboardService.getSystemHealth();
      lastCheckedAt = new Date().toISOString();
    } catch (err) {
      error = err instanceof Error ? err.message : 'Failed to load health data';
      healthData = null;
    } finally {
      loading = false;
    }
  }

  const overallStatus = $derived(healthData?.status?.toLowerCase() ?? 'unknown');

  const overallLabel = $derived(
    overallStatus === 'healthy'
      ? 'All Systems Operational'
      : overallStatus === 'warning'
        ? 'Some Services Degraded'
        : overallStatus === 'unknown'
          ? 'Status Unknown'
          : 'System Issues Detected'
  );

  const overallColor = $derived(
    overallStatus === 'healthy'
      ? 'var(--color-success-500)'
      : overallStatus === 'warning'
        ? 'var(--color-warning-500)'
        : 'var(--color-error-600)'
  );

  function toHealthCards(): Array<{ title: string; status: HealthStatus; metrics: HealthMetric[] }> {
    if (!healthData?.services) return [];
    return healthData.services.map((svc) => ({
      title: svc.name,
      status: (
        svc.status.toLowerCase() === 'healthy'
          ? 'healthy'
          : svc.status.toLowerCase() === 'warning'
            ? 'warning'
            : 'error'
      ) as HealthStatus,
      metrics: [
        { label: 'Status', value: svc.status },
        ...(svc.responseTimeMs !== null ? [{ label: 'Response Time', value: `${svc.responseTimeMs}ms` }] : []),
        ...(svc.message ? [{ label: 'Message', value: svc.message }] : []),
      ],
    }));
  }

  function formatCheckedTime(iso: string): string {
    return formatDateTime(iso, { month: 'short', second: '2-digit' });
  }
</script>

<PageShell>
  {#if loading && !healthData}
    <div class="flex-1 flex items-center justify-center pb-28"><Spinner size="lg" /></div>
  {:else if error}
    <div class="flex-1 flex items-center justify-center pb-28">
      <div class="p-12 text-center text-sm text-[var(--color-error-600)]">
        <p class="mb-4">{error}</p>
        <Button variant="secondary" onclick={loadHealth}>Retry</Button>
      </div>
    </div>
  {:else}
  <PageHeader title="Health" {loading}>
    {#snippet actions()}
      {#if healthData}
        <div
          class="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-md text-xs font-semibold"
          style="background: {overallColor}15; color: {overallColor}"
        >
          <span class="text-sm leading-none">
            {#if overallStatus === 'healthy'}&#x2713;{:else if overallStatus === 'warning'}&#x26A0;{:else}&#x2717;{/if}
          </span>
          <span>{overallLabel}</span>
        </div>
      {/if}
      {#if lastCheckedAt}
        <span class="text-xs text-[var(--color-text-tertiary)]">
          {formatCheckedTime(lastCheckedAt)}
        </span>
      {/if}
      <IconButton icon="refresh-cw" title="Refresh" onclick={loadHealth} />
    {/snippet}
  </PageHeader>

  <PageBody>
      <!-- Service Health Cards -->
      <CardGrid cols={4} gap="md" class="mx-3 mt-2 mb-8">
        {#each toHealthCards() as card (card.title)}
          <StatCard title={card.title} status={card.status} metrics={card.metrics} />
        {:else}
          <div class="col-span-4 p-8 text-center text-sm text-[var(--color-text-tertiary)]">
            No health data available
          </div>
        {/each}
      </CardGrid>
  </PageBody>
  {/if}
</PageShell>
