/**
 * Conversation Type Definitions
 * Core types for the conversation system (Channel, Forum, DirectMessage)
 * Based on the Conversation Taxonomy spec.
 *
 * Channels/Topics are unified under the Conversation entity (conversations table).
 */

// ============================================
// Enums (matching backend)
// ============================================

import type { ConversationType } from './conversation-taxonomy.types';
import type { UserRef } from '../../shared/types';

export type ChannelStatus = 'Active' | 'Concluded';

export type MemberType = 'Human' | 'AI';

export type MessageType =
  | 'Normal'
  | 'Proposal'
  | 'Question'
  | 'Objection'
  | 'Reference'
  | 'Decision'
  | 'AiReminder'
  | 'AiSummary'
  | 'AiSuggestion';

// ============================================
// DTOs (matching backend response)
// ============================================

/**
 * Member of a conversation
 */
export interface ConversationMember {
  id: string;
  user: UserRef;
  type: MemberType;
  role: string;
  joinedAt: string;
  isActive: boolean;
}

/**
 * Message in a conversation
 */
export interface Message {
  id: string;
  conversationId: string;
  topicId?: string;
  sender: UserRef;
  type: MessageType;
  content: string;
  references: string[] | null;
  replyToId: string | null;
  createdAt: string;
  isEdited: boolean;
}

/**
 * Channel summary (list view)
 * Represents a conversation channel summary
 */
export interface ChannelSummary {
  id: string;
  tenantId?: string;
  projectId?: string;
  topic: string;
  title?: string;
  status: ChannelStatus;
  participantCount: number;
  messageCount: number;
  createdAt: string;
  concludedAt: string | null;
  updatedAt?: string;
}

/**
 * Channel detail (with members)
 * Note: messages are loaded separately via getMessages API
 */
export interface ChannelDetail extends ChannelSummary {
  participants: ConversationMember[];
}

/**
 * Paginated messages response
 */
export interface MessagesPage {
  messages: Message[];
  nextCursor: string | null;
  hasMore: boolean;
  totalCount: number;
}

// ============================================
// Request DTOs
// ============================================

export interface CreateChannelRequest {
  topic: string;
}

export interface CreateMessageRequest {
  type: MessageType;
  content: string;
  references?: string[];
  replyToId?: string;
}

export interface CloseChannelRequest {
  decisionSpecId?: string;
}

// ============================================
// Unread/Starred/DM DTOs
// ============================================

/**
 * Conversation unread count
 */
export interface ConversationUnread {
  conversationId: string;
  topic: string;
  unreadCount: number;
  lastMessageAt: string | null;
}

/**
 * Unread counts response
 */
export interface UnreadCounts {
  totalUnread: number;
  byConversation: ConversationUnread[];
}

/**
 * User conversation settings
 */
export interface UserConversationSettings {
  conversationId: string;
  isStarred: boolean;
  isMuted: boolean;
  lastReadAt: string | null;
}

/**
 * DM info
 */
export interface DirectMessageInfo {
  id: string;
  otherUser: UserRef;
  unreadCount: number;
  lastMessage: Message | null;
  createdAt: string;
  updatedAt: string;
  status?: 'Active' | 'Concluded' | null;
}

// ============================================
// SignalR Events
// ============================================

export interface UserJoinedEvent {
  userId: string;
  timestamp: string;
}

export interface UserLeftEvent {
  userId: string;
  timestamp: string;
}

export interface UserTypingEvent {
  userId: string;
  isTyping: boolean;
  timestamp: string;
}

export interface ConversationClosedEvent {
  conversationId: string;
  decisionSpecId: string | null;
  concludedBy?: string | null;
  timestamp: string;
}

export interface DMConcludedEvent {
  conversationId: string;
  concludedBy: string;
  timestamp: string;
}

export interface MemberJoinedEvent {
  conversationId: string;
  userId: string;
  displayName: string;
  timestamp: string;
}

export interface MemberRemovedEvent {
  conversationId: string;
  removedUserId: string;
  removedUserName: string;
  timestamp: string;
}

export interface ProjectChannelCreatedEvent {
  conversationId: string;
  projectId: string;
  channelName: string;
  createdByUserId: string;
  timestamp: string;
}

export interface ConversationInvitationEvent {
  conversationId: string;
  conversationName: string;
  inviterUserId: string;
  inviterName: string;
  projectId?: string | null;
  timestamp: string;
}

export interface InvitationResponseEvent {
  conversationId: string;
  responderUserId: string;
  responderName: string;
  accepted: boolean;
  timestamp: string;
}

export interface UserOnlineEvent {
  userId: string;
  timestamp: string;
}

export interface UserOfflineEvent {
  userId: string;
  timestamp: string;
}

export interface PresenceStateEvent {
  onlineUserIds: string[];
  timestamp: string;
}

// ============================================
// Store State
// ============================================

