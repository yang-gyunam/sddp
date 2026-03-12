/**
 * ConversationService - API Client for Conversations
 * Handles CRUD operations for conversations, channels, and messages
 */

import { fetchWithAuth } from '../../shared/api';
import type { UserRef } from '../../shared/types';
import type {
  ChannelSummary,
  ChannelDetail,
  ChannelStatus,
  Message,
  MessagesPage,
  CreateChannelRequest,
  CreateMessageRequest,
  CloseChannelRequest,
  UnreadCounts,
  UserConversationSettings,
  DirectMessageInfo,
  ConversationMember,
  ConversationSearchResult,
  LinkedRequirement,
} from '../types';
import type { ConversationEntry } from '../types/conversation-entry.types';
import type {
  ConversationScope,
  ConversationType,
  ConversationVisibility,
} from '../types/conversation-taxonomy.types';

// ============================================
// Conversation API Types
// ============================================

export interface ConversationSummaryDto {
  id: string;
  projectId?: string | null;         // null = tenant-wide (global) conversation
  name: string;
  description?: string | null;
  conversationType: ConversationType;
  visibility: ConversationVisibility;
  scope: ConversationScope;
  isArchived: boolean;
  sortOrder: number;
  memberCount: number;
  unreadCount: number;
  lastMessageAt?: string | null;
  channelStatus?: 'Active' | 'Concluded' | null;
  decisionSpecId?: string | null;
}

export interface ConversationPagedResult<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

export interface DirectMessageInvitationResultDto {
  sent: boolean;
  message: string;
}

interface ConversationPagingOptions {
  pageNumber?: number;
  page?: number;
  pageSize?: number;
}

function appendPagingParams(params: URLSearchParams, options?: ConversationPagingOptions): void {
  if (!options) return;
  if (options.pageNumber) params.set('pageNumber', options.pageNumber.toString());
  if (options.page) params.set('page', options.page.toString());
  if (options.pageSize) params.set('pageSize', options.pageSize.toString());
}

function normalizePageNumber(pageNumber: number): number {
  return pageNumber < 1 ? 1 : pageNumber;
}

async function collectAllPages<T>(
  loadPage: (pageNumber: number) => Promise<ConversationPagedResult<T>>
): Promise<T[]> {
  const collected: T[] = [];
  let pageNumber = 1;

  while (true) {
    const page = await loadPage(pageNumber);
    collected.push(...page.items);

    if (!page.hasNextPage) {
      break;
    }

    pageNumber += 1;
  }

  return collected;
}

/**
 * Convert API ConversationSummaryDto to UI ConversationEntry type
 */
export function convertToConversationEntry(dto: ConversationSummaryDto): ConversationEntry {
  return {
    id: dto.id,
    projectId: dto.projectId ?? null,
    name: dto.name,
    type: dto.conversationType,
    visibility: dto.visibility,
    scope: dto.scope,
    description: dto.description ?? undefined,
    isPrivate: dto.visibility === 'Private',
    unreadCount: dto.unreadCount,
    channelStatus: dto.channelStatus ?? null,
    decisionSpecId: dto.decisionSpecId ?? null,
  };
}

