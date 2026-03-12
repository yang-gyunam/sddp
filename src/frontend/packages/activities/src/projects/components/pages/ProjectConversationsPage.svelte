<!-- Activity: Projects > Nav: Conversations (project-{id}-conversations) | Screen ID: PRJ-DISC-001 -->
<script lang="ts">
  import { untrack } from 'svelte';
  import { SvelteMap, SvelteSet } from 'svelte/reactivity';
  import { Icon, Input, IconButton, Button, CardGrid, Checkbox, Spinner, SearchField } from '@sddp/ui';
  import { PageShell, PageHeader, PageBody, CollapsibleGroup, SidebarDetailLayout, SIDEBAR_DETAIL_LAYOUT, getTabState, setTabState, toast, Avatar } from '@sddp/shell';
  import { getAuthState } from '@sddp/shell/auth';
  import { consumePendingEntityId } from '../../stores';
  import type { ProjectDetail, ProjectMember } from '../../types';
  import type {
    ConversationEntry,
    DirectMessage,
    ConversationMember,
    DirectMessageInfo,
  } from '../../../conversations/types';
  import { getConversationService, getConversationHubService } from '../../../conversations/services';
  import type { ConversationSummaryDto, InvitableUser } from '../../../conversations/services';
  import {
    subscribeConversation,
    getConversationStoreState,
    removeDirectMessage,
    setActiveConversationContext,
    clearActiveConversationContext,
  } from '../../../conversations/stores';
  import { StatCard } from '../../../shared/components/idioms';
  import { ConversationCreatePanel } from '../../../conversations/components/sections';
  import DirectMessageList from '../../../conversations/components/sections/DirectMessageList.svelte';
  import ChannelView from '../../../conversations/components/sections/ChannelView.svelte';
  import ForumView from '../../../conversations/components/sections/ForumView.svelte';
  import TraceGraphSection from '../../../relationship/components/sections/TraceGraphSection.svelte';

  /** Sidebar conversation group (channels, forums) */
  interface CategoryAction {
    id: string;
    icon: string;
    label: string;
    onClick: () => void;
  }

  interface Category {
    id: string;
    name: string;
    isCollapsed: boolean;
    conversations: ConversationEntry[];
    actions: CategoryAction[];
  }

  interface Props {
    projectId: string;
    projectName?: string;
    project?: ProjectDetail;
    tabId?: string;
    class?: string;
  }

  type ProjectConversationSelectDetail = {
    projectId: string;
    conversationId: string;
    conversationKind?: 'channel' | 'forum' | 'dm';
    participantName?: string;
    channelStatus?: 'Active' | 'Concluded' | null;
  };

  type DirectMessageConcludedDetail = {
    conversationId: string;
    origin?: string;
  };

  let { projectId, projectName = '', project, tabId = '', class: className = '' }: Props = $props();

  const pageTitle = 'Conversations';
  const pageMeta = $derived(project?.name || projectName || undefined);
  type CreateMode = 'channel' | 'private-channel' | 'topic' | 'dm' | null;

  // Data (API-backed)
  let categories = $state<Category[]>([]);
  let directMessages = $state<DirectMessage[]>([]);
  let loading = $state(false);
  let loadingMore = $state(false);
  let error = $state<string | null>(null);
  let tenantId = $state<string | null>(null);
  let allConversationDtos = $state<ConversationSummaryDto[]>([]);
  let allDmChannels = $state<DirectMessageInfo[]>([]);
  let conversationPageNumber = $state(1);
  let dmPageNumber = $state(1);
  let hasMoreConversations = $state(false);
  let hasMoreDirectMessages = $state(false);
  const CONVERSATION_PAGE_SIZE = 20;
  let latestConversationRequestId = $state(0);

  // UI state
  let selectedConversationId = $state<string | null>(null);
  let selectedDmId = $state<string | null>(null);
  let createMode = $state<CreateMode>(null);
  let searchQuery = $state('');
  let createChannelName = $state('');
  let createChannelTopic = $state('');
  let createChannelIsPrivate = $state(false);
  let createTopicTitle = $state('');
  let createPanelLoading = $state(false);
  let createPanelError = $state<string | null>(null);
  let selectedParticipantIds = new SvelteSet<string>();
  let participantQuery = $state('');
  let currentUserId = $state<string | null>(getAuthState().user?.id ?? null);
  let dmExpanded = $state(true);
  let dmFallbackMembers = $state<ProjectMember[]>([]);
  let pendingSelectionId = $state<string | null>(null);

  function ensureConversationContext(svc?: ReturnType<typeof getConversationService>): ReturnType<typeof getConversationService> {
    const service = svc ?? getConversationService();
    const authState = getAuthState();
    const resolvedTenantId = tenantId ?? authState.user?.tenantId ?? '';
    service.setContext(resolvedTenantId, projectId);
    return service;
  }

  const isCreateFormValid = $derived(
    createMode === 'channel' || createMode === 'private-channel'
      ? createChannelName.trim().length > 0
      : createMode === 'topic'
        ? createTopicTitle.trim().length > 0
        : false
  );

  interface ProjectConversationsTabState {
    selectedConversationId: string | null;
    selectedDmId: string | null;
    searchQuery: string;
  }

  const tabStateKey = $derived(tabId || `project-${projectId}-conversations`);
  const activeConversationTabId = $derived(tabId || `project-${projectId}-conversations`);
  let restoredKey = $state<string | null>(null);
  let isRestored = $state(false);

  $effect(() => {
    if (!tabStateKey || restoredKey === tabStateKey) return;
    const saved = getTabState<ProjectConversationsTabState>(tabStateKey);
    if (saved) {
      selectedConversationId = saved.selectedConversationId ?? null;
      selectedDmId = saved.selectedDmId ?? null;
      searchQuery = saved.searchQuery ?? '';
    }
    restoredKey = tabStateKey;
    isRestored = true;
  });

  // Persist tab state
  $effect(() => {
    if (!tabStateKey || !isRestored) return;
    const state: ProjectConversationsTabState = {
      selectedConversationId,
      selectedDmId,
      searchQuery,
    };
    setTabState<ProjectConversationsTabState>(tabStateKey, state);
  });

  // Derived data
  const allConversations = $derived.by(() => categories.flatMap((category) => category.conversations));
  const selectedConversation = $derived(allConversations.find((c) => c.id === selectedConversationId) ?? null);
  const selectedConversationKind = $derived.by(() => {
    if (selectedDmId) return 'dm' as const;
    if (!selectedConversation) return null;
    return selectedConversation.type === 'Forum' ? 'forum' as const : 'channel' as const;
  });
  // ChannelView sourceType domain glossary(Channel/Forum) DTO.
  // 'channel' uses channel-specific DTO (MessagesPage), 'conversation' uses generic DTO (ConversationMessagesPage).
  // API (/conversations/{id}/messages).
  const selectedConversationSource = $derived(
    selectedConversation?.type === 'Forum' ? 'conversation' as const : 'channel' as const
  );
  const directMessagesWithPresence = $derived.by(() =>
    directMessages.map((dm) => ({
      ...dm,
      isOnline: dm.otherUserId ? onlineUsers.has(dm.otherUserId) : (dm.isOnline ?? false),
    }))
  );
  const selectedDm = $derived(directMessagesWithPresence.find((dm) => dm.id === selectedDmId) ?? null);

  const filteredCategories = $derived.by(() => {
    const query = searchQuery.trim().toLowerCase();
    if (!query) return categories;
    return categories.map((category) => ({
      ...category,
      conversations: category.conversations.filter((c) => c.name?.toLowerCase().includes(query)),
    }));
  });

  const filteredDms = $derived.by(() => {
    const query = searchQuery.trim().toLowerCase();
    if (!query) return directMessagesWithPresence;
    return directMessagesWithPresence.filter((dm) => dm.name?.toLowerCase().includes(query));
  });

  $effect(() => {
    if (!selectedConversationId) return;
    if (loading) return;
    if (!allConversations.some((c) => c.id === selectedConversationId)) {
      selectedConversationId = null;
    }
  });

  $effect(() => {
    if (!selectedDmId) return;
    if (!directMessagesWithPresence.some((dm) => dm.id === selectedDmId)) {
      selectedDmId = null;
    }
  });

  $effect(() => {
    if (!pendingSelectionId || loading) return;

    if (allConversations.some((c) => c.id === pendingSelectionId)) {
      selectedConversationId = pendingSelectionId;
      selectedDmId = null;
      pendingSelectionId = null;
      return;
    }

    if (directMessagesWithPresence.some((dm) => dm.id === pendingSelectionId)) {
      selectedDmId = pendingSelectionId;
      selectedConversationId = null;
      pendingSelectionId = null;
    }
  });

  $effect(() => {
    if (!activeConversationTabId) return;

    setActiveConversationContext({
      tabId: activeConversationTabId,
      conversationId: selectedDmId ?? selectedConversationId,
      projectId,
      kind: selectedConversationKind,
    });

    return () => {
      clearActiveConversationContext(activeConversationTabId);
    };
  });

  // NOTE: No auto-selection - user must explicitly select a conversation/DM
  // Empty state will show overview/stats when nothing is selected

  // Stats for overview
  const channelCount = $derived(
    categories.find((c) => c.id === 'cat-channels')?.conversations.length ?? 0
  );
  const forumCount = $derived(
    categories.find((c) => c.id === 'cat-forums')?.conversations.length ?? 0
  );
  const dmCount = $derived(directMessagesWithPresence.length);
  const totalUnreadCount = $derived(
    allConversations.reduce((sum, c) => sum + (c.unreadCount ?? 0), 0) +
    directMessages.reduce((sum, dm) => sum + (dm.unreadCount ?? 0), 0)
  );

  // Participants data (loaded from API per conversation)
  let channelParticipants = $state<ConversationMember[]>([]);

  const dmParticipants = $derived.by<ConversationMember[] | null>(() => {
    if (!selectedDm) return null;
    const now = new Date().toISOString();
    const otherUserId = selectedDm.otherUserId ?? selectedDm.id;
    return [
      {
        id: 'participant-self',
        user: { id: currentUserId ?? 'current-user', name: getAuthState().user?.displayName ?? 'You', avatarUrl: null },
        type: 'Human' as const,
        role: 'Member',
        joinedAt: now,
        isActive: true,
      },
      {
        id: `participant-${otherUserId}`,
        user: { id: otherUserId, name: selectedDm.name, avatarUrl: null },
        type: 'Human' as const,
        role: 'Member',
        joinedAt: now,
        isActive: selectedDm.isOnline ?? false,
      },
    ];
  });

  const activeParticipants = $derived.by<ConversationMember[]>(() => {
    // DM: API,
    if (selectedDm) return channelParticipants.length > 0 ? channelParticipants : (dmParticipants ?? []);
    if (selectedConversation && selectedConversation.type !== 'Forum') return channelParticipants;
    return [];
  });

  /** Whether current user is the Owner of the selected conversation (can remove members) */
  const isConversationOwner = $derived(
    activeParticipants.some((p) => p.user.id === currentUserId && (p.role ?? '').toLowerCase() === 'owner')
  );

  function isOwnerRole(role: string | null | undefined): boolean {
    return (role ?? '').toLowerCase() === 'owner';
  }

  /** Project members used for create-mode participant selection */
  const projectParticipants = $derived.by(() => {
    const members = (project?.members && project.members.length > 0)
      ? project.members
      : dmFallbackMembers;
    return members.map((member) => ({
      id: member.userId,
      user: { id: member.userId, name: member.displayName, avatarUrl: null },
      type: 'Human' as const,
      role: member.role || 'Member',
      joinedAt: member.lastActivityAt ?? new Date().toISOString(),
      isActive: member.isOnline,
    }));
  });

  const selectableParticipants = $derived.by(() => {
    const query = participantQuery.trim().toLowerCase();
    const base = projectParticipants.filter((p) => p.type === 'Human');
    if (!query) return base;
    return base.filter((p) => (p.user.name ?? '').toLowerCase().includes(query));
  });
  const selectedParticipantCount = $derived(selectedParticipantIds.size);

  /** Right panel shows Participants + TraceGraphSection for Channels and DMs only (not Forums) */
  const shouldShowRightPanel = $derived(
    !!((selectedConversation && selectedConversation.type !== 'Forum') || selectedDm) && !createMode
  );

  let participantsExpanded = $state(true);

  // Inline invite search state
  let showInviteSearch = $state(false);
  let inviteSearchQuery = $state('');
  let inviteSearchResults = $state<InvitableUser[]>([]);
  let inviteSearching = $state(false);
  let sendingUserId = $state<string | null>(null);
  let searchDebounceTimer: ReturnType<typeof setTimeout> | null = null;

  // Show invite action if owner AND has tenantId+conversationId
  const canInvite = $derived(isConversationOwner && !!tenantId && !!selectedConversationId);

  // Online users from ConversationHub presence tracking
  let onlineUsers = $state<Set<string>>(getConversationStoreState().onlineUsers);

  $effect(() => {
    const unsubscribe = subscribeConversation((state) => {
      onlineUsers = state.onlineUsers;
    });
    return unsubscribe;
  });

  function clearUnreadForConversation(conversationId: string) {
    categories = categories.map((category) => ({
      ...category,
      conversations: category.conversations.map((c) =>
        c.id === conversationId ? { ...c, unreadCount: 0 } : c
      ),
    }));
  }

  // Inline invite search handlers
  function toggleInviteSearch() {
    showInviteSearch = !showInviteSearch;
    if (showInviteSearch) {
      inviteSearchQuery = '';
      inviteSearchResults = [];
      sendingUserId = null;
    }
  }

  function handleInviteSearchInput(query: string) {
    inviteSearchQuery = query;
    if (searchDebounceTimer) clearTimeout(searchDebounceTimer);
    if (!query || query.length < 2) {
      inviteSearchResults = [];
      return;
    }
    searchDebounceTimer = setTimeout(() => {
      untrack(() => searchInvitableUsers(query));
    }, 300);
  }

  async function searchInvitableUsers(query: string) {
    if (!tenantId || !selectedConversationId) return;
    inviteSearching = true;
    try {
      const service = getConversationService();
      service.setContext(tenantId, '');
      inviteSearchResults = await service.getInvitableUsers(selectedConversationId, query);
    } catch {
      inviteSearchResults = [];
    } finally {
      inviteSearching = false;
    }
  }

  async function handleSelectInviteUser(user: InvitableUser) {
    if (sendingUserId || !selectedConversationId) return;
    sendingUserId = user.id;
    try {
      const hubService = getConversationHubService();
      const convName = selectedConversation?.name ?? 'Channel';
      await hubService.sendInvitation(selectedConversationId, user.id, convName);
      toast.success(`Invitation sent to ${user.displayName}`);
      inviteSearchResults = inviteSearchResults.filter((u) => u.id !== user.id);
    } catch {
      toast.error(`Failed to send invitation to ${user.displayName}`);
    } finally {
      sendingUserId = null;
    }
  }

  function handleConversationSelect(conversation: ConversationEntry) {
    createMode = null;
    selectedConversationId = conversation.id;
    selectedDmId = null;
    clearUnreadForConversation(conversation.id);
  }

  function handleDmSelect(dm: DirectMessage) {
    createMode = null;
    selectedDmId = dm.id;
    selectedConversationId = null;
    channelParticipants = [];
  }

  function handleConversationStatusChange(
    conversationId: string,
    status: 'Active' | 'Concluded'
  ): void {
    categories = categories.map((category) => ({
      ...category,
      conversations: category.conversations.map((conversation) =>
        conversation.id === conversationId
          ? { ...conversation, channelStatus: status }
          : conversation
      ),
    }));
  }

  function handleDirectMessageStatusChange(
    dmId: string,
    status: 'Active' | 'Concluded'
  ): void {
    // status (Concluded /)
    // user X MemberRemoved
    directMessages = directMessages.map((dm) =>
      dm.id === dmId ? { ...dm, channelStatus: status } : dm
    );
  }

  function handleSearchInput(e: Event) {
    const target = e.target as HTMLInputElement;
    searchQuery = target.value;
  }

  function resetCreateForm() {
    createChannelName = '';
    createChannelTopic = '';
    createChannelIsPrivate = false;
    createTopicTitle = '';
    createPanelError = null;
    participantQuery = '';
    selectedParticipantIds = new SvelteSet(currentUserId ? [currentUserId] : []);
  }

  function handleCreateChannel() {
    resetCreateForm();
    createMode = 'channel';
    selectedConversationId = null;
    selectedDmId = null;
  }

  function handleCreateTopic() {
    resetCreateForm();
    createMode = 'topic';
    selectedConversationId = null;
    selectedDmId = null;
  }

  async function handleCreateDM() {
    resetCreateForm();
    createMode = 'dm';
    selectedConversationId = null;
    selectedDmId = null;

    if (!project?.members || project.members.length === 0) {
      createPanelLoading = true;
      createPanelError = null;
      try {
        const authState = getAuthState();
        const tid = tenantId ?? authState.user?.tenantId ?? '';
        if (!tid) throw new Error('Missing tenant ID');
        const { getProjectById } = await import('../../services/ProjectService');
        const detail = await getProjectById(tid, projectId);
        dmFallbackMembers = detail.members;
      } catch (err) {
        createPanelError = err instanceof Error ? err.message : 'Failed to load members.';
      } finally {
        createPanelLoading = false;
      }
    }
  }

  async function handleDmSelectUser(targetUserId: string) {
    if (createPanelLoading) return;
    createPanelLoading = true;
    createPanelError = null;
    try {
      const authState = getAuthState();
      const resolvedTenantId = tenantId ?? authState.user?.tenantId ?? '';
      if (!resolvedTenantId) {
        throw new Error('Missing tenant ID');
      }
      const service = ensureConversationContext();
      const result = await service.sendDMInvitation(targetUserId);
      if (result.sent) {
        toast.success('Invitation sent');
        createMode = null;
      } else {
        toast.info(result.message);
      }
    } catch (err) {
      createPanelError = err instanceof Error ? err.message : 'Failed to send invitation.';
    } finally {
      createPanelLoading = false;
    }
  }

  function cancelCreateMode() {
    createMode = null;
    createPanelError = null;
  }

  function toggleParticipant(userId: string) {
    const next = new SvelteSet(selectedParticipantIds);
    if (next.has(userId)) {
      next.delete(userId);
    } else {
      next.add(userId);
    }
    selectedParticipantIds = next;
  }

  function isParticipantSelected(userId: string): boolean {
    return selectedParticipantIds.has(userId);
  }

  async function loadConversationMembers(conversationId: string): Promise<void> {
    try {
      const authState = getAuthState();
      const resolvedTenantId = tenantId ?? authState.user?.tenantId ?? '';
      if (!resolvedTenantId) return;

      currentUserId = authState.user?.id ?? null;

      const service = ensureConversationContext();
      const members = await service.getConversationMembers(conversationId);
      if (conversationId !== selectedConversationId) return;
      channelParticipants = members;
    } catch (err) {
      console.warn('Failed to load conversation members:', err);
      if (conversationId !== selectedConversationId) return;
      channelParticipants = [];
    }
  }

  async function handleRemoveParticipant(participant: ConversationMember): Promise<void> {
    const conversationId = selectedConversationId ?? selectedDmId;
    // Capture before await — SignalR MemberRemoved may null these during the call
    const dmIdBeforeAwait = selectedDmId;
    const channelIdBeforeAwait = selectedConversationId;
    if (!conversationId) return;
    try {
      const authState = getAuthState();
      const resolvedTenantId = tenantId ?? authState.user?.tenantId ?? '';
      if (!resolvedTenantId) return;

      const service = ensureConversationContext();
      await service.removeConversationMember(conversationId, participant.user.id);

      const isSelf = participant.user.id === currentUserId;

      if (isSelf && dmIdBeforeAwait) {
        // DM → + + sidebar store
        const dmId = dmIdBeforeAwait;
        selectedDmId = null;
        directMessages = directMessages.filter((dm) => dm.id !== dmId);
        removeDirectMessage(dmId);
        toast.success('Left the conversation');
        if (typeof window !== 'undefined') {
          window.dispatchEvent(
            new CustomEvent('sddp:dm-concluded', {
              detail: { conversationId: dmId, origin: 'self-leave' },
            })
          );
        }
      } else if (isSelf && channelIdBeforeAwait) {
        // channel →
        selectedConversationId = null;
        toast.success('Left the channel');
      } else {
        toast.success(`${participant.user.name} removed`);
        await loadConversationMembers(conversationId);
      }
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Failed to remove member');
    }
  }

  async function markConversationAsRead(conversationId: string): Promise<void> {
    try {
      const authState = getAuthState();
      const resolvedTenantId = tenantId ?? authState.user?.tenantId ?? '';
      if (!resolvedTenantId) return;

      const service = ensureConversationContext();
      await service.markAsRead(conversationId);
    } catch (err) {
      console.warn('Failed to mark conversation as read:', err);
    }
  }

  async function submitCreateMode(): Promise<void> {
    if (createMode === 'channel' || createMode === 'private-channel') {
      if (!createChannelName.trim()) {
        createPanelError = 'Name is required.';
        return;
      }
    } else if (createMode === 'topic') {
      if (!createTopicTitle.trim()) {
        createPanelError = 'Topic title is required.';
        return;
      }
    } else {
      return;
    }

    createPanelLoading = true;
    createPanelError = null;
    try {
      const authState = getAuthState();
      const resolvedTenantId = tenantId ?? authState.user?.tenantId ?? '';
      if (!resolvedTenantId) {
        throw new Error('Missing tenant ID');
      }

      const service = ensureConversationContext();

      if (createMode === 'channel' || createMode === 'private-channel') {
        const created = await service.createConversation({
          name: createChannelName.trim(),
          conversationType: 'Channel',
          visibility: createChannelIsPrivate ? 'Private' : 'Public',
          scope: 'ProjectScoped',
          description: createChannelTopic.trim() || undefined,
        });

        await loadConversationData();
        createMode = null;
        selectedConversationId = created.id;
        selectedDmId = null;
      } else if (createMode === 'topic') {
        const created = await service.createConversation({
          name: createTopicTitle.trim(),
          conversationType: 'Forum',
          scope: 'ProjectScoped',
        });

        await loadConversationData();
        createMode = null;
        selectedConversationId = created.id;
        selectedDmId = null;
      }
    } catch (err) {
      createPanelError = err instanceof Error ? err.message : 'Failed to create conversation.';
    } finally {
      createPanelLoading = false;
    }
  }

  // Track previous selection to prevent redundant API calls on re-render
  let prevSelectedConvId = $state<string | null>(null);

  $effect(() => {
    if (!selectedConversation) {
      channelParticipants = [];
      prevSelectedConvId = null;
      return;
    }
    if (selectedConversation.id === prevSelectedConvId) return;
    prevSelectedConvId = selectedConversation.id;

    // channel participants stale owner
    channelParticipants = [];
    // Close invite search on conversation change
    if (showInviteSearch) showInviteSearch = false;

    // Load data based on conversation type
    // Members are loaded by ChannelView's own $effect → onMembersChanged callback
    if (selectedConversation.type === 'Forum') {
      untrack(() => markConversationAsRead(selectedConversation.id));
    }
  });

  function mapChannelConversation(dto: ConversationSummaryDto): ConversationEntry {
    return {
      id: dto.id,
      name: dto.name,
      type: dto.conversationType,
      visibility: dto.visibility,
      scope: dto.scope,
      description: dto.description ?? undefined,
      isPrivate: dto.visibility === 'Private',
      unreadCount: dto.unreadCount ?? 0,
      channelStatus: dto.channelStatus ?? null,
      decisionSpecId: dto.decisionSpecId ?? null,
    };
  }

  function buildCategories(
    conversations: ConversationSummaryDto[],
  ): Category[] {
    const allChannels = conversations.filter((c) => c.conversationType === 'Channel');
    const forums = conversations.filter((c) => c.conversationType === 'Forum');

    return [
      {
        id: 'cat-channels',
        name: 'CHANNELS',
        isCollapsed: false,
        conversations: allChannels.map(mapChannelConversation),
        actions: [{ id: 'add-channel', icon: 'plus', label: 'New Channel', onClick: handleCreateChannel }],
      },
      {
        id: 'cat-forums',
        name: 'FORUMS',
        isCollapsed: false,
        conversations: forums.map(mapChannelConversation),
        actions: [{ id: 'add-forum', icon: 'plus', label: 'New Forum', onClick: handleCreateTopic }],
      },
    ];
  }

  function mapDmChannel(channel: DirectMessageInfo): DirectMessage {
    return {
      id: channel.id,
      otherUserId: channel.otherUser.id,
      name: channel.otherUser.name ?? '',
      lastMessage: channel.lastMessage?.content,
      lastActiveAt: channel.lastMessage?.createdAt ?? channel.updatedAt,
      unreadCount: channel.unreadCount,
      isOnline: false,
      channelStatus: channel.status ?? null,
    };
  }

  function mergeById<T extends { id: string }>(current: T[], incoming: T[]): T[] {
    const merged = new SvelteMap<string, T>();
    for (const item of current) {
      merged.set(item.id, item);
    }
    for (const item of incoming) {
      merged.set(item.id, item);
    }
    return Array.from(merged.values());
  }

  async function loadConversationData(reset: boolean = true): Promise<void> {
    if (!reset && (loading || loadingMore || (!hasMoreConversations && !hasMoreDirectMessages))) {
      return;
    }

    const requestId = ++latestConversationRequestId;
    const nextConversationPage = reset
      ? 1
      : hasMoreConversations ? conversationPageNumber + 1 : conversationPageNumber;
    const nextDmPage = reset
      ? 1
      : hasMoreDirectMessages ? dmPageNumber + 1 : dmPageNumber;

    if (reset) {
      loading = true;
      error = null;
      conversationPageNumber = 1;
      dmPageNumber = 1;
      hasMoreConversations = false;
      hasMoreDirectMessages = false;
      allConversationDtos = [];
      allDmChannels = [];
    } else {
      loadingMore = true;
    }

    try {
      const authState = getAuthState();
      if (!authState.user?.tenantId) {
        throw new Error('User not authenticated or missing tenant');
      }

      tenantId = authState.user.tenantId;
      currentUserId = authState.user.id ?? null;
      const service = ensureConversationContext();

      const conversationPageRequest = reset || hasMoreConversations
        ? service.getConversationsPage({
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

      const [convResult, dmResult] = await Promise.allSettled([
        conversationPageRequest,
        dmPageRequest,
      ]);
      if (requestId !== latestConversationRequestId) return;
      const conversationPage = convResult.status === 'fulfilled' ? convResult.value : null;
      const dmPage = dmResult.status === 'fulfilled' ? dmResult.value : null;

      allConversationDtos = conversationPage
        ? mergeById(reset ? [] : allConversationDtos, conversationPage.items)
        : (reset ? [] : allConversationDtos);
      allDmChannels = dmPage
        ? mergeById(reset ? [] : allDmChannels, dmPage.items)
        : (reset ? [] : allDmChannels);

      if (conversationPage) {
        conversationPageNumber = conversationPage.pageNumber ?? nextConversationPage;
        hasMoreConversations = conversationPage.hasNextPage
          ?? allConversationDtos.length < conversationPage.totalCount;
      } else if (reset) {
        hasMoreConversations = false;
      }

      if (dmPage) {
        dmPageNumber = dmPage.pageNumber ?? nextDmPage;
        hasMoreDirectMessages = dmPage.hasNextPage
          ?? allDmChannels.length < dmPage.totalCount;
      } else if (reset) {
        hasMoreDirectMessages = false;
      }

      categories = buildCategories(allConversationDtos);
      directMessages = allDmChannels.map(mapDmChannel);
    } catch (err) {
      if (requestId !== latestConversationRequestId) return;
      console.error('Failed to load conversations:', err);
      toast.error(err instanceof Error ? err.message : 'Failed to load conversations');
      error = err instanceof Error ? err.message : 'Failed to load conversations';
      hasMoreConversations = false;
      hasMoreDirectMessages = false;
      if (reset) {
        categories = [];
        directMessages = [];
      }
    } finally {
      if (requestId === latestConversationRequestId) {
        loading = false;
        loadingMore = false;
      }
    }
  }

  function handleSidebarScroll(e: Event): void {
    if (loading || loadingMore || (!hasMoreConversations && !hasMoreDirectMessages)) return;

    const target = e.currentTarget as HTMLElement;
    const nearBottom = target.scrollHeight - target.scrollTop - target.clientHeight <= 100;
    if (nearBottom) {
      void loadConversationData(false);
    }
  }

  function upsertPendingDirectMessage(
    dmId: string,
    participantName?: string,
    status?: 'Active' | 'Concluded' | null
  ): void {
    const provisional: DirectMessage = {
      id: dmId,
      name: participantName?.trim() || 'Direct Message',
      lastMessage: undefined,
      lastActiveAt: new Date().toISOString(),
      unreadCount: 0,
      isOnline: false,
      channelStatus: status ?? 'Active',
    };

    if (directMessages.some((dm) => dm.id === dmId)) {
      directMessages = directMessages.map((dm) =>
        dm.id === dmId
          ? {
              ...dm,
              name: participantName?.trim() || dm.name,
              channelStatus: status ?? dm.channelStatus ?? 'Active',
            }
          : dm
      );
      return;
    }

    directMessages = [provisional, ...directMessages];
  }

  function handleExternalConversationSelection(event: Event): void {
    const detail = (event as CustomEvent<ProjectConversationSelectDetail>).detail;
    if (!detail?.projectId || !detail?.conversationId) return;
    if (detail.projectId !== projectId) return;

    if (detail.conversationKind === 'dm') {
      upsertPendingDirectMessage(detail.conversationId, detail.participantName, detail.channelStatus);
      selectedDmId = detail.conversationId;
      selectedConversationId = null;
    } else {
      selectedConversationId = allConversations.some((conversation) => conversation.id === detail.conversationId)
        ? detail.conversationId
        : null;
      selectedDmId = null;
    }
    pendingSelectionId = detail.conversationId;
    void loadConversationData();
  }

  function handleExternalDirectMessageConcluded(event: Event): void {
    const detail = (event as CustomEvent<DirectMessageConcludedDetail>).detail;
    if (!detail?.conversationId) return;
    if (detail.origin === 'self-leave' || detail.origin === 'member-removed') {
      // Self-leave or SignalR removal → remove from local DM list
      directMessages = directMessages.filter((dm) => dm.id !== detail.conversationId);
      if (selectedDmId === detail.conversationId) {
        selectedDmId = null;
      }
    } else {
      handleDirectMessageStatusChange(detail.conversationId, 'Concluded');
    }
  }

  $effect(() => {
    // Check for cross-type navigation (e.g., from Specs → Conversations)
    const pendingId = consumePendingEntityId();
    if (pendingId) {
      pendingSelectionId = pendingId;
    }
    untrack(() => void loadConversationData());

    if (typeof window === 'undefined') {
      return;
    }

    window.addEventListener(
      'sddp:project-conversation-select',
      handleExternalConversationSelection
    );
    window.addEventListener(
      'sddp:dm-concluded',
      handleExternalDirectMessageConcluded
    );

    return () => {
      window.removeEventListener(
        'sddp:project-conversation-select',
        handleExternalConversationSelection
      );
      window.removeEventListener(
        'sddp:dm-concluded',
        handleExternalDirectMessageConcluded
      );
    };
  });
</script>

<PageShell class="h-full {className}">
  <PageHeader title={pageTitle} meta={pageMeta} {loading}>
      {#snippet actions()}
        <IconButton
          icon="refresh-cw"
          size="sm"
          variant="ghost"
          title="Refresh"
          onclick={() => loadConversationData()}
        />
    {/snippet}
  </PageHeader>

  <PageBody class="p-0" scrollable={false}>
    <SidebarDetailLayout
      showRightPanel={shouldShowRightPanel}
      sidebarWidth={SIDEBAR_DETAIL_LAYOUT.sidebarWidth}
      minSidebarWidth={SIDEBAR_DETAIL_LAYOUT.minSidebarWidth}
      maxSidebarWidth={SIDEBAR_DETAIL_LAYOUT.maxSidebarWidth}
      rightPanelWidth={SIDEBAR_DETAIL_LAYOUT.rightPanelWidth}
      minRightPanelWidth={SIDEBAR_DETAIL_LAYOUT.minRightPanelWidth}
      maxRightPanelWidth={SIDEBAR_DETAIL_LAYOUT.maxRightPanelWidth}
    >
      {#snippet sidebar()}
        <div class="flex flex-col h-full bg-[var(--color-bg-primary)]">
          <!-- Header with Search -->
          <div class="flex-shrink-0 flex items-center p-2 min-h-12 border-b border-[var(--color-border-primary)]">
            <div class="relative flex-1">
              <Icon
                name="search"
                size="sm"
                class="absolute left-2.5 top-1/2 -translate-y-1/2 text-[var(--color-text-tertiary)]"
              />
              <Input
                type="text"
                placeholder="Search channels or DMs..."
                value={searchQuery}
                oninput={handleSearchInput}
                variant="flat"
                class="pl-8 w-full"
                size="sm"
              />
            </div>
          </div>

        <div
          class="flex-1 min-h-0 flex flex-col overflow-y-auto pb-1"
          onscroll={handleSidebarScroll}
        >
          {#if loading}
            <div class="flex-1 flex items-center justify-center">
              <!-- <Spinner size="lg" /> -->
            </div>
          {:else if error}
            <div class="px-3 py-4 text-sm text-red-500">
              {error}
            </div>
          {:else}
            <!-- All sections at the same level: CHANNELS, FORUMS, DIRECT MESSAGES -->
            {#each filteredCategories as category (category.id)}
              <CollapsibleGroup
                title={category.name}
                actions={category.actions}
                expanded={!category.isCollapsed}
                onToggle={() => {
                  categories = categories.map((c) =>
                    c.id === category.id ? { ...c, isCollapsed: !c.isCollapsed } : c
                  );
                }}
              >
                <div class="bg-[var(--color-bg-primary)]">
                  {#each category.conversations as conversation (conversation.id)}
                    <Button
                      variant="unstyled"
                      class="w-full flex items-center gap-2 px-2 py-1.5 cursor-pointer
                        transition-colors duration-150
                        border-b border-[var(--color-border-primary)]
                        {selectedConversationId === conversation.id
                          ? 'bg-[var(--color-accent-primary)]/10'
                          : 'hover:bg-[var(--color-bg-tertiary)]'}"
                      onclick={() => handleConversationSelect(conversation)}
                    >
                      <Icon
                        name={conversation.type === 'Forum' ? 'list' : conversation.isPrivate ? 'lock' : 'hash'}
                        size="sm"
                        class="flex-shrink-0 text-[var(--color-text-tertiary)]"
                      />
                      <span class="flex-1 text-left text-sm truncate text-[var(--color-text-primary)]">
                        {conversation.name}
                      </span>
                      {#if (conversation.unreadCount ?? 0) > 0}
                        <span
                          class="min-w-[18px] h-[18px] px-1 flex items-center justify-center text-xs font-medium
                            rounded-full bg-[var(--color-accent-primary)] text-white"
                        >
                          {(conversation.unreadCount ?? 0) > 99 ? '99+' : conversation.unreadCount}
                        </span>
                      {/if}
                    </Button>
                  {/each}
                  {#if category.conversations.length === 0}
                    <div class="px-2 py-2 text-center text-sm text-[var(--color-text-tertiary)]">
                      No {category.name.toLowerCase()}
                    </div>
                  {/if}
                </div>
              </CollapsibleGroup>
            {/each}

            <!-- DIRECT MESSAGES Section -->
            <CollapsibleGroup
              title="DIRECT MESSAGES"
              actions={[{ id: 'add-dm', icon: 'plus', label: 'New Message', onClick: handleCreateDM }]}
              expanded={dmExpanded}
              onToggle={() => (dmExpanded = !dmExpanded)}
            >
              <div class="bg-[var(--color-bg-primary)]">
                <DirectMessageList
                  items={filteredDms}
                  selectedId={selectedDmId}
                  onSelect={handleDmSelect}
                />
              </div>
            </CollapsibleGroup>

            {#if loadingMore}
              <div class="flex items-center justify-center py-3">
                <Spinner size="sm" />
              </div>
            {/if}
          {/if}
          </div>
        </div>
      {/snippet}

      <!-- Main Conversation Area (children slot) -->
      {#if createMode === 'channel' || createMode === 'private-channel'}
        <ConversationCreatePanel
          icon={createMode === 'private-channel' ? 'lock' : 'hash'}
          title={createMode === 'private-channel' ? 'New Private Channel' : 'New Channel'}
          bind:name={createChannelName}
          bind:description={createChannelTopic}
          namePlaceholder={createMode === 'channel' ? 'e.g., general' : 'e.g., architecture-team'}
          descriptionPlaceholder="What is this conversation for?"
          descriptionLabel="Topic"
          loading={createPanelLoading}
          error={createPanelError}
          isValid={isCreateFormValid}
          onSubmit={submitCreateMode}
          onCancel={cancelCreateMode}
        >
          {#if createMode === 'private-channel'}
            <div class="space-y-2">
              <div class="flex items-center justify-between">
                <span class="text-sm font-medium text-[var(--color-text-primary)]">Participants</span>
                <span class="text-xs text-[var(--color-text-tertiary)]">{selectedParticipantCount} selected</span>
              </div>
              <div class="relative">
                <Icon
                  name="search"
                  size="sm"
                  class="absolute left-2 top-1/2 -translate-y-1/2 text-[var(--color-text-tertiary)]"
                />
                <Input
                  unstyled
                  placeholder="Search participants..."
                  bind:value={participantQuery}
                  class="w-full pl-7 pr-2 py-1.5 text-sm rounded
                    bg-[var(--color-bg-primary)] border border-[var(--color-border)]
                    text-[var(--color-text-primary)] placeholder:text-[var(--color-text-tertiary)]
                    focus:outline-none focus:border-[var(--color-accent-primary)]"
                />
              </div>
              <div class="max-h-48 overflow-y-auto border border-[var(--color-border-secondary)] rounded-lg divide-y divide-[var(--color-border-secondary)]">
                {#if selectableParticipants.length === 0}
                  <div class="px-3 py-2 text-xs text-[var(--color-text-tertiary)]">
                    No matching participants.
                  </div>
                {:else}
                  {#each selectableParticipants as participant (participant.id)}
                    <Button
                      variant="unstyled"
                      class="w-full flex items-center gap-2 px-3 py-2 text-sm text-left hover:bg-[var(--color-bg-tertiary)] transition-colors"
                      onclick={() => toggleParticipant(participant.user.id)}
                    >
                      <Checkbox
                        unstyled
                        class="h-4 w-4"
                        checked={isParticipantSelected(participant.user.id)}
                      />
                      <div class="flex-1 min-w-0">
                        <div class="font-medium text-[var(--color-text-primary)] truncate">
                          {participant.user.name}
                        </div>
                        <div class="text-xs text-[var(--color-text-tertiary)] truncate">
                          {participant.role}
                        </div>
                      </div>
                    </Button>
                  {/each}
                {/if}
              </div>
            </div>
          {/if}
        </ConversationCreatePanel>
      {:else if createMode === 'topic'}
        <ConversationCreatePanel
          icon="list"
          title="New Forum"
          bind:name={createTopicTitle}
          bind:description={createChannelTopic}
          nameLabel="Forum Name"
          namePlaceholder="e.g., API contract discussion"
          showDescription={false}
          loading={createPanelLoading}
          error={createPanelError}
          isValid={isCreateFormValid}
          onSubmit={submitCreateMode}
          onCancel={cancelCreateMode}
        />
      {:else if createMode === 'dm'}
        <div class="flex flex-col h-full bg-[var(--color-bg-primary)]">
          <!-- Header -->
          <div class="flex items-center justify-between min-h-12 px-4 border-b border-[var(--color-border-primary)]">
            <div class="flex items-center">
              <Icon
                name="message-square"
                size="md"
                class="text-[var(--color-text-tertiary)] mr-2"
              />
              <h2 class="text-sm font-semibold text-[var(--color-text-primary)]">
                New Direct Message
              </h2>
            </div>
            <IconButton
              icon="x"
              variant="ghost"
              onclick={cancelCreateMode}
              disabled={createPanelLoading}
              title="Cancel"
            />
          </div>

          <!-- Body -->
          <div class="flex-1 overflow-auto p-6">
            <div class="max-w-xl mx-auto space-y-4">
              <div class="space-y-2">
                <span class="block text-sm font-medium text-[var(--color-text-primary)]">
                  Select Member
                </span>
                <div class="relative">
                  <Icon
                    name="search"
                    size="sm"
                    class="absolute left-2 top-1/2 -translate-y-1/2 text-[var(--color-text-tertiary)]"
                  />
                  <Input
                    unstyled
                    placeholder="Search members..."
                    bind:value={participantQuery}
                    class="w-full pl-7 pr-2 py-1.5 text-sm rounded
                      bg-[var(--color-bg-primary)] border border-[var(--color-border)]
                      text-[var(--color-text-primary)] placeholder:text-[var(--color-text-tertiary)]
                      focus:outline-none focus:border-[var(--color-accent-primary)]"
                  />
                </div>
                <div class="max-h-80 overflow-y-auto border border-[var(--color-border-secondary)] rounded-lg divide-y divide-[var(--color-border-secondary)]">
                  {#if createPanelLoading}
                    <div class="flex items-center justify-center py-6">
                      <Spinner size="md" />
                    </div>
                  {:else if selectableParticipants.length === 0}
                    <div class="px-3 py-4 text-center text-sm text-[var(--color-text-tertiary)]">
                      No matching participants.
                    </div>
                  {:else}
                    {#each selectableParticipants as participant (participant.id)}
                      <Button
                        variant="unstyled"
                        class="w-full flex items-center gap-3 px-3 py-2.5 text-left transition-colors
                          hover:bg-[var(--color-bg-tertiary)] disabled:opacity-50 disabled:cursor-not-allowed"
                        onclick={() => handleDmSelectUser(participant.user.id)}
                        disabled={createPanelLoading}
                      >
                        <div class="relative flex-shrink-0">
                          <Avatar
                            name={participant.user.name ?? 'Unknown'}
                            avatarUrl={participant.user.avatarUrl}
                            size="sm"
                          />
                          <span
                            class="absolute -bottom-0.5 -right-0.5 w-2.5 h-2.5 rounded-full border-2 border-[var(--color-bg-primary)] {onlineUsers.has(participant.user.id)
                              ? 'bg-[var(--color-success-500)]'
                              : 'bg-[var(--color-neutral-400)]'}"
                          ></span>
                        </div>
                        <div class="flex-1 min-w-0">
                          <div class="font-medium text-sm text-[var(--color-text-primary)] truncate">
                            {participant.user.name}
                          </div>
                          <div class="text-xs text-[var(--color-text-tertiary)] truncate">
                            {participant.role}
                          </div>
                        </div>
                      </Button>
                    {/each}
                  {/if}
                </div>
              </div>

              {#if createPanelError}
                <p class="text-sm text-red-500">{createPanelError}</p>
              {/if}
            </div>
          </div>
        </div>
      {:else if selectedConversation && selectedConversation.type === 'Forum'}
          <ForumView
            tenantId={tenantId ?? ''}
            {projectId}
            conversationId={selectedConversation.id}
            conversationName={selectedConversation.name}
            class="h-full"
          />
      {:else if selectedConversation}
          <ChannelView
            conversationId={selectedConversation.id}
            conversationName={selectedConversation.name}
            conversationDescription={selectedConversation.description}
            headerIcon="hash"
            sourceType={selectedConversationSource}
            conversationType={selectedConversation.type}
            tenantId={tenantId ?? undefined}
            {projectId}
            channelStatus={selectedConversation.channelStatus}
            allowRequirementExtract={selectedConversationSource === 'channel'}
            hideParticipantPanel={true}
            onStatusChange={(status) => handleConversationStatusChange(selectedConversation.id, status)}
            onMembersChanged={(conversationId, members) => {
              if (conversationId !== selectedConversationId) return;
              currentUserId = getAuthState().user?.id ?? null;
              channelParticipants = members;
            }}
            onClose={() => { selectedConversationId = null; }}
            class="h-full"
          />
        {:else if selectedDm}
          <ChannelView
            conversationId={selectedDm.id}
            conversationName={selectedDm.name}
            headerIcon="message-circle"
            sourceType="conversation"
            conversationType="DirectMessage"
            tenantId={tenantId ?? undefined}
            {projectId}
            channelStatus={selectedDm.channelStatus}
            allowRequirementExtract={false}
            hideParticipantPanel={true}
            onStatusChange={(status) => handleDirectMessageStatusChange(selectedDm.id, status)}
            onMembersChanged={(convId, members) => {
              if (convId !== selectedDmId) return;
              currentUserId = getAuthState().user?.id ?? null;
              channelParticipants = members;
            }}
            onClose={() => { selectedDmId = null; }}
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
        <div class="flex flex-col h-full bg-[var(--color-bg-secondary)] overflow-y-auto">
          <!-- Participants Section -->
          <CollapsibleGroup
            title="Participants ({activeParticipants.length})"
            actions={canInvite ? [{ id: 'invite', icon: 'plus', label: 'Invite Members', onClick: toggleInviteSearch }] : []}
            expanded={participantsExpanded}
            onToggle={() => (participantsExpanded = !participantsExpanded)}
          >
            <!-- Inline Invite Search -->
            {#if showInviteSearch}
              <div class="px-2 pt-2 pb-1 border-b border-[var(--color-border)]">
                <SearchField
                  bind:value={inviteSearchQuery}
                  placeholder="Search users by name or email..."
                  debounceMs={0}
                  onSearch={handleInviteSearchInput}
                  size="sm"
                />
                {#if inviteSearching}
                  <div class="flex justify-center py-3"><Spinner size="sm" /></div>
                {:else if inviteSearchResults.length > 0}
                  <div class="mt-1 max-h-[160px] overflow-y-auto">
                    {#each inviteSearchResults as user (user.id)}
                      <Button
                        variant="unstyled"
                        class="w-full flex items-center gap-2 px-2 py-1.5 rounded text-left
                          hover:bg-[var(--color-bg-tertiary)] transition-colors
                          {sendingUserId === user.id ? 'opacity-50 pointer-events-none' : ''}"
                        onclick={() => handleSelectInviteUser(user)}
                        disabled={sendingUserId === user.id}
                      >
                        <Avatar name={user.displayName} isAI={user.isAi} size="sm" />
                        <div class="flex-1 min-w-0">
                          <span class="text-sm text-[var(--color-text-primary)] truncate block">{user.displayName}</span>
                          <span class="text-xs text-[var(--color-text-tertiary)] truncate block">{user.email}</span>
                        </div>
                        {#if sendingUserId === user.id}
                          <Spinner size="sm" />
                        {/if}
                      </Button>
                    {/each}
                  </div>
                {:else if inviteSearchQuery.length >= 2}
                  <div class="text-xs text-[var(--color-text-tertiary)] mt-2 mb-1 text-center">No users found</div>
                {/if}
              </div>
            {/if}

            <div class="px-2 py-2">
              {#each activeParticipants as participant (participant.id)}
                <div class="group relative flex items-center gap-2 px-2 py-1.5 rounded hover:bg-[var(--color-bg-tertiary)] transition-colors">
                  <div class="relative flex-shrink-0">
                    <Avatar
                      name={participant.user.name ?? 'Unknown'}
                      avatarUrl={participant.user.avatarUrl}
                      isAI={participant.type === 'AI'}
                      size="sm"
                    />
                    <span
                      class="absolute -bottom-0.5 -right-0.5 w-2.5 h-2.5 rounded-full border-2 border-[var(--color-bg-secondary)] {onlineUsers.has(participant.user.id)
                        ? 'bg-[var(--color-success-500)]'
                        : 'bg-[var(--color-neutral-400)]'}"
                    ></span>
                  </div>
                  <span class="text-sm text-[var(--color-text-primary)] truncate flex-1">
                    {participant.user.name ?? 'Unknown'}{#if participant.user.id === currentUserId} <span class="text-[var(--color-text-tertiary)]">(you)</span>{/if}
                  </span>
                  {#if participant.type === 'AI'}
                    <Icon name="bot" size="xs" class="text-[var(--color-accent-primary)]" />
                  {/if}
                  {#if selectedDm
                    ? participant.user.id === currentUserId
                    : (!isOwnerRole(participant.role) && (isConversationOwner || participant.user.id === currentUserId))}
                    <Button
                      variant="unstyled"
                      class="absolute right-1 top-1/2 -translate-y-1/2
                        w-5 h-5 flex items-center justify-center rounded
                        text-[var(--color-text-tertiary)] hover:text-[var(--color-error-500)]
                        hover:bg-[var(--color-error-50)] transition-colors"
                      title={participant.user.id === currentUserId ? (selectedDm ? 'Leave conversation' : 'Leave channel') : 'Remove member'}
                      onclick={(e) => { e.stopPropagation(); handleRemoveParticipant(participant); }}
                    >
                      <Icon name="x" size="xs" />
                    </Button>
                  {/if}
                </div>
              {/each}
              {#if activeParticipants.length === 0}
                <p class="text-xs text-[var(--color-text-tertiary)] text-center py-2">
                  No participants
                </p>
              {/if}
            </div>
          </CollapsibleGroup>

          <!-- TraceGraphSection (Primary Flow + Related Items) -->
          {#if selectedConversation}
            <TraceGraphSection
              entityType="Conversation"
              entityId={selectedConversation.id}
              {projectId}
            />
          {/if}
        </div>
      {/snippet}
    </SidebarDetailLayout>
  </PageBody>
</PageShell>
