<!-- Section: ChannelView — Conversations > Channel/DM/Global, Projects > Conversations -->
<script lang="ts">
  import { untrack } from 'svelte';
  import { Icon, Button, IconButton, Spinner, ResizeHandle } from '@sddp/ui';
  import { PageBody, PageShell, toast, RouterService } from '@sddp/shell';
  import { tabActions } from '@sddp/shell/stores';
  import { getConversationHubService } from '../../services';
  import MessageBubble from '../idioms/MessageBubble.svelte';
  import MessageInput from '../idioms/MessageInput.svelte';
  import { ConversationHeader } from '../idioms';
  import ParticipantsPanel from './ParticipantsPanel.svelte';
  import { getAuthState } from '@sddp/shell/auth';
  import { getConversationService } from '../../services';
  import type { Message, CreateMessageRequest, ChannelDetail, ConversationMember, LinkedRequirement } from '../../types';
  import type { ChannelEntryStatus } from '../../types';
  import RequirementForm from '../../../requirements/components/idioms/RequirementForm.svelte';
  import type { CreateRequirementRequest, RequirementLevel } from '../../../requirements/types';
  import { getRequirementService } from '../../../requirements/services';
  import {
    getConversationViewMode,
    subscribeConversationView,
    removeConversation,
    removeDirectMessage,
    setSelectedConversation,
    type ConversationViewMode,
  } from '../../stores';
  import { loadGlobalConversationsSidebar } from '../../actions';
  import type { ConversationMessageDto } from '../../services';

  interface Props {
    conversationId: string;
    conversationName: string;
    conversationDescription?: string;
    headerIcon?: string;
    /**
     * DTO. domain ConversationType(Channel/Forum/DM).
     * - 'channel': channel DTO(MessagesPage)
     * - 'conversation': DTO(ConversationMessagesPage)
     * API (/conversations/{id}/messages).
     */
    sourceType?: 'channel' | 'conversation';
    /** Conversation type hint for enabling status controls on DMs */
    conversationType?: 'Channel' | 'Forum' | 'DirectMessage';
    tenantId?: string;
    projectId?: string;
    allowRequirementExtract?: boolean;
    tabId?: string;
    groupId?: string;
    channelStatus?: ChannelEntryStatus | null;
    readonly?: boolean;
    hideParticipantPanel?: boolean;
    /** Hide internal header (for Pattern B layout where header is rendered externally) */
    showHeader?: boolean;
    onClose?: () => void;
    onStatusChange?: (status: ChannelEntryStatus) => void;
    onMembersChanged?: (conversationId: string, members: ConversationMember[]) => void;
    onSendInvitation?: (userId: string, displayName: string) => Promise<void>;
    class?: string;
  }

  let {
    conversationId,
    conversationName,
    conversationDescription,
    headerIcon = 'hash',
    sourceType = 'channel',
    conversationType,
    tenantId,
    projectId,
    allowRequirementExtract = false,
    tabId,
    groupId = '',
    channelStatus: propChannelStatus = null,
    readonly: isReadOnlyProp = false,
    hideParticipantPanel = false,
    showHeader: showHeaderProp = true,
    onClose,
    onStatusChange,
    onMembersChanged,
    onSendInvitation,
    class: className = '',
  }: Props = $props();

  const draftKey = $derived(tabId ? `${tabId}:${conversationId}` : conversationId);

  const service = getConversationService();
  const hubService = getConversationHubService();
  let messages = $state<Message[]>([]);
  let loading = $state(false);
  let sending = $state(false);
  let error = $state<string | null>(null);
  let nextCursor = $state<string | null>(null);
  let hasMore = $state(false);
  let currentUserId = $state<string | null>(null);
  let messagesContainer: HTMLDivElement;
  let viewMode = $state<ConversationViewMode>(getConversationViewMode('chat'));
  let hubConnected = $state(false);
  let hubError = $state<string | null>(null);
  let extractingMessage = $state<Message | null>(null);
  let extractInitialData = $state<{
    code?: string;
    title?: string;
    description?: string;
    level?: RequirementLevel;
  } | null>(null);
  let extractLoading = $state(false);
  let extractError = $state<string | null>(null);


  // Right panel (Participants) resize state
  let participantsPanelWidth = $state(224); // default w-56 = 224px
  const minParticipantsWidth = 180;
  const maxParticipantsWidth = 400;
  let isParticipantsResizing = $state(false);
  let participantsRafId: number | null = null;
  let cleanupResize: (() => void) | null = null;

  function handleParticipantsResizeStart(e: PointerEvent) {
    e.preventDefault();
    isParticipantsResizing = true;
    const startX = e.clientX;
    const startWidth = participantsPanelWidth;

    function onPointerMove(moveEvent: PointerEvent) {
      if (participantsRafId !== null) {
        cancelAnimationFrame(participantsRafId);
      }
      participantsRafId = requestAnimationFrame(() => {
        // Reverse direction: drag left = increase width
        const delta = startX - moveEvent.clientX;
        participantsPanelWidth = Math.min(maxParticipantsWidth, Math.max(minParticipantsWidth, startWidth + delta));
        participantsRafId = null;
      });
    }

    function onPointerUp() {
      if (participantsRafId !== null) {
        cancelAnimationFrame(participantsRafId);
        participantsRafId = null;
      }
      isParticipantsResizing = false;
      document.removeEventListener('pointermove', onPointerMove);
      document.removeEventListener('pointerup', onPointerUp);
      cleanupResize = null;
    }

    document.addEventListener('pointermove', onPointerMove);
    document.addEventListener('pointerup', onPointerUp);

    // Store cleanup function for unmount during active resize
    cleanupResize = () => {
      if (participantsRafId !== null) {
        cancelAnimationFrame(participantsRafId);
        participantsRafId = null;
      }
      isParticipantsResizing = false;
      document.removeEventListener('pointermove', onPointerMove);
      document.removeEventListener('pointerup', onPointerUp);
    };
  }

  // Clean up resize listeners if component unmounts during active resize
  $effect(() => {
    return () => {
      cleanupResize?.();
      cleanupResize = null;
    };
  });

  let channelDetail = $state<ChannelDetail | null>(null);
  let linkedRequirements = $state<LinkedRequirement[]>([]);
  let channelStatus = $state<ChannelEntryStatus | null>(null);
  // Resolved name/description from API (overrides prop when loaded)
  let resolvedName = $state<string | null>(null);
  let resolvedDescription = $state<string | null>(null);
  const displayName = $derived(resolvedName || conversationName);
  const displayDescription = $derived(resolvedDescription || conversationDescription);
  const creatorName = $derived.by(() => {
    const participants = channelDetail?.participants ?? [];
    if (participants.length === 0) return null;

    const owner = participants.find((member) => (member.role ?? '').toLowerCase() === 'owner');
    if (owner?.user?.name) return owner.user.name;

    const earliest = [...participants].sort(
      (a, b) => new Date(a.joinedAt).getTime() - new Date(b.joinedAt).getTime()
    )[0];
    return earliest?.user?.name ?? null;
  });
  let statusLoading = $state(false);
  let suppressClosedToastUntil = $state(0);

  // Sync status from props when explicitly provided.
  // Do not reset to null on missing prop; API hydration can set the latest status afterward.
  $effect(() => {
    if (propChannelStatus != null) {
      channelStatus = propChannelStatus;
      return;
    }

    if ((conversationType === 'DirectMessage' || conversationType === 'Channel') && channelStatus == null) {
      channelStatus = 'Active';
    }
  });
  let currentUserRole = $state<string>('Member');

  // Deduplicate messages by ID (real-time hub can deliver duplicates)
  const uniqueMessages = $derived(
    messages.filter((msg, idx, arr) => arr.findIndex((m) => m.id === msg.id) === idx)
  );

  /** Whether status controls should be shown (Channels via sourceType or conversationType, DMs via conversationType) */
  const hasStatusControls = $derived(
    sourceType === 'conversation' || conversationType === 'Channel' || conversationType === 'DirectMessage'
  );
  const isDirectMessage = $derived(conversationType === 'DirectMessage');
  const isChannelReadOnly = $derived(
    isReadOnlyProp || (hasStatusControls && channelStatus != null && channelStatus !== 'Active')
  );
  const canConclude = $derived(
    hasStatusControls
      && channelStatus === 'Active'
      && (isDirectMessage || currentUserRole.toLowerCase() === 'owner')
  );
  const canCloseFromHeader = $derived(
    !!onClose || !!tabId
  );

  function handleHeaderClose(): void {
    if (onClose) {
      onClose();
      return;
    }

    if (tabId) {
      tabActions.closeTab(tabId, groupId || undefined);
    }
  }

  async function handleConclude(): Promise<void> {
    statusLoading = true;
    try {
      await ensureContext();
      const isDM = conversationType === 'DirectMessage';
      const result = isDM
        ? await service.concludeDM(conversationId)
        : await service.concludeChannel(conversationId);
      channelStatus = result.channelStatus ?? 'Concluded';
      onStatusChange?.('Concluded');
      toast.success(isDM ? 'Conversation concluded' : 'Channel concluded');
      suppressClosedToastUntil = Date.now() + 5000;

      if (isDM) {
        if (typeof window !== 'undefined') {
          window.dispatchEvent(
            new CustomEvent('sddp:dm-concluded', {
              detail: { conversationId },
            })
          );
        }
        // 1. Sidebar store DM +
        removeDirectMessage(conversationId);
        setSelectedConversation(null);
        // 2.
        if (tabId) {
          tabActions.closeTab(tabId, groupId || undefined);
        }
        // 3. onClose (GlobalConversationsPage inline view)
        onClose?.();
        // 4. DM
        const resolvedTenantId = tenantId ?? getAuthState().user?.tenantId ?? '';
        if (resolvedTenantId) {
          loadGlobalConversationsSidebar(resolvedTenantId);
        }
      }
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Failed to conclude');
    } finally {
      statusLoading = false;
    }
  }

  async function handleRemoveMember(member: ConversationMember): Promise<void> {
    const isSelf = member.user.id === currentUserId;
    try {
      await ensureContext();
      await service.removeConversationMember(conversationId, member.user.id);

      if (isSelf) {
        // Self-removal (Leave): close tab + remove from sidebar
        toast.info(isDirectMessage ? 'Left direct message' : `Left #${displayName}`);
        if (isDirectMessage) {
          removeDirectMessage(conversationId);
          // Notify other views (ProjectConversationsPage) to update their local DM list
          if (typeof window !== 'undefined') {
            window.dispatchEvent(
              new CustomEvent('sddp:dm-concluded', {
                detail: { conversationId, origin: 'self-leave' },
              })
            );
          }
        } else {
          removeConversation(conversationId);
        }
        setSelectedConversation(null);
        if (tabId) {
          tabActions.closeTab(tabId, groupId || undefined);
        }
        onClose?.();
      } else {
        toast.success(`${member.user.name} was removed from channel`);
        await loadMembers();
      }
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Failed to remove member');
    }
  }

  async function handleEditMessage(message: Message, newContent: string): Promise<void> {
    try {
      await ensureContext();
      const updated = await service.editConversationMessage(conversationId, message.id, newContent);
      const mapped = mapConversationMessage(updated);
      messages = messages.map((m) => (m.id === mapped.id ? mapped : m));
      toast.success('Message edited');
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Failed to edit message');
    }
  }

  async function handleDeleteMessage(message: Message): Promise<void> {
    try {
      await ensureContext();
      await service.deleteConversationMessage(conversationId, message.id);
      messages = messages.filter((m) => m.id !== message.id);
      toast.success('Message deleted');
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Failed to delete message');
    }
  }

  function scrollToBottomIfNear(): void {
    if (!messagesContainer) return;
    const { scrollHeight, scrollTop, clientHeight } = messagesContainer;
    const isNearBottom = scrollHeight - scrollTop - clientHeight < 80;
    if (isNearBottom) {
      requestAnimationFrame(() => {
        messagesContainer?.scrollTo({ top: messagesContainer.scrollHeight, behavior: 'smooth' });
      });
    }
  }

  $effect(() => {
    const unsubscribe = subscribeConversationView((state) => {
      viewMode = state.chatMode;
    });
    return unsubscribe;
  });

  function handleSendMessage(message: CreateMessageRequest) {
    sendMessage(message);
  }

  function mapConversationMessage(message: ConversationMessageDto): Message {
    return {
      id: message.id,
      conversationId: message.conversationId,
      sender: message.sender,
      type: message.type,
      content: message.content,
      references: message.references ?? [],
      replyToId: message.replyToId ?? null,
      createdAt: message.createdAt,
      isEdited: message.isEdited,
    };
  }

  function mapChannelMessages(items: Message[]): Message[] {
    return items.map((message) => ({
      ...message,
      references: message.references ?? [],
    }));
  }

  function normalizeMessage(message: Message): Message {
    return {
      ...message,
      references: message.references ?? [],
      replyToId: message.replyToId ?? null,
    };
  }

  const canExtractRequirement = $derived(
    allowRequirementExtract && sourceType === 'channel' && !!projectId
  );

  function generateRequirementCode(seed: string): string {
    const cleaned = seed.replace(/[^a-zA-Z0-9]/g, '').toUpperCase();
    const short = cleaned.slice(0, 6) || 'DISC';
    const stamp = new Date().toISOString().slice(2, 10).replace(/-/g, '');
    return `REQ-${short}-${stamp}`;
  }

  function getRequirementTitleFromMessage(message: Message): string {
    const content = message.content?.trim() || '';
    const firstLine = content.split('\n').find((line) => line.trim().length > 0) || '';
    const title = firstLine.trim().slice(0, 80);
    if (title.length > 0) return title;
    return `Requirement from ${displayName}`;
  }

  function handleExtractRequirement(message: Message): void {
    extractingMessage = message;
    extractInitialData = {
      code: generateRequirementCode(conversationId),
      title: getRequirementTitleFromMessage(message),
      description: message.content ?? '',
      level: 'B',
    };
    extractError = null;
  }

  function handleCancelExtract(): void {
    extractingMessage = null;
    extractInitialData = null;
    extractError = null;
  }

  async function handleExtractSubmit(data: CreateRequirementRequest): Promise<void> {
    extractLoading = true;
    extractError = null;
    try {
      const authState = getAuthState();
      const tenant = authState.user?.tenantId;
      if (!tenant || !projectId) {
        throw new Error('Missing project context');
      }

      const requirementService = getRequirementService();
      requirementService.setContext(tenant, projectId);
      const detail = await requirementService.createRequirement(data);

      if (sourceType === 'channel') {
        await requirementService.linkConversation(detail.id, { conversationId });
      }

      toast.success('Requirement created', { title: detail.title });

      if (typeof window !== 'undefined') {
        window.dispatchEvent(
          new CustomEvent('sddp:navigate', {
            detail: {
              type: 'requirement',
              id: detail.id,
              label: detail.title,
            },
          })
        );
      }

      handleCancelExtract();
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to create requirement';
      extractError = message;
      toast.error(message, { title: 'Requirement Extract' });
    } finally {
      extractLoading = false;
    }
  }

  async function ensureContext(): Promise<void> {
    const authState = getAuthState();
    currentUserId = authState.user?.id ?? null;
    const resolvedTenantId = tenantId ?? authState.user?.tenantId ?? '';
    const resolvedProjectId = projectId ?? '';
    if (!resolvedTenantId) {
      throw new Error('Missing tenant ID');
    }
    service.setContext(resolvedTenantId, resolvedProjectId);
  }

  async function loadMessages(reset = false): Promise<void> {
    const targetConversationId = conversationId;
    loading = true;
    error = null;
    try {
      await ensureContext();
      const cursor = reset ? undefined : nextCursor ?? undefined;
      if (sourceType === 'conversation') {
        const page = await service.getConversationMessages(targetConversationId, { cursor, limit: 50 });
        if (targetConversationId !== conversationId) {
          return;
        }
        const batch = page.messages.map(mapConversationMessage).reverse();
        messages = reset ? batch : [...batch, ...messages];
        nextCursor = page.nextCursor;
        hasMore = page.hasMore;
      } else {
        const page = await service.getMessages(targetConversationId, { cursor, limit: 50 });
        if (targetConversationId !== conversationId) {
          return;
        }
        const batch = mapChannelMessages(page.messages).reverse();
        messages = reset ? batch : [...batch, ...messages];
        nextCursor = page.nextCursor;
        hasMore = page.hasMore;
      }
      // Scroll to bottom on initial load (channel selected)
      if (reset) {
        requestAnimationFrame(() => {
          messagesContainer?.scrollTo({ top: messagesContainer.scrollHeight });
        });
      }
    } catch (err) {
      error = err instanceof Error ? err.message : 'Failed to load messages';
    } finally {
      if (targetConversationId === conversationId) {
        loading = false;
      }
    }
  }

  async function sendMessage(message: CreateMessageRequest): Promise<void> {
    sending = true;
    try {
      await ensureContext();
      if (sourceType === 'conversation') {
        const created = await service.postConversationMessage(conversationId, message);
        const mapped = mapConversationMessage(created);
        if (!messages.some((item) => item.id === mapped.id)) {
          messages = [...messages, mapped];
        }
      } else {
        const created = await service.postMessage(conversationId, message);
        if (!messages.some((item) => item.id === created.id)) {
          messages = [...messages, created];
        }
      }

      // Always scroll to bottom for own messages
      requestAnimationFrame(() => {
        messagesContainer?.scrollTo({ top: messagesContainer.scrollHeight, behavior: 'smooth' });
      });
    } catch (err) {
      error = err instanceof Error ? err.message : 'Failed to send message';
    } finally {
      sending = false;
    }
  }

  function handleLoadMore(): void {
    if (loading || !hasMore) return;
    loadMessages(false);
  }

  async function handleTypingStart(): Promise<void> {
    if (!hubConnected) return;
    try {
      await hubService.startTyping(conversationId);
    } catch (err) {
      console.warn('Failed to send typing start:', err);
    }
  }

  async function handleTypingStop(): Promise<void> {
    if (!hubConnected) return;
    try {
      await hubService.stopTyping(conversationId);
    } catch (err) {
      console.warn('Failed to send typing stop:', err);
    }
  }

  async function loadMembers(): Promise<void> {
    const targetConversationId = conversationId;
    try {
      await ensureContext();

      // Fetch actual conversation detail to get the real name and status
      try {
        const conv = await service.getConversationById(targetConversationId);
        if (conv.name) {
          resolvedName = conv.name;
          resolvedDescription = conv.description ?? null;
          // Update tab title to show real conversation name
          if (tabId) {
            tabActions.updateTab(tabId, { title: conv.name });
          }
        }
        // Always sync channel status from API detail to prevent stale/null status after reload.
        if (conv.channelStatus) {
          channelStatus = conv.channelStatus;
        }
      } catch {
        // Fallback: continue with prop name if API fails
      }

      const members = await service.getConversationMembers(targetConversationId);
      if (targetConversationId !== conversationId) {
        return;
      }
      channelDetail = {
        id: conversationId,
        topic: displayName,
        status: 'Active',
        participantCount: members.length,
        messageCount: messages.length,
        createdAt: new Date().toISOString(),
        concludedAt: null,
        participants: members,
      };
      // Extract current user's role
      if (currentUserId) {
        const myMember = members.find((m) => m.user.id === currentUserId);
        currentUserRole = myMember?.role ?? 'Member';
      }
      // Notify parent of member changes
      onMembersChanged?.(targetConversationId, members);
    } catch {
      if (targetConversationId !== conversationId) {
        return;
      }
      channelDetail = null;
    }
  }

  async function loadLinkedRequirements(): Promise<void> {
    const targetConversationId = conversationId;
    try {
      await ensureContext();
      const requirements = await service.getLinkedRequirements(targetConversationId);
      if (targetConversationId !== conversationId) {
        return;
      }
      linkedRequirements = requirements;
    } catch {
      if (targetConversationId !== conversationId) {
        return;
      }
      linkedRequirements = [];
    }
  }

  function handleLinkedRequirementClick(req: LinkedRequirement): void {
    RouterService.navigate(`/requirement/${req.id}`);
  }

  let prevConversationId = $state<string | null>(null);

  $effect(() => {
    if (!conversationId) {
      prevConversationId = null;
      return;
    }
    if (conversationId === prevConversationId) return;
    prevConversationId = conversationId;
    untrack(() => { loadMessages(true); loadMembers(); loadLinkedRequirements(); });
  });

  $effect(() => {
    if (!conversationId) return;
    const unsubscribe = hubService.setCallbacks({
      onNewMessage: (message) => {
        if (message.conversationId !== conversationId) return;
        if (messages.some((item) => item.id === message.id)) return;
        messages = [...messages, normalizeMessage(message)];
        scrollToBottomIfNear();
      },
      onMessageEdited: (message) => {
        if (message.conversationId !== conversationId) return;
        messages = messages.map((item) => (item.id === message.id ? normalizeMessage(message) : item));
      },
      onMessageDeleted: (messageId) => {
        messages = messages.filter((item) => item.id !== messageId);
      },
      onMemberJoined: (event) => {
        if (event.conversationId !== conversationId) return;
        loadMembers();
      },
      onMemberRemoved: (event) => {
        if (event.conversationId !== conversationId) return;
        // If the current user was removed, remove from sidebar, close tab, and notify
        if (currentUserId && event.removedUserId === currentUserId) {
          const isDm = conversationType === 'DirectMessage';
          toast.info(isDm ? `Left ${displayName}` : `You were removed from #${displayName}`);
          removeConversation(conversationId);
          removeDirectMessage(conversationId);
          // Refresh sidebar from backend to ensure consistency
          const resolvedTid = tenantId ?? getAuthState().user?.tenantId ?? '';
          if (resolvedTid) {
            loadGlobalConversationsSidebar(resolvedTid);
          }
          if (tabId) {
            tabActions.closeTab(tabId, groupId || undefined);
          }
          // Notify ProjectConversationsPage to clean up its local DM list
          if (isDm && typeof window !== 'undefined') {
            window.dispatchEvent(
              new CustomEvent('sddp:dm-concluded', {
                detail: { conversationId, origin: 'member-removed' },
              })
            );
          }
          onClose?.();
          return;
        }
        loadMembers();
      },
      onConversationClosed: (event) => {
        if (event.conversationId !== conversationId) return;

        channelStatus = 'Concluded';
        onStatusChange?.('Concluded');

        if (Date.now() < suppressClosedToastUntil) return;
        const concludedBy = event.concludedBy?.trim();
        const defaultClosedMessage = conversationType === 'DirectMessage'
          ? 'This direct message has been concluded'
          : 'This channel has been concluded';
        toast.info(concludedBy
          ? `${concludedBy} concluded this ${conversationType === 'DirectMessage' ? 'direct message' : 'channel'}`
          : defaultClosedMessage);
      },
      onConnectionStateChange: (connected) => {
        hubConnected = connected;
        if (connected) {
          hubError = null;
        }
      },
      onError: (error) => {
        hubError = error.message;
      },
    });

    hubService.connect()
      .then(() => hubService.joinConversation(conversationId))
      .catch((err) => {
        hubError = err instanceof Error ? err.message : 'Failed to connect to live updates';
      });

    return () => {
      unsubscribe();
      hubService.leaveConversation(conversationId).catch((err) => console.warn('[ChannelView] leaveConversation cleanup failed:', err));
    };
  });

  $effect(() => {
    if (!conversationId) return;
    extractingMessage = null;
    extractInitialData = null;
    extractError = null;
  });

