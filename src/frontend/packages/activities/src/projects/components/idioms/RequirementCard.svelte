<!--
  RequirementCard Component
  Card-style display for a Requirement with key information
-->
<script lang="ts">
  import { Icon, Button } from '@sddp/ui';
  import { formatDate as formatDateUtil } from '@sddp/shell';
  import { RequirementLevelBadge, RequirementStatusBadge } from '../../../requirements';
  import type { Requirement } from '../../../requirements/types';

  interface Props {
    requirement: Requirement;
    onclick?: () => void;
    class?: string;
  }

  let {
    requirement,
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
      <Icon name="clipboard-list" size="sm" class="text-[var(--color-accent-primary)]" />
      <span class="text-xs text-[var(--color-text-muted)] font-mono">
        {requirement.code}
      </span>
      <RequirementLevelBadge level={requirement.level} showIcon={false} />
    </div>
    <RequirementStatusBadge status={requirement.status} showIcon={false} />
  </div>

  <!-- Title -->
  <h3 class="text-sm font-semibold text-[var(--color-text-primary)] mb-2 line-clamp-2">
    {requirement.title}
  </h3>

  <!-- Description -->
  {#if requirement.description}
    <p class="text-xs text-[var(--color-text-secondary)] mb-3 line-clamp-3">
      {requirement.description}
    </p>
  {/if}

  <!-- Acceptance Criteria Count -->
  {#if requirement.acceptanceCriteria && requirement.acceptanceCriteria.length > 0}
    <div class="mb-3 flex items-center gap-1 text-xs text-[var(--color-text-muted)]">
      <Icon name="check-square" size="xs" />
      <span>{requirement.acceptanceCriteria.length} acceptance criteria</span>
    </div>
  {/if}

  <!-- Footer -->
  <div class="flex items-center justify-between gap-2 pt-3 border-t border-[var(--color-border-secondary)]">
    <div class="flex items-center gap-3 text-xs text-[var(--color-text-muted)]">
      {#if requirement.parentId}
        <span class="flex items-center gap-1">
          <Icon name="git-branch" size="xs" />
          Child
        </span>
      {/if}
      {#if requirement.bornFromConversationId}
        <span class="flex items-center gap-1">
          <Icon name="message-square" size="xs" />
          From Conversation
        </span>
      {/if}
    </div>
    <span class="text-xs text-[var(--color-text-muted)]">
      {formatDateStr(requirement.updatedAt)}
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

  .line-clamp-3 {
    display: -webkit-box;
    -webkit-line-clamp: 3;
    line-clamp: 3;
    -webkit-box-orient: vertical;
    overflow: hidden;
  }
</style>
