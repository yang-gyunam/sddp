/**
 * Centralized color constants for data visualization and status indicators.
 * These map to CSS custom properties in design-tokens.css where possible.
 *
 * Usage: import { AUDIT_ACTION_COLORS, ENTITY_COLORS } from '../shared/constants/semanticColors';
 */

/** Audit log action → color mapping (used in AuditLogDetailPanel, SystemAuditLogsPage) */
export const AUDIT_ACTION_COLORS: Record<string, string> = {
  create: 'var(--color-success-500)',
  update: 'var(--color-info-500)',
  delete: 'var(--color-error-500)',
  login: 'var(--color-purple-500)',
  logout: 'var(--color-neutral-500)',
  refresh_token: 'var(--color-purple-500)',
  activate: 'var(--color-success-500)',
  deactivate: 'var(--color-warning-500)',
  approve: 'var(--color-success-500)',
  reject: 'var(--color-error-500)',
  set_value: 'var(--color-info-500)',
  set_group: 'var(--color-info-500)',
  save: 'var(--color-success-500)',
  delete_value: 'var(--color-error-500)',
  reset: 'var(--color-warning-500)',
};

const AUDIT_DEFAULT_COLOR = 'var(--color-neutral-500)';

export function getAuditActionColor(action: string): string {
  return AUDIT_ACTION_COLORS[action.toLowerCase()] || AUDIT_DEFAULT_COLOR;
}

/** Entity type → color mapping (used in OwnershipTreemap, graphs) */
export const ENTITY_COLORS: Record<string, string> = {
  Spec: 'var(--color-entity-spec)',
  Requirement: 'var(--color-entity-requirement)',
  GlossaryTerm: 'var(--color-entity-glossary)',
  Task: 'var(--color-entity-task)',
  Artifact: 'var(--color-entity-artifact)',
};

export const ENTITY_DEFAULT_COLOR = 'var(--color-neutral-500)';

/** Chart palette (beyond semantic colors) for pie/donut/bar charts */
export const CHART_PALETTE = [
  'var(--color-accent-primary)',
  'var(--color-success-500)',
  'var(--color-warning-500)',
  'var(--color-error-500)',
  'var(--color-info-500)',
  'var(--color-chart-purple)',
  'var(--color-chart-pink)',
  'var(--color-chart-teal)',
];

/** Task status → color mapping (used in KanbanBoard) */
export const TASK_STATUS_COLORS = {
  Backlog: 'var(--color-neutral-300)',
  ToDo: 'var(--color-neutral-400)',
  InProgress: 'var(--color-info-500)',
  Done: 'var(--color-success-500)',
  Blocked: 'var(--color-error-500)',
} as const;
