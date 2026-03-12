export function isConversationVisible(params: {
  conversationId: string;
  activeTabPath?: string | null;
  activeConversationId?: string | null;
}): boolean {
  const { conversationId, activeTabPath = null, activeConversationId = null } = params;

  if (!conversationId) {
    return false;
  }

  if (activeConversationId === conversationId) {
    return true;
  }

  return [
    `/conversation/${conversationId}`,
    `/dm/${conversationId}`,
    `/forum/${conversationId}`,
  ].includes(activeTabPath ?? '');
}
