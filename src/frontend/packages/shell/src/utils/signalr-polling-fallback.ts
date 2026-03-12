/**
 * SignalR Polling Fallback
 *
 * After SignalR reconnection fails (5 attempts exhausted, onclose fires),
 * falls back to periodic REST polling for critical data (notifications).
 * Periodically retries SignalR reconnection and stops polling on success.
 */

import { reportWarning, reportInfo, showProblemsPanel } from './error-handler';
import { problems } from '../stores/panel-content.store';

// ============================================
// Types
// ============================================

export type ConnectionMode = 'realtime' | 'polling' | 'disconnected';

export interface PollingFallbackOptions {
  /** Polling interval in ms (default: 30000) */
  pollingIntervalMs?: number;
  /** Interval to retry SignalR reconnection in ms (default: 300000 = 5 min) */
  reconnectRetryIntervalMs?: number;
  /** Whether to clear previous SignalR problems before adding a new one (default: true) */
  clearExistingSignalrProblems?: boolean;
  /** Called on each poll tick — callers run their REST fetches here */
  onPoll?: () => Promise<void>;
  /** Called to attempt SignalR reconnection. Return true if successful. */
  onReconnectAttempt?: () => Promise<boolean>;
  /** Called when connection mode changes */
  onModeChange?: (mode: ConnectionMode) => void;
}

// ============================================
// Module State (Singleton)
// ============================================

let pollTimerId: ReturnType<typeof setInterval> | null = null;
let reconnectTimerId: ReturnType<typeof setInterval> | null = null;
let currentMode: ConnectionMode = 'realtime';
let options: PollingFallbackOptions = {};

function shouldClearSignalrProblems(): boolean {
  return options.clearExistingSignalrProblems ?? true;
}

// ============================================
// Internal Functions
// ============================================

async function executePoll(): Promise<void> {
  if (typeof document !== 'undefined' && document.visibilityState !== 'visible') {
    return;
  }

  try {
    await options.onPoll?.();
  } catch {
    // Polling is best-effort — swallow errors
  }
}

async function attemptReconnect(): Promise<void> {
  try {
    const success = await options.onReconnectAttempt?.();
    if (success) {
      stopPollingFallback();
      currentMode = 'realtime';
      options.onModeChange?.('realtime');

      if (shouldClearSignalrProblems()) {
        problems.clearBySource('SignalR');
      }
      reportInfo('Realtime connection restored - switching to realtime mode.', {
        source: 'SignalR',
        code: 'SIGNALR_RECONNECTED',
      });
    }
  } catch {
    // Reconnect failed — keep polling
  }
}

// ============================================
// Public API
// ============================================

/**
 * Start polling fallback. Called when SignalR onclose fires
 * after all reconnect attempts are exhausted.
 */
export function startPollingFallback(opts?: PollingFallbackOptions): void {
  if (pollTimerId !== null) return; // Already polling

  options = { ...opts };
  const pollInterval = options.pollingIntervalMs ?? 30_000;
  const reconnectInterval = options.reconnectRetryIntervalMs ?? 300_000;

  currentMode = 'polling';
  options.onModeChange?.('polling');

  if (shouldClearSignalrProblems()) {
    problems.clearBySource('SignalR');
  }
  reportWarning('Realtime unavailable - switching to 30-second polling mode.', {
    source: 'SignalR',
    code: 'SIGNALR_POLLING_FALLBACK',
  });
  showProblemsPanel();

  // Immediate first poll
  executePoll();

  // Start periodic polling
  pollTimerId = setInterval(executePoll, pollInterval);

  // Start periodic reconnect retry
  reconnectTimerId = setInterval(attemptReconnect, reconnectInterval);
}

/**
 * Stop polling fallback. Called when SignalR reconnects
 * or when the user logs out.
 */
export function stopPollingFallback(): void {
  if (pollTimerId !== null) {
    clearInterval(pollTimerId);
    pollTimerId = null;
  }
  if (reconnectTimerId !== null) {
    clearInterval(reconnectTimerId);
    reconnectTimerId = null;
  }
}

/**
 * Get current connection mode.
 */
export function getConnectionMode(): ConnectionMode {
  return currentMode;
}

/**
 * Destroy all state and timers. Called on app teardown.
 */
export function destroyPollingFallback(): void {
  stopPollingFallback();
  currentMode = 'realtime';
  options = {};
}
