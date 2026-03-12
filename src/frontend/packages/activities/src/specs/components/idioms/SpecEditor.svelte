<!--
  SpecEditor Component
  Rich text block editor for Spec content with toolbar and custom blocks
-->
<script lang="ts">
  import { Icon, Button } from '@sddp/ui';
  import TiptapEditor from '../../../shared/components/controls/TiptapEditor.svelte';
  import type { Editor, Extensions } from '@tiptap/core';
  import {
    DecisionBlock,
    AcceptanceCriteriaBlock,
    RiskBlock,
    GlossaryLink,
    GlossaryLinkSuggestion,
    SpecReference,
    ChangeHighlight,
  } from '../../extensions';

  interface Props {
    content?: string;
    placeholder?: string;
    onUpdate?: (html: string) => void;
    readonly?: boolean;
    minHeight?: string;
    class?: string;
    tenantId?: string;
    projectId?: string;
    showChanges?: boolean;
  }

  let {
    content = '',
    placeholder = 'Start typing... (use [[ for glossary terms)',
    onUpdate,
    readonly = false,
    minHeight = '150px',
    class: className = '',
    tenantId = '',
    projectId = '',
    showChanges: _showChanges = false,
  }: Props = $props();

  let editorComponent: TiptapEditor;
  let isFocused = $state(false);
  let showBlockMenu = $state(false);

  // Build custom extensions array
  const customExtensions: Extensions = $derived.by(() => {
    const exts: Extensions = [
      DecisionBlock,
      AcceptanceCriteriaBlock,
      RiskBlock,
      GlossaryLink,
      SpecReference,
      ChangeHighlight,
    ];

    // Add GlossaryLinkSuggestion only if we have tenant/project context
    if (tenantId && projectId) {
      exts.push(
        GlossaryLinkSuggestion.configure({
          tenantId,
          projectId,
        })
      );
    }

    return exts;
  });

  function getEditor(): Editor | null {
    return editorComponent?.getEditor() ?? null;
  }

  // Toolbar actions
  function toggleBold() {
    getEditor()?.chain().focus().toggleBold().run();
  }

  function toggleItalic() {
    getEditor()?.chain().focus().toggleItalic().run();
  }

  function toggleStrike() {
    getEditor()?.chain().focus().toggleStrike().run();
  }

  function toggleCode() {
    getEditor()?.chain().focus().toggleCode().run();
  }

  function toggleHeading(level: 1 | 2 | 3) {
    getEditor()?.chain().focus().toggleHeading({ level }).run();
  }

  function toggleBulletList() {
    getEditor()?.chain().focus().toggleBulletList().run();
  }

  function toggleOrderedList() {
    getEditor()?.chain().focus().toggleOrderedList().run();
  }

  function toggleBlockquote() {
    getEditor()?.chain().focus().toggleBlockquote().run();
  }

  function toggleCodeBlock() {
    getEditor()?.chain().focus().toggleCodeBlock().run();
  }

  function insertTable() {
    getEditor()?.chain().focus().insertTable({ rows: 3, cols: 3, withHeaderRow: true }).run();
  }

  function insertHorizontalRule() {
    getEditor()?.chain().focus().setHorizontalRule().run();
  }

  function undo() {
    getEditor()?.chain().focus().undo().run();
  }

  function redo() {
    getEditor()?.chain().focus().redo().run();
  }

  // Custom block insertions
  function insertDecisionBlock() {
    const editor = getEditor();
    if (editor) {
      editor.chain().focus().insertDecisionBlock({}).run();
    }
    showBlockMenu = false;
  }

  function insertAcceptanceCriteriaBlock() {
    const editor = getEditor();
    if (editor) {
      editor.chain().focus().insertAcceptanceCriteriaBlock({}).run();
    }
    showBlockMenu = false;
  }

  function insertRiskBlock() {
    const editor = getEditor();
    if (editor) {
      editor.chain().focus().insertRiskBlock({}).run();
    }
    showBlockMenu = false;
  }

  // Check if format is active
  function isActive(name: string, attributes?: Record<string, unknown>): boolean {
    return getEditor()?.isActive(name, attributes) ?? false;
  }

  // Reactive active states
  let activeBold = $derived(isFocused && isActive('bold'));
  let activeItalic = $derived(isFocused && isActive('italic'));
  let activeStrike = $derived(isFocused && isActive('strike'));
  let activeCode = $derived(isFocused && isActive('code'));
  let activeH1 = $derived(isFocused && isActive('heading', { level: 1 }));
  let activeH2 = $derived(isFocused && isActive('heading', { level: 2 }));
  let activeH3 = $derived(isFocused && isActive('heading', { level: 3 }));
  let activeBulletList = $derived(isFocused && isActive('bulletList'));
  let activeOrderedList = $derived(isFocused && isActive('orderedList'));
  let activeBlockquote = $derived(isFocused && isActive('blockquote'));
  let activeCodeBlock = $derived(isFocused && isActive('codeBlock'));

  function handleFocus() {
    isFocused = true;
  }

  function handleBlur() {
    // Small delay to allow toolbar clicks to register
    setTimeout(() => {
      isFocused = false;
    }, 100);
  }

  function toggleBlockMenu() {
    showBlockMenu = !showBlockMenu;
  }

  function closeBlockMenu() {
    showBlockMenu = false;
  }

  export function focus() {
    editorComponent?.focus();
  }

  export function getHTML(): string {
    return editorComponent?.getHTML() ?? '';
  }

  export function isEmpty(): boolean {
    return editorComponent?.isEmpty() ?? true;
  }
