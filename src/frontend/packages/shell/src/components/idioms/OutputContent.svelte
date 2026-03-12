<!--
  OutputContent Component
  Displays output logs with level filtering and source grouping
-->
<script lang="ts">
  import { Icon, Button, Select } from '@sddp/ui';
  import { output } from '../../stores/panel-content.store';
  import { formatTime } from '../../utils/datetime.utils';
  import type { OutputState, OutputLogLevel } from '../../types';
  import EmptyState from './EmptyState.svelte';

  let outputState: OutputState = $state({
    entries: [],
    filter: 'all',
    sources: [],
  });

  $effect(() => {
    const unsubscribe = output.subscribe((s) => {
      outputState = s;
    });
    return unsubscribe;
  });

  const filteredEntries = $derived(
    outputState.filter === 'all' ? outputState.entries : outputState.entries.filter((e) => e.level === outputState.filter)
  );

  function formatTimestamp(date: Date): string {
    return formatTime(date, { locale: 'en-US', second: '2-digit' });
  }

  function getLevelIcon(level: OutputLogLevel): string {
    switch (level) {
      case 'debug':
        return 'bug';
      case 'info':
        return 'info';
      case 'warn':
        return 'alert-triangle';
      case 'error':
        return 'circle-x';
      default:
        return 'info';
    }
  }

  function getLevelColor(level: OutputLogLevel): string {
    switch (level) {
      case 'debug':
        return 'text-gray-400';
      case 'info':
        return 'text-blue-400';
      case 'warn':
        return 'text-yellow-400';
      case 'error':
        return 'text-red-400';
      default:
        return 'text-[var(--color-text-secondary)]';
    }
  }

  function handleClear() {
    output.clear();
  }

  function setFilter(filter: OutputLogLevel | 'all') {
    output.setFilter(filter);
  }

  function formatData(data: unknown): string | null {
    if (data === undefined || data === null) return null;
    try {
      return JSON.stringify(data, null, 2);
    } catch {
      return String(data);
    }
  }
</script>

<div class="h-full flex flex-col">
  {#if outputState.entries.length === 0}
    <EmptyState
      icon="scroll-text"
      heading="No output"
      subtext="Application logs and debug output will appear here"
      iconSize="lg"
    />
  {:else}
    <!-- Output Header -->
    <div class="flex items-center justify-between h-7 px-3 border-b border-[var(--color-border)]">
      <div class="flex items-center gap-2">
        <!-- Level Filter Dropdown -->
        <Select
          unstyled
          class="text-xs bg-transparent border border-[var(--color-border)] rounded px-2 py-0.5 text-[var(--color-text-secondary)] focus:outline-none focus:border-[var(--color-accent-primary)]"
          value={outputState.filter}
          onchange={(value) => setFilter(value as OutputLogLevel | 'all')}
        >
          <option value="all">All Levels</option>
          <option value="debug">Debug</option>
          <option value="info">Info</option>
          <option value="warn">Warning</option>
          <option value="error">Error</option>
        </Select>
        <span class="text-xs text-[var(--color-text-muted)]">
          {filteredEntries.length} entries
        </span>
      </div>
      <Button
        variant="unstyled"
        class="text-xs text-[var(--color-text-muted)] hover:text-[var(--color-text-primary)] transition-colors"
        onclick={handleClear}
      >
        Clear
      </Button>
    </div>

    <!-- Output Entries -->
    <div class="flex-1 overflow-auto font-mono text-xs">
      {#each filteredEntries as entry (entry.id)}
        <div class="flex gap-2 px-3 py-1.5 hover:bg-[var(--color-bg-hover)] border-b border-[var(--color-border-subtle)]">
          <span class="text-[var(--color-text-muted)] shrink-0">
            [{formatTimestamp(entry.timestamp)}]
          </span>
          <Icon name={getLevelIcon(entry.level)} size="xs" class="{getLevelColor(entry.level)} shrink-0 mt-0.5" />
          <span class="text-[var(--color-accent-primary)] shrink-0">
            [{entry.source}]
          </span>
          <div class="flex-1 min-w-0">
            <span class={getLevelColor(entry.level)}>{entry.message}</span>
            {#if entry.data !== undefined}
              {@const formattedData = formatData(entry.data)}
              {#if formattedData}
                <pre class="mt-1 text-[var(--color-text-muted)] whitespace-pre-wrap break-all bg-[var(--color-bg-tertiary)] p-1 rounded">{formattedData}</pre>
              {/if}
            {/if}
          </div>
        </div>
      {/each}
    </div>
  {/if}
</div>
