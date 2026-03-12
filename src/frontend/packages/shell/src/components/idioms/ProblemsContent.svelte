<!--
  ProblemsContent Component
  Displays problems/diagnostics with severity filtering and stack traces
-->
<script lang="ts">
  import { SvelteSet } from 'svelte/reactivity';
  import { Icon, Button } from '@sddp/ui';
  import { problems } from '../../stores/panel-content.store';
  import type { ProblemsState, ProblemEntry, ProblemSeverity } from '../../types';
  import { formatDateWithOptions } from '../../utils/datetime.utils';
  import EmptyState from './EmptyState.svelte';

  let problemsState: ProblemsState = $state({
    entries: [],
    errorCount: 0,
    warningCount: 0,
    infoCount: 0,
  });

  let currentFilter: ProblemSeverity | 'all' = $state('all');
  let expandedStacks = new SvelteSet<string>();

  $effect(() => {
    const unsubscribe = problems.subscribe((s) => {
      problemsState = s;
    });
    return unsubscribe;
  });

  const filteredEntries = $derived(
    currentFilter === 'all' ? problemsState.entries : problemsState.entries.filter((e) => e.severity === currentFilter)
  );

  function getSeverityIcon(severity: ProblemSeverity): string {
    switch (severity) {
      case 'error':
        return 'alert-circle';
      case 'warning':
        return 'alert-triangle';
      case 'info':
        return 'info';
      case 'hint':
        return 'lightbulb';
      default:
        return 'info';
    }
  }

  function getSeverityColor(severity: ProblemSeverity): string {
    switch (severity) {
      case 'error':
        return 'text-red-400';
      case 'warning':
        return 'text-yellow-400';
      case 'info':
        return 'text-blue-400';
      case 'hint':
        return 'text-green-400';
      default:
        return 'text-[var(--color-text-secondary)]';
    }
  }

  function formatLocation(entry: ProblemEntry): string {
    if (!entry.file) return '';
    let location = entry.file;
    if (entry.line !== undefined) {
      location += `:${entry.line}`;
      if (entry.column !== undefined) {
        location += `:${entry.column}`;
      }
    }
    return location;
  }

  function splitMessage(message: string): { title: string; description: string } {
    const normalized = message.trim();
    const separators = [' — ', ' - '];
    for (const separator of separators) {
      const index = normalized.indexOf(separator);
      if (index > 0) {
        return {
          title: normalized.slice(0, index).trim(),
          description: normalized.slice(index + separator.length).trim(),
        };
      }
    }
    return { title: normalized, description: '' };
  }

  function getCategory(entry: ProblemEntry): string {
    if (entry.code) return entry.code.toLowerCase();
    if (entry.type) return entry.type;
    return entry.source.trim().toLowerCase().replace(/\s+/g, '_');
  }

  function getTitle(entry: ProblemEntry): string {
    return splitMessage(entry.message).title;
  }

  function getDescription(entry: ProblemEntry): string {
    const parts = splitMessage(entry.message);
    if (parts.description) return parts.description;
    const location = formatLocation(entry);
    if (location) return location;
    return '-';
  }

  function formatTimestamp(timestamp: Date): string {
    return `[${formatDateWithOptions(timestamp, {
      locale: 'sv-SE',
      year: 'numeric',
      month: '2-digit',
      day: '2-digit',
      hour: '2-digit',
      minute: '2-digit',
      second: '2-digit',
      hour12: false,
    })}]`;
  }

  function toggleStack(id: string) {
    if (expandedStacks.has(id)) {
      expandedStacks.delete(id);
    } else {
      expandedStacks.add(id);
    }
  }

  function handleClear() {
    problems.clear();
    expandedStacks.clear();
  }

  function setFilter(newFilter: ProblemSeverity | 'all') {
    currentFilter = newFilter;
  }
</script>

