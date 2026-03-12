/**
 * Visibility Sync Utility
 * Detects tab focus changes via visibilitychange event and performs:
 * - JWT token refresh if expired
 * - Notification refresh on tab return
 * Debounced with a minimum interval to prevent rapid tab-switching overhead.
 */

import { hasSession, getAccessToken, refreshToken } from '../auth/stores/auth.store';

export interface VisibilitySyncOptions {
  /** Called after successful token refresh */
  onTokenRefreshed?: () => void;
  /** Called when refresh token fails (session expired) */
  onSessionExpired?: () => void;
  /** Called to refresh notifications on tab return */
  onRefreshNotifications?: () => void;
  /** Minimum interval between syncs in ms (default: 30000) */
  minIntervalMs?: number;
}

let isInitialized = false;
let syncOptions: VisibilitySyncOptions = {};
let lastSyncTimestamp = 0;

function handleVisibilityChange(): void {
  if (document.visibilityState !== 'visible') return;

  const now = Date.now();
  const minInterval = syncOptions.minIntervalMs ?? 30_000;

  if (now - lastSyncTimestamp < minInterval) return;
  lastSyncTimestamp = now;

  if (!hasSession()) return;

  const token = getAccessToken();

  if (!token) {
    // Token expired — attempt refresh
    refreshToken().then((success) => {
      if (success) {
        syncOptions.onTokenRefreshed?.();
        syncOptions.onRefreshNotifications?.();
      } else {
        syncOptions.onSessionExpired?.();
      }
    });
  } else {
    // Token still valid — just refresh notifications
    syncOptions.onRefreshNotifications?.();
  }
}

/**
 * Initialize visibility sync listener
 */
export function initVisibilitySync(opts?: VisibilitySyncOptions): void {
  if (isInitialized) return;

  syncOptions = { ...opts };
  lastSyncTimestamp = Date.now();

  document.addEventListener('visibilitychange', handleVisibilityChange);
  isInitialized = true;
}

/**
 * Remove visibility sync listener and reset state
 */
export function destroyVisibilitySync(): void {
  if (!isInitialized) return;

  document.removeEventListener('visibilitychange', handleVisibilityChange);
  isInitialized = false;
  syncOptions = {};
  lastSyncTimestamp = 0;
}
