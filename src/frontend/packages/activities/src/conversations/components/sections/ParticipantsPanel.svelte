<!-- Section: ParticipantsPanel — Conversations > Channel/Forum/Topic/Global, Projects > Conversations -->
<script lang="ts">
  import MemberHistoryIndicator from '../idioms/MemberHistoryIndicator.svelte';
  import MemberParticipantActions from '../idioms/MemberParticipantActions.svelte';
  import { getMessageAuthor } from '../../utils/message-author';
  import { onDestroy, untrack } from 'svelte';
  import { Icon, Button, SearchField, Spinner } from '@sddp/ui';
  import { CollapsibleGroup, Avatar, formatRelativeTime, toast } from '@sddp/shell';
  import type {
    ChannelDetail,
    Message,
    ConversationMember,
    ChannelSummary,
    LinkedRequirement,
  } from '../../types';
  import { subscribeConversation, getConversationStoreState } from '../../stores';
  import {
    createConversationService,
    type InvitableUser,
  } from '../../services/ConversationService';
  import { getConversationHubService } from '../../services/ConversationHubService';
  import type { createProjectParticipantExtension } from '../../../projects/stores/project-participant-extension.svelte';

  interface Props {
    channel: ChannelDetail | null;
    currentUserId?: string | null;
    tenantId?: string;
    projectId?: string;
    conversationId?: string;
    memberExtension?: ReturnType<typeof createProjectParticipantExtension>;
    pinnedMessages?: Message[];
    relatedTopics?: ChannelSummary[];
    linkedRequirements?: LinkedRequirement[];
    isDirectMessage?: boolean;
    readonly?: boolean;
    onParticipantClick?: (participant: ConversationMember) => void;
    onRemoveMember?: (participant: ConversationMember) => void;
    onPinnedMessageClick?: (message: Message) => void;
    onRelatedTopicClick?: (topic: ChannelSummary) => void;
    onLinkedRequirementClick?: (requirement: LinkedRequirement) => void;
    onSendInvitation?: (userId: string, displayName: string) => Promise<void>;
    class?: string;
  }

  let {
    channel,
    currentUserId = null,
    tenantId = '',
    projectId = '',
    conversationId = '',
    memberExtension,
    pinnedMessages = [],
    relatedTopics = [],
    linkedRequirements = [],
    isDirectMessage: isDirectMessageProp = false,
    readonly: isReadOnly = false,
    onParticipantClick,
    onRemoveMember,
    onPinnedMessageClick,
    onRelatedTopicClick,
    onLinkedRequirementClick,
    onSendInvitation,
    class: className = '',
  }: Props = $props();

  let participantsExpanded = $state(true);
  let pinnedExpanded = $state(true);
  let relatedExpanded = $state(false);
  let requirementsExpanded = $state(true);

  // Inline invite search state
  let showInviteSearch = $state(false);
  let inviteSearchQuery = $state('');
  let inviteSearchResults = $state<InvitableUser[]>([]);
  let inviteSearching = $state(false);
  let sendingUserId = $state<string | null>(null);
  let searchDebounceTimer: ReturnType<typeof setTimeout> | null = null;

  const participants = $derived(channel?.participants ?? []);

  function isOwnerRole(role: string | null | undefined): boolean {
    return (role ?? '').toLowerCase() === 'owner';
  }

  /** Whether current user is the Owner of the selected conversation (can remove members) */
  const isConversationOwner = $derived(
    participants.some((p) => p.user.id === currentUserId && isOwnerRole(p.role))
  );

  // Show invite action if owner AND has tenantId+conversationId (can search invitable users)
  const canInviteUsers = $derived(isConversationOwner && !!tenantId && !!conversationId);
  const canManageMembers = $derived(!isReadOnly && !!memberExtension?.canManage);
  const canInvite = $derived(!isReadOnly && (canInviteUsers || canManageMembers));
  const additionalParticipants = $derived(memberExtension?.participants ?? []);
  const inviteCandidates = $derived([
    ...(canInviteUsers
      ? inviteSearchResults.map((user) => ({
          key: `user:${user.id}`,
          displayName: user.displayName,
          subtitle: user.email,
          isAi: user.isAi,
          select: () => handleSelectInviteUser(user),
        }))
      : []),
    ...(canManageMembers
      ? (memberExtension?.candidates(inviteSearchQuery) ?? []).map((member) => ({
          key: `member:${member.id}`,
          displayName: member.displayName,
          subtitle: 'AI',
          isAi: false,
          select: () => handleAddMember(member.id, member.displayName),
        }))
      : []),
  ]);
  let inviteRequest = 0;
  let disposed = false;
  onDestroy(() => {
    disposed = true;
  });
  const inviteScope = $derived(
    JSON.stringify([tenantId, projectId, currentUserId, conversationId])
  );

  // Online users from ConversationHub presence tracking
  let onlineUsers = $state<Set<string>>(getConversationStoreState().onlineUsers);

  $effect(() => {
    const unsubscribe = subscribeConversation((state) => {
      onlineUsers = state.onlineUsers;
    });
    return unsubscribe;
  });

  function isOnline(userId: string): boolean {
    return onlineUsers.has(userId);
  }

  function formatTimeAgo(dateStr: string): string {
    return formatRelativeTime(dateStr, undefined, { locale: 'en' });
  }

  // Inline invite search handlers
  function toggleInviteSearch() {
    showInviteSearch = !showInviteSearch;
    if (showInviteSearch) {
      inviteSearchQuery = '';
      inviteSearchResults = [];
      sendingUserId = null;
      if (memberExtension && !memberExtension.loading) void memberExtension.refresh();
    }
  }

  function handleInviteSearchInput(query: string) {
    if (disposed || !showInviteSearch) return;
    inviteSearchQuery = query;
    const request = ++inviteRequest;
    if (searchDebounceTimer) clearTimeout(searchDebounceTimer);
    inviteSearchResults = [];
    inviteSearching = false;
    if (query.trim().length < 2 || !canInviteUsers) return;
    inviteSearching = true;
    searchDebounceTimer = setTimeout(() => {
      untrack(() => searchInvitableUsers(query.trim(), request));
    }, 300);
  }

  async function searchInvitableUsers(query: string, request: number) {
    if (disposed || !tenantId || !conversationId || request !== inviteRequest) return;
    const scope = inviteScope;
    const isCurrent = () => !disposed && request === inviteRequest && scope === inviteScope;
    try {
      const service = createConversationService(tenantId, projectId);
      const users = await service.getInvitableUsers(conversationId, query);
      if (isCurrent()) inviteSearchResults = users;
    } catch {
      if (isCurrent()) inviteSearchResults = [];
    } finally {
      if (isCurrent()) inviteSearching = false;
    }
  }

  async function handleSelectInviteUser(user: InvitableUser) {
    if (sendingUserId || !conversationId || !canInviteUsers || isReadOnly) return;
    const scope = inviteScope;
    const isCurrent = () => !disposed && scope === inviteScope;
    sendingUserId = `user:${user.id}`;
    try {
      if (onSendInvitation) {
        await onSendInvitation(user.id, user.displayName);
      } else {
        const hubService = getConversationHubService();
        const convName = channel?.topic ?? 'Channel';
        await hubService.sendInvitation(conversationId, user.id, convName);
      }
      if (!isCurrent()) return;
      toast.success(`Invitation sent to ${user.displayName}`);
      inviteSearchResults = inviteSearchResults.filter((u) => u.id !== user.id);
    } catch {
      if (isCurrent()) toast.error(`Failed to send invitation to ${user.displayName}`);
    } finally {
      if (isCurrent()) sendingUserId = null;
    }
  }

  async function handleAddMember(memberId: string, displayName: string) {
    if (sendingUserId || !canManageMembers) return;
    const scope = inviteScope;
    sendingUserId = `member:${memberId}`;
    try {
      if ((await memberExtension?.add(memberId)) && !disposed && scope === inviteScope)
        toast.success(`${displayName} added`);
    } finally {
      if (!disposed && scope === inviteScope) sendingUserId = null;
    }
  }

  async function handleRemoveMember(memberId: string, displayName: string) {
    if (!canManageMembers) return;
    const scope = inviteScope;
    if ((await memberExtension?.remove(memberId)) && !disposed && scope === inviteScope)
      toast.success(`${displayName} removed`);
  }

  $effect(() => {
    void inviteScope;
    untrack(() => {
      inviteRequest++;
      clearTimeout(searchDebounceTimer ?? undefined);
      showInviteSearch = false;
      inviteSearchQuery = '';
      inviteSearchResults = [];
      inviteSearching = false;
      sendingUserId = null;
    });
    return () => {
      inviteRequest++;
      clearTimeout(searchDebounceTimer ?? undefined);
    };
  });
