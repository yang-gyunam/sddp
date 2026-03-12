<!-- Section: ParticipantsPanel — Conversations > Channel/Forum/Topic/Global, Projects > Conversations -->
<script lang="ts">
  import { untrack } from 'svelte';
  import { Icon, Button, SearchField, Spinner } from '@sddp/ui';
  import { CollapsibleGroup, Avatar, formatRelativeTime, toast } from '@sddp/shell';
  import type { ChannelDetail, Message, ConversationMember, ChannelSummary, LinkedRequirement } from '../../types';
  import { subscribeConversation, getConversationStoreState } from '../../stores';
  import { getConversationService, type InvitableUser } from '../../services/ConversationService';
  import { getConversationHubService } from '../../services/ConversationHubService';

  interface Props {
    channel: ChannelDetail | null;
    currentUserId?: string | null;
    tenantId?: string;
    conversationId?: string;
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
    conversationId = '',
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
  const canInvite = $derived(isConversationOwner && !!tenantId && !!conversationId);

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
    if (!tenantId || !conversationId) return;
    inviteSearching = true;
    try {
      const service = getConversationService();
      service.setContext(tenantId, '');
      inviteSearchResults = await service.getInvitableUsers(conversationId, query);
    } catch {
      inviteSearchResults = [];
    } finally {
      inviteSearching = false;
    }
  }

  async function handleSelectInviteUser(user: InvitableUser) {
    if (sendingUserId || !conversationId) return;
    sendingUserId = user.id;
    try {
      if (onSendInvitation) {
        await onSendInvitation(user.id, user.displayName);
      } else {
        const hubService = getConversationHubService();
        const convName = channel?.topic ?? 'Channel';
        await hubService.sendInvitation(conversationId, user.id, convName);
      }
      toast.success(`Invitation sent to ${user.displayName}`);
      inviteSearchResults = inviteSearchResults.filter((u) => u.id !== user.id);
    } catch {
      toast.error(`Failed to send invitation to ${user.displayName}`);
    } finally {
      sendingUserId = null;
    }
  }

  // Close invite search when conversation changes
  let prevConversationId = $state('');
  $effect(() => {
    if (conversationId && conversationId !== prevConversationId) {
      prevConversationId = conversationId;
      if (showInviteSearch) showInviteSearch = false;
    }
  });
</script>

<aside
  class="participants-panel flex flex-col h-full bg-[var(--color-bg-secondary)] {className}"
>
  {#if channel}
    <div class="flex-1 overflow-y-auto">
      <!-- Participants Section -->
      <CollapsibleGroup
        title="Participants ({participants.length})"
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
        {#each participants as participant (participant.id)}
          <div class="group relative">
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
                  class="absolute -bottom-0.5 -right-0.5 w-2.5 h-2.5 rounded-full border-2 border-[var(--color-bg-secondary)] {isOnline(participant.user.id)
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
            </Button>
            {#if !isReadOnly && onRemoveMember && (isDirectMessageProp ? participant.user.id === currentUserId : (!isOwnerRole(participant.role) && (isConversationOwner || participant.user.id === currentUserId)))}
              <Button
                variant="unstyled"
                class="absolute right-1 top-1/2 -translate-y-1/2 opacity-0 group-hover:opacity-100
                  w-5 h-5 flex items-center justify-center rounded
                  text-[var(--color-text-tertiary)] hover:text-[var(--color-error-500)]
                  hover:bg-[var(--color-error-50)] transition-all"
                title={participant.user.id === currentUserId ? 'Leave channel' : 'Remove from channel'}
                onclick={(e) => { e.stopPropagation(); onRemoveMember?.(participant); }}
              >
                <Icon name="x" size="xs" />
              </Button>
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
                    {message.sender?.name ?? 'Unknown'}
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
                class="w-full py-1 text-xs text-[var(--color-accent-primary)]
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
                  <Icon name="clipboard-list" size="sm" class="text-[var(--color-text-tertiary)] flex-shrink-0" />
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

          <Button
            variant="ghost"
            size="sm"
            class="w-full mt-2 justify-start"
          >
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
