// @sddp/ui - Type Definitions

/**
 * Button component variants
 */
export type ButtonVariant = 'primary' | 'secondary' | 'ghost' | 'danger' | 'outline' | 'unstyled';

/**
 * Button component sizes
 */
export type ButtonSize = 'sm' | 'md' | 'lg';

/**
 * Input component types
 */
export type InputType = 'text' | 'password' | 'email' | 'number' | 'tel' | 'url' | 'search' | 'date' | 'datetime-local' | 'time';

/**
 * Badge component variants
 */
export type BadgeVariant = 'default' | 'primary' | 'secondary' | 'success' | 'warning' | 'error' | 'info';

/**
 * Badge component sizes
 */
export type BadgeSize = 'sm' | 'md' | 'lg';

/**
 * Icon component sizes
 */
export type IconSize = 'xs' | 'sm' | 'md' | 'lg' | 'xl';

/**
 * Icon source library
 */
export type IconSource = 'lucide' | 'codicon';

/**
 * Typography component variants
 */
export type TypographyVariant =
  | 'h1'
  | 'h2'
  | 'h3'
  | 'h4'
  | 'h5'
  | 'h6'
  | 'body1'
  | 'body2'
  | 'caption'
  | 'overline';

/**
 * Checkbox sizes
 */
export type CheckboxSize = 'sm' | 'md' | 'lg';

/**
 * Radio sizes
 */
export type RadioSize = 'sm' | 'md' | 'lg';

/**
 * Switch sizes
 */
export type SwitchSize = 'sm' | 'md' | 'lg';

/**
 * Select sizes
 */
export type SelectSize = 'sm' | 'md' | 'lg';

/**
 * Tooltip placement
 */
export type TooltipPlacement = 'top' | 'bottom' | 'left' | 'right';

/**
 * Divider orientation
 */
export type DividerOrientation = 'horizontal' | 'vertical';

/**
 * Resize direction
 */
export type ResizeDirection = 'horizontal' | 'vertical' | 'both';

/**
 * Select option type
 */
export interface SelectOption {
  value: string;
  label: string;
  disabled?: boolean;
}

/**
 * Combobox (searchable select) option type
 */
export interface ComboboxOption {
  value: string;
  label: string;
  description?: string;
  disabled?: boolean;
}

/**
 * Re-export date types from @internationalized/date for convenience
 */
export type {
  DateValue,
  CalendarDate,
  CalendarDateTime,
  ZonedDateTime,
} from '@internationalized/date';

/**
 * Date range type for DateRangePicker
 */
export interface DateRange {
  start: import('@internationalized/date').DateValue | undefined;
  end: import('@internationalized/date').DateValue | undefined;
}

/**
 * Calendar cell render info for custom rendering
 */
export interface CalendarCellRenderInfo {
  class?: string;
  content?: string;
}

/**
 * Tab item for TabBar component
 */
export interface TabItem {
  id: string;
  label: string;
  icon?: string;
  badge?: number;
  disabled?: boolean;
}

/**
 * TabBar size variants
 */
export type TabBarSize = 'sm' | 'md';

/**
 * TabBar visual variants
 */
export type TabBarVariant = 'underline' | 'pill';
