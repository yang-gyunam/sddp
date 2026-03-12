<!--
  SpecCard Component
  Card-style display for a Spec with key information
-->
<script lang="ts">
  import { Icon, Button } from '@sddp/ui';
  import { formatDate as formatDateUtil } from '@sddp/shell';
  import { SpecStatusBadge } from '../../../specs';
  import type { Spec } from '../../../specs/types';

  interface Props {
    spec: Spec;
    onclick?: () => void;
    class?: string;
  }

  let {
    spec,
    onclick,
    class: className = '',
  }: Props = $props();

  function formatDateStr(dateStr: string): string {
    return formatDateUtil(dateStr, { month: 'short' });
  }
</script>

<Button
  variant="unstyled"
  onclick={onclick}
  class="w-full text-left p-4 rounded-lg border border-[var(--color-border-primary)] bg-[var(--color-surface-50)] hover:bg-[var(--color-surface-200)] hover:border-[var(--color-accent-primary)] transition-all {className}"
>
  <!-- Header -->
  <div class="flex items-start justify-between gap-2 mb-3">
    <div class="flex items-center gap-2">
      <Icon name="file-signature" size="sm" class="text-[var(--color-accent-primary)]" />
      <span class="text-xs text-[var(--color-text-muted)] font-mono">
        {spec.code}
      </span>
      <span class="text-xs text-[var(--color-text-muted)]">
        v{spec.version}
      </span>
    </div>
    <SpecStatusBadge status={spec.status} showIcon={false} />
  </div>

  <!-- Title -->
  <h3 class="text-sm font-semibold text-[var(--color-text-primary)] mb-2 line-clamp-2">
    {spec.title}
  </h3>

  <!-- Description -->
  {#if spec.description}
    <p class="text-xs text-[var(--color-text-secondary)] mb-3 line-clamp-2">
      {spec.description}
    </p>
  {/if}

  <!-- Decision (if exists) -->
  {#if spec.decision}
    <div class="mb-3 p-2 rounded bg-[var(--color-surface-100)] border border-[var(--color-border-secondary)]">
      <div class="flex items-center gap-1 mb-1">
        <Icon name="check-circle" size="xs" class="text-[var(--color-success-500)]" />
        <span class="text-xs font-medium text-[var(--color-text-secondary)]">Decision</span>
      </div>
      <p class="text-xs text-[var(--color-text-secondary)] line-clamp-2">
        {spec.decision}
      </p>
    </div>
  {/if}

  <!-- Footer -->
  <div class="flex items-center justify-between gap-2 pt-3 border-t border-[var(--color-border-secondary)]">
    <div class="flex items-center gap-3 text-xs text-[var(--color-text-muted)]">
      {#if spec.requirementId}
        <span class="flex items-center gap-1">
          <Icon name="link" size="xs" />
          Linked Req
        </span>
      {/if}
      {#if spec.bornFromConversationId}
        <span class="flex items-center gap-1">
          <Icon name="message-square" size="xs" />
          From Conversation
        </span>
      {/if}
      {#if spec.lockedAt}
        <span class="flex items-center gap-1">
          <Icon name="lock" size="xs" />
          Locked
        </span>
      {/if}
    </div>
    <span class="text-xs text-[var(--color-text-muted)]">
      {formatDateStr(spec.updatedAt)}
    </span>
  </div>
</Button>

<style>
  .line-clamp-2 {
    display: -webkit-box;
    -webkit-line-clamp: 2;
    line-clamp: 2;
    -webkit-box-orient: vertical;
    overflow: hidden;
  }
</style>
