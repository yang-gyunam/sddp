<script lang="ts">
  import { SvelteMap } from 'svelte/reactivity';
  import { Badge, Button, Checkbox, Icon, Select } from '@sddp/ui';
  import { formatDate, formatDateTime, formatTime } from '../../utils/datetime.utils';
  import type { StatusReading, StatusObservation } from '../../types/status-monitor.types';
  import { presentation, statusHistoryLimit } from '../../utils/status-monitor';

  let {
    observations,
    title = 'Service health',
    itemLabel = 'Service',
    itemsLabel = 'Services',
  }: {
    observations: StatusObservation[];
    title?: string;
    itemLabel?: string;
    itemsLabel?: string;
  } = $props();
  const recent = $derived(observations.slice(-statusHistoryLimit));
  let selectedServer = $state('');
  let changesOnly = $state(false);
  let expanded = $state.raw<StatusObservation | null>(null);
  const instance = $props.id();
  $effect(() => {
    if (expanded && !recent.includes(expanded)) expanded = null;
  });
  const servers = $derived.by(() => {
    const names = new SvelteMap<string, string>();
    for (const point of recent) {
      for (const row of point.report?.services ?? []) names.set(row.id, row.name);
    }
    return Array.from(names, ([value, label]) => ({ value, label }));
  });
  const server = $derived(servers.some((s) => s.value === selectedServer) ? selectedServer : '');
  const options = $derived([{ value: '', label: 'All ' + itemsLabel.toLowerCase() }, ...servers]);
  function clockTime(at: string) {
    return formatTime(at, { second: '2-digit' });
  }
  function date(at: string) {
    return formatDate(at, { month: 'short' });
  }
  function readingTime(at: string) {
    return formatDateTime(at, { month: 'short', second: '2-digit' });
  }

  type Change = {
    id: string;
    name: string;
    before?: string;
    after?: string;
    message?: string | null;
  };
  function compare(previous: StatusReading[], current: StatusReading[]): Change[] {
    const prior = new SvelteMap(previous.map((row) => [row.id, row]));
    const changes: Change[] = [];
    for (const row of current) {
      const before = prior.get(row.id)?.status;
      if (before !== row.status)
        changes.push({
          id: row.id,
          name: row.name,
          before,
          after: row.status,
          message: row.message,
        });
      prior.delete(row.id);
    }
    for (const row of prior.values())
      changes.push({ id: row.id, name: row.name, before: row.status });
    return changes;
  }

  const entries = $derived.by(() => {
    return recent
      .map((point, i) => {
        const prev = recent[i - 1];
        const rows = (point.report?.services ?? []).filter((row) => !server || row.id === server);
        const prior = (prev?.report?.services ?? []).filter((row) => !server || row.id === server);
        const resumed = !!point.report && prev?.report === null;
        const lost = !point.report && (!prev || !!prev.report);
        const changes = point.report && prev?.report ? compare(prior, rows) : [];
        const onlyChange = changes.length === 1 ? changes[0] : undefined;
        const heading = !point.report
          ? 'Report unavailable'
          : resumed
            ? 'Reporting resumed'
            : !prev
              ? 'First check in view'
              : changes.length
                ? onlyChange
                  ? onlyChange.name
                  : changes.length + ' ' + itemsLabel.toLowerCase() + ' changed'
                : 'No status changes';
        const status = !point.report
          ? 'Unknown'
          : onlyChange
            ? (onlyChange.after ?? 'Unknown')
            : server
              ? (rows[0]?.status ?? 'Unknown')
              : point.report.status;
        return {
          point,
          rows,
          changes,
          heading,
          status,
          changed: resumed || lost || changes.length > 0,
        };
      })
      .reverse()
      .filter((entry) => !changesOnly || entry.changed);
  });
</script>

