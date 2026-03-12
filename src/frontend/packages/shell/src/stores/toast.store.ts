/**
 * ToastData Store - Manage toast notifications
 * - success/info -> OUTPUT panel
 * - warning -> PROBLEMS panel
 * - error -> PROBLEMS panel + Bottom Panel open
 *
 * L-3 migration note:
 * This module keeps the public store contract stable while shadow-validating
 * the runes implementation against the legacy writable implementation.
 */

import { writable, type Subscriber, type Unsubscriber } from 'svelte/store';
import { config } from '../core/services/ConfigurationManager';
import { problems, output } from './panel-content.store';
import { panel } from './panel.store';
import { createRunesToastDataStore } from './toast.store.runes';

export type ToastType = 'info' | 'success' | 'warning' | 'error';
export type ToastActionVariant = 'primary' | 'secondary' | 'ghost' | 'danger' | 'outline';

export interface ToastAction {
  label: string;
  onClick?: () => void | Promise<void>;
  dismissOnClick?: boolean;
  variant?: ToastActionVariant;
}

export interface ToastData {
  id: string;
  type: ToastType;
  title?: string;
  message: string;
  duration?: number;
  closable?: boolean;
  actions?: ToastAction[];
  onContentClick?: () => void;
}

export interface ToastDataState {
  toasts: ToastData[];
}

export interface ToastProblemPayload {
  severity: 'info' | 'warning' | 'error';
  type: 'runtime';
  message: string;
  source: 'Toast';
  code: string;
}

export interface ToastEffects {
  outputInfo: (source: string, message: string) => void;
  addProblem: (problem: ToastProblemPayload) => void;
  panelShow: () => void;
  panelSetActiveTab: (tabId: string) => void;
}

export interface ToastStoreOptions {
  now?: () => number;
  idFactory?: () => string;
}

export interface ToastStoreLike {
  subscribe: (run: Subscriber<ToastDataState>) => Unsubscriber;
  addToastData: (toast: Omit<ToastData, 'id'>) => string;
  removeToastData: (id: string) => void;
  clearAll: () => void;
  info: (message: string, options?: Partial<Omit<ToastData, 'id' | 'type' | 'message'>>) => string;
  success: (message: string, options?: Partial<Omit<ToastData, 'id' | 'type' | 'message'>>) => string;
  warning: (message: string, options?: Partial<Omit<ToastData, 'id' | 'type' | 'message'>>) => string;
  error: (message: string, options?: Partial<Omit<ToastData, 'id' | 'type' | 'message'>>) => string;
  getSnapshot: () => ToastDataState;
}

type ToastEffectCall =
  | { target: 'output.info'; args: [string, string] }
  | { target: 'problems.addProblem'; args: [ToastProblemPayload] }
  | { target: 'panel.show'; args: [] }
  | { target: 'panel.setActiveTab'; args: [string] };

const initialState: ToastDataState = {
  toasts: [],
};

function getDefaultDuration(): number {
  try {
    const stored = localStorage.getItem('sddp-preferences');
    if (stored) {
      const prefs = JSON.parse(stored);
      if (prefs.toastDuration && typeof prefs.toastDuration === 'number') {
        return prefs.toastDuration;
      }
    }
  } catch {
    // ignore invalid preference payloads
  }
  return 0;
}

function cloneToastState(state: ToastDataState): ToastDataState {
  return {
    toasts: state.toasts.map((toastEntry) => ({
      ...toastEntry,
      actions: toastEntry.actions ? [...toastEntry.actions] : undefined,
    })),
  };
}

function normalizeToastState(state: ToastDataState): unknown {
  return {
    toasts: state.toasts.map((toastEntry) => ({
      id: toastEntry.id,
      type: toastEntry.type,
      title: toastEntry.title ?? null,
      message: toastEntry.message,
      duration: toastEntry.duration ?? null,
      closable: toastEntry.closable ?? null,
      actions: toastEntry.actions?.map((action) => ({
        label: action.label,
        dismissOnClick: action.dismissOnClick ?? null,
        variant: action.variant ?? null,
        hasOnClick: typeof action.onClick === 'function',
      })) ?? [],
      hasOnContentClick: typeof toastEntry.onContentClick === 'function',
    })),
  };
}

