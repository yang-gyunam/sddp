/**
 * Conversation Store - Conversation state management
 * Manages channel/conversation state for the conversation system.
 */

import { createStore, type Store } from '@sddp/shell/core';
import type {
  ChannelSummary,
  ChannelDetail,
  ConversationStoreState,
  Message,
  MessagesPage,
} from '../types';

// ============================================
// Initial State
// ============================================

const initialState: ConversationStoreState = {
  // Channels list
  channels: [],
  channelsLoading: false,
  channelsError: null,

  // Current channel
  currentChannel: null,
  currentChannelLoading: false,
  currentChannelError: null,

  // Messages
  messages: [],
  messagesLoading: false,
  messagesError: null,
  nextMessageCursor: null,
  hasMoreMessages: false,

  // Real-time state
  typingUsers: new Map(),
  onlineUsers: new Set(),

  // Connection state
  hubConnected: false,
  hubConnectionError: null,
};

// Create the store
const conversationStoreInstance: Store<ConversationStoreState> = createStore<ConversationStoreState>(initialState);

// ============================================
// Channel List Actions
// ============================================

/**
 * Set channels list
 */
export function setChannels(channels: ChannelSummary[]): void {
  conversationStoreInstance.update((state) => ({
    ...state,
    channels,
    channelsLoading: false,
    channelsError: null,
  }));
}

/**
 * Set channels loading state
 */
export function setChannelsLoading(loading: boolean): void {
  conversationStoreInstance.update((state) => ({
    ...state,
    channelsLoading: loading,
  }));
}

/**
 * Set channels error
 */
export function setChannelsError(error: string | null): void {
  conversationStoreInstance.update((state) => ({
    ...state,
    channelsError: error,
    channelsLoading: false,
  }));
}

/**
 * Add a new channel to the list
 */
export function addChannel(channel: ChannelSummary): void {
  conversationStoreInstance.update((state) => ({
    ...state,
    channels: [channel, ...state.channels],
  }));
}

/**
 * Update a channel in the list
 */
export function updateChannel(channelId: string, updates: Partial<ChannelSummary>): void {
  conversationStoreInstance.update((state) => ({
    ...state,
    channels: state.channels.map((d) =>
      d.id === channelId ? { ...d, ...updates } : d
    ),
  }));
}

/**
 * Remove a channel from the list
 */
export function removeChannel(channelId: string): void {
  conversationStoreInstance.update((state) => ({
    ...state,
    channels: state.channels.filter((d) => d.id !== channelId),
  }));
}

// ============================================
// Current Channel Actions
// ============================================

/**
 * Set current channel (detail view)
 */
export function setCurrentChannel(channel: ChannelDetail | null): void {
  conversationStoreInstance.update((state) => ({
    ...state,
    currentChannel: channel,
    currentChannelLoading: false,
    currentChannelError: null,
    // Reset messages when changing channel (messages are loaded separately)
    messages: [],
    nextMessageCursor: null,
    hasMoreMessages: true,
  }));
}

/**
 * Set current channel loading state
 */
export function setCurrentChannelLoading(loading: boolean): void {
  conversationStoreInstance.update((state) => ({
    ...state,
    currentChannelLoading: loading,
  }));
}

/**
 * Set current channel error
 */
export function setCurrentChannelError(error: string | null): void {
  conversationStoreInstance.update((state) => ({
    ...state,
    currentChannelError: error,
    currentChannelLoading: false,
  }));
}

/**
 * Clear current channel
 */
export function clearCurrentChannel(): void {
  conversationStoreInstance.update((state) => ({
    ...state,
    currentChannel: null,
    currentChannelLoading: false,
    currentChannelError: null,
    messages: [],
    nextMessageCursor: null,
    hasMoreMessages: false,
    typingUsers: new Map(),
  }));
}

// ============================================
// Messages Actions
// ============================================

/**
 * Set messages (initial load or page load)
 */
export function setMessages(page: MessagesPage, append: boolean = false): void {
  conversationStoreInstance.update((state) => ({
    ...state,
    messages: append
      ? [...state.messages, ...page.messages]
      : page.messages,
    messagesLoading: false,
    messagesError: null,
    nextMessageCursor: page.nextCursor,
    hasMoreMessages: page.hasMore,
  }));
}

/**
 * Set messages loading state
 */
export function setMessagesLoading(loading: boolean): void {
  conversationStoreInstance.update((state) => ({
    ...state,
    messagesLoading: loading,
  }));
}

/**
 * Set messages error
 */
export function setMessagesError(error: string | null): void {
  conversationStoreInstance.update((state) => ({
    ...state,
    messagesError: error,
    messagesLoading: false,
  }));
}

/**
 * Add a new message (real-time)
 */
