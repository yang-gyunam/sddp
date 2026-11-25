<!-- Activity: Projects > Files | Committed files with current SDDP context. -->
<script lang="ts">
  import { onDestroy, untrack } from 'svelte';
  import { Icon, FileIcon, IconButton, Button } from '@sddp/ui';
  import {
    SidebarDetailLayout,
    CodeEditor,
    getTabState,
    setTabState,
    globalActiveTab,
    statusBar,
    PageShell,
    PageHeader,
    PageBody,
  } from '@sddp/shell';
  import DetailHeader from '../../../shared/components/idioms/DetailHeader.svelte';
  import DetailChatToggle from '../../../ai/components/idioms/DetailChatToggle.svelte';
  import DetailChatPanel from '../../../ai/components/sections/DetailChatPanel.svelte';
  import { createDetailChat } from '../../../ai/stores/detail-chat.svelte';
  import { useDetailRoute } from '../../routing/useDetailRoute.svelte';
  import { FileViewer, type FileSelection } from '../../files/fileViewer.svelte';
  import {
    createProjectFilesService,
    fileLanguage,
    fileSize,
  } from '../../files/projectFiles.service';
  import FileTree from '../../files/FileTree.svelte';
  import FileDetails from '../../files/FileDetails.svelte';
  import FolderContents from '../../files/FolderContents.svelte';

  let {
    projectId,
    tabId = '',
    projectName = '',
    readonly: readOnly = false,
    referenceId = '',
    referenceRevision,
    referenceLine,
    class: className = '',
  }: {
    projectId: string;
    tabId?: string;
    projectName?: string;
    readonly?: boolean;
    referenceId?: string;
    referenceRevision?: string;
    referenceLine?: number;
    class?: string;
  } = $props();
  let viewer = $state<FileViewer | null>(null);
  let initializedScope = '';
  const stateKey = $derived(tabId || `project-${projectId}-files`);
  $effect(() => {
    const id = projectId;
    const key = stateKey;
    const reference = readOnly
      ? { path: referenceId, revision: referenceRevision, line: referenceLine }
      : undefined;
    untrack(() => {
      // Editor-group focus updates replace the props bag without changing the tab.
      // Keep the existing instance and its pending requests for that same identity.
      const scope = JSON.stringify([id, key, reference]);
      if (initializedScope === scope && viewer) return;
      viewer?.dispose();
      initializedScope = scope;
      const instance = new FileViewer(createProjectFilesService(id));
      viewer = instance;
      const saved = getTabState<{
        fileSelection?: FileSelection;
        expandedFolders?: string[];
        searchQuery?: string;
      }>(key);
      instance.setSearchQuery(saved?.searchQuery ?? '');
      void instance.initialize(reference ?? saved?.fileSelection, saved?.expandedFolders);
    });
  });
  onDestroy(() => viewer?.dispose());
  $effect(() => {
    const selection = viewer?.selection;
    const ready = viewer?.tip;
    const loading = viewer?.loading;
    const searchQuery = viewer?.searchQuery ?? '';
    const expandedFolders = Object.entries(viewer?.expanded ?? {})
      .filter(([path, open]) => path && open)
      .map(([path]) => path);
    const key = stateKey;
    if (selection && ready && !loading)
      setTabState(key, { fileSelection: { ...selection }, expandedFolders, searchQuery });
  });
  useDetailRoute({
    tabId: () => stateKey,
    path: () => `/project/${projectId}/files`,
    selection: () => viewer?.selection.path || null,
    query: () => ({
      kind: viewer?.selection.kind ?? null,
      line: viewer?.selection.line ? String(viewer.selection.line) : null,
      revision: viewer?.selection.revision ?? null,
      revisionPath: viewer?.selection.revision
        ? (viewer.selection.revisionPath ?? viewer.selection.path)
        : null,
    }),
    ready: () => !readOnly && !!viewer?.tip && !viewer?.loading,
    restore: (path, query) => {
      if (viewer) {
        if (!path) return viewer.select({ path: '' });
        if (query?.get('kind') === 'directory') return viewer.select({ path, kind: 'directory' });
        const revision = query?.get('revision') || undefined;
        return viewer.select({
          path,
          revision,
          revisionPath: revision ? query?.get('revisionPath') || path : undefined,
          line: Number(query?.get('line')) > 0 ? Number(query?.get('line')) : undefined,
        });
      }
    },
  });
  $effect(() => {
    const key = `files-${stateKey}`;
    const content = viewer?.content;
    const active = $globalActiveTab?.id === tabId;
    if (!active || !content) return;
    const text = `${content.branch} · ${content.commit.slice(0, 8)} · ${fileSize(content.sizeBytes)} · ${content.isBinary ? 'Binary' : content.tooLarge ? 'Preview unavailable' : fileLanguage(content.path)} · Read only`;
    untrack(() =>
      statusBar.addItem({
        id: key,
        text,
        tooltip: content.path,
        alignment: 'right',
        priority: 20,
        visible: true,
      })
    );
    return () => statusBar.removeItem(key);
  });
  const detailChat = createDetailChat(() => {
    const selection = viewer?.selection;
    const content = viewer?.content;
    if (readOnly || !selection?.path || !content || viewer?.contentLoading || viewer?.contentError)
      return undefined;
    return {
      projectId,
      entityType: 'ProjectFile',
      filePath: content.path,
      fileRevision: selection.revision ? content.commit : undefined,
      fileCommit: content.commit,
    };
  });
</script>

