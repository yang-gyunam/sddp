/**
 * AI Store - State Management for AI Context Section
 * Manages AI context items, messages, and analysis requests
 */

import { createStore, type Store } from '@sddp/shell/core';
import type {
  AiState,
  AiContextItem,
  AiMessage,
  AiAnalysisRequest,
} from '../types';
import { INITIAL_AI_STATE, sortContextItems } from '../types';

// Create the store
const aiStore: Store<AiState> = createStore<AiState>(INITIAL_AI_STATE);

// ============================================
// Context Actions
// ============================================

/**
 * Set current target context
 */
export function setAiTarget(targetId: string | null, targetType: string | null): void {
  aiStore.update((state) => ({
    ...state,
    currentTargetId: targetId,
    currentTargetType: targetType,
  }));
}

/**
 * Set context items
 */
export function setContextItems(items: AiContextItem[]): void {
  aiStore.update((state) => ({
    ...state,
    contextItems: sortContextItems(items),
    contextLoading: false,
    contextError: null,
  }));
}

/**
 * Add context item
 */
export function addContextItem(item: AiContextItem): void {
  aiStore.update((state) => ({
    ...state,
    contextItems: sortContextItems([...state.contextItems, item]),
  }));
}

/**
 * Mark context item as read
 */
export function markContextItemRead(itemId: string): void {
  aiStore.update((state) => ({
    ...state,
    contextItems: state.contextItems.map((item) =>
      item.id === itemId ? { ...item, isRead: true } : item
    ),
  }));
}

/**
 * Remove context item
 */
export function removeContextItem(itemId: string): void {
  aiStore.update((state) => ({
    ...state,
    contextItems: state.contextItems.filter((item) => item.id !== itemId),
  }));
}

/**
 * Set context loading state
 */
export function setContextLoading(loading: boolean): void {
  aiStore.update((state) => ({
    ...state,
    contextLoading: loading,
  }));
}

/**
 * Set context error
 */
export function setContextError(error: string | null): void {
  aiStore.update((state) => ({
    ...state,
    contextError: error,
    contextLoading: false,
  }));
}

/**
 * Clear context items
 */
export function clearContextItems(): void {
  aiStore.update((state) => ({
    ...state,
    contextItems: [],
    contextLoading: false,
    contextError: null,
  }));
}

// ============================================
// Message Actions
// ============================================

/**
 * Add message
 */
export function addMessage(message: AiMessage): void {
  aiStore.update((state) => ({
    ...state,
    messages: [...state.messages, message],
  }));
}

/**
 * Update message
 */
export function updateMessage(
  messageId: string,
  updates: Partial<AiMessage>
): void {
  aiStore.update((state) => ({
    ...state,
    messages: state.messages.map((msg) =>
      msg.id === messageId ? { ...msg, ...updates } : msg
    ),
  }));
}

/**
 * Set messages loading
 */
export function setMessagesLoading(loading: boolean): void {
  aiStore.update((state) => ({
    ...state,
    messagesLoading: loading,
  }));
}

/**
 * Clear messages
 */
export function clearMessages(): void {
  aiStore.update((state) => ({
    ...state,
    messages: [],
    messagesLoading: false,
  }));
}

// ============================================
// Analysis Request Actions
// ============================================

/**
 * Add analysis request
 */
export function addAnalysisRequest(request: AiAnalysisRequest): void {
  aiStore.update((state) => ({
    ...state,
    activeRequests: [...state.activeRequests, request],
  }));
}

/**
 * Update analysis request
 */
export function updateAnalysisRequest(
  requestId: string,
  updates: Partial<AiAnalysisRequest>
): void {
  aiStore.update((state) => ({
    ...state,
    activeRequests: state.activeRequests.map((req) =>
      req.id === requestId ? { ...req, ...updates } : req
    ),
  }));
}

/**
 * Remove analysis request
 */
export function removeAnalysisRequest(requestId: string): void {
  aiStore.update((state) => ({
    ...state,
    activeRequests: state.activeRequests.filter((req) => req.id !== requestId),
  }));
}

/**
 * Clear completed requests
 */
export function clearCompletedRequests(): void {
  aiStore.update((state) => ({
    ...state,
    activeRequests: state.activeRequests.filter(
      (req) => req.status === 'pending' || req.status === 'processing'
    ),
  }));
}

// ============================================
// UI State Actions
// ============================================

/**
 * Set expanded state
 */
export function setAiExpanded(expanded: boolean): void {
  aiStore.update((state) => ({
    ...state,
    isExpanded: expanded,
  }));
}

/**
 * Toggle expanded state
 */
export function toggleAiExpanded(): void {
  aiStore.update((state) => ({
    ...state,
    isExpanded: !state.isExpanded,
  }));
}

/**
 * Set active tab
 */
export function setActiveTab(tab: AiState['activeTab']): void {
  aiStore.update((state) => ({
    ...state,
    activeTab: tab,
  }));
}

/**
 * Set AI enabled state (from AiStatusService response)
 */
export function setAiEnabled(systemEnabled: boolean, effectiveEnabled: boolean): void {
  aiStore.update((state) => ({
    ...state,
    aiSystemEnabled: systemEnabled,
    aiEffectiveEnabled: effectiveEnabled,
  }));
}

// ============================================
// Getters
// ============================================

/**
 * Get current state
 */
export function getAiState(): AiState {
  return aiStore.get();
}

/**
 * Get context items
 */
export function getContextItems(): AiContextItem[] {
  return aiStore.get().contextItems;
}

/**
 * Get unread context items count
 */
export function getUnreadCount(): number {
  return aiStore.get().contextItems.filter((item) => !item.isRead).length;
}

/**
 * Get messages
 */
export function getMessages(): AiMessage[] {
  return aiStore.get().messages;
}

/**
 * Get active requests
 */
export function getActiveRequests(): AiAnalysisRequest[] {
  return aiStore.get().activeRequests;
}

/**
 * Get pending requests count
 */
export function getPendingRequestsCount(): number {
  return aiStore.get().activeRequests.filter(
    (req) => req.status === 'pending' || req.status === 'processing'
  ).length;
}

/**
 * Check if AI section is expanded
 */
export function isAiExpanded(): boolean {
  return aiStore.get().isExpanded;
}

/**
 * Get active tab
 */
export function getActiveTab(): AiState['activeTab'] {
  return aiStore.get().activeTab;
}

// ============================================
// Subscribe
// ============================================

/**
 * Subscribe to AI state changes
 */
export function subscribeAi(
  listener: (state: AiState, prevState: AiState) => void
): () => void {
  return aiStore.subscribe(listener);
}

/**
 * Reset AI store
 */
export function resetAiStore(): void {
  aiStore.reset();
}

// Export the store for direct access
export { aiStore };

// Re-export types
export type { AiState } from '../types';
