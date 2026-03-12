<!-- Activity: Conversations > Nav: Global (ACT-CONV-001) -->
<script lang="ts">
  import { untrack } from 'svelte';
  import { SvelteMap } from 'svelte/reactivity';
  import { PageShell, PageHeader, PageBody, toast, getAuthState, subscribeAuth, SidebarDetailLayout, SIDEBAR_DETAIL_LAYOUT, getTabState, setTabState, Avatar } from '@sddp/shell';
  import { Icon, Input, IconButton, Spinner, CardGrid, Button } from '@sddp/ui';
  import { StatCard } from '../../../shared/components/idioms';
  import { getTenantMembers } from '../../../settings/services/UserManagementService';
  import type { TenantMember } from '../../../settings/services/UserManagementService';
  import type {
    ConversationSummary,
    DirectMessageSummary,
    ChannelDetail,
    ConversationMember,
  } from '../../types';
  import {
    getConversationService,
    type ConversationSummaryDto,
  } from '../../services/ConversationService';
  import type { DirectMessageInfo } from '../../types';
  import {
    setSelectedConversation,
    setFlatConversations,
    setDirectMessages as setSidebarDirectMessages,
    setStarredConversations as setSidebarStarredConversations,
    toggleSectionExpanded,
    toggleConversationStarred,
    setSearchQuery,
    subscribeSidebar,
    setActiveConversationContext,
    clearActiveConversationContext,
  } from '../../stores';
  import { getConversationStoreState, subscribeConversation } from '../../stores/conversation.store';
  import { getConversationHubService } from '../../services/ConversationHubService';
  import { ConversationCreatePanel, ConversationSidebar, ForumView, ParticipantsPanel } from '../sections';
  import ChannelView from '../sections/ChannelView.svelte';

  interface Props {
    tenantId?: string;
    projectId?: string;
    tabId?: string;
    initialCreateMode?: 'channel' | 'forum' | 'dm' | null;
    class?: string;
  }

  let { tenantId: propTenantId = '', tabId = '', initialCreateMode = null, class: className = '' }: Props = $props();

  // Auth state
  let authState = $state(getAuthState());
  const tenantId = $derived(propTenantId || authState.user?.tenantId || '');

  // Subscribe to auth state changes
  $effect(() => {
    const unsubscribe = subscribeAuth((state) => {
      authState = state;
    });
    return unsubscribe;
  });

  // State - Flat structure for global conversations
  let channels = $state<ConversationSummary[]>([]);
  let privateChannels = $state<ConversationSummary[]>([]);
  let topics = $state<ConversationSummary[]>([]);
  let directMessages = $state<DirectMessageSummary[]>([]);
  let starredConversations = $state<ConversationSummary[]>([]);
  let selectedConversation = $state<ConversationSummary | null>(null);
  let selectedConversationId = $state<string | null>(null);
  let searchQuery = $state('');
  let lastLoadedConversationId = $state<string | null>(null);

  // Create mode state
  type CreateMode = 'channel' | 'forum' | 'dm' | null;
  let createMode = $state<CreateMode>(null);
  let createName = $state('');
  let createDescription = $state('');
  let isCreating = $state(false);
  let createError = $state<string | null>(null);

  const isCreateFormValid = $derived(createName.trim().length > 0);

  // DM create state
  let dmUsers = $state<TenantMember[]>([]);
  let dmUsersLoading = $state(false);
  let dmSearchQuery = $state('');

  const currentUserId = $derived(authState.user?.id || '');

  const activeDmUsers = $derived(
    dmUsers.filter(u => u.id !== currentUserId)
  );

  const filteredDmUsers = $derived.by(() => {
    const q = dmSearchQuery.toLowerCase().trim();
    if (!q) return activeDmUsers;
    return activeDmUsers.filter(
      u => u.name.toLowerCase().includes(q) || u.email.toLowerCase().includes(q)
    );
  });

  let currentChannel = $state<ChannelDetail | null>(null);
  let loading = $state(false);
  let loadingMore = $state(false);
  let allConversationDtos = $state<ConversationSummaryDto[]>([]);
  let allDmChannels = $state<DirectMessageInfo[]>([]);
  let conversationPageNumber = $state(1);
  let dmPageNumber = $state(1);
  let hasMoreConversations = $state(false);
  let hasMoreDirectMessages = $state(false);
  let starredConversationIds = $state<Set<string>>(new Set());
  const CONVERSATION_PAGE_SIZE = 20;
  let latestSidebarRequestId = $state(0);

  // Online users from ConversationHub presence tracking
  let onlineUsers = $state<Set<string>>(getConversationStoreState().onlineUsers);

  $effect(() => {
    const unsubscribe = subscribeConversation((state) => {
      onlineUsers = state.onlineUsers;
    });
    return unsubscribe;
  });

  // Tab State Persistence
  interface ConversationsTabState {
    searchQuery: string;
    selectedConversationId: string | null;
  }

  const tabStateKey = $derived(tabId || 'global-conversations');
  const activeConversationTabId = $derived(tabId || 'global-conversations');
  let restoredKey = $state<string | null>(null);
  let isRestored = $state(false);

  $effect(() => {
    if (!tabStateKey || restoredKey === tabStateKey) return;
    const saved = getTabState<ConversationsTabState>(tabStateKey);
    if (saved) {
      // state settings (save effect)
      if (saved.searchQuery) searchQuery = saved.searchQuery;
      if (saved.selectedConversationId) selectedConversationId = saved.selectedConversationId;
      // Store (sidebar)
      if (saved.searchQuery) setSearchQuery(saved.searchQuery);
      if (saved.selectedConversationId) setSelectedConversation(saved.selectedConversationId);
    }
    restoredKey = tabStateKey;
    isRestored = true;
  });

  $effect(() => {
    if (!tabStateKey || !isRestored) return;
    setTabState<ConversationsTabState>(tabStateKey, {
      searchQuery,
      selectedConversationId,
    });
  });

  let sidebarLoadKey = $state<string | null>(null);
  $effect(() => {
    if (!tenantId) return;
    const key = `${tenantId}`;
    if (sidebarLoadKey === key) return;

    sidebarLoadKey = key;
    untrack(() => loadConversationSidebarData(true));
  });

  // Determine if selected conversation is a Forum
  const isForumSelected = $derived(
    selectedConversation != null && topics.some((t) => t.id === selectedConversation!.id)
  );
  const selectedConversationKind = $derived.by(() => {
    if (!selectedConversation) return null;
    if (selectedConversation.type === 'dm') return 'dm' as const;
    return isForumSelected ? 'forum' as const : 'channel' as const;
  });

  $effect(() => {
    if (!activeConversationTabId) return;

    setActiveConversationContext({
      tabId: activeConversationTabId,
      conversationId: selectedConversationId,
      projectId: selectedConversation?.projectId ?? null,
      kind: selectedConversationKind,
    });

    return () => {
      clearActiveConversationContext(activeConversationTabId);
    };
  });

  // Stats for overview
  const channelCount = $derived(channels.length + privateChannels.length);
  const forumCount = $derived(topics.length);
  const dmCount = $derived(directMessages.length);
  const totalUnreadCount = $derived(
    channels.reduce((sum, s) => sum + (s.unreadCount ?? 0), 0) +
    privateChannels.reduce((sum, s) => sum + (s.unreadCount ?? 0), 0) +
    topics.reduce((sum, s) => sum + (s.unreadCount ?? 0), 0) +
    directMessages.reduce((sum, dm) => sum + (dm.unreadCount ?? 0), 0)
  );

  function mergeById<T extends { id: string }>(existing: T[], incoming: T[]): T[] {
    const map = new SvelteMap<string, T>();
    for (const item of existing) {
      map.set(item.id, item);
    }
    for (const item of incoming) {
      map.set(item.id, item);
    }
    return Array.from(map.values());
  }

  function convertToUiConversation(
    dto: ConversationSummaryDto,
    isStarred: boolean
  ): ConversationSummary {
    const typeMap: Record<ConversationSummaryDto['conversationType'], 'public' | 'private' | 'dm'> = {
      Channel: dto.visibility === 'Private' ? 'private' : 'public',
      DirectMessage: 'dm',
      Forum: 'public',
    };

    return {
      id: dto.id,
      name: dto.name,
      type: typeMap[dto.conversationType] ?? 'public',
      projectId: dto.projectId ?? null,
      projectName: null,
      channelStatus: dto.channelStatus ?? null,
      unreadCount: dto.unreadCount,
      hasUnreadMentions: false,
      lastMessageAt: dto.lastMessageAt ?? null,
      starred: isStarred,
      muted: false,
    };
  }

  function mapDmChannel(channel: DirectMessageInfo): DirectMessageSummary {
    return {
      id: channel.id,
      participantId: channel.otherUser.id,
      participantName: channel.otherUser.name ?? 'Unknown',
      participantAvatar: channel.otherUser.avatarUrl ?? undefined,
      channelStatus: channel.status ?? null,
      isOnline: false,
      lastMessage: channel.lastMessage?.content,
      lastMessageAt: channel.lastMessage?.createdAt ?? channel.updatedAt,
      unreadCount: channel.unreadCount,
    };
  }

  function buildConversationLists(
    conversations: ConversationSummaryDto[],
    starredIds: Set<string>
  ): {
    channels: ConversationSummary[];
    privateChannels: ConversationSummary[];
    topics: ConversationSummary[];
    starredConversations: ConversationSummary[];
  } {
    const channels: ConversationSummary[] = [];
    const privateChannels: ConversationSummary[] = [];
    const topics: ConversationSummary[] = [];
    const starredConversations: ConversationSummary[] = [];

    for (const conversation of conversations) {
      if (conversation.conversationType !== 'Channel' && conversation.conversationType !== 'Forum') {
        continue;
      }

      const uiConversation = convertToUiConversation(conversation, starredIds.has(conversation.id));

      if (conversation.conversationType === 'Forum') {
        topics.push(uiConversation);
      } else if (conversation.visibility === 'Private') {
        privateChannels.push(uiConversation);
      } else {
        channels.push(uiConversation);
      }

      if (starredIds.has(conversation.id)) {
        starredConversations.push(uiConversation);
      }
    }

    return { channels, privateChannels, topics, starredConversations };
  }

  async function loadConversationSidebarData(reset: boolean = true): Promise<void> {
    if (!tenantId) return;
    if (!reset && (loading || loadingMore || (!hasMoreConversations && !hasMoreDirectMessages))) return;

    const requestId = ++latestSidebarRequestId;
    const nextConversationPage = reset
      ? 1
      : hasMoreConversations ? conversationPageNumber + 1 : conversationPageNumber;
    const nextDmPage = reset
      ? 1
      : hasMoreDirectMessages ? dmPageNumber + 1 : dmPageNumber;

    if (reset) {
      loading = true;
      conversationPageNumber = 1;
      dmPageNumber = 1;
      hasMoreConversations = false;
      hasMoreDirectMessages = false;
      allConversationDtos = [];
      allDmChannels = [];
      starredConversationIds = new Set();
    } else {
      loadingMore = true;
    }

    try {
      const service = getConversationService();
      service.setContext(tenantId, '');

      const conversationPageRequest = reset || hasMoreConversations
        ? service.getGlobalConversationsPage({
            pageNumber: nextConversationPage,
            pageSize: CONVERSATION_PAGE_SIZE,
          })
        : Promise.resolve(null);

      const dmPageRequest = reset || hasMoreDirectMessages
        ? service.getDMChannelsPage({
            pageNumber: nextDmPage,
            pageSize: CONVERSATION_PAGE_SIZE,
          })
        : Promise.resolve(null);

      const starredRequest = reset
        ? service.getStarredConversations().catch(() => [])
        : Promise.resolve(null);

      const [convResult, dmResult, starredResult] = await Promise.allSettled([
        conversationPageRequest,
        dmPageRequest,
        starredRequest,
      ]);
      if (requestId !== latestSidebarRequestId) return;
      const conversationPage = convResult.status === 'fulfilled' ? convResult.value : null;
      const dmPage = dmResult.status === 'fulfilled' ? dmResult.value : null;
      const starredDtos = starredResult.status === 'fulfilled' ? starredResult.value : null;

      allConversationDtos = conversationPage
        ? mergeById(reset ? [] : allConversationDtos, conversationPage.items)
        : (reset ? [] : allConversationDtos);
      allDmChannels = dmPage
        ? mergeById(reset ? [] : allDmChannels, dmPage.items)
        : (reset ? [] : allDmChannels);
      if (starredDtos) {
        starredConversationIds = new Set(starredDtos.map((conversation) => conversation.id));
      }

      const { channels, privateChannels, topics, starredConversations } = buildConversationLists(
        allConversationDtos,
        starredConversationIds
      );

      setFlatConversations({ channels, privateChannels, topics });
      setSidebarDirectMessages(allDmChannels.map(mapDmChannel));
      setSidebarStarredConversations(starredConversations);

      if (conversationPage) {
        conversationPageNumber = conversationPage.pageNumber ?? nextConversationPage;
        hasMoreConversations = conversationPage.hasNextPage
          ?? allConversationDtos.length < conversationPage.totalCount;
      } else if (reset) {
        conversationPageNumber = 1;
        hasMoreConversations = false;
      }

      if (dmPage) {
        dmPageNumber = dmPage.pageNumber ?? nextDmPage;
        hasMoreDirectMessages = dmPage.hasNextPage
          ?? allDmChannels.length < dmPage.totalCount;
      } else if (reset) {
        dmPageNumber = 1;
        hasMoreDirectMessages = false;
      }
    } catch (err) {
      if (requestId !== latestSidebarRequestId) return;
      console.error('Failed to load global conversations sidebar:', err);
      toast.error(err instanceof Error ? err.message : 'Failed to load conversations');
      hasMoreConversations = false;
      hasMoreDirectMessages = false;

      if (reset) {
        allConversationDtos = [];
        allDmChannels = [];
        setFlatConversations({ channels: [], privateChannels: [], topics: [] });
        setSidebarDirectMessages([]);
        setSidebarStarredConversations([]);
      }
    } finally {
      if (requestId === latestSidebarRequestId) {
        loading = false;
        loadingMore = false;
      }
    }
  }

  $effect(() => {
    const unsubscribe = subscribeSidebar((state) => {
      channels = state.channels;
      privateChannels = state.privateChannels;
      topics = state.topics;
      directMessages = state.directMessages;
      starredConversations = state.starredConversations;
      selectedConversationId = state.selectedConversationId;
      selectedConversation = [
        ...state.channels,
        ...state.privateChannels,
        ...state.topics,
      ].find((conversation) => conversation.id === state.selectedConversationId) ?? null;
      searchQuery = state.searchQuery;
    });

    // Trigger initial create mode if requested (e.g., from app sidebar DM "+" button)
    if (initialCreateMode === 'dm') {
      untrack(() => handleCreateDM());
    } else if (initialCreateMode === 'channel') {
      untrack(() => handleCreateChannel());
    } else if (initialCreateMode === 'forum') {
      untrack(() => handleCreateForum());
    }

    return () => {
      unsubscribe();
    };
  });

  // Load participants when conversation changes (for right-panel ParticipantsPanel)
  $effect(() => {
    if (!selectedConversationId || selectedConversationId === lastLoadedConversationId) return;
    lastLoadedConversationId = selectedConversationId;
    currentChannel = null;
    untrack(() => loadConversationParticipants(selectedConversationId!));
  });

  async function loadConversationParticipants(conversationId: string) {
    if (!tenantId) return;
    loading = true;

    const conversationService = getConversationService();
    conversationService.setContext(tenantId, selectedConversation?.projectId || '');

    try {
      const members = await conversationService.getConversationMembers(conversationId);
      currentChannel = {
        id: conversationId,
        topic: selectedConversation?.name ?? 'Channel',
        status: 'Active',
        participantCount: members.length,
        messageCount: 0,
        createdAt: new Date().toISOString(),
        concludedAt: null,
        participants: members,
      };
    } catch {
      currentChannel = null;
    } finally {
      loading = false;
    }
  }

  function handleConversationSelect(conversationId: string) {
    setSelectedConversation(conversationId);
  }

  function handleSectionToggle(section: string) {
    toggleSectionExpanded(section);
  }

  function handleConversationStarToggle(conversationId: string) {
    toggleConversationStarred(conversationId);
  }

  function handleSearchChange(query: string) {
    setSearchQuery(query);
  }

  function handleLoadMoreSidebar(): void {
    void loadConversationSidebarData(false);
  }


  // Create conversation handlers
  function handleCreateChannel() {
    resetCreateForm();
    createMode = 'channel';
    setSelectedConversation(null);
  }

  function handleCreateForum() {
    resetCreateForm();
    createMode = 'forum';
    setSelectedConversation(null);
  }

  async function handleCreateDM() {
    resetCreateForm();
    createMode = 'dm';
    setSelectedConversation(null);
    if (tenantId && dmUsers.length === 0) {
      dmUsersLoading = true;
      try {
        dmUsers = await getTenantMembers(tenantId);
      } catch (err) {
        createError = err instanceof Error ? err.message : 'Failed to load users.';
      } finally {
        dmUsersLoading = false;
      }
    }
  }

  async function handleDmSelectUser(targetUserId: string) {
    if (isCreating) return;
    isCreating = true;
    createError = null;
    try {
      const service = getConversationService();
      service.setContext(tenantId, '');
      const result = await service.sendDMInvitation(targetUserId);
      if (result.sent) {
        toast.success('Invitation sent');
      } else {
        toast.info(result.message);
      }
      createMode = null;
    } catch (err) {
      createError = err instanceof Error ? err.message : 'Failed to send invitation.';
    } finally {
      isCreating = false;
    }
  }

  function resetCreateForm() {
    createName = '';
    createDescription = '';
    createError = null;
    dmSearchQuery = '';
  }

  function cancelCreate() {
    createMode = null;
    createError = null;
  }

  async function submitCreate() {
    if (!createName.trim()) {
      createError = 'Name is required.';
      return;
    }
    isCreating = true;
    createError = null;
    try {
      const service = getConversationService();
      service.setContext(tenantId, '');
      const created = await service.createGlobalConversation({
        name: createName.trim(),
        conversationType: createMode === 'forum' ? 'Forum' : 'Channel',
        visibility: 'Public',
        description: createDescription.trim() || undefined,
      });
      toast.success(`${createMode === 'forum' ? 'Forum' : 'Channel'} created`);
      createMode = null;
      await loadConversationSidebarData(true);
      setSelectedConversation(created.id);
    } catch (err) {
      createError = err instanceof Error ? err.message : 'Failed to create.';
    } finally {
      isCreating = false;
    }
  }

  async function handleSendInvitation(targetUserId: string, _displayName: string): Promise<void> {
    if (!selectedConversationId || !selectedConversation) return;
    const hubService = getConversationHubService();
    await hubService.sendInvitation(selectedConversationId, targetUserId, selectedConversation.name);
  }

  // Listen for invitation responses dispatched by App.svelte to reload participants
  $effect(() => {
    function onInvitationResponse(e: Event) {
      const event = (e as CustomEvent).detail as { accepted: boolean; conversationId: string };
      if (event.accepted && event.conversationId === selectedConversationId) {
        loadConversationParticipants(event.conversationId);
      }
    }
    window.addEventListener('sddp:invitation-response', onInvitationResponse);
    return () => window.removeEventListener('sddp:invitation-response', onInvitationResponse);
  });

  async function handleRemoveParticipant(participant: ConversationMember): Promise<void> {
    if (!selectedConversationId) return;
    try {
      const service = getConversationService();
      service.setContext(tenantId, selectedConversation?.projectId || '');
      await service.removeConversationMember(selectedConversationId, participant.user.id);
      toast.success(`${participant.user.name} removed`);
      await loadConversationParticipants(selectedConversationId);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Failed to remove member');
    }
  }
