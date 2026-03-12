/**
 * Conversation Draft Store
 * Persists per-tab message drafts across tab moves
 */

import { writable, get } from 'svelte/store';
import type { MessageType } from '../types';

export interface ConversationDraft {
  content: string;
  type: MessageType;
  updatedAt: number;
}

type DraftState = Record<string, ConversationDraft>;

function createConversationDraftStore() {
  const { subscribe, update } = writable<DraftState>({});

  return {
    subscribe,
    get: () => get({ subscribe }),
    getDraft: (key: string): ConversationDraft | undefined => get({ subscribe })[key],
    setDraft: (key: string, draft: ConversationDraft): void => {
      update((state) => ({
        ...state,
        [key]: draft,
      }));
    },
    clearDraft: (key: string): void => {
      update((state) => {
        if (!state[key]) return state;
        const next = { ...state };
        delete next[key];
        return next;
      });
    },
  };
}

export const conversationDraftStore = createConversationDraftStore();

export const getConversationDraft = (key: string): ConversationDraft | undefined =>
  conversationDraftStore.getDraft(key);

export const setConversationDraft = (key: string, draft: ConversationDraft): void =>
  conversationDraftStore.setDraft(key, draft);

export const clearConversationDraft = (key: string): void =>
  conversationDraftStore.clearDraft(key);
