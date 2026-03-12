/**
 * useTheme Hook - Svelte 5 runes-based theme hook
 * Provides reactive access to the current theme
 */

import { themeStore, getResolvedTheme } from '$stores';

export function useTheme() {
  let currentTheme = $state(getResolvedTheme());

  $effect(() => {
    const unsubscribe = themeStore.subscribe(() => {
      currentTheme = getResolvedTheme();
    });

    return unsubscribe;
  });

  return {
    get current() {
      return currentTheme;
    },
  };
}
