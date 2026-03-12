<!--
  GlossaryVersionDetail Component
  Read-only display of a past glossary term version (used in version history tabs).
-->
<script lang="ts">
  import { Icon } from '@sddp/ui';
  import { formatDateTime } from '@sddp/shell';
  import DetailField from '../../../shared/components/idioms/DetailField.svelte';
  import { TermCategoryBadge, TermStatusBadge } from '../idioms';
  import type { GlossaryTermVersion } from '../../types';

  interface Props {
    version: GlossaryTermVersion;
    class?: string;
  }

  let { version, class: className = '' }: Props = $props();
</script>

<div class="space-y-4 {className}">
  <!-- Version header -->
  <div class="flex items-center gap-3">
    <Icon name="book-open" size="md" class="text-[var(--color-info-600)]" />
    <div class="min-w-0">
      <h3 class="text-sm font-semibold text-[var(--color-text-primary)] truncate">
        {version.term}
      </h3>
      <div class="flex items-center gap-2 mt-0.5">
        {#if version.abbreviation}
          <span class="text-xs text-[var(--color-text-tertiary)] font-mono">({version.abbreviation})</span>
          <span class="text-xs text-[var(--color-text-muted)]">·</span>
        {/if}
        <span class="text-xs font-mono text-[var(--color-accent-primary)]">v{version.version}</span>
      </div>
    </div>
  </div>

  <!-- Status -->
  <DetailField label="Status">
    <TermStatusBadge status={version.status} size="sm" />
  </DetailField>

  <!-- Definition -->
  <DetailField label="Definition">
    <div class="prose prose-sm dark:prose-invert max-w-none text-[var(--color-text-primary)] whitespace-pre-wrap">
      {version.definition}
    </div>
  </DetailField>

  <!-- Category -->
  <DetailField label="Category">
    <TermCategoryBadge category={version.category} size="sm" />
  </DetailField>

  <!-- Synonyms -->
  {#if version.synonyms}
    <DetailField label="Synonyms">
      <div class="flex flex-wrap gap-2">
        {#each version.synonyms.split(',').map(s => s.trim()).filter(Boolean) as synonym (synonym)}
          <span class="px-2 py-1 text-xs bg-[var(--color-surface-200)] text-[var(--color-text-secondary)] rounded">
            {synonym}
          </span>
        {/each}
      </div>
    </DetailField>
  {/if}

  <!-- Metadata -->
  <div class="grid grid-cols-2 gap-3 text-sm">
    <DetailField label="Valid From">
      <span class="text-[var(--color-text-secondary)]">{formatDateTime(version.validFrom, { month: 'short' })}</span>
    </DetailField>
    {#if version.validTo}
      <DetailField label="Valid To">
        <span class="text-[var(--color-text-secondary)]">{formatDateTime(version.validTo, { month: 'short' })}</span>
      </DetailField>
    {/if}
    <DetailField label="Created">
      <span class="text-[var(--color-text-secondary)]">{formatDateTime(version.createdAt, { month: 'short' })}</span>
    </DetailField>
    <DetailField label="Updated">
      <span class="text-[var(--color-text-secondary)]">{formatDateTime(version.updatedAt, { month: 'short' })}</span>
    </DetailField>
    {#if version.updatedBy?.name}
      <DetailField label="Updated By">
        <span class="text-[var(--color-text-secondary)]">{version.updatedBy.name}</span>
      </DetailField>
    {/if}
  </div>
</div>
