/**
 * AI Analysis Base Type Definitions
 * Moved from @sddp/features/ai (features package to be removed)
 */

// ============================================
// Enums (matching backend)
// ============================================

export type AiAnalysisType =
  | 'Reminder'
  | 'Summary'
  | 'MissingField'
  | 'Conflict'
  | 'Quality'
  | 'Impact';

export type AiReportStatus = 'Pending' | 'Processing' | 'Completed' | 'Failed';

// ============================================
// DTOs (matching backend response)
// ============================================

export interface AiReport {
  id: string;
  analysisType: AiAnalysisType;
  status: AiReportStatus;
  targetId: string;
  targetType: string;
  resultJson?: string | null;
  errorMessage?: string | null;
  modelUsed?: string | null;
  tokensUsed?: number | null;
  jobId?: string | null;
  startedAt?: string | null;
  completedAt?: string | null;
  createdAt: string;
}

export interface AiReportSummary {
  id: string;
  analysisType: AiAnalysisType;
  status: AiReportStatus;
  targetId: string;
  targetType: string;
  modelUsed?: string | null;
  tokensUsed?: number | null;
  startedAt?: string | null;
  completedAt?: string | null;
  createdAt: string;
}

// ============================================
// Request DTOs
// ============================================

export interface TriggerAnalysisRequest {
  analysisType: AiAnalysisType;
  projectId: string;
  targetId: string;
  targetType?: string;
  inputText?: string;
}

// ============================================
// Result JSON Schemas
// ============================================

export interface QualityAnalysisResult {
  overallScore: number;
  scores: {
    clarity: { score: number; issues: string[] };
    completeness: { score: number; issues: string[] };
    consistency: { score: number; issues: string[] };
    measurability: { score: number; issues: string[] };
    terminology: { score: number; issues: string[] };
  };
  improvements: Array<{
    section: string;
    issue: string;
    suggestion: string;
  }>;
  strengths: string[];
}

export interface ImpactAnalysisResult {
  impactLevel: 'high' | 'medium' | 'low';
  affectedSpecs: Array<{
    specCode: string;
    impactLevel: 'high' | 'medium' | 'low';
    relationship: string;
    description: string;
    action: string;
  }>;
  affectedArtifacts: Array<{
    artifactPath: string;
    impactLevel: 'high' | 'medium' | 'low';
    description: string;
  }>;
  recommendation: string;
  breakingChanges: boolean;
}

// ============================================
// UI State Types (for AI store)
// ============================================

export type AiContextItemType = 'reminder' | 'suggestion' | 'warning' | 'info' | 'error';

export interface AiContextItemAction {
  label: string;
  handler?: string;
}

export interface AiContextItem {
  id: string;
  type: AiContextItemType;
  title: string;
  content: string;
  isRead: boolean;
  isActionable: boolean;
  action?: AiContextItemAction;
  targetId?: string;
  targetType?: string;
  createdAt: string;
}

export interface AiMessage {
  id: string;
  role: 'user' | 'assistant' | 'system';
  content: string;
  status: 'pending' | 'streaming' | 'complete' | 'error';
  analysisType?: AiAnalysisType;
  metadata?: Record<string, unknown>;
  timestamp?: string;
  createdAt: string;
}

export interface AiAnalysisRequest {
  id: string;
  type: AiAnalysisType;
  status: 'pending' | 'processing' | 'completed' | 'failed';
  targetId: string;
  targetLabel: string;
  progress?: number;
  result?: unknown;
  error?: string;
  createdAt: string;
}

export interface AiState {
  currentTargetId: string | null;
  currentTargetType: string | null;
  contextItems: AiContextItem[];
  contextLoading: boolean;
  contextError: string | null;
  messages: AiMessage[];
  messagesLoading: boolean;
  activeRequests: AiAnalysisRequest[];
  isExpanded: boolean;
  activeTab: 'context' | 'chat' | 'history';
  /** System-level AI enabled (admin toggle) */
  aiSystemEnabled: boolean;
  /** Effective AI enabled (system AND project) */
  aiEffectiveEnabled: boolean;
}

// ============================================
// Constants & Helpers
// ============================================

export const INITIAL_AI_STATE: AiState = {
  currentTargetId: null,
  currentTargetType: null,
  contextItems: [],
  contextLoading: false,
  contextError: null,
  messages: [],
  messagesLoading: false,
  activeRequests: [],
  isExpanded: false,
  activeTab: 'context',
  aiSystemEnabled: true,
  aiEffectiveEnabled: true,
};

export function sortContextItems(items: AiContextItem[]): AiContextItem[] {
  return [...items].sort((a, b) => {
    // Unread first
    if (a.isRead !== b.isRead) return a.isRead ? 1 : -1;
    // Then by date (newest first)
    return new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime();
  });
}

export interface AiContextItemStyle {
  icon: string;
  color: string;
  bgColor: string;
  borderColor: string;
}

export const AI_CONTEXT_ITEM_STYLES: Record<AiContextItemType, AiContextItemStyle> = {
  reminder: { icon: 'bell', color: 'text-[var(--color-warning-500)]', bgColor: 'bg-[var(--color-warning-500)]/10', borderColor: 'border-[var(--color-warning-500)]/20' },
  suggestion: { icon: 'lightbulb', color: 'text-[var(--color-info-500)]', bgColor: 'bg-[var(--color-info-500)]/10', borderColor: 'border-[var(--color-info-500)]/20' },
  warning: { icon: 'alert-triangle', color: 'text-[var(--color-orange-500)]', bgColor: 'bg-[var(--color-orange-500)]/10', borderColor: 'border-[var(--color-orange-500)]/20' },
  info: { icon: 'info', color: 'text-[var(--color-cyan-500)]', bgColor: 'bg-[var(--color-cyan-500)]/10', borderColor: 'border-[var(--color-cyan-500)]/20' },
  error: { icon: 'alert-circle', color: 'text-[var(--color-error-500)]', bgColor: 'bg-[var(--color-error-500)]/10', borderColor: 'border-[var(--color-error-500)]/20' },
};

export interface AiAnalysisTypeStyle {
  icon: string;
  color: string;
  label: string;
  description?: string;
}

export const AI_ANALYSIS_TYPE_STYLES: Record<AiAnalysisType, AiAnalysisTypeStyle> = {
  Quality: { icon: 'star', color: 'text-[var(--color-yellow-500)]', label: 'Quality Analysis' },
  Impact: { icon: 'zap', color: 'text-[var(--color-error-500)]', label: 'Impact Analysis' },
  Summary: { icon: 'file-text', color: 'text-[var(--color-info-500)]', label: 'Summary' },
  MissingField: { icon: 'alert-triangle', color: 'text-[var(--color-orange-500)]', label: 'Missing Fields' },
  Conflict: { icon: 'git-merge', color: 'text-[var(--color-purple-500)]', label: 'Conflict Detection' },
  Reminder: { icon: 'bell', color: 'text-[var(--color-cyan-500)]', label: 'Reminders' },
};
