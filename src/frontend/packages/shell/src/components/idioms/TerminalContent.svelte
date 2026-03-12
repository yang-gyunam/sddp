<!--
  TerminalContent Component
  Displays terminal entries with command history and output
-->
<script lang="ts">
  import { Spinner, Button } from '@sddp/ui';
  import { terminal } from '../../stores/panel-content.store';
  import { formatTime } from '../../utils/datetime.utils';
  import type { TerminalState, TerminalEntry } from '../../types';
  import EmptyState from './EmptyState.svelte';

  let terminalState: TerminalState = $state({
    entries: [],
    isRunning: false,
    currentCommand: '',
  });

  $effect(() => {
    const unsubscribe = terminal.subscribe((s) => {
      terminalState = s;
    });
    return unsubscribe;
  });

  function formatTimestamp(date: Date): string {
    return formatTime(date, { locale: 'en-US', second: '2-digit' });
  }

  function getEntryColor(type: TerminalEntry['type']): string {
    switch (type) {
      case 'input':
        return 'text-[var(--color-primary-400)]';
      case 'output':
        return 'text-[var(--color-text-secondary)]';
      case 'error':
        return 'text-red-400';
      case 'system':
        return 'text-yellow-400';
      default:
        return 'text-[var(--color-text-secondary)]';
    }
  }

  function getEntryPrefix(type: TerminalEntry['type']): string {
    switch (type) {
      case 'input':
        return '$ ';
      case 'error':
        return '! ';
      case 'system':
        return '> ';
      default:
        return '';
    }
  }

  function handleClear() {
    terminal.clear();
  }
</script>

<div class="h-full flex flex-col">
  {#if terminalState.entries.length === 0}
    <EmptyState
      icon="terminal"
      heading="No terminal output"
      subtext="Terminal commands and their output will appear here"
      iconSize="lg"
    />
  {:else}
    <!-- Terminal Header -->
    <div class="flex items-center justify-between px-3 py-1 border-b border-[var(--color-border)]">
      <span class="text-xs text-[var(--color-text-muted)]">
        {terminalState.entries.length} entries
      </span>
      <Button
        variant="unstyled"
        class="text-xs text-[var(--color-text-muted)] hover:text-[var(--color-text-primary)] transition-colors"
        onclick={handleClear}
      >
        Clear
      </Button>
    </div>

    <!-- Terminal Entries -->
    <div class="flex-1 overflow-auto p-3 font-mono text-sm">
      {#each terminalState.entries as entry (entry.id)}
        <div class="flex gap-2 py-0.5 {getEntryColor(entry.type)}">
          <span class="text-[var(--color-text-muted)] text-xs shrink-0">
            [{formatTimestamp(entry.timestamp)}]
          </span>
          <span class="whitespace-pre-wrap break-all">
            <span class="opacity-70">{getEntryPrefix(entry.type)}</span>{entry.content}
          </span>
        </div>
      {/each}
      {#if terminalState.isRunning}
        <div class="flex items-center gap-2 py-0.5 text-[var(--color-text-muted)]">
          <Spinner size="sm" />
          <span>Running...</span>
        </div>
      {/if}
    </div>
  {/if}
</div>
