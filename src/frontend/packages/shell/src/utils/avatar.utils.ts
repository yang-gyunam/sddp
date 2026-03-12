/**
 * Avatar Utilities - Consistent color generation for user avatars
 */

/**
 * Tailwind color classes for avatars
 * 17 distinct colors for good variety
 */
export const AVATAR_COLORS = [
  'bg-red-500',
  'bg-orange-500',
  'bg-amber-500',
  'bg-yellow-500',
  'bg-lime-500',
  'bg-green-500',
  'bg-emerald-500',
  'bg-teal-500',
  'bg-cyan-500',
  'bg-sky-500',
  'bg-blue-500',
  'bg-indigo-500',
  'bg-violet-500',
  'bg-purple-500',
  'bg-fuchsia-500',
  'bg-pink-500',
  'bg-rose-500',
] as const;

/**
 * AI avatar gradient class
 */
export const AI_AVATAR_COLOR = 'bg-gradient-to-br from-violet-500 to-purple-600';

/**
 * Generate consistent avatar color from a name using hash
 * Same name always produces the same color
 *
 * @param name - User's display name or identifier
 * @returns Tailwind background color class
 */
export function getAvatarColor(name: string): string {
  if (!name) return AVATAR_COLORS[0];

  let hash = 0;
  for (let i = 0; i < name.length; i++) {
    hash = name.charCodeAt(i) + ((hash << 5) - hash);
  }

  return AVATAR_COLORS[Math.abs(hash) % AVATAR_COLORS.length]!;
}

/**
 * Hex color values for avatars (used with inline style to avoid Tailwind JIT purge)
 */
export const AVATAR_HEX_COLORS = [
  '#ef4444', '#f97316', '#f59e0b', '#eab308', '#84cc16',
  '#22c55e', '#10b981', '#14b8a6', '#06b6d4', '#0ea5e9',
  '#3b82f6', '#6366f1', '#8b5cf6', '#a855f7', '#d946ef',
  '#ec4899', '#f43f5e',
] as const;

/**
 * Generate consistent avatar hex color from a name using hash.
 * Use with inline style to avoid Tailwind JIT purge issues.
 *
 * @param name - User's display name or identifier
 * @returns CSS hex color value (e.g., '#3b82f6')
 */
export function getAvatarHexColor(name: string): string {
  if (!name) return AVATAR_HEX_COLORS[0];

  let hash = 0;
  for (let i = 0; i < name.length; i++) {
    hash = name.charCodeAt(i) + ((hash << 5) - hash);
  }

  return AVATAR_HEX_COLORS[Math.abs(hash) % AVATAR_HEX_COLORS.length]!;
}

/**
 * Get avatar initials from name (up to 2 characters)
 *
 * @param name - User's display name
 * @returns Uppercase initials (e.g., "JK" for "John Kim")
 */
export function getAvatarInitials(name: string): string {
  if (!name) return '?';

  const parts = name.trim().split(/\s+/);
  if (parts.length >= 2) {
    // First letter of first and last name
    return (parts[0]!.charAt(0) + parts[parts.length - 1]!.charAt(0)).toUpperCase();
  }

  // Single name: first two letters
  return name.substring(0, 2).toUpperCase();
}

/**
 * Preset avatar image definitions
 */
export const PRESET_AVATARS = [
  { id: 'bear', label: 'Bear', emoji: '🐻' },
  { id: 'cat', label: 'Cat', emoji: '🐱' },
  { id: 'dog', label: 'Dog', emoji: '🐶' },
  { id: 'fox', label: 'Fox', emoji: '🦊' },
  { id: 'panda', label: 'Panda', emoji: '🐼' },
  { id: 'koala', label: 'Koala', emoji: '🐨' },
  { id: 'robot', label: 'Robot', emoji: '🤖' },
  { id: 'alien', label: 'Alien', emoji: '👾' },
  { id: 'octopus', label: 'Octopus', emoji: '🐙' },
  { id: 'unicorn', label: 'Unicorn', emoji: '🦄' },
  { id: 'penguin', label: 'Penguin', emoji: '🐧' },
  { id: 'owl', label: 'Owl', emoji: '🦉' },
] as const;

export type PresetAvatarId = (typeof PRESET_AVATARS)[number]['id'];

/**
 * Parse avatarUrl into type + value for rendering.
 * - `color:#hex` → user-chosen color
 * - `preset:id` → preset emoji avatar
 * - other URL → external image
 * - null/undefined → default (hash-based color + initials)
 */
export function parseAvatarUrl(avatarUrl: string | null | undefined): {
  type: 'color' | 'preset' | 'image' | 'default';
  value: string;
} {
  if (!avatarUrl) return { type: 'default', value: '' };
  if (avatarUrl.startsWith('color:')) return { type: 'color', value: avatarUrl.slice(6) };
  if (avatarUrl.startsWith('preset:')) return { type: 'preset', value: avatarUrl.slice(7) };
  return { type: 'image', value: avatarUrl };
}

/**
 * Get emoji for a preset avatar ID
 */
export function getPresetAvatarEmoji(presetId: string): string {
  return PRESET_AVATARS.find(p => p.id === presetId)?.emoji ?? '?';
}
