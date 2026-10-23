<!--
  TiptapEditor Component
  Svelte 5 wrapper for TipTap rich text editor
-->
<script lang="ts">
  import { onMount } from 'svelte';
  import { Editor, type Extensions } from '@tiptap/core';
  import StarterKit from '@tiptap/starter-kit';
  import Placeholder from '@tiptap/extension-placeholder';
  import { Table } from '@tiptap/extension-table';
  import { TableRow } from '@tiptap/extension-table-row';
  import { TableCell } from '@tiptap/extension-table-cell';
  import { TableHeader } from '@tiptap/extension-table-header';

  interface Props {
    content?: string;
    placeholder?: string;
    editable?: boolean;
    extensions?: Extensions;
    onUpdate?: (html: string) => void;
    onFocus?: () => void;
    onBlur?: () => void;
    ariaLabel?: string;
    class?: string;
  }

  let {
    content = '',
    placeholder = 'Start typing...',
    editable = true,
    extensions = [],
    onUpdate,
    onFocus,
    onBlur,
    ariaLabel = 'Rich text editor',
    class: className = '',
  }: Props = $props();

  let element: HTMLDivElement;
  let editor: Editor | null = $state(null);

  // Mount-only: create editor once, destroy on unmount
  onMount(() => {
    editor = new Editor({
      element,
      extensions: [
        StarterKit.configure({
          heading: {
            levels: [1, 2, 3],
          },
        }),
        Placeholder.configure({ placeholder }),
        Table.configure({
          resizable: true,
        }),
        TableRow,
        TableCell,
        TableHeader,
        ...extensions,
      ],
      content,
      editable,
      editorProps: {
        attributes: {
          role: 'textbox',
          'aria-label': ariaLabel,
          'aria-multiline': 'true',
          'aria-readonly': editable ? 'false' : 'true',
        },
      },
      onUpdate: ({ editor: e }) => {
        onUpdate?.(e.getHTML());
      },
      onFocus: () => {
        onFocus?.();
      },
      onBlur: () => {
        onBlur?.();
      },
    });

    return () => editor?.destroy();
  });

  // Cancel can return to the same source HTML prop while the editor owns a draft.
  // Synchronize on mode changes too, without reporting programmatic edits.
  $effect(() => {
    if (editor) {
      editor.setEditable(editable, false);
      if (content !== editor.getHTML()) {
        editor.commands.setContent(content, { emitUpdate: false });
      }
    }
  });

  // Accessibility changes must not reset an in-progress draft.
  $effect(() => {
    if (editor) {
      editor.view.dom.setAttribute('aria-label', ariaLabel);
      editor.view.dom.setAttribute('aria-readonly', editable ? 'false' : 'true');
    }
  });

  export function getEditor(): Editor | null {
    return editor;
  }

  export function focus(): void {
    editor?.commands.focus();
  }

  export function getHTML(): string {
    return editor?.getHTML() ?? '';
  }

  export function getJSON() {
    return editor?.getJSON();
  }

  export function isEmpty(): boolean {
    return editor?.isEmpty ?? true;
  }
</script>

<div bind:this={element} class="tiptap-editor {className}"></div>

