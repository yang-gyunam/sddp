<!-- Section: TermContent — Glossary > Global -->
<script lang="ts">
  import { Icon, Button, Spinner } from '@sddp/ui';
  import { formatDate as formatDateUtil, renderMarkdown } from '@sddp/shell';
  import { TermStatusBadge, TermCategoryBadge } from '../..';  // From glossary index
  import type { GlossaryTermDetail } from '../../types';
  import { TERM_CATEGORY_STYLES } from '../../types';

  interface Props {
    term: GlossaryTermDetail | null;
    loading?: boolean;
    onEdit?: () => void;
    onApprove?: () => void;
    onDeprecate?: () => void;
    onReactivate?: () => void;
    /** Hide internal header (for Pattern B layout where header is rendered externally) */
    showHeader?: boolean;
    class?: string;
  }

  let {
    term,
    loading = false,
    onEdit,
    onApprove,
    onDeprecate,
    onReactivate,
    showHeader = true,
    class: className = '',
  }: Props = $props();

  const categoryStyle = $derived(term ? TERM_CATEGORY_STYLES[term.category] : null);

  function formatDateStr(dateStr: string): string {
    return formatDateUtil(dateStr, { month: 'short' });
  }

</script>

<div class="flex flex-col h-full bg-[var(--color-bg-primary)] {className}">
  {#if loading}
    <!-- Loading state -->
    <div class="flex-1 flex items-center justify-center">
      <div class="flex flex-col items-center gap-3 text-[var(--color-text-tertiary)]">
        <Spinner size="lg" />
        <span class="text-sm">Loading term...</span>
      </div>
    </div>
  {:else if !term}
    <!-- Empty state -->
    <div class="flex items-center justify-center h-full">
      <div class="text-center">
        <Icon name="book-open" size="xl" class="mx-auto mb-2 text-[var(--color-text-tertiary)] opacity-50" />
        <p class="text-sm font-medium text-[var(--color-text-primary)]">Select a term</p>
        <p class="text-xs text-[var(--color-text-secondary)] mt-1">
          Choose a term from the sidebar to view details.
        </p>
      </div>
    </div>
  {:else}
    {#if showHeader}
    <!-- Header -->
    <div class="flex-shrink-0 p-4 border-b border-[var(--color-border-primary)]">
      <div class="flex items-start justify-between">
        <div class="flex-1 min-w-0">
          <div class="flex items-center gap-2 mb-2">
            {#if categoryStyle}
              <span class={categoryStyle.color}>
                <Icon name={categoryStyle.icon} size="md" />
              </span>
            {/if}
            <h1 class="text-xl font-bold text-[var(--color-text-primary)] truncate">
              {term.term}
            </h1>
            {#if term.abbreviation}
              <span class="text-sm text-[var(--color-text-tertiary)]">
                ({term.abbreviation})
              </span>
            {/if}
            <span class="text-xs text-[var(--color-text-tertiary)]">v{term.version}</span>
          </div>
          <div class="flex items-center gap-2">
            <TermCategoryBadge category={term.category} size="sm" />
            <TermStatusBadge status={term.status} size="sm" />
          </div>
        </div>

        <!-- Actions -->
        <div class="flex items-center gap-2 ml-4">
          {#if term.status === 'Draft'}
            {#if onApprove}
              <Button size="sm" variant="primary" onclick={onApprove}>
                <Icon name="check" size="sm" />
                <span class="ml-1">Approve</span>
              </Button>
            {/if}
          {:else if term.status === 'Active'}
            {#if onDeprecate}
              <Button size="sm" variant="ghost" onclick={onDeprecate}>
                <Icon name="archive" size="sm" />
                <span class="ml-1">Deprecate</span>
              </Button>
            {/if}
          {:else if term.status === 'Deprecated'}
            {#if onReactivate}
              <Button size="sm" variant="ghost" onclick={onReactivate}>
                <Icon name="refresh-cw" size="sm" />
                <span class="ml-1">Reactivate</span>
              </Button>
            {/if}
          {/if}

          {#if term.status !== 'Deprecated' && onEdit}
            <Button size="sm" variant="secondary" onclick={onEdit}>
              <Icon name="edit" size="sm" />
              <span class="ml-1">Edit</span>
            </Button>
          {/if}
        </div>
      </div>
    </div>
    {/if}

    <!-- Content -->
    <div class="flex-1 overflow-y-auto p-4 space-y-6">
      <!-- Definition -->
      <section>
        <h3 class="text-sm font-semibold text-[var(--color-text-secondary)] mb-2 flex items-center gap-2">
          <Icon name="file-text" size="sm" class="text-[var(--color-text-tertiary)]" />
          Definition
        </h3>
        <div class="prose prose-sm dark:prose-invert max-w-none text-[var(--color-text-secondary)]
          p-3 bg-[var(--color-bg-secondary)] rounded-lg">
          <!-- eslint-disable-next-line svelte/no-at-html-tags -->
          {@html renderMarkdown(term.definition)}
        </div>
      </section>

      <!-- Synonyms -->
      {#if term.synonyms}
        <section>
          <h3 class="text-sm font-semibold text-[var(--color-text-secondary)] mb-2 flex items-center gap-2">
            <Icon name="copy" size="sm" class="text-[var(--color-text-tertiary)]" />
            Synonyms
          </h3>
          <div class="flex flex-wrap gap-2">
            {#each term.synonyms.split(',').map(s => s.trim()).filter(Boolean) as synonym (synonym)}
              <span class="px-2 py-1 text-sm bg-[var(--color-bg-tertiary)] text-[var(--color-text-secondary)] rounded-md">
                {synonym}
              </span>
            {/each}
          </div>
        </section>
      {/if}

      <!-- Usage Examples -->
      {#if term.usageExamples && term.usageExamples.length > 0}
        <section>
          <h3 class="text-sm font-semibold text-[var(--color-text-secondary)] mb-2 flex items-center gap-2">
            <Icon name="quote" size="sm" class="text-[var(--color-text-tertiary)]" />
            Usage Examples
          </h3>
          <ul class="space-y-2">
            {#each term.usageExamples as example, i (i)}
              <li class="flex items-start gap-2 p-2 bg-[var(--color-bg-secondary)] rounded-lg">
                <Icon name="chevron-right" size="sm" class="mt-0.5 text-[var(--color-text-tertiary)] flex-shrink-0" />
                <span class="text-sm text-[var(--color-text-secondary)] italic">{example}</span>
              </li>
            {/each}
          </ul>
        </section>
      {/if}

      <!-- Source -->
      {#if term.source}
        <section>
          <h3 class="text-sm font-semibold text-[var(--color-text-secondary)] mb-2 flex items-center gap-2">
            <Icon name="external-link" size="sm" class="text-[var(--color-text-tertiary)]" />
            Source
          </h3>
          <p class="text-sm text-[var(--color-text-secondary)]">
            {#if term.source.startsWith('http')}
              <a
                href={term.source}
                target="_blank"
                rel="noopener noreferrer"
                class="text-[var(--color-accent-primary)] hover:underline"
              >
                {term.source}
              </a>
            {:else}
              {term.source}
            {/if}
          </p>
        </section>
      {/if}

      <!-- Metadata -->
      <section class="pt-4 border-t border-[var(--color-border-primary)]">
        <h3 class="text-sm font-semibold text-[var(--color-text-secondary)] mb-3 flex items-center gap-2">
          <Icon name="info" size="sm" class="text-[var(--color-text-tertiary)]" />
          Metadata
        </h3>
        <div class="grid grid-cols-2 gap-3 text-sm">
          <div class="flex items-center gap-2 text-[var(--color-text-secondary)]">
            <Icon name="user" size="sm" class="text-[var(--color-text-tertiary)]" />
            <span class="text-[var(--color-text-tertiary)]">Defined by:</span>
            <span>{term.definedBy?.name || 'Unknown'}</span>
          </div>
          {#if term.approvedBy?.name}
            <div class="flex items-center gap-2 text-[var(--color-text-secondary)]">
              <Icon name="check-circle" size="sm" class="text-[var(--color-text-tertiary)]" />
              <span class="text-[var(--color-text-tertiary)]">Approved by:</span>
              <span>{term.approvedBy.name}</span>
            </div>
          {/if}
          <div class="flex items-center gap-2 text-[var(--color-text-secondary)]">
            <Icon name="calendar" size="sm" class="text-[var(--color-text-tertiary)]" />
            <span class="text-[var(--color-text-tertiary)]">Created:</span>
            <span>{formatDateStr(term.createdAt)}</span>
          </div>
          <div class="flex items-center gap-2 text-[var(--color-text-secondary)]">
            <Icon name="clock" size="sm" class="text-[var(--color-text-tertiary)]" />
            <span class="text-[var(--color-text-tertiary)]">Updated:</span>
            <span>{formatDateStr(term.updatedAt)}</span>
          </div>
        </div>
      </section>
    </div>
  {/if}
</div>
