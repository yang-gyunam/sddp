/**
 * useConversationToastRouting — Phase 1
 * Handles sddp:new-message events → toast display + click-to-navigate
 */

import { get } from 'svelte/store';
import { toast } from '@sddp/shell';
import { globalActiveTab } from '@sddp/shell/stores';
import { getAuthState } from '@sddp/shell/auth/stores';
import {
  getConversationService,
  getSidebarState,
  getActiveConversationContext,
  type ConversationEntry,
  type Message,
} from '@sddp/activities/conversations';
import { isConversationVisible } from '../lib/conversation-toast';

export interface ConversationToastRoutingParams {
  onConversationSelect: (conv: ConversationEntry) => void;
  onDirectMessageSelect: (item: {
    id: string;
    name: string;
    channelStatus?: 'Active' | 'Concluded' | null;
    projectId?: string;
  }) => void;
  onOpenProjectDM: (
    conversationId: string,
    projectId: string,
    options?: {
      participantName?: string;
      channelStatus?: 'Active' | 'Concluded' | null;
    }
  ) => boolean;
  onOpenProjectConversation: (
    conversationId: string,
    projectId: string,
    options?: {
      conversationKind?: 'channel' | 'forum';
      channelStatus?: 'Active' | 'Concluded' | null;
    }
  ) => boolean;
}

function findConversationInSidebar(
  conversationId: string
): {
  name: string;
  type: 'channel' | 'dm' | 'forum';
  muted: boolean;
  originalType?: string;
  projectId?: string | null;
  channelStatus?: 'Active' | 'Concluded' | null;
} | null {
  const sidebar = getSidebarState();

  const dm = sidebar.directMessages.find((d) => d.id === conversationId);
  if (dm)
    return {
      name: dm.participantName,
      type: 'dm',
      muted: false,
      projectId: null,
      channelStatus: dm.channelStatus ?? null,
    };

  const ch = sidebar.channels.find((c) => c.id === conversationId);
  if (ch)
    return {
      name: ch.name,
      type: 'channel',
      muted: ch.muted,
      originalType: 'Channel',
      projectId: ch.projectId ?? null,
      channelStatus: ch.channelStatus ?? null,
    };

  const pc = sidebar.privateChannels.find((c) => c.id === conversationId);
  if (pc)
    return {
      name: pc.name,
      type: 'channel',
      muted: pc.muted,
      originalType: 'Channel',
      projectId: pc.projectId ?? null,
      channelStatus: pc.channelStatus ?? null,
    };

  const topic = sidebar.topics.find((c) => c.id === conversationId);
  if (topic)
    return {
      name: topic.name,
      type: 'forum',
      muted: topic.muted,
      originalType: 'Forum',
      projectId: topic.projectId ?? null,
      channelStatus: topic.channelStatus ?? null,
    };

  for (const group of sidebar.projectGroups) {
    const gc = group.conversations.find((c) => c.id === conversationId);
    if (gc) {
      return {
        name: gc.name,
        type: gc.type === 'dm' ? 'dm' : 'channel',
        muted: gc.muted ?? false,
        originalType: 'Channel',
        projectId: gc.projectId ?? group.projectId,
        channelStatus: gc.channelStatus ?? null,
      };
    }
  }

  return null;
}

export function useConversationToastRouting(params: ConversationToastRoutingParams) {
  function handleNewMessageToast(event: Event) {
    const message = (event as CustomEvent<Message>).detail;
    if (!message?.conversationId) return;

    const currentUser = getAuthState().user;
    if (!currentUser || message.sender?.id === currentUser.id) return;

    // Skip if the active tab is already showing this conversation
    const activeTab = get(globalActiveTab);
    const activeConversationContext = activeTab?.id
      ? getActiveConversationContext(activeTab.id)
      : null;
    if (
      isConversationVisible({
        conversationId: message.conversationId,
        activeTabPath: activeTab?.path,
        activeConversationId: activeConversationContext?.conversationId ?? null,
      })
    ) {
      return;
    }

    const conv = findConversationInSidebar(message.conversationId);
    if (conv?.muted) return;

    const senderName = message.sender?.name ?? 'Unknown';
    const prefix = conv?.type === 'dm' ? '' : `#${conv?.name ?? 'channel'} · `;
    const truncated =
      message.content.length > 80 ? message.content.slice(0, 80) + '...' : message.content;

    toast.info(truncated, {
      title: `${prefix}${senderName}`,
      duration: 5000,
      onContentClick: async () => {
        let resolvedProjectId = conv?.projectId ?? null;
        let resolvedChannelStatus = conv?.channelStatus ?? null;
        let resolvedConversationKind: 'channel' | 'forum' | 'dm' =
          conv?.type === 'forum' ? 'forum' : conv?.type === 'dm' ? 'dm' : 'channel';

        if (!resolvedProjectId) {
          const tenantId = getAuthState().user?.tenantId;
          if (tenantId) {
            try {
              const service = getConversationService();
              service.setContext(tenantId, '');
              const detail = await service.getConversationById(message.conversationId);
              resolvedProjectId = detail.projectId ?? null;
              resolvedChannelStatus = detail.channelStatus ?? resolvedChannelStatus;
              resolvedConversationKind =
                detail.conversationType === 'Forum'
                  ? 'forum'
                  : detail.conversationType === 'DirectMessage'
                    ? 'dm'
                    : 'channel';
            } catch (error) {
              console.warn(
                'Failed to resolve conversation scope for toast navigation:',
                error
              );
            }
          }
        }

        if (resolvedProjectId) {
          if (resolvedConversationKind === 'dm') {
            params.onOpenProjectDM(message.conversationId, resolvedProjectId, {
              participantName: conv?.name ?? senderName,
              channelStatus: resolvedChannelStatus,
            });
            return;
          }

          params.onOpenProjectConversation(message.conversationId, resolvedProjectId, {
            conversationKind: resolvedConversationKind === 'forum' ? 'forum' : 'channel',
            channelStatus: resolvedChannelStatus,
          });
          return;
        }

        if (conv?.type === 'dm') {
          params.onDirectMessageSelect({
            id: message.conversationId,
            name: conv?.name ?? senderName,
          });
        } else {
          params.onConversationSelect({
            id: message.conversationId,
            name: conv?.name ?? 'Channel',
            type: (conv?.originalType as 'Channel' | 'Forum') ?? 'Channel',
            isPrivate: false,
          });
        }
      },
    });
  }

  function start(): () => void {
    window.addEventListener('sddp:new-message', handleNewMessageToast as () => void);
    return () => {
      window.removeEventListener('sddp:new-message', handleNewMessageToast as () => void);
    };
  }

  return { start };
}
