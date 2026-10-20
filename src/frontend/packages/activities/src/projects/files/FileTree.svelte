<script lang="ts">
  import { onDestroy, tick } from 'svelte';
  import { Icon, FileIcon, IconButton, Button, Input } from '@sddp/ui';
  import { Dropdown, RichListItem, TreeItem } from '@sddp/shell';
  import ProjectListLayout from '../../shared/components/sections/ProjectListLayout.svelte';
  import type { FileEntry } from './projectFiles.service';
  import type { FileViewer } from './fileViewer.svelte';
  let { viewer }: { viewer: FileViewer } = $props();
  const searchId = $props.id();
  let tree: HTMLElement;
  let focusedPath = $state('');
  const visiblePaths = $derived.by(() => {
    const paths: string[] = [];
    const visit = (directory: string) => {
      for (const entry of viewer.directories[directory] ?? []) {
        paths.push(entry.path);
        if (entry.kind === 'directory' && viewer.expanded[entry.path]) visit(entry.path);
      }
    };
    visit('');
    return paths;
  });
  const tabStop = $derived(
    visiblePaths.includes(focusedPath)
      ? focusedPath
      : visiblePaths.includes(viewer.selection.path)
        ? viewer.selection.path
        : visiblePaths[0]
  );
  // Wait for lazy ancestors to render; changing unrelated folders must not reset scrolling.
  const revealPath = $derived(
    !viewer.loading && !viewer.searchQuery.trim() && visiblePaths.includes(viewer.selection.path)
      ? viewer.selection.path
      : ''
  );
  $effect(() => {
    const path = revealPath;
    if (!path) return;
    let cancelled = false;
    void tick().then(() => {
      if (cancelled) return;
      const row = [...tree.querySelectorAll<HTMLElement>('[role="treeitem"]')].find(
        (item) => item.dataset.nodeId === path
      );
      const body = tree.querySelector<HTMLElement>('.tree-body');
      if (!row || !body?.clientHeight) return;
      const bounds = body.getBoundingClientRect();
      const item = row.getBoundingClientRect();
      // Only scroll this pane, without moving keyboard focus or the editor group.
      if (item.top < bounds.top) body.scrollTop += item.top - bounds.top;
      else if (item.bottom > bounds.bottom) body.scrollTop += item.bottom - bounds.bottom;
    });
    return () => {
      cancelled = true;
    };
  });
  let searchTimer: ReturnType<typeof setTimeout> | undefined;
  onDestroy(() => clearTimeout(searchTimer));
  function searchInput(value: string) {
    clearTimeout(searchTimer);
    viewer.setSearchQuery(value);
    if (value.trim()) searchTimer = setTimeout(() => void viewer.search(), 250);
  }
  async function treeKey(event: KeyboardEvent, entry: FileEntry) {
    const keys = ['ArrowUp', 'ArrowDown', 'ArrowLeft', 'ArrowRight', 'Home', 'End'];
    if (!keys.includes(event.key)) return;
    event.preventDefault();
    const current = event.currentTarget as HTMLElement;
    if (event.key === 'ArrowRight' && entry.kind === 'directory' && !viewer.expanded[entry.path]) {
      await viewer.toggleDirectory(entry.path);
      return;
    }
    if (event.key === 'ArrowLeft' && entry.kind === 'directory' && viewer.expanded[entry.path]) {
      await viewer.toggleDirectory(entry.path);
      return;
    }
    await tick();
    const rows = [...tree.querySelectorAll<HTMLElement>('[role="treeitem"]')];
    const index = rows.indexOf(current);
    const parent = entry.path.includes('/') ? entry.path.slice(0, entry.path.lastIndexOf('/')) : '';
    const target =
      event.key === 'ArrowLeft'
        ? rows.find((row) => row.dataset.nodeId === parent)
        : event.key === 'Home'
          ? rows[0]
          : event.key === 'End'
            ? rows.at(-1)
            : event.key === 'ArrowUp'
              ? rows[index - 1]
              : event.key === 'ArrowDown'
                ? rows[index + 1]
                : event.key === 'ArrowRight' &&
                    entry.kind === 'directory' &&
                    rows[index + 1]?.dataset.nodeId?.startsWith(`${entry.path}/`)
                  ? rows[index + 1]
                  : undefined;
    target?.focus();
  }
