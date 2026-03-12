<!-- Section: SpecDetailDocumentationTab — Specs > SpecDetailView -->
<script lang="ts">
  import { Button, Icon, Spinner } from '@sddp/ui';
  import { renderMarkdown } from '@sddp/shell';
  import type { SpecSummaryResult } from '../../../types';

  type DocViewMode = 'preview' | 'markdown';

  interface Props {
    isReadOnly: boolean;
    docMarkdown: string | null;
    docLoading: boolean;
    docError: string | null;
    docViewMode: DocViewMode;
    summaryResult: SpecSummaryResult | null;
    summaryLoading: boolean;
    summaryError: string | null;
    formatDateStr: (dateStr: string) => string;
    onLoadDocMarkdown: () => void | Promise<void>;
    onLoadSummary: (refresh?: boolean) => void | Promise<void>;
    onDownloadDocMarkdown: () => void;
    onDocViewModeChange: (mode: DocViewMode) => void;
  }

  let {
    isReadOnly,
    docMarkdown,
    docLoading,
    docError,
    docViewMode,
    summaryResult,
    summaryLoading,
    summaryError,
    formatDateStr,
    onLoadDocMarkdown,
    onLoadSummary,
    onDownloadDocMarkdown,
    onDocViewModeChange,
  }: Props = $props();
</script>

<div class="rounded-lg border border-[var(--color-border-secondary)] bg-[var(--color-surface-50)] p-4 space-y-3">
  <div class="flex items-start justify-between gap-3">
    <div>
      <h3 class="text-sm font-medium text-[var(--color-text-secondary)]">Documentation</h3>
      <p class="text-xs text-[var(--color-text-muted)]">
        Generate and preview spec documentation.
      </p>
    </div>
    {#if !isReadOnly}
      <div class="flex items-center gap-2">
        <Button variant="ghost" size="sm" onclick={onLoadDocMarkdown}>
          <Icon name="file-text" size="sm" />
          Generate Doc
        </Button>
        <Button variant="ghost" size="sm" onclick={() => onLoadSummary(true)}>
          <Icon name="sparkles" size="sm" />
          Refresh Summary
        </Button>
        <Button variant="ghost" size="sm" onclick={onDownloadDocMarkdown} disabled={!docMarkdown}>
          <Icon name="download" size="sm" />
          Download
        </Button>
      </div>
    {/if}
  </div>

  {#if summaryLoading}
    <div class="flex-1 flex items-center justify-center">
      <Spinner size="lg" />
    </div>
  {:else if summaryError}
    <div class="text-xs text-[var(--color-error-600)]">{summaryError}</div>
  {:else if summaryResult}
    <div class="rounded-lg border border-[var(--color-border-secondary)] bg-[var(--color-surface-100)] p-3 space-y-2">
      <div class="flex items-center justify-between text-xs text-[var(--color-text-tertiary)]">
        <span class="flex items-center gap-1">
          {#if summaryResult.modelUsed}<Icon name="sparkles" size="xs" />{/if}
          {summaryResult.modelUsed ? 'AI Summary' : 'Summary'} {summaryResult.fromCache ? '(cached)' : '(fresh)'}
        </span>
        <span>{formatDateStr(summaryResult.generatedAt)}</span>
      </div>
      <div class="text-sm text-[var(--color-text-primary)] whitespace-pre-wrap">
        {summaryResult.summary || 'Summary not available yet.'}
      </div>
    </div>
  {/if}

  <div class="flex items-center gap-2 text-xs">
    <Button
      variant="unstyled"
      type="button"
      class="px-2 py-1 rounded-md border text-[var(--color-text-secondary)]
        {docViewMode === 'preview'
          ? 'border-[var(--color-accent-primary)] bg-[var(--color-accent-primary)]/10'
          : 'border-[var(--color-border-secondary)]'}"
      onclick={() => onDocViewModeChange('preview')}
    >
      Preview
    </Button>
    <Button
      variant="unstyled"
      type="button"
      class="px-2 py-1 rounded-md border text-[var(--color-text-secondary)]
        {docViewMode === 'markdown'
          ? 'border-[var(--color-accent-primary)] bg-[var(--color-accent-primary)]/10'
          : 'border-[var(--color-border-secondary)]'}"
      onclick={() => onDocViewModeChange('markdown')}
    >
      Markdown
    </Button>
  </div>

  {#if docLoading}
    <div class="flex-1 flex items-center justify-center">
      <Spinner size="lg" />
    </div>
  {:else if docError}
    <div class="text-xs text-[var(--color-error-600)]">{docError}</div>
  {:else if docMarkdown}
    {#if docViewMode === 'preview'}
      <div class="prose prose-sm dark:prose-invert max-w-none text-[var(--color-text-primary)]">
        <!-- eslint-disable-next-line svelte/no-at-html-tags -->
        {@html renderMarkdown(docMarkdown)}
      </div>
    {:else}
      <pre class="text-xs bg-[var(--color-surface-200)] border border-[var(--color-border-secondary)] rounded p-3 overflow-auto">{docMarkdown}</pre>
    {/if}
  {:else}
    <div class="text-xs text-[var(--color-text-tertiary)]">
      No document generated yet.
    </div>
  {/if}
</div>