export interface ConversationMessageDto {
  id: string;
  conversationId: string;
  channelId?: string;
  sender: UserRef;
  type: Message['type'];
  content: string;
  references: string[];
  replyToId?: string | null;
  isEdited: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface ConversationMessagesPage {
  messages: ConversationMessageDto[];
  nextCursor: string | null;
  hasMore: boolean;
}

export interface CreateConversationRequest {
  name: string;
  conversationType?: ConversationType;
  visibility?: ConversationVisibility;
  scope?: ConversationScope;
  description?: string | null;
  sortOrder?: number;
}

// ============================================
// Channel API
// ============================================

/**
 * Get list of channels
 */
export async function getChannelList(
  tenantId: string,
  projectId: string,
  options?: {
    page?: number;
    pageSize?: number;
    status?: ChannelStatus;
  }
): Promise<ChannelSummary[]> {
  const params = new URLSearchParams();
  if (options?.page) params.set('page', options.page.toString());
  if (options?.pageSize) params.set('pageSize', options.pageSize.toString());
  if (options?.status) params.set('status', options.status);

  const queryString = params.toString();
  const endpoint = `/conversations${queryString ? `?${queryString}` : ''}`;

  return fetchWithAuth<ChannelSummary[]>(endpoint, { tenantId, projectId });
}

// ============================================
// Invitable Users API Types
// ============================================

export interface InvitableUser {
  id: string;
  displayName: string;
  email: string;
  isAi: boolean;
}

/** Backend ParticipantDto shape */
interface ParticipantDto {
  id: string;
  user: UserRef;
  type: string;
  role: string | null;
  joinedAt: string;
  leftAt: string | null;
  isActive: boolean;
}

/**
 * Get members of a conversation
 */
export async function getConversationMembers(
  tenantId: string,
  conversationId: string
): Promise<ConversationMember[]> {
  const dtos = await fetchWithAuth<ParticipantDto[]>(`/conversations/${conversationId}/members`, {
    tenantId,
  });
  return dtos.map((dto) => ({
    id: dto.id,
    user: dto.user,
    type: dto.type as ConversationMember['type'],
    role: dto.role ?? 'Member',
    joinedAt: dto.joinedAt,
    isActive: dto.isActive,
  }));
}

/**
 * Get requirements linked to a conversation
 */
export async function getLinkedRequirements(
  tenantId: string,
  conversationId: string
): Promise<LinkedRequirement[]> {
  return fetchWithAuth<LinkedRequirement[]>(`/conversations/${conversationId}/linked-requirements`, {
    tenantId,
  });
}

/**
 * Get conversation by ID (returns ConversationSummaryDto)
 */
export async function getConversationById(
  tenantId: string,
  conversationId: string
): Promise<ConversationSummaryDto> {
  return fetchWithAuth<ConversationSummaryDto>(`/conversations/${conversationId}`, {
    tenantId,
  });
}

/**
 * Get channel by ID (with members)
 */
export async function getChannelById(
  tenantId: string,
  projectId: string,
  channelId: string
): Promise<ChannelDetail> {
  return fetchWithAuth<ChannelDetail>(`/conversations/${channelId}`, {
    tenantId,
    projectId,
  });
}

/**
 * Create a new channel (conversation)
 */
export async function createChannel(
  tenantId: string,
  projectId: string,
  request: CreateChannelRequest
): Promise<ChannelDetail> {
  return fetchWithAuth<ChannelDetail>('/conversations', {
    method: 'POST',
    body: { ...request, name: request.topic, conversationType: 'Channel' },
    tenantId,
    projectId,
  });
}

/**
 * Close a channel
 */
export async function closeChannel(
  tenantId: string,
  projectId: string,
  channelId: string,
  request?: CloseChannelRequest
): Promise<ChannelSummary> {
  return fetchWithAuth<ChannelSummary>(`/conversations/${channelId}/close`, {
    method: 'POST',
    body: request || {},
    tenantId,
    projectId,
  });
}

// ============================================
// Messages API
// ============================================

/**
 * Get messages for a channel (cursor-based pagination)
 */
export async function getMessages(
  tenantId: string,
  projectId: string,
  channelId: string,
  options?: {
    cursor?: string;
    limit?: number;
  }
): Promise<MessagesPage> {
  const params = new URLSearchParams();
  if (options?.cursor) params.set('cursor', options.cursor);
  if (options?.limit) params.set('limit', options.limit.toString());

  const queryString = params.toString();
  const endpoint = `/conversations/${channelId}/messages${queryString ? `?${queryString}` : ''}`;

  return fetchWithAuth<MessagesPage>(endpoint, { tenantId, projectId });
}

/**
 * Get pinned messages for a channel
 */
export async function getPinnedMessages(
  tenantId: string,
  projectId: string,
  channelId: string,
  options?: { limit?: number }
): Promise<Message[]> {
  const params = new URLSearchParams();
  if (options?.limit) params.set('limit', options.limit.toString());

  const queryString = params.toString();
  const endpoint = `/conversations/${channelId}/pinned${queryString ? `?${queryString}` : ''}`;

  return fetchWithAuth<Message[]>(endpoint, { tenantId, projectId });
}

/**
 * Post a new message to a channel
 */
export async function postMessage(
  tenantId: string,
  projectId: string,
  channelId: string,
  request: CreateMessageRequest
): Promise<Message> {
  return fetchWithAuth<Message>(`/conversations/${channelId}/messages`, {
    method: 'POST',
    body: request,
    tenantId,
    projectId,
  });
}

// ============================================
// Invite Members API
// ============================================

/**
 * Get users that can be invited to a conversation
 */
export async function getInvitableUsers(
  tenantId: string,
  conversationId: string,
  search?: string
): Promise<InvitableUser[]> {
  const params = new URLSearchParams();
  if (search) params.set('search', search);

  const queryString = params.toString();
  const endpoint = `/conversations/${conversationId}/invitable-users${queryString ? `?${queryString}` : ''}`;

  return fetchWithAuth<InvitableUser[]>(endpoint, { tenantId });
}

/**
 * Add members to a conversation
 */
export async function addConversationMembers(
  tenantId: string,
  conversationId: string,
  userIds: string[]
): Promise<ConversationMember[]> {
  const dtos = await fetchWithAuth<ParticipantDto[]>(`/conversations/${conversationId}/members`, {
    method: 'POST',
    body: { userIds },
    tenantId,
  });
  return dtos.map((dto) => ({
    id: dto.id,
    user: dto.user,
    type: dto.type as ConversationMember['type'],
    role: dto.role ?? 'Member',
    joinedAt: dto.joinedAt,
    isActive: dto.isActive,
  }));
}

// ============================================
// Unread Messages API
// ============================================

/**
 * Get unread message counts
 */
export async function getUnreadCounts(
  tenantId: string,
  projectId?: string
): Promise<UnreadCounts> {
  return fetchWithAuth<UnreadCounts>('/conversations/unread', {
    tenantId,
    projectId,
  });
}

/**
 * Mark conversation as read
 */
export async function markAsRead(
  tenantId: string,
  conversationId: string,
  readUntil?: string
): Promise<void> {
  await fetchWithAuth<{ success: boolean }>(`/conversations/${conversationId}/read`, {
    method: 'POST',
    body: readUntil ? { readUntil } : {},
    tenantId,
  });
}

// ============================================
// Starred/Muted API
// ============================================

/**
 * Get starred conversations
 */
export async function getStarredConversations(
  tenantId: string,
  projectId?: string
): Promise<ConversationSummaryDto[]> {
  return fetchWithAuth<ConversationSummaryDto[]>('/conversations/starred', {
    tenantId,
    ...(projectId ? { projectId } : {}),
  });
}

/**
 * Toggle starred status
 */
export async function toggleStarred(
  tenantId: string,
  conversationId: string
): Promise<{ isStarred: boolean }> {
  return fetchWithAuth<{ isStarred: boolean }>(`/conversations/${conversationId}/star`, {
    method: 'POST',
    tenantId,
  });
}

/**
 * Toggle muted status
 */
export async function toggleMuted(
  tenantId: string,
  conversationId: string
): Promise<{ isMuted: boolean }> {
  return fetchWithAuth<{ isMuted: boolean }>(`/conversations/${conversationId}/mute`, {
    method: 'POST',
    tenantId,
  });
}

/**
 * Get user conversation settings
 */
export async function getUserConversationSettings(
  tenantId: string,
  conversationId: string
): Promise<UserConversationSettings> {
  return fetchWithAuth<UserConversationSettings>(`/conversations/${conversationId}/settings`, {
    tenantId,
  });
}

// ============================================
// DM (Direct Message) API
// ============================================

/**
 * Get DM channels
 */
export async function getDMChannels(
  tenantId: string,
  projectId?: string
): Promise<DirectMessageInfo[]> {
  return collectAllPages((pageNumber) =>
    getDMChannelsPage(tenantId, projectId, {
      pageNumber,
      pageSize: 50,
    })
  );
}

export async function getDMChannelsPage(
  tenantId: string,
  projectId?: string,
  options?: ConversationPagingOptions
): Promise<ConversationPagedResult<DirectMessageInfo>> {
  const params = new URLSearchParams();
  appendPagingParams(params, options);

  const queryString = params.toString();
  const endpoint = `/conversations/dm${queryString ? `?${queryString}` : ''}`;
  return fetchWithAuth<ConversationPagedResult<DirectMessageInfo>>(endpoint, { tenantId, projectId });
}

/**
 * Get or create DM channel with a user
 */
export async function getOrCreateDMChannel(
  tenantId: string,
  targetUserId: string,
  projectId?: string
): Promise<ChannelDetail> {
  return fetchWithAuth<ChannelDetail>(`/conversations/dm/${targetUserId}`, {
    method: 'POST',
    tenantId,
    projectId,
  });
}

/**
 * Send DM invitation (DM is created when recipient accepts)
 */
export async function sendDMInvitation(
  tenantId: string,
  targetUserId: string,
  projectId?: string
): Promise<DirectMessageInvitationResultDto> {
  return fetchWithAuth<DirectMessageInvitationResultDto>(`/conversations/dm/invitations/${targetUserId}`, {
    method: 'POST',
    tenantId,
    projectId,
  });
}

/**
 * Accept DM invitation
 */
export async function acceptDMInvitation(
  tenantId: string,
  notificationId: string,
  projectId?: string
): Promise<ConversationSummaryDto> {
  return fetchWithAuth<ConversationSummaryDto>(`/conversations/dm/invitations/${notificationId}/accept`, {
    method: 'POST',
    tenantId,
    projectId,
  });
}

/**
 * Reject DM invitation
 */
export async function rejectDMInvitation(
  tenantId: string,
  notificationId: string,
  projectId?: string
): Promise<{ message: string }> {
  return fetchWithAuth<{ message: string }>(`/conversations/dm/invitations/${notificationId}/reject`, {
    method: 'POST',
    tenantId,
    projectId,
  });
}

// ============================================
// DM Status API
// ============================================

/**
 * Conclude a DM (Active → Concluded)
 */
export async function concludeDM(
  tenantId: string,
  dmId: string,
  request?: { decisionSpecId?: string }
): Promise<ConversationSummaryDto> {
  return fetchWithAuth<ConversationSummaryDto>(`/conversations/dm/${dmId}/conclude`, {
    method: 'POST',
    body: request || {},
    tenantId,
  });
}

// ============================================
// Conversations API
// ============================================

/**
 * Get conversations for a project
 */
export async function getConversations(
  tenantId: string,
  projectId: string
): Promise<ConversationSummaryDto[]> {
  return collectAllPages((pageNumber) =>
    getConversationsPage(tenantId, projectId, {
      pageNumber,
      pageSize: 50,
    })
  );
}

export async function getConversationsPage(
  tenantId: string,
  projectId: string,
  options?: ConversationPagingOptions
): Promise<ConversationPagedResult<ConversationSummaryDto>> {
  const params = new URLSearchParams();
  appendPagingParams(params, options);

  const queryString = params.toString();
  const endpoint = `/conversations${queryString ? `?${queryString}` : ''}`;
  return fetchWithAuth<ConversationPagedResult<ConversationSummaryDto>>(endpoint, {
    tenantId,
    projectId,
  });
}

/**
 * Get global (tenant-wide) conversations (no project association)
 */
export async function getGlobalConversations(
  tenantId: string
): Promise<ConversationSummaryDto[]> {
  return collectAllPages((pageNumber) =>
    getGlobalConversationsPage(tenantId, {
      pageNumber,
      pageSize: 50,
    })
  );
}

export async function getGlobalConversationsPage(
  tenantId: string,
  options?: ConversationPagingOptions
): Promise<ConversationPagedResult<ConversationSummaryDto>> {
  const params = new URLSearchParams();
  appendPagingParams(params, options);

  const queryString = params.toString();
  const endpoint = `/conversations/global${queryString ? `?${queryString}` : ''}`;
  return fetchWithAuth<ConversationPagedResult<ConversationSummaryDto>>(endpoint, {
    tenantId,
  });
}

/**
 * Create a new conversation
 */
export async function createConversation(
  tenantId: string,
  projectId: string,
  request: CreateConversationRequest
): Promise<ConversationSummaryDto> {
  return fetchWithAuth<ConversationSummaryDto>('/conversations', {
    method: 'POST',
    body: request,
    tenantId,
    projectId,
  });
}

/**
 * Get messages for a conversation (cursor-based pagination)
 */
export async function getConversationMessages(
  tenantId: string,
  projectId: string,
  conversationId: string,
  options?: {
    cursor?: string;
    limit?: number;
  }
): Promise<ConversationMessagesPage> {
  const params = new URLSearchParams();
  if (options?.cursor) params.set('cursor', options.cursor);
  if (options?.limit) params.set('limit', options.limit.toString());

  const queryString = params.toString();
  const endpoint = `/conversations/${conversationId}/messages${queryString ? `?${queryString}` : ''}`;

  return fetchWithAuth<ConversationMessagesPage>(endpoint, { tenantId, projectId });
}

/**
 * Get pinned messages for a conversation
 */
export async function getPinnedConversationMessages(
  tenantId: string,
  projectId: string,
  conversationId: string,
  options?: { limit?: number }
): Promise<ConversationMessageDto[]> {
  const params = new URLSearchParams();
  if (options?.limit) params.set('limit', options.limit.toString());

  const queryString = params.toString();
  const endpoint = `/conversations/${conversationId}/pinned${queryString ? `?${queryString}` : ''}`;

  return fetchWithAuth<ConversationMessageDto[]>(endpoint, { tenantId, projectId });
}

/**
 * Post a new message to a conversation
 */
export async function postConversationMessage(
  tenantId: string,
  projectId: string,
  conversationId: string,
  request: CreateMessageRequest
): Promise<ConversationMessageDto> {
  return fetchWithAuth<ConversationMessageDto>(`/conversations/${conversationId}/messages`, {
    method: 'POST',
    body: request,
    tenantId,
    projectId,
  });
}

// ============================================
// Message Edit/Delete API
// ============================================

/**
 * Edit a conversation message
 */
export async function editConversationMessage(
  tenantId: string,
  projectId: string,
  conversationId: string,
  messageId: string,
  content: string
): Promise<ConversationMessageDto> {
  return fetchWithAuth<ConversationMessageDto>(
    `/conversations/${conversationId}/messages/${messageId}`,
    {
      method: 'PUT',
      body: { content },
      tenantId,
      projectId,
    }
  );
}

/**
 * Delete a conversation message
 */
export async function deleteConversationMessage(
  tenantId: string,
  projectId: string,
  conversationId: string,
  messageId: string
): Promise<void> {
  await fetchWithAuth<{ deleted: boolean }>(
    `/conversations/${conversationId}/messages/${messageId}`,
    {
      method: 'DELETE',
      tenantId,
      projectId,
    }
  );
}

// ============================================
// Channel Status API
// ============================================

/**
 * Conclude a channel (Active → Concluded)
 */
export async function concludeChannel(
  tenantId: string,
  conversationId: string,
  request?: { decisionSpecId?: string }
): Promise<ConversationSummaryDto> {
  return fetchWithAuth<ConversationSummaryDto>(`/conversations/${conversationId}/conclude`, {
    method: 'POST',
    body: request || {},
    tenantId,
  });
}

// ============================================
// Search API
// ============================================

/**
 * Search conversations by name or description
 */
export async function searchConversations(
  tenantId: string,
  projectId: string,
  query: string,
  limit: number = 15
): Promise<ConversationSearchResult[]> {
  const params = new URLSearchParams();
  params.set('q', query);
  if (limit !== 15) params.set('limit', limit.toString());

  return fetchWithAuth<ConversationSearchResult[]>(`/conversations/search?${params.toString()}`, {
    tenantId,
    projectId,
  });
}

/**
 * Remove a member from a conversation
 */
export async function removeConversationMember(
  tenantId: string,
  conversationId: string,
  targetUserId: string
): Promise<{ removed: boolean }> {
  return fetchWithAuth<{ removed: boolean }>(
    `/conversations/${conversationId}/members/${targetUserId}`,
    {
      method: 'DELETE',
      tenantId,
    }
  );
}

// ============================================
// Topics API Types
// ============================================

export type TopicStatus = 'Open' | 'Closed';

export interface TopicDto {
  id: string;
  forumId: string;
  title: string;
  author: UserRef;
  status: TopicStatus;
  isPinned: boolean;
  isLocked: boolean;
  decisionSpecId?: string | null;
  messageCount: number;
  createdAt: string;
  updatedAt: string;
}

export interface TopicDetailDto {
  id: string;
  forumId: string;
  forumName: string;
  title: string;
  author: UserRef;
  status: TopicStatus;
  isPinned: boolean;
  isLocked: boolean;
  decisionSpecId?: string | null;
  conversationId?: string | null;
  recentMessages: ForumMessageDto[];
  messageCount: number;
  createdAt: string;
  updatedAt: string;
}

export interface TopicsPageDto {
  topics: TopicDto[];
  totalCount: number;
  page: number;
  pageSize: number;
  hasMore: boolean;
}

export interface ForumMessageDto {
  id: string;
  topicId: string;
  sender: UserRef;
  type: Message['type'];
  content: string;
  references: string[];
  replyToId?: string | null;
  isEdited: boolean;
  isPinned: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface ForumMessagesPageDto {
  messages: ForumMessageDto[];
  nextCursor: string | null;
  hasMore: boolean;
}

export interface CreateTopicRequest {
  title: string;
  initialMessageContent?: string | null;
}

export interface UpdateTopicRequest {
  title: string;
}

export interface CloseTopicRequest {
  decisionSpecId?: string | null;
}

// ============================================
// Topics API Functions
// ============================================

/**
 * Get topics for a conversation
 */
export async function getTopics(
  tenantId: string,
  conversationId: string,
  options?: {
    page?: number;
    pageSize?: number;
    status?: TopicStatus;
  }
): Promise<TopicsPageDto> {
  const params = new URLSearchParams();
  if (options?.page) params.set('page', options.page.toString());
  if (options?.pageSize) params.set('pageSize', options.pageSize.toString());
  if (options?.status) params.set('status', options.status);

  const queryString = params.toString();
  const endpoint = `/conversations/${conversationId}/topics${queryString ? `?${queryString}` : ''}`;

  return fetchWithAuth<TopicsPageDto>(endpoint, { tenantId });
}

/**
 * Get topic by ID
 */
export async function getTopicById(
  tenantId: string,
  conversationId: string,
  topicId: string
): Promise<TopicDetailDto> {
  return fetchWithAuth<TopicDetailDto>(
    `/conversations/${conversationId}/topics/${topicId}`,
    { tenantId }
  );
}

/**
 * Create a new topic
 */
export async function createTopic(
  tenantId: string,
  conversationId: string,
  request: CreateTopicRequest
): Promise<TopicDetailDto> {
  return fetchWithAuth<TopicDetailDto>(`/conversations/${conversationId}/topics`, {
    method: 'POST',
    body: request,
    tenantId,
  });
}

/**
 * Update a topic
 */
export async function updateTopic(
  tenantId: string,
  conversationId: string,
  topicId: string,
  request: UpdateTopicRequest
): Promise<TopicDto> {
  return fetchWithAuth<TopicDto>(
    `/conversations/${conversationId}/topics/${topicId}`,
    {
      method: 'PUT',
      body: request,
      tenantId,
    }
  );
}

/**
 * Close a topic
 */
export async function closeTopic(
  tenantId: string,
  conversationId: string,
  topicId: string,
  request?: CloseTopicRequest
): Promise<TopicDto> {
  return fetchWithAuth<TopicDto>(
    `/conversations/${conversationId}/topics/${topicId}/close`,
    {
      method: 'POST',
      body: request || {},
      tenantId,
    }
  );
}

/**
 * Reopen a topic
 */
export async function reopenTopic(
  tenantId: string,
  conversationId: string,
  topicId: string
): Promise<TopicDto> {
  return fetchWithAuth<TopicDto>(
    `/conversations/${conversationId}/topics/${topicId}/reopen`,
    {
      method: 'POST',
      tenantId,
    }
  );
}

/**
 * Toggle topic pin status
 */
export async function toggleTopicPin(
  tenantId: string,
  conversationId: string,
  topicId: string
): Promise<{ isPinned: boolean }> {
  return fetchWithAuth<{ isPinned: boolean }>(
    `/conversations/${conversationId}/topics/${topicId}/pin`,
    {
      method: 'POST',
      tenantId,
    }
  );
}

/**
 * Toggle topic lock status
 */
export async function toggleTopicLock(
  tenantId: string,
  conversationId: string,
  topicId: string
): Promise<{ isLocked: boolean }> {
  return fetchWithAuth<{ isLocked: boolean }>(
    `/conversations/${conversationId}/topics/${topicId}/lock`,
    {
      method: 'POST',
      tenantId,
    }
  );
}

/**
 * Get topic messages
 */
export async function getTopicMessages(
  tenantId: string,
  conversationId: string,
  topicId: string,
  options?: {
    cursor?: string;
    limit?: number;
  }
): Promise<ForumMessagesPageDto> {
  const params = new URLSearchParams();
  if (options?.cursor) params.set('cursor', options.cursor);
  if (options?.limit) params.set('limit', options.limit.toString());

  const queryString = params.toString();
  const endpoint = `/conversations/${conversationId}/topics/${topicId}/messages${queryString ? `?${queryString}` : ''}`;

  return fetchWithAuth<ForumMessagesPageDto>(endpoint, { tenantId });
}

/**
 * Post a message to a topic
 */
export async function postTopicMessage(
  tenantId: string,
  conversationId: string,
  topicId: string,
  request: CreateMessageRequest
): Promise<ForumMessageDto> {
  return fetchWithAuth<ForumMessageDto>(
    `/conversations/${conversationId}/topics/${topicId}/messages`,
    {
      method: 'POST',
      body: request,
      tenantId,
    }
  );
}

// ============================================
// Singleton Service Class
// ============================================

/**
 * ConversationService class for dependency injection
 */
export class ConversationService {
  private tenantId: string = '';
  private projectId: string = '';

