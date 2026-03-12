/**
 * Date/Time Utilities - Intl API based formatting
 */

const DEFAULT_LOCALE = 'en-US';

export interface DateFormatOptions {
  locale?: string;
  year?: 'numeric' | '2-digit';
  month?: 'numeric' | '2-digit' | 'long' | 'short' | 'narrow';
  day?: 'numeric' | '2-digit';
}

export interface TimeFormatOptions {
  locale?: string;
  hour?: 'numeric' | '2-digit';
  minute?: 'numeric' | '2-digit';
  second?: 'numeric' | '2-digit';
  hour12?: boolean;
}

export interface DateTimeFormatOptions extends DateFormatOptions, TimeFormatOptions {}

export interface IntlDateTimeFormatOptions extends Intl.DateTimeFormatOptions {
  locale?: string;
}

export interface RelativeTimeOptions {
  locale?: string;
  numeric?: 'always' | 'auto';
}

export interface DurationOptions {
  locale?: string;
  maxUnit?: 'day' | 'hour' | 'minute' | 'second';
}

/** Format date portion */
export function formatDate(date: Date | string | number, options?: DateFormatOptions): string {
  const d = toDate(date);
  const locale = options?.locale ?? DEFAULT_LOCALE;
  return new Intl.DateTimeFormat(locale, {
    year: options?.year ?? 'numeric',
    month: options?.month ?? 'numeric',
    day: options?.day ?? 'numeric',
  }).format(d);
}

/** Format time portion */
export function formatTime(date: Date | string | number, options?: TimeFormatOptions): string {
  const d = toDate(date);
  const locale = options?.locale ?? DEFAULT_LOCALE;
  return new Intl.DateTimeFormat(locale, {
    hour: options?.hour ?? '2-digit',
    minute: options?.minute ?? '2-digit',
    second: options?.second,
    hour12: options?.hour12 ?? false,
  }).format(d);
}

/** Format date and time */
export function formatDateTime(
  date: Date | string | number,
  options?: DateTimeFormatOptions,
): string {
  const d = toDate(date);
  const locale = options?.locale ?? DEFAULT_LOCALE;
  return new Intl.DateTimeFormat(locale, {
    year: options?.year ?? 'numeric',
    month: options?.month ?? 'numeric',
    day: options?.day ?? 'numeric',
    hour: options?.hour ?? '2-digit',
    minute: options?.minute ?? '2-digit',
    second: options?.second,
    hour12: options?.hour12 ?? false,
  }).format(d);
}

/** Format date/time with raw Intl options (no defaults applied) */
export function formatDateWithOptions(
  date: Date | string | number,
  options?: IntlDateTimeFormatOptions,
): string {
  const d = toDate(date);
  const locale = options?.locale ?? DEFAULT_LOCALE;
  const formatOptions: Intl.DateTimeFormatOptions = { ...(options ?? {}) };
  delete (formatOptions as IntlDateTimeFormatOptions).locale;

  return new Intl.DateTimeFormat(locale, formatOptions).format(d);
}

/** Format month as 3-letter abbreviation (e.g., "Jan", "Feb") */
export function formatMonthShort(date: Date | string | number, locale?: string): string {
  const d = toDate(date);
  return new Intl.DateTimeFormat(locale ?? DEFAULT_LOCALE, { month: 'short' }).format(d);
}

/** Format relative time (e.g., "3 hours ago") */
export function formatRelativeTime(
  date: Date | string | number,
  base: Date | string | number = new Date(),
  options?: RelativeTimeOptions,
): string {
  const d = toDate(date);
  const b = toDate(base);
  const diffMs = d.getTime() - b.getTime();

  // Guard against NaN or Infinity (invalid date input)
  if (!Number.isFinite(diffMs)) {
    return '';
  }

  const locale = options?.locale ?? DEFAULT_LOCALE;
  const numeric = options?.numeric ?? 'auto';

  const rtf = new Intl.RelativeTimeFormat(locale, { numeric });

  const absDiff = Math.abs(diffMs);
  const sign = diffMs < 0 ? -1 : 1;

  if (absDiff < 60_000) {
    return rtf.format(sign * Math.round(absDiff / 1000), 'second');
  }
  if (absDiff < 3_600_000) {
    return rtf.format(sign * Math.round(absDiff / 60_000), 'minute');
  }
  if (absDiff < 86_400_000) {
    return rtf.format(sign * Math.round(absDiff / 3_600_000), 'hour');
  }
  if (absDiff < 2_592_000_000) {
    return rtf.format(sign * Math.round(absDiff / 86_400_000), 'day');
  }
  if (absDiff < 31_536_000_000) {
    return rtf.format(sign * Math.round(absDiff / 2_592_000_000), 'month');
  }
  return rtf.format(sign * Math.round(absDiff / 31_536_000_000), 'year');
}

/** Format duration in milliseconds */
export function formatDuration(ms: number, options?: DurationOptions): string {
  const maxUnit = options?.maxUnit ?? 'day';
  const abs = Math.abs(ms);
  const parts: string[] = [];

  let remaining = abs;

  if (maxUnit === 'day') {
    const days = Math.floor(remaining / 86_400_000);
    if (days > 0) {
      parts.push(`${days}d`);
      remaining %= 86_400_000;
    }
  }

  if (maxUnit === 'day' || maxUnit === 'hour') {
    const hours = Math.floor(remaining / 3_600_000);
    if (hours > 0) {
      parts.push(`${hours}h`);
      remaining %= 3_600_000;
    }
  }

  if (maxUnit !== 'second' || parts.length === 0) {
    const minutes = Math.floor(remaining / 60_000);
    if (minutes > 0) {
      parts.push(`${minutes}m`);
      remaining %= 60_000;
    }
  }

  if (maxUnit === 'second' || parts.length === 0) {
    const seconds = Math.floor(remaining / 1000);
    if (seconds > 0 || parts.length === 0) {
      parts.push(`${seconds}s`);
    }
  }

  return parts.join(' ');
}

