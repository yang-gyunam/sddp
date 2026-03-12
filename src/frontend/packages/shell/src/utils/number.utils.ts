/**
 * Number/Currency Utilities - Intl API based formatting
 */

const DEFAULT_LOCALE = 'en-US';

export interface NumberFormatOptions {
  locale?: string;
  minimumFractionDigits?: number;
  maximumFractionDigits?: number;
}

export interface CurrencyFormatOptions extends NumberFormatOptions {
  currency?: string;
  currencyDisplay?: 'symbol' | 'narrowSymbol' | 'code' | 'name';
}

export interface CompactNumberOptions {
  locale?: string;
  maximumFractionDigits?: number;
}

export interface FileSizeOptions {
  locale?: string;
  decimals?: number;
}

/** Format number with locale-aware grouping */
export function formatNumber(value: number, options?: NumberFormatOptions): string {
  const locale = options?.locale ?? DEFAULT_LOCALE;
  return new Intl.NumberFormat(locale, {
    minimumFractionDigits: options?.minimumFractionDigits,
    maximumFractionDigits: options?.maximumFractionDigits,
  }).format(value);
}

/** Format currency value */
export function formatCurrency(value: number, options?: CurrencyFormatOptions): string {
  const locale = options?.locale ?? DEFAULT_LOCALE;
  const currency = options?.currency ?? 'KRW';
  return new Intl.NumberFormat(locale, {
    style: 'currency',
    currency,
    currencyDisplay: options?.currencyDisplay ?? 'symbol',
    minimumFractionDigits: options?.minimumFractionDigits,
    maximumFractionDigits: options?.maximumFractionDigits,
  }).format(value);
}

/** Format as percentage */
export function formatPercent(value: number, options?: NumberFormatOptions): string {
  const locale = options?.locale ?? DEFAULT_LOCALE;
  return new Intl.NumberFormat(locale, {
    style: 'percent',
    minimumFractionDigits: options?.minimumFractionDigits,
    maximumFractionDigits: options?.maximumFractionDigits ?? 1,
  }).format(value);
}

/** Format with compact notation (e.g., "1.2M") */
export function formatCompactNumber(value: number, options?: CompactNumberOptions): string {
  const locale = options?.locale ?? DEFAULT_LOCALE;
  return new Intl.NumberFormat(locale, {
    notation: 'compact',
    maximumFractionDigits: options?.maximumFractionDigits ?? 1,
  }).format(value);
}

const FILE_SIZE_UNITS = ['B', 'KB', 'MB', 'GB', 'TB'];

/** Format bytes to human-readable file size */
export function formatFileSize(bytes: number, options?: FileSizeOptions): string {
  const decimals = options?.decimals ?? 1;
  if (bytes === 0) return '0 B';

  const abs = Math.abs(bytes);
  const k = 1024;
  const i = Math.min(Math.floor(Math.log(abs) / Math.log(k)), FILE_SIZE_UNITS.length - 1);
  const value = abs / Math.pow(k, i);
  const sign = bytes < 0 ? '-' : '';

  return `${sign}${value.toFixed(decimals)} ${FILE_SIZE_UNITS[i]}`;
}

/** Parse locale-formatted number string to number */
export function parseNumber(str: string, locale?: string): number | null {
  const loc = locale ?? DEFAULT_LOCALE;
  // Get locale's group and decimal separators
  const parts = new Intl.NumberFormat(loc).formatToParts(1234.5);
  const group = parts.find((p) => p.type === 'group')?.value ?? ',';
  const decimal = parts.find((p) => p.type === 'decimal')?.value ?? '.';

  const cleaned = str
    .replace(new RegExp(`[${escapeRegex(group)}]`, 'g'), '')
    .replace(new RegExp(`[${escapeRegex(decimal)}]`), '.');

  const num = Number(cleaned);
  return isNaN(num) ? null : num;
}

/** Round to specified decimal places */
export function roundTo(value: number, decimals: number): number {
  const factor = Math.pow(10, decimals);
  return Math.round(value * factor) / factor;
}

/** Clamp value within range */
export function clamp(value: number, min: number, max: number): number {
  return Math.min(Math.max(value, min), max);
}

function escapeRegex(str: string): string {
  return str.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
}