  /**
   * Set tenant context
   */
  setContext(tenantId: string, projectId: string): void {
    this.tenantId = tenantId;
    this.projectId = projectId;
  }

  /**
   * Get tenant ID
   */
  getTenantId(): string {
    return this.tenantId;
  }

  /**
   * Get project ID
   */
  getProjectId(): string {
    return this.projectId;
  }

  // Channels
  async getChannelList(options?: {
    page?: number;
    pageSize?: number;
    status?: ChannelStatus;
  }): Promise<ChannelSummary[]> {
    return getChannelList(this.tenantId, this.projectId, options);
  }

  async getConversationMembers(conversationId: string): Promise<ConversationMember[]> {
    return getConversationMembers(this.tenantId, conversationId);
  }

  async getLinkedRequirements(conversationId: string): Promise<LinkedRequirement[]> {
    return getLinkedRequirements(this.tenantId, conversationId);
  }

  async getConversationById(conversationId: string): Promise<ConversationSummaryDto> {
    return getConversationById(this.tenantId, conversationId);
  }

  async getChannelById(channelId: string): Promise<ChannelDetail> {
    return getChannelById(this.tenantId, this.projectId, channelId);
  }

  async createChannel(request: CreateChannelRequest): Promise<ChannelDetail> {
    return createChannel(this.tenantId, this.projectId, request);
  }

