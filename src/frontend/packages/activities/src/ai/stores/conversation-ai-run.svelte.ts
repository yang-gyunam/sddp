import type { Message } from '../../conversations/types';
import { createResourceAiCapability } from './resource-ai-capability.svelte';

export function createConversationAiRun(
  scope: () => { tenantId: string; projectId: string; containerType: 'conversation' | 'topic'; containerId: string; readonly?: boolean },
  _reload: (isCurrent: () => boolean, outputId?: string) => Promise<readonly { id: string }[]>
) {
  const capability = createResourceAiCapability(scope);
  return {
    get view() { return capability.view('ai.conversation'); },
    refreshAvailability: capability.refresh,
    canAsk: false,
    ask: async (_message: Message) => {},
  };
}

export type ConversationAiRun = ReturnType<typeof createConversationAiRun>;