</script>

{#snippet fileName(name: string)}
  {@const parts = name.match(/^(.+)(\.[^.]{1,12})$/)}
  <span class="file-name">
    <span class="truncate">{parts?.[1] ?? name}</span>{#if parts}<span class="file-extension"
        >{parts[2]}</span
      >{/if}
  </span>
{/snippet}

{#snippet directory(path: string, depth: number)}
  <ul
    class="file-tree-list"
    role={depth === 0 ? 'tree' : 'group'}
    aria-label={depth === 0 ? 'Repository file tree' : undefined}
  >
    {#each viewer.directories[path] ?? [] as entry (entry.path)}
      <li role="none">
        <TreeItem
          node={{
            id: entry.path,
            label: entry.name,
            children: entry.kind === 'directory' ? [] : undefined,
          }}
          variant="flat"
          class="file-row"
          level={depth}
          indentStep={14}
          expanded={!!viewer.expanded[entry.path]}
          expandable={entry.kind === 'directory'}
          selected={viewer.selection.path === entry.path}
          toggleOnClick
          draggable={false}
          tabindex={entry.path === tabStop ? 0 : -1}
          onfocus={() => (focusedPath = entry.path)}
          onkeydown={(event) => treeKey(event, entry)}
          title={entry.path}
          onToggle={() => viewer.toggleDirectory(entry.path)}
          onSelect={() => viewer.select({ path: entry.path })}
        >
          {#snippet leading()}<FileIcon path={entry.path} kind={entry.kind} />{/snippet}
          {#snippet label()}{@render fileName(entry.name)}{/snippet}
          {#snippet trailing()}{#if entry.kind === 'submodule'}<span class="kind">submodule</span
              >{/if}{/snippet}
        </TreeItem>
        {#if entry.kind === 'directory' && viewer.expanded[entry.path]}
          {#if viewer.directoryLoading[entry.path]}<p class="tree-hint" role="status">Loading…</p>
          {:else if viewer.directoryErrors[entry.path]}
            <p class="tree-hint" role="alert">
              {viewer.directoryErrors[entry.path]}
              <Button
                variant="unstyled"
                class="retry"
                onclick={() => viewer.retryDirectory(entry.path)}
                aria-label={`Retry ${entry.name}`}>Retry</Button
              >
            </p>
          {:else if viewer.directories[entry.path]?.length === 0}<p class="tree-hint">
              Empty folder
            </p>
          {:else}{@render directory(entry.path, depth + 1)}{/if}
        {/if}
      </li>
    {/each}
  </ul>
{/snippet}

<nav class="file-tree" aria-label="Repository files" bind:this={tree}>
  <ProjectListLayout lines={viewer.searchQuery.trim() ? 2 : 1}>
    <div
      class="project-list-search flex-shrink-0 flex items-center gap-1 p-2 min-h-12 border-b border-[var(--color-border-primary)]"
    >
      <div class="relative flex-1 min-w-0">
        <Icon
          name="search"
          size="sm"
          class="absolute left-2.5 top-1/2 -translate-y-1/2 text-[var(--color-text-tertiary)]"
        />
        <label for={searchId} class="sr-only">Search repository files</label>
        <Input
          id={searchId}
          type="text"
          placeholder="Search files by name or path..."
          value={viewer.searchQuery}
          maxlength={256}
          oninput={(event) => searchInput((event.target as HTMLInputElement).value)}
          variant="flat"
          class="pl-8 w-full"
          size="sm"
        />
      </div>
      {#if viewer.searchQuery}<IconButton
          icon="x"
          size="sm"
          variant="ghost"
          title="Clear file search"
          onclick={() => searchInput('')}
        />{/if}
      <Dropdown position="bottom-right">
        {#snippet trigger({ toggle, open })}
          <IconButton
            icon="more-vertical"
            size="sm"
            variant="ghost"
            title="File list options"
            onclick={toggle}
            ariaHasPopup
            ariaExpanded={open}
          />
        {/snippet}
        <Button
          variant="unstyled"
          role="menuitem"
          class="list-option"
          disabled={!!viewer.searchQuery.trim() ||
            !Object.entries(viewer.expanded).some(([path, expanded]) => path && expanded)}
          onclick={() => (viewer.expanded = { '': true })}
          ><Icon name="folder" size="sm" />Collapse all folders</Button
        >
        <Button
          variant="unstyled"
          role="menuitem"
          class="list-option"
          disabled={viewer.loading}
          onclick={() => viewer.refresh()}
          ><Icon name="refresh" size="sm" />Refresh repository</Button
        >
      </Dropdown>
    </div>
    <div class="tree-body">
      {#if viewer.loading && !viewer.tip}<p class="tree-hint" role="status">Loading repository…</p>
      {:else if viewer.error}<div class="tree-hint" role="alert">
          {viewer.error}<Button variant="unstyled" class="retry" onclick={() => viewer.refresh()}
            >Retry</Button
          >
        </div>
      {:else if viewer.directories['']?.length === 0}<p class="tree-hint">
          No committed files in this branch.
        </p>
      {:else if viewer.searchQuery.trim()}
        <div aria-live="polite">
          {#if viewer.searchLoading}<p class="tree-hint" role="status">
              Searching repository…
            </p>{/if}
          {#if viewer.searchError}<div class="tree-hint" role="alert">
              {viewer.searchError}<Button
                variant="unstyled"
                class="retry"
                onclick={() => viewer.search()}>Retry search</Button
              >
            </div>{/if}
          {#if viewer.searchResults}<p class="tree-hint">
              {viewer.searchResults.totalCount} matching files · {viewer.branch}
            </p>{/if}
        </div>
        <ul class="file-tree-list" aria-label="File search results">
          {#each viewer.searchResults?.entries ?? [] as entry (entry.path)}
            <li title={entry.path}>
              <RichListItem
                title={entry.name}
                ariaLabel={entry.path}
                titleClass="font-normal"
                description={entry.path.includes('/')
                  ? entry.path.slice(0, entry.path.lastIndexOf('/'))
                  : 'Repository root'}
                density="compact"
                selected={viewer.selection.path === entry.path}
                onclick={() => viewer.selectSearchResult(entry.path)}
              >
                {#snippet leading()}<FileIcon path={entry.path} kind={entry.kind} />{/snippet}
                {#snippet titleContent()}{@render fileName(entry.name)}{/snippet}
              </RichListItem>
            </li>
          {/each}
        </ul>
        {#if viewer.searchResults?.nextSkip != null}<Button
            variant="ghost"
            size="sm"
            disabled={viewer.searchLoading}
            onclick={() => viewer.search(true)}>Load more files</Button
          >{/if}
      {:else}{@render directory('', 0)}{/if}
    </div>
  </ProjectListLayout>
</nav>

<style>
  .file-tree {
    display: flex;
    flex-direction: column;
    height: 100%;
  }
  .tree-body {
    flex: 1;
    min-height: 0;
    overflow: auto;
    padding: 4px 0;
  }
  .file-tree-list {
    list-style: none;
    margin: 0;
    padding: 0;
  }
  .file-tree :global(.retry:focus-visible) {
    outline: 2px solid var(--color-primary-500);
    outline-offset: -2px;
  }
  .file-name {
    display: flex;
    min-width: 0;
    overflow: hidden;
  }
  .file-extension {
    flex-shrink: 0;
  }
  .file-tree :global(.list-option) {
    display: flex;
    align-items: center;
    gap: var(--space-2);
    width: 100%;
    padding: var(--space-1-5) var(--space-3);
    font-size: var(--text-sm);
    text-align: left;
    color: var(--color-text-secondary);
  }
  .file-tree :global(.list-option:hover:not(:disabled)),
  .file-tree :global(.list-option:focus-visible) {
    background: var(--color-bg-tertiary);
  }
  .file-tree :global(.list-option:disabled) {
    opacity: 0.4;
    cursor: not-allowed;
  }
  .kind {
    margin-left: auto;
    font-size: 10px;
    color: var(--color-text-tertiary);
  }
  .tree-hint {
    padding: 10px 14px;
    font-size: 12px;
    color: var(--color-text-secondary);
    overflow-wrap: anywhere;
  }
  .file-tree :global(.retry) {
    display: block;
    margin-top: 8px;
    text-decoration: underline;
  }
</style>