  async closeChannel(
    channelId: string,
    request?: CloseChannelRequest
  ): Promise<ChannelSummary> {
    return closeChannel(this.tenantId, this.projectId, channelId, request);
  }

  // Messages
  async getMessages(
    channelId: string,
    options?: { cursor?: string; limit?: number }
  ): Promise<MessagesPage> {
    return getMessages(this.tenantId, this.projectId, channelId, options);
  }

  async getPinnedMessages(
    channelId: string,
    options?: { limit?: number }
  ): Promise<Message[]> {
    return getPinnedMessages(this.tenantId, this.projectId, channelId, options);
  }

  async postMessage(
    channelId: string,
    request: CreateMessageRequest
  ): Promise<Message> {
    return postMessage(this.tenantId, this.projectId, channelId, request);
  }

  // Invite Members
  async getInvitableUsers(conversationId: string, search?: string): Promise<InvitableUser[]> {
    return getInvitableUsers(this.tenantId, conversationId, search);
  }

  async addConversationMembers(conversationId: string, userIds: string[]): Promise<ConversationMember[]> {
    return addConversationMembers(this.tenantId, conversationId, userIds);
  }

  async removeConversationMember(conversationId: string, targetUserId: string): Promise<{ removed: boolean }> {
    return removeConversationMember(this.tenantId, conversationId, targetUserId);
  }

