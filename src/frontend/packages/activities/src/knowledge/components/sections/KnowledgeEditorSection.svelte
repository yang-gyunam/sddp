<script lang="ts">
  import type { Editor, Extensions } from '@tiptap/core';
  import { Badge, Button, Input, Select } from '@sddp/ui';
  import { CollapsibleGroup } from '@sddp/shell';
  import DetailField from '../../../shared/components/idioms/DetailField.svelte';
  import TiptapEditor from '../../../shared/components/controls/TiptapEditor.svelte';
  import type { KnowledgePage } from '../../types';
  import KnowledgeToolbarIcon from '../KnowledgeToolbarIcon.svelte';

  interface EditorHandle {
    getEditor(): Editor | null;
    getJSON(): import('@tiptap/core').JSONContent | undefined;
    focus(): void;
  }

  interface SelectOption {
    value: string;
    label: string;
  }

  interface Props {
    page: KnowledgePage;
    editing: boolean;
    dirty: boolean;
    editTitle: string;
    editSlug: string;
    editTags: string;
    changeSummary: string;
    savedChangeSummary?: string;
    editorHtml: string;
    editorRef?: EditorHandle;
    moving: boolean;
    moveParentId: string;
    movePosition: number;
    moveParentOptions: SelectOption[];
    addingReference: boolean;
    referenceType: string;
    referenceId: string;
    extensions: Extensions;
    onEditorUpdate: () => void;
    onCancelMove: () => void;
    onMove: () => void;
    onInsertReference: () => void;
  }

  let {
    page,
    editing,
    dirty,
    editTitle = $bindable(),
    editSlug = $bindable(),
    editTags = $bindable(),
    changeSummary = $bindable(),
    savedChangeSummary = '',
    editorHtml,
    editorRef = $bindable(),
    moving,
    moveParentId = $bindable(),
    movePosition = $bindable(),
    moveParentOptions,
    addingReference = $bindable(),
    referenceType = $bindable(),
    referenceId = $bindable(),
    extensions,
    onEditorUpdate,
    onCancelMove,
    onMove,
    onInsertReference,
  }: Props = $props();

  let basicExpanded = $state(true);

  const toolbarButtonClass =
    'inline-flex h-7 min-w-7 items-center justify-center rounded px-1.5 text-sm text-[var(--color-text-secondary)] transition-colors hover:bg-[var(--color-bg-hover)] hover:text-[var(--color-text-primary)] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-accent-primary)]';

  function runEditorCommand(command: (editor: Editor) => void): void {
    const editor = editorRef?.getEditor();
    if (!editor) return;
    command(editor);
  }

  function formatDate(value: string): string {
    return new Intl.DateTimeFormat(undefined, {
      dateStyle: 'medium',
      timeStyle: 'short',
    }).format(new Date(value));
  }
</script>

<CollapsibleGroup
  title="Basic Information"
  variant="plain"
  expanded={basicExpanded}
  onToggle={() => (basicExpanded = !basicExpanded)}
