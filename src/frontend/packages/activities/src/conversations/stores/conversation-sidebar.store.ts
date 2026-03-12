/**
 * Conversation Sidebar Store - Sidebar State Management
 * Manages project groups, conversations, DMs, and sidebar UI state
 */

import { createStore, type Store } from '@sddp/shell/core';
import type {
  ConversationSidebarState,
  ProjectConversationGroup,
  ConversationSummary,
  DirectMessageSummary,
  ConversationFilterType,
} from '../types';
import { toggleStarred, toggleMuted } from '../services/ConversationService';
import { getAuthState } from '@sddp/shell/auth/stores';

// ============================================
// Initial State
// ============================================

const initialState: ConversationSidebarState = {
  // Search and filter
  searchQuery: '',
  filterType: 'recent',

  // UI state
  expandedSections: new Set<string>(['channels', 'private', 'topics', 'dm']),
  expandedProjects: new Set<string>(),
  selectedConversationId: null,

  // Data (flat structure for Conversations Activity)
  channels: [],
  privateChannels: [],
  topics: [],
  directMessages: [],
  starredConversations: [],

  // Legacy: project-grouped data
  projectGroups: [],

  // Loading
  loading: false,
  error: null,
};

// Create the store
const sidebarStore: Store<ConversationSidebarState> = createStore<ConversationSidebarState>(initialState);

// ============================================
// Project Groups Actions
// ============================================

/**
 * Set project groups
 */
export function setProjectGroups(groups: ProjectConversationGroup[]): void {
  sidebarStore.update((state) => ({
    ...state,
    projectGroups: groups,
    loading: false,
    error: null,
  }));
}

// ============================================
// Flat Structure Actions (for Global Conversations)
// ============================================

/**
 * Set channels (public conversations)
 */
export function setChannels(channels: ConversationSummary[]): void {
  sidebarStore.update((state) => ({
    ...state,
    channels,
  }));
}

/**
 * Set private channels
 */
export function setPrivateChannels(privateChannels: ConversationSummary[]): void {
  sidebarStore.update((state) => ({
    ...state,
    privateChannels,
  }));
}

/**
 * Set topics (forum conversations) for sidebar
 */
export function setSidebarTopics(topics: ConversationSummary[]): void {
  sidebarStore.update((state) => ({
    ...state,
    topics,
  }));
}

/**
 * Set all flat structure data at once
 */
export function setFlatConversations(data: {
  channels: ConversationSummary[];
  privateChannels: ConversationSummary[];
  topics: ConversationSummary[];
}): void {
  sidebarStore.update((state) => ({
    ...state,
    channels: data.channels,
    privateChannels: data.privateChannels,
    topics: data.topics,
    loading: false,
    error: null,
  }));
}

/**
 * Toggle section expanded state
 */
export function toggleSectionExpanded(section: string): void {
  sidebarStore.update((state) => {
    const newExpanded = new Set(state.expandedSections);
    if (newExpanded.has(section)) {
      newExpanded.delete(section);
    } else {
      newExpanded.add(section);
    }
    return {
      ...state,
      expandedSections: newExpanded,
    };
  });
}

/**
 * Toggle project expanded state
 */
export function toggleProjectExpanded(projectId: string): void {
  sidebarStore.update((state) => {
    const newExpanded = new Set(state.expandedProjects);
    if (newExpanded.has(projectId)) {
      newExpanded.delete(projectId);
    } else {
      newExpanded.add(projectId);
    }
    // Also update in projectGroups
    const updatedGroups = state.projectGroups.map((group) =>
      group.projectId === projectId ? { ...group, expanded: newExpanded.has(projectId) } : group
    );
    return {
      ...state,
      expandedProjects: newExpanded,
      projectGroups: updatedGroups,
    };
  });
}

/**
 * Set project expanded state
 */
export function setProjectExpanded(projectId: string, expanded: boolean): void {
  sidebarStore.update((state) => {
    const newExpanded = new Set(state.expandedProjects);
    if (expanded) {
      newExpanded.add(projectId);
    } else {
      newExpanded.delete(projectId);
    }
    const updatedGroups = state.projectGroups.map((group) =>
      group.projectId === projectId ? { ...group, expanded } : group
    );
    return {
      ...state,
      expandedProjects: newExpanded,
      projectGroups: updatedGroups,
    };
  });
}