</script>

{#snippet headerActions()}
  {#if canConclude}
    <IconButton
      icon="check-circle"
      size="sm"
      variant="success"
      disabled={statusLoading}
      onclick={handleConclude}
      title="Conclude"
    />
  {/if}
  {#if canCloseFromHeader}
    <IconButton
      icon="x"
      size="sm"
      variant="ghost"
      title="Close"
      onclick={handleHeaderClose}
    />
  {/if}
{/snippet}

<PageShell class={className}>
  <!-- Conversation Header -->
  {#if showHeaderProp}
    <ConversationHeader
      icon={headerIcon}
      title={displayName}
      description={displayDescription}
      byName={creatorName ?? undefined}
      actions={headerActions}
    />
  {/if}

  <PageBody padded={false} scrollable={false} class="flex min-h-0">
    <!-- Chat area (messages + input) -->
    <div class="flex-1 flex flex-col min-w-0 min-h-0">
      <!-- Channel Status Banner -->
      {#if channelStatus === 'Concluded'}
        <div class="flex-shrink-0 flex items-center gap-2 px-4 py-2 bg-[var(--color-warning-50)] border-b border-[var(--color-warning-200)] text-[var(--color-warning-700)]">
          <Icon name="check-circle" size="sm" />
          <span class="text-sm">This channel has been concluded. No new messages can be posted.</span>
        </div>
      {/if}

      {#if extractingMessage}
        <div class="flex-shrink-0 border-b border-[var(--color-border-primary)] bg-[var(--color-surface-100)]">
          <div class="flex items-center justify-between px-3 py-2">
            <div>
              <div class="text-sm font-medium text-[var(--color-text-primary)]">Extract Requirement</div>
              <div class="text-xs text-[var(--color-text-tertiary)]">
                Drafted from a decision in {displayName}
              </div>
            </div>
            <Button variant="ghost" size="sm" onclick={handleCancelExtract}>
              <Icon name="x" size="sm" />
            </Button>
          </div>
          {#if extractError}
            <div class="px-3 pb-2 text-xs text-[var(--color-error-600)]">{extractError}</div>
          {/if}
          <div class="px-3 pb-4">
            <RequirementForm
              mode="create"
              loading={extractLoading}
              initialData={extractInitialData ?? {}}
              onSubmit={handleExtractSubmit}
            />
          </div>
        </div>
      {/if}
      <!-- Messages Area -->
      <div
        bind:this={messagesContainer}
        class="flex-1 overflow-y-auto p-3 space-y-3 bg-[var(--color-surface-50)]"
      >
        {#if loading && uniqueMessages.length === 0}
          <div class="flex-1 flex items-center justify-center">
            <Spinner size="lg" />
          </div>
        {:else if error && uniqueMessages.length === 0}
          <div class="flex flex-col items-center justify-center h-full text-center">
            <Icon name="alert-circle" size="xl" class="text-[var(--color-text-muted)] mb-3" />
            <p class="text-sm text-[var(--color-text-muted)]">{error}</p>
          </div>
        {:else if hubError && uniqueMessages.length === 0}
          <div class="flex flex-col items-center justify-center h-full text-center">
            <Icon name="wifi-off" size="xl" class="text-[var(--color-text-muted)] mb-3" />
            <p class="text-sm text-[var(--color-text-muted)]">{hubError}</p>
          </div>
        {:else}
          {#if hasMore}
            <Button
              variant="unstyled"
              class="w-full py-2 text-sm text-[var(--color-accent-primary)]
                hover:bg-[var(--color-surface-200)] rounded transition-colors"
              onclick={handleLoadMore}
            >
              Load earlier messages
            </Button>
          {/if}

          {#each uniqueMessages as message, index (message.id)}
            <MessageBubble
              {message}
              viewMode={viewMode}
              {projectId}
              isOwn={currentUserId ? message.sender?.id === currentUserId : false}
              isLast={index === uniqueMessages.length - 1}
              onExtractRequirement={canExtractRequirement ? handleExtractRequirement : undefined}
              onEdit={!isChannelReadOnly ? handleEditMessage : undefined}
              onDelete={!isChannelReadOnly ? handleDeleteMessage : undefined}
              {currentUserRole}
            />
          {/each}

          {#if uniqueMessages.length === 0}
            <div class="flex flex-col items-center justify-center h-full text-center">
              <Icon name="message-circle" size="xl" class="text-[var(--color-text-muted)] mb-3" />
              <p class="text-sm text-[var(--color-text-muted)]">
                No messages yet. Start the conversation!
              </p>
            </div>
          {/if}
        {/if}
      </div>

      <!-- Message Input (hidden for concluded/readonly channels) -->
      {#if !isChannelReadOnly}
        <div class="flex-shrink-0 bg-[var(--color-surface-50)]">
          <MessageInput
            onSubmit={handleSendMessage}
            onTypingStart={handleTypingStart}
            onTypingStop={handleTypingStop}
            placeholder="Message #{displayName}"
            draftKey={draftKey}
            disabled={sending}
          />
        </div>
      {:else if isReadOnlyProp}
        <div class="border-t border-[var(--color-border-primary)] bg-[var(--color-surface-100)] px-4 py-2">
          <p class="text-xs text-[var(--color-text-muted)] text-center">
            <Icon name="eye" size="xs" class="inline-block mr-1 align-text-bottom" />
            Read-only view
          </p>
        </div>
      {/if}
    </div>

    <!-- Participants Panel (right side, resizable) — hidden when external right panel is used -->
    {#if !hideParticipantPanel}
      <div class="relative z-10 flex-shrink-0">
        <ResizeHandle
          orientation="vertical"
          isResizing={isParticipantsResizing}
          onpointerdown={handleParticipantsResizeStart}
          ariaLabel="Resize participants panel"
        />
      </div>
      <div
        class="flex-shrink-0 border-l border-[var(--color-border-primary)]"
        class:participants-resizing={isParticipantsResizing}
        style="width: {participantsPanelWidth}px"
      >
        <ParticipantsPanel
          channel={channelDetail}
          {currentUserId}
          tenantId={tenantId ?? ''}
          {conversationId}
          {linkedRequirements}
          {isDirectMessage}
          readonly={isReadOnlyProp}
          {onSendInvitation}
          onRemoveMember={isReadOnlyProp ? undefined : handleRemoveMember}
          onLinkedRequirementClick={handleLinkedRequirementClick}
          class="h-full"
        />
      </div>
    {/if}
  </PageBody>
</PageShell>

<style>
  .participants-resizing {
    will-change: width;
    pointer-events: none;
    user-select: none;
  }
</style>