<section class="timeline" aria-label={title + ' timeline'}>
  <div class="timeline-head">
    <div>
      <h3>Check history</h3>
      <p>Latest checks first · Local time</p>
    </div>
    <div class="controls">
      <Checkbox size="sm" label="Changes only" bind:checked={changesOnly} />
      <label for={instance + '-server'} class="sr-only">Timeline {itemLabel.toLowerCase()}</label>
      <Select
        placeholder=""
        id={instance + '-server'}
        {options}
        value={server}
        onchange={(v) => (selectedServer = String(v))}
        size="sm"
      />
    </div>
  </div>

  {#if entries.length}
    <ol class="events">
      {#each entries as entry, index (entry.point)}
        {@const style = presentation(entry.status)}
        {@const open = expanded === entry.point}
        {@const detailsId = instance + '-' + index}
        {@const previous = entries[index - 1]}
        {#if !previous || date(previous.point.at) !== date(entry.point.at)}
          <li class="day">{date(entry.point.at)}</li>
        {/if}
        <li class="event">
          {#snippet stamp()}
            <time datetime={entry.point.at} title={readingTime(entry.point.at)}
              >{clockTime(entry.point.at)}</time
            >
            <span class="rail" aria-hidden="true"><span class={'dot ' + style.tone}></span></span>
          {/snippet}
          {#snippet entryTitle()}
            <span class="event-heading">{entry.heading}</span>
            {#if entry.changes.length}
              <span class="changes">
                {#each entry.changes as change (change.id)}
                  <span class="change">
                    {#if entry.changes.length > 1}<b>{change.name}</b>{/if}
                    <span
                      >{change.before ? presentation(change.before).label : 'First reported'}</span
                    >
                    <span aria-hidden="true">→</span>
                    <Badge
                      size="sm"
                      variant={change.after ? presentation(change.after).variant : 'secondary'}
                    >
                      {change.after ? presentation(change.after).label : 'No longer reported'}
                    </Badge>
                    {#if change.message}<span class="diagnostic">{change.message}</span>{/if}
                  </span>
                {/each}
              </span>
            {:else if entry.point.report}
              <span class="context"
                >{entry.rows.length
                  ? entry.rows.length +
                    ' ' +
                    (entry.rows.length === 1 ? itemLabel.toLowerCase() : itemsLabel.toLowerCase()) +
                    ' observed'
                  : itemLabel + ' not reported'}</span
              >
            {:else}
              <span class="context">{itemLabel} states could not be observed.</span>
            {/if}
          {/snippet}
          {#if entry.point.report && entry.rows.length}
            <Button
              variant="unstyled"
              class="event-summary"
              aria-label={'Inspect check ' + readingTime(entry.point.at) + ' · ' + entry.heading}
              aria-controls={detailsId}
              aria-expanded={open}
              onclick={() => (expanded = open ? null : entry.point)}
            >
              {@render stamp()}
              <span class="entry-content"
                ><span class="entry-copy">{@render entryTitle()}</span>
                <Icon name={open ? 'chevron-up' : 'chevron-down'} size="sm" /></span
              >
            </Button>
            {#if open}<span class="rail continued" aria-hidden="true"></span>{/if}
            <div class="details-pane" id={detailsId} hidden={!open}>
              {#if open}
                {#if entry.point.at !== entry.point.report.checkedAt}
                  <p class="context">
                    Report measured at {readingTime(entry.point.report.checkedAt)}
                  </p>
                {/if}
                <div
                  class="details"
                  role="table"
                  aria-label={itemLabel +
                    ' readings at ' +
                    readingTime(entry.point.report.checkedAt)}
                >
                  <div class="details-row details-header" role="row">
                    <div role="columnheader">{itemLabel}</div>
                    <div role="columnheader">Status</div>
                    <div role="columnheader">Check time</div>
                  </div>
                  {#each entry.rows as row (row.id)}
                    {@const state = presentation(row.status)}
                    <div class="details-row" role="row">
                      <div role="rowheader">
                        {row.name}{#if row.message}<small>{row.message}</small>{/if}
                      </div>
                      <div role="cell">
                        <Badge variant={state.variant} size="sm">{state.label}</Badge>
                      </div>
                      <div role="cell">
                        {row.responseTimeMs == null ? '—' : row.responseTimeMs + ' ms'}
                      </div>
                    </div>
                  {/each}
                </div>
              {/if}
            </div>
          {:else}
            <div class="gap-summary">
              {@render stamp()}
              <div class="entry-content">
                <span class="entry-copy">{@render entryTitle()}</span>
              </div>
            </div>
          {/if}
        </li>
      {/each}
    </ol>
  {:else}
    <p class="empty">
      {changesOnly && observations.length
        ? 'No observed status changes in these checks.'
        : 'Waiting for a check.'}
    </p>
  {/if}
  <p class="note">
    <Icon name="clock" size="xs" /> Up to {statusHistoryLimit} observations while this view is open.
  </p>
</section>

<style>
  .timeline {
    container-type: inline-size;
    min-width: 0;
  }
  .timeline-head,
  .controls {
    display: flex;
    align-items: center;
    gap: var(--space-4);
  }
  .timeline-head {
    justify-content: space-between;
    flex-wrap: wrap;
    margin-bottom: var(--space-4);
  }
  h3 {
    margin: 0;
    font-size: var(--text-lg);
    font-weight: 600;
  }
  .timeline-head p,
  .context,
  .diagnostic,
  .note,
  .empty,
  .change {
    font-size: var(--text-xs);
    color: var(--color-text-secondary);
  }
  .timeline-head p {
    margin: var(--space-1) 0 0;
  }
  .controls {
    flex-wrap: wrap;
    gap: var(--space-3);
  }
  .controls :global(.w-full) {
    width: auto;
    max-width: 100%;
  }
  .events {
    padding: 0;
    margin: 0;
    border: 1px solid var(--color-border-primary);
    border-radius: var(--space-2);
    overflow: hidden;
    list-style: none;
  }
  .day {
    padding: var(--space-2) var(--space-4);
    background: var(--color-bg-secondary);
    font-weight: 600;
    font-size: var(--text-xs);
    border-bottom: 1px solid var(--color-border-secondary);
  }
  .event {
    display: grid;
    grid-template-columns: 5.2rem var(--space-8) minmax(0, 1fr);
    padding: 0 var(--space-4);
    align-items: stretch;
  }
  time {
    align-self: start;
    font-size: var(--text-xs);
    font-variant-numeric: tabular-nums;
    margin-top: var(--space-4);
    color: var(--color-text-secondary);
  }
  .rail {
    position: relative;
    display: flex;
    justify-content: center;
  }
  .rail::before {
    content: '';
    position: absolute;
    inset-block: 0;
    width: 1px;
    background: var(--color-border-primary);
  }
  .dot {
    position: relative;
    width: 8px;
    height: 8px;
    margin-top: calc(var(--space-4) + 3px);
    border-radius: 50%;
    background: var(--signal);
    box-shadow: 0 0 0 3px var(--color-bg-primary);
  }
  .day + .event .rail:not(.continued)::before {
    inset-block-start: var(--space-4);
  }
  .event:last-child .rail:not(.continued)::before {
    height: calc(var(--space-4) + 4px);
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
  .event :global(.event-summary),
  .gap-summary {
    display: grid;
    grid-template-columns: subgrid;
    grid-column: 1/-1;
    text-align: start;
    min-width: 0;
  }
  .entry-content {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: var(--space-2);
    padding: var(--space-3) var(--space-2);
    min-width: 0;
  }
  .event + .event .entry-content {
    border-top: 1px solid var(--color-border-secondary);
  }
  .event :global(.event-summary:hover) {
    background: var(--color-bg-secondary);
  }
  .event :global(.event-summary:focus-visible) {
    outline: 2px solid var(--color-accent-primary);
    outline-offset: -2px;
  }
  .continued {
    grid-column: 2;
  }
  .details-pane {
    grid-column: 3;
    min-width: 0;
  }
  .entry-copy,
  .changes {
    display: grid;
    gap: var(--space-1);
    min-width: 0;
  }
  .event-heading {
    font-weight: 600;
    font-size: var(--text-sm);
    overflow-wrap: anywhere;
  }
  .change {
    display: flex;
    align-items: center;
    flex-wrap: wrap;
    gap: var(--space-1-5);
  }
  .change b {
    font-weight: 500;
    color: var(--color-text-primary);
  }
  .details {
    width: 100%;
    display: grid;
    grid-template-columns: minmax(0, 1fr) auto auto;
    text-align: left;
    font-size: var(--text-xs);
    margin-bottom: var(--space-3);
  }
  .details-row {
    display: grid;
    grid-column: 1 / -1;
    grid-template-columns: subgrid;
  }
  .details-row > div {
    padding: var(--space-2);
    border-top: 1px solid var(--color-border-secondary);
  }
  .details-header > div {
    background: var(--color-bg-secondary);
    font-weight: 500;
    color: var(--color-text-secondary);
  }
  .details [role='rowheader'] {
    font-weight: 500;
    overflow-wrap: anywhere;
  }
  .details small {
    display: block;
    color: var(--color-text-secondary);
    font-size: inherit;
    font-weight: 400;
  }
  .details-row > div:last-child {
    text-align: right;
    font-variant-numeric: tabular-nums;
    white-space: nowrap;
  }
  .note {
    display: flex;
    align-items: center;
    gap: var(--space-2);
    margin: var(--space-3) 0 0;
  }
  .empty {
    padding: var(--space-5);
    margin: 0;
    border: 1px solid var(--color-border-primary);
    border-radius: var(--space-2);
  }
  @container (max-width:560px) {
    .event {
      grid-template-columns: 3.8rem var(--space-4) minmax(0, 1fr);
      padding-inline: var(--space-2);
    }
    .day {
      padding-inline: var(--space-2);
    }
    .details-row > div {
      padding: var(--space-1);
    }
    .controls {
      max-width: 100%;
    }
  }
</style>