/**
 * Update conversation in project group
 */
export function updateConversationInGroup(conversationId: string, updates: Partial<ConversationSummary>): void {
  sidebarStore.update((state) => {
    const updatedGroups = state.projectGroups.map((group) => ({
      ...group,
      conversations: group.conversations.map((c) => (c.id === conversationId ? { ...c, ...updates } : c)),
    }));
    return {
      ...state,
      projectGroups: updatedGroups,
    };
  });
}

// ============================================
// Direct Messages Actions
// ============================================

/**
 * Set direct messages
 */
export function setDirectMessages(dms: DirectMessageSummary[]): void {
  sidebarStore.update((state) => ({
    ...state,
    directMessages: dms,
  }));
}

/**
 * Update direct message
 */
export function updateDirectMessage(dmId: string, updates: Partial<DirectMessageSummary>): void {
  sidebarStore.update((state) => ({
    ...state,
    directMessages: state.directMessages.map((dm) => (dm.id === dmId ? { ...dm, ...updates } : dm)),
  }));
}

/**
 * Remove a conversation (channel/forum) from all sidebar lists (e.g. after member removal)
 */
export function removeConversation(conversationId: string): void {
  sidebarStore.update((state) => ({
    ...state,
    channels: state.channels.filter((c) => c.id !== conversationId),
    privateChannels: state.privateChannels.filter((c) => c.id !== conversationId),
    topics: state.topics.filter((c) => c.id !== conversationId),
    starredConversations: state.starredConversations.filter((c) => c.id !== conversationId),
    selectedConversationId: state.selectedConversationId === conversationId ? null : state.selectedConversationId,
  }));
}

/**
 * Remove a direct message from sidebar (e.g. after conclude)
 */
export function removeDirectMessage(dmId: string): void {
  sidebarStore.update((state) => ({
    ...state,
    directMessages: state.directMessages.filter((dm) => dm.id !== dmId),
    selectedConversationId: state.selectedConversationId === dmId ? null : state.selectedConversationId,
  }));
}

/**
 * Set DM online status
 */
export function setDMOnlineStatus(participantId: string, isOnline: boolean): void {
  sidebarStore.update((state) => ({
    ...state,
    directMessages: state.directMessages.map((dm) =>
      dm.participantId === participantId ? { ...dm, isOnline } : dm
    ),
  }));
}

// ============================================
// Starred Conversations Actions
// ============================================

/**
 * Set starred conversations
 */
export function setStarredConversations(conversations: ConversationSummary[]): void {
  sidebarStore.update((state) => ({
    ...state,
    starredConversations: conversations,
  }));
}

/**
 * Toggle conversation starred (optimistic update + API call)
 */
export function toggleConversationStarred(conversationId: string): void {
  sidebarStore.update((state) => {
    const updateFlat = (conversations: ConversationSummary[]) =>
      conversations.map((c) => (c.id === conversationId ? { ...c, starred: !c.starred } : c));

    // Find the conversation in flat structure first, then project groups
    let conversation =
      state.channels.find((c) => c.id === conversationId) ??
      state.privateChannels.find((c) => c.id === conversationId) ??
      state.topics.find((c) => c.id === conversationId);

    if (!conversation) {
      for (const group of state.projectGroups) {
        conversation = group.conversations.find((c) => c.id === conversationId);
        if (conversation) break;
      }
    }

    if (!conversation) return state;

    const isCurrentlyStarred = conversation.starred;
    const nextStarred = !isCurrentlyStarred;

    // Update in project groups
    const updatedGroups = state.projectGroups.map((group) => ({
      ...group,
      conversations: group.conversations.map((c) =>
        c.id === conversationId ? { ...c, starred: nextStarred } : c
      ),
    }));

    // Update in flat structure (Global Conversations page)
    const updatedChannels = updateFlat(state.channels);
    const updatedPrivateChannels = updateFlat(state.privateChannels);
    const updatedTopics = updateFlat(state.topics);

    // Update starred list
    let updatedStarred: ConversationSummary[];
    if (isCurrentlyStarred) {
      updatedStarred = state.starredConversations.filter((c) => c.id !== conversationId);
    } else {
      updatedStarred = [...state.starredConversations, { ...conversation, starred: true }]
        .filter((c, index, self) => self.findIndex((item) => item.id === c.id) === index);
    }

    return {
      ...state,
      projectGroups: updatedGroups,
      channels: updatedChannels,
      privateChannels: updatedPrivateChannels,
      topics: updatedTopics,
      starredConversations: updatedStarred,
    };
  });

  // Fire API call (rollback on failure)
  const tenantId = getAuthState().user?.tenantId;
  if (tenantId) {
    toggleStarred(tenantId, conversationId).catch(() => {
      // Rollback: toggle back on failure
      toggleConversationStarred(conversationId);
    });
  }
}