function createWritableToastDataStore(
  effects: ToastEffects,
  options: ToastStoreOptions = {}
): ToastStoreLike {
  const { subscribe, update } = writable<ToastDataState>(cloneToastState(initialState));

  let idCounter = 0;
  const now = options.now ?? Date.now;
  const idFactory = options.idFactory ?? (() => `toast-${now()}-${++idCounter}`);

  function getSnapshot(): ToastDataState {
    let snapshot = cloneToastState(initialState);
    const unsubscribe = subscribe((state) => {
      snapshot = cloneToastState(state);
    });
    unsubscribe();
    return snapshot;
  }

  function addToastData(toastEntry: Omit<ToastData, 'id'>): string {
    const id = idFactory();
    const duration = toastEntry.duration ?? getDefaultDuration();
    update((state) => ({
      ...state,
      toasts: [...state.toasts, { ...toastEntry, id, duration }],
    }));
    return id;
  }

  function removeToastData(id: string): void {
    update((state) => ({
      ...state,
      toasts: state.toasts.filter((toastEntry) => toastEntry.id !== id),
    }));
  }

  function clearAll(): void {
    update((state) => ({
      ...state,
      toasts: [],
    }));
  }

  return {
    subscribe,
    addToastData,
    removeToastData,
    clearAll,
    info: (message, optionsOverride) => {
      effects.outputInfo('Application', message);
      effects.addProblem({ severity: 'info', type: 'runtime', message, source: 'Toast', code: 'TOAST_INFO' });
      return addToastData({ type: 'info', message, ...optionsOverride });
    },
    success: (message, optionsOverride) => {
      effects.outputInfo('Application', message);
      effects.addProblem({ severity: 'info', type: 'runtime', message, source: 'Toast', code: 'TOAST_SUCCESS' });
      return addToastData({ type: 'success', message, ...optionsOverride });
    },
    warning: (message, optionsOverride) => {
      effects.addProblem({ severity: 'warning', type: 'runtime', message, source: 'Toast', code: 'TOAST_WARNING' });
      return addToastData({ type: 'warning', message, ...optionsOverride });
    },
    error: (message, optionsOverride) => {
      effects.addProblem({ severity: 'error', type: 'runtime', message, source: 'Toast', code: 'TOAST_ERROR' });
      effects.panelShow();
      effects.panelSetActiveTab('problems');
      return addToastData({ type: 'error', message, ...optionsOverride });
    },
    getSnapshot,
  };
}

function createRecordingEffects(
  mode: 'active' | 'shadow',
  effectLog: ToastEffectCall[]
): ToastEffects {
  return {
    outputInfo: (source, message) => {
      effectLog.push({ target: 'output.info', args: [source, message] });
      if (mode === 'active') {
        output.info(source, message);
      }
    },
    addProblem: (problem) => {
      effectLog.push({ target: 'problems.addProblem', args: [problem] });
      if (mode === 'active') {
        problems.addProblem(problem);
      }
    },
    panelShow: () => {
      effectLog.push({ target: 'panel.show', args: [] });
      if (mode === 'active') {
        panel.show();
      }
    },
    panelSetActiveTab: (tabId) => {
      effectLog.push({ target: 'panel.setActiveTab', args: [tabId] });
      if (mode === 'active') {
        panel.setActiveTab(tabId);
      }
    },
  };
}

function createPairedIdFactories(now: () => number): {
  primary: () => string;
  shadow: () => string;
} {
  let counter = 0;
  const queue: string[] = [];

  return {
    primary: () => {
      const id = `toast-${now()}-${++counter}`;
      queue.push(id);
      return id;
    },
    shadow: () => queue.shift() ?? `toast-${now()}-${++counter}`,
  };
}

function areEqual(left: unknown, right: unknown): boolean {
  return JSON.stringify(left) === JSON.stringify(right);
}