{#snippet aiChatPanel()}<DetailChatPanel chat={detailChat} />{/snippet}

<PageShell class={className}>
  <PageHeader title="Files" meta={projectName} loading={viewer?.loading}>
    {#snippet actions()}
      {#if viewer?.branch}<span class="revision">{viewer.branch}</span>{/if}
      <IconButton
        icon="refresh-cw"
        size="sm"
        variant="ghost"
        title="Refresh repository"
        disabled={!viewer || viewer.loading}
        onclick={() => viewer?.refresh()}
      />
    {/snippet}
  </PageHeader>
  <PageBody noPadding scrollable={false}>
    <section class="project-files" aria-label="Project files">
      {#if viewer}
        {@const model = viewer}
        {@const isFolder = model.selection.kind === 'directory'}
        {#snippet fileSidebar()}<FileTree viewer={model} />{/snippet}
        <SidebarDetailLayout
          sidebar={readOnly ? undefined : fileSidebar}
          showRightPanel={!!model.selection.path && !isFolder}
          auxiliaryPanel={detailChat.visible ? aiChatPanel : undefined}
          showAuxiliaryPanel={detailChat.open}
        >
          {#snippet header()}
            <DetailHeader>
              {#snippet leading()}<FileIcon
                  path={model.content?.path ?? model.selection.path}
                  kind={isFolder ? 'directory' : model.content?.kind}
                  size="md"
                />{/snippet}
              {#if isFolder}
                {@const path = model.selection.path}
                <span class="file-path folder-name" title={`/${path}`}>
                  {path.slice(path.lastIndexOf('/') + 1)}
                </span>
                <span class="file-path folder-path" title={`/${path}`}>/{path}</span>
              {:else}
                <span class="file-path" title={model.content?.path ?? model.selection.path}
                  >{model.content?.path ?? (model.selection.path || 'Files')}</span
                >
              {/if}
              {#snippet actions()}
                {#if model.selection.revision}
                  <span class="revision">{model.selection.revision.slice(0, 8)}</span>
                  {#if !readOnly}
                    <Button variant="ghost" size="sm" onclick={() => model.showVersion(null)}
                      >Back to latest</Button
                    >
                  {/if}
                {:else if model.selection.path}<span class="revision">Latest</span>{/if}
                {#if detailChat.visible}
                  <DetailChatToggle open={detailChat.open} onclick={detailChat.toggle} />
                {/if}
              {/snippet}
            </DetailHeader>
          {/snippet}
          {#snippet rightPanel()}<FileDetails
              viewer={model}
              {projectId}
              readonly={readOnly}
            />{/snippet}
          {#if isFolder}
            <FolderContents viewer={model} />
          {:else}
            <section
              class="file-content"
              aria-label="File contents"
              aria-busy={model.contentLoading}
            >
              {#if !model.selection.path}<div class="empty">
                  <Icon name="file-code" size="lg" />
                  <p>Select a file to view its contents, history and SDDP relationships.</p>
                </div>
              {:else if model.contentLoading}<div class="empty" role="status">Loading file…</div>
              {:else if model.contentError}<div class="empty" role="alert">
                  <p>{model.contentError}</p>
                  <Button
                    variant="ghost"
                    size="sm"
                    onclick={() =>
                      model.showVersion(
                        model.selection.revision
                          ? {
                              commit: model.selection.revision,
                              path: model.selection.revisionPath ?? model.selection.path,
                            }
                          : null,
                        model.selection.line
                      )}>Retry file</Button
                  >
                </div>
              {:else if model.content?.kind === 'submodule'}<div class="empty">
                  This entry is a submodule. Its contents belong to a separate repository.
                </div>
              {:else if model.content?.tooLarge}<div class="empty">
                  Preview unavailable: this file exceeds {fileSize(model.content.maxBytes)}.
                </div>
              {:else if model.content?.isBinary}<div class="empty">
                  Binary or non-UTF-8 file.
                  {readOnly
                    ? 'Metadata is available on the right.'
                    : 'Metadata and history are available on the right.'}
                </div>
              {:else if model.content}
                {#if model.content.kind === 'symlink'}<p class="link-note">
                    Symbolic link target (read only)
                  </p>{/if}
                <CodeEditor
                  value={model.content.content ?? ''}
                  language={fileLanguage(model.content.path)}
                  readOnly
                  revealLine={model.selection.line}
                  embedded
                />
              {/if}
            </section>
          {/if}
        </SidebarDetailLayout>
      {/if}
    </section>
  </PageBody>
</PageShell>

<style>
  .project-files {
    height: 100%;
    min-height: 0;
    color: var(--color-text-primary);
  }
  .file-path {
    font-size: 13px;
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
  }
  .folder-name {
    flex-shrink: 0;
    max-width: min(16rem, 55%);
    font-weight: 500;
  }
  .folder-path {
    min-width: 0;
    font-size: 11px;
    color: var(--color-text-secondary);
  }
  .revision {
    font-size: 11px;
    color: var(--color-text-tertiary);
    font-family: var(--font-mono, monospace);
  }
  .file-content {
    display: flex;
    flex-direction: column;
    height: 100%;
    min-height: 0;
    overflow: hidden;
    isolation: isolate;
  }
  .empty {
    display: flex;
    flex: 1;
    align-items: center;
    justify-content: center;
    flex-direction: column;
    gap: 12px;
    padding: 32px;
    text-align: center;
    color: var(--color-text-secondary);
    font-size: 13px;
  }
  .link-note {
    font-size: 11px;
    padding: 4px 8px 8px;
    margin: 0;
    color: var(--color-text-secondary);
  }
</style>
