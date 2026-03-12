<!--
  MessageInput Component
  Unified chat input with textarea, send button, bottom toolbar, and markdown toolbar/context menu
-->
<script lang="ts">
  import { untrack } from 'svelte';
  import { Icon, Button, Textarea } from '@sddp/ui';
  import { ContextMenu, renderMarkdown } from '@sddp/shell';
  import type { ContextMenuItem } from '@sddp/shell';
  import MessageTypeIcon from './MessageTypeIcon.svelte';
  import { HUMAN_MESSAGE_TYPES, type MessageType, type Message, type CreateMessageRequest } from '../../types';
  import { getConversationDraft, setConversationDraft, clearConversationDraft } from '../../stores';

  interface Props {
    disabled?: boolean;
    placeholder?: string;
    showToolbar?: boolean;
    replyTo?: Message | null;
    onCancelReply?: () => void;
    onSubmit: (message: CreateMessageRequest) => void;
    onTypingStart?: () => void;
    onTypingStop?: () => void;
    draftKey?: string;
    class?: string;
  }

  let {
    disabled = false,
    placeholder = 'Type your message...',
    showToolbar = false,
    replyTo = null,
    onCancelReply,
    onSubmit,
    onTypingStart,
    onTypingStop,
    draftKey,
    class: className = '',
  }: Props = $props();

  let content = $state('');
  let selectedType = $state<MessageType>('Normal');
  let showTypeSelector = $state(false);
  let typingTimeout: ReturnType<typeof setTimeout> | null = null;
  let isTyping = $state(false);
  let lastDraftKey = $state<string | null>(null);
  let inputMode = $state<'write' | 'preview'>('write');
  let textareaEl: HTMLTextAreaElement | undefined = $state();
  let contentAreaEl: HTMLDivElement | undefined = $state();
  let lockedHeight = $state<string | undefined>(undefined);
  let isFocused = $state(false);

  function setInputMode(mode: 'write' | 'preview'): void {
    if (contentAreaEl) {
      lockedHeight = `${contentAreaEl.offsetHeight}px`;
    }
    inputMode = mode;
    if (mode === 'write') {
      requestAnimationFrame(() => {
        autoResize();
        lockedHeight = undefined;
      });
    }
  }

  // Markdown context menu state
  let ctxMenuVisible = $state(false);
  let ctxMenuX = $state(0);
  let ctxMenuY = $state(0);
  let savedSelStart = $state(0);
  let savedSelEnd = $state(0);

  const canSubmit = $derived(content.trim().length > 0 && !disabled);

  function loadDraft(): void {
    if (!draftKey) return;
    const draft = getConversationDraft(draftKey);
    content = draft?.content ?? '';
    selectedType = draft?.type ?? 'Normal';
  }

  function persistDraft(): void {
    if (!draftKey) return;
    setConversationDraft(draftKey, {
      content,
      type: selectedType,
      updatedAt: Date.now(),
    });
  }

  $effect(() => {
    if (!draftKey || draftKey === lastDraftKey) return;
    lastDraftKey = draftKey;
    untrack(() => loadDraft());
  });

  function handleInput(): void {
    if (!isTyping) {
      isTyping = true;
      onTypingStart?.();
    }

    if (typingTimeout) {
      clearTimeout(typingTimeout);
    }

    typingTimeout = setTimeout(() => {
      isTyping = false;
      onTypingStop?.();
    }, 1000);

    persistDraft();
    autoResize();
  }

  function autoResize(): void {
    if (!textareaEl) return;
    textareaEl.style.height = 'auto';
    const maxHeight = showToolbar ? 320 : 160;
    textareaEl.style.height = `${Math.min(textareaEl.scrollHeight, maxHeight)}px`;
    textareaEl.style.overflowY = textareaEl.scrollHeight > maxHeight ? 'auto' : 'hidden';
  }

  function handleSubmit(): void {
    if (!canSubmit) return;

    onSubmit({
      type: selectedType,
      content: content.trim(),
      replyToId: replyTo?.id,
    });

    content = '';
    inputMode = 'write';
    isTyping = false;
    onTypingStop?.();
    onCancelReply?.();
    if (typingTimeout) {
      clearTimeout(typingTimeout);
      typingTimeout = null;
    }
    if (draftKey) {
      clearConversationDraft(draftKey);
    }

    requestAnimationFrame(() => {
      if (textareaEl) {
        textareaEl.style.height = 'auto';
      }
    });
  }

  function handleKeyDown(e: KeyboardEvent): void {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      handleSubmit();
    }
  }

  function selectType(type: MessageType): void {
    selectedType = type;
    showTypeSelector = false;
    persistDraft();
  }

  // --- Markdown formatting ---

  // Get current selection from textarea (for toolbar mode, selection is live)
  function getSelection(): { start: number; end: number } {
    if (showToolbar && textareaEl) {
      return { start: textareaEl.selectionStart, end: textareaEl.selectionEnd };
    }
    return { start: savedSelStart, end: savedSelEnd };
  }

  // Context menu handler (only used when showToolbar is false)
  function handleContextMenu(e: MouseEvent): void {
    if (showToolbar || !textareaEl) return;
    e.preventDefault();
    savedSelStart = textareaEl.selectionStart;
    savedSelEnd = textareaEl.selectionEnd;
    ctxMenuX = e.clientX;
    ctxMenuY = e.clientY;
    ctxMenuVisible = true;
  }

  function applyWrap(prefix: string, suffix: string): void {
    if (!textareaEl) return;
    const { start, end } = getSelection();
    const selected = content.slice(start, end);
    const before = content.slice(0, start);
    const after = content.slice(end);
    content = before + prefix + selected + suffix + after;
    persistDraft();

    requestAnimationFrame(() => {
      if (!textareaEl) return;
      textareaEl.focus();
      textareaEl.selectionStart = start + prefix.length;
      textareaEl.selectionEnd = end + prefix.length;
      autoResize();
    });
  }

  function applyLinePrefix(prefix: string): void {
    if (!textareaEl) return;
    const { start, end } = getSelection();
    const selected = content.slice(start, end);
    const before = content.slice(0, start);
    const after = content.slice(end);

    const lines = selected.split('\n');
    const formatted = lines.map((line) => prefix + line).join('\n');
    content = before + formatted + after;
    persistDraft();

    requestAnimationFrame(() => {
      if (!textareaEl) return;
      textareaEl.focus();
      textareaEl.selectionStart = start;
      textareaEl.selectionEnd = start + formatted.length;
      autoResize();
    });
  }

  // Toolbar button handler: preventDefault keeps textarea focus + selection
  function toolbarAction(e: MouseEvent, action: () => void): void {
    e.preventDefault();
    action();
  }

  function toolbarLink(e: MouseEvent): void {
    e.preventDefault();
    const { start, end } = getSelection();
    if (end > start) {
      applyWrap('[', '](url)');
    } else {
      applyWrap('[text](', ')');
    }
  }

  // Context menu items (only used when showToolbar is false)
  function buildContextMenuItems(): ContextMenuItem[] {
    const hasSelection = savedSelEnd > savedSelStart;
    return [
      {
        id: 'bold',
        label: 'Bold',
        icon: 'edit',
        shortcut: 'Ctrl+B',
        disabled: !hasSelection,
        action: () => applyWrap('**', '**'),
      },
      {
        id: 'italic',
        label: 'Italic',
        icon: 'edit',
        shortcut: 'Ctrl+I',
        disabled: !hasSelection,
        action: () => applyWrap('*', '*'),
      },
      {
        id: 'strikethrough',
        label: 'Strikethrough',
        icon: 'minus',
        disabled: !hasSelection,
        action: () => applyWrap('~~', '~~'),
      },
      {
        id: 'inline-code',
        label: 'Inline Code',
        icon: 'code',
        disabled: !hasSelection,
        action: () => applyWrap('`', '`'),
      },
      { id: 'sep1', label: '', separator: true },
      {
        id: 'code-block',
        label: 'Code Block',
        icon: 'terminal',
        action: () => applyWrap('```\n', '\n```'),
      },
      {
        id: 'blockquote',
        label: 'Blockquote',
        icon: 'chevron-right',
        action: () => applyLinePrefix('> '),
      },
      { id: 'sep2', label: '', separator: true },
      {
        id: 'heading',
        label: 'Heading',
        icon: 'hash',
        action: () => applyLinePrefix('## '),
      },
      {
        id: 'bullet-list',
        label: 'Bullet List',
        icon: 'list',
        action: () => applyLinePrefix('- '),
      },
      {
        id: 'link',
        label: 'Link',
        icon: 'link',
        action: () => {
          if (savedSelEnd > savedSelStart) {
            applyWrap('[', '](url)');
          } else {
            applyWrap('[text](', ')');
          }
        },
      },
    ];
  }

  const typeLabels: Record<MessageType, string> = {
    Normal: 'Normal',
    Proposal: 'Proposal',
    Question: 'Question',
    Objection: 'Objection',
    Reference: 'Reference',
    Decision: 'Decision',
    AiReminder: 'AI Reminder',
    AiSummary: 'AI Summary',
    AiSuggestion: 'AI Suggestion',
  };
