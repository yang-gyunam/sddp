<script lang="ts">
  import { SvelteSet } from 'svelte/reactivity';
  import { Badge, Icon, TabBar } from '@sddp/ui';
  import { formatDateTime } from '../../utils/datetime.utils';
  import type { StatusReport } from '../../types/status-monitor.types';
  import { needsAttention, presentation, statusHistoryLimit } from '../../utils/status-monitor';
  let {
    report,
    samples: history = [],
    failed = false,
    attentionOnly = $bindable(false),
    icons = {},
    groupOrder = [],
    itemLabel = 'Service',
    itemsLabel = 'Services',
  }: {
    report: StatusReport;
    samples?: StatusReport[];
    failed?: boolean;
    attentionOnly?: boolean;
    icons?: Record<string, string>;
    groupOrder?: string[];
    itemLabel?: string;
    itemsLabel?: string;
  } = $props();
  const samples = $derived(history.length ? history.slice(-statusHistoryLimit) : [report]);
  const rows = $derived(report.services);
  const issues = $derived(rows.filter(needsAttention));
  const visible = $derived(attentionOnly ? issues : rows);
  const groups = $derived(
    [...new SvelteSet([...groupOrder, ...visible.map((row) => row.group)])].filter((group) =>
      visible.some((row) => row.group === group)
    )
  );
</script>

