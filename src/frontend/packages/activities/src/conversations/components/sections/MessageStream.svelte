<!-- Section: MessageStream — Conversations (internal, used by ChannelView/TopicView) -->
<script lang="ts">
  import { Icon, Button, Badge, IconButton, Spinner, Textarea } from '@sddp/ui';
  import { formatTime } from '@sddp/shell';
  import DetailHeader from '../../../shared/components/idioms/DetailHeader.svelte';
  import DetailTitle from '../../../shared/components/idioms/DetailTitle.svelte';
  import type { ChannelDetail, Message, MessageType, ConversationSummary } from '../../types';

  interface Props {
    conversation: ConversationSummary | null;
    channel: ChannelDetail | null;
    messages: Message[];
    typingUsers?: string[];
    loading?: boolean;
    sending?: boolean;
    onSendMessage?: (content: string, type: MessageType) => void;
    onLoadMore?: () => void;
    onStarConversation?: () => void;
    onOpenSettings?: () => void;
    onClose?: () => void;
    class?: string;
  }

  let {
    conversation,
    channel,
    messages,
    typingUsers = [],
    loading = false,
    sending = false,
    onSendMessage,
    onLoadMore,
    onStarConversation,
    onOpenSettings,
    onClose,
    class: className = '',
  }: Props = $props();

  let messageInput = $state('');
  let selectedMessageType = $state<MessageType>('Proposal');
  let messageListEl: HTMLDivElement | undefined = $state();
  let lastConversationId = $state<string | null>(null);

  // Scroll to bottom on conversation change (initial load)
  $effect(() => {
    const convId = conversation?.id ?? null;
    if (convId && convId !== lastConversationId) {
      lastConversationId = convId;
      if (messages.length > 0 && messageListEl) {
        requestAnimationFrame(() => {
          messageListEl?.scrollTo({ top: messageListEl.scrollHeight });
        });
      }
    }
  });

  // Auto-scroll to bottom when new messages arrive (only if already near bottom)
  $effect(() => {
    if (messages.length > 0 && messageListEl) {
      const isNearBottom =
        messageListEl.scrollHeight - messageListEl.scrollTop - messageListEl.clientHeight < 50;
      if (isNearBottom) {
        requestAnimationFrame(() => {
          messageListEl?.scrollTo({ top: messageListEl.scrollHeight, behavior: 'smooth' });
        });
      }
    }
  });

  const hasMessages = $derived(messages.length > 0);
  const typingDisplay = $derived(
    typingUsers.length === 1
      ? `${typingUsers[0]} is typing...`
      : typingUsers.length === 2
        ? `${typingUsers[0]} and ${typingUsers[1]} are typing...`
        : typingUsers.length > 2
          ? `${typingUsers[0]} and ${typingUsers.length - 1} others are typing...`
          : ''
  );

  function handleSend() {
    if (!messageInput.trim() || sending) return;
    onSendMessage?.(messageInput.trim(), selectedMessageType);
    messageInput = '';
  }

  function handleKeyDown(e: KeyboardEvent) {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      handleSend();
    }
  }

  // Message type options
  const MESSAGE_TYPES: { value: MessageType; label: string; icon: string }[] = [
    { value: 'Proposal', label: 'Proposal', icon: 'lightbulb' },
    { value: 'Question', label: 'Question', icon: 'help-circle' },
    { value: 'Objection', label: 'Objection', icon: 'alert-triangle' },
    { value: 'Reference', label: 'Reference', icon: 'link' },
    { value: 'Decision', label: 'Decision', icon: 'check-circle' },
  ];
</script>