  // Channel Status
  async concludeChannel(conversationId: string, request?: { decisionSpecId?: string }): Promise<ConversationSummaryDto> {
    return concludeChannel(this.tenantId, conversationId, request);
  }

  // Search
  async searchConversations(query: string, limit: number = 15): Promise<ConversationSearchResult[]> {
    return searchConversations(this.tenantId, this.projectId, query, limit);
  }

  // Unread
  async getUnreadCounts(): Promise<UnreadCounts> {
    return getUnreadCounts(this.tenantId, this.projectId);
  }

  async markAsRead(conversationId: string, readUntil?: string): Promise<void> {
    return markAsRead(this.tenantId, conversationId, readUntil);
  }

  // Starred/Muted
  async getStarredConversations(): Promise<ConversationSummaryDto[]> {
    return getStarredConversations(this.tenantId, this.projectId || undefined);
  }

  async toggleStarred(conversationId: string): Promise<{ isStarred: boolean }> {
    return toggleStarred(this.tenantId, conversationId);
  }

  async toggleMuted(conversationId: string): Promise<{ isMuted: boolean }> {
    return toggleMuted(this.tenantId, conversationId);
  }

  async getUserConversationSettings(conversationId: string): Promise<UserConversationSettings> {
    return getUserConversationSettings(this.tenantId, conversationId);
  }

