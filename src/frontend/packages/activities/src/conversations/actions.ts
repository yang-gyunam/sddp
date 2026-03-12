/**
 * Conversation Actions
 * Mediates between Conversation stores and ConversationService
 */

import { getConversationService, type ConversationSummaryDto } from './services';
import {
  setProjectGroups,
  setDirectMessages,
  setStarredConversations,
  setFlatConversations,
  setLoading,
  setError,
} from './stores';
import type {
  ProjectConversationGroup,
  ConversationSummary as UiConversationSummary,
  DirectMessageSummary,
} from './types';

/**
 * Load conversation sidebar data from API
 * Falls back to dummy data if API is unavailable
 */
export async function loadConversationsSidebar(
  tenantId: string,
  projectId: string
): Promise<void> {
  setLoading(true);
  setError(null);

  try {
    const service = getConversationService();
    service.setContext(tenantId, projectId);

    // Fetch channels from API
    const channels = await service.getChannelList();

    // Transform to ProjectConversationGroup format
    // Since Channel doesn't carry projectId, group all under the current project
    const conversations: UiConversationSummary[] = channels.map((channel) => ({
      id: channel.id,
      name: channel.topic,
      type: 'public' as const,
      projectId,
      projectName: projectId,
      unreadCount: 0,
      hasUnreadMentions: false,
      lastMessageAt: channel.createdAt,
      starred: false,
      muted: false,
    }));

    const group: ProjectConversationGroup = {
      projectId,
      projectName: projectId,
      projectCode: projectId,
      conversations,
      unreadCount: 0,
      expanded: true,
    };

    setProjectGroups([group]);
    setDirectMessages([]);
    setStarredConversations([]);
  } catch (error) {
    const message = error instanceof Error ? error.message : 'Failed to load conversations';
    console.error('Conversation API failed:', message);
    setError(message);
    setProjectGroups([]);
    setDirectMessages([]);
    setStarredConversations([]);
  } finally {
    setLoading(false);
  }
}

/**
 * Convert API ConversationSummaryDto to UI ConversationSummary
 */
function convertToUiConversationSummary(dto: ConversationSummaryDto, isStarred: boolean = false): UiConversationSummary {
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

/**
 * Load global (tenant-wide) conversation sidebar data from API
 * For Conversations Activity (no project association)
 */
export async function loadGlobalConversationsSidebar(
  tenantId: string
): Promise<void> {
  setLoading(true);
  setError(null);

  try {
    const service = getConversationService();
    service.setContext(tenantId, '');

    // Fetch global conversations + DM list from API
    // Starred endpoint may not exist yet; fetch separately to avoid breaking sidebar
    const [convResult, dmResult] = await Promise.allSettled([
      service.getGlobalConversations(),
      service.getDMChannels(),
    ]);
    const conversations = convResult.status === 'fulfilled' ? convResult.value : [];
    const dmChannels = dmResult.status === 'fulfilled' ? dmResult.value : [];
    const starredDtos = await service.getStarredConversations().catch(() => []);

    const starredIds = new Set(starredDtos.map((dto) => dto.id));

    // Categorize conversations into channels, privateChannels, topics
    const channels: UiConversationSummary[] = [];
    const privateChannels: UiConversationSummary[] = [];
    const topics: UiConversationSummary[] = [];

    for (const conv of conversations) {
      const uiConv = convertToUiConversationSummary(conv, starredIds.has(conv.id));

      if (conv.conversationType === 'Forum') {
        topics.push(uiConv);
      } else if (conv.conversationType === 'Channel') {
        if (conv.visibility === 'Private') {
          privateChannels.push(uiConv);
        } else {
          channels.push(uiConv);
        }
      }
    }

    const directMessages: DirectMessageSummary[] = dmChannels.map((dm) => ({
      id: dm.id,
      participantId: dm.otherUser.id,
      participantName: dm.otherUser.name ?? 'Unknown',
      participantAvatar: dm.otherUser.avatarUrl ?? undefined,
      channelStatus: dm.status ?? null,
      isOnline: false,
      lastMessage: dm.lastMessage?.content,
      lastMessageAt: dm.lastMessage?.createdAt ?? dm.updatedAt,
      unreadCount: dm.unreadCount,
    }));

    const starredConversations = starredDtos.map((dto) => convertToUiConversationSummary(dto, true));

    setFlatConversations({ channels, privateChannels, topics });
    setDirectMessages(directMessages);
    setStarredConversations(starredConversations);
  } catch (error) {
    const message = error instanceof Error ? error.message : 'Failed to load conversations';
    console.error('Global Conversations API failed:', message);
    setError(message);
    setFlatConversations({ channels: [], privateChannels: [], topics: [] });
    setDirectMessages([]);
    setStarredConversations([]);
  } finally {
    setLoading(false);
  }
}
