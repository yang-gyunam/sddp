<!--
  ProvenanceSection Idiom
  Displays ownership, authorship, and version/date metadata.
  Reusable across Requirements, Specs, Glossary, and Artifacts.
  Layout: paired rows (person + date side by side).
-->
<script lang="ts">
  import { Avatar, formatDateTime } from '@sddp/shell';

  interface Props {
    ownerName?: string | null;
    createdByName?: string | null;
    updatedByName?: string | null;
    definedByName?: string | null;
    approvedByName?: string | null;
    version?: string;
    createdAt?: string;
    updatedAt?: string;
    validFrom?: string;
    validTo?: string | null;
    lockedAt?: string | null;
    class?: string;
  }

  let {
    ownerName = null,
    createdByName = null,
    updatedByName = null,
    definedByName = null,
    approvedByName = null,
    version,
    createdAt,
    updatedAt,
    validFrom,
    validTo = null,
    lockedAt = null,
    class: className = '',
  }: Props = $props();

  function formatDateStr(dateStr: string): string {
    return formatDateTime(dateStr, { month: 'short' });
  }

  const hasContent = $derived(
    !!ownerName || !!createdByName || !!updatedByName || !!definedByName || !!approvedByName
    || !!version || !!createdAt || !!updatedAt || !!validFrom || !!validTo || !!lockedAt
  );
</script>

{#if hasContent}
  <div class="border-t border-[var(--color-border-secondary)] pt-4 {className}">
    <dl class="grid grid-cols-2 gap-4 text-sm">
      <!-- Owner -->
      {#if ownerName !== undefined}
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
      {/if}

      <!-- Defined By -->
      {#if definedByName}
        <div>
          <dt class="text-[var(--color-text-muted)]">Defined By</dt>
          <dd class="flex items-center gap-2 mt-1">
            <Avatar name={definedByName} size="xs" />
            <span class="text-[var(--color-text-primary)]">{definedByName}</span>
          </dd>
        </div>
      {/if}

      <!-- Approved By -->
      {#if approvedByName}
        <div>
          <dt class="text-[var(--color-text-muted)]">Approved By</dt>
          <dd class="flex items-center gap-2 mt-1">
            <Avatar name={approvedByName} size="xs" />
            <span class="text-[var(--color-text-primary)]">{approvedByName}</span>
          </dd>
        </div>
      {/if}

      <!-- Version -->
      {#if version}
        <div>
          <dt class="text-[var(--color-text-muted)]">Version</dt>
          <dd class="text-[var(--color-text-primary)]">{version}</dd>
        </div>
      {/if}

      <!-- Created By + Created (paired) -->
      {#if createdByName}
        <div>
          <dt class="text-[var(--color-text-muted)]">Created By</dt>
          <dd class="flex items-center gap-2 mt-1">
            <Avatar name={createdByName} size="xs" />
            <span class="text-[var(--color-text-primary)]">{createdByName}</span>
          </dd>
        </div>
      {/if}
      {#if createdAt}
        <div>
          <dt class="text-[var(--color-text-muted)]">Created</dt>
          <dd class="text-[var(--color-text-primary)]">{formatDateStr(createdAt)}</dd>
        </div>
      {/if}

      <!-- Updated By + Updated (paired) -->
      {#if updatedByName}
        <div>
          <dt class="text-[var(--color-text-muted)]">Updated By</dt>
          <dd class="flex items-center gap-2 mt-1">
            <Avatar name={updatedByName} size="xs" />
            <span class="text-[var(--color-text-primary)]">{updatedByName}</span>
          </dd>
        </div>
      {/if}
      {#if updatedAt}
        <div>
          <dt class="text-[var(--color-text-muted)]">Updated</dt>
          <dd class="text-[var(--color-text-primary)]">{formatDateStr(updatedAt)}</dd>
        </div>
      {/if}

      <!-- Valid From / Valid To -->
      {#if validFrom}
        <div>
          <dt class="text-[var(--color-text-muted)]">Valid From</dt>
          <dd class="text-[var(--color-text-primary)]">{formatDateStr(validFrom)}</dd>
        </div>
      {/if}
      {#if validTo}
        <div>
          <dt class="text-[var(--color-text-muted)]">Valid To</dt>
          <dd class="text-[var(--color-text-primary)]">{formatDateStr(validTo)}</dd>
        </div>
      {/if}

      <!-- Locked At -->
      {#if lockedAt}
        <div>
          <dt class="text-[var(--color-text-muted)]">Locked At</dt>
          <dd class="text-[var(--color-text-primary)]">{formatDateStr(lockedAt)}</dd>
        </div>
      {/if}
    </dl>
  </div>
{/if}
