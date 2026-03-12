/**
 * System-wide Priority type and styles
 * Used across Task, Requirement, and other domains
 */
import type { BadgeStyleConfig } from './badge.types';

export type Priority = 'Low' | 'Medium' | 'High' | 'Urgent';

export const PRIORITY_STYLES: Record<Priority, BadgeStyleConfig> = {
  Low: {
    bgColor: 'bg-[var(--color-neutral-500)]/10',
    borderColor: 'border-[var(--color-neutral-500)]/20',
    textColor: 'text-[var(--color-neutral-600)] dark:text-[var(--color-neutral-400)]',
    icon: 'arrow-down',
    label: 'L',
    description: 'Low',
  },
  Medium: {
    bgColor: 'bg-[var(--color-warning-500)]/10',
    borderColor: 'border-[var(--color-warning-500)]/20',
    textColor: 'text-[var(--color-warning-600)] dark:text-[var(--color-warning-400)]',
    icon: 'minus',
    label: 'M',
    description: 'Medium',
  },
  High: {
    bgColor: 'bg-[var(--color-error-500)]/10',
    borderColor: 'border-[var(--color-error-500)]/20',
    textColor: 'text-[var(--color-error-600)] dark:text-[var(--color-error-400)]',
    icon: 'arrow-up',
    label: 'H',
    description: 'High',
  },
  Urgent: {
    bgColor: 'bg-[var(--color-error-600)]/15',
    borderColor: 'border-[var(--color-error-600)]/30',
    textColor: 'text-[var(--color-error-700)] dark:text-[var(--color-error-300)]',
    icon: 'alert-triangle',
    label: 'U',
    description: 'Urgent',
  },
};
