<!--
  MessageBubble Component
  Displays a single message with type-based styling
-->
<script lang="ts">
  import { Icon, Button, Textarea } from '@sddp/ui';
  import { SurfaceCard, capitalize, truncate, formatDateByPreference, getAvatarHexColor, AI_AVATAR_COLOR, parseAvatarUrl, getPresetAvatarEmoji, getAvatarInitials, renderMarkdown } from '@sddp/shell';
  import MessageTypeIcon from './MessageTypeIcon.svelte';
  import DecisionImpactBadge from './DecisionImpactBadge.svelte';
  import { MESSAGE_TYPE_STYLES, MESSAGE_TYPE_WIDTH, AI_MESSAGE_TYPES, type Message } from '../../types';
  import type { ConversationViewMode } from '../../stores';

  interface Props {
    message: Message;
    isOwn?: boolean;
    isLast?: boolean;
    viewMode?: ConversationViewMode;
    projectId?: string;
    showToolbar?: boolean;
    class?: string;
    onReferenceClick?: (reference: ReferenceMeta) => void;
    onExtractRequirement?: (message: Message) => void;
    onEdit?: (message: Message, newContent: string) => void;
    onDelete?: (message: Message) => void;
    onReply?: (message: Message) => void;
    messages?: Message[];
    currentUserRole?: string;
  }

  let {
    message,
    isOwn = false,
    isLast = false,
    viewMode = 'slack',
    projectId = '',
    showToolbar = false,
    class: className = '',
    onReferenceClick,
    onExtractRequirement,
    onEdit,
    onDelete,
    onReply,
    messages = [],
    currentUserRole = 'Member',
  }: Props = $props();

  // Edit/Delete state
  let showMenu = $state(false);
  let isEditing = $state(false);
  let editContent = $state('');
  let showDeleteConfirm = $state(false);
  let editTextareaEl: HTMLTextAreaElement | undefined = $state();

  // Markdown toolbar helpers for edit mode
  function editApplyWrap(prefix: string, suffix: string): void {
    if (!editTextareaEl) return;
    const start = editTextareaEl.selectionStart;
    const end = editTextareaEl.selectionEnd;
    const selected = editContent.slice(start, end);
    editContent = editContent.slice(0, start) + prefix + selected + suffix + editContent.slice(end);
    requestAnimationFrame(() => {
      if (!editTextareaEl) return;
      editTextareaEl.focus();
      editTextareaEl.selectionStart = start + prefix.length;
      editTextareaEl.selectionEnd = end + prefix.length;
    });
  }

  function editApplyLinePrefix(prefix: string): void {
    if (!editTextareaEl) return;
    const start = editTextareaEl.selectionStart;
    const end = editTextareaEl.selectionEnd;
    const selected = editContent.slice(start, end);
    const formatted = selected.split('\n').map((line) => prefix + line).join('\n');
    editContent = editContent.slice(0, start) + formatted + editContent.slice(end);
    requestAnimationFrame(() => {
      if (!editTextareaEl) return;
      editTextareaEl.focus();
      editTextareaEl.selectionStart = start;
      editTextareaEl.selectionEnd = start + formatted.length;
    });
  }

  function editToolbarAction(e: MouseEvent, action: () => void): void {
    e.preventDefault();
    action();
  }

  function editToolbarLink(e: MouseEvent): void {
    e.preventDefault();
    if (!editTextareaEl) return;
    if (editTextareaEl.selectionEnd > editTextareaEl.selectionStart) {
      editApplyWrap('[', '](url)');
    } else {
      editApplyWrap('[text](', ')');
    }
  }

  const canEdit = $derived(isOwn && !!onEdit);
  const canDelete = $derived(
    !!onDelete && (isOwn || currentUserRole === 'Moderator' || currentUserRole === 'Owner')
  );
  const canReply = $derived(!!onReply);
  const hasActions = $derived(canEdit || canDelete || canReply);

  // Find the message being replied to
  const replyToMessage = $derived(
    message.replyToId ? messages.find((m) => m.id === message.replyToId) ?? null : null
  );

  // Close menu on outside click
  $effect(() => {
    if (!showMenu) return;
    function onWindowClick() {
      showMenu = false;
    }
    // Defer to next tick so the toggle click doesn't immediately close
    const timer = setTimeout(() => {
      window.addEventListener('click', onWindowClick);
    }, 0);
    return () => {
      clearTimeout(timer);
      window.removeEventListener('click', onWindowClick);
    };
  });

  function handleMenuToggle(e: MouseEvent): void {
    e.stopPropagation();
    showMenu = !showMenu;
  }

  function handleStartEdit(): void {
    showMenu = false;
    isEditing = true;
    editContent = message.content;
  }

  function handleCancelEdit(): void {
    isEditing = false;
    editContent = '';
  }

  function handleSaveEdit(): void {
    const trimmed = editContent.trim();
    if (!trimmed || trimmed === message.content) {
      handleCancelEdit();
      return;
    }
    onEdit?.(message, trimmed);
    isEditing = false;
    editContent = '';
  }

  function handleEditKeydown(e: KeyboardEvent): void {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      handleSaveEdit();
    } else if (e.key === 'Escape') {
      handleCancelEdit();
    }
  }

  function handleStartDelete(): void {
    showMenu = false;
    showDeleteConfirm = true;
  }

  function handleConfirmDelete(): void {
    showDeleteConfirm = false;
    onDelete?.(message);
  }

  function handleCancelDelete(): void {
    showDeleteConfirm = false;
  }

  function handleReply(): void {
    showMenu = false;
    onReply?.(message);
  }

  function handleCloseMenu(): void {
    showMenu = false;
  }

  // Last message gets subtle highlight effect
  const lastMessageClass = $derived(
    isLast ? 'ring-1 ring-[var(--color-accent-primary)]/20 shadow-sm' : ''
  );

  const style = $derived(MESSAGE_TYPE_STYLES[message.type] ?? MESSAGE_TYPE_STYLES.Normal);
  const widthTier = $derived(MESSAGE_TYPE_WIDTH[message.type] ?? 'compact');
  const bubbleWidthClass = $derived(
    widthTier === 'compact' ? 'max-w-[55%]'
    : widthTier === 'standard' ? 'max-w-[72%]'
    : 'max-w-[88%]'
  );
  const isAiMessage = $derived(AI_MESSAGE_TYPES.includes(message.type));
  const senderAvatar = $derived(parseAvatarUrl(message.sender?.avatarUrl));
  const avatarText = $derived(
    isAiMessage ? 'AI' : getAvatarInitials(message.sender?.name ?? '')
  );

  const avatarBgStyle = $derived(
    isAiMessage ? '' : `background-color: ${getAvatarHexColor(message.sender?.name ?? '')}`
  );
  const avatarBgClass = $derived(
    isAiMessage ? AI_AVATAR_COLOR : ''
  );
  const slackBorderClass = $derived(
    isAiMessage
      ? 'border-dotted border-[var(--color-accent-primary)]'
      : 'border-[var(--color-border-secondary)]'
  );
  const logBorderClass = $derived(
    isAiMessage
      ? 'border-dotted border-[var(--color-accent-primary)]'
      : style.borderColor
  );
  const documentBorderClass = $derived(
    isAiMessage
      ? 'border-dotted border-[var(--color-accent-primary)]'
      : 'border-[var(--color-border-secondary)]'
  );

  type ReferenceType = 'spec' | 'conversation' | 'requirement' | 'glossary' | 'report' | 'url' | 'text';
  type ReferenceMeta = {
    raw: string;
    type: ReferenceType;
    label: string;
    id?: string;
    href?: string;
  };

  const isDecision = $derived(message.type === 'Decision');
  const canExtractRequirement = $derived(
    !!onExtractRequirement && (isDecision || message.type === 'AiSummary')
  );

  const referenceItems = $derived.by(() => {
    if (!message.references || message.references.length === 0) return [];
    return message.references
      .filter((ref) => ref && ref.trim().length > 0)
      .map((ref) => parseReference(ref));
  });
  const reminderReference = $derived(
    referenceItems.find((ref) => ref.type === 'spec' || ref.type === 'conversation' || ref.type === 'requirement') ?? null
  );

  // Reactive time display - updates every 30 seconds for relative time
  let timeTick = $state(0);
  const formattedTime = $derived.by(() => {
    // timeTick dependency triggers re-computation
    void timeTick;
    return formatDateByPreference(message.createdAt);
  });

  $effect(() => {
    // Update every 30 seconds for "X min ago" style displays
    const interval = setInterval(() => {
      timeTick++;
    }, 30_000);
    return () => clearInterval(interval);
  });

  function parseReference(ref: string): ReferenceMeta {
    const trimmed = ref.trim();
    if (/^https?:\/\//i.test(trimmed)) {
      try {
        const url = new URL(trimmed);
        return { raw: trimmed, type: 'url', label: url.host, href: trimmed };
      } catch {
        return { raw: trimmed, type: 'url', label: trimmed, href: trimmed };
      }
    }

    const match = trimmed.match(/^(spec|conversation|requirement|glossary|report)[/:#-](.+)$/i);
    if (match) {
      const type = match[1]!.toLowerCase() as ReferenceType;
      const id = match[2]!;
      return { raw: trimmed, type, id, label: formatReferenceLabel(type, id) };
    }

    return { raw: trimmed, type: 'text', label: trimmed };
  }

  function formatReferenceLabel(type: ReferenceType, id: string): string {
    const label = capitalize(type);
    const shortId = truncate(id, 8, '…');
    return `${label} ${shortId}`;
  }

  function getReferenceIcon(type: ReferenceType): string {
    switch (type) {
      case 'spec':
        return 'file-signature';
      case 'conversation':
        return 'message-square';
      case 'requirement':
        return 'file-text';
      case 'glossary':
        return 'file-text';
      case 'report':
        return 'cpu';
      case 'url':
        return 'external-link';
      default:
        return 'link';
    }
  }

  function handleReferenceNavigate(reference: ReferenceMeta): void {
    if (onReferenceClick) {
      onReferenceClick(reference);
      return;
    }

    if (reference.type === 'spec' || reference.type === 'conversation' || reference.type === 'requirement' || reference.type === 'glossary') {
      if (typeof window === 'undefined' || !reference.id) return;
      window.dispatchEvent(
        new CustomEvent('sddp:navigate', {
          detail: {
            type: reference.type,
            id: reference.id,
            label: reference.label,
          },
        })
      );
    }
  }

  function handleExtractRequirement(): void {
    if (!onExtractRequirement) return;
    onExtractRequirement(message);
  }

  function handleReminderDetail(): void {
    if (!reminderReference) return;
    handleReferenceNavigate(reminderReference);
  }
</script>

{#if viewMode === 'log'}
  <!-- svelte-ignore a11y_click_events_have_key_events -->
  <!-- svelte-ignore a11y_no_static_element_interactions -->
  <div class="group relative flex gap-4 {className}" data-message-id={message.id} onclick={handleCloseMenu}>
    <!-- Actions menu button (hover) -->
    {#if hasActions && !isEditing && !showDeleteConfirm}
      <Button
        variant="unstyled"
        class="absolute top-0 right-0 opacity-0 group-hover:opacity-100
          w-6 h-6 flex items-center justify-center rounded
          bg-[var(--color-bg-secondary)] border border-[var(--color-border)]
          text-[var(--color-text-tertiary)] hover:text-[var(--color-text-primary)]
          shadow-sm transition-all z-10"
        title="Message actions"
        onclick={handleMenuToggle}
      >
        <Icon name="more-horizontal" size="xs" />
      </Button>

      {#if showMenu}
        <div class="absolute top-0 right-8 z-20
          bg-[var(--color-bg-secondary)] border border-[var(--color-border)]
          rounded-md shadow-lg py-1 min-w-[120px]">
          {#if canReply}
            <Button
              variant="unstyled"
              class="w-full flex items-center gap-2 px-3 py-1.5 text-sm text-[var(--color-text-primary)]
                hover:bg-[var(--color-bg-tertiary)] transition-colors text-left"
              onclick={(e) => { e.stopPropagation(); handleReply(); }}
            >
              <Icon name="corner-up-left" size="xs" />
              Reply
            </Button>
          {/if}
          {#if canEdit}
            <Button
              variant="unstyled"
              class="w-full flex items-center gap-2 px-3 py-1.5 text-sm text-[var(--color-text-primary)]
                hover:bg-[var(--color-bg-tertiary)] transition-colors text-left"
              onclick={(e) => { e.stopPropagation(); handleStartEdit(); }}
            >
              <Icon name="pencil" size="xs" />
              Edit
            </Button>
          {/if}
          {#if canDelete}
            <Button
              variant="unstyled"
              class="w-full flex items-center gap-2 px-3 py-1.5 text-sm text-[var(--color-error-600)]
                hover:bg-[var(--color-error-50)] transition-colors text-left"
              onclick={(e) => { e.stopPropagation(); handleStartDelete(); }}
            >
              <Icon name="trash-2" size="xs" />
              Delete
            </Button>
          {/if}
        </div>
      {/if}
    {/if}

    <div class="w-16 flex-shrink-0 text-right">
      <div class="text-xs text-[var(--color-text-secondary)] font-medium">
        {message.sender?.name ?? 'Unknown'}
      </div>
      <div class="text-[0.6875rem] text-[var(--color-text-muted)]">{formattedTime}</div>
    </div>
    <div class="flex-1 min-w-0">
      <div class="flex items-center gap-2 mb-1 text-xs">
        <Icon name={style.icon} size="xs" class={style.textColor} />
        <span
          class="px-1.5 py-0.5 rounded border {style.borderColor} {style.textColor}"
        >
          {message.type}
        </span>
        {#if isAiMessage}
          <span class="px-1.5 py-0.5 rounded border border-dotted border-[var(--color-accent-primary)] text-[0.625rem] uppercase text-[var(--color-accent-primary)]">
            AI
          </span>
        {/if}
        {#if message.isEdited}
          <span class="text-[var(--color-text-muted)] italic">(edited)</span>
        {/if}
      </div>
      <div class="pl-3 border-l-2 {logBorderClass}">
        <!-- Reply quote -->
        {#if replyToMessage}
          <div class="reply-quote mb-1">
            <Icon name="corner-up-left" size="xs" class="reply-quote__icon" />
            <span class="reply-quote__author">{replyToMessage.sender?.name ?? 'Unknown'}</span>
            <span class="reply-quote__text">{replyToMessage.content.length > 80 ? replyToMessage.content.slice(0, 80) + '…' : replyToMessage.content}</span>
          </div>
        {/if}
        <!-- Delete confirmation -->
        {#if showDeleteConfirm}
          <div class="flex items-center gap-2 py-1 text-sm">
            <span class="text-[var(--color-error-600)]">Delete this message?</span>
            <Button variant="ghost" size="sm" onclick={handleConfirmDelete} class="text-[var(--color-error-600)]">Yes</Button>
            <Button variant="ghost" size="sm" onclick={handleCancelDelete}>No</Button>
          </div>
        {:else if isEditing}
          <!-- Edit mode -->
          <div class="relative flex flex-col rounded-xl border border-[var(--color-accent-primary)] shadow-sm bg-[var(--color-bg-primary)] overflow-hidden">
            {#if showToolbar}
              <div class="edit-toolbar flex items-center gap-0.5 px-2 pt-1.5 pb-0.5 border-b border-[var(--color-border-secondary)]">
                <Button variant="unstyled" class="edit-toolbar-btn" title="Heading" onmousedown={(e) => editToolbarAction(e, () => editApplyLinePrefix('## '))}><span class="edit-toolbar-text font-bold">H</span></Button>
                <Button variant="unstyled" class="edit-toolbar-btn" title="Bold" onmousedown={(e) => editToolbarAction(e, () => editApplyWrap('**', '**'))}><span class="edit-toolbar-text font-bold">B</span></Button>
                <Button variant="unstyled" class="edit-toolbar-btn" title="Italic" onmousedown={(e) => editToolbarAction(e, () => editApplyWrap('*', '*'))}><span class="edit-toolbar-text italic">I</span></Button>
                <Button variant="unstyled" class="edit-toolbar-btn" title="Strikethrough" onmousedown={(e) => editToolbarAction(e, () => editApplyWrap('~~', '~~'))}><span class="edit-toolbar-text line-through">S</span></Button>
                <span class="edit-toolbar-sep"></span>
                <Button variant="unstyled" class="edit-toolbar-btn" title="Inline code" onmousedown={(e) => editToolbarAction(e, () => editApplyWrap('`', '`'))}><Icon name="code" size="xs" /></Button>
                <Button variant="unstyled" class="edit-toolbar-btn" title="Code block" onmousedown={(e) => editToolbarAction(e, () => editApplyWrap('```\n', '\n```'))}><span class="edit-toolbar-text font-mono text-[0.5625rem]">{'{}'}</span></Button>
                <Button variant="unstyled" class="edit-toolbar-btn" title="Blockquote" onmousedown={(e) => editToolbarAction(e, () => editApplyLinePrefix('> '))}><span class="edit-toolbar-text text-[0.8125rem]">"</span></Button>
                <span class="edit-toolbar-sep"></span>
                <Button variant="unstyled" class="edit-toolbar-btn" title="Bullet list" onmousedown={(e) => editToolbarAction(e, () => editApplyLinePrefix('- '))}><Icon name="list" size="xs" /></Button>
                <Button variant="unstyled" class="edit-toolbar-btn" title="Numbered list" onmousedown={(e) => editToolbarAction(e, () => editApplyLinePrefix('1. '))}><span class="edit-toolbar-text font-mono text-[0.5625rem]">1.</span></Button>
                <Button variant="unstyled" class="edit-toolbar-btn" title="Link" onmousedown={editToolbarLink}><Icon name="link" size="xs" /></Button>
              </div>
            {/if}
            <Textarea
              unstyled
              bind:element={editTextareaEl}
              class="w-full px-3.5 pt-3 pb-2 text-sm
                text-[var(--color-text-primary)] bg-transparent resize-none
                focus:outline-none {showToolbar ? 'min-h-[9.75rem] max-h-[20rem]' : 'min-h-[3.25rem] max-h-[10rem]'}"
              rows={showToolbar ? 6 : 2}
              bind:value={editContent}
              onkeydown={handleEditKeydown}
            />
            <div class="flex items-center gap-2 px-2 pb-2 pt-0">
              <span class="text-[0.625rem] text-[var(--color-text-muted)] select-none">Enter · Shift+Enter · Esc</span>
              <div class="flex-1"></div>
              <Button
                variant="unstyled"
                onclick={handleCancelEdit}
                class="px-2 h-6 text-[0.6875rem] font-medium rounded-md text-[var(--color-text-muted)] hover:text-[var(--color-text-secondary)] hover:bg-[var(--color-surface-100)] transition-colors"
              >Cancel</Button>
              <Button
                variant="unstyled"
                onclick={handleSaveEdit}
                class="px-2 h-6 text-[0.6875rem] font-medium rounded-md bg-[var(--color-accent-primary)] text-white hover:opacity-90 transition-colors"
              >Save</Button>
            </div>
          </div>
        {:else}
        {#if message.replyToId}
          <div
            class="text-xs text-[var(--color-text-muted)] mb-2 pb-2 border-b border-[var(--color-border-secondary)] italic"
          >
            Replying to a message...
          </div>
        {/if}
        {#if message.type === 'AiReminder'}
          <SurfaceCard padding="md" class="space-y-2">
            <div class="flex items-center gap-2 text-xs text-[var(--color-text-secondary)]">
              <Icon name="bell" size="xs" />
              <span>Past Decision Reminder</span>
            </div>
            <div class="text-sm text-[var(--color-text-primary)] break-words prose-message">
              <!-- eslint-disable-next-line svelte/no-at-html-tags -->
              {@html renderMarkdown(message.content)}
            </div>
            {#if reminderReference}
              <Button
                variant="ghost"
                size="sm"
                onclick={handleReminderDetail}
                class="text-[var(--color-accent-primary)] hover:text-[var(--color-accent-primary)]"
              >
                <Icon name="external-link" size="xs" />
                View Details
              </Button>
            {/if}
          </SurfaceCard>
        {:else}
          <div class="text-sm text-[var(--color-text-primary)] break-words prose-message">
            <!-- eslint-disable-next-line svelte/no-at-html-tags -->
            {@html renderMarkdown(message.content)}
          </div>
        {/if}
        {#if referenceItems.length > 0}
          <div class="mt-2 pt-2 border-t border-[var(--color-border-secondary)]">
            <span class="text-xs text-[var(--color-text-muted)]">References:</span>
            <div class="flex flex-wrap gap-1 mt-1">
              {#each referenceItems as ref (ref.raw)}
                {#if ref.type === 'url' && ref.href}
                  <a
                    href={ref.href}
                    target="_blank"
                    rel="noreferrer"
                    class="text-xs inline-flex items-center gap-1 px-2 py-0.5 rounded bg-[var(--color-bg-tertiary)] text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)] hover:bg-[var(--color-bg-secondary)] transition-colors"
                  >
                    <Icon name={getReferenceIcon(ref.type)} size="xs" />
                    {ref.label}
                  </a>
                {:else if ref.type !== 'text'}
                  <Button
                    variant="unstyled"
                    onclick={() => handleReferenceNavigate(ref)}
                    class="text-xs inline-flex items-center gap-1 px-2 py-0.5 rounded bg-[var(--color-bg-tertiary)] text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)] hover:bg-[var(--color-bg-secondary)] transition-colors"
                  >
                    <Icon name={getReferenceIcon(ref.type)} size="xs" />
                    {ref.label}
                  </Button>
                {:else}
                  <span
                    class="text-xs inline-flex items-center gap-1 px-2 py-0.5 rounded bg-[var(--color-bg-tertiary)] text-[var(--color-text-secondary)]"
                  >
                    <Icon name={getReferenceIcon(ref.type)} size="xs" />
                    {ref.label}
                  </span>
                {/if}
              {/each}
            </div>
          </div>
        {/if}
        {#if canExtractRequirement || isDecision}
          <div class="mt-2 flex items-center gap-2">
            {#if canExtractRequirement}
              <Button
                variant="ghost"
                size="sm"
                onclick={handleExtractRequirement}
                class="text-[var(--color-accent-primary)] hover:text-[var(--color-accent-primary)]"
              >
                <Icon name="clipboard-list" size="xs" />
                Extract Requirement
              </Button>
            {/if}
            {#if isDecision}
              <DecisionImpactBadge
                messageId={message.id}
                {projectId}
                referenceCount={referenceItems.length}
              />
            {/if}
          </div>
        {/if}
        {/if}
      </div>
    </div>
  </div>
{:else if viewMode === 'document'}
  <!-- svelte-ignore a11y_click_events_have_key_events -->
  <!-- svelte-ignore a11y_no_static_element_interactions -->
  <div
    class="group relative border {documentBorderClass} bg-[var(--color-bg-secondary)] rounded-lg p-3 {className}"
    data-message-id={message.id}
    onclick={handleCloseMenu}
  >
    <!-- Actions menu button (hover) -->
    {#if hasActions && !isEditing && !showDeleteConfirm}
      <Button
        variant="unstyled"
        class="absolute top-2 right-2 opacity-0 group-hover:opacity-100
          w-6 h-6 flex items-center justify-center rounded
          bg-[var(--color-bg-secondary)] border border-[var(--color-border)]
          text-[var(--color-text-tertiary)] hover:text-[var(--color-text-primary)]
          shadow-sm transition-all z-10"
        title="Message actions"
        onclick={handleMenuToggle}
      >
        <Icon name="more-horizontal" size="xs" />
      </Button>

      {#if showMenu}
        <div class="absolute top-2 right-10 z-20
          bg-[var(--color-bg-secondary)] border border-[var(--color-border)]
          rounded-md shadow-lg py-1 min-w-[120px]">
          {#if canReply}
            <Button
              variant="unstyled"
              class="w-full flex items-center gap-2 px-3 py-1.5 text-sm text-[var(--color-text-primary)]
                hover:bg-[var(--color-bg-tertiary)] transition-colors text-left"
              onclick={(e) => { e.stopPropagation(); handleReply(); }}
            >
              <Icon name="corner-up-left" size="xs" />
              Reply
            </Button>
          {/if}
          {#if canEdit}
            <Button
              variant="unstyled"
              class="w-full flex items-center gap-2 px-3 py-1.5 text-sm text-[var(--color-text-primary)]
                hover:bg-[var(--color-bg-tertiary)] transition-colors text-left"
              onclick={(e) => { e.stopPropagation(); handleStartEdit(); }}
            >
              <Icon name="pencil" size="xs" />
              Edit
            </Button>
          {/if}
          {#if canDelete}
            <Button
              variant="unstyled"
              class="w-full flex items-center gap-2 px-3 py-1.5 text-sm text-[var(--color-error-600)]
                hover:bg-[var(--color-error-50)] transition-colors text-left"
              onclick={(e) => { e.stopPropagation(); handleStartDelete(); }}
            >
              <Icon name="trash-2" size="xs" />
              Delete
            </Button>
          {/if}
        </div>
      {/if}
    {/if}

    <div class="flex items-center gap-2 text-xs mb-2">
      <Icon name={style.icon} size="xs" class={style.textColor} />
      <span class="text-sm font-medium text-[var(--color-text-primary)]">
        {message.sender?.name ?? 'Unknown'}
      </span>
      <span class="text-[var(--color-text-muted)]">{formattedTime}</span>
      <span class="px-1.5 py-0.5 rounded border {style.borderColor} {style.textColor}">
        {message.type}
      </span>
      {#if isAiMessage}
        <span class="px-1.5 py-0.5 rounded border border-dotted border-[var(--color-accent-primary)] text-[0.625rem] uppercase text-[var(--color-accent-primary)]">
          AI
        </span>
      {/if}
      {#if message.isEdited}
        <span class="text-[var(--color-text-muted)] italic">(edited)</span>
      {/if}
    </div>

    <!-- Reply quote -->
    {#if replyToMessage}
      <div class="reply-quote mb-2">
        <Icon name="corner-up-left" size="xs" class="reply-quote__icon" />
        <span class="reply-quote__author">{replyToMessage.sender?.name ?? 'Unknown'}</span>
        <span class="reply-quote__text">{replyToMessage.content.length > 80 ? replyToMessage.content.slice(0, 80) + '…' : replyToMessage.content}</span>
      </div>
    {/if}

    <!-- Delete confirmation -->
    {#if showDeleteConfirm}
      <div class="flex items-center gap-2 py-1 text-sm">
        <span class="text-[var(--color-error-600)]">Delete this message?</span>
        <Button variant="ghost" size="sm" onclick={handleConfirmDelete} class="text-[var(--color-error-600)]">Yes</Button>
        <Button variant="ghost" size="sm" onclick={handleCancelDelete}>No</Button>
      </div>
    {:else if isEditing}
      <!-- Edit mode -->
      <div class="relative flex flex-col rounded-xl border border-[var(--color-accent-primary)] shadow-sm bg-[var(--color-bg-primary)] overflow-hidden">
        {#if showToolbar}
          <div class="edit-toolbar flex items-center gap-0.5 px-2 pt-1.5 pb-0.5 border-b border-[var(--color-border-secondary)]">
            <Button variant="unstyled" class="edit-toolbar-btn" title="Heading" onmousedown={(e) => editToolbarAction(e, () => editApplyLinePrefix('## '))}><span class="edit-toolbar-text font-bold">H</span></Button>
            <Button variant="unstyled" class="edit-toolbar-btn" title="Bold" onmousedown={(e) => editToolbarAction(e, () => editApplyWrap('**', '**'))}><span class="edit-toolbar-text font-bold">B</span></Button>
            <Button variant="unstyled" class="edit-toolbar-btn" title="Italic" onmousedown={(e) => editToolbarAction(e, () => editApplyWrap('*', '*'))}><span class="edit-toolbar-text italic">I</span></Button>
            <Button variant="unstyled" class="edit-toolbar-btn" title="Strikethrough" onmousedown={(e) => editToolbarAction(e, () => editApplyWrap('~~', '~~'))}><span class="edit-toolbar-text line-through">S</span></Button>
            <span class="edit-toolbar-sep"></span>
            <Button variant="unstyled" class="edit-toolbar-btn" title="Inline code" onmousedown={(e) => editToolbarAction(e, () => editApplyWrap('`', '`'))}><Icon name="code" size="xs" /></Button>
            <Button variant="unstyled" class="edit-toolbar-btn" title="Code block" onmousedown={(e) => editToolbarAction(e, () => editApplyWrap('```\n', '\n```'))}><span class="edit-toolbar-text font-mono text-[0.5625rem]">{'{}'}</span></Button>
            <Button variant="unstyled" class="edit-toolbar-btn" title="Blockquote" onmousedown={(e) => editToolbarAction(e, () => editApplyLinePrefix('> '))}><span class="edit-toolbar-text text-[0.8125rem]">"</span></Button>
            <span class="edit-toolbar-sep"></span>
            <Button variant="unstyled" class="edit-toolbar-btn" title="Bullet list" onmousedown={(e) => editToolbarAction(e, () => editApplyLinePrefix('- '))}><Icon name="list" size="xs" /></Button>
            <Button variant="unstyled" class="edit-toolbar-btn" title="Numbered list" onmousedown={(e) => editToolbarAction(e, () => editApplyLinePrefix('1. '))}><span class="edit-toolbar-text font-mono text-[0.5625rem]">1.</span></Button>
            <Button variant="unstyled" class="edit-toolbar-btn" title="Link" onmousedown={editToolbarLink}><Icon name="link" size="xs" /></Button>
          </div>
        {/if}
        <Textarea
          unstyled
          bind:element={editTextareaEl}
          class="w-full px-3.5 pt-3 pb-2 text-sm
            text-[var(--color-text-primary)] bg-transparent resize-none
            focus:outline-none {showToolbar ? 'min-h-[9.75rem] max-h-[20rem]' : 'min-h-[3.25rem] max-h-[10rem]'}"
          rows={showToolbar ? 6 : 2}
          bind:value={editContent}
          onkeydown={handleEditKeydown}
        />
        <div class="flex items-center gap-2 px-2 pb-2 pt-0">
          <span class="text-[0.625rem] text-[var(--color-text-muted)] select-none">Enter · Shift+Enter · Esc</span>
          <div class="flex-1"></div>
          <Button
            variant="unstyled"
            type="button"
            onclick={handleCancelEdit}
            class="px-2 h-6 text-[0.6875rem] font-medium rounded-md text-[var(--color-text-muted)] hover:text-[var(--color-text-secondary)] hover:bg-[var(--color-surface-100)] transition-colors"
          >Cancel</Button>
          <Button
            variant="unstyled"
            type="button"
            onclick={handleSaveEdit}
            class="px-2 h-6 text-[0.6875rem] font-medium rounded-md bg-[var(--color-accent-primary)] text-white hover:opacity-90 transition-colors"
          >Save</Button>
        </div>
      </div>
    {:else}
    {#if message.replyToId}
      <div
        class="text-xs text-[var(--color-text-muted)] mb-2 pb-2 border-b border-[var(--color-border-secondary)] italic"
      >
        Replying to a message...
      </div>
    {/if}
    {#if message.type === 'AiReminder'}
      <SurfaceCard padding="md" class="space-y-2">
        <div class="flex items-center gap-2 text-xs text-[var(--color-text-secondary)]">
          <Icon name="bell" size="xs" />
          <span>Past Decision Reminder</span>
        </div>
        <div class="text-sm text-[var(--color-text-primary)] break-words prose-message">
          <!-- eslint-disable-next-line svelte/no-at-html-tags -->
          {@html renderMarkdown(message.content)}
        </div>
        {#if reminderReference}
          <Button
            variant="ghost"
            size="sm"
            onclick={handleReminderDetail}
            class="text-[var(--color-accent-primary)] hover:text-[var(--color-accent-primary)]"
          >
            <Icon name="external-link" size="xs" />
            View Details
          </Button>
        {/if}
      </SurfaceCard>
    {:else}
      <div class="text-sm text-[var(--color-text-primary)] break-words prose-message">
        <!-- eslint-disable-next-line svelte/no-at-html-tags -->
        {@html renderMarkdown(message.content)}
      </div>
    {/if}
    {#if referenceItems.length > 0}
      <div class="mt-2 pt-2 border-t border-[var(--color-border-secondary)]">
        <span class="text-xs text-[var(--color-text-muted)]">References:</span>
        <div class="flex flex-wrap gap-1 mt-1">
          {#each referenceItems as ref (ref.raw)}
            {#if ref.type === 'url' && ref.href}
              <a
                href={ref.href}
                target="_blank"
                rel="noreferrer"
                class="text-xs inline-flex items-center gap-1 px-2 py-0.5 rounded bg-[var(--color-bg-tertiary)] text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)] hover:bg-[var(--color-bg-secondary)] transition-colors"
              >
                <Icon name={getReferenceIcon(ref.type)} size="xs" />
                {ref.label}
              </a>
            {:else if ref.type !== 'text'}
              <Button
                variant="unstyled"
                type="button"
                onclick={() => handleReferenceNavigate(ref)}
                class="text-xs inline-flex items-center gap-1 px-2 py-0.5 rounded bg-[var(--color-bg-tertiary)] text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)] hover:bg-[var(--color-bg-secondary)] transition-colors"
              >
                <Icon name={getReferenceIcon(ref.type)} size="xs" />
                {ref.label}
              </Button>
            {:else}
              <span
                class="text-xs inline-flex items-center gap-1 px-2 py-0.5 rounded bg-[var(--color-bg-tertiary)] text-[var(--color-text-secondary)]"
              >
                <Icon name={getReferenceIcon(ref.type)} size="xs" />
                {ref.label}
              </span>
            {/if}
          {/each}
        </div>
      </div>
    {/if}
    {#if canExtractRequirement || isDecision}
      <div class="mt-2 flex items-center gap-2">
        {#if canExtractRequirement}
          <Button
            variant="ghost"
            size="sm"
            onclick={handleExtractRequirement}
            class="text-[var(--color-accent-primary)] hover:text-[var(--color-accent-primary)]"
          >
            <Icon name="clipboard-list" size="xs" />
            Extract Requirement
          </Button>
        {/if}
        {#if isDecision}
          <DecisionImpactBadge
            messageId={message.id}
            {projectId}
            referenceCount={referenceItems.length}
          />
        {/if}
      </div>
    {/if}
    {/if}
  </div>
{:else}
  <!-- svelte-ignore a11y_click_events_have_key_events -->
  <!-- svelte-ignore a11y_no_static_element_interactions -->
  <div
    class="group relative flex gap-3 {isOwn ? '' : 'flex-row-reverse'} {className}"
    data-message-id={message.id}
    onclick={handleCloseMenu}
  >
    <!-- Avatar with user-specific color -->
    {#if senderAvatar.type === 'color'}
      <div
        class="flex-shrink-0 w-9 h-9 rounded-full flex items-center justify-center text-xs font-semibold text-white shadow-sm"
        style="background-color: {senderAvatar.value}"
      >
        {avatarText}
      </div>
    {:else if senderAvatar.type === 'preset'}
      <div class="flex-shrink-0 w-9 h-9 rounded-full flex items-center justify-center text-lg shadow-sm bg-[var(--color-bg-tertiary)]">
        {getPresetAvatarEmoji(senderAvatar.value)}
      </div>
    {:else}
      <div
        class="flex-shrink-0 w-9 h-9 rounded-full flex items-center justify-center text-xs font-semibold text-white shadow-sm {avatarBgClass}"
        style={avatarBgStyle}
      >
        {avatarText}
      </div>
    {/if}

    <!-- Message content -->
    <div class="flex flex-col flex-1 min-w-[180px] {bubbleWidthClass} {isOwn ? 'items-start' : 'items-end'}">
      <!-- Header -->
      <div class="flex items-center gap-2 mb-1 {isOwn ? '' : 'flex-row-reverse'}">
        <span class="text-sm font-medium text-[var(--color-text-primary)]">
          {message.sender?.name ?? 'Unknown'}
        </span>
        <MessageTypeIcon type={message.type} size="sm" />
        {#if isAiMessage}
          <span class="px-1.5 py-0.5 rounded border border-dotted border-[var(--color-accent-primary)] text-[0.625rem] uppercase text-[var(--color-accent-primary)]">
            AI
          </span>
        {/if}
        <span class="text-xs text-[var(--color-text-muted)]">
          {formattedTime}
        </span>
        {#if message.isEdited}
          <span class="text-xs text-[var(--color-text-muted)] italic">(edited)</span>
        {/if}
      </div>

      <!-- Bubble -->
      <div
        class="relative rounded-lg px-4 py-2.5 border {style.bgColor} {slackBorderClass} {lastMessageClass} max-w-full"
        class:rounded-tl-none={isOwn}
        class:rounded-tr-none={!isOwn}
      >
        <!-- Actions menu button (hover) -->
        {#if hasActions && !isEditing && !showDeleteConfirm}
          <Button
            variant="unstyled"
            type="button"
            class="absolute -top-2 right-1 opacity-0 group-hover:opacity-100
              w-6 h-6 flex items-center justify-center rounded
              bg-[var(--color-bg-secondary)] border border-[var(--color-border)]
              text-[var(--color-text-tertiary)] hover:text-[var(--color-text-primary)]
              shadow-sm transition-all z-10"
            title="Message actions"
            onclick={handleMenuToggle}
          >
            <Icon name="more-horizontal" size="xs" />
          </Button>

          <!-- Dropdown menu -->
          {#if showMenu}
            <div class="absolute -top-2 right-8 z-20
              bg-[var(--color-bg-secondary)] border border-[var(--color-border)]
              rounded-md shadow-lg py-1 min-w-[120px]">
              {#if canReply}
                <Button
                  variant="unstyled"
                  type="button"
                  class="w-full flex items-center gap-2 px-3 py-1.5 text-sm text-[var(--color-text-primary)]
                    hover:bg-[var(--color-bg-tertiary)] transition-colors text-left"
                  onclick={(e) => { e.stopPropagation(); handleReply(); }}
                >
                  <Icon name="corner-up-left" size="xs" />
                  Reply
                </Button>
              {/if}
              {#if canEdit}
                <Button
                  variant="unstyled"
                  type="button"
                  class="w-full flex items-center gap-2 px-3 py-1.5 text-sm text-[var(--color-text-primary)]
                    hover:bg-[var(--color-bg-tertiary)] transition-colors text-left"
                  onclick={(e) => { e.stopPropagation(); handleStartEdit(); }}
                >
                  <Icon name="pencil" size="xs" />
                  Edit
                </Button>
              {/if}
              {#if canDelete}
                <Button
                  variant="unstyled"
                  type="button"
                  class="w-full flex items-center gap-2 px-3 py-1.5 text-sm text-[var(--color-error-600)]
                    hover:bg-[var(--color-error-50)] transition-colors text-left"
                  onclick={(e) => { e.stopPropagation(); handleStartDelete(); }}
                >
                  <Icon name="trash-2" size="xs" />
                  Delete
                </Button>
              {/if}
            </div>
          {/if}
        {/if}

        <!-- Reply quote -->
        {#if replyToMessage}
          <div class="reply-quote mb-1.5">
            <Icon name="corner-up-left" size="xs" class="reply-quote__icon" />
            <span class="reply-quote__author">{replyToMessage.sender?.name ?? 'Unknown'}</span>
            <span class="reply-quote__text">{replyToMessage.content.length > 80 ? replyToMessage.content.slice(0, 80) + '…' : replyToMessage.content}</span>
          </div>
        {/if}

        <!-- Delete confirmation -->
        {#if showDeleteConfirm}
          <div class="flex items-center gap-2 py-1 text-sm">
            <span class="text-[var(--color-error-600)]">Delete this message?</span>
            <Button variant="ghost" size="sm" onclick={handleConfirmDelete} class="text-[var(--color-error-600)]">Yes</Button>
            <Button variant="ghost" size="sm" onclick={handleCancelDelete}>No</Button>
          </div>
        {:else if isEditing}
          <!-- Edit mode -->
          <div class="relative flex flex-col rounded-xl border border-[var(--color-accent-primary)] shadow-sm bg-[var(--color-bg-primary)] overflow-hidden">
            {#if showToolbar}
              <div class="edit-toolbar flex items-center gap-0.5 px-2 pt-1.5 pb-0.5 border-b border-[var(--color-border-secondary)]">
                <Button variant="unstyled" class="edit-toolbar-btn" title="Heading" onmousedown={(e) => editToolbarAction(e, () => editApplyLinePrefix('## '))}><span class="edit-toolbar-text font-bold">H</span></Button>
                <Button variant="unstyled" class="edit-toolbar-btn" title="Bold" onmousedown={(e) => editToolbarAction(e, () => editApplyWrap('**', '**'))}><span class="edit-toolbar-text font-bold">B</span></Button>
                <Button variant="unstyled" class="edit-toolbar-btn" title="Italic" onmousedown={(e) => editToolbarAction(e, () => editApplyWrap('*', '*'))}><span class="edit-toolbar-text italic">I</span></Button>
                <Button variant="unstyled" class="edit-toolbar-btn" title="Strikethrough" onmousedown={(e) => editToolbarAction(e, () => editApplyWrap('~~', '~~'))}><span class="edit-toolbar-text line-through">S</span></Button>
                <span class="edit-toolbar-sep"></span>
                <Button variant="unstyled" class="edit-toolbar-btn" title="Inline code" onmousedown={(e) => editToolbarAction(e, () => editApplyWrap('`', '`'))}><Icon name="code" size="xs" /></Button>
                <Button variant="unstyled" class="edit-toolbar-btn" title="Code block" onmousedown={(e) => editToolbarAction(e, () => editApplyWrap('```\n', '\n```'))}><span class="edit-toolbar-text font-mono text-[0.5625rem]">{'{}'}</span></Button>
                <Button variant="unstyled" class="edit-toolbar-btn" title="Blockquote" onmousedown={(e) => editToolbarAction(e, () => editApplyLinePrefix('> '))}><span class="edit-toolbar-text text-[0.8125rem]">"</span></Button>
                <span class="edit-toolbar-sep"></span>
                <Button variant="unstyled" class="edit-toolbar-btn" title="Bullet list" onmousedown={(e) => editToolbarAction(e, () => editApplyLinePrefix('- '))}><Icon name="list" size="xs" /></Button>
                <Button variant="unstyled" class="edit-toolbar-btn" title="Numbered list" onmousedown={(e) => editToolbarAction(e, () => editApplyLinePrefix('1. '))}><span class="edit-toolbar-text font-mono text-[0.5625rem]">1.</span></Button>
                <Button variant="unstyled" class="edit-toolbar-btn" title="Link" onmousedown={editToolbarLink}><Icon name="link" size="xs" /></Button>
              </div>
            {/if}
            <Textarea
              unstyled
              bind:element={editTextareaEl}
              class="w-full px-3.5 pt-3 pb-2 text-sm
                text-[var(--color-text-primary)] bg-transparent resize-none
                focus:outline-none {showToolbar ? 'min-h-[9.75rem] max-h-[20rem]' : 'min-h-[3.25rem] max-h-[10rem]'}"
              rows={showToolbar ? 6 : 2}
              bind:value={editContent}
              onkeydown={handleEditKeydown}
            />
            <div class="flex items-center gap-2 px-2 pb-2 pt-0">
              <span class="text-[0.625rem] text-[var(--color-text-muted)] select-none">Enter · Shift+Enter · Esc</span>
              <div class="flex-1"></div>
              <Button
                variant="unstyled"
                onclick={handleCancelEdit}
                class="px-2 h-6 text-[0.6875rem] font-medium rounded-md text-[var(--color-text-muted)] hover:text-[var(--color-text-secondary)] hover:bg-[var(--color-surface-100)] transition-colors"
              >Cancel</Button>
              <Button
                variant="unstyled"
                onclick={handleSaveEdit}
                class="px-2 h-6 text-[0.6875rem] font-medium rounded-md bg-[var(--color-accent-primary)] text-white hover:opacity-90 transition-colors"
              >Save</Button>
            </div>
          </div>
        {:else}
        <!-- Reply reference -->
        {#if message.replyToId}
          <div
            class="text-xs text-[var(--color-text-muted)] mb-2 pb-2 border-b border-[var(--color-border-secondary)] italic"
          >
            Replying to a message...
          </div>
        {/if}

        <!-- Content -->
        {#if message.type === 'AiReminder'}
          <SurfaceCard padding="md" class="space-y-2">
            <div class="flex items-center gap-2 text-xs text-[var(--color-text-secondary)]">
              <Icon name="bell" size="xs" />
              <span>Past Decision Reminder</span>
            </div>
            <div class="text-sm text-[var(--color-text-primary)] break-words prose-message">
              <!-- eslint-disable-next-line svelte/no-at-html-tags -->
              {@html renderMarkdown(message.content)}
            </div>
            {#if reminderReference}
              <Button
                variant="ghost"
                size="sm"
                onclick={handleReminderDetail}
                class="text-[var(--color-accent-primary)] hover:text-[var(--color-accent-primary)]"
              >
                <Icon name="external-link" size="xs" />
                View Details
              </Button>
            {/if}
          </SurfaceCard>
        {:else}
          <div class="text-sm text-[var(--color-text-primary)] break-words prose-message">
            <!-- eslint-disable-next-line svelte/no-at-html-tags -->
            {@html renderMarkdown(message.content)}
          </div>
        {/if}

        <!-- References -->
    {#if referenceItems.length > 0}
      <div class="mt-2 pt-2 border-t border-[var(--color-border-secondary)]">
        <span class="text-xs text-[var(--color-text-muted)]">References:</span>
            <div class="flex flex-wrap gap-1 mt-1">
              {#each referenceItems as ref (ref.raw)}
                {#if ref.type === 'url' && ref.href}
                  <a
                    href={ref.href}
                    target="_blank"
                    rel="noreferrer"
                    class="text-xs inline-flex items-center gap-1 px-2 py-0.5 rounded bg-[var(--color-surface-200)] text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)] hover:bg-[var(--color-surface-300)] transition-colors"
                  >
                    <Icon name={getReferenceIcon(ref.type)} size="xs" />
                    {ref.label}
                  </a>
                {:else if ref.type !== 'text'}
                  <Button
                    variant="unstyled"
                    type="button"
                    onclick={() => handleReferenceNavigate(ref)}
                    class="text-xs inline-flex items-center gap-1 px-2 py-0.5 rounded bg-[var(--color-surface-200)] text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)] hover:bg-[var(--color-surface-300)] transition-colors"
                  >
                    <Icon name={getReferenceIcon(ref.type)} size="xs" />
                    {ref.label}
                  </Button>
                {:else}
                  <span
                    class="text-xs inline-flex items-center gap-1 px-2 py-0.5 rounded bg-[var(--color-surface-200)] text-[var(--color-text-secondary)]"
                  >
                    <Icon name={getReferenceIcon(ref.type)} size="xs" />
                    {ref.label}
                  </span>
                {/if}
              {/each}
        </div>
      </div>
    {/if}
    {#if canExtractRequirement || isDecision}
      <div class="mt-2 flex items-center gap-2">
        {#if canExtractRequirement}
          <Button
            variant="ghost"
            size="sm"
            onclick={handleExtractRequirement}
            class="text-[var(--color-accent-primary)] hover:text-[var(--color-accent-primary)]"
          >
            <Icon name="clipboard-list" size="xs" />
            Extract Requirement
          </Button>
        {/if}
        {#if isDecision}
          <DecisionImpactBadge
            messageId={message.id}
            {projectId}
            referenceCount={referenceItems.length}
          />
        {/if}
      </div>
    {/if}
        {/if}
  </div>
</div>
  </div>
{/if}

<style>
  /* Compact markdown styling for message bubbles */
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

  /* Edit toolbar styles — :global() needed because targets are inside Button child components */
  .edit-toolbar :global(.edit-toolbar-btn) {
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

  .edit-toolbar :global(.edit-toolbar-btn:hover) {
    background: var(--color-surface-200);
    color: var(--color-text-primary);
  }

  .edit-toolbar :global(.edit-toolbar-text) {
    font-size: 12px;
    line-height: 1;
    user-select: none;
  }

  .edit-toolbar-sep {
    width: 1px;
    height: 14px;
    margin: 0 4px;
    background: var(--color-border-secondary);
    flex-shrink: 0;
  }

  /* Reply quote block */
  .reply-quote {
    display: flex;
    align-items: center;
    gap: 6px;
    padding: 4px 8px;
    border-left: 2px solid var(--color-accent-primary);
    border-radius: 0 4px 4px 0;
    background: var(--color-surface-100);
    font-size: 0.75rem;
    line-height: 1.4;
    overflow: hidden;
  }

  .reply-quote :global(.reply-quote__icon) {
    flex-shrink: 0;
    color: var(--color-accent-primary);
  }

  .reply-quote__author {
    font-weight: 600;
    color: var(--color-text-secondary);
    white-space: nowrap;
    flex-shrink: 0;
  }

  .reply-quote__text {
    color: var(--color-text-muted);
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
  }
</style>
