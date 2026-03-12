// Conversation Stores
// Explicit re-exports to avoid naming conflicts
// (e.g. setChannels exists in both conversation.store and conversation-sidebar.store)

export {
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
} from './conversation.store';
export type { ConversationStoreState } from './conversation.store';

export {
  setActiveConversationContext,
  clearActiveConversationContext,
  getActiveConversationContext,
  subscribeActiveConversationContext,
  resetActiveConversationContextStore,
} from './active-conversation-context.store';

// Sidebar store - rename setChannels to avoid conflict
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
} from './conversation-sidebar.store';

// View and Draft stores (no conflicts)
export * from './conversation-view.store';
export * from './conversation-draft.store';
