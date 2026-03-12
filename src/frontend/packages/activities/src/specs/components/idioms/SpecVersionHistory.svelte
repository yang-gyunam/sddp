<!--
  SpecVersionHistory Component
  Displays a timeline of spec versions
-->
<script lang="ts">
  import { Icon, Spinner, Button } from '@sddp/ui';
  import { formatDate as formatDateUtil } from '@sddp/shell';
  import SpecStatusBadge from './SpecStatusBadge.svelte';
  import type { Spec } from '../../types';

  interface Props {
    versions: Spec[];
    currentId?: string | null;
    loading?: boolean;
    onSelect?: (spec: Spec) => void;
    class?: string;
  }

  let {
    versions,
    currentId = null,
    loading = false,
    onSelect,
    class: className = '',
  }: Props = $props();

  function formatDateStr(dateStr: string): string {
    return formatDateUtil(dateStr, { month: 'short' });
  }

  function formatVersion(version: string): string {
    return `v${version}`;
  }
</script>

<div class="flex flex-col h-full {className}">
  <!-- Header -->
  <div class="px-4 py-3 border-b border-[var(--color-border-primary)]">
    <h3 class="text-sm font-medium text-[var(--color-text-primary)]">Version History</h3>
  </div>

  <!-- Timeline -->
  <div class="flex-1 overflow-y-auto">
    {#if loading}
      <div class="flex-1 flex items-center justify-center">
        <Spinner size="lg" />
      </div>
    {:else if versions.length === 0}
      <div class="flex flex-col items-center justify-center h-32 text-center px-4">
        <Icon name="git-branch" size="lg" class="text-[var(--color-text-muted)] mb-2" />
        <p class="text-sm text-[var(--color-text-muted)]">No version history</p>
      </div>
    {:else}
      <div class="relative px-4 py-4">
        <!-- Vertical line -->
        <div class="absolute left-7 top-6 bottom-6 w-px bg-[var(--color-border-secondary)]"></div>

        <ul class="space-y-4">
          {#each versions as version, index (version.id)}
            {@const isCurrent = currentId === version.id}
            {@const isLatest = index === 0}
            <li class="relative">
              <Button
                variant="unstyled"
                onclick={() => onSelect?.(version)}
                disabled={!onSelect}
                class="w-full text-left pl-8 group"
              >
                <!-- Timeline dot -->
                <div
                  class="absolute left-0 w-6 h-6 rounded-full border-2 flex items-center justify-center transition-colors
                    {isCurrent
                      ? 'bg-[var(--color-accent-primary)] border-[var(--color-accent-primary)]'
                      : 'bg-[var(--color-surface-50)] border-[var(--color-border-secondary)] group-hover:border-[var(--color-accent-primary)]'}"
                >
                  {#if isCurrent}
                    <Icon name="check" size="xs" class="text-white" />
                  {:else if version.status === 'Locked'}
                    <Icon name="lock" size="xs" class="text-[var(--color-text-muted)]" />
                  {:else}
                    <div class="w-2 h-2 rounded-full bg-[var(--color-border-secondary)]"></div>
                  {/if}
                </div>

                <!-- Version card -->
                <div
                  class="p-3 rounded-lg border transition-colors
                    {isCurrent
                      ? 'border-[var(--color-accent-primary)] bg-[var(--color-accent-primary)] dark:bg-[var(--color-accent-primary)]'
                      : 'border-[var(--color-border-secondary)] bg-[var(--color-surface-100)] hover:bg-[var(--color-surface-200)]'}"
                >
                  <div class="flex items-center justify-between mb-2">
                    <div class="flex items-center gap-2">
                      <span class="text-sm font-medium text-[var(--color-text-primary)]">
                        {formatVersion(version.version)}
                      </span>
                      {#if isLatest}
                        <span class="text-xs px-1.5 py-0.5 rounded bg-[var(--color-accent-primary)] text-[var(--color-accent-primary)] dark:bg-[var(--color-accent-primary)] dark:text-[var(--color-accent-primary)]">
                          Latest
                        </span>
                      {/if}
                    </div>
                    <SpecStatusBadge status={version.status} showIcon={false} size="sm" />
                  </div>

                  <div class="text-xs text-[var(--color-text-muted)] space-y-1">
                    <div class="flex items-center gap-1">
                      <Icon name="calendar" size="xs" />
                      <span>{formatDateStr(version.createdAt)}</span>
                    </div>
                    {#if version.lockedAt}
                      <div class="flex items-center gap-1">
                        <Icon name="lock" size="xs" />
                        <span>Locked: {formatDateStr(version.lockedAt)}</span>
                      </div>
                    {/if}
                  </div>
                </div>
              </Button>
            </li>
          {/each}
        </ul>
      </div>
    {/if}
  </div>
</div>
