/**
 * Global Error Handler
 * Captures uncaught errors and unhandled promise rejections
 * and reports them to the problems store for display in Bottom Panel.
 */

import { problems } from '../stores/panel-content.store';
import { panel } from '../stores/panel.store';

export interface ErrorHandlerOptions {
  logToConsole?: boolean;
  showPanelOnError?: boolean;
}

let isInitialized = false;
let options: ErrorHandlerOptions = {
  logToConsole: true,
  showPanelOnError: false,
};

function formatStack(stack?: string): { file?: string; line?: number; column?: number } {
  if (!stack) return {};

  const match = stack.match(/at\s+(?:.*?\s+\()?(.+?):(\d+):(\d+)\)?/);
  if (match) {
    return {
      file: match[1]!,
      line: parseInt(match[2]!, 10),
      column: parseInt(match[3]!, 10),
    };
  }
  return {};
}

function handleError(event: ErrorEvent): void {
  const { file, line, column } = formatStack(event.error?.stack);

  problems.addProblem({
    severity: 'error',
    type: 'runtime',
    message: event.message || 'Unknown error',
    source: 'Runtime',
    file: file || event.filename,
    line: line || event.lineno,
    column: column || event.colno,
    code: event.error?.name || 'UNCAUGHT_ERROR',
    stack: event.error?.stack,
  });

  if (options.logToConsole) {
    console.error('[ErrorHandler] Uncaught error:', event.error || event.message);
  }

  if (options.showPanelOnError) {
    panel.show();
    panel.setActiveTab('problems');
  }
}

function handleUnhandledRejection(event: PromiseRejectionEvent): void {
  const error = event.reason;
  const message = error instanceof Error ? error.message : String(error);
  const stack = error instanceof Error ? error.stack : undefined;
  const { file, line, column } = error instanceof Error ? formatStack(error.stack) : {};

  problems.addProblem({
    severity: 'error',
    type: 'runtime',
    message: `Unhandled Promise Rejection: ${message}`,
    source: 'Runtime',
    file,
    line,
    column,
    code: error instanceof Error ? error.name : 'UNHANDLED_REJECTION',
    stack,
  });

  if (options.logToConsole) {
    console.error('[ErrorHandler] Unhandled rejection:', error);
  }

  if (options.showPanelOnError) {
    panel.show();
    panel.setActiveTab('problems');
  }
}

export function initErrorHandler(opts?: ErrorHandlerOptions): void {
  if (isInitialized) {
    console.warn('[ErrorHandler] Already initialized');
    return;
  }

  options = { ...options, ...opts };

  window.addEventListener('error', handleError);
  window.addEventListener('unhandledrejection', handleUnhandledRejection);

  isInitialized = true;

  if (options.logToConsole) {
    console.info('[ErrorHandler] Global error handler initialized');
  }
}

export function destroyErrorHandler(): void {
  if (!isInitialized) return;

  window.removeEventListener('error', handleError);
  window.removeEventListener('unhandledrejection', handleUnhandledRejection);
  isInitialized = false;
}

export function reportError(
  message: string,
  opts?: {
    source?: string;
    file?: string;
    line?: number;
    column?: number;
    code?: string;
  }
): void {
  problems.addProblem({
    severity: 'error',
    message,
    source: opts?.source || 'Application',
    file: opts?.file,
    line: opts?.line,
    column: opts?.column,
    code: opts?.code,
  });
}

export function reportWarning(
  message: string,
  opts?: {
    source?: string;
    file?: string;
    line?: number;
    column?: number;
    code?: string;
  }
): void {
  problems.addProblem({
    severity: 'warning',
    message,
    source: opts?.source || 'Application',
    file: opts?.file,
    line: opts?.line,
    column: opts?.column,
    code: opts?.code,
  });
}

export function reportInfo(
  message: string,
  opts?: {
    source?: string;
    code?: string;
  }
): void {
  problems.addProblem({
    severity: 'info',
    message,
    source: opts?.source || 'Application',
    code: opts?.code,
  });
}

export function showProblemsPanel(): void {
  panel.show();
  panel.setActiveTab('problems');
}
