/**
 * Error Store - Error Notification Management
 * Manages error notifications and toast messages
 */

import { createStore, type Store } from '@sddp/shell/core';
import type { ErrorNotification } from '$types';

interface ErrorState {
  errors: ErrorNotification[];
}

// Create the error store
const errorStore: Store<ErrorState> = createStore<ErrorState>({
  errors: [],
});

// Auto-dismiss timeout (ms)
const AUTO_DISMISS_TIMEOUT = 5000;

/**
 * Generate a unique ID for notifications
 */
function generateId(): string {
  return `error-${Date.now()}-${Math.random().toString(36).substring(2, 9)}`;
}

/**
 * Get current error state
 */
export function getErrorState(): ErrorState {
  return errorStore.get();
}

/**
 * Get all active (non-dismissed) errors
 */
export function getActiveErrors(): ErrorNotification[] {
  return errorStore.get().errors.filter((e) => !e.dismissed);
}

/**
 * Add an error notification
 */
export function addError(
  message: string,
  options?: {
    code?: string;
    severity?: ErrorNotification['severity'];
    autoDismiss?: boolean;
    autoDismissTimeout?: number;
  }
): string {
  const id = generateId();
  const notification: ErrorNotification = {
    id,
    message,
    code: options?.code,
    severity: options?.severity ?? 'error',
    timestamp: new Date().toISOString(),
    dismissed: false,
  };

  errorStore.update((state) => ({
    errors: [...state.errors, notification],
  }));

  // Auto-dismiss if enabled (default: true for info/warning, false for error)
  const shouldAutoDismiss = options?.autoDismiss ?? options?.severity !== 'error';
  if (shouldAutoDismiss) {
    const timeout = options?.autoDismissTimeout ?? AUTO_DISMISS_TIMEOUT;
    setTimeout(() => {
      dismissError(id);
    }, timeout);
  }

  return id;
}

/**
 * Add an info notification
 */
export function addInfo(message: string, autoDismiss = true): string {
  return addError(message, { severity: 'info', autoDismiss });
}

/**
 * Add a warning notification
 */
export function addWarning(message: string, autoDismiss = true): string {
  return addError(message, { severity: 'warning', autoDismiss });
}

/**
 * Dismiss a specific error
 */
export function dismissError(id: string): void {
  errorStore.update((state) => ({
    errors: state.errors.map((e) => (e.id === id ? { ...e, dismissed: true } : e)),
  }));

  // Remove from store after animation
  setTimeout(() => {
    removeError(id);
  }, 300);
}

/**
 * Remove a specific error from the store
 */
export function removeError(id: string): void {
  errorStore.update((state) => ({
    errors: state.errors.filter((e) => e.id !== id),
  }));
}

/**
 * Clear all errors
 */
export function clearErrors(): void {
  errorStore.set({ errors: [] });
}

/**
 * Clear all dismissed errors
 */
export function clearDismissedErrors(): void {
  errorStore.update((state) => ({
    errors: state.errors.filter((e) => !e.dismissed),
  }));
}

/**
 * Subscribe to error state changes
 */
export function subscribeErrors(
  listener: (state: ErrorState, prevState: ErrorState) => void
): () => void {
  return errorStore.subscribe(listener);
}

/**
 * Handle API error response
 */
export function handleApiError(error: unknown): string {
  let message = 'An unexpected error occurred';

  if (error instanceof Error) {
    message = error.message;
  } else if (typeof error === 'object' && error !== null) {
    const apiError = error as { message?: string; code?: string };
    message = apiError.message ?? message;
    return addError(message, { code: apiError.code });
  }

  return addError(message);
}

// Export the store for direct access in Svelte components
export { errorStore };
