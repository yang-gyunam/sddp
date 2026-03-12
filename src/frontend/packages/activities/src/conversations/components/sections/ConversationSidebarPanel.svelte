<!-- Section: ConversationSidebarPanel — Self-contained Conversations Sidebar -->
<script lang="ts">
  import { untrack } from 'svelte';
  import { CollapsiblePanel } from '@sddp/shell';
  import { Button } from '@sddp/ui';
  import { Avatar } from '@sddp/shell';
  import ConversationSidebar from './ConversationSidebar.svelte';
  import {
    subscribeSidebar,
    getSidebarState,
    setSelectedConversation,
    toggleConversationStarred,
  } from '../../stores/conversation-sidebar.store';
  import { loadGlobalConversationsSidebar } from '../../actions';
  import type {
    ConversationSummary,
    DirectMessageSummary,
    ConversationSidebarState,
  } from '../../types';
  import type { ConversationEntry } from '../../types';

  interface Props {
    tenantId: string;
    onConversationSelect: (conversation: ConversationEntry) => void;
    onDirectMessageSelect: (dm: { id: string; name: string; channelStatus?: 'Active' | 'Concluded' | null }) => void;
    onCreateChannel: () => void;
    onCreateForum: () => void;
    onCreateDM: () => void;
  }

  let {
    tenantId,
    onConversationSelect,
    onDirectMessageSelect,
    onCreateChannel,
    onCreateForum,
    onCreateDM,
  }: Props = $props();

  // Local state synced from sidebar store
  let channels = $state<ConversationSummary[]>([]);
  let privateChannels = $state<ConversationSummary[]>([]);
  let topics = $state<ConversationSummary[]>([]);
  let directMessages = $state<DirectMessageSummary[]>([]);
  let starredConversations = $state<ConversationSummary[]>([]);
  let selectedConversationId = $state<string | null>(null);
  let loading = $state(false);

  // DM section expanded state
  let dmExpanded = $state(true);

  // prevId guard for tenant change
  let prevTenantId = $state<string | null>(null);

  // Subscribe to sidebar store
  let unsubscribe: (() => void) | null = null;

  function syncFromStore(state: ConversationSidebarState) {
    channels = state.channels;
    privateChannels = state.privateChannels;
    topics = state.topics;
    directMessages = state.directMessages;
    starredConversations = state.starredConversations;
    selectedConversationId = state.selectedConversationId;
    loading = state.loading;
  }

  // Initialize from current store state + subscribe
  $effect(() => {
    syncFromStore(getSidebarState());
    unsubscribe = subscribeSidebar((state) => syncFromStore(state));
    return () => {
      unsubscribe?.();
      unsubscribe = null;
    };
  });

  // Load data when tenantId changes (prevId guard)
  $effect(() => {
    if (!tenantId) {
      prevTenantId = null;
      return;
    }
    if (tenantId === prevTenantId) return;
    prevTenantId = tenantId;
    untrack(() => loadGlobalConversationsSidebar(tenantId));
  });

  // Handle conversation select from ConversationSidebar
  function handleConversationSelect(conversationId: string) {
    setSelectedConversation(conversationId);
    // Find conversation from store data to build ConversationEntry
    const conv = [...channels, ...privateChannels].find(c => c.id === conversationId);
    if (conv) {
      onConversationSelect({
        id: conv.id,
        name: conv.name,
        type: 'Channel',
        isPrivate: conv.type === 'private',
        channelStatus: conv.channelStatus ?? null,
      });
      return;
    }
    const topic = topics.find(c => c.id === conversationId);
    if (topic) {
      onConversationSelect({
        id: topic.id,
        name: topic.name,
        type: 'Forum',
        isPrivate: false,
        channelStatus: topic.channelStatus ?? null,
      });
      return;
    }
    // Starred conversation
    const starred = starredConversations.find(c => c.id === conversationId);
    if (starred) {
      const isForum = starred.type === 'public' && topics.some(t => t.id === starred.id);
      onConversationSelect({
        id: starred.id,
        name: starred.name,
        type: isForum ? 'Forum' : 'Channel',
        isPrivate: starred.type === 'private',
        channelStatus: starred.channelStatus ?? null,
      });
    }
  }

  // Handle DM select
  function handleDMSelect(dm: DirectMessageSummary) {
    setSelectedConversation(dm.id);
    onDirectMessageSelect({
      id: dm.id,
      name: dm.participantName,
      channelStatus: dm.channelStatus ?? null,
    });
  }

  function handleStarToggle(conversationId: string) {
    toggleConversationStarred(conversationId);
  }
</script>

<ConversationSidebar
  {channels}
  {privateChannels}
  {topics}
  {starredConversations}
  {selectedConversationId}
  {loading}
  onConversationSelect={handleConversationSelect}
  onConversationStarToggle={handleStarToggle}
  onCreateChannel={onCreateChannel}
  onCreateForum={onCreateForum}
>
  <!-- Direct Messages Section (rendered inside ConversationSidebar's scrollable area) -->
  {#if !loading}
    <CollapsiblePanel
      title="DIRECT MESSAGES"
      expanded={dmExpanded}
      onToggle={(expanded) => (dmExpanded = expanded)}
      actions={[{ id: 'add-dm', icon: 'plus', label: 'New DM', onClick: onCreateDM }]}
    >
      <div class="bg-[var(--color-bg-primary)]">
        {#each directMessages as dm (dm.id)}
          <Button
            variant="unstyled"
            class="w-full flex items-center gap-2 px-2 py-1.5 cursor-pointer
              transition-colors duration-150
              {selectedConversationId === dm.id
                ? 'bg-[var(--color-accent-primary)]/10'
                : 'hover:bg-[var(--color-bg-tertiary)]'}"
            onclick={() => handleDMSelect(dm)}
          >
            <div class="relative flex-shrink-0">
              <Avatar name={dm.participantName} size="xs" />
              <span
                class="absolute -bottom-0.5 -right-0.5 w-2 h-2 rounded-full border border-[var(--color-bg-primary)] {dm.isOnline
                  ? 'bg-[var(--color-success-500)]'
                  : 'bg-[var(--color-neutral-400)]'}"
              ></span>
            </div>
            <span class="flex-1 text-left text-sm truncate text-[var(--color-text-primary)]">
              {dm.participantName}
            </span>
            {#if dm.unreadCount > 0}
              <span
                class="min-w-[18px] h-[18px] px-1 flex items-center justify-center text-xs font-medium
                  rounded-full bg-[var(--color-accent-primary)] text-white"
              >
                {dm.unreadCount > 99 ? '99+' : dm.unreadCount}
              </span>
            {/if}
          </Button>
        {/each}
        {#if directMessages.length === 0}
          <div class="px-2 py-2 text-center text-sm text-[var(--color-text-secondary)]">
            No direct messages
          </div>
        {/if}
      </div>
    </CollapsiblePanel>
  {/if}
</ConversationSidebar>