/**
 * Toggle conversation muted (optimistic update + API call)
 */
export function toggleConversationMuted(conversationId: string): void {
  sidebarStore.update((state) => {
    // Find conversation from any list to determine the single source of truth for current muted value
    let conversation =
      state.channels.find((c) => c.id === conversationId) ??
      state.privateChannels.find((c) => c.id === conversationId) ??
      state.topics.find((c) => c.id === conversationId);

    if (!conversation) {
      for (const group of state.projectGroups) {
        conversation = group.conversations.find((c) => c.id === conversationId);
        if (conversation) break;
      }
    }
    if (!conversation) {
      conversation = state.starredConversations.find((c) => c.id === conversationId);
    }

    if (!conversation) return state;

    const nextMuted = !conversation.muted;
    const setMuted = (conversations: ConversationSummary[]) =>
      conversations.map((c) => (c.id === conversationId ? { ...c, muted: nextMuted } : c));

    const updatedGroups = state.projectGroups.map((group) => ({
      ...group,
      conversations: group.conversations.map((c) =>
        c.id === conversationId ? { ...c, muted: nextMuted } : c
      ),
    }));

    const updatedChannels = setMuted(state.channels);
    const updatedPrivateChannels = setMuted(state.privateChannels);
    const updatedTopics = setMuted(state.topics);
    const updatedStarred = setMuted(state.starredConversations);

    return {
      ...state,
      projectGroups: updatedGroups,
      channels: updatedChannels,
      privateChannels: updatedPrivateChannels,
      topics: updatedTopics,
      starredConversations: updatedStarred,
    };
  });

  // Fire API call (rollback on failure)
  const tenantId = getAuthState().user?.tenantId;
  if (tenantId) {
    toggleMuted(tenantId, conversationId).catch(() => {
      // Rollback: toggle back on failure
      toggleConversationMuted(conversationId);
    });
  }
}

// ============================================
// Selection Actions
// ============================================

/**
 * Set selected conversation
 */
export function setSelectedConversation(conversationId: string | null): void {
  sidebarStore.update((state) => ({
    ...state,
    selectedConversationId: conversationId,
  }));
}

/**
 * Clear selection
 */
export function clearSelection(): void {
  sidebarStore.update((state) => ({
    ...state,
    selectedConversationId: null,
  }));
}

// ============================================
// Search/Filter Actions
// ============================================

/**
 * Set search query
 */
export function setSearchQuery(query: string): void {
  sidebarStore.update((state) => ({
    ...state,
    searchQuery: query,
  }));
}

/**
 * Clear search query
 */
export function clearSearchQuery(): void {
  sidebarStore.update((state) => ({
    ...state,
    searchQuery: '',
  }));
}

/**
 * Set filter type
 */
export function setFilterType(filterType: ConversationFilterType): void {
  sidebarStore.update((state) => ({
    ...state,
    filterType,
  }));
}

// ============================================
// Loading Actions
// ============================================

/**
 * Set loading state
 */
export function setLoading(loading: boolean): void {
  sidebarStore.update((state) => ({
    ...state,
    loading,
  }));
}

/**
 * Set error
 */
export function setError(error: string | null): void {
  sidebarStore.update((state) => ({
    ...state,
    error,
    loading: false,
  }));
}

// ============================================
// Getters
// ============================================

/**
 * Get current state
 */
export function getSidebarState(): ConversationSidebarState {
  return sidebarStore.get();
}

/**
 * Get project groups
 */
export function getProjectGroups(): ProjectConversationGroup[] {
  return sidebarStore.get().projectGroups;
}

/**
 * Get filtered project groups (by search query and filter type)
 */