export interface ConversationStoreState {
  // List of channels
  channels: ChannelSummary[];
  channelsLoading: boolean;
  channelsError: string | null;

  // Current channel
  currentChannel: ChannelDetail | null;
  currentChannelLoading: boolean;
  currentChannelError: string | null;

  // Messages
  messages: Message[];
  messagesLoading: boolean;
  messagesError: string | null;
  nextMessageCursor: string | null;
  hasMoreMessages: boolean;

  // Real-time state
  typingUsers: Map<string, string>; // userId -> displayName
  onlineUsers: Set<string>; // userIds

  // Connection state
  hubConnected: boolean;
  hubConnectionError: string | null;
}

// ============================================
// UI Types
// ============================================

export interface MessageGroup {
  date: string;
  messages: Message[];
}

export interface MessageTypeStyle {
  bgColor: string;
  borderColor: string;
  textColor: string;
  icon: string;
}

/**
 * Bubble width tier for message layout regulation.
 * Prevents uneven horizontal lengths across message types.
 */
export type BubbleWidthTier = 'compact' | 'standard' | 'wide';

/**
 * Maps each message type to a bubble width tier.
 * - compact (55%): Conversational messages
 * - standard (72%): Structured opinions, decisions
 * - wide (88%): AI analysis, long-form content
 */
export const MESSAGE_TYPE_WIDTH: Record<MessageType, BubbleWidthTier> = {
  Normal: 'compact',
  Question: 'compact',
  Proposal: 'standard',
  Objection: 'standard',
  Reference: 'standard',
  Decision: 'standard',
  AiReminder: 'wide',
  AiSummary: 'wide',
  AiSuggestion: 'wide',
};

export const MESSAGE_TYPE_STYLES: Record<MessageType, MessageTypeStyle> = {
  Normal: {
    bgColor: 'bg-[var(--color-bg-secondary)]',
    borderColor: 'border-transparent',
    textColor: 'text-[var(--color-text-primary)]',
    icon: 'message-circle',
  },
  Proposal: {
    bgColor: 'bg-[var(--color-bg-secondary)]',
    borderColor: 'border-[var(--color-accent-primary)]',
    textColor: 'text-[var(--color-accent-primary)]',
    icon: 'lightbulb',
  },
  Question: {
    bgColor: 'bg-[var(--color-bg-secondary)]',
    borderColor: 'border-[var(--color-warning-500)]',
    textColor: 'text-[var(--color-warning-700)]',
    icon: 'help-circle',
  },
  Objection: {
    bgColor: 'bg-[var(--color-bg-secondary)]',
    borderColor: 'border-[var(--color-error-500)]',
    textColor: 'text-[var(--color-error-600)]',
    icon: 'alert-triangle',
  },
  Reference: {
    bgColor: 'bg-[var(--color-bg-secondary)]',
    borderColor: 'border-[var(--color-info-500)]',
    textColor: 'text-[var(--color-info-600)]',
    icon: 'link',
  },
  Decision: {
    bgColor: 'bg-[var(--color-bg-secondary)]',
    borderColor: 'border-[var(--color-success-500)]',
    textColor: 'text-[var(--color-success-600)]',
    icon: 'check-circle',
  },
  AiReminder: {
    bgColor: 'bg-[var(--color-bg-secondary)]',
    borderColor: 'border-[var(--color-border-secondary)]',
    textColor: 'text-[var(--color-text-secondary)]',
    icon: 'bell',
  },
  AiSummary: {
    bgColor: 'bg-[var(--color-bg-secondary)]',
    borderColor: 'border-[var(--color-border-secondary)]',
    textColor: 'text-[var(--color-text-secondary)]',
    icon: 'file-text',
  },
  AiSuggestion: {
    bgColor: 'bg-[var(--color-bg-secondary)]',
    borderColor: 'border-[var(--color-border-secondary)]',
    textColor: 'text-[var(--color-text-secondary)]',
    icon: 'sparkles',
  },
};

/**
 * Human-only message types (AI cannot use these)
 */
export const HUMAN_MESSAGE_TYPES: MessageType[] = [
  'Normal',
  'Proposal',
  'Question',
  'Objection',
  'Reference',
  'Decision',
];

/**
 * AI-only message types (Human cannot use these)
 */
export const AI_MESSAGE_TYPES: MessageType[] = ['AiReminder', 'AiSummary', 'AiSuggestion'];

// ============================================
// Linked Requirements
// ============================================

/**
 * Requirement linked to a conversation (from GET /conversations/{id}/linked-requirements)
 */
export interface LinkedRequirement {
  id: string;
  code: string;
  title: string;
  level: string;
  priority: string;
  linkedAt: string;
}

// ============================================
// Search DTOs
// ============================================

/**
 * Lightweight conversation search result (from GET /conversations/search)
 */
export interface ConversationSearchResult {
  id: string;
  name: string;
  description: string | null;
  conversationType: ConversationType;
}
