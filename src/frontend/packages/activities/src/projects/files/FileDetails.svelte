<script lang="ts">
  import { CollapsibleGroup, Avatar, formatDateTime } from '@sddp/shell';
  import { Button } from '@sddp/ui';
  import TraceEntityButton from '../../relationship/components/idioms/TraceEntityButton.svelte';
  import { navigateToEntity } from '../../shared/navigation/entityNavigation';
  import type { FileViewer } from './fileViewer.svelte';
  import { fileSize, type FileLink } from './projectFiles.service';
  let { viewer, projectId }: { viewer: FileViewer; projectId: string } = $props();
  let collapsed = $state<Record<string, boolean>>({});
  const groups: { type: FileLink['type']; label: string }[] = [
    { type: 'Artifact', label: 'Artifacts' },
    { type: 'Task', label: 'Tasks' },
    { type: 'Spec', label: 'Specs' },
    { type: 'Requirement', label: 'Requirements' },
    { type: 'Conversation', label: 'Conversations' },
  ];
  const date = (value: string | undefined) =>
    value ? formatDateTime(value, { month: 'short' }) : '—';
</script>

<aside class="file-details" aria-label="File details">
  <CollapsibleGroup
    title="File Info"
    expanded={!collapsed.info}
    onToggle={() => (collapsed.info = !collapsed.info)}
  >
    <dl>
      <dt>Path</dt>
      <dd class="path">{viewer.content?.path ?? viewer.selection.path}</dd>
      <dt>Size</dt>
      <dd>{viewer.content ? fileSize(viewer.content.sizeBytes) : '—'}</dd>
      <dt title="Git does not store filesystem modification time">Modified (commit)</dt>
      <dd>{date(viewer.content?.lastChange?.authoredAt)}</dd>
      <dt title="Current SDDP artifact owner, independent of the Git author">Owner · SDDP</dt>
      <dd>
        {#if viewer.relationsLoading}Loading…
        {:else if viewer.relationsError}Unavailable
        {:else if viewer.relations?.owners.length}
          {#each viewer.relations.owners as owner (owner.id)}
            <span class="inline-flex items-center gap-1.5 text-sm font-medium"
              ><Avatar
                name={owner.name ?? owner.id}
                avatarUrl={owner.avatarUrl}
                size="xs"
              />{owner.name ?? owner.id}</span
            >
          {/each}
        {:else}Unassigned{/if}
      </dd>
    </dl>
  </CollapsibleGroup>
  <CollapsibleGroup
    title="Git"
    expanded={!collapsed.git}
    onToggle={() => (collapsed.git = !collapsed.git)}
  >
    <dl>
      <dt>Branch</dt>
      <dd>{viewer.branch}</dd>
      <dt>Viewing</dt>
      <dd class="mono" title={viewer.content?.commit}>
        {viewer.content?.commit.slice(0, 8) ?? '—'}
      </dd>
      <dt>Last changed by</dt>
      <dd>{viewer.content?.lastChange?.author ?? '—'}</dd>
    </dl>
    <div class="history-heading">File history <span>on {viewer.branch}</span></div>
    {#if viewer.historyError}<div class="hint" role="alert">
        {viewer.historyError}<Button
          variant="unstyled"
          class="text-action"
          onclick={() => viewer.loadHistory()}>Retry history</Button
        >
      </div>{/if}
    <div class="history" aria-label="File change history">
      {#each viewer.history?.items ?? [] as commit (commit.commit)}
        <Button
          variant="unstyled"
          class={`commit ${viewer.selection.revision === commit.commit ? 'chosen' : ''}`}
          aria-pressed={viewer.selection.revision === commit.commit}
          title={`View file at ${commit.commit}`}
          onclick={() => viewer.showVersion(commit)}
        >
          <span class="commit-message">{commit.message}</span>
          <span class="commit-meta"
            ><span class="mono">{commit.commit.slice(0, 8)}</span><span>{commit.author}</span></span
          >
          <time datetime={commit.authoredAt}>{date(commit.authoredAt)}</time>
        </Button>
      {/each}
    </div>
    {#if viewer.historyLoading}<p class="hint" role="status">Loading history…</p>
    {:else if viewer.history?.nextSkip !== null && viewer.history?.nextSkip !== undefined}
      <Button
        variant="unstyled"
        class="text-action more"
        onclick={() => viewer.loadHistory(undefined, true)}>Load older changes</Button
      >
    {:else if viewer.history && !viewer.history.items.length}<p class="hint">
        No changes found.
      </p>{/if}
  </CollapsibleGroup>
  <p class="context-note">SDDP relationships · current</p>
  {#if viewer.relationsError}<div class="hint" role="alert">
      {viewer.relationsError}<Button
        variant="unstyled"
        class="text-action"
        onclick={() => viewer.loadRelations()}>Retry relationships</Button
      >
    </div>{/if}
  {#each groups as group (group.type)}
    {@const items = viewer.relations?.items.filter((item) => item.type === group.type) ?? []}
    <CollapsibleGroup
      title={group.label}
      badge={viewer.relations ? items.length : undefined}
      expanded={!collapsed[group.type]}
      onToggle={() => (collapsed[group.type] = !collapsed[group.type])}
    >
      {#if viewer.relationsLoading}<p class="hint">Loading…</p>
      {:else if viewer.relationsError}<p class="hint">Unavailable</p>
      {:else if items.length === 0}<p class="hint">No linked {group.label.toLowerCase()}</p>
      {:else}
        <div class="pl-2 pr-3 pt-2 pb-3 space-y-0.5">
          {#each items as item (item.id)}
            <TraceEntityButton
              entityType={item.type}
              label={item.title}
              code={item.code}
              onclick={() => navigateToEntity(projectId, item.type, item.id)}
            />
          {/each}
        </div>
      {/if}
    </CollapsibleGroup>
  {/each}
</aside>

<style>
  .file-details {
    height: 100%;
    overflow-y: auto;
    background: var(--color-bg-secondary);
  }
  dl {
    display: grid;
    grid-template-columns: minmax(0, 1fr);
    gap: 8px;
    padding: 12px;
    font-size: 12px;
    margin: 0;
  }
  dt {
    color: var(--color-text-tertiary);
  }
  dd {
    margin: -4px 0 4px;
    font-size: 13px;
    overflow-wrap: anywhere;
    color: var(--color-text-primary);
  }
  .path,
  .mono {
    font-family: var(--font-mono, monospace);
  }
  .history-heading {
    display: flex;
    gap: 8px;
    padding: 8px 12px;
    font-size: 11px;
    font-weight: 600;
  }
  .history-heading span {
    color: var(--color-text-tertiary);
    font-weight: 400;
  }
  .history {
    max-height: 280px;
    overflow: auto;
  }
  .file-details :global(.commit) {
    display: flex;
    flex-direction: column;
    gap: 4px;
    text-align: left;
    width: 100%;
    padding: 9px 12px;
    border-left: 2px solid transparent;
    border-top: 1px solid var(--color-border-primary);
  }
  .file-details :global(.commit:hover) {
    background: var(--color-bg-tertiary);
  }
  .file-details :global(.commit.chosen) {
    border-left-color: var(--color-primary-500);
    background: var(--color-bg-tertiary);
  }
  .commit-message {
    font-size: 12px;
    overflow-wrap: anywhere;
  }
  .commit-meta {
    display: flex;
    gap: 8px;
    font-size: 10px;
    color: var(--color-text-secondary);
  }
  time {
    font-size: 10px;
    color: var(--color-text-tertiary);
  }
  .hint {
    margin: 0;
    padding: 10px 12px;
    color: var(--color-text-tertiary);
    font-size: 11px;
    overflow-wrap: anywhere;
  }
  .file-details :global(.text-action) {
    display: block;
    color: var(--color-primary-600);
    font-size: 11px;
    padding-top: 6px;
  }
  .file-details :global(.more) {
    padding: 10px 12px;
  }
  .context-note {
    padding: 12px 12px 8px;
    margin: 0;
    font-size: 10px;
    color: var(--color-text-tertiary);
  }
  .file-details :global(button:focus-visible) {
    outline: 2px solid var(--color-primary-500);
    outline-offset: -2px;
  }
</style>
