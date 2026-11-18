export interface ConversationEvidenceReference {
  raw: string;
  type?: string;
  id?: string;
}

export interface ConversationEvidenceSource {
  sourceType: string;
  sourceId: string;
  label: string;
  content: string;
  isTruncated: boolean;
}

export interface ConversationEvidenceContext {
  messageId: string;
  references: string[];
  projectId: string;
  tenantId: string;
  compactUnresolved: boolean;
}
