// @sddp/activities/conversations - Conversations Activity
//
// :
//   conversations/
// ├── types/ #
// ├── services/ # API
// ├── stores/ # status
// └── components/ #
// ├── idioms/ #
// ├── sections/ #
// └── pages/ #
//
// Pages (UI Definition):
// - GlobalConversationsPage (ACT-CONV-001)

// Types (explicit exports to avoid naming conflicts with components)
export type {
  // Channel types (API DTOs)
  Message,
  MessageType,
  MessagesPage,
  CreateMessageRequest,
  MessageGroup,
  MessageTypeStyle,
  // Conversation entry types (UI prototype)
  ConversationMemberRole,
  TopicStatus,
  ConversationEntry,
  Topic,
  DirectMessage,
  ConversationType,
  ConversationVisibility,
  ConversationScope,
  // Activity conversation types
  ConversationCategory,
  ConversationSummary,
  ProjectConversationGroup as ProjectConversationGroupType, // Rename to avoid conflict with component
  DirectMessageSummary as DirectMessageSummaryType, // Rename to avoid conflict with component
  ConversationFilterType,
  ConversationSidebarState,
} from './types';

export {
  MESSAGE_TYPE_STYLES,
  HUMAN_MESSAGE_TYPES,
  AI_MESSAGE_TYPES,
  CONVERSATION_CATEGORY_ICONS,
  CONVERSATION_FILTER_LABELS,
} from './types';

// Services (API functions - use explicit imports to avoid conflicts with store getters)
export {
  // API functions
  getChannelList as fetchChannels,
  getChannelById as fetchChannelById,
  createChannel,
  closeChannel,
  getMessages as fetchMessages,
  getPinnedMessages,
  postMessage,
  getUnreadCounts,
  markAsRead,
  getStarredConversations as fetchStarredConversations,
  toggleStarred,
  toggleMuted,
  getUserConversationSettings,
  getDMChannels,
  getOrCreateDMChannel,
  getConversations,
  createConversation,
  getConversationMessages,
  getPinnedConversationMessages,
  postConversationMessage,
  // Conversion helpers
  convertToConversationEntry,
  // Service class
  ConversationService,
  getConversationService,
  resetConversationService,
} from './services';

// Hub Service
export * from './services/ConversationHubService';

// Stores (state management)
export {
  setActiveConversationContext,
  clearActiveConversationContext,
  getActiveConversationContext,
  subscribeActiveConversationContext,
  resetActiveConversationContextStore,
} from './stores/active-conversation-context.store';

export {
  // Conversation store (channels)
  setChannels,
  setChannelsLoading,
  setChannelsError,
  addChannel,
  updateChannel,
  removeChannel,
  setCurrentChannel,
  setCurrentChannelLoading,
  setCurrentChannelError,
  clearCurrentChannel,
  setMessages,
  setMessagesLoading,
  setMessagesError,
  addMessage,
  updateMessage,
  removeMessage,
  setUserTyping,
  clearTypingUsers,
  addOnlineUser,
  removeOnlineUser,
  setOnlineUsers,
  setHubConnected,
  setHubConnectionError,
  getConversationStoreState,
  getChannels,
  getCurrentChannel,
  getMessages,
  getTypingUsersDisplay,
  isHubConnected,
  subscribeConversation,
  resetConversationStore,
  conversationStoreInstance,
  conversationStore,
} from './stores';

// Conversation view store
export * from './stores/conversation-view.store';

// Conversation draft store
export * from './stores/conversation-draft.store';

// Conversation sidebar store (explicit to avoid setChannels conflict)
export {
  setProjectGroups,
  setChannels as setSidebarChannels,
  setPrivateChannels,
  setSidebarTopics,
  setFlatConversations,
  toggleSectionExpanded,
  toggleProjectExpanded,
  setProjectExpanded,
  updateConversationInGroup,
  setDirectMessages,
  updateDirectMessage,
  removeConversation,
  removeDirectMessage,
  setDMOnlineStatus,
  setStarredConversations,
  toggleConversationStarred,
  setSelectedConversation,
  clearSelection,
  setSearchQuery,
  clearSearchQuery,
  setFilterType,
  setLoading,
  setError,
  getSidebarState,
  getProjectGroups,
  getFilteredProjectGroups,
  getDirectMessages,
  getFilteredDirectMessages,
  getStarredConversations,
  getSidebarTopics,
  getSelectedConversationId,
  getSelectedConversation,
  isProjectExpanded,
  getTotalUnreadCount,
  subscribeSidebar,
  resetSidebarStore,
} from './stores/conversation-sidebar.store';

// Actions (sidebar data loading)
export { loadGlobalConversationsSidebar } from './actions';

// Components (explicit exports)
export {
  // Idioms
  MessageTypeIcon,
  MessageBubble,
  MessageInput,
  TypingIndicator,
  DirectMessageSummary,
  ConversationItem,
  ConversationHeader,
  ProjectConversationGroup,
  // Sections
  ConversationSidebar,
  ConversationSidebarPanel,
  MessageStream,
  ParticipantsPanel,
  MessageList,
  ParticipantList,
  DirectMessageList,
  ChannelView,
  ForumView,
  TopicView,
  // Pages
  GlobalConversationsPage,
  TopicPage,
} from './components';

// Activity Root
export { default as ConversationActivity } from './components/ConversationActivity.svelte';

// Activity ID
export const CONVERSATIONS_ACTIVITY_ID = 'conversations';