  // DM
  async getDMChannels(): Promise<DirectMessageInfo[]> {
    return getDMChannels(this.tenantId, this.projectId || undefined);
  }

  async getDMChannelsPage(options?: {
    pageNumber?: number;
    page?: number;
    pageSize?: number;
  }): Promise<ConversationPagedResult<DirectMessageInfo>> {
    const pageNumber = normalizePageNumber(options?.pageNumber ?? options?.page ?? 1);
    return getDMChannelsPage(this.tenantId, this.projectId || undefined, {
      pageNumber,
      pageSize: options?.pageSize,
    });
  }

  async getOrCreateDMChannel(targetUserId: string): Promise<ChannelDetail> {
    return getOrCreateDMChannel(this.tenantId, targetUserId, this.projectId || undefined);
  }

  async sendDMInvitation(targetUserId: string): Promise<DirectMessageInvitationResultDto> {
    return sendDMInvitation(this.tenantId, targetUserId, this.projectId || undefined);
  }

  async acceptDMInvitation(notificationId: string): Promise<ConversationSummaryDto> {
    return acceptDMInvitation(this.tenantId, notificationId, this.projectId || undefined);
  }

  async rejectDMInvitation(notificationId: string): Promise<{ message: string }> {
    return rejectDMInvitation(this.tenantId, notificationId, this.projectId || undefined);
  }

