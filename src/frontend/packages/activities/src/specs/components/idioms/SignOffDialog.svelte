<!--
  SignOffDialog Component
  Sign-off dialog for Spec approval workflow
 REQ-04.3: Sign-off
  REQ-06.2: approve permission
-->
<script lang="ts">
  import { Icon, Button, Spinner, Textarea } from '@sddp/ui';
  import { Modal, formatDateTime, formatNumber, formatPercent } from '@sddp/shell';
  import type {
    SignOffSummary,
    SignOffDecision,
    SignOffRequest,
  } from '../../types';
  import {
    SIGN_OFF_DECISION_STYLES,
    ROLE_TYPE_STYLES,
  } from '../../types';

  interface Props {
    open?: boolean;
    summary: SignOffSummary;
    currentUserId?: string;
    loading?: boolean;
    onSignOff?: (request: SignOffRequest) => void;
    onClose?: () => void;
  }

  let {
    open = false,
    summary,
    currentUserId,
    loading = false,
    onSignOff,
    onClose,
  }: Props = $props();

  // Form state
  let selectedDecision = $state<SignOffDecision | null>(null);
  let conditions = $state('');
  let comments = $state('');

  // Find current user's sign-off
  const currentUserSignOff = $derived(
    currentUserId
      ? summary.signOffs.find((s) => s.stakeholder.id === currentUserId)
      : null
  );

  // Can current user sign off?
  const canSignOff = $derived(
    currentUserSignOff && currentUserSignOff.decision === 'Pending'
  );

  // Summary stats
  const progressPercentage = $derived(
    summary.totalCount > 0
      ? Math.round(
          ((summary.approvedCount + summary.conditionalCount + summary.rejectedCount) /
            summary.totalCount) *
            100
        )
      : 0
  );
  const progressLabel = $derived(
    formatPercent(progressPercentage / 100, { maximumFractionDigits: 0 })
  );

  function formatDateStr(dateStr: string | null | undefined): string {
    if (!dateStr) return '-';
    return formatDateTime(dateStr, { month: 'short' });
  }

  function handleDecisionSelect(decision: SignOffDecision) {
    selectedDecision = decision;
    // Reset conditions if not conditional
    if (decision !== 'Conditional') {
      conditions = '';
    }
  }

  function handleSubmit() {
    if (!selectedDecision) return;

    const request: SignOffRequest = {
      decision: selectedDecision,
      comments: comments.trim() || undefined,
    };

    if (selectedDecision === 'Conditional' && conditions.trim()) {
      request.conditions = conditions.trim();
    }

    onSignOff?.(request);
  }

  function handleClose() {
    // Reset form state
    selectedDecision = null;
    conditions = '';
    comments = '';
    onClose?.();
  }

  function getDecisionStyle(decision: SignOffDecision) {
    return SIGN_OFF_DECISION_STYLES[decision];
  }

  function getRoleStyle(role: string) {
    return ROLE_TYPE_STYLES[role as keyof typeof ROLE_TYPE_STYLES] || { label: role, icon: 'user' };
  }
</script>

<Modal
  {open}
  title="Sign-off Status"
  size="lg"
  onClose={handleClose}