</script>

<aside class="participants-panel flex flex-col h-full bg-[var(--color-bg-secondary)] {className}">
  {#if channel}
    <div class="flex-1 overflow-y-auto">
      <!-- Participants Section -->
      <CollapsibleGroup
        title="Participants ({participants.length + additionalParticipants.length})"
        allowOverflow
        actions={canInvite
          ? [{ id: 'invite', icon: 'plus', label: 'Invite Members', onClick: toggleInviteSearch }]
          : []}
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
            {/if}
            {#if inviteCandidates.length > 0}
              <div class="mt-1 max-h-[160px] overflow-y-auto">
                {#each inviteCandidates as user (user.key)}
                  <Button
                    variant="unstyled"
                    class="w-full flex items-center gap-2 px-2 py-1.5 rounded text-left
                    hover:bg-[var(--color-bg-tertiary)] transition-colors
                    {sendingUserId === user.key ? 'opacity-50 pointer-events-none' : ''}"
                    onclick={user.select}
                    disabled={sendingUserId === user.key}
                  >
                    <Avatar name={user.displayName} isAI={user.isAi} size="sm" />
                    <div class="flex-1 min-w-0">
                      <span class="text-sm text-[var(--color-text-primary)] truncate block"
                        >{user.displayName}</span
                      >
                      <span class="text-xs text-[var(--color-text-tertiary)] truncate block"
                        >{user.subtitle}</span
                      >
                    </div>
                    {#if sendingUserId === user.key}
                      <Spinner size="sm" />
                    {/if}
                  </Button>
                {/each}
              </div>
            {:else if inviteSearchQuery.length >= 2 && !inviteSearching}
              <div class="text-xs text-[var(--color-text-tertiary)] mt-2 mb-1 text-center">
                No users found
              </div>
            {/if}
          </div>
        {/if}

        {#if memberExtension?.error}
          <div role="alert" class="px-2 py-2 text-xs text-[var(--color-error-500)]">
            {memberExtension.error}
            <Button variant="ghost" size="sm" onclick={() => memberExtension?.refresh()}
              >Retry participants</Button
            >
          </div>
        {/if}
        <div class="px-2 py-2" role="list" aria-label="Conversation participants">
          {#each participants as participant (participant.id)}
            <div class="group/participant relative" role="listitem">
              <Button
                variant="unstyled"
                class="w-full flex items-center gap-2 px-2 py-1.5 rounded
                hover:bg-[var(--color-bg-tertiary)] transition-colors text-left"
                onclick={() => onParticipantClick?.(participant)}
              >
                <div class="relative flex-shrink-0">
                  <Avatar
                    name={participant.user.name ?? 'Unknown'}
                    avatarUrl={participant.user.avatarUrl}
                    isAI={participant.type === 'AI'}
                    size="sm"
                  />
                  <span
                    class="absolute -bottom-0.5 -right-0.5 w-2.5 h-2.5 rounded-full border-2 border-[var(--color-bg-secondary)] {isOnline(
                      participant.user.id
                    )
                      ? 'bg-[var(--color-success-500)]'
                      : 'bg-[var(--color-neutral-400)]'}"
                  ></span>
                </div>
                <span class="text-sm text-[var(--color-text-primary)] truncate flex-1">
                  {participant.user.name ?? 'Unknown'}{#if participant.user.id === currentUserId}
                    <span class="text-[var(--color-text-tertiary)]">(you)</span>{/if}
                </span>
                {#if participant.type === 'AI'}
                  <Icon name="bot" size="xs" class="text-[var(--color-accent-text)]" />
                {/if}
              </Button>
              {#if !isReadOnly && onRemoveMember && (isDirectMessageProp ? participant.user.id === currentUserId : !isOwnerRole(participant.role) && (isConversationOwner || participant.user.id === currentUserId))}
                <Button
                  variant="unstyled"
                  class="absolute right-1 top-1/2 -translate-y-1/2 opacity-0 pointer-events-none
                  group-hover/participant:opacity-100 group-hover/participant:pointer-events-auto
                  focus-visible:opacity-100 focus-visible:pointer-events-auto
                  [@media(hover:none)]:opacity-100 [@media(hover:none)]:pointer-events-auto
                  w-5 h-5 flex items-center justify-center rounded
                  text-[var(--color-text-tertiary)] hover:text-[var(--color-error-500)]
                  hover:bg-[var(--color-error-50)] transition-all"
                  title={participant.user.id === currentUserId
                    ? 'Leave channel'
                    : 'Remove from channel'}
                  onclick={(e) => {
                    e.stopPropagation();
                    onRemoveMember?.(participant);
                  }}
                >
                  <Icon name="x" size="xs" />
                </Button>
              {/if}
            </div>
          {/each}
          {#each additionalParticipants as member (member.id)}
            <div
              role="listitem"
              class="group/participant relative flex items-center gap-2 pl-2 pr-7 py-1.5 rounded hover:bg-[var(--color-bg-tertiary)] transition-colors"
            >
              <div class="relative flex-shrink-0">
                <Avatar name={member.displayName} size="sm" />
                <MemberHistoryIndicator history={member.history} />
              </div>
              <div class="flex-1 min-w-0">
                <span class="block text-sm text-[var(--color-text-primary)] truncate"
                  >{member.displayName}</span
                >
              </div>
              <span class="text-xs text-[var(--color-text-tertiary)]">AI</span>
              {#if canManageMembers}
                <div class="absolute inset-y-0 right-1 flex items-center">
                  <MemberParticipantActions
                    displayName={member.displayName}
                    prepare={!member.history ||
                    ['failed', 'unprepared'].includes(member.history.status)
                      ? () => memberExtension?.retryPreparation(member.id)
                      : undefined}
                    prepareLabel={member.history?.status === 'failed'
                      ? 'Retry history preparation'
                      : 'Prepare history'}
                    remove={() => handleRemoveMember(member.id, member.displayName)}
                  />
                </div>
              {/if}
            </div>
          {/each}
        </div>
      </CollapsibleGroup>

      <!-- Pinned Messages Section -->
      {#if pinnedMessages.length > 0}
        <CollapsibleGroup
          title="Pinned"
          icon="pin"
          iconClass="text-[var(--color-warning-500)]"
          badge={pinnedMessages.length}
          expanded={pinnedExpanded}
          onToggle={() => (pinnedExpanded = !pinnedExpanded)}
        >
          <div class="px-2 pt-2 pb-2 space-y-2">
            {#each pinnedMessages.slice(0, 3) as message (message.id)}
              <Button
                variant="unstyled"
                class="w-full p-2 rounded text-left
                  bg-[var(--color-bg-primary)] hover:bg-[var(--color-bg-tertiary)]
                  border border-[var(--color-border)] transition-colors"
                onclick={() => onPinnedMessageClick?.(message)}
              >
                <div class="flex items-center gap-1 mb-1">
                  <Icon name="pin" size="xs" class="text-[var(--color-warning-500)]" />
                  <span class="text-xs font-medium text-[var(--color-text-secondary)]">
                    {getMessageAuthor(message)?.name ?? 'Unknown'}
                  </span>
                </div>
                <p class="text-xs text-[var(--color-text-primary)] line-clamp-2">
                  {message.content}
                </p>
              </Button>
            {/each}

            {#if pinnedMessages.length > 3}
              <Button
                variant="unstyled"
                class="w-full py-1 text-xs text-[var(--color-accent-text)]
                  hover:underline"
              >
                View all {pinnedMessages.length} pinned messages
              </Button>
            {/if}
          </div>
        </CollapsibleGroup>
      {/if}

      <!-- Related Requirements Section (only show when data exists) -->
      {#if linkedRequirements.length > 0}
        <CollapsibleGroup
          title="Related Requirements"
          icon="clipboard-list"
          badge={linkedRequirements.length}
          expanded={requirementsExpanded}
          onToggle={() => (requirementsExpanded = !requirementsExpanded)}
        >
          <div class="px-2 pt-2 pb-2">
            <div class="space-y-1">
              {#each linkedRequirements as req (req.id)}
                <Button
                  variant="unstyled"
                  class="w-full flex flex-col gap-0.5 px-2 py-1.5 rounded text-left
                  hover:bg-[var(--color-bg-tertiary)] transition-colors"
                  onclick={() => onLinkedRequirementClick?.(req)}
                >
                  <div class="flex items-center gap-2">
                    <Icon
                      name="clipboard-list"
                      size="sm"
                      class="text-[var(--color-text-tertiary)] flex-shrink-0"
                    />
                    <span class="text-sm text-[var(--color-text-primary)] truncate flex-1">
                      [{req.code}] {req.title}
                    </span>
                  </div>
                  <span class="text-[0.625rem] text-[var(--color-text-muted)] pl-6">
                    Linked {formatTimeAgo(req.linkedAt)}
                  </span>
                </Button>
              {/each}
            </div>
          </div>
        </CollapsibleGroup>
      {/if}

      <!-- Related Conversations Section (only show when data exists) -->
      {#if relatedTopics.length > 0}
        <CollapsibleGroup
          title="Related Topics"
          icon="message-square"
          expanded={relatedExpanded}
          onToggle={() => (relatedExpanded = !relatedExpanded)}
        >
          <div class="px-2 pt-2 pb-2">
            <div class="space-y-1">
              {#each relatedTopics as relatedTopic (relatedTopic.id)}
                <Button
                  variant="unstyled"
                  class="w-full flex items-center gap-2 px-2 py-1.5 rounded text-left
                  hover:bg-[var(--color-bg-tertiary)] transition-colors"
                  onclick={() => onRelatedTopicClick?.(relatedTopic)}
                >
                  <Icon name="message-square" size="sm" class="text-[var(--color-text-tertiary)]" />
                  <span class="text-sm text-[var(--color-text-primary)] truncate flex-1">
                    {relatedTopic.topic}
                  </span>
                </Button>
              {/each}
            </div>

            <Button variant="ghost" size="sm" class="w-full mt-2 justify-start">
              <Icon name="link" size="sm" class="mr-2" />
              Add Relationship
            </Button>
          </div>
        </CollapsibleGroup>
      {/if}
    </div>
  {:else}
    <!-- No Conversation Selected -->
    <div class="flex flex-col items-center justify-center h-full text-center p-4">
      <Icon name="user" size="lg" class="text-[var(--color-text-tertiary)] mb-2" />
      <p class="text-sm text-[var(--color-text-tertiary)]">
        Select a conversation to see participants
      </p>
    </div>
  {/if}
</aside>