</script>

<!-- Unified input container -->
<div class="px-3 py-2 {className}">
  <div
    class="relative flex flex-col rounded-xl border transition-colors {isFocused
      ? 'border-[var(--color-accent-primary)] shadow-sm'
      : 'border-[var(--color-border-secondary)]'} bg-[var(--color-bg-primary)]"
  >
    <!-- Reply preview bar -->
    {#if replyTo}
      <div class="flex items-center gap-2 px-3 py-1.5 rounded-t-[11px] border-b border-[var(--color-border-secondary)] bg-[var(--color-surface-50)]">
        <Icon name="corner-up-left" size="xs" class="text-[var(--color-accent-primary)] flex-shrink-0" />
        <span class="text-xs text-[var(--color-text-secondary)]">
          Replying to <span class="font-semibold">{replyTo.sender?.name ?? 'Unknown'}</span>
        </span>
        <span class="text-xs text-[var(--color-text-muted)] truncate flex-1 min-w-0">
          {replyTo.content.length > 60 ? replyTo.content.slice(0, 60) + '…' : replyTo.content}
        </span>
        <Button
          variant="unstyled"
          class="flex-shrink-0 w-5 h-5 flex items-center justify-center rounded
            text-[var(--color-text-muted)] hover:text-[var(--color-text-primary)]
            hover:bg-[var(--color-surface-200)] transition-colors"
          title="Cancel reply"
          onclick={() => onCancelReply?.()}
        >
          <Icon name="x" size="xs" />
        </Button>
      </div>
    {/if}

    <!-- Markdown formatting toolbar (topic mode only) -->
    {#if showToolbar && inputMode === 'write'}
      <div class="md-toolbar flex items-center gap-0.5 px-2 pt-1.5 pb-0.5 border-b border-[var(--color-border-secondary)]">
        <!-- Text formatting -->
        <Button variant="unstyled" class="md-toolbar__btn" title="Heading (## )" onmousedown={(e) => toolbarAction(e, () => applyLinePrefix('## '))}>
          <span class="md-toolbar__text font-bold">H</span>
        </Button>
        <Button variant="unstyled" class="md-toolbar__btn" title="Bold (**text**)" onmousedown={(e) => toolbarAction(e, () => applyWrap('**', '**'))}>
          <span class="md-toolbar__text font-bold">B</span>
        </Button>
        <Button variant="unstyled" class="md-toolbar__btn" title="Italic (*text*)" onmousedown={(e) => toolbarAction(e, () => applyWrap('*', '*'))}>
          <span class="md-toolbar__text italic">I</span>
        </Button>
        <Button variant="unstyled" class="md-toolbar__btn" title="Strikethrough (~~text~~)" onmousedown={(e) => toolbarAction(e, () => applyWrap('~~', '~~'))}>
          <span class="md-toolbar__text line-through">S</span>
        </Button>

        <span class="md-toolbar__sep"></span>

        <!-- Block formatting -->
        <Button variant="unstyled" class="md-toolbar__btn" title="Inline code" onmousedown={(e) => toolbarAction(e, () => applyWrap('`', '`'))}>
          <Icon name="code" size="xs" />
        </Button>
        <Button variant="unstyled" class="md-toolbar__btn" title="Code block" onmousedown={(e) => toolbarAction(e, () => applyWrap('```\n', '\n```'))}>
          <span class="md-toolbar__text font-mono text-[0.5625rem]">{'{}'}</span>
        </Button>
        <Button variant="unstyled" class="md-toolbar__btn" title="Blockquote (> )" onmousedown={(e) => toolbarAction(e, () => applyLinePrefix('> '))}>
          <span class="md-toolbar__text text-[0.8125rem]">"</span>
        </Button>

        <span class="md-toolbar__sep"></span>

        <!-- List & link -->
        <Button variant="unstyled" class="md-toolbar__btn" title="Bullet list (- )" onmousedown={(e) => toolbarAction(e, () => applyLinePrefix('- '))}>
          <Icon name="list" size="xs" />
        </Button>
        <Button variant="unstyled" class="md-toolbar__btn" title="Numbered list (1. )" onmousedown={(e) => toolbarAction(e, () => applyLinePrefix('1. '))}>
          <span class="md-toolbar__text font-mono text-[0.5625rem]">1.</span>
        </Button>
        <Button variant="unstyled" class="md-toolbar__btn" title="Link ([text](url))" onmousedown={toolbarLink}>
          <Icon name="link" size="xs" />
        </Button>
      </div>
    {/if}

    <!-- Textarea / Preview area + Send button -->
    <div bind:this={contentAreaEl} class="relative flex items-start pr-10" style:min-height={lockedHeight}>
      {#if inputMode === 'write'}
        <Textarea
          unstyled
          bind:element={textareaEl}
          bind:value={content}
          oninput={handleInput}
          onkeydown={handleKeyDown}
          oncontextmenu={handleContextMenu}
          onfocus={() => (isFocused = true)}
          onblur={() => (isFocused = false)}
          {placeholder}
          rows={1}
          class="flex-1 px-3.5 pt-3 pb-2 text-sm text-[var(--color-text-primary)] placeholder:text-[var(--color-text-muted)] bg-transparent resize-none focus:outline-none {showToolbar ? 'min-h-[9.75rem] max-h-[20rem]' : 'min-h-[5rem] max-h-[10rem] overflow-y-hidden'}"
          {disabled}
        />
      {:else}
        <div
          class="flex-1 px-3.5 pt-3 pb-2 text-sm text-[var(--color-text-primary)] overflow-y-auto prose-message {showToolbar ? 'min-h-[9.75rem] max-h-[20rem]' : 'min-h-[5rem]'}"
          role="region"
          aria-label="Markdown preview"
        >
          {#if content.trim()}
            <!-- eslint-disable-next-line svelte/no-at-html-tags -->
            {@html renderMarkdown(content)}
          {:else}
            <span class="text-[var(--color-text-muted)]">{placeholder}</span>
          {/if}
        </div>
      {/if}

      <!-- Send button (top-right, inside container) -->
      <div class="absolute top-2 right-2">
        <Button
          variant="unstyled"
          disabled={!canSubmit}
          onclick={handleSubmit}
          class="flex items-center justify-center w-7 h-7 rounded-lg transition-colors {canSubmit
            ? 'bg-[var(--color-accent-primary)] text-white hover:opacity-90 cursor-pointer'
            : 'bg-[var(--color-surface-200)] text-[var(--color-text-muted)] cursor-not-allowed'}"
          title="Send (Enter)"
        >
          <Icon name="arrow-up" size="sm" />
        </Button>
      </div>
    </div>

    <!-- Bottom toolbar (no divider, distinguished by typography) -->
    <div class="flex items-center gap-2 px-2 pb-2 pt-0">
      <!-- Message Type selector -->
      <div class="relative">
        <Button
          variant="unstyled"
          onclick={() => (showTypeSelector = !showTypeSelector)}
          class="flex items-center gap-1.5 px-2 h-6 text-[0.6875rem] rounded-md hover:bg-[var(--color-surface-200)] transition-colors text-[var(--color-text-secondary)]"
          {disabled}
        >
          <MessageTypeIcon type={selectedType} size="sm" />
          <span>{typeLabels[selectedType]}</span>
          <Icon name="chevron-down" size="xs" />
        </Button>

        {#if showTypeSelector}
          <div
            class="absolute bottom-full left-0 mb-1 bg-[var(--color-surface-100)] border border-[var(--color-border-primary)] rounded-lg shadow-lg py-1 min-w-[160px] z-20"
          >
            {#each HUMAN_MESSAGE_TYPES as type (type)}
              <Button
                variant="unstyled"
                onclick={() => selectType(type)}
                class="flex items-center gap-2 w-full px-3 py-1.5 hover:bg-[var(--color-surface-200)] transition-colors
                  {selectedType === type ? 'bg-[var(--color-surface-200)]' : ''}"
              >
                <MessageTypeIcon {type} size="sm" />
                <span class="text-xs text-[var(--color-text-primary)]">{typeLabels[type]}</span>
              </Button>
            {/each}
          </div>
        {/if}
      </div>

      <!-- Write / Preview toggle -->
      <div class="flex items-center h-6 rounded-md overflow-hidden">
        <Button
          variant="unstyled"
          onclick={() => setInputMode('write')}
          class="px-2 h-full text-[0.6875rem] font-medium rounded-l-md transition-colors {inputMode === 'write'
            ? 'bg-[var(--color-surface-200)] text-[var(--color-text-primary)]'
            : 'text-[var(--color-text-muted)] hover:text-[var(--color-text-secondary)] hover:bg-[var(--color-surface-100)]'}"
        >
          Write
        </Button>
        <Button
          variant="unstyled"
          onclick={() => setInputMode('preview')}
          class="px-2 h-full text-[0.6875rem] font-medium rounded-r-md transition-colors {inputMode === 'preview'
            ? 'bg-[var(--color-surface-200)] text-[var(--color-text-primary)]'
            : 'text-[var(--color-text-muted)] hover:text-[var(--color-text-secondary)] hover:bg-[var(--color-surface-100)]'}"
        >
          Preview
        </Button>
      </div>

      <!-- Spacer + keyboard hint -->
      <span class="ml-auto text-[0.625rem] text-[var(--color-text-muted)] select-none">
        Enter &middot; Shift+Enter
      </span>
    </div>
  </div>
</div>

<!-- Click outside to close type selector -->
{#if showTypeSelector}
  <Button
    variant="unstyled"
    class="fixed inset-0 z-10"
    onclick={() => (showTypeSelector = false)}
    aria-label="Close type selector"
  ></Button>
{/if}

<!-- Markdown formatting context menu (channel mode only) -->
{#if !showToolbar}
  <ContextMenu
    visible={ctxMenuVisible}
    x={ctxMenuX}
    y={ctxMenuY}
    items={buildContextMenuItems()}
    onClose={() => { ctxMenuVisible = false; }}
  />
{/if}

<style>
  /* Markdown formatting toolbar — :global() needed because targets are inside Button child components */
  .md-toolbar :global(.md-toolbar__btn) {
    display: flex;
    align-items: center;
    justify-content: center;
    width: 26px;
    height: 24px;
    border: none;
    border-radius: 4px;
    background: transparent;
    color: var(--color-text-secondary);
    cursor: pointer;
    transition: background-color 0.1s ease, color 0.1s ease;
  }

  .md-toolbar :global(.md-toolbar__btn:hover) {
    background: var(--color-surface-200);
    color: var(--color-text-primary);
  }

  .md-toolbar :global(.md-toolbar__text) {
    font-size: 12px;
    line-height: 1;
    user-select: none;
  }

  .md-toolbar__sep {
    width: 1px;
    height: 14px;
    margin: 0 4px;
    background: var(--color-border-secondary);
    flex-shrink: 0;
  }

  /* Compact markdown preview styling for message input */
  .prose-message :global(h1),
  .prose-message :global(h2),
  .prose-message :global(h3),
  .prose-message :global(h4),
  .prose-message :global(h5),
  .prose-message :global(h6) {
    font-weight: 600;
    margin: 0.25em 0;
  }
  .prose-message :global(h1) { font-size: 1.25em; }
  .prose-message :global(h2) { font-size: 1.125em; }
  .prose-message :global(h3) { font-size: 1em; }
  .prose-message :global(strong) { font-weight: 600; }
  .prose-message :global(em) { font-style: italic; }
  .prose-message :global(del) { text-decoration: line-through; opacity: 0.7; }
  .prose-message :global(code.md-inline-code) {
    padding: 0.125rem 0.375rem;
    border-radius: 0.25rem;
    font-size: 0.85em;
    background: var(--color-surface-200);
  }
  .prose-message :global(pre.md-code-block) {
    padding: 0.5rem 0.75rem;
    border-radius: 0.375rem;
    font-size: 0.8em;
    background: var(--color-surface-200);
    overflow-x: auto;
    margin: 0.25em 0;
  }
  .prose-message :global(pre.md-code-block code) {
    background: none;
    padding: 0;
  }
  .prose-message :global(blockquote.md-blockquote) {
    border-left: 3px solid var(--color-border-secondary);
    padding-left: 0.75rem;
    color: var(--color-text-secondary);
    margin: 0.25em 0;
  }
  .prose-message :global(ul.md-list),
  .prose-message :global(ol.md-list) {
    padding-left: 1.25rem;
    margin: 0.25em 0;
  }
  .prose-message :global(ul.md-list) { list-style-type: disc; }
  .prose-message :global(ol.md-list) { list-style-type: decimal; }
  .prose-message :global(a.md-link) {
    color: var(--color-accent-primary);
    text-decoration: underline;
  }
  .prose-message :global(a.md-link:hover) {
    opacity: 0.8;
  }
</style>
