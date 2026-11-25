<script lang="ts">
  import { Button, FileIcon, Table } from '@sddp/ui';
  import { ProfileAvatar } from '@sddp/shell';
  import type { FileViewer } from './fileViewer.svelte';
  import { fileSize, type FileEntry } from './projectFiles.service';

  let { viewer }: { viewer: FileViewer } = $props();
  const kindLabels: Record<FileEntry['kind'], string> = {
    directory: 'Folder',
    file: 'File',
    symlink: 'Symbolic link',
    submodule: 'Submodule',
  };
</script>

<section class="folder-contents" aria-label="Folder contents" aria-busy={viewer.folderLoading}>
  {#if viewer.folderLoading}
    <p class="folder-state" role="status">Loading folder…</p>
  {:else if viewer.folderError}
    <div class="folder-state" role="alert">
      <p>{viewer.folderError}</p>
      <Button
        variant="ghost"
        size="sm"
        onclick={() => viewer.select({ path: viewer.selection.path, kind: 'directory' })}
        >Retry folder</Button
      >
    </div>
  {:else if !viewer.folderEntries.length}
    <p class="folder-state">Empty folder</p>
  {:else}
    <Table label="Files and folders" class="folder-table">
      {#snippet header()}
        <tr>
          <th scope="col">Name</th>
          <th scope="col">Type</th>
          <th scope="col" class="size">Size</th>
          <th scope="col" title="Current SDDP artifact owners">Ownership</th>
          <th scope="col" title="Git author of the latest file change at this revision">
            Last changed by
          </th>
        </tr>
      {/snippet}
      {#each viewer.folderEntries as entry (entry.path)}
        <tr>
          <th scope="row" class="name">
            <Button
              variant="unstyled"
              class="entry-button"
              title={entry.path}
              onclick={() =>
                viewer.select(
                  entry.kind === 'directory'
                    ? { path: entry.path, kind: 'directory' }
                    : { path: entry.path }
                )}
            >
              <FileIcon path={entry.path} kind={entry.kind} />
              <span class="entry-name">{entry.name}</span>
            </Button>
          </th>
          <td>{kindLabels[entry.kind]}</td>
          <td class="size">{entry.sizeBytes === null ? '—' : fileSize(entry.sizeBytes)}</td>
          <td>
            {#if entry.kind === 'directory'}
              —
            {:else if entry.owners?.length}
              <div class="owners">
                {#each entry.owners as owner (owner.id)}
                  <ProfileAvatar name={owner.name ?? owner.id} profile={owner} />
                {/each}
              </div>
            {:else}
              Unassigned
            {/if}
          </td>
          <td>
            <div class="author">
              <ProfileAvatar
                name={entry.lastChangedBy ?? '—'}
                profile={entry.lastChangedByProfile}
              />
            </div>
          </td>
        </tr>
      {/each}
    </Table>
  {/if}
</section>

<style>
  .folder-contents {
    height: 100%;
    min-height: 0;
    overflow: auto;
    font-size: 13px;
  }
  .folder-state {
    display: grid;
    justify-items: center;
    align-content: center;
    min-height: 100%;
    padding: 24px;
    gap: 8px;
    text-align: center;
    color: var(--color-text-secondary);
  }
  .folder-contents :global(.folder-table) {
    min-width: 32rem;
  }
  th,
  td {
    text-align: left;
  }
  td {
    width: 1%;
    white-space: nowrap;
    color: var(--color-text-secondary);
  }
  .name {
    max-width: 0;
    font-weight: 500;
  }
  .size {
    text-align: right;
    font-variant-numeric: tabular-nums;
  }
  .owners {
    display: flex;
    flex-wrap: wrap;
    align-items: center;
    gap: 4px;
    min-width: 5.5rem;
  }
  .author {
    min-width: 6.5rem;
    max-width: 11rem;
  }
  :global(.entry-button) {
    display: flex;
    align-items: center;
    gap: 8px;
    max-width: 100%;
    text-align: left;
  }
  :global(.entry-button:hover) .entry-name {
    text-decoration: underline;
  }
  .entry-name {
    min-width: 0;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
  }
</style>