</script>

<svelte:window
  onclick={(e) => {
    // Close block menu if clicking outside
    const target = e.target as HTMLElement;
    if (!target.closest('.block-menu-container')) {
      closeBlockMenu();
    }
  }}
/>

<div class="spec-editor rounded-lg border border-[var(--color-border)] bg-[var(--color-bg-primary)] {className}">
  <!-- Toolbar -->
  {#if !readonly}
    <div class="flex flex-wrap items-center gap-0.5 p-1.5 border-b border-[var(--color-border)] bg-[var(--color-bg-secondary)]">
      <!-- Undo/Redo -->
      <div class="flex items-center gap-0.5 pr-2 border-r border-[var(--color-border-subtle)]">
        <Button
          variant="unstyled"
          onclick={undo}
          class="p-1.5 rounded hover:bg-[var(--color-bg-hover)] text-[var(--color-text-secondary)] transition-colors"
          title="Undo (Ctrl+Z)"
        >
          <Icon name="undo-2" size="sm" />
        </Button>
        <Button
          variant="unstyled"
          onclick={redo}
          class="p-1.5 rounded hover:bg-[var(--color-bg-hover)] text-[var(--color-text-secondary)] transition-colors"
          title="Redo (Ctrl+Y)"
        >
          <Icon name="redo-2" size="sm" />
        </Button>
      </div>

      <!-- Text formatting -->
      <div class="flex items-center gap-0.5 px-2 border-r border-[var(--color-border-subtle)]">
        <Button
          variant="unstyled"
          onclick={toggleBold}
          class="p-1.5 rounded transition-colors {activeBold
            ? 'bg-[var(--color-accent-primary)]/10 text-[var(--color-accent-primary)]'
            : 'hover:bg-[var(--color-bg-hover)] text-[var(--color-text-secondary)]'}"
          title="Bold (Ctrl+B)"
        >
          <Icon name="bold" size="sm" />
        </Button>
        <Button
          variant="unstyled"
          onclick={toggleItalic}
          class="p-1.5 rounded transition-colors {activeItalic
            ? 'bg-[var(--color-accent-primary)]/10 text-[var(--color-accent-primary)]'
            : 'hover:bg-[var(--color-bg-hover)] text-[var(--color-text-secondary)]'}"
          title="Italic (Ctrl+I)"
        >
          <Icon name="italic" size="sm" />
        </Button>
        <Button
          variant="unstyled"
          onclick={toggleStrike}
          class="p-1.5 rounded transition-colors {activeStrike
            ? 'bg-[var(--color-accent-primary)]/10 text-[var(--color-accent-primary)]'
            : 'hover:bg-[var(--color-bg-hover)] text-[var(--color-text-secondary)]'}"
          title="Strikethrough"
        >
          <Icon name="strikethrough" size="sm" />
        </Button>
        <Button
          variant="unstyled"
          onclick={toggleCode}
          class="p-1.5 rounded transition-colors {activeCode
            ? 'bg-[var(--color-accent-primary)]/10 text-[var(--color-accent-primary)]'
            : 'hover:bg-[var(--color-bg-hover)] text-[var(--color-text-secondary)]'}"
          title="Inline Code"
        >
          <Icon name="code" size="sm" />
        </Button>
      </div>

      <!-- Headings -->
      <div class="flex items-center gap-0.5 px-2 border-r border-[var(--color-border-subtle)]">
        <Button
          variant="unstyled"
          onclick={() => toggleHeading(1)}
          class="p-1.5 rounded text-xs font-bold transition-colors {activeH1
            ? 'bg-[var(--color-accent-primary)]/10 text-[var(--color-accent-primary)]'
            : 'hover:bg-[var(--color-bg-hover)] text-[var(--color-text-secondary)]'}"
          title="Heading 1"
        >
          H1
        </Button>
        <Button
          variant="unstyled"
          onclick={() => toggleHeading(2)}
          class="p-1.5 rounded text-xs font-bold transition-colors {activeH2
            ? 'bg-[var(--color-accent-primary)]/10 text-[var(--color-accent-primary)]'
            : 'hover:bg-[var(--color-bg-hover)] text-[var(--color-text-secondary)]'}"
          title="Heading 2"
        >
          H2
        </Button>
        <Button
          variant="unstyled"
          onclick={() => toggleHeading(3)}
          class="p-1.5 rounded text-xs font-bold transition-colors {activeH3
            ? 'bg-[var(--color-accent-primary)]/10 text-[var(--color-accent-primary)]'
            : 'hover:bg-[var(--color-bg-hover)] text-[var(--color-text-secondary)]'}"
          title="Heading 3"
        >
          H3
        </Button>
      </div>

      <!-- Lists -->
      <div class="flex items-center gap-0.5 px-2 border-r border-[var(--color-border-subtle)]">
        <Button
          variant="unstyled"
          onclick={toggleBulletList}
          class="p-1.5 rounded transition-colors {activeBulletList
            ? 'bg-[var(--color-accent-primary)]/10 text-[var(--color-accent-primary)]'
            : 'hover:bg-[var(--color-bg-hover)] text-[var(--color-text-secondary)]'}"
          title="Bullet List"
        >
          <Icon name="list" size="sm" />
        </Button>
        <Button
          variant="unstyled"
          onclick={toggleOrderedList}
          class="p-1.5 rounded transition-colors {activeOrderedList
            ? 'bg-[var(--color-accent-primary)]/10 text-[var(--color-accent-primary)]'
            : 'hover:bg-[var(--color-bg-hover)] text-[var(--color-text-secondary)]'}"
          title="Numbered List"
        >
          <Icon name="list-ordered" size="sm" />
        </Button>
      </div>

      <!-- Blocks -->
      <div class="flex items-center gap-0.5 px-2 border-r border-[var(--color-border-subtle)]">
        <Button
          variant="unstyled"
          onclick={toggleBlockquote}
          class="p-1.5 rounded transition-colors {activeBlockquote
            ? 'bg-[var(--color-accent-primary)]/10 text-[var(--color-accent-primary)]'
            : 'hover:bg-[var(--color-bg-hover)] text-[var(--color-text-secondary)]'}"
          title="Quote"
        >
          <Icon name="quote" size="sm" />
        </Button>
        <Button
          variant="unstyled"
          onclick={toggleCodeBlock}
          class="p-1.5 rounded transition-colors {activeCodeBlock
            ? 'bg-[var(--color-accent-primary)]/10 text-[var(--color-accent-primary)]'
            : 'hover:bg-[var(--color-bg-hover)] text-[var(--color-text-secondary)]'}"
          title="Code Block"
        >
          <Icon name="file-code" size="sm" />
        </Button>
        <Button
          variant="unstyled"
          onclick={insertTable}
          class="p-1.5 rounded hover:bg-[var(--color-bg-hover)] text-[var(--color-text-secondary)] transition-colors"
          title="Insert Table"
        >
          <Icon name="table" size="sm" />
        </Button>
        <Button
          variant="unstyled"
          onclick={insertHorizontalRule}
          class="p-1.5 rounded hover:bg-[var(--color-bg-hover)] text-[var(--color-text-secondary)] transition-colors"
          title="Horizontal Rule"
        >
          <Icon name="minus" size="sm" />
        </Button>
      </div>

      <!-- Custom Blocks Dropdown -->
      <div class="relative block-menu-container px-2">
        <Button
          variant="unstyled"
          onclick={toggleBlockMenu}
          class="flex items-center gap-1 px-2 py-1.5 rounded hover:bg-[var(--color-bg-hover)] text-[var(--color-text-secondary)] transition-colors"
          title="Insert Block"
        >
          <Icon name="plus" size="sm" />
          <span class="text-xs">Insert</span>
          <Icon name="chevron-down" size="xs" />
        </Button>

        {#if showBlockMenu}
          <div
            class="absolute top-full left-0 mt-1 w-56 bg-[var(--color-bg-primary)] border border-[var(--color-border)] rounded-lg shadow-lg z-50"
          >
            <div class="p-1">
              <Button
                variant="unstyled"
                onclick={insertDecisionBlock}
                class="w-full flex items-center gap-2 px-3 py-2 text-left rounded hover:bg-[var(--color-bg-hover)] transition-colors"
              >
                <span class="text-lg">🎯</span>
                <div>
                  <div class="text-sm font-medium text-[var(--color-text-primary)]">Decision</div>
                  <div class="text-xs text-[var(--color-text-muted)]">Document a decision</div>
                </div>
              </Button>
              <Button
                variant="unstyled"
                onclick={insertAcceptanceCriteriaBlock}
                class="w-full flex items-center gap-2 px-3 py-2 text-left rounded hover:bg-[var(--color-bg-hover)] transition-colors"
              >
                <span class="text-lg">✅</span>
                <div>
                  <div class="text-sm font-medium text-[var(--color-text-primary)]">Acceptance Criteria</div>
                  <div class="text-xs text-[var(--color-text-muted)]">Given/When/Then checklist</div>
                </div>
              </Button>
              <Button
                variant="unstyled"
                onclick={insertRiskBlock}
                class="w-full flex items-center gap-2 px-3 py-2 text-left rounded hover:bg-[var(--color-bg-hover)] transition-colors"
              >
                <span class="text-lg">⚠️</span>
                <div>
                  <div class="text-sm font-medium text-[var(--color-text-primary)]">Risk</div>
                  <div class="text-xs text-[var(--color-text-muted)]">Document a risk assessment</div>
                </div>
              </Button>
            </div>
          </div>
        {/if}
      </div>
    </div>
  {/if}

  <!-- Editor -->
  <div class="editor-content" style="min-height: {minHeight}">
    <TiptapEditor
      bind:this={editorComponent}
      {content}
      {placeholder}
      editable={!readonly}
      extensions={customExtensions}
      {onUpdate}
      onFocus={handleFocus}
      onBlur={handleBlur}
      class="text-[var(--color-text-primary)]"
    />
  </div>
</div>

<style>
  .spec-editor {
    overflow: hidden;
  }

  .editor-content {
    overflow-y: auto;
  }
</style>