function createParityAwareToastStore(
  primary: ToastStoreLike,
  shadow: ToastStoreLike,
  primaryEffectsLog: ToastEffectCall[],
  shadowEffectsLog: ToastEffectCall[],
  reportMismatch: (context: {
    action: string;
    primaryState: ToastDataState;
    shadowState: ToastDataState;
    primaryEffects: ToastEffectCall[];
    shadowEffects: ToastEffectCall[];
  }) => void
): ToastStoreLike {
  const listeners = new Set<Subscriber<ToastDataState>>();
  let currentState = primary.getSnapshot();

  primary.subscribe((state) => {
    currentState = cloneToastState(state);
    for (const listener of listeners) {
      listener(cloneToastState(state));
    }
  });

  function subscribe(run: Subscriber<ToastDataState>): Unsubscriber {
    run(cloneToastState(currentState));
    listeners.add(run);
    return () => {
      listeners.delete(run);
    };
  }

  function execute<T>(
    action: string,
    invokePrimary: () => T,
    invokeShadow: () => void
  ): T {
    primaryEffectsLog.length = 0;
    shadowEffectsLog.length = 0;

    const result = invokePrimary();
    invokeShadow();

    const primaryState = primary.getSnapshot();
    const shadowState = shadow.getSnapshot();
    const primaryEffects = [...primaryEffectsLog];
    const shadowEffects = [...shadowEffectsLog];

    if (
      !areEqual(normalizeToastState(primaryState), normalizeToastState(shadowState))
      || !areEqual(primaryEffects, shadowEffects)
    ) {
      reportMismatch({ action, primaryState, shadowState, primaryEffects, shadowEffects });
    }

    return result;
  }

  return {
    subscribe,
    addToastData: (toastEntry) => execute(
      'addToastData',
      () => primary.addToastData(toastEntry),
      () => { shadow.addToastData(toastEntry); }
    ),
    removeToastData: (id) => {
      execute(
        'removeToastData',
        () => {
          primary.removeToastData(id);
          return undefined;
        },
        () => { shadow.removeToastData(id); }
      );
    },
    clearAll: () => {
      execute(
        'clearAll',
        () => {
          primary.clearAll();
          return undefined;
        },
        () => { shadow.clearAll(); }
      );
    },
    info: (message, optionsOverride) => {
      return execute(
        'info',
        () => primary.info(message, optionsOverride),
        () => { shadow.info(message, optionsOverride); }
      );
    },
    success: (message, optionsOverride) => {
      return execute(
        'success',
        () => primary.success(message, optionsOverride),
        () => { shadow.success(message, optionsOverride); }
      );
    },
    warning: (message, optionsOverride) => {
      return execute(
        'warning',
        () => primary.warning(message, optionsOverride),
        () => { shadow.warning(message, optionsOverride); }
      );
    },
    error: (message, optionsOverride) => {
      return execute(
        'error',
        () => primary.error(message, optionsOverride),
        () => { shadow.error(message, optionsOverride); }
      );
    },
    getSnapshot: () => cloneToastState(currentState),
  };
}

function createToastStore(): ToastStoreLike {
  const enableRunesToastStore = config.isRunesToastStoreEnabled();
  const enableShadowParity = config.isRunesStoreShadowParityEnabled();

  if (!enableRunesToastStore && !enableShadowParity) {
    return createWritableToastDataStore(createRecordingEffects('active', []));
  }

  const now = Date.now;
  const idFactories = createPairedIdFactories(now);
  const primaryEffectsLog: ToastEffectCall[] = [];
  const shadowEffectsLog: ToastEffectCall[] = [];

  const legacyStore = createWritableToastDataStore(
    createRecordingEffects(enableRunesToastStore ? 'shadow' : 'active', enableRunesToastStore ? shadowEffectsLog : primaryEffectsLog),
    { now, idFactory: enableRunesToastStore ? idFactories.shadow : idFactories.primary }
  );

  const runesStore = createRunesToastDataStore(
    createRecordingEffects(enableRunesToastStore ? 'active' : 'shadow', enableRunesToastStore ? primaryEffectsLog : shadowEffectsLog),
    { now, idFactory: enableRunesToastStore ? idFactories.primary : idFactories.shadow }
  );

  const primaryStore = enableRunesToastStore ? runesStore : legacyStore;
  const shadowStore = enableRunesToastStore ? legacyStore : runesStore;

  if (!enableShadowParity) {
    return primaryStore;
  }

  return createParityAwareToastStore(primaryStore, shadowStore, primaryEffectsLog, shadowEffectsLog, ({ action, primaryState, shadowState, primaryEffects, shadowEffects }) => {
    console.error('[toast.store parity mismatch]', {
      action,
      primaryState: normalizeToastState(primaryState),
      shadowState: normalizeToastState(shadowState),
      primaryEffects,
      shadowEffects,
    });
  });
}

export const __internal = {
  cloneToastState,
  normalizeToastState,
  getDefaultDuration,
  createWritableToastDataStore,
  createParityAwareToastStore,
  createRecordingEffects,
  createPairedIdFactories,
  createRunesToastDataStore,
};

export const toastStore = createToastStore();

export const toast = {
  info: toastStore.info,
  success: toastStore.success,
  warning: toastStore.warning,
  error: toastStore.error,
  dismiss: toastStore.removeToastData,
  clear: toastStore.clearAll,
};