>
  <div inert={!basicExpanded} class="knowledge-fields pb-3">
    <div class="mb-4 flex flex-col gap-4 pl-5 pt-2 pb-3">
      {#if editing}
        <Input
          id={'knowledge-page-title-' + page.id}
          bind:value={editTitle}
          label="Title"
          placeholder="Enter page title..."
          maxlength={200}
          required
        />
      {:else}
        <DetailField label="Title">
          <p class="knowledge-read-value">{page.title}</p>
        </DetailField>
      {/if}
      <div class="grid grid-cols-1 gap-4 md:grid-cols-2">
        {#if editing}
          <Input
            id={'knowledge-page-slug-' + page.id}
            bind:value={editSlug}
            label="Slug"
            hint="URL-friendly identifier"
            maxlength={200}
            required
            mono
          />
          <Input
            id={'knowledge-page-tags-' + page.id}
            bind:value={editTags}
            label="Tags"
            hint="Comma-separated"
          />
        {:else}
          <DetailField label="Slug">
            <p class="knowledge-read-value font-mono">{page.slug}</p>
          </DetailField>
          <DetailField label="Tags">
            <div class="knowledge-read-value flex flex-wrap gap-1">
              {#if page.tags.length > 0}
                {#each page.tags as tag (tag)}
                  <Badge variant="secondary" size="sm">{tag}</Badge>
                {/each}
              {:else}
                <span class="text-[var(--color-text-tertiary)]">No tags</span>
              {/if}
            </div>
          </DetailField>
        {/if}
      </div>
    </div>

    {#if moving}
      <div
        class="ml-5 mb-4 grid grid-cols-1 items-end gap-2 rounded border border-[var(--color-border-primary)] bg-[var(--color-bg-secondary)] p-3 sm:grid-cols-[minmax(12rem,1fr)_8rem_auto]"
        aria-label="Move Knowledge page"
      >
        <Select bind:value={moveParentId} options={moveParentOptions} label="Parent page" />
        <Input bind:value={movePosition} type="number" min={0} label="Position" />
        <div class="flex gap-1">
          <Button variant="ghost" size="sm" onclick={onCancelMove}>Cancel</Button>
          <Button variant="primary" size="sm" onclick={onMove}>Move</Button>
        </div>
      </div>
    {/if}

    {#if editing}
      <div
        class="ml-5 mb-2 flex flex-wrap items-center gap-1 rounded-t border border-[var(--color-border-primary)] bg-[var(--color-bg-secondary)] p-2"
        role="toolbar"
        aria-label="Knowledge formatting"
      >
        <Button
          variant="unstyled"
          title="Bold"
          aria-label="Bold"
          class={toolbarButtonClass}
          onclick={() => runEditorCommand((editor) => editor.chain().focus().toggleBold().run())}
        >
          <KnowledgeToolbarIcon name="bold" />
        </Button>
        <Button
          variant="unstyled"
          title="Italic"
          aria-label="Italic"
          class={toolbarButtonClass}
          onclick={() => runEditorCommand((editor) => editor.chain().focus().toggleItalic().run())}
        >
          <KnowledgeToolbarIcon name="italic" />
        </Button>
        <Button
          variant="unstyled"
          title="Inline code"
          aria-label="Inline code"
          class={toolbarButtonClass}
          onclick={() => runEditorCommand((editor) => editor.chain().focus().toggleCode().run())}
        >
          <KnowledgeToolbarIcon name="code" />
        </Button>
        <span class="mx-1 h-5 w-px bg-[var(--color-border-primary)]" aria-hidden="true"></span>
        <Button
          variant="unstyled"
          title="Heading 1"
          aria-label="Heading 1"
          class={toolbarButtonClass}
          onclick={() =>
            runEditorCommand((editor) => editor.chain().focus().toggleHeading({ level: 1 }).run())}
        >
          <KnowledgeToolbarIcon name="heading-1" />
        </Button>
        <Button
          variant="unstyled"
          title="Heading 2"
          aria-label="Heading 2"
          class={toolbarButtonClass}
          onclick={() =>
            runEditorCommand((editor) => editor.chain().focus().toggleHeading({ level: 2 }).run())}
        >
          <KnowledgeToolbarIcon name="heading-2" />
        </Button>
        <span class="mx-1 h-5 w-px bg-[var(--color-border-primary)]" aria-hidden="true"></span>
        <Button
          variant="unstyled"
          title="Bullet list"
          aria-label="Bullet list"
          class={toolbarButtonClass}
          onclick={() =>
            runEditorCommand((editor) => editor.chain().focus().toggleBulletList().run())}
        >
          <KnowledgeToolbarIcon name="list" />
        </Button>
        <Button
          variant="unstyled"
          title="Numbered list"
          aria-label="Numbered list"
          class={toolbarButtonClass}
          onclick={() =>
            runEditorCommand((editor) => editor.chain().focus().toggleOrderedList().run())}
        >
          <KnowledgeToolbarIcon name="list-ordered" />
        </Button>
        <Button
          variant="unstyled"
          title="Blockquote"
          aria-label="Blockquote"
          class={toolbarButtonClass}
          onclick={() =>
            runEditorCommand((editor) => editor.chain().focus().toggleBlockquote().run())}
        >
          <KnowledgeToolbarIcon name="quote" />
        </Button>
        <span class="mx-1 h-5 w-px bg-[var(--color-border-primary)]" aria-hidden="true"></span>
        <Button
          variant="unstyled"
          title="Insert table"
          aria-label="Insert table"
          class={toolbarButtonClass}
          onclick={() =>
            runEditorCommand((editor) =>
              editor.chain().focus().insertTable({ rows: 3, cols: 3, withHeaderRow: true }).run()
            )}
        >
          <KnowledgeToolbarIcon name="table" />
        </Button>
        <Button
          variant="unstyled"
          title="Reference an SDDP entity"
          aria-label="Reference an SDDP entity"
          aria-pressed={addingReference}
          class={`${toolbarButtonClass} ${
            addingReference
              ? 'bg-[var(--color-accent-primary)]/10 text-[var(--color-accent-text)]'
              : ''
          }`}
          onclick={() => (addingReference = !addingReference)}
        >
          <KnowledgeToolbarIcon name="at-sign" />
        </Button>
      </div>
      {#if addingReference}
        <div
          class="ml-5 mb-2 grid grid-cols-1 items-end gap-2 rounded border border-[var(--color-border-primary)] bg-[var(--color-bg-secondary)] p-3 sm:grid-cols-[10rem_minmax(12rem,1fr)_auto]"
          aria-label="Insert SDDP entity reference"
        >
          <Select
            bind:value={referenceType}
            label="Entity type"
            options={[
              { value: 'Spec', label: 'Spec' },
              { value: 'Requirement', label: 'Requirement' },
              { value: 'Decision', label: 'Decision' },
              { value: 'Artifact', label: 'Artifact' },
            ]}
          />
          <Input
            bind:value={referenceId}
            label="Entity ID"
            placeholder="UUID"
            mono
            onkeydown={(event) => {
              if (event.key === 'Enter') onInsertReference();
            }}
          />
          <Button
            variant="primary"
            size="sm"
            disabled={!referenceId.trim()}
            onclick={onInsertReference}>Insert</Button
          >
        </div>
      {/if}
    {/if}

    <div
      class="knowledge-editor ml-5 rounded border min-h-[28rem] bg-[var(--color-bg-primary)]
  {editing ? 'border-[var(--color-border-primary)]' : 'border-transparent'}"
    >
      <TiptapEditor
        bind:this={editorRef}
        content={editorHtml}
        editable={editing}
        {extensions}
        onUpdate={onEditorUpdate}
        placeholder="Write project knowledge..."
        ariaLabel={editing ? 'Knowledge page editor' : 'Knowledge page content'}
        class="min-h-[28rem]"
      />
    </div>

    {#if editing}
      <div class="ml-5 mt-3">
        <Input
          bind:value={changeSummary}
          label="Change summary"
          maxlength={500}
          placeholder="What changed?"
        />
        {#if dirty}
          <div class="mt-1 text-xs text-[var(--color-warning-600)]" role="status">
            Unsaved changes
          </div>
        {/if}
      </div>
    {/if}

    {#if !editing}
      <div class="ml-5 mt-3">
        <DetailField label="Change summary">
          <p class="knowledge-read-value whitespace-pre-wrap">
            {savedChangeSummary}
          </p>
        </DetailField>
      </div>
    {/if}
    <div class="ml-5 mt-4 text-xs text-[var(--color-text-tertiary)]">
      Updated {formatDate(page.updatedAt)}
    </div>
  </div>
</CollapsibleGroup>

<style>
  .knowledge-read-value {
    border: 1px solid transparent;
    padding: var(--space-2) var(--space-3);
    font-size: 0.875rem;
    line-height: 1.25rem;
    color: var(--color-text-primary);
    overflow-wrap: anywhere;
  }

  .knowledge-editor :global(.ProseMirror) {
    min-height: 28rem;
    /* Match the default Input inset used by Change summary in both modes. */
    padding-inline: var(--space-3);
    line-height: 1.65;
  }

  .knowledge-editor :global(.sddp-entity-mention) {
    border-radius: 0.25rem;
    background: color-mix(in srgb, var(--color-accent-primary) 12%, transparent);
    color: var(--color-accent-primary);
    padding: 0.1rem 0.3rem;
    font-weight: 500;
  }
</style>
