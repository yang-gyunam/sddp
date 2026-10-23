<script lang="ts">
  import type { Extensions } from '@tiptap/core';
  import { Button, Spinner } from '@sddp/ui';
  import TiptapEditor from '../../../shared/components/controls/TiptapEditor.svelte';
  import type {
    KnowledgePage,
    KnowledgePageVersion,
    KnowledgePageVersionSummary,
  } from '../../types';

  interface Props {
    page: KnowledgePage;
    versions: KnowledgePageVersionSummary[];
    loading: boolean;
    comparedVersion: KnowledgePageVersion | null;
    comparedVersionHtml: string;
    currentVersionHtml: string;
    canRestore: boolean;
    extensions: Extensions;
    onCompare: (revision: number) => void;
    onRestore: (revision: number) => void;
  }

  let {
    page,
    versions,
    loading,
    comparedVersion,
    comparedVersionHtml,
    currentVersionHtml,
    canRestore,
    extensions,
    onCompare,
    onRestore,
  }: Props = $props();

  function formatDate(value: string): string {
    return new Intl.DateTimeFormat(undefined, {
      dateStyle: 'medium',
      timeStyle: 'short',
    }).format(new Date(value));
  }
</script>

<section
  class="mt-6 border-t border-[var(--color-border-primary)] pt-4"
  aria-label="Revision history"
>
  <h2 class="mb-3 text-sm font-semibold text-[var(--color-text-primary)]">Revision history</h2>
  {#if loading}
    <Spinner size="md" />
  {:else}
    <div class="space-y-1">
      {#each versions as version (version.id)}
        <div
          class="flex items-center gap-3 rounded border border-[var(--color-border-primary)] px-3 py-2"
        >
          <span class="w-12 text-sm font-medium text-[var(--color-text-primary)]">
            r{version.revision}
          </span>
          <div class="min-w-0 flex-1">
            <div class="truncate text-sm text-[var(--color-text-primary)]">
              {version.changeSummary || version.title}
            </div>
            <div class="text-xs text-[var(--color-text-tertiary)]">
              {formatDate(version.createdAt)} · {version.status}
            </div>
          </div>
          <Button variant="ghost" size="sm" onclick={() => onCompare(version.revision)}>
            Compare
          </Button>
          {#if canRestore && version.revision !== page.revision && page.status !== 'Archived'}
            <Button variant="ghost" size="sm" onclick={() => onRestore(version.revision)}>
              Restore
            </Button>
          {/if}
        </div>
      {/each}
    </div>
    {#if comparedVersion}
      <div
        class="mt-4 grid grid-cols-1 gap-3 xl:grid-cols-2"
        aria-label={`Comparison with revision ${comparedVersion.revision}`}
      >
        <div class="min-w-0">
          <div class="mb-2 text-xs font-semibold text-[var(--color-text-secondary)]">
            Revision {comparedVersion.revision} · {formatDate(comparedVersion.createdAt)}
          </div>
          <div class="knowledge-comparison rounded border border-[var(--color-border-primary)]">
            <TiptapEditor
              content={comparedVersionHtml}
              editable={false}
              {extensions}
              ariaLabel={`Revision ${comparedVersion.revision} content`}
            />
          </div>
        </div>
        <div class="min-w-0">
          <div class="mb-2 text-xs font-semibold text-[var(--color-text-secondary)]">
            Current revision {page.revision}
          </div>
          <div class="knowledge-comparison rounded border border-[var(--color-border-primary)]">
            <TiptapEditor
              content={currentVersionHtml}
              editable={false}
              {extensions}
              ariaLabel="Current revision content"
            />
          </div>
        </div>
      </div>
    {/if}
  {/if}
</section>

<style>
  .knowledge-comparison :global(.ProseMirror) {
    min-height: 12rem;
    max-height: 28rem;
    overflow: auto;
    padding: 1rem;
  }
</style>
