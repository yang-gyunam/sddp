import { fetchWithAuth } from '../../shared/api';

export interface AiMemberReplyStatus {
  id: string;
  memberId: string;
  displayName: string;
  status: string;
  outputMessageId: string | null;
  errorCode: string | null;
  canCancel?: boolean;
}
interface StatusPage {
  items: AiMemberReplyStatus[];
  nextCursor: string | null;
}
interface StatusScope {
  tenantId: string;
  projectId: string;
  conversationId: string;
}

/** Reconcile active work and recent results without downloading the reply history. */
export async function loadAiMemberReplyStatus(
  scope: StatusScope,
  previouslyActive: readonly string[],
  isCurrent: () => boolean
): Promise<AiMemberReplyStatus[]> {
  const base = `/conversations/${encodeURIComponent(scope.conversationId)}/ai-member-replies/status`;
  async function page(mode: string, after?: string, ids?: readonly string[]) {
    if (!isCurrent()) return null;
    const query = new URLSearchParams({ mode, limit: '100' });
    if (after) query.set('after', after);
    for (const id of ids ?? []) query.append('replyIds', id);
    const data = await fetchWithAuth<StatusPage>(`${base}?${query}`, scope);
    if (!isCurrent()) return null;
    if (!Array.isArray(data.items) || data.items.length > 100)
      throw new Error('Invalid AI member reply status page.');
    const next = data.nextCursor?.toLowerCase();
    if (
      next &&
      (mode !== 'active' ||
        data.items.length === 0 ||
        next !== data.items.at(-1)?.id.toLowerCase() ||
        (after && next <= after))
    )
      throw new Error('Invalid AI member reply status cursor.');
    return { items: data.items, nextCursor: next || undefined };
  }

  const active: AiMemberReplyStatus[] = [];
  let after: string | undefined;
  do {
    const data = await page('active', after);
    if (!data) return [];
    active.push(...data.items);
    after = data.nextCursor;
  } while (after);
  const recent = await page('recent');
  if (!recent) return [];
  // The later recent response takes precedence when an active reply finishes mid-read.
  const result = new Map(recent.items.map((reply) => [reply.id, reply]));
  for (const reply of active) if (!result.has(reply.id)) result.set(reply.id, reply);
  const missing = [...new Set(previouslyActive)].filter((id) => !result.has(id));
  for (let index = 0; index < missing.length; index += 100) {
    const data = await page('tracked', undefined, missing.slice(index, index + 100));
    if (!data) return [];
    for (const reply of data.items) result.set(reply.id, reply);
  }
  return [...result.values()];
}
