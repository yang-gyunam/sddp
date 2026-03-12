/**
 * Conversation Entry Types
 * UI
 */

import type {
  ConversationScope,
  ConversationType,
  ConversationVisibility,
} from './conversation-taxonomy.types';
import type { UserRef } from '../../shared/types';

// Conversation Member Role
export type ConversationMemberRole = 'Member' | 'Moderator' | 'Owner';

// Canonical Topic Status (UI prototype)
export type TopicStatus = 'Open' | 'Closed';

// Channel Status (matches backend ChannelStatus enum)
export type ChannelEntryStatus = 'Active' | 'Concluded';

// Canonical conversation model (UI layer)
export interface ConversationEntry {
  id: string;
  projectId?: string | null;  // null = tenant-wide (global) conversation
  name: string;
  type: ConversationType;
  visibility?: ConversationVisibility;
  scope?: ConversationScope;
  description?: string;
  isPrivate: boolean;
  unreadCount?: number;
  channelStatus?: ChannelEntryStatus | null;
  decisionSpecId?: string | null;
}

// Canonical topic (Forum unit)
export interface Topic {
  id: string;
  forumId: string;
  title: string;
  author: UserRef;
  status: TopicStatus;
  isPinned: boolean;
  isLocked: boolean;
  decisionSpecId?: string;
  messageCount: number;
  createdAt: string;
  updatedAt: string;
  lastReplyAt?: string;  // Optional - for UI display
}

// Direct Message (1:1)
export interface DirectMessage {
  id: string;
  otherUserId?: string;
  name: string;
  lastMessage?: string;
  lastActiveAt?: string;
  unreadCount?: number;
  isOnline?: boolean;
  channelStatus?: ChannelEntryStatus | null;
}
