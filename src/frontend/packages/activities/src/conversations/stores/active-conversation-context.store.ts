import { createStore, type Store } from '@sddp/shell/core';

export type ActiveConversationKind = 'channel' | 'forum' | 'dm';

export interface ActiveConversationContext {
  tabId: string;
  conversationId: string | null;
  projectId: string | null;
  kind: ActiveConversationKind | null;
}

type ActiveConversationContextState = Record<string, Omit<ActiveConversationContext, 'tabId'>>;

const activeConversationContextStore: Store<ActiveConversationContextState> =
  createStore<ActiveConversationContextState>({});

function isSameContext(
  current: Omit<ActiveConversationContext, 'tabId'> | undefined,
  next: Omit<ActiveConversationContext, 'tabId'>
): boolean {
  return current?.conversationId === next.conversationId
    && current?.projectId === next.projectId
    && current?.kind === next.kind;
}

export function setActiveConversationContext(context: ActiveConversationContext): void {
  const nextContext = {
    conversationId: context.conversationId,
    projectId: context.projectId,
    kind: context.kind,
  };

  activeConversationContextStore.update((state) => {
    if (isSameContext(state[context.tabId], nextContext)) {
      return state;
    }

    return {
      ...state,
      [context.tabId]: nextContext,
    };
  });
}

export function clearActiveConversationContext(tabId: string): void {
  activeConversationContextStore.update((state) => {
    if (!(tabId in state)) {
      return state;
    }

    const nextState = { ...state };
    delete nextState[tabId];
    return nextState;
  });
}

export function getActiveConversationContext(
  tabId: string
): Omit<ActiveConversationContext, 'tabId'> | null {
  return activeConversationContextStore.get()[tabId] ?? null;
}

export function subscribeActiveConversationContext(
  listener: (state: ActiveConversationContextState) => void
): () => void {
  return activeConversationContextStore.subscribe((state) => {
    listener(state);
  });
}

export function resetActiveConversationContextStore(): void {
  activeConversationContextStore.reset();
}
