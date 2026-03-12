<!--
  RequirementContentBody Component (Idiom)
  Shared content body for requirement detail views.
  Renders: Description, People (Owner, Created By, Updated By), Metadata grid.
  Used by RequirementDetailView (full) and RequirementVersionDetail (read-only).
-->
<script lang="ts">
  import { Avatar, PriorityBadge, formatDateTime } from '@sddp/shell';
  import type { RequirementStatus, RequirementPriority } from '../../types';
  import { REQUIREMENT_STATUS_STYLES } from '../../types';

  interface Props {
    description?: string;
    showDescription?: boolean;
    status: RequirementStatus;
    priority: RequirementPriority;
    version: string;
    ownerName?: string | null;
    createdByName?: string | null;
    updatedByName?: string | null;
    createdAt: string;
    updatedAt: string;
    validFrom: string;
    validTo?: string | null;
    class?: string;
  }

  let {
    description = '',
    showDescription = true,
    status,
    priority,
    version,
    ownerName = null,
    createdByName = null,
    updatedByName = null,
    createdAt,
    updatedAt,
    validFrom,
    validTo = null,
    class: className = '',
  }: Props = $props();

  function formatDateStr(dateStr: string): string {
    return formatDateTime(dateStr, { month: 'short' });
  }
</script>

<div class="space-y-6 {className}">
  <!-- Description (optional, hidden when parent renders its own) -->
  {#if showDescription}
    <div>
      <h3 class="text-sm font-medium text-[var(--color-text-secondary)] mb-2">Description</h3>
      <div class="prose prose-sm dark:prose-invert max-w-none text-[var(--color-text-primary)] whitespace-pre-wrap">
        {description || 'No description provided.'}
      </div>
    </div>
  {/if}

  <!-- People -->
  <div class="pt-4 border-t border-[var(--color-border-secondary)]">
    <dl class="grid grid-cols-2 gap-4 text-sm">
      <div>
        <dt class="text-[var(--color-text-muted)]">Owner</dt>
        <dd class="flex items-center gap-2 mt-1">
          {#if ownerName}
            <Avatar name={ownerName} size="xs" />
            <span class="text-[var(--color-text-primary)]">{ownerName}</span>
          {:else}
            <span class="text-[var(--color-text-muted)]">Unassigned</span>
          {/if}
        </dd>
      </div>
      <div>
        <dt class="text-[var(--color-text-muted)]">Created By</dt>
        <dd class="flex items-center gap-2 mt-1">
          {#if createdByName}
            <Avatar name={createdByName} size="xs" />
            <span class="text-[var(--color-text-primary)]">{createdByName}</span>
          {:else}
            <span class="text-[var(--color-text-muted)]">Unknown</span>
          {/if}
        </dd>
      </div>
      {#if updatedByName}
        <div>
          <dt class="text-[var(--color-text-muted)]">Updated By</dt>
          <dd class="flex items-center gap-2 mt-1">
            <Avatar name={updatedByName} size="xs" />
            <span class="text-[var(--color-text-primary)]">{updatedByName}</span>
          </dd>
        </div>
      {/if}
    </dl>
  </div>

  <!-- Metadata -->
  <div class="pt-4 border-t border-[var(--color-border-secondary)]">
    <dl class="grid grid-cols-2 gap-4 text-sm">
      <div>
        <dt class="text-[var(--color-text-muted)]">Status</dt>
        <dd class="text-xs font-medium {REQUIREMENT_STATUS_STYLES[status].textColor}">
          {REQUIREMENT_STATUS_STYLES[status].label}
        </dd>
      </div>
      <div>
        <dt class="text-[var(--color-text-muted)]">Priority</dt>
        <dd>
          <PriorityBadge {priority} />
        </dd>
      </div>
      <div>
        <dt class="text-[var(--color-text-muted)]">Version</dt>
        <dd class="text-[var(--color-text-primary)]">{version}</dd>
      </div>
      <div>
        <dt class="text-[var(--color-text-muted)]">Created</dt>
        <dd class="text-[var(--color-text-primary)]">{formatDateStr(createdAt)}</dd>
      </div>
      <div>
        <dt class="text-[var(--color-text-muted)]">Updated</dt>
        <dd class="text-[var(--color-text-primary)]">{formatDateStr(updatedAt)}</dd>
      </div>
      <div>
        <dt class="text-[var(--color-text-muted)]">Valid From</dt>
        <dd class="text-[var(--color-text-primary)]">{formatDateStr(validFrom)}</dd>
      </div>
      {#if validTo}
        <div>
          <dt class="text-[var(--color-text-muted)]">Valid To</dt>
          <dd class="text-[var(--color-text-primary)]">{formatDateStr(validTo)}</dd>
        </div>
      {/if}
    </dl>
  </div>
</div>
