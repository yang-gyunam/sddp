import type { Subscriber, Unsubscriber } from 'svelte/store';
import type {
  ToastData,
  ToastDataState,
  ToastEffects,
  ToastStoreLike,
  ToastStoreOptions,
} from './toast.store';

const initialState: ToastDataState = {
  toasts: [],
};

function cloneToastState(state: ToastDataState): ToastDataState {
  return {
    toasts: state.toasts.map((toast) => ({
      ...toast,
      actions: toast.actions ? [...toast.actions] : undefined,
    })),
  };
}

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

export function createRunesToastDataStore(
  effects: ToastEffects,
  options: ToastStoreOptions = {}
): ToastStoreLike {
  let state = cloneToastState(initialState);
  const subscribers = new Set<Subscriber<ToastDataState>>();
  let idCounter = 0;

  const now = options.now ?? Date.now;
  const idFactory = options.idFactory ?? (() => `toast-${now()}-${++idCounter}`);

  function snapshot(): ToastDataState {
    return cloneToastState(state);
  }

  function commit(nextState: ToastDataState): void {
    state = cloneToastState(nextState);
    const latest = snapshot();
    for (const subscriber of subscribers) {
      subscriber(latest);
    }
  }

  function subscribe(run: Subscriber<ToastDataState>): Unsubscriber {
    run(snapshot());
    subscribers.add(run);
    return () => {
      subscribers.delete(run);
    };
  }

  function addToastData(toast: Omit<ToastData, 'id'>): string {
    const id = idFactory();
    const duration = toast.duration ?? getDefaultDuration();
    commit({
      toasts: [...state.toasts, { ...toast, id, duration }],
    });
    return id;
  }

  function removeToastData(id: string): void {
    commit({
      toasts: state.toasts.filter((toast) => toast.id !== id),
    });
  }

  function clearAll(): void {
    commit({ toasts: [] });
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
    getSnapshot: snapshot,
  };
}
