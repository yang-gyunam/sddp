<script lang="ts">
  import { untrack } from 'svelte';
  import { Button, Icon, IconButton, Spinner, TabBar } from '@sddp/ui';
  import PageShell from './PageShell.svelte';
  import PageHeader from './PageHeader.svelte';
  import PageBody from './PageBody.svelte';
  import StatusTimeline from '../idioms/StatusTimeline.svelte';
  import StatusOverview from '../idioms/StatusOverview.svelte';
  import { formatDateTime } from '../../utils/datetime.utils';
  import { needsAttention, presentation, statusHistoryLimit } from '../../utils/status-monitor';
  import type { StatusReport, StatusObservation } from '../../types/status-monitor.types';
  let {
    title,
    read,
    clearHistoryOnError = () => false,
    icons = {},
    groupOrder = [],
    itemLabel = 'Service',
    itemsLabel = 'Services',
    sourceKey = '',
  }: {
    title: string;
    read: (signal: AbortSignal) => Promise<StatusReport>;
    clearHistoryOnError?: (error: unknown) => boolean;
    icons?: Record<string, string>;
    groupOrder?: string[];
    itemLabel?: string;
    itemsLabel?: string;
    sourceKey?: string;
  } = $props();
  let view = $state<'overview' | 'timeline'>('overview');
  let observations = $state<StatusObservation[]>([]);
  let attentionOnly = $state(false);

  function observe(at: string, report: StatusReport | null) {
    const last = observations.at(-1);
    // Failures use the browser clock. A recovered API can have measured its
    // report before the locally observed failure; keep the event in observation order.
    if (last && Date.parse(at) < Date.parse(last.at)) {
      at = new Date(Math.max(Date.now(), Date.parse(last.at))).toISOString();
    }
    observations = [...observations.slice(1 - statusHistoryLimit), { at, report }];
  }

  function time(iso: string) {
    return formatDateTime(iso, { month: 'short', second: '2-digit' });
  }

  let report = $state<StatusReport | null>(null);
  let samples = $state<StatusReport[]>([]);
  let loading = $state(true);
  let failed = $state(false);
  let controller: AbortController | undefined;
  const rows = $derived(report?.services ?? []);
  const issues = $derived(rows.filter(needsAttention));
  const overall = $derived(failed || !report ? 'Unknown' : report.status);
  const banner = $derived(
    !report && loading
      ? 'Checking ' + itemsLabel.toLowerCase() + '…'
      : failed
        ? 'Current status unavailable'
        : overall === 'Healthy'
          ? 'All monitored ' + itemsLabel.toLowerCase() + ' operational'
          : overall === 'Unhealthy'
            ? 'Core ' + itemLabel.toLowerCase() + ' unavailable'
            : overall === 'Warning'
              ? 'Some ' + itemsLabel.toLowerCase() + ' need attention'
              : 'Status incomplete'
  );
  const subtitle = $derived(
    failed
      ? report
        ? 'Showing the last successful report.'
        : 'The status report could not be retrieved.'
      : !report
        ? 'Waiting for the first report.'
        : issues.length
          ? issues
              .slice(0, 2)
              .map((s) => (s.message ? s.name + ' · ' + s.message : s.name))
              .join(' / ') + (issues.length > 2 ? ' +' + (issues.length - 2) : '')
          : rows.filter((r) => r.status === 'Healthy').length +
            ' operational · ' +
            rows.length +
            ' ' +
            itemsLabel.toLowerCase() +
            ' listed'
  );

  async function refresh(loader = read) {
    if (controller) return;
    const request = new AbortController();
    controller = request;
    loading = true;
    try {
      const result = await loader(request.signal);
      if (request.signal.aborted) return;
      const measured = Date.parse(result.checkedAt);
      if (!Number.isFinite(measured)) throw new Error('Invalid measurement time');
      const latest = report ? Date.parse(report.checkedAt) : null;
      // Different API instances can serve independently cached reports.
      if (latest !== null && measured < latest) return;
      if (latest !== measured) {
        samples = [...samples.slice(1 - statusHistoryLimit), result];
        observe(result.checkedAt, result);
        report = result;
      } else if (failed) {
        // The report endpoint is reachable again, though its cached measurement is unchanged.
        observe(new Date().toISOString(), report);
      }
      failed = false;
    } catch (err) {
      if (request.signal.aborted) return;
      if (clearHistoryOnError(err)) {
        report = null;
        samples = [];
        observations = [];
      } else {
        observe(new Date().toISOString(), null);
      }
      failed = true;
    } finally {
      if (!request.signal.aborted) loading = false;
      if (controller === request) controller = undefined;
    }
  }

  function startMonitoring(loader: typeof read) {
    report = null;
    samples = [];
    observations = [];
    attentionOnly = false;
    failed = false;
    loading = true;
    const checkVisible = () => {
      if (document.visibilityState === 'visible') void refresh(loader);
    };
    checkVisible();
    const timer = window.setInterval(checkVisible, 60000);
    document.addEventListener('visibilitychange', checkVisible);
    return () => {
      window.clearInterval(timer);
      document.removeEventListener('visibilitychange', checkVisible);
      controller?.abort();
      controller = undefined;
    };
  }
  $effect(() => {
    // A consumer can keep the same loader when switching projects or deployments.
    void sourceKey;
    const loader = read;
    return untrack(() => startMonitoring(loader));
  });
