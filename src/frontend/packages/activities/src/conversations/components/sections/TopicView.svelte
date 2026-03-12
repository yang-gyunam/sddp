<!-- Section: TopicView — Conversations > Topic -->
<script lang="ts">
  import { untrack } from 'svelte';
  import { Icon, IconButton, Spinner } from '@sddp/ui';
  import { PageBody, PageShell, toast, getAuthState } from '@sddp/shell';
  import DetailHeader from '../../../shared/components/idioms/DetailHeader.svelte';
  import DetailTitle from '../../../shared/components/idioms/DetailTitle.svelte';
  import MessageBubble from '../idioms/MessageBubble.svelte';
  import MessageInput from '../idioms/MessageInput.svelte';
  import type { Message, CreateMessageRequest } from '../../types';
  import type { Topic, TopicStatus } from '../../types/conversation-entry.types';
  import { getConversationService, type ForumMessageDto } from '../../services/ConversationService';
  import {
    getConversationViewMode,
    subscribeConversationView,
    type ConversationViewMode,
  } from '../../stores';

  interface Props {
    topic: Topic;
    tenantId?: string;
    projectId?: string;
    onBack?: () => void;
    onTopicClosed?: () => void;
    tabId?: string;
    class?: string;
  }

  let {
    topic,
    tenantId: propTenantId = '',
    projectId = '',
    onBack,
    onTopicClosed,
    tabId,
    class: className = '',
  }: Props = $props();

  // Get tenantId from props or auth state
  const authState = getAuthState();
  const tenantId = $derived(propTenantId || authState.user?.tenantId || '');
  const currentUserId = $derived(authState.user?.id || '');

  const draftKey = $derived(tabId ? `${tabId}:${topic.id}` : topic.id);

  let messages = $state<Message[]>([]);
  let isLoadingMessages = $state(false);
  let messagesContainer: HTMLDivElement;
  let viewMode = $state<ConversationViewMode>(getConversationViewMode('topic'));

  // Guard duplicate effect runs when the topic id is unchanged
  let prevTopicId = $state<string | null>(null);

  // Convert ForumMessageDto to Message
  function toMessage(dto: ForumMessageDto): Message {
    return {
      id: dto.id,
      conversationId: topic.forumId,
      topicId: dto.topicId,
      sender: dto.sender,
      type: dto.type,
      content: dto.content,
      references: dto.references?.length ? dto.references : null,
      replyToId: dto.replyToId ?? null,
      createdAt: dto.createdAt,
      isEdited: dto.isEdited,
    };
  }

  async function loadMessages() {
    if (!tenantId || !topic.forumId || !topic.id) return;

    isLoadingMessages = true;
    try {
      const service = getConversationService();
      service.setContext(tenantId, projectId);
      const result = await service.getTopicMessages(topic.forumId, topic.id);
      messages = result.messages.map(toMessage);

      // Scroll to bottom to show latest messages
      setTimeout(() => {
        if (messagesContainer) {
          messagesContainer.scrollTop = messagesContainer.scrollHeight;
        }
      }, 0);
    } catch (err) {
      console.error('Failed to load topic messages:', err);
      toast.error('Failed to load messages');
      messages = [];
    } finally {
      isLoadingMessages = false;
    }
  }

  // Load messages when topic changes
  $effect(() => {
    const topicId = topic.id;
    if (!topicId) { prevTopicId = null; return; }
    if (topicId === prevTopicId) return;
    prevTopicId = topicId;
    untrack(() => loadMessages());
  });

  $effect(() => {
    const unsubscribe = subscribeConversationView((state) => {
      viewMode = state.topicMode;
    });
    return unsubscribe;
  });

  function getStatusColor(status: TopicStatus): string {
    switch (status) {
      case 'Open':
        return 'bg-[var(--color-bg-secondary)] text-[var(--color-success-600)] border border-[var(--color-border-secondary)]';
      case 'Closed':
        return 'bg-[var(--color-bg-secondary)] text-[var(--color-text-secondary)] border border-[var(--color-border-secondary)]';
      default:
        return 'bg-[var(--color-bg-secondary)] text-[var(--color-text-secondary)] border border-[var(--color-border-secondary)]';
    }
  }

  let replyingTo = $state<Message | null>(null);

  function handleReplyMessage(message: Message) {
    replyingTo = message;
  }

  function handleCancelReply() {
    replyingTo = null;
  }

  async function handleSendMessage(request: CreateMessageRequest) {
    if (!tenantId || !topic.forumId) return;

    try {
      const service = getConversationService();
      service.setContext(tenantId, projectId);
      const dto = await service.postTopicMessage(topic.forumId, topic.id, request);
      const newMessage = toMessage(dto);
      messages = [...messages, newMessage];
      replyingTo = null;

      setTimeout(() => {
        if (messagesContainer) {
          messagesContainer.scrollTop = messagesContainer.scrollHeight;
        }
      }, 0);
    } catch (err) {
      console.error('Failed to send message:', err);
      toast.error('Failed to send message');
    }
  }

  async function handleEditMessage(message: Message, newContent: string) {
    if (!tenantId || !topic.forumId) return;

    try {
      const service = getConversationService();
      service.setContext(tenantId, projectId);
      await service.editConversationMessage(topic.forumId, message.id, newContent);
      messages = messages.map((m) =>
        m.id === message.id ? { ...m, content: newContent, isEdited: true } : m
      );
    } catch (err) {
      console.error('Failed to edit message:', err);
      toast.error('Failed to edit message');
    }
  }

  async function handleDeleteMessage(message: Message) {
    if (!tenantId || !topic.forumId) return;

    try {
      const service = getConversationService();
      service.setContext(tenantId, projectId);
      await service.deleteConversationMessage(topic.forumId, message.id);
      messages = messages.filter((m) => m.id !== message.id);
    } catch (err) {
      console.error('Failed to delete message:', err);
      toast.error('Failed to delete message');
    }
  }

  async function handleCloseTopic() {
    if (!tenantId || !projectId) {
      toast.error('Missing context for closing topic');
      return;
    }

    try {
      const conversationService = getConversationService();
      conversationService.setContext(tenantId, projectId);
      await conversationService.closeTopic(topic.forumId, topic.id);
      toast.success('Topic closed successfully');
      onTopicClosed?.();
    } catch (err) {
      console.error('Failed to close topic:', err);
      toast.error('Failed to close topic');
    }
  }
