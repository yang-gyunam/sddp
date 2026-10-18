import type { ConversationHubCallbacks } from '../../conversations/services/ConversationHubService';

const uuid = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

/** Notices only invalidate an authenticated view. Message content always comes from its scoped API. */
export function observeConversationCompletions(
  hub: { setCallbacks(callbacks: ConversationHubCallbacks): () => void },
  trackedRun: () => string | null,
  reload: (isCurrent: () => boolean, outputMessageId: string) => Promise<unknown>
): () => void {
  let disposed = false;
  let pending = Promise.resolve();
  const seen = new Set<string>();
  const queued = new Set<string>();
  const unsubscribe = hub.setCallbacks({
    onAiRunEvent(value) {
      if (disposed || !value || typeof value !== 'object') return;
      const notice = value as Record<string, unknown>;
      if (
        notice.status !== 'completed' ||
        notice.sequence !== 3 ||
        typeof notice.runId !== 'string' ||
        !uuid.test(notice.runId) ||
        typeof notice.messageId !== 'string' ||
        !uuid.test(notice.messageId)
      )
        return;
      const { runId, messageId } = notice;
      if (runId === trackedRun() || seen.has(runId) || queued.has(runId)) return;
      queued.add(runId);
      // Serialize distinct completions so an older response cannot replace a newer message list.
      pending = pending.then(async () => {
        try {
          if (disposed || runId === trackedRun()) return;
          await reload(() => !disposed, messageId);
          if (disposed) return;
          seen.add(runId);
          if (seen.size > 128) seen.delete(seen.values().next().value!);
        } catch {
          // A later durable notice may retry; never render transport text or clear existing messages.
        } finally {
          queued.delete(runId);
        }
      });
    },
  });
  return () => {
    disposed = true;
    unsubscribe();
    seen.clear();
    queued.clear();
  };
}
