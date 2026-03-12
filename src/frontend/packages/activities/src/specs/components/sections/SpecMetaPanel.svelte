<!-- Section: SpecMetaPanel — Specs > Global -->
<script lang="ts">
  import { Icon, Button } from '@sddp/ui';
  import { formatDate, formatNumber, formatPercent } from '@sddp/shell';
  import LinkedEntityCard from '../../../shared/components/idioms/LinkedEntityCard.svelte';
  import type { SpecDetail, SignOffSummary } from '../../types';

  interface VersionInfo {
    version: string;
    status: string;
    createdAt: string;
    isCurrent: boolean;
  }

  interface Props {
    spec: SpecDetail | null;
    signOffSummary?: SignOffSummary | null;
    versionHistory?: VersionInfo[];
    onVersionSelect?: (version: string) => void;
    onOpenSignOff?: () => void;
    class?: string;
  }

  let {
    spec,
    signOffSummary = null,
    versionHistory = [],
    onVersionSelect,
    onOpenSignOff,
    class: className = '',
  }: Props = $props();

  // Calculate sign-off progress
  const signOffProgress = $derived(() => {
    if (!signOffSummary) return 0;
    const { totalCount, pendingCount } = signOffSummary;
    if (totalCount === 0) return 0;
    return Math.round(((totalCount - pendingCount) / totalCount) * 100);
  });
  const signOffProgressLabel = $derived(
    formatPercent(signOffProgress() / 100, { maximumFractionDigits: 0 })
  );
  const signOffCompletedLabel = $derived(
    signOffSummary ? formatNumber(signOffSummary.totalCount - signOffSummary.pendingCount) : '0'
  );
  const signOffTotalLabel = $derived(
    signOffSummary ? formatNumber(signOffSummary.totalCount) : '0'
  );
</script>

