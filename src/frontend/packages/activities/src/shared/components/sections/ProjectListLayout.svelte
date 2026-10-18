<script lang="ts">
  import type { Snippet } from 'svelte';

  let { lines, children }: { lines: 1 | 2 | 3; children: Snippet } = $props();
</script>

<div class="project-list-layout" data-lines={lines}>
  {@render children()}
</div>

<style>
  /* Project lists opt in here; typography and shared controls retain their defaults. */
  .project-list-layout {
    --project-list-search-height: var(--space-12);
    --project-list-heading-height: var(--space-7);
    --project-list-row-height: var(--space-9);
    display: flex;
    flex-direction: column;
    height: 100%;
    min-height: 0;
    background-color: var(--color-bg-primary);
  }

  .project-list-layout[data-lines='2'] {
    --project-list-row-height: calc(var(--space-9) + var(--space-12) / 2);
  }

  .project-list-layout[data-lines='3'] {
    --project-list-row-height: calc(var(--space-9) + var(--space-12));
  }

  .project-list-layout :global(.project-list-search) {
    min-height: var(--project-list-search-height);
    flex-shrink: 0;
    padding-block: var(--space-2);
  }

  .project-list-layout :global(.project-list-heading),
  .project-list-layout :global(.group-header) {
    min-height: var(--project-list-heading-height);
    height: auto;
    flex-shrink: 0;
    align-items: center;
    padding-block: 0;
  }

  .project-list-layout :global(.project-list-row),
  .project-list-layout :global(.list-item),
  .project-list-layout :global(.rich-list-item),
  .project-list-layout :global(.member-card) {
    min-height: var(--project-list-row-height);
    padding-block: var(--space-1-5);
  }

  .project-list-layout :global(.rich-list-item),
  .project-list-layout :global(.knowledge-search-row) {
    display: flex;
    flex-direction: column;
    justify-content: center;
  }

  /* Name + two stable metadata lines, regardless of available member column width. */
  .project-list-layout :global(.member-card__member) {
    display: grid;
    grid-template-columns: var(--space-6) minmax(0, 1fr);
    gap: var(--space-1) var(--space-2);
  }

  .project-list-layout :global(.member-card__avatar) {
    width: var(--space-6);
    height: var(--space-6);
  }

  .project-list-layout :global(.member-card__member-info) {
    display: contents;
  }

  .project-list-layout :global(.member-card__meta) {
    grid-column: 1 / -1;
    display: grid;
    grid-template-columns: repeat(2, minmax(0, 1fr));
    gap: 0 var(--space-2);
  }

  .project-list-layout :global(.member-card__meta-item) {
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
  }
</style>
