import { createConversationService } from '@sddp/activities/runtime';

interface NavigationContext {
  tenantId: string;
  userId: string;
  path: string;
}

interface ConversationPathCallbacks {
  getContext: () => NavigationContext;
  openTab: (
    title: string,
    icon: string,
    path: string,
    menuId: string,
    options: { additionalProps: Record<string, unknown> }
  ) => void;
  showError: (path: string, conversationId: string) => void;
}

/** A standalone conversation resolves its own scope before any conversation UI is mounted. */
export function createConversationPathOpener(callbacks: ConversationPathCallbacks) {
  let generation = 0;
  return async (path: string, conversationId: string): Promise<void> => {
    const context = callbacks.getContext();
    if (!context.tenantId || !context.userId) return;
    const request = ++generation;
    const isCurrent = () => {
      const current = callbacks.getContext();
      return request === generation && current.path === path &&
        current.tenantId === context.tenantId && current.userId === context.userId;
    };
    try {
      const conversation = await createConversationService(context.tenantId, '')
        .getConversationById(conversationId);
      if (!isCurrent()) return;
      const isForum = conversation.conversationType === 'Forum';
      const isDm = conversation.conversationType === 'DirectMessage';
      callbacks.openTab(
        conversation.name,
        isForum ? 'list' : isDm ? 'message-circle' : 'hash',
        path,
        `${isForum ? 'forum' : isDm ? 'dm' : 'conversation'}-${conversationId}`,
        { additionalProps: {
          tenantId: context.tenantId,
          projectId: conversation.projectId ?? '',
          conversationType: conversation.conversationType,
          channelStatus: conversation.channelStatus ?? null,
        } }
      );
    } catch {
      if (isCurrent()) callbacks.showError(path, conversationId);
    }
  };
}
