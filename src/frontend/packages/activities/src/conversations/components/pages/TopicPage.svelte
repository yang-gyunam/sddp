<!-- Activity: Conversations > Nav: Topic (forum-topic-{id}) -->
<script lang="ts">
  import { untrack } from 'svelte';
  import { Icon, Button } from '@sddp/ui';
  import { PageShell, PageHeader, PageBody, getTabState, setTabState } from '@sddp/shell';
  import { MessageStream, ParticipantsPanel } from '../sections';
  import { getConversationService } from '../../services';
  import { getAuthState } from '@sddp/shell/auth';
  import type { Message, ChannelDetail, MessageType, ConversationSummary } from '../../types';

  interface Props {
    tenantId?: string;
    projectId?: string;
    topicId?: string;
    tabId?: string;
    class?: string;
  }

  let {
    tenantId = '',
    projectId = '',
    topicId = '',
    tabId = '',
    class: className = '',
  }: Props = $props();

  const service = getConversationService();

  const currentUserId = $derived(getAuthState().user?.id ?? null);

  let channel = $state<ChannelDetail | null>(null);
  let messages = $state<Message[]>([]);
  let typingUsers = $state<string[]>([]);
  let loading = $state(false);
  let sending = $state(false);
  let pageError = $state<string | null>(null);
  let nextCursor = $state<string | null>(null);
  let hasMore = $state(false);
  let showParticipants = $state(true);
  let lastTopicKey = $state<string | null>(null);

  // Tab State Persistence
  interface TopicTabState {
    showParticipants: boolean;
  }

  const tabStateKey = $derived(tabId || 'topic');
  let restoredKey = $state<string | null>(null);
  let isRestored = $state(false);

  $effect(() => {
    if (!tabStateKey || restoredKey === tabStateKey) return;
    const saved = getTabState<TopicTabState>(tabStateKey);
    if (saved) {
      showParticipants = saved.showParticipants ?? true;
    }
    restoredKey = tabStateKey;
    isRestored = true;
  });

  $effect(() => {
    if (!tabStateKey || !isRestored) return;
    setTabState<TopicTabState>(tabStateKey, { showParticipants });
  });

  const space = $derived.by<ConversationSummary | null>(() => {
    if (!channel) return null;
    return {
      id: channel.id,
      name: channel.topic,
      type: 'public',
      projectId,
      projectName: null,
      unreadCount: 0,
      hasUnreadMentions: false,
      lastMessageAt: channel.updatedAt ?? channel.createdAt,
      starred: false,
      muted: false,
    };
  });

  function ensureContext(): boolean {
    if (!tenantId || !projectId) {
      pageError = 'Missing tenant or project context';
      return false;
    }
    service.setContext(tenantId, projectId);
    return true;
  }

  function normalizeMessage(message: Message): Message {
    return {
      ...message,
      references: message.references ?? [],
      replyToId: message.replyToId ?? null,
    };
  }

  async function loadTopic(): Promise<void> {
    if (!topicId) {
      channel = null;
      messages = [];
      return;
    }
    if (!ensureContext()) {
      channel = null;
      messages = [];
      return;
    }

    loading = true;
    pageError = null;
    try {
      channel = await service.getChannelById(topicId);
      await loadMessages(true);
    } catch (err) {
      pageError = err instanceof Error ? err.message : 'Failed to load topic';
      channel = null;
      messages = [];
    } finally {
      loading = false;
    }
  }

  async function loadMessages(reset = false): Promise<void> {
    if (!topicId) return;
    if (!ensureContext()) return;

    try {
      const cursor = reset ? undefined : nextCursor ?? undefined;
      const page = await service.getMessages(topicId, { cursor, limit: 50 });
      const batch = page.messages.map(normalizeMessage).reverse();
      messages = reset ? batch : [...batch, ...messages];
      nextCursor = page.nextCursor;
      hasMore = page.hasMore;
    } catch (err) {
      pageError = err instanceof Error ? err.message : 'Failed to load messages';
    }
  }

  async function handleSendMessage(content: string, type: MessageType): Promise<void> {
    if (!topicId) return;
    if (!ensureContext()) return;

    sending = true;
    try {
      const created = await service.postMessage(topicId, { content, type });
      messages = [...messages, normalizeMessage(created)];
    } catch (err) {
      pageError = err instanceof Error ? err.message : 'Failed to send message';
    } finally {
      sending = false;
    }
  }

  function handleLoadMore(): void {
    if (loading || !hasMore) return;
    loadMessages(false);
  }

  function toggleParticipants(): void {
    showParticipants = !showParticipants;
  }

  $effect(() => {
    if (!topicId) {
      channel = null;
      messages = [];
      lastTopicKey = null;
      return;
    }
    const key = `${tenantId}:${projectId}:${topicId}`;
    if (key === lastTopicKey) return;
    lastTopicKey = key;
    untrack(() => loadTopic());
  });
</script>

<PageShell class="h-full {className}">
  <PageHeader title={channel?.topic ?? 'Topic'} {loading}>
    {#snippet actions()}
      <Button variant="ghost" size="sm" onclick={toggleParticipants} title="Toggle participants">
        <Icon name="user" size="sm" />
      </Button>
    {/snippet}
  </PageHeader>

  <PageBody class="p-0">
    {#if pageError}
      <div class="px-4 py-2 text-sm text-red-500 border-b border-red-300/40">
        {pageError}
      </div>
    {/if}

    {#if !topicId}
      <div class="flex items-center justify-center h-full text-[var(--color-text-tertiary)]">
        Select a topic to view messages
      </div>
    {:else}
      <div class="flex h-full">
        <MessageStream
          conversation={space}
          channel={channel}
          {messages}
          {typingUsers}
          {loading}
          {sending}
          onSendMessage={handleSendMessage}
          onLoadMore={handleLoadMore}
          class="flex-1 min-w-0"
        />

        {#if showParticipants}
          <ParticipantsPanel channel={channel} {currentUserId} class="w-64 flex-shrink-0" />
        {/if}
      </div>
    {/if}
  </PageBody>
</PageShell>