</script>

<PageShell class="h-full {className}">
  <PageHeader title="Conversations" {loading}>
    {#snippet actions()}
      <IconButton
        icon="plus"
        size="sm"
        variant="ghost"
        title="Create conversation"
        onclick={handleCreateChannel}
      />
    {/snippet}
  </PageHeader>

  <PageBody class="p-0">
    {#if createMode === 'dm'}
      <!-- DM creation is shown full-width without left panel -->
      <div class="flex flex-col h-full bg-[var(--color-bg-primary)]">
        <div class="flex items-center justify-between min-h-12 px-4 border-b border-[var(--color-border-primary)]">
          <div class="flex items-center">
            <Icon name="message-square" size="md" class="text-[var(--color-text-tertiary)] mr-2" />
            <h2 class="text-sm font-semibold text-[var(--color-text-primary)]">New Direct Message</h2>
          </div>
          <IconButton icon="x" variant="ghost" onclick={cancelCreate} disabled={isCreating} title="Cancel" />
        </div>
        <div class="flex-1 overflow-auto p-6">
          <div class="max-w-xl mx-auto space-y-4">
            <div class="space-y-2">
              <span class="block text-sm font-medium text-[var(--color-text-primary)]">Select Member</span>
              <div class="relative">
                <Icon
                  name="search"
                  size="sm"
                  class="absolute left-2 top-1/2 -translate-y-1/2 text-[var(--color-text-tertiary)]"
                />
                <Input
                  unstyled
                  placeholder="Search members..."
                  bind:value={dmSearchQuery}
                  class="w-full pl-7 pr-2 py-1.5 text-sm rounded
                    bg-[var(--color-bg-primary)] border border-[var(--color-border)]
                    text-[var(--color-text-primary)] placeholder:text-[var(--color-text-tertiary)]
                    focus:outline-none focus:border-[var(--color-accent-primary)]"
                />
              </div>
              <div class="max-h-80 overflow-y-auto border border-[var(--color-border-secondary)] rounded-lg divide-y divide-[var(--color-border-secondary)]">
                {#if dmUsersLoading}
                  <div class="flex items-center justify-center py-6">
                    <Spinner size="md" />
                  </div>
                {:else if filteredDmUsers.length === 0}
                  <div class="px-3 py-4 text-center text-sm text-[var(--color-text-tertiary)]">
                    No matching members.
                  </div>
                {:else}
                  {#each filteredDmUsers as user (user.id)}
                    <Button
                      variant="unstyled"
                      class="w-full flex items-center gap-3 px-3 py-2.5 text-left transition-colors
                        hover:bg-[var(--color-bg-tertiary)] disabled:opacity-50 disabled:cursor-not-allowed"
                      onclick={() => handleDmSelectUser(user.id)}
                      disabled={isCreating}
                    >
                      <div class="relative flex-shrink-0">
                        <Avatar
                          name={user.name ?? 'Unknown'}
                          size="sm"
                        />
                        <span
                          class="absolute -bottom-0.5 -right-0.5 w-2.5 h-2.5 rounded-full border-2 border-[var(--color-bg-primary)] {onlineUsers.has(user.id)
                            ? 'bg-[var(--color-success-500)]'
                            : 'bg-[var(--color-neutral-400)]'}"
                        ></span>
                      </div>
                      <div class="flex-1 min-w-0">
                        <div class="font-medium text-sm text-[var(--color-text-primary)] truncate">
                          {user.name}
                        </div>
                        <div class="text-xs text-[var(--color-text-tertiary)] truncate">
                          {user.email}
                        </div>
                      </div>
                    </Button>
                  {/each}
                {/if}
              </div>
            </div>
            {#if createError}
              <p class="text-sm text-red-500">{createError}</p>
            {/if}
          </div>
        </div>
      </div>
    {:else}
      <SidebarDetailLayout
        showRightPanel
        sidebarWidth={SIDEBAR_DETAIL_LAYOUT.sidebarWidth}
        minSidebarWidth={SIDEBAR_DETAIL_LAYOUT.minSidebarWidth}
        maxSidebarWidth={SIDEBAR_DETAIL_LAYOUT.maxSidebarWidth}
        rightPanelWidth={SIDEBAR_DETAIL_LAYOUT.rightPanelWidth}
        minRightPanelWidth={SIDEBAR_DETAIL_LAYOUT.minRightPanelWidth}
        maxRightPanelWidth={SIDEBAR_DETAIL_LAYOUT.maxRightPanelWidth}
      >
        {#snippet sidebar()}
          <ConversationSidebar
            {channels}
            {privateChannels}
            {topics}
            {starredConversations}
            {selectedConversationId}
            {searchQuery}
            onConversationSelect={handleConversationSelect}
            onSectionToggle={handleSectionToggle}
            onConversationStarToggle={handleConversationStarToggle}
            onSearchChange={handleSearchChange}
            onCreateChannel={handleCreateChannel}
            onCreateForum={handleCreateForum}
            hasMore={hasMoreConversations || hasMoreDirectMessages}
            {loadingMore}
            onLoadMore={handleLoadMoreSidebar}
            class="h-full"
          />
        {/snippet}

        <!-- Main content: Create form, Forum, Channel, or Overview -->
        {#if createMode}
          <ConversationCreatePanel
            icon={createMode === 'forum' ? 'list' : 'hash'}
            title={createMode === 'forum' ? 'New Forum' : 'New Channel'}
            bind:name={createName}
            bind:description={createDescription}
            namePlaceholder={createMode === 'forum' ? 'e.g., API design discussions' : 'e.g., general'}
            descriptionPlaceholder="What is this conversation for?"
            loading={isCreating}
            error={createError}
            isValid={isCreateFormValid}
            onSubmit={submitCreate}
            onCancel={cancelCreate}
          />
        {:else if selectedConversation && isForumSelected}
          <ForumView
            {tenantId}
            conversationId={selectedConversation.id}
            conversationName={selectedConversation.name}
            class="h-full"
          />
        {:else if selectedConversation}
          <ChannelView
            conversationId={selectedConversation.id}
            conversationName={selectedConversation.name}
            headerIcon={selectedConversation.type === 'dm' ? 'message-circle' : selectedConversation.type === 'private' ? 'lock' : 'hash'}
            sourceType="conversation"
            conversationType={selectedConversation.type === 'dm' ? 'DirectMessage' : 'Channel'}
            tenantId={tenantId || undefined}
            projectId={selectedConversation.projectId || ''}
            channelStatus={selectedConversation.channelStatus ?? null}
            hideParticipantPanel={true}
            onMembersChanged={(conversationId, members) => {
              if (conversationId !== selectedConversationId) return;
              currentChannel = {
                id: conversationId,
                topic: selectedConversation?.name ?? 'Channel',
                status: 'Active',
                participantCount: members.length,
                messageCount: 0,
                createdAt: new Date().toISOString(),
                concludedAt: null,
                participants: members,
              };
            }}
            onClose={() => { setSelectedConversation(null); }}
            class="h-full"
          />
        {:else}
          <!-- Overview -->
          <PageShell class="h-full">
            <PageHeader title="Overview" />
            <PageBody padded={false} class="p-3 space-y-3">
              <CardGrid cols={4} gap="md">
                <StatCard title="Unread" value={totalUnreadCount} icon="bell" />
                <StatCard title="Channels" value={channelCount} icon="hash" />
                <StatCard title="Forums" value={forumCount} icon="message-square" />
                <StatCard title="DMs" value={dmCount} icon="user" />
              </CardGrid>

              <p class="text-sm text-[var(--color-text-secondary)] text-center">
                Select a channel, forum, or DM from the sidebar to start chatting.
              </p>
            </PageBody>
          </PageShell>
        {/if}

        {#snippet rightPanel()}
          <ParticipantsPanel
            channel={currentChannel}
            {currentUserId}
            {tenantId}
            conversationId={selectedConversationId ?? ''}
            onRemoveMember={handleRemoveParticipant}
            onSendInvitation={handleSendInvitation}
            class="h-full"
          />
        {/snippet}
      </SidebarDetailLayout>
    {/if}
  </PageBody>
</PageShell>