  // DM Status
  async concludeDM(dmId: string, request?: { decisionSpecId?: string }): Promise<ConversationSummaryDto> {
    return concludeDM(this.tenantId, dmId, request);
  }

  // Conversations
  async getConversations(): Promise<ConversationSummaryDto[]> {
    return getConversations(this.tenantId, this.projectId);
  }

  async getConversationsPage(options?: {
    pageNumber?: number;
    page?: number;
    pageSize?: number;
  }): Promise<ConversationPagedResult<ConversationSummaryDto>> {
    const pageNumber = normalizePageNumber(options?.pageNumber ?? options?.page ?? 1);
    return getConversationsPage(this.tenantId, this.projectId, {
      pageNumber,
      pageSize: options?.pageSize,
    });
  }

  /**
   * Get global (tenant-wide) conversations
   */
  async getGlobalConversations(): Promise<ConversationSummaryDto[]> {
    return getGlobalConversations(this.tenantId);
  }

  async getGlobalConversationsPage(options?: {
    pageNumber?: number;
    page?: number;
    pageSize?: number;
  }): Promise<ConversationPagedResult<ConversationSummaryDto>> {
    const pageNumber = normalizePageNumber(options?.pageNumber ?? options?.page ?? 1);
    return getGlobalConversationsPage(this.tenantId, {
      pageNumber,
      pageSize: options?.pageSize,
    });
  }