</script>

<PageShell>
  <PageHeader {title}>
    {#snippet actions()}
      <span class="cadence"><Icon name="clock" size="xs" /> Refreshes every minute</span>
      <IconButton
        icon="refresh-cw"
        size="sm"
        title={'Refresh ' + title.toLowerCase()}
        disabled={loading}
        onclick={() => refresh()}
      />
    {/snippet}
  </PageHeader>
  <PageBody>
    <div class="health-page">
      <section class={'summary ' + presentation(overall).tone} aria-label="Overall health">
        <span class="signal"
          >{#if loading && !report}<Spinner size="sm" />{:else}<Icon
              name={presentation(overall).icon}
              size="lg"
            />{/if}</span
        >
        <div class="summary-copy" role="status">
          <h2>{banner}</h2>
          <p>{subtitle}</p>
        </div>
        {#if report}<div class="timestamp">
            <span>Last checked</span><time datetime={report.checkedAt}
              >{time(report.checkedAt)}</time
            >
          </div>{/if}
      </section>

      <div class="view-picker" role="group" aria-label={title + ' view'}>
        <TabBar
          tabs={[
            { id: 'overview', label: 'Overview', icon: 'list' },
            { id: 'timeline', label: 'Timeline', icon: 'history' },
          ]}
          activeTab={view}
          onchange={(id) => (view = id === 'timeline' ? 'timeline' : 'overview')}
        />
      </div>
      {#if view === 'timeline'}
        <StatusTimeline {observations} {itemLabel} {itemsLabel} {title} />
      {:else if report}
        <StatusOverview
          bind:attentionOnly
          {report}
          {samples}
          {failed}
          {icons}
          {groupOrder}
          {itemLabel}
          {itemsLabel}
        />
      {:else if failed}
        <Button variant="secondary" size="sm" onclick={() => refresh()} disabled={loading}
          >Retry check</Button
        >
      {/if}
    </div>
  </PageBody>
</PageShell>

<style>
  .health-page {
    container-type: inline-size;
    max-width: 74rem;
    margin: 0 auto;
    padding: var(--space-5) var(--space-6) var(--space-8);
  }
  .summary {
    display: flex;
    align-items: center;
    gap: var(--space-4);
    padding: var(--space-5);
    border: 1px solid var(--color-border-primary);
    border-inline-start: 3px solid var(--signal);
    border-radius: var(--space-2);
    background: linear-gradient(
      105deg,
      color-mix(in srgb, var(--signal) 8%, var(--color-bg-primary)),
      var(--color-bg-primary) 70%
    );
    margin-bottom: var(--space-6);
  }
  .up {
    --signal: var(--color-success-500);
  }
  .warn {
    --signal: var(--color-warning-500);
  }
  .down {
    --signal: var(--color-error-500);
  }
  .idle {
    --signal: var(--color-text-tertiary);
  }
  .signal {
    display: grid;
    place-items: center;
    flex: none;
    border-radius: var(--space-2);
  }
  .signal {
    width: var(--space-10);
    height: var(--space-10);
    color: var(--signal);
    background: color-mix(in srgb, var(--signal) 12%, var(--color-bg-primary));
  }
  .summary-copy {
    flex: 1;
    min-width: 0;
  }
  h2,
  p {
    margin: 0;
  }
  h2 {
    font-size: var(--text-xl);
    line-height: 1.25;
    letter-spacing: -0.025em;
    font-weight: 650;
    color: var(--color-text-primary);
  }
  .summary-copy p,
  .timestamp,
  .cadence {
    color: var(--color-text-secondary);
    font-size: var(--text-xs);
  }
  .summary-copy p {
    margin-top: var(--space-1);
  }
  .timestamp {
    display: grid;
    gap: var(--space-1);
    text-align: end;
  }
  time {
    font-variant-numeric: tabular-nums;
  }
  .timestamp time {
    color: var(--color-text-primary);
  }
  .cadence {
    display: inline-flex;
    align-items: center;
    gap: var(--space-2);
  }
  .view-picker {
    display: flex;
    gap: var(--space-4);
    border-bottom: 1px solid var(--color-border-primary);
    margin-bottom: var(--space-4);
  }
  @container (max-width:680px) {
    .summary {
      flex-wrap: wrap;
    }
    .timestamp {
      text-align: start;
    }
  }
  @container (max-width:420px) {
    .timestamp {
      flex-basis: 100%;
    }
  }
  @media (max-width: 650px) {
    .health-page {
      padding: var(--space-3);
    }
    .cadence {
      display: none;
    }
  }
</style>
