/**
 * String Utilities - Common string manipulation functions
 */

/** Truncate string to maxLength, appending suffix if truncated */
export function truncate(str: string, maxLength: number, suffix = '...'): string {
  if (str.length <= maxLength) return str;
  return str.slice(0, maxLength - suffix.length) + suffix;
}

/** Capitalize first character */
export function capitalize(str: string): string {
  if (!str) return str;
  return str.charAt(0).toUpperCase() + str.slice(1);
}

/** Capitalize first character of each word */
export function capitalizeWords(str: string): string {
  if (!str) return str;
  return str.replace(/\b\w/g, (c) => c.toUpperCase());
}

/** Split string into words (handles camelCase, kebab-case, snake_case, spaces) */
function splitWords(str: string): string[] {
  return str
    .replace(/([a-z])([A-Z])/g, '$1 $2')
    .replace(/[_-]+/g, ' ')
    .trim()
    .split(/\s+/)
    .filter(Boolean);
}

/** Convert to camelCase */
export function camelCase(str: string): string {
  const words = splitWords(str);
  if (words.length === 0) return '';
  return words[0]!.toLowerCase() + words.slice(1).map((w) => capitalize(w.toLowerCase())).join('');
}

/** Convert to kebab-case */
export function kebabCase(str: string): string {
  return splitWords(str).map((w) => w.toLowerCase()).join('-');
}

/** Convert to snake_case */
export function snakeCase(str: string): string {
  return splitWords(str).map((w) => w.toLowerCase()).join('_');
}

/** Convert to PascalCase */
export function pascalCase(str: string): string {
  return splitWords(str).map((w) => capitalize(w.toLowerCase())).join('');
}

/** Convert to URL-safe slug */
export function slugify(str: string): string {
  return str
    .toLowerCase()
    .trim()
    .replace(/[^\w\s-]/g, '')
    .replace(/[\s_]+/g, '-')
    .replace(/-+/g, '-')
    .replace(/^-|-$/g, '');
}

const HTML_ESCAPE_MAP: Record<string, string> = {
  '&': '&amp;',
  '<': '&lt;',
  '>': '&gt;',
  '"': '&quot;',
  "'": '&#39;',
};

/** Escape HTML special characters */
export function escapeHtml(str: string): string {
  return str.replace(/[&<>"']/g, (c) => HTML_ESCAPE_MAP[c] ?? c);
}

/** Strip HTML tags from string */
export function stripHtml(str: string): string {
  return str.replace(/<[^>]*>/g, '');
}

/** Check if string is null, undefined, or empty */
export function isEmpty(str: string | null | undefined): boolean {
  return str == null || str.length === 0;
}

/** Check if string is null, undefined, empty, or whitespace only */
export function isBlank(str: string | null | undefined): boolean {
  return str == null || str.trim().length === 0;
}

/** Simple template interpolation: "Hello {{name}}" with { name: "World" } */
export function template(tpl: string, data: Record<string, unknown>): string {
  return tpl.replace(/\{\{(\w+)\}\}/g, (_, key) => {
    const value = data[key];
    return value != null ? String(value) : '';
  });
}

/** Count words in string */
export function countWords(str: string): number {
  const trimmed = str.trim();
  if (!trimmed) return 0;
  return trimmed.split(/\s+/).length;
}

/** Simple string hash code (Java-style) */
export function hashCode(str: string): number {
  let hash = 0;
  for (let i = 0; i < str.length; i++) {
    const char = str.charCodeAt(i);
    hash = ((hash << 5) - hash + char) | 0;
  }
  return hash;
}