export function getFilteredProjectGroups(): ProjectConversationGroup[] {
  const state = sidebarStore.get();
  let groups = state.projectGroups;

  // Apply filter type
  if (state.filterType === 'unread') {
    groups = groups
      .map((group) => ({
        ...group,
        conversations: group.conversations.filter((c) => c.unreadCount > 0),
      }))
      .filter((group) => group.conversations.length > 0);
  } else if (state.filterType === 'starred') {
    groups = groups
      .map((group) => ({
        ...group,
        conversations: group.conversations.filter((c) => c.starred),
      }))
      .filter((group) => group.conversations.length > 0);
  }

  // Apply search query
  if (state.searchQuery) {
    const query = state.searchQuery.toLowerCase();
    groups = groups
      .map((group) => ({
        ...group,
        conversations: group.conversations.filter(
          (c) =>
            c.name.toLowerCase().includes(query) ||
            group.projectName.toLowerCase().includes(query)
        ),
      }))
      .filter((group) => group.conversations.length > 0);
  }

  return groups;
}

/**
 * Get direct messages
 */
export function getDirectMessages(): DirectMessageSummary[] {
  return sidebarStore.get().directMessages;
}

/**
 * Get filtered direct messages
 */
export function getFilteredDirectMessages(): DirectMessageSummary[] {
  const state = sidebarStore.get();
  let dms = state.directMessages;

  // Apply filter type
  if (state.filterType === 'unread') {
    dms = dms.filter((dm) => dm.unreadCount > 0);
  }

  // Apply search query
  if (state.searchQuery) {
    const query = state.searchQuery.toLowerCase();
    dms = dms.filter((dm) => dm.participantName.toLowerCase().includes(query));
  }

  return dms;
}

/**
 * Get starred conversations
 */
export function getStarredConversations(): ConversationSummary[] {
  return sidebarStore.get().starredConversations;
}

/**
 * Get forum topics (flat structure)
 */
export function getSidebarTopics(): ConversationSummary[] {
  return sidebarStore.get().topics;
}

/**
 * Get selected conversation ID
 */
export function getSelectedConversationId(): string | null {
  return sidebarStore.get().selectedConversationId;
}

/**
 * Get selected conversation
 * Searches both project groups and flat structure (channels, privateChannels, topics)
 */
export function getSelectedConversation(): ConversationSummary | null {
  const state = sidebarStore.get();
  if (!state.selectedConversationId) return null;

  // Search flat structure first (used by GlobalConversationsPage)
  const flatMatch =
    state.channels.find((c) => c.id === state.selectedConversationId) ??
    state.privateChannels.find((c) => c.id === state.selectedConversationId) ??
    state.topics.find((c) => c.id === state.selectedConversationId);
  if (flatMatch) return flatMatch;

  // DM is represented in a separate list; adapt it to ConversationSummary shape for view selection.
  const dmMatch = state.directMessages.find((dm) => dm.id === state.selectedConversationId);
  if (dmMatch) {
    return {
      id: dmMatch.id,
      name: dmMatch.participantName,
      type: 'dm',
      projectId: null,
      projectName: null,
      channelStatus: dmMatch.channelStatus ?? null,
      unreadCount: dmMatch.unreadCount,
      hasUnreadMentions: false,
      lastMessageAt: dmMatch.lastMessageAt ?? null,
      starred: false,
      muted: false,
    };
  }

  // Search project groups (used by project-scoped views)
  for (const group of state.projectGroups) {
    const conversation = group.conversations.find((c) => c.id === state.selectedConversationId);
    if (conversation) return conversation;
  }
  return null;
}

/**
 * Check if project is expanded
 */
export function isProjectExpanded(projectId: string): boolean {
  return sidebarStore.get().expandedProjects.has(projectId);
}

/**
 * Get total unread count
 */
export function getTotalUnreadCount(): number {
  const state = sidebarStore.get();
  let total = 0;
  for (const group of state.projectGroups) {
    total += group.unreadCount;
  }
  for (const dm of state.directMessages) {
    total += dm.unreadCount;
  }
  return total;
}

// ============================================
// Subscribe
// ============================================

/**
 * Subscribe to sidebar state changes
 */
export function subscribeSidebar(
  listener: (state: ConversationSidebarState, prevState: ConversationSidebarState) => void
): () => void {
  return sidebarStore.subscribe(listener);
}

/**
 * Reset sidebar store
 */
export function resetSidebarStore(): void {
  sidebarStore.reset();
}

// Export the store for direct access
export { sidebarStore };

// Re-export types for convenience
export type { ConversationSidebarState } from '../types';
