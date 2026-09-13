/**
 * resetAppState - Resets all application state (stores + services)
 *
 * Called on:
 *   1. Explicit logout — clear previous user's data
 *   2. Login success (before loading data) — ensure clean slate
 */

// Shell stores
import { tabsStore, resetLayout } from '@sddp/shell/stores';
import { navigationService } from '@sddp/shell/core/services';

/**
 * Reset app state but preserve layout (sidebar, panel positions).
 * Used on browser reload (token refresh) where layout should survive.
 */
export async function resetAppStatePreserveLayout(): Promise<void> {
  // Shell: tabs & navigation only (preserve layout)
  tabsStore.reset();
  navigationService.clearHistory();

  await resetDomainStores();
}

export async function resetAppState(): Promise<void> {
  // Shell: tabs, navigation & layout
  tabsStore.reset();
  navigationService.clearHistory();
  resetLayout();

  await resetDomainStores();
}

async function resetDomainStores(): Promise<void> {
  const { resetActivityState } = await import('@sddp/activities/reset');
  resetActivityState();

  // Clear user-specific localStorage entries
  clearUserLocalStorage();
}

function clearUserLocalStorage(): void {
  const keysToRemove = [
    'sddp-expanded-projects',
    'settings-active-category',
    'settings-selected-project-id',
    'sddp-saved-searches',
    'sddp-notification-settings',
    'sddp-preferences',
  ];
  keysToRemove.forEach((key) => localStorage.removeItem(key));

  // Pattern-matched keys (sidebar-panels-*, task-categories-*)
  const prefixes = ['sidebar-panels-', 'task-categories-'];
  const allKeys = Object.keys(localStorage);
  for (const key of allKeys) {
    if (prefixes.some((prefix) => key.startsWith(prefix))) {
      localStorage.removeItem(key);
    }
  }
}
