/**
 * Badge Types - Generic type definitions for enum-based badges
 * Used by EnumBadge component and domain-specific badge wrappers
 */

/**
 * Style configuration for a single badge state/type
 * Matches the pattern used in domain types (e.g., SpecStatusStyle)
 */
export interface BadgeStyleConfig {
  /** Background color class (e.g., 'bg-[var(--color-success-500)]/10') */
  bgColor: string;
  /** Border color class (e.g., 'border-[var(--color-success-500)]/20') */
  borderColor: string;
  /** Text color class - supports both 'textColor' and 'color' for compatibility */
  textColor?: string;
  /** Text color class (alias for textColor, used by some domain styles) */
  color?: string;
  /** Display label for this state */
  label: string;
  /** Optional icon name (for @sddp/ui Icon component) */
  icon?: string;
  /** Optional description for tooltip */
  description?: string;
}

/**
 * Badge size variants
 */
export type BadgeSize = 'sm' | 'md' | 'lg';

/**
 * Badge display mode - controls what content is shown
 */
export type BadgeDisplayMode = 'full' | 'icon-only' | 'label-only';

/**
 * Badge shape variants
 */
export type BadgeShape = 'rounded' | 'rounded-full' | 'rounded-md';