<div class="spec-meta-panel flex flex-col h-full bg-[var(--color-bg-secondary)] {className}">
  {#if spec}
    <!-- Quick Info -->
    <div class="panel-section p-3 border-b border-[var(--color-border-primary)]">
      <h3 class="text-xs font-semibold uppercase tracking-wide text-[var(--color-text-secondary)] mb-2">
        Quick Info
      </h3>
      <div class="space-y-2 text-sm">
        <div class="flex items-center justify-between">
          <span class="text-[var(--color-text-tertiary)]">Code</span>
          <span class="font-mono">{spec.code}</span>
        </div>
        <div class="flex items-center justify-between">
          <span class="text-[var(--color-text-tertiary)]">Version</span>
          <span>{spec.version}</span>
        </div>
        <div class="flex items-center justify-between">
          <span class="text-[var(--color-text-tertiary)]">Status</span>
          <span>{spec.status}</span>
        </div>
      </div>
    </div>

    <!-- Sign-Off Progress (for InReview) -->
    {#if spec.status === 'InReview' && signOffSummary}
      <div class="panel-section p-3 border-b border-[var(--color-border-primary)]">
        <h3 class="text-xs font-semibold uppercase tracking-wide text-[var(--color-text-secondary)] mb-2">
          Sign-Off Progress
        </h3>

        <!-- Progress Bar -->
        <div class="mb-3">
          <div class="flex items-center justify-between text-xs mb-1">
            <span class="text-[var(--color-text-tertiary)]">
              {signOffCompletedLabel} of {signOffTotalLabel}
            </span>
            <span class="font-medium">{signOffProgressLabel}</span>
          </div>
          <div class="h-2 bg-[var(--color-bg-tertiary)] rounded-full overflow-hidden">
            <div
              class="h-full bg-[var(--color-accent-primary)] transition-all"
              style="width: {signOffProgress()}%"
            ></div>
          </div>
        </div>

        <!-- Stats Grid -->
        <div class="grid grid-cols-2 gap-2 text-xs">
          <div class="flex items-center gap-1.5 p-2 rounded bg-[var(--color-bg-primary)]">
            <Icon name="clock" size="xs" class="text-[var(--color-text-tertiary)]" />
            <span>Pending: {formatNumber(signOffSummary.pendingCount)}</span>
          </div>
          <div class="flex items-center gap-1.5 p-2 rounded bg-[var(--color-bg-primary)]">
            <Icon name="check" size="xs" class="text-[var(--color-success-500)]" />
            <span>Approved: {formatNumber(signOffSummary.approvedCount)}</span>
          </div>
          <div class="flex items-center gap-1.5 p-2 rounded bg-[var(--color-bg-primary)]">
            <Icon name="alert-circle" size="xs" class="text-[var(--color-warning-500)]" />
            <span>Conditional: {formatNumber(signOffSummary.conditionalCount)}</span>
          </div>
          <div class="flex items-center gap-1.5 p-2 rounded bg-[var(--color-bg-primary)]">
            <Icon name="x" size="xs" class="text-[var(--color-error-500)]" />
            <span>Rejected: {formatNumber(signOffSummary.rejectedCount)}</span>
          </div>
        </div>

        <Button
          variant="unstyled"
          class="mt-3 w-full flex items-center justify-center gap-2 px-3 py-2 text-sm rounded
                 bg-[var(--color-bg-tertiary)] text-[var(--color-text-primary)]
                 hover:bg-[var(--color-bg-tertiary)]/80"
          onclick={() => onOpenSignOff?.()}
        >
          <Icon name="user" size="xs" />
          <span>View All Sign-Offs</span>
        </Button>
      </div>
    {/if}

    <!-- Version History -->
    <div class="panel-section flex-1 p-3 overflow-y-auto">
      <h3 class="text-xs font-semibold uppercase tracking-wide text-[var(--color-text-secondary)] mb-2">
        Version History
      </h3>

      {#if versionHistory.length > 0}
        <div class="relative">
          <!-- Timeline Line -->
          <div class="absolute left-2 top-2 bottom-2 w-0.5 bg-[var(--color-border-primary)]"></div>

          <div class="space-y-3">
            {#each versionHistory as version (version.version)}
              <Button
                variant="unstyled"
                class="relative flex items-start gap-3 w-full text-left pl-6 py-2 rounded
                       {version.isCurrent ? 'bg-[var(--color-accent-primary)]/10' : 'hover:bg-[var(--color-bg-tertiary)]'}"
                onclick={() => onVersionSelect?.(version.version)}
              >
                <!-- Timeline Dot -->
                <div
                  class="absolute left-0.5 top-3 w-3 h-3 rounded-full border-2 border-[var(--color-border-primary)]
                         {version.isCurrent ? 'bg-[var(--color-accent-primary)]' : 'bg-[var(--color-bg-secondary)]'}"
                ></div>

                <div class="flex-1 min-w-0">
                  <div class="flex items-center gap-2">
                    <span class="font-mono text-sm">v{version.version}</span>
                    {#if version.isCurrent}
                      <span class="text-xs px-1.5 py-0.5 rounded bg-[var(--color-accent-primary)]/20 text-[var(--color-accent-primary)]">
                        Current
                      </span>
                    {/if}
                    {#if version.status === 'Locked'}
                      <Icon name="lock" size="xs" class="text-[var(--color-text-tertiary)]" />
                    {/if}
                  </div>
                  <div class="text-xs text-[var(--color-text-tertiary)]">
                    {formatDate(version.createdAt)}
                  </div>
                </div>
              </Button>
            {/each}
          </div>
        </div>
      {:else}
        <p class="text-sm text-[var(--color-text-tertiary)] italic">
          No version history
        </p>
      {/if}
    </div>

    <!-- Linked Items -->
    {#if spec.requirementId || spec.bornFromConversationId}
      <div class="panel-section p-3 border-t border-[var(--color-border-primary)]">
        <h3 class="text-xs font-semibold uppercase tracking-wide text-[var(--color-text-secondary)] mb-2">
          Linked Items
        </h3>
        <div class="space-y-2">
          {#if spec.requirementId}
            <LinkedEntityCard
              icon="file-text"
              label="Linked Requirement"
            />
          {/if}
          {#if spec.bornFromConversationId}
            <LinkedEntityCard
              icon="message-circle"
              label="Source Conversation"
            />
          {/if}
        </div>
      </div>
    {/if}
  {:else}
    <!-- Empty State -->
    <div class="flex-1 flex items-center justify-center p-4">
      <div class="text-center">
        <Icon name="file-code" size="lg" class="mx-auto mb-2 text-[var(--color-text-tertiary)] opacity-70" />
        <p class="text-sm font-medium text-[var(--color-text-primary)]">Select a spec</p>
        <p class="text-xs text-[var(--color-text-secondary)] mt-1">
          Choose a spec to view details.
        </p>
      </div>
    </div>
  {/if}
</div>
