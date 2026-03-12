import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';

vi.mock('@sddp/shell/stores/panel-content.store', () => ({
  problems: {
    addProblem: vi.fn(),
  },
}));

vi.mock('@sddp/shell/stores/panel.store', () => ({
  panel: {
    show: vi.fn(),
    setActiveTab: vi.fn(),
  },
}));

type EventHandler = (...args: unknown[]) => void;

function createMockWindow() {
  const listeners: Record<string, EventHandler[]> = {};

  const mockWindow = {
    addEventListener: vi.fn((type: string, handler: EventHandler) => {
      if (!listeners[type]) listeners[type] = [];
      listeners[type].push(handler);
    }),
    removeEventListener: vi.fn((type: string, handler: EventHandler) => {
      if (listeners[type]) {
        listeners[type] = listeners[type].filter((h) => h !== handler);
      }
    }),
    dispatchEvent: vi.fn((event: Record<string, unknown> & { type: string }) => {
      const handlers = listeners[event.type] || [];
      handlers.forEach((h) => h(event));
      return true;
    }),
  };

  return mockWindow;
}

import {
  initErrorHandler,
  destroyErrorHandler,
  reportError,
  reportWarning,
  showProblemsPanel,
} from '@sddp/shell/utils/error-handler';
import { problems } from '@sddp/shell/stores/panel-content.store';
import { panel } from '@sddp/shell/stores/panel.store';

describe('error-handler', () => {
  let mockWindow: ReturnType<typeof createMockWindow>;

  beforeEach(() => {
    vi.clearAllMocks();
    mockWindow = createMockWindow();
    vi.stubGlobal('window', mockWindow);
    destroyErrorHandler();
  });

  afterEach(() => {
    destroyErrorHandler();
    vi.unstubAllGlobals();
  });

  it('initializes event listeners', () => {
    const consoleSpy = vi.spyOn(console, 'info').mockImplementation(() => {});
    initErrorHandler({ logToConsole: true });
    expect(mockWindow.addEventListener).toHaveBeenCalledWith('error', expect.any(Function));
    expect(mockWindow.addEventListener).toHaveBeenCalledWith('unhandledrejection', expect.any(Function));
    expect(consoleSpy).toHaveBeenCalledWith('[ErrorHandler] Global error handler initialized');
    consoleSpy.mockRestore();
  });

  it('adds an error to the problems store', () => {
    reportError('Test error message');
    expect(problems.addProblem).toHaveBeenCalledWith({
      severity: 'error',
      message: 'Test error message',
      source: 'Application',
      file: undefined,
      line: undefined,
      column: undefined,
      code: undefined,
    });
  });

  it('adds a warning to the problems store', () => {
    reportWarning('Test warning message');
    expect(problems.addProblem).toHaveBeenCalledWith({
      severity: 'warning',
      message: 'Test warning message',
      source: 'Application',
      file: undefined,
      line: undefined,
      column: undefined,
      code: undefined,
    });
  });

  it('shows the problems panel', () => {
    showProblemsPanel();
    expect(panel.show).toHaveBeenCalled();
    expect(panel.setActiveTab).toHaveBeenCalledWith('problems');
  });
});