>
  <div class="space-y-6">
    <!-- Summary Section -->
    <div class="p-4 rounded-lg bg-[var(--color-surface-100)] border border-[var(--color-border-secondary)]">
      <div class="flex items-center justify-between mb-3">
        <h3 class="text-sm font-medium text-[var(--color-text-primary)]">Progress</h3>
        <span class="text-sm text-[var(--color-text-muted)]">
          {progressLabel} Complete
        </span>
      </div>

      <!-- Progress bar -->
      <div class="h-2 bg-[var(--color-surface-200)] rounded-full overflow-hidden mb-4">
        <div
          class="h-full transition-all duration-300"
          style="width: {progressPercentage}%; background: linear-gradient(90deg, var(--color-success-500), var(--color-accent-primary));"
        ></div>
      </div>

      <!-- Stats -->
      <div class="grid grid-cols-4 gap-4 text-center">
        <div>
          <div class="text-lg font-semibold text-[var(--color-text-primary)]">{formatNumber(summary.pendingCount)}</div>
          <div class="text-xs text-[var(--color-text-muted)]">Pending</div>
        </div>
        <div>
          <div class="text-lg font-semibold text-green-600 dark:text-green-400">{formatNumber(summary.approvedCount)}</div>
          <div class="text-xs text-[var(--color-text-muted)]">Approved</div>
        </div>
        <div>
          <div class="text-lg font-semibold text-yellow-600 dark:text-yellow-400">{formatNumber(summary.conditionalCount)}</div>
          <div class="text-xs text-[var(--color-text-muted)]">Conditional</div>
        </div>
        <div>
          <div class="text-lg font-semibold text-red-600 dark:text-red-400">{formatNumber(summary.rejectedCount)}</div>
          <div class="text-xs text-[var(--color-text-muted)]">Rejected</div>
        </div>
      </div>
    </div>

    <!-- Reviewers List -->
    <div>
      <h3 class="text-sm font-medium text-[var(--color-text-primary)] mb-3">Reviewers</h3>
      <div class="space-y-2 max-h-60 overflow-y-auto">
        {#each summary.signOffs as signOff (signOff.id)}
          {@const style = getDecisionStyle(signOff.decision)}
          {@const roleStyle = getRoleStyle(signOff.role)}
          {@const isCurrentUser = signOff.stakeholder.id === currentUserId}

          <div
            class="p-3 rounded-lg border transition-colors {isCurrentUser ? 'ring-2 ring-[var(--color-accent-primary)]' : ''} {style.bgColor} {style.borderColor}"
          >
            <div class="flex items-start justify-between gap-3">
              <div class="flex items-start gap-3 min-w-0">
                <!-- Avatar placeholder -->
                <div class="w-10 h-10 rounded-full bg-[var(--color-surface-300)] flex items-center justify-center flex-shrink-0">
                  <Icon name="user" size="sm" class="text-[var(--color-text-muted)]" />
                </div>

                <div class="min-w-0">
                  <div class="flex items-center gap-2">
                    <span class="font-medium text-[var(--color-text-primary)] truncate">
                      {signOff.stakeholder.name}
                    </span>
                    {#if isCurrentUser}
                      <span class="text-xs px-1.5 py-0.5 rounded bg-[var(--color-accent-primary)] text-white">
                        You
                      </span>
                    {/if}
                  </div>
                  <div class="flex items-center gap-2 text-xs text-[var(--color-text-muted)]">
                    <Icon name={roleStyle.icon} size="xs" />
                    <span>{roleStyle.label}</span>
                  </div>
                </div>
              </div>

              <!-- Decision badge -->
              <div class="flex items-center gap-2 flex-shrink-0">
                <div class="flex items-center gap-1 px-2 py-1 rounded-full {style.bgColor} {style.textColor}">
                  <Icon name={style.icon} size="xs" />
                  <span class="text-xs font-medium">{style.label}</span>
                </div>
              </div>
            </div>

            <!-- Comments/Conditions -->
            {#if signOff.conditions}
              <div class="mt-2 p-2 rounded bg-yellow-100/50 dark:bg-yellow-900/30 text-sm">
                <span class="font-medium text-yellow-700 dark:text-yellow-300">Conditions: </span>
                <span class="text-[var(--color-text-primary)]">{signOff.conditions}</span>
              </div>
            {/if}
            {#if signOff.comments}
              <div class="mt-2 text-sm text-[var(--color-text-secondary)] italic">
                "{signOff.comments}"
              </div>
            {/if}
            {#if signOff.signedAt}
              <div class="mt-2 text-xs text-[var(--color-text-muted)]">
                Signed: {formatDateStr(signOff.signedAt)}
              </div>
            {/if}
          </div>
        {/each}
      </div>
    </div>

    <!-- Sign-off Form (only if current user can sign off) -->
    {#if canSignOff}
      <div class="pt-4 border-t border-[var(--color-border-secondary)]">
        <h3 class="text-sm font-medium text-[var(--color-text-primary)] mb-3">Your Sign-off</h3>

        <!-- Decision buttons -->
        <div class="flex flex-wrap gap-2 mb-4">
          <Button
            variant="unstyled"
            onclick={() => handleDecisionSelect('Approved')}
            class="flex items-center gap-2 px-4 py-2 rounded-lg border-2 transition-colors {selectedDecision === 'Approved' ? 'border-green-500 bg-green-50 dark:bg-green-950' : 'border-[var(--color-border-secondary)] hover:border-green-300'}"
          >
            <Icon name="check-circle" size="sm" class="text-green-600 dark:text-green-400" />
            <span class="text-sm font-medium">Approve</span>
          </Button>

          <Button
            variant="unstyled"
            onclick={() => handleDecisionSelect('Conditional')}
            class="flex items-center gap-2 px-4 py-2 rounded-lg border-2 transition-colors {selectedDecision === 'Conditional' ? 'border-yellow-500 bg-yellow-50 dark:bg-yellow-950' : 'border-[var(--color-border-secondary)] hover:border-yellow-300'}"
          >
            <Icon name="alert-circle" size="sm" class="text-yellow-600 dark:text-yellow-400" />
            <span class="text-sm font-medium">Conditional</span>
          </Button>

          <Button
            variant="unstyled"
            onclick={() => handleDecisionSelect('Rejected')}
            class="flex items-center gap-2 px-4 py-2 rounded-lg border-2 transition-colors {selectedDecision === 'Rejected' ? 'border-red-500 bg-red-50 dark:bg-red-950' : 'border-[var(--color-border-secondary)] hover:border-red-300'}"
          >
            <Icon name="x-circle" size="sm" class="text-red-600 dark:text-red-400" />
            <span class="text-sm font-medium">Reject</span>
          </Button>
        </div>

        <!-- Conditions field (only for Conditional) -->
        {#if selectedDecision === 'Conditional'}
          <Textarea
            label="Conditions"
            required
            bind:value={conditions}
            placeholder="Specify the conditions that must be met for approval..."
            rows={2}
            class="mb-4"
          />
        {/if}

        <!-- Comments field -->
        <Textarea
          label="Comments"
          required={selectedDecision === 'Rejected'}
          bind:value={comments}
          placeholder={selectedDecision === 'Rejected' ? 'Please explain why this spec is being rejected...' : 'Add any additional comments...'}
          rows={3}
          class="mb-4"
        />
      </div>
    {/if}
  </div>

  {#snippet footer()}
    <div class="flex justify-end gap-2">
      <Button variant="ghost" onclick={handleClose}>
        Close
      </Button>
      {#if canSignOff}
        <Button
          variant="primary"
          onclick={handleSubmit}
          disabled={loading || !selectedDecision || (selectedDecision === 'Conditional' && !conditions.trim()) || (selectedDecision === 'Rejected' && !comments.trim())}
        >
          {#if loading}
            <Spinner size="lg" />
          {/if}
          Submit Sign-off
        </Button>
      {/if}
    </div>
  {/snippet}
</Modal>
