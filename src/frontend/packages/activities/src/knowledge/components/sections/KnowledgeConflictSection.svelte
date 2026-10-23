<script lang="ts">
  import type { Extensions } from '@tiptap/core';
  import { Button } from '@sddp/ui';
  import TiptapEditor from '../../../shared/components/controls/TiptapEditor.svelte';
  import type { KnowledgeConflictState } from '../../state';

  interface Props {
    conflict: Extract<KnowledgeConflictState, { kind: 'active' }>;
    extensions: Extensions;
    onLoadServer: () => void;
    onApplyMine: () => void;
  }

  let { conflict, extensions, onLoadServer, onApplyMine }: Props = $props();
</script>

<section
  class="mb-4 rounded border border-[var(--color-warning-500)]/40 bg-[var(--color-warning-500)]/10 p-3"
  role="alert"
  aria-live="polite"
  aria-label="Knowledge edit conflict"
>
  <div class="flex flex-wrap items-start justify-between gap-3">
    <div>
      <h2 class="text-sm font-semibold text-[var(--color-text-primary)]">
        This page changed on the server
      </h2>
      <p class="mt-1 text-xs text-[var(--color-text-secondary)]">
        Compare your local edits with server revision {conflict.serverPage.revision}. Your editor
        content has not been discarded.
      </p>
    </div>
    <div class="flex flex-wrap gap-1">
      <Button variant="ghost" size="sm" onclick={onLoadServer}>Load server version</Button>
      {#if conflict.serverPage.status !== 'Archived'}
        <Button
          variant="secondary"
          size="sm"
          aria-label="Apply my changes to latest"
          onclick={onApplyMine}
        >
          Apply mine to r{conflict.serverPage.revision}
        </Button>
      {/if}
    </div>
  </div>
  <div class="mt-3 grid grid-cols-1 gap-3 xl:grid-cols-2">
    <div class="min-w-0">
      <div class="mb-1 text-xs font-semibold text-[var(--color-text-secondary)]">
        Server · r{conflict.serverPage.revision}
      </div>
      <div
        class="knowledge-comparison rounded border border-[var(--color-border-primary)] bg-[var(--color-bg-primary)]"
      >
        <TiptapEditor
          content={conflict.serverHtml}
          editable={false}
          {extensions}
          ariaLabel={`Server revision ${conflict.serverPage.revision} content`}
        />
      </div>
    </div>
    <div class="min-w-0">
      <div class="mb-1 text-xs font-semibold text-[var(--color-text-secondary)]">
        Your unsaved version
      </div>
      <div
        class="knowledge-comparison rounded border border-[var(--color-border-primary)] bg-[var(--color-bg-primary)]"
      >
        <TiptapEditor
          content={conflict.localHtml}
          editable={false}
          {extensions}
          ariaLabel="Local unsaved Knowledge content"
        />
      </div>
    </div>
  </div>
</section>

<style>
  .knowledge-comparison :global(.ProseMirror) {
    min-height: 12rem;
    max-height: 28rem;
    overflow: auto;
    padding: 1rem;
  }
</style>
