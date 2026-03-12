/**
 * Conversation View Store - UI mode for message rendering
 */

import { createStore, type Store } from '@sddp/shell/core';

export type ConversationViewMode = 'slack' | 'log' | 'document';
export type ConversationViewContext = 'chat' | 'topic';

export interface ConversationViewState {
  chatMode: ConversationViewMode;
  topicMode: ConversationViewMode;
}

const VIEW_STORAGE_KEY = 'sddp-conversation-view';

const initialState: ConversationViewState = {
  chatMode: 'slack',
  topicMode: 'document',
};

const conversationViewStore: Store<ConversationViewState> = createStore(initialState, {
  persist: true,
  key: VIEW_STORAGE_KEY,
});

export function setConversationViewMode(
  context: ConversationViewContext,
  mode: ConversationViewMode
): void {
  conversationViewStore.update((state) => ({
    ...state,
    chatMode: context === 'chat' ? mode : state.chatMode,
    topicMode: context === 'topic' ? mode : state.topicMode,
  }));
}

export function getConversationViewState(): ConversationViewState {
  return conversationViewStore.get();
}

export function getConversationViewMode(context: ConversationViewContext): ConversationViewMode {
  const state = conversationViewStore.get();
  return context === 'chat' ? state.chatMode : state.topicMode;
}

export function subscribeConversationView(
  listener: (state: ConversationViewState, prevState: ConversationViewState) => void
): () => void {
  return conversationViewStore.subscribe(listener);
}

export function resetConversationView(): void {
  conversationViewStore.reset();
}

export { conversationViewStore };
