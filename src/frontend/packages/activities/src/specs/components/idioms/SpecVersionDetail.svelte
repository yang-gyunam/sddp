<!--
  SpecVersionDetail Component
  Read-only display of a past spec version (used in version history tabs).
-->
<script lang="ts">
  import { Icon } from '@sddp/ui';
  import { formatDateTime } from '@sddp/shell';
  import DetailField from '../../../shared/components/idioms/DetailField.svelte';
  import { SPEC_STATUS_STYLES } from '../../types';
  import type { Spec } from '../../types';

  interface Props {
    version: Spec;
    class?: string;
  }

  let { version, class: className = '' }: Props = $props();

  const statusStyle = $derived(SPEC_STATUS_STYLES[version.status]);
</script>

<div class="space-y-4 {className}">
  <!-- Version header -->
  <div class="flex items-center gap-3">
    <Icon name="file-text" size="md" class="text-[var(--color-info-600)]" />
    <div class="min-w-0">
      <h3 class="text-sm font-semibold text-[var(--color-text-primary)] truncate">
        {version.title}
      </h3>
      <div class="flex items-center gap-2 mt-0.5">
        <span class="text-xs text-[var(--color-text-tertiary)] font-mono">{version.code}</span>
        <span class="text-xs text-[var(--color-text-muted)]">·</span>
        <span class="text-xs font-mono text-[var(--color-accent-primary)]">v{version.version}</span>
      </div>
    </div>
  </div>

  <!-- Status -->
  <DetailField label="Status">
    <span class="text-sm font-medium {statusStyle.textColor}">{statusStyle.label}</span>
  </DetailField>

  <!-- Description -->
  {#if version.description}
    <DetailField label="Description">
      <div class="prose prose-sm dark:prose-invert max-w-none text-[var(--color-text-primary)] whitespace-pre-wrap">
        {version.description}
      </div>
    </DetailField>
  {/if}

  <!-- Decision -->
  {#if version.decision}
    <DetailField label="Decision">
      <div class="prose prose-sm dark:prose-invert max-w-none text-[var(--color-text-primary)] whitespace-pre-wrap">
        {version.decision}
      </div>
    </DetailField>
  {/if}

  <!-- Metadata -->
  <div class="grid grid-cols-2 gap-3 text-sm">
    <DetailField label="Created">
      <span class="text-[var(--color-text-secondary)]">{formatDateTime(version.createdAt, { month: 'short' })}</span>
    </DetailField>
    <DetailField label="Updated">
      <span class="text-[var(--color-text-secondary)]">{formatDateTime(version.updatedAt, { month: 'short' })}</span>
    </DetailField>
  </div>
</div>