<div class="h-full flex flex-col">
  {#if problemsState.entries.length === 0}
    <EmptyState
      icon="check-circle"
      heading="No problems detected"
      subtext="When issues are found, they will appear here"
      iconSize="lg"
    />
  {:else}
    <!-- Problems Header -->
    <div class="flex items-center justify-between h-7 px-3 border-b border-[var(--color-border)]">
      <div class="flex items-center gap-3">
        <!-- Severity Filters -->
        <Button
          variant="unstyled"
          onclick={() => setFilter('all')}
          class="flex items-center gap-1 text-xs transition-colors {currentFilter === 'all'
            ? 'text-[var(--color-text-primary)]'
            : 'text-[var(--color-text-muted)] hover:text-[var(--color-text-secondary)]'}"
        >
          All ({problemsState.entries.length})
        </Button>
        <Button
          variant="unstyled"
          onclick={() => setFilter('error')}
          class="flex items-center gap-1 text-xs transition-colors {currentFilter === 'error'
            ? 'text-[var(--color-error-500)]'
            : 'text-[var(--color-text-muted)] hover:text-[var(--color-error-500)]'}"
        >
          Error ({problemsState.errorCount})
        </Button>
        {#if problemsState.warningCount > 0}
          <Button
            variant="unstyled"
            onclick={() => setFilter('warning')}
            class="flex items-center gap-1 text-xs transition-colors {currentFilter === 'warning'
              ? 'text-[var(--color-warning-500)]'
              : 'text-[var(--color-text-muted)] hover:text-[var(--color-warning-500)]'}"
          >
            Warning ({problemsState.warningCount})
          </Button>
        {/if}
        {#if problemsState.infoCount > 0}
          <Button
            variant="unstyled"
            onclick={() => setFilter('info')}
            class="flex items-center gap-1 text-xs transition-colors {currentFilter === 'info'
              ? 'text-[var(--color-info-500)]'
              : 'text-[var(--color-text-muted)] hover:text-[var(--color-info-500)]'}"
          >
            Info ({problemsState.infoCount})
          </Button>
        {/if}
      </div>
      <Button
        variant="unstyled"
        class="text-xs text-[var(--color-text-muted)] hover:text-[var(--color-text-primary)] transition-colors"
        onclick={handleClear}
      >
        Clear All
      </Button>
    </div>

    <!-- Problems List -->
    <div class="flex-1 overflow-auto">
      {#each filteredEntries as entry (entry.id)}
        <div class="border-b border-[var(--color-border-subtle)]">
          <Button
            variant="unstyled"
            class="w-full text-left px-3 py-1.5 hover:bg-[var(--color-bg-hover)] cursor-pointer"
            onclick={() => entry.stack && toggleStack(entry.id)}
            aria-label="{entry.severity}: {entry.message}"
          >
            <div class="grid grid-cols-[50px_175px_220px_minmax(180px,1fr)_minmax(260px,2fr)] gap-2 items-center text-xs">
              <span class="flex items-center gap-1">
                {#if entry.stack}
                  <Icon
                    name={expandedStacks.has(entry.id) ? 'chevron-down' : 'chevron-right'}
                    size="xs"
                    class="text-[var(--color-text-muted)]"
                  />
                {/if}
                <Icon name={getSeverityIcon(entry.severity)} size="sm" class="{getSeverityColor(entry.severity)}" />
              </span>
              <span class="font-mono text-[var(--color-text-secondary)] truncate">{formatTimestamp(entry.timestamp)}</span>
              <span class="font-mono text-[var(--color-text-muted)] truncate">{getCategory(entry)}</span>
              <span class="text-[var(--color-text-primary)] truncate">{getTitle(entry)}</span>
              <span class="text-[var(--color-text-secondary)] truncate">{getDescription(entry)}</span>
            </div>
          </Button>

          <!-- Stack Trace (collapsible) -->
          {#if entry.stack && expandedStacks.has(entry.id)}
            <div class="px-3 py-2 ml-12 bg-[var(--color-bg-secondary)] border-t border-[var(--color-border-subtle)]">
              <pre class="text-xs font-mono text-[var(--color-text-secondary)] whitespace-pre-wrap break-all overflow-x-auto">{entry.stack}</pre>
            </div>
          {/if}
        </div>
      {/each}
    </div>
  {/if}
</div>