<div class="status-overview">
  <div class="toolbar">
    <h3>{itemsLabel} <span>{rows.length}</span></h3>
    <div aria-label={itemLabel + ' filter'} role="group">
      <TabBar
        variant="pill"
        activeTab={attentionOnly ? 'attention' : 'all'}
        tabs={[
          { id: 'all', label: 'All' },
          { id: 'attention', label: 'Needs attention ' + issues.length },
        ]}
        onchange={(id) => (attentionOnly = id === 'attention')}
      />
    </div>
  </div>
  <div class="roster" aria-label={itemLabel + ' status'}>
    {#if groups.length}
      <div class="column-head">
        <span>{itemLabel}</span><span class="history-col">Recent checks</span><span class="latency"
          >Check time</span
        ><span class="badge-col">{failed ? 'Last known status' : 'Status'}</span>
      </div>
    {/if}
    {#each groups as group (group)}
      <section class="group">
        <header><h4>{group}</h4></header>
        {#each visible.filter((s) => s.group === group) as row (row.id)}
          {@const state = presentation(row.status)}
          <div class="service-row">
            <div class="name-cell">
              <span class="service-icon"><Icon name={icons[row.id] ?? 'server'} size="md" /></span>
              <div>
                <strong>{row.name}</strong>
                <p>
                  {row.check}{#if row.responseTimeMs != null}<span class="mobile-time">
                      · {row.responseTimeMs} ms</span
                    >{/if}{#if row.message}<span class="reason"> · {row.message}</span>{/if}
                </p>
              </div>
            </div>
            <div class="history-col trail" aria-label={'Recent checks for ' + row.name}>
              {#each Array(statusHistoryLimit - samples.length) as _, i (i)}<span
                  class="slot empty"
                  aria-hidden="true"
                ></span>{/each}
              {#each samples as sample (sample.checkedAt)}
                {@const point = sample.services.find((s) => s.id === row.id)}
                {@const desc =
                  formatDateTime(sample.checkedAt, { month: 'short', second: '2-digit' }) +
                  ' · ' +
                  (point ? presentation(point.status).label : 'No sample')}
                <span
                  class={'slot ' + (point ? presentation(point.status).tone : 'empty')}
                  title={desc}
                  role="img"
                  aria-label={desc}
                ></span>
              {/each}
            </div>
            <span class="latency number"
              >{row.responseTimeMs == null ? '—' : row.responseTimeMs + ' ms'}</span
            >
            <span class="badge-col"
              ><Badge variant={state.variant} size="sm"
                ><Icon name={state.icon} size="xs" />{state.label}</Badge
              ></span
            >
          </div>
        {/each}
      </section>
    {:else}
      <p class="empty-label">
        {attentionOnly
          ? 'No ' + itemsLabel.toLowerCase() + ' need attention.'
          : 'No ' + itemsLabel.toLowerCase() + ' were reported.'}
      </p>
    {/each}
  </div>
  <p class="history-note">
    <Icon name="clock" size="xs" /> Last {statusHistoryLimit} checks while this view is open. Empty bars
    have no sample.
  </p>
</div>

<style>
  .status-overview {
    container-type: inline-size;
    min-width: 0;
  }
  h3,
  h4,
  p {
    margin: 0;
  }
  .service-icon {
    display: grid;
    place-items: center;
    flex: none;
    border-radius: var(--space-2);
  }
  .history-note,
  .empty-label {
    color: var(--color-text-secondary);
    font-size: var(--text-xs);
  }
  .history-note {
    display: inline-flex;
    align-items: center;
    gap: var(--space-2);
  }
  .number {
    font-variant-numeric: tabular-nums;
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
  .toolbar {
    display: flex;
    justify-content: space-between;
    align-items: center;
    gap: var(--space-3);
    margin-bottom: var(--space-3);
  }
  h3 {
    font-size: var(--text-lg);
    font-weight: 600;
  }
  h3 span {
    font-size: var(--text-sm);
    font-weight: 500;
    color: var(--color-text-secondary);
    margin-inline-start: var(--space-1);
  }
  .roster {
    border: 1px solid var(--color-border-primary);
    border-radius: var(--space-2);
    overflow: hidden;
  }
  .column-head,
  .service-row {
    display: grid;
    grid-template-columns: minmax(12rem, 1fr) 7rem 5rem 8rem;
    gap: var(--space-4);
    align-items: center;
    padding-inline: var(--space-4);
  }
  .column-head {
    min-height: var(--space-8);
    font-size: var(--text-xs);
    color: var(--color-text-secondary);
    border-bottom: 1px solid var(--color-border-primary);
  }
  .group header {
    display: flex;
    align-items: center;
    padding-inline: var(--space-4);
    background: var(--color-bg-secondary);
    min-height: var(--space-7);
    font-size: var(--text-xs);
    color: var(--color-text-secondary);
  }
  h4 {
    font-weight: 600;
    color: var(--color-text-primary);
  }
  .service-row {
    padding-block: var(--space-3);
    border-top: 1px solid var(--color-border-secondary);
  }
  .group + .group {
    border-top: 1px solid var(--color-border-primary);
  }
  .name-cell {
    display: flex;
    align-items: center;
    gap: var(--space-3);
    min-width: 0;
  }
  .service-icon {
    width: var(--space-8);
    height: var(--space-8);
    border: 1px solid var(--color-border-secondary);
    color: var(--color-text-secondary);
    background: var(--color-bg-secondary);
  }
  .name-cell strong {
    font-size: var(--text-sm);
    font-weight: 600;
    overflow-wrap: anywhere;
  }
  .name-cell p {
    font-size: var(--text-xs);
    color: var(--color-text-secondary);
    margin-top: 2px;
  }
  .mobile-time {
    display: none;
  }
  .reason {
    font-weight: 500;
  }
  .latency,
  .badge-col {
    text-align: end;
  }
  .number {
    font-size: var(--text-xs);
    color: var(--color-text-secondary);
  }
  .trail {
    display: grid;
    grid-template-columns: repeat(12, 1fr);
    gap: 2px;
  }
  .slot {
    display: block;
    height: var(--space-3);
    border-radius: 2px;
    background: var(--signal);
  }
  .slot.empty {
    background: var(--color-bg-tertiary);
    border: 1px solid var(--color-border-secondary);
  }
  .history-note {
    margin-top: var(--space-3);
  }
  .empty-label {
    padding: var(--space-4);
  }
  @container (max-width:680px) {
    .column-head,
    .service-row {
      grid-template-columns: minmax(8rem, 1fr) 4rem 7rem;
      gap: var(--space-2);
    }
    .history-col {
      display: none;
    }
    .history-note {
      display: none;
    }
  }
  @container (max-width:420px) {
    .column-head,
    .service-row {
      grid-template-columns: minmax(0, 1fr) auto;
      padding-inline: var(--space-2);
    }
    .mobile-time {
      display: inline;
    }
    .latency {
      display: none;
    }
    .service-icon {
      display: none;
    }
    .toolbar {
      flex-wrap: wrap;
    }
    .badge-col {
      white-space: nowrap;
    }
  }
</style>
