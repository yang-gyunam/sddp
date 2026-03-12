/**
 * Conversation Activity Type Definitions
 * Activity-specific types for the global conversations view
 *
 * Note: Core conversation types (ChannelSummary, Message, etc.) are re-exported
 * from index.ts directly from conversation.types.ts.
 * This file only contains activity-specific types.
 */

// ============================================
// Conversation Category Types (Activity-specific)
// ============================================

/**
 * Conversation category for sidebar display
 */
export type ConversationCategory = 'public' | 'private' | 'dm';

/**
 * Conversation summary for sidebar display
 */
export interface ConversationSummary {
  id: string;
  name: string;
  type: ConversationCategory;
  projectId?: string | null;      // null = tenant-wide (global) conversation
  projectName?: string | null;
  channelStatus?: 'Active' | 'Concluded' | null;
  unreadCount: number;
  hasUnreadMentions: boolean;
  lastMessageAt: string | null;
  starred: boolean;
  muted: boolean;
}

/**
 * Project conversation group for sidebar
 */
export interface ProjectConversationGroup {
  projectId: string;
  projectName: string;
  projectCode: string;
  conversations: ConversationSummary[];
  unreadCount: number;
  expanded: boolean;
}

/**
 * Direct message summary
 */
export interface DirectMessageSummary {
  id: string;
  participantId: string;
  participantName: string;
  participantAvatar?: string;
  channelStatus?: 'Active' | 'Concluded' | null;
  isOnline: boolean;
  lastMessage?: string;
  lastMessageAt?: string;
  unreadCount: number;
}

// ============================================
// Sidebar State Types
// ============================================

/**
 * Sidebar filter type
 */
export type ConversationFilterType = 'recent' | 'unread' | 'starred' | 'all';

/**
 * Sidebar state for flat structure (Conversations Activity)
 */
export interface ConversationSidebarState {
  // Search and filter
  searchQuery: string;
  filterType: ConversationFilterType;

  // UI state
  expandedSections: Set<string>;  // 'channels' | 'private' | 'topics' | 'dm' | 'starred'
  expandedProjects: Set<string>;  // Legacy: for project-grouped view
  selectedConversationId: string | null;

  // Data (flat structure for Conversations Activity)
  channels: ConversationSummary[];        // public channels (type: 'public')
  privateChannels: ConversationSummary[];    // private channels (type: 'private')
  topics: ConversationSummary[];          // forum topics
  directMessages: DirectMessageSummary[];
  starredConversations: ConversationSummary[];

  // Legacy: project-grouped data (for Projects Activity -> Conversations Tab)
  projectGroups: ProjectConversationGroup[];

  // Loading
  loading: boolean;
  error: string | null;
}

// ============================================
// Style Constants
// ============================================

export const CONVERSATION_CATEGORY_ICONS: Record<ConversationCategory, string> = {
  public: 'hash',
  private: 'lock',
  dm: 'message-circle',
};

export const CONVERSATION_FILTER_LABELS: Record<ConversationFilterType, string> = {
  recent: 'Recent Activity',
  unread: 'Unread Only',
  starred: 'Starred',
  all: 'All Conversations',
};
