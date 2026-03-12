<!-- Section: SpecContent — Specs > Global -->
<script lang="ts">
  import { Icon, Button } from '@sddp/ui';
  import { formatDate } from '@sddp/shell';
  import LinkedEntityCard from '../../../shared/components/idioms/LinkedEntityCard.svelte';
  import type { SpecDetail } from '../../types';
  import { SPEC_STATUS_STYLES } from '../../types';
  import { SpecStatusBadge } from '../..';

  interface Props {
    spec: SpecDetail | null;
    loading?: boolean;
    onEdit?: () => void;
    onSubmitForReview?: () => void;
    onApprove?: () => void;
    onReject?: () => void;
    onLock?: () => void;
    onSignOff?: () => void;
    class?: string;
  }

  let {
    spec,
    loading = false,
    onEdit,
    onSubmitForReview,
    onApprove,
    onReject,
    onLock,
    onSignOff,
    class: className = '',
  }: Props = $props();

  const isEditable = $derived(spec?.status === 'Draft');
  const canSubmit = $derived(spec?.status === 'Draft');
  const canApprove = $derived(spec?.status === 'InReview');
  const canLock = $derived(spec?.status === 'Approved');
</script>

<div class="spec-content flex flex-col h-full bg-[var(--color-bg-primary)] {className}">
  {#if loading}
    <!-- Loading State -->
    <div class="flex-1 flex items-center justify-center">
      <div class="text-center">
        <Icon name="loading" size="xl" class="animate-spin text-[var(--color-text-tertiary)]" />
        <p class="mt-2 text-sm text-[var(--color-text-tertiary)]">Loading spec...</p>
      </div>
    </div>
  {:else if spec}
    <!-- Header -->
    <div class="content-header flex-shrink-0 p-4 border-b border-[var(--color-border-primary)]">
      <div class="flex items-start justify-between gap-4">
        <div class="flex-1 min-w-0">
          <div class="flex items-center gap-2 mb-2">
            <span class="text-sm font-mono text-[var(--color-text-tertiary)]">
              {spec.code}
            </span>
            <span class="text-xs text-[var(--color-text-tertiary)]">
              v{spec.version}
            </span>
          </div>
          <h1 class="text-xl font-semibold text-[var(--color-text-primary)]">
            {spec.title}
          </h1>
        </div>

        <SpecStatusBadge status={spec.status} />
      </div>

      <!-- Actions -->
      <div class="flex items-center gap-2 mt-4">
        {#if isEditable}
          <Button
            variant="unstyled"
            class="flex items-center gap-1.5 px-3 py-1.5 text-sm rounded
                   bg-[var(--color-accent-primary)] text-white
                   hover:bg-[var(--color-accent-primary)]/90"
            onclick={() => onEdit?.()}
          >
            <Icon name="edit" size="xs" />
            <span>Edit</span>
          </Button>
        {/if}

        {#if canSubmit}
          <Button
            variant="unstyled"
            class="flex items-center gap-1.5 px-3 py-1.5 text-sm rounded
                   bg-[var(--color-bg-tertiary)] text-[var(--color-text-primary)]
                   hover:bg-[var(--color-bg-tertiary)]/80"
            onclick={() => onSubmitForReview?.()}
          >
            <Icon name="send" size="xs" />
            <span>Submit for Review</span>
          </Button>
        {/if}

        {#if canApprove}
          <Button
            variant="unstyled"
            class="flex items-center gap-1.5 px-3 py-1.5 text-sm rounded
                   bg-[var(--color-success-500)] text-white hover:bg-[var(--color-success-500)]/90"
            onclick={() => onApprove?.()}
          >
            <Icon name="check" size="xs" />
            <span>Approve</span>
          </Button>
          <Button
            variant="unstyled"
            class="flex items-center gap-1.5 px-3 py-1.5 text-sm rounded
                   bg-[var(--color-error-500)] text-white hover:bg-[var(--color-error-500)]/90"
            onclick={() => onReject?.()}
          >
            <Icon name="x" size="xs" />
            <span>Reject</span>
          </Button>
          <Button
            variant="unstyled"
            class="flex items-center gap-1.5 px-3 py-1.5 text-sm rounded
                   bg-[var(--color-bg-tertiary)] text-[var(--color-text-primary)]
                   hover:bg-[var(--color-bg-tertiary)]/80"
            onclick={() => onSignOff?.()}
          >
            <Icon name="badge-check" size="xs" />
            <span>Sign Off</span>
          </Button>
        {/if}

        {#if canLock}
          <Button
            variant="unstyled"
            class="flex items-center gap-1.5 px-3 py-1.5 text-sm rounded
                   bg-[var(--color-bg-tertiary)] text-[var(--color-text-primary)]
                   hover:bg-[var(--color-bg-tertiary)]/80"
            onclick={() => onLock?.()}
          >
            <Icon name="lock" size="xs" />
            <span>Lock Version</span>
          </Button>
        {/if}
      </div>
    </div>

    <!-- Content Body -->
    <div class="content-body flex-1 overflow-y-auto p-4">
      <!-- Decision -->
      {#if spec.decision}
        <section class="mb-6">
          <h2 class="text-sm font-semibold text-[var(--color-text-primary)] mb-2 flex items-center gap-2">
            <Icon name="lightbulb" size="xs" />
            Decision
          </h2>
          <div class="prose prose-sm dark:prose-invert max-w-none p-3 rounded
                      bg-[var(--color-bg-secondary)] border border-[var(--color-border-primary)]">
            {spec.decision}
          </div>
        </section>
      {/if}

      <!-- Description -->
      {#if spec.description}
        <section class="mb-6">
          <h2 class="text-sm font-semibold text-[var(--color-text-primary)] mb-2 flex items-center gap-2">
            <Icon name="note" size="xs" />
            Description
          </h2>
          <div class="prose prose-sm dark:prose-invert max-w-none p-3 rounded
                      bg-[var(--color-bg-secondary)] border border-[var(--color-border-primary)]">
            {spec.description}
          </div>
        </section>
      {/if}

      <!-- Context -->
      {#if spec.context}
        <section class="mb-6">
          <h2 class="text-sm font-semibold text-[var(--color-text-primary)] mb-2 flex items-center gap-2">
            <Icon name="info" size="xs" />
            Context
          </h2>
          <div class="prose prose-sm dark:prose-invert max-w-none p-3 rounded
                      bg-[var(--color-bg-secondary)] border border-[var(--color-border-primary)]">
            {spec.context}
          </div>
        </section>
      {/if}

      <!-- Acceptance Criteria -->
      {#if spec.acceptanceCriteria}
        <section class="mb-6">
          <h2 class="text-sm font-semibold text-[var(--color-text-primary)] mb-2 flex items-center gap-2">
            <Icon name="check-circle" size="xs" />
            Acceptance Criteria
          </h2>
          <div class="prose prose-sm dark:prose-invert max-w-none p-3 rounded
                      bg-[var(--color-bg-secondary)] border border-[var(--color-border-primary)]">
            {spec.acceptanceCriteria}
          </div>
        </section>
      {/if}

      <!-- Linked Requirement -->
      {#if spec.requirementId}
        <section class="mb-6">
          <h2 class="text-sm font-semibold text-[var(--color-text-primary)] mb-2 flex items-center gap-2">
            <Icon name="link" size="xs" />
            Linked Requirement
          </h2>
          <LinkedEntityCard
            icon="file-text"
            label="Requirement #{spec.requirementId.slice(0, 8)}"
          />
        </section>
      {/if}

      <!-- Metadata -->
      <section class="mb-6">
        <h2 class="text-sm font-semibold text-[var(--color-text-primary)] mb-2 flex items-center gap-2">
          <Icon name="info" size="xs" />
          Metadata
        </h2>
        <div class="grid grid-cols-2 gap-4 text-sm">
          <div>
            <span class="text-[var(--color-text-tertiary)]">Created:</span>
            <span class="ml-2">{formatDate(spec.createdAt)}</span>
          </div>
          <div>
            <span class="text-[var(--color-text-tertiary)]">Updated:</span>
            <span class="ml-2">{formatDate(spec.updatedAt)}</span>
          </div>
          <div>
            <span class="text-[var(--color-text-tertiary)]">Version:</span>
            <span class="ml-2">{spec.version}</span>
          </div>
          <div>
            <span class="text-[var(--color-text-tertiary)]">Status:</span>
            <span class="ml-2">{SPEC_STATUS_STYLES[spec.status].label}</span>
          </div>
        </div>
      </section>
    </div>
  {:else}
    <!-- Empty State -->
    <div class="flex-1 flex items-center justify-center">
      <div class="text-center">
        <Icon name="file-code" size="xl" class="mx-auto mb-3 text-[var(--color-text-tertiary)] opacity-50" />
        <p class="text-sm font-medium text-[var(--color-text-primary)]">Select a spec</p>
        <p class="text-xs text-[var(--color-text-secondary)] mt-1">
          Choose a spec from the sidebar to view details.
        </p>
      </div>
    </div>
  {/if}
</div>