</script>

<PageShell class={className}>
  <DetailHeader>
    {#snippet leading()}
      {#if topic.isPinned}
        <Icon name="pin" size="sm" class="text-[var(--color-accent-primary)]" />
      {/if}
    {/snippet}
    <DetailTitle title={topic.title}>
      <span class="text-xs text-[var(--color-text-tertiary)] truncate">by {topic.author?.name ?? 'Unknown'} · {topic.messageCount} messages</span>
      <span class="px-2 py-1 rounded text-xs font-medium flex-shrink-0 {getStatusColor(topic.status)}">
        {topic.status}
      </span>
    </DetailTitle>
    {#snippet actions()}
      {#if topic.status === 'Open'}
        <IconButton icon="check-circle" size="sm" variant="success" onclick={handleCloseTopic} title="Close Topic" />
      {/if}
      {#if onBack}
        <IconButton icon="x" size="sm" variant="ghost" onclick={onBack} title="Close" />
      {/if}
    {/snippet}
  </DetailHeader>

  <PageBody padded={false} scrollable={false} class="flex flex-col min-h-0">
    <!-- Messages Area -->
    <div
      bind:this={messagesContainer}
      class="flex-1 overflow-y-auto p-3 space-y-3 bg-[var(--color-surface-50)]"
    >
      {#if isLoadingMessages}
        <div class="flex items-center justify-center h-full py-6">
          <Spinner size="lg" />
        </div>
      {:else if messages.length === 0}
        <div class="flex items-center justify-center h-full py-6 text-sm text-[var(--color-text-tertiary)]">
          No messages yet. Start the discussion!
        </div>
      {:else}
        {#each messages as message, index (message.id)}
          <MessageBubble
            {message}
            {messages}
            viewMode={viewMode}
            {projectId}
            showToolbar={true}
            isOwn={message.sender?.id === currentUserId}
            isLast={index === messages.length - 1}
            onEdit={handleEditMessage}
            onDelete={handleDeleteMessage}
            onReply={topic.status === 'Open' ? handleReplyMessage : undefined}
          />
        {/each}
      {/if}
    </div>

    <!-- Message Input (only if topic is open) -->
    {#if topic.status === 'Open'}
      <div class="flex-shrink-0 bg-[var(--color-surface-50)]">
        <MessageInput
          onSubmit={handleSendMessage}
          placeholder="Reply to this topic..."
          showToolbar={true}
          draftKey={draftKey}
          replyTo={replyingTo}
          onCancelReply={handleCancelReply}
        />
      </div>
    {:else}
      <div
        class="px-4 py-3 text-center text-sm text-[var(--color-text-muted)] bg-[var(--color-surface-200)] border-t border-[var(--color-border-primary)]"
      >
        <Icon name="lock" size="sm" class="inline mr-1" />
        This topic is {topic.status.toLowerCase()}. No new replies allowed.
      </div>
    {/if}
  </PageBody>
</PageShell>
