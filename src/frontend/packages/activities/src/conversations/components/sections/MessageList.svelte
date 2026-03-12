<!-- Section: MessageList — Conversations (internal, used by MessageStream) -->
<script lang="ts">
  import { Spinner, Button } from '@sddp/ui';
  import { EmptyState, formatDateWithOptions } from '@sddp/shell';
  import MessageBubble from '../idioms/MessageBubble.svelte';
  import TypingIndicator from '../idioms/TypingIndicator.svelte';
  import type { Message, MessageGroup } from '../../types';
  import {
    getConversationViewMode,
    subscribeConversationView,
    type ConversationViewContext,
    type ConversationViewMode,
  } from '../../stores';

  interface Props {
    messages: Message[];
    currentUserId?: string;
    projectId?: string;
    typingUsers?: Map<string, string>;
    loading?: boolean;
    hasMore?: boolean;
    onLoadMore?: () => void;
    viewContext?: ConversationViewContext;
    class?: string;
  }

  let {
    messages,
    currentUserId,
    projectId = '',
    typingUsers = new Map(),
    loading = false,
    hasMore = false,
    onLoadMore,
    viewContext = 'chat',
    class: className = '',
  }: Props = $props();

  let containerRef: HTMLElement | null = $state(null);
  let shouldAutoScroll = $state(true);
  let viewMode = $state<ConversationViewMode>('slack');

  $effect(() => {
    viewMode = getConversationViewMode(viewContext);
    const unsubscribe = subscribeConversationView((state) => {
      viewMode =
        viewContext === 'chat'
          ? state.chatMode
          : state.topicMode;
    });
    return unsubscribe;
  });

  const listSpacing = $derived(
    viewMode === 'log' ? 'space-y-3' : viewMode === 'document' ? 'space-y-4' : 'space-y-6'
  );
  const groupSpacing = $derived(
    viewMode === 'log' ? 'space-y-2' : viewMode === 'document' ? 'space-y-3' : 'space-y-4'
  );

  // Group messages by date
  const messageGroups = $derived.by(() => {
    const groups: MessageGroup[] = [];
    let currentDate = '';

    for (const message of messages) {
      const messageDate = new Date(message.createdAt).toDateString();

      if (messageDate !== currentDate) {
        currentDate = messageDate;
        groups.push({
          date: messageDate,
          messages: [message],
        });
      } else {
        const lastGroup = groups[groups.length - 1];
        if (lastGroup) {
          lastGroup.messages.push(message);
        }
      }
    }

    return groups;
  });

  function formatDateHeader(dateStr: string): string {
    const date = new Date(dateStr);
    const now = Date.now();
    const todayStart = now - (now % 86400000);
    const yesterdayStart = todayStart - 86400000;
    const dateTime = date.getTime();

    if (dateTime >= todayStart) {
      return 'Today';
    } else if (dateTime >= yesterdayStart) {
      return 'Yesterday';
    } else {
      return formatDateWithOptions(date, {
        weekday: 'long',
        month: 'long',
        day: 'numeric',
      });
    }
  }

  function handleScroll(e: Event): void {
    const target = e.target as HTMLElement;
    const { scrollTop, scrollHeight, clientHeight } = target;

    // Load more when scrolled to top (for older messages)
    if (scrollTop === 0 && hasMore && !loading && onLoadMore) {
      onLoadMore();
    }

    // Check if should auto-scroll (user is near bottom)
    shouldAutoScroll = scrollHeight - scrollTop - clientHeight < 100;
  }

  // Auto-scroll to bottom when new messages arrive
  $effect(() => {
    if (messages.length > 0 && containerRef && shouldAutoScroll) {
      containerRef.scrollTop = containerRef.scrollHeight;
    }
  });
</script>

<div
  bind:this={containerRef}
  onscroll={handleScroll}
  class="flex-1 overflow-y-auto px-4 py-4 {listSpacing} {className}"
>
  {#if loading && messages.length === 0}
    <div class="flex-1 flex items-center justify-center">
      <Spinner size="lg" />
    </div>
  {:else if messages.length === 0}
    <EmptyState
      icon="message-circle"
      heading="No messages yet"
      subtext="Start the conversation by sending a message"
    />
  {:else}
    <!-- Load more indicator at top -->
    {#if hasMore}
      <div class="flex justify-center py-2">
        {#if loading}
          <Spinner size="lg" />
        {:else}
          <Button
            variant="unstyled"
            onclick={onLoadMore}
            class="text-sm text-[var(--color-accent-primary)] hover:underline"
          >
            Load older messages
          </Button>
        {/if}
      </div>
    {/if}

    <!-- Message groups by date -->
    {#each messageGroups as group (group.date)}
      <div class="{groupSpacing}">
        <!-- Date header -->
        <div class="flex items-center gap-4">
          <div class="flex-1 h-px bg-[var(--color-border-secondary)]"></div>
          <span class="text-xs text-[var(--color-text-muted)] font-medium">
            {formatDateHeader(group.date)}
          </span>
          <div class="flex-1 h-px bg-[var(--color-border-secondary)]"></div>
        </div>

        <!-- Messages -->
        {#each group.messages as message, msgIndex (message.id)}
          {@const isLastMessage = messageGroups.indexOf(group) === messageGroups.length - 1 && msgIndex === group.messages.length - 1}
          <MessageBubble
            {message}
            viewMode={viewMode}
            {projectId}
            isOwn={message.sender?.id === currentUserId}
            isLast={isLastMessage}
          />
        {/each}
      </div>
    {/each}

    <!-- Typing indicator -->
    <TypingIndicator {typingUsers} />
  {/if}
</div>