<div class="message-stream flex flex-col h-full bg-[var(--color-bg-primary)] {className}">
  {#if conversation}
    <!-- Conversation Header -->
    <DetailHeader>
      {#snippet leading()}
        <Icon
          name={conversation.type === 'private' ? 'lock' : 'hash'}
          size="md"
          class="text-[var(--color-text-tertiary)]"
        />
      {/snippet}
      <DetailTitle title={conversation.name}>
        {#if conversation.projectName}
          <span class="text-xs text-[var(--color-text-tertiary)] flex-shrink-0">
            ({conversation.projectName})
          </span>
        {/if}
        {#if channel}
          <Badge
            variant={channel.status === 'Active' ? 'success' : channel.status === 'Concluded' ? 'warning' : 'secondary'}
          >
            {channel.status}
          </Badge>
          <span class="text-xs text-[var(--color-text-tertiary)]">
            {channel.participantCount} members
          </span>
        {/if}
      </DetailTitle>
      {#snippet actions()}
        <IconButton
          icon="star"
          size="sm"
          variant="ghost"
          title="Star conversation"
          class={conversation.starred ? 'fill-current text-[var(--color-warning-500)]' : ''}
          onclick={onStarConversation}
        />
        <IconButton
          icon="settings"
          size="sm"
          variant="ghost"
          title="Conversation settings"
          onclick={onOpenSettings}
        />
        {#if onClose}
          <IconButton
            icon="x"
            size="sm"
            variant="ghost"
            title="Close"
            onclick={onClose}
          />
        {/if}
      {/snippet}
    </DetailHeader>

    <!-- Messages Area -->
    <div bind:this={messageListEl} class="flex-1 overflow-y-auto px-4 py-2">
      {#if loading}
        <div class="flex-1 flex items-center justify-center">
          <Spinner size="lg" />
        </div>
      {:else if !hasMessages}
        <div class="flex flex-col items-center justify-center h-full text-center">
          <Icon name="message-circle" size="xl" class="text-[var(--color-text-tertiary)] mb-3" />
          <h3 class="text-lg font-medium text-[var(--color-text-primary)] mb-1">
            No messages yet
          </h3>
          <p class="text-sm text-[var(--color-text-tertiary)]">
            Start the conversation by sending a message.
          </p>
        </div>
      {:else}
        <!-- Load More Button -->
        <Button
          variant="unstyled"
          class="w-full py-2 mb-4 text-sm text-[var(--color-accent-primary)]
            hover:bg-[var(--color-bg-secondary)] rounded transition-colors"
          onclick={onLoadMore}
        >
          Load earlier messages
        </Button>

        <!-- Message List -->
        <div class="space-y-4">
          {#each messages as message (message.id)}
            <div class="message-item flex gap-3">
              <!-- Avatar -->
              <div
                class="flex-shrink-0 w-8 h-8 rounded-full bg-[var(--color-bg-tertiary)]
                  flex items-center justify-center text-sm font-medium"
              >
                {(message.sender?.name ?? '?').charAt(0).toUpperCase()}
              </div>

              <!-- Content -->
              <div class="flex-1 min-w-0">
                <div class="flex items-baseline gap-2">
                  <span class="font-medium text-[var(--color-text-primary)]">
                    {message.sender?.name ?? 'Unknown'}
                  </span>
                  <span class="text-xs text-[var(--color-text-tertiary)]">
                    {formatTime(message.createdAt, { hour: '2-digit', minute: '2-digit' })}
                  </span>
                  {#if message.type !== 'Proposal'}
                    <Badge variant="secondary" size="sm">
                      {message.type}
                    </Badge>
                  {/if}
                </div>
                <p class="text-sm text-[var(--color-text-primary)] mt-0.5 whitespace-pre-wrap">
                  {message.content}
                </p>
              </div>
            </div>
          {/each}
        </div>
      {/if}
    </div>

    <!-- Typing Indicator -->
    {#if typingDisplay}
      <div class="px-4 py-1 text-xs text-[var(--color-text-tertiary)] italic">
        {typingDisplay}
      </div>
    {/if}

    <!-- Message Input -->
    <div class="flex-shrink-0 px-4 py-3 border-t border-[var(--color-border)]">
      <!-- Message Type Selector -->
      <div class="flex items-center gap-1 mb-2">
        {#each MESSAGE_TYPES as type (type.value)}
          <Button
            variant="unstyled"
            class="flex items-center gap-1 px-2 py-1 text-xs rounded transition-colors
              {selectedMessageType === type.value
              ? 'bg-[var(--color-accent-primary)]/10 text-[var(--color-accent-primary)]'
              : 'text-[var(--color-text-tertiary)] hover:bg-[var(--color-bg-secondary)]'}"
            onclick={() => (selectedMessageType = type.value)}
            title={type.label}
          >
            <Icon name={type.icon} size="xs" />
            <span class="hidden sm:inline">{type.label}</span>
          </Button>
        {/each}
      </div>

      <!-- Input Area -->
      <div class="flex items-end gap-2">
        <Textarea
          unstyled
          bind:value={messageInput}
          placeholder="Type a message..."
          rows={1}
          class="flex-1 px-3 py-2 text-sm rounded-lg resize-none
            bg-[var(--color-bg-secondary)] border border-[var(--color-border)]
            text-[var(--color-text-primary)] placeholder:text-[var(--color-text-tertiary)]
            focus:outline-none focus:border-[var(--color-accent-primary)]"
          onkeydown={handleKeyDown}
        />
        <Button
          variant="primary"
          size="sm"
          onclick={handleSend}
          disabled={!messageInput.trim() || sending}
          class="px-0 w-9 h-9 flex-shrink-0"
        >
          {#if sending}
            <Spinner size="lg" />
          {:else}
            <Icon name="send" size="sm" />
          {/if}
        </Button>
      </div>
    </div>
  {:else}
    <!-- No Conversation Selected -->
    <div class="flex flex-col items-center justify-center h-full text-center">
      <Icon name="message-square" size="xl" class="text-[var(--color-text-tertiary)] mb-3" />
      <h3 class="text-lg font-medium text-[var(--color-text-primary)] mb-1">
        Select a conversation
      </h3>
      <p class="text-sm text-[var(--color-text-tertiary)]">
        Choose a conversation from the sidebar to start chatting.
      </p>
    </div>
  {/if}
</div>