export function addMessage(message: Message): void {
  conversationStoreInstance.update((state) => {
    // Check if message already exists (prevent duplicates)
    if (state.messages.some((m) => m.id === message.id)) {
      return state;
    }

    // Remove typing indicator for the sender
    const newTypingUsers = new Map(state.typingUsers);
    newTypingUsers.delete(message.sender?.id ?? '');

    return {
      ...state,
      messages: [...state.messages, message],
      typingUsers: newTypingUsers,
    };
  });
}

/**
 * Update an existing message
 */
export function updateMessage(messageId: string, updates: Partial<Message>): void {
  conversationStoreInstance.update((state) => ({
    ...state,
    messages: state.messages.map((m) =>
      m.id === messageId ? { ...m, ...updates } : m
    ),
  }));
}

/**
 * Remove a message
 */
export function removeMessage(messageId: string): void {
  conversationStoreInstance.update((state) => ({
    ...state,
    messages: state.messages.filter((m) => m.id !== messageId),
  }));
}

// ============================================
// Real-time State Actions
// ============================================

/**
 * Set user typing state
 */
export function setUserTyping(userId: string, displayName: string, isTyping: boolean): void {
  conversationStoreInstance.update((state) => {
    const newTypingUsers = new Map(state.typingUsers);
    if (isTyping) {
      newTypingUsers.set(userId, displayName);
    } else {
      newTypingUsers.delete(userId);
    }
    return {
      ...state,
      typingUsers: newTypingUsers,
    };
  });
}

/**
 * Clear all typing users (when leaving space)
 */
export function clearTypingUsers(): void {
  conversationStoreInstance.update((state) => ({
    ...state,
    typingUsers: new Map(),
  }));
}

/**
 * Add online user
 */
export function addOnlineUser(userId: string): void {
  conversationStoreInstance.update((state) => {
    const newOnlineUsers = new Set(state.onlineUsers);
    newOnlineUsers.add(userId);
    return {
      ...state,
      onlineUsers: newOnlineUsers,
    };
  });
}

/**
 * Remove online user
 */
export function removeOnlineUser(userId: string): void {
  conversationStoreInstance.update((state) => {
    const newOnlineUsers = new Set(state.onlineUsers);
    newOnlineUsers.delete(userId);
    // Also remove from typing users
    const newTypingUsers = new Map(state.typingUsers);
    newTypingUsers.delete(userId);
    return {
      ...state,
      onlineUsers: newOnlineUsers,
      typingUsers: newTypingUsers,
    };
  });
}

/**
 * Set online users (batch)
 */
export function setOnlineUsers(userIds: string[]): void {
  conversationStoreInstance.update((state) => ({
    ...state,
    onlineUsers: new Set(userIds),
  }));
}

// ============================================
// Hub Connection State
// ============================================

/**
 * Set hub connection state
 */
export function setHubConnected(connected: boolean): void {
  conversationStoreInstance.update((state) => ({
    ...state,
    hubConnected: connected,
    hubConnectionError: connected ? null : state.hubConnectionError,
  }));
}

/**
 * Set hub connection error
 */
export function setHubConnectionError(error: string | null): void {
  conversationStoreInstance.update((state) => ({
    ...state,
    hubConnectionError: error,
    hubConnected: false,
  }));
}

// ============================================
// Getters
// ============================================

/**
 * Get current state
 */
export function getConversationStoreState(): ConversationStoreState {
  return conversationStoreInstance.get();
}

/**
 * Get channels list
 */
export function getChannels(): ChannelSummary[] {
  return conversationStoreInstance.get().channels;
}

/**
 * Get current channel
 */
export function getCurrentChannel(): ChannelDetail | null {
  return conversationStoreInstance.get().currentChannel;
}

/**
 * Get messages for current channel
 */
export function getMessages(): Message[] {
  return conversationStoreInstance.get().messages;
}

/**
 * Get typing users display string
 */
export function getTypingUsersDisplay(): string {
  const { typingUsers } = conversationStoreInstance.get();
  const names = Array.from(typingUsers.values());
  if (names.length === 0) return '';
  if (names.length === 1) return `${names[0]} is typing...`;
  if (names.length === 2) return `${names[0]} and ${names[1]} are typing...`;
  return `${names[0]} and ${names.length - 1} others are typing...`;
}

/**
 * Check if hub is connected
 */
export function isHubConnected(): boolean {
  return conversationStoreInstance.get().hubConnected;
}

// ============================================
// Subscribe
// ============================================

/**
 * Subscribe to conversation state changes
 */
export function subscribeConversation(
  listener: (state: ConversationStoreState, prevState: ConversationStoreState) => void
): () => void {
  return conversationStoreInstance.subscribe(listener);
}

/**
 * Reset conversation store
 */
export function resetConversationStore(): void {
  conversationStoreInstance.reset();
}

// Export the store for direct access
export { conversationStoreInstance };

// Canonical alias
export const conversationStore = conversationStoreInstance;

// Re-export types for convenience
export type { ConversationStoreState } from '../types';
