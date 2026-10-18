import type { Message } from "../../conversations/types";
export function createMemberConversationReplies(
  _scope: () => {
    tenantId: string;
    projectId: string;
    conversationId: string;
    readonly: boolean;
  },
  _reload: (
    isCurrent: () => boolean,
    outputIds: readonly string[],
  ) => Promise<readonly { id: string }[]>,
) {
  return {
    sent: async (_message: Message) => {},
    requestSaved: async (_message: Message) => {},
    canRequest: (_message: Message) => false,
    cancel: async (_id: string) => {},
    isCancelling: (_id: string) => false,
    cancellableReplies: [] as { id: string; displayName: string }[],
    statusItems: [] as {
      id: string;
      displayName: string;
      state: string;
      label: string;
      delivery: string;
      errorCode: string | null;
    }[],
    canRetryRequest: false,
    retryRequest: async () => {},
    decorate: (message: Message) => message,
    status: "",
    error: "",
    canRetryDelivery: false,
    retryDelivery: async () => {},
  };
}