/** Parse date string to Date object */
export function parseDate(str: string): Date | null {
  const d = new Date(str);
  return isNaN(d.getTime()) ? null : d;
}

/** Check if date is today */
export function isToday(date: Date | string | number): boolean {
  return isSameDay(toDate(date), new Date());
}

/** Check if two dates are the same calendar day */
export function isSameDay(d1: Date | string | number, d2: Date | string | number): boolean {
  const a = toDate(d1);
  const b = toDate(d2);
  return (
    a.getFullYear() === b.getFullYear() &&
    a.getMonth() === b.getMonth() &&
    a.getDate() === b.getDate()
  );
}

/** Get difference in calendar days between two dates */
export function getDaysDifference(d1: Date | string | number, d2: Date | string | number): number {
  const a = startOfDay(toDate(d1));
  const b = startOfDay(toDate(d2));
  return Math.round((a.getTime() - b.getTime()) / 86_400_000);
}

/** Add days to a date (returns new Date) */
export function addDays(date: Date | string | number, days: number): Date {
  const d = toDate(date);
  const result = new Date(d);
  result.setDate(result.getDate() + days);
  return result;
}

/** Get start of day (00:00:00.000) */
export function startOfDay(date: Date | string | number): Date {
  const d = toDate(date);
  const result = new Date(d);
  result.setHours(0, 0, 0, 0);
  return result;
}

/** Get end of day (23:59:59.999) */
export function endOfDay(date: Date | string | number): Date {
  const d = toDate(date);
  const result = new Date(d);
  result.setHours(23, 59, 59, 999);
  return result;
}

/**
 * Format time smartly for compact display (i18n supported)
 * - < 1 min: "just now"
 * - < 1 hour: "X min ago"
 * - < 3 hours: "X hr ago"
 * - same day: "HH:MM"
 * - other: "M/D HH:MM" / "M/D HH:MM"
 */
export function formatSmartTime(
  date: Date | string | number,
  options?: { locale?: string },
): string {
  const d = toDate(date);
  const now = new Date();
  const diffMs = now.getTime() - d.getTime();

  // Guard against NaN or Infinity (invalid date input)
  if (!Number.isFinite(diffMs)) {
    return '';
  }

  const locale = options?.locale ?? DEFAULT_LOCALE;

  // Future time or invalid
  if (diffMs < 0) {
    return formatTime(d, { locale });
  }

  const diffMinutes = Math.floor(diffMs / 60_000);
  const diffHours = Math.floor(diffMs / 3_600_000);

  // Less than 1 minute
  if (diffMinutes < 1) {
    return 'just now';
  }

  // Less than 1 hour
  if (diffMinutes < 60) {
    return diffMinutes === 1 ? '1 min ago' : `${diffMinutes} min ago`;
  }

  // Less than 3 hours
  if (diffHours < 3) {
    return diffHours === 1 ? '1 hr ago' : `${diffHours} hr ago`;
  }

  // Same day - show time only
  if (isSameDay(d, now)) {
    return formatTime(d, { locale, hour: '2-digit', minute: '2-digit' });
  }

  // Different day - show date and time
  const month = d.getMonth() + 1;
  const day = d.getDate();
  const time = formatTime(d, { locale, hour: '2-digit', minute: '2-digit' });

  // Different year - include year
  if (d.getFullYear() !== now.getFullYear()) {
    const year = d.getFullYear();
    return `${year}/${month}/${day} ${time}`;
  }

  return `${month}/${day} ${time}`;
}

function toDate(value: Date | string | number): Date {
  if (value instanceof Date) return value;
  return new Date(value);
}

// ============================================
// Local Timezone Date String
// ============================================

/**
 * Format a Date to YYYY-MM-DD in local timezone.
 * Use this instead of `date.toISOString().split('T')[0]` which converts to UTC
 * and shifts dates backward in UTC+ timezones (e.g. Asia/Seoul).
 */
export function toLocalDateString(date: Date | string | number): string {
  const d = toDate(date);
  const year = d.getFullYear();
  const month = String(d.getMonth() + 1).padStart(2, '0');
  const day = String(d.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
}

// ============================================
// User Preference-Aware Formatting
// ============================================

export type DateFormatPreference = 'relative' | 'absolute' | 'iso';

/** Read date format preference from localStorage */
function getDateFormatPreference(): DateFormatPreference {
  try {
    const stored = localStorage.getItem('sddp-preferences');
    if (stored) {
      const prefs = JSON.parse(stored);
      if (prefs.dateFormat === 'relative' || prefs.dateFormat === 'absolute' || prefs.dateFormat === 'iso') {
        return prefs.dateFormat;
      }
    }
  } catch { /* ignore */ }
  return 'relative';
}

/** Format date according to user preference setting */
export function formatDateByPreference(date: Date | string | number): string {
  const pref = getDateFormatPreference();
  switch (pref) {
    case 'relative': return formatSmartTime(date);
    case 'absolute': return formatDateTime(date);
    case 'iso': return toDate(date).toISOString();
  }
}