  /**
   * Create a new conversation
   */
  async createConversation(request: CreateConversationRequest): Promise<ConversationSummaryDto> {
    return createConversation(this.tenantId, this.projectId, request);
  }

  /**
   * Create a global (tenant-wide) conversation
   */
  async createGlobalConversation(request: CreateConversationRequest): Promise<ConversationSummaryDto> {
    return createConversation(this.tenantId, this.projectId, {
      ...request,
      scope: 'TenantWide',
    });
  }

  async getConversationMessages(
    conversationId: string,
    options?: { cursor?: string; limit?: number }
  ): Promise<ConversationMessagesPage> {
    return getConversationMessages(this.tenantId, this.projectId, conversationId, options);
  }

  async getPinnedConversationMessages(
    conversationId: string,
    options?: { limit?: number }
  ): Promise<ConversationMessageDto[]> {
    return getPinnedConversationMessages(this.tenantId, this.projectId, conversationId, options);
  }

  async postConversationMessage(
    conversationId: string,
    request: CreateMessageRequest
  ): Promise<ConversationMessageDto> {
    return postConversationMessage(this.tenantId, this.projectId, conversationId, request);
  }

  async editConversationMessage(
    conversationId: string,
    messageId: string,
    content: string
  ): Promise<ConversationMessageDto> {
    return editConversationMessage(this.tenantId, this.projectId, conversationId, messageId, content);
  }

  async deleteConversationMessage(
    conversationId: string,
    messageId: string
  ): Promise<void> {
    return deleteConversationMessage(this.tenantId, this.projectId, conversationId, messageId);
  }

  // Forum Topics
  async getTopics(
    conversationId: string,
    options?: {
      page?: number;
      pageSize?: number;
      status?: TopicStatus;
    }
  ): Promise<TopicsPageDto> {
    return getTopics(this.tenantId, conversationId, options);
  }

  async getTopicById(
    conversationId: string,
    topicId: string
  ): Promise<TopicDetailDto> {
    return getTopicById(this.tenantId, conversationId, topicId);
  }

  async createTopic(
    conversationId: string,
    request: CreateTopicRequest
  ): Promise<TopicDetailDto> {
    return createTopic(this.tenantId, conversationId, request);
  }

  async updateTopic(
    conversationId: string,
    topicId: string,
    request: UpdateTopicRequest
  ): Promise<TopicDto> {
    return updateTopic(this.tenantId, conversationId, topicId, request);
  }

  async closeTopic(
    conversationId: string,
    topicId: string,
    request?: CloseTopicRequest
  ): Promise<TopicDto> {
    return closeTopic(this.tenantId, conversationId, topicId, request);
  }

  async reopenTopic(
    conversationId: string,
    topicId: string
  ): Promise<TopicDto> {
    return reopenTopic(this.tenantId, conversationId, topicId);
  }

  async toggleTopicPin(
    conversationId: string,
    topicId: string
  ): Promise<{ isPinned: boolean }> {
    return toggleTopicPin(this.tenantId, conversationId, topicId);
  }

  async toggleTopicLock(
    conversationId: string,
    topicId: string
  ): Promise<{ isLocked: boolean }> {
    return toggleTopicLock(this.tenantId, conversationId, topicId);
  }

  async getTopicMessages(
    conversationId: string,
    topicId: string,
    options?: {
      cursor?: string;
      limit?: number;
    }
  ): Promise<ForumMessagesPageDto> {
    return getTopicMessages(this.tenantId, conversationId, topicId, options);
  }

  async postTopicMessage(
    conversationId: string,
    topicId: string,
    request: CreateMessageRequest
  ): Promise<ForumMessageDto> {
    return postTopicMessage(this.tenantId, conversationId, topicId, request);
  }
}

// Singleton instance
let conversationServiceInstance: ConversationService | null = null;

/**
 * Get the singleton ConversationService instance
 */
export function getConversationService(): ConversationService {
  if (!conversationServiceInstance) {
    conversationServiceInstance = new ConversationService();
  }
  return conversationServiceInstance;
}

/**
 * Reset the singleton instance (for testing)
 */
export function resetConversationService(): void {
  conversationServiceInstance = null;
}
