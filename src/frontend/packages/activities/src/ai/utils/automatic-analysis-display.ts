/** Read-only presentation of the existing analysis result contract. */
const summaryFields = [
  'keyProposals',
  'agreements',
  'disagreements',
  'decisions',
  'unresolvedIssues',
  'nextSteps',
] as const;
type SummaryField = (typeof summaryFields)[number];
export type SummaryDisplay = { kind: 'summary'; summary: string } & Record<SummaryField, string[]>;
export type ReminderDisplay = {
  kind: 'reminder';
  recommendation: string;
  hasConflicts: boolean;
  relatedDecisions: {
    source: string;
    summary: string;
    relevance: string;
    relationship: 'aligns' | 'contradicts' | 'extends' | 'unrelated' | 'uncertain';
  }[];
};
export type AutomaticAnalysisDisplay = SummaryDisplay | ReminderDisplay;
const text = (value: unknown): value is string => typeof value === 'string' && value.length <= 8000;
const list = (value: unknown): value is unknown[] => Array.isArray(value) && value.length <= 100;
function shape(value: unknown, keys: readonly string[]): value is Record<string, unknown> {
  return (
    !!value &&
    typeof value === 'object' &&
    !Array.isArray(value) &&
    Object.keys(value).length === keys.length &&
    keys.every((key) => Object.hasOwn(value, key))
  );
}

export function parseAutomaticAnalysis(
  type: string,
  content: string
): AutomaticAnalysisDisplay | null {
  if (!['AiSummary', 'AiReminder'].includes(type) || content.length > 65536) return null;
  try {
    const value: unknown = JSON.parse(content);
    if (
      type === 'AiSummary' &&
      shape(value, ['summary', ...summaryFields]) &&
      text(value.summary) &&
      summaryFields.every((field) => list(value[field]) && value[field].every(text))
    ) {
      return { ...value, kind: 'summary' } as SummaryDisplay;
    }
    if (
      type === 'AiReminder' &&
      shape(value, ['relatedDecisions', 'recommendation', 'hasConflicts']) &&
      text(value.recommendation) &&
      typeof value.hasConflicts === 'boolean' &&
      list(value.relatedDecisions) &&
      value.relatedDecisions.every(
        (item) =>
          shape(item, ['source', 'summary', 'relevance', 'relationship']) &&
          Object.values(item).every(text) &&
          ['aligns', 'contradicts', 'extends', 'unrelated', 'uncertain'].includes(
            item.relationship as string
          )
      ) &&
      value.hasConflicts ===
        value.relatedDecisions.some(
          (item) => (item as { relationship: string }).relationship === 'contradicts'
        )
    ) {
      return { ...value, kind: 'reminder' } as ReminderDisplay;
    }
  } catch {
    /* Historical or unsupported content keeps its existing Markdown presentation. */
  }
  return null;
}
