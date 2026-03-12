/**
 * Theme Store - Theme State Management
 * Supports light, dark, and system preference modes
 * Supports typography style selection (default, krds)
 * Persists user preference to localStorage
 */

import { createStore, type Store } from '@sddp/shell/core';
export type ThemeMode = 'light' | 'dark' | 'system';

const THEME_STORAGE_KEY = 'sddp-theme';
const TYPOGRAPHY_STORAGE_KEY = 'sddp-typography';
const ACCENT_STORAGE_KEY = 'sddp-accent';

export type TypographyStyle = 'default' | 'krds';

export type AccentColor =
  | 'blue'
  | 'indigo'
  | 'purple'
  | 'cyan'
  | 'teal'
  | 'rose'
  | 'orange'
  | 'sky'
  | 'pink'
  | 'lime'
  | 'amber'
  | 'seoul-green-aurora'
  | 'seoul-skycoral';

export const ACCENT_COLORS: { id: AccentColor; label: string; color: string }[] = [
  { id: 'blue', label: 'Blue (VS Code)', color: '#007ACC' },
  { id: 'indigo', label: 'Indigo', color: '#6366f1' },
  { id: 'purple', label: 'Purple', color: '#8b5cf6' },
  { id: 'cyan', label: 'Cyan', color: '#06b6d4' },
  { id: 'teal', label: 'Teal', color: '#14b8a6' },
  { id: 'sky', label: 'Sky', color: '#0ea5e9' },
  { id: 'rose', label: 'Rose', color: '#f43f5e' },
  { id: 'orange', label: 'Orange', color: '#f97316' },
  { id: 'pink', label: 'Pink', color: '#ec4899' },
  { id: 'lime', label: 'Lime', color: '#84cc16' },
  { id: 'amber', label: 'Amber', color: '#f59e0b' },
  { id: 'seoul-green-aurora', label: 'Seoul Green Aurora', color: '#00B493' },
  { id: 'seoul-skycoral', label: 'Seoul Skycoral', color: '#F8496C' },
];

interface ThemeState {
  mode: ThemeMode;
  resolvedTheme: 'light' | 'dark';
  typography: TypographyStyle;
  accent: AccentColor;
}

/**
 * Get system preference for dark mode
 */
function getSystemPreference(): 'light' | 'dark' {
  if (typeof window === 'undefined') return 'light';
  return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
}

/**
 * Resolve the actual theme based on mode
 */
function resolveTheme(mode: ThemeMode): 'light' | 'dark' {
  if (mode === 'system') {
    return getSystemPreference();
  }
  return mode;
}

/**
 * Load initial theme from localStorage
 */
function loadInitialTheme(): ThemeState {
  let mode: ThemeMode = 'system';
  let typography: TypographyStyle = 'default';
  let accent: AccentColor = 'indigo';

  if (typeof window !== 'undefined') {
    const storedTheme = localStorage.getItem(THEME_STORAGE_KEY);
    if (storedTheme === 'light' || storedTheme === 'dark' || storedTheme === 'system') {
      mode = storedTheme;
    }

    const storedTypography = localStorage.getItem(TYPOGRAPHY_STORAGE_KEY);
    if (storedTypography === 'default' || storedTypography === 'krds') {
      typography = storedTypography;
    }

    const storedAccent = localStorage.getItem(ACCENT_STORAGE_KEY);
    if (storedAccent && ACCENT_COLORS.some((c) => c.id === storedAccent)) {
      accent = storedAccent as AccentColor;
    }
  }

  return {
    mode,
    resolvedTheme: resolveTheme(mode),
    typography,
    accent,
  };
}

// Create the theme store
const themeStore: Store<ThemeState> = createStore<ThemeState>(loadInitialTheme());

/**
 * Apply theme to document
 */
function applyTheme(theme: 'light' | 'dark'): void {
  if (typeof document === 'undefined') return;

  const root = document.documentElement;

  if (theme === 'dark') {
    root.classList.add('dark');
    root.classList.remove('light');
  } else {
    root.classList.add('light');
    root.classList.remove('dark');
  }
}

/**
 * Apply typography style to document
 */
function applyTypography(style: TypographyStyle): void {
  if (typeof document === 'undefined') return;

  const root = document.documentElement;
  root.classList.remove('typography-default', 'typography-krds');
  root.classList.add(`typography-${style}`);
}

/**
 * Apply accent color to document
 */
function applyAccent(accent: AccentColor): void {
  if (typeof document === 'undefined') return;

  const root = document.documentElement;
  root.setAttribute('data-accent', accent);
}

// Apply initial theme, typography, and accent
const initialState = themeStore.get();
applyTheme(initialState.resolvedTheme);
applyTypography(initialState.typography);
applyAccent(initialState.accent);

// Listen for system preference changes
if (typeof window !== 'undefined') {
  const mediaQuery = window.matchMedia('(prefers-color-scheme: dark)');

  mediaQuery.addEventListener('change', () => {
    const state = themeStore.get();
    if (state.mode === 'system') {
      const newResolved = getSystemPreference();
      themeStore.set({
        ...state,
        resolvedTheme: newResolved,
      });
      applyTheme(newResolved);
    }
  });
}

/**
 * Get current theme state
 */
export function getThemeState(): ThemeState {
  return themeStore.get();
}

/**
 * Get the resolved (actual) theme
 */
export function getResolvedTheme(): 'light' | 'dark' {
  return themeStore.get().resolvedTheme;
}

/**
 * Get the theme mode setting
 */
export function getThemeMode(): ThemeMode {
  return themeStore.get().mode;
}

/**
 * Check if dark mode is active
 */
export function isDarkMode(): boolean {
  return themeStore.get().resolvedTheme === 'dark';
}

/**
 * Set theme mode
 */
export function setThemeMode(mode: ThemeMode): void {
  const state = themeStore.get();
  const resolved = resolveTheme(mode);

  if (typeof localStorage !== 'undefined') {
    localStorage.setItem(THEME_STORAGE_KEY, mode);
  }

  themeStore.set({
    ...state,
    mode,
    resolvedTheme: resolved,
  });

  applyTheme(resolved);
}

/**
 * Get the current accent color
 */
export function getAccentColor(): AccentColor {
  return themeStore.get().accent;
}

/**
 * Set accent color
 */
export function setAccentColor(accent: AccentColor): void {
  const state = themeStore.get();

  if (typeof localStorage !== 'undefined') {
    localStorage.setItem(ACCENT_STORAGE_KEY, accent);
  }

  themeStore.set({
    ...state,
    accent,
  });

  applyAccent(accent);
}

/**
 * Get the typography style setting
 */
export function getTypography(): TypographyStyle {
  return themeStore.get().typography;
}

/**
 * Set typography style
 */
export function setTypography(style: TypographyStyle): void {
  const state = themeStore.get();

  // Persist to localStorage
  if (typeof localStorage !== 'undefined') {
    localStorage.setItem(TYPOGRAPHY_STORAGE_KEY, style);
  }

  themeStore.set({
    ...state,
    typography: style,
  });

  applyTypography(style);
}

/**
 * Toggle between light and dark modes
 * If currently on system, switches to opposite of resolved theme
 */
export function toggleTheme(): void {
  const state = themeStore.get();
  const newMode: ThemeMode = state.resolvedTheme === 'dark' ? 'light' : 'dark';
  setThemeMode(newMode);
}

/**
 * Subscribe to theme state changes
 */
export function subscribeTheme(
  listener: (state: ThemeState, prevState: ThemeState) => void
): () => void {
  return themeStore.subscribe(listener);
}

// Export the store for direct access in Svelte components
export { themeStore };