<style>
  .tiptap-editor :global(.ProseMirror) {
    outline: none;
    min-height: 100px;
    padding: 0.5rem;
    font-family:
      ui-sans-serif,
      -apple-system,
      BlinkMacSystemFont,
      'Segoe UI',
      Ubuntu,
      'Helvetica Neue',
      sans-serif;
    font-size: 16px;
    font-weight: 400;
    line-height: 1.5;
    letter-spacing: normal;
  }

  .tiptap-editor :global(.ProseMirror:focus) {
    outline: none;
  }

  /* Placeholder */
  .tiptap-editor :global(.ProseMirror p.is-editor-empty:first-child::before) {
    content: attr(data-placeholder);
    float: left;
    color: var(--color-text-muted);
    pointer-events: none;
    height: 0;
  }

  /* Shared document typography for reading and editing. */
  .tiptap-editor :global(.ProseMirror h1) {
    font-size: 1.71429em;
    font-weight: 600;
    line-height: 1.16667;
    margin: 1.45833em 0 0;
  }

  .tiptap-editor :global(.ProseMirror h2) {
    font-size: 1.43em;
    font-weight: 600;
    line-height: 1.2;
    margin: 1.4em 0 0;
  }

  .tiptap-editor :global(.ProseMirror h3) {
    font-size: 1.142em;
    font-weight: 600;
    line-height: 1.5;
    margin: 1.31249em 0 0;
  }

  .tiptap-editor :global(.ProseMirror p) {
    line-height: 1.5;
    margin: 0.5em 0 0;
  }

  .tiptap-editor :global(.ProseMirror ul),
  .tiptap-editor :global(.ProseMirror ol) {
    padding-left: 24px;
    margin: 10px 0 0;
  }

  .tiptap-editor :global(.ProseMirror ul) {
    list-style-type: disc;
  }

  /* Undo Preflight's marker reset in its own layer. The browser then honors
     ol.type case-sensitively; CSS [type] selectors otherwise fold a/A and i/I. */
  @layer base {
    .tiptap-editor :global(.ProseMirror ol) {
      list-style-type: revert-layer;
    }
  }

  .tiptap-editor :global(.ProseMirror li) {
    margin: 0;
  }

  .tiptap-editor :global(.ProseMirror li + li) {
    margin-top: 4px;
  }

  .tiptap-editor :global(.ProseMirror li > ul),
  .tiptap-editor :global(.ProseMirror li > ol) {
    margin-top: 0;
  }

  .tiptap-editor :global(.ProseMirror > :first-child),
  .tiptap-editor :global(.ProseMirror li > p:first-child),
  .tiptap-editor :global(.ProseMirror blockquote > :first-child),
  .tiptap-editor :global(.ProseMirror td > :first-child),
  .tiptap-editor :global(.ProseMirror th > :first-child) {
    margin-top: 0;
  }

  .tiptap-editor :global(.ProseMirror blockquote) {
    border-left: 3px solid var(--color-border);
    padding-left: 1rem;
    margin-left: 0;
    margin-bottom: 0.5rem;
    color: var(--color-text-secondary);
  }

  .tiptap-editor :global(.ProseMirror code) {
    background-color: var(--color-bg-tertiary);
    border-radius: 0.25rem;
    padding: 0.125rem 0.25rem;
    font-family: ui-monospace, Menlo, 'Segoe UI Mono', 'Ubuntu Mono', monospace;
    font-size: 0.875em;
  }

  .tiptap-editor :global(.ProseMirror pre) {
    background-color: var(--color-bg-tertiary);
    border-radius: 0.375rem;
    padding: 0.75rem 1rem;
    margin-bottom: 0.5rem;
    overflow-x: auto;
  }

  .tiptap-editor :global(.ProseMirror pre code) {
    background: none;
    padding: 0;
  }

  /* Table styles */
  .tiptap-editor :global(.ProseMirror table) {
    border-collapse: collapse;
    margin-bottom: 0.5rem;
    width: 100%;
  }

  .tiptap-editor :global(.ProseMirror th),
  .tiptap-editor :global(.ProseMirror td) {
    border: 1px solid var(--color-border);
    padding: 0.5rem;
    text-align: left;
    vertical-align: top;
  }

  .tiptap-editor :global(.ProseMirror th) {
    background-color: var(--color-bg-secondary);
    font-weight: 600;
  }

  .tiptap-editor :global(.ProseMirror .selectedCell) {
    background-color: var(--color-accent-primary-10, rgba(59, 130, 246, 0.1));
  }

  /* Horizontal rule */
  .tiptap-editor :global(.ProseMirror hr) {
    border: none;
    border-top: 1px solid var(--color-border);
    margin: 1rem 0;
  }
</style>
