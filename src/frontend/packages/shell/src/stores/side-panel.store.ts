/**
 * Side Panel Store - Right-side slide-in panel management
 *
 * L-3 migration note:
 * This module keeps the public store contract stable while shadow-validating
 * the runes implementation against the legacy writable implementation.
 */

import { writable, type Subscriber, type Unsubscriber } from 'svelte/store';
import { config } from '../core/services/ConfigurationManager';
import type { SidePanelConfig, SidePanelState } from '../types';
import { clamp } from '../utils/number.utils';
import { createRunesSidePanelStore } from './side-panel.store.runes';

export interface SidePanelScheduler {
  schedule: (callback: () => void, delayMs: number) => number;
}

export interface SidePanelStoreLike {
  subscribe: (run: Subscriber<SidePanelState>) => Unsubscriber;
  show: (title: string, component: string, props?: Record<string, unknown>) => void;
  hide: () => void;
  toggle: () => void;
  setWidth: (width: number) => void;
  startResize: () => void;
  endResize: () => void;
  setMobile: (isMobile: boolean) => void;
  updateProps: (props: Record<string, unknown>) => void;
  reset: () => void;
  get: () => SidePanelState;
}

type SidePanelMismatchContext = {
  action: string;
  primaryState: SidePanelState;
  shadowState: SidePanelState;
};

const DEFAULT_WIDTH = 400;
const MIN_WIDTH = 280;
const MAX_WIDTH = 800;
const ANIMATION_DELAY_MS = 300;

const initialState: SidePanelState = {
  visible: false,
  title: '',
  component: null,
  props: {},
  width: DEFAULT_WIDTH,
  minWidth: MIN_WIDTH,
  maxWidth: MAX_WIDTH,
  animating: false,
  resizing: false,
  isMobile: false,
};

function cloneSidePanelState(state: SidePanelState): SidePanelState {
  return {
    ...state,
    props: { ...state.props },
  };
}

function normalizeSidePanelState(state: SidePanelState): unknown {
  return {
    visible: state.visible,
    title: state.title,
    component: state.component,
    props: state.props,
    width: state.width,
    minWidth: state.minWidth,
    maxWidth: state.maxWidth,
    animating: state.animating,
    resizing: state.resizing,
    isMobile: state.isMobile,
  };
}

function areEqual(left: unknown, right: unknown): boolean {
  return JSON.stringify(left) === JSON.stringify(right);
}

function createNativeScheduler(): SidePanelScheduler {
  return {
    schedule: (callback, delayMs) => globalThis.setTimeout(callback, delayMs) as unknown as number,
  };
}

function createWritableSidePanelStore(
  panelConfig?: SidePanelConfig,
  scheduler: SidePanelScheduler = createNativeScheduler()
): SidePanelStoreLike {
  const configuredInitialState: SidePanelState = {
    ...initialState,
    width: panelConfig?.defaultWidth || DEFAULT_WIDTH,
    minWidth: panelConfig?.minWidth || MIN_WIDTH,
    maxWidth: panelConfig?.maxWidth || MAX_WIDTH,
  };
  const { subscribe, set, update } = writable<SidePanelState>(cloneSidePanelState(configuredInitialState));

  let currentState = cloneSidePanelState(configuredInitialState);
  subscribe((state) => {
    currentState = cloneSidePanelState(state);
  });

  return {
    subscribe,
    show: (title, component, props = {}) => {
      update((state) => ({
        ...state,
        visible: true,
        title,
        component,
        props,
        animating: true,
      }));

      scheduler.schedule(() => {
        update((state) => ({ ...state, animating: false }));
      }, ANIMATION_DELAY_MS);
    },
    hide: () => {
      update((state) => ({
        ...state,
        animating: true,
      }));

      scheduler.schedule(() => {
        update((state) => ({
          ...state,
          visible: false,
          animating: false,
          component: null,
          props: {},
        }));
      }, ANIMATION_DELAY_MS);
    },
    toggle: () => {
      const state = currentState;
      if (state.visible) {
        update((current) => ({ ...current, animating: true }));
        scheduler.schedule(() => {
          update((current) => ({
            ...current,
            visible: false,
            animating: false,
          }));
        }, ANIMATION_DELAY_MS);
        return;
      }

      update((current) => ({
        ...current,
        visible: true,
        animating: true,
      }));
      scheduler.schedule(() => {
        update((current) => ({ ...current, animating: false }));
      }, ANIMATION_DELAY_MS);
    },
    setWidth: (width) => {
      update((state) => ({
        ...state,
        width: clamp(width, state.minWidth, state.maxWidth),
      }));
    },
    startResize: () => {
      update((state) => ({ ...state, resizing: true }));
    },
    endResize: () => {
      update((state) => ({ ...state, resizing: false }));
    },
    setMobile: (isMobile) => {
      update((state) => ({ ...state, isMobile }));
    },
    updateProps: (props) => {
      update((state) => ({
        ...state,
        props: { ...state.props, ...props },
      }));
    },
    reset: () => {
      set(cloneSidePanelState(configuredInitialState));
    },
    get: () => cloneSidePanelState(currentState),
  };
}

function createPairedSchedulers(onSettled: () => void): {
  primary: SidePanelScheduler;
  shadow: SidePanelScheduler;
} {
  let counter = 0;
  const queue = new Map<number, { primary?: () => void; shadow?: () => void }>();

  return {
    primary: {
      schedule: (callback, delayMs) => {
        const id = ++counter;
        queue.set(id, { primary: callback });
        globalThis.setTimeout(() => {
          const entry = queue.get(id);
          entry?.primary?.();
          entry?.shadow?.();
          queue.delete(id);
          onSettled();
        }, delayMs);
        return id;
      },
    },
    shadow: {
      schedule: (callback) => {
        const pending = [...queue.entries()].find(([, entry]) => !entry.shadow);
        if (!pending) {
          const id = ++counter;
          queue.set(id, { shadow: callback });
          return id;
        }

        pending[1].shadow = callback;
        return pending[0];
      },
    },
  };
}

function createParityAwareSidePanelStore(
  primary: SidePanelStoreLike,
  shadow: SidePanelStoreLike,
  reportMismatch: (context: SidePanelMismatchContext) => void
): SidePanelStoreLike {
  const listeners = new Set<Subscriber<SidePanelState>>();
  let currentState = primary.get();

  function compare(action: string): void {
    const primaryState = primary.get();
    const shadowState = shadow.get();

    if (!areEqual(normalizeSidePanelState(primaryState), normalizeSidePanelState(shadowState))) {
      reportMismatch({ action, primaryState, shadowState });
    }
  }

  primary.subscribe((state) => {
    currentState = cloneSidePanelState(state);
    for (const listener of listeners) {
      listener(cloneSidePanelState(state));
    }
  });

  function subscribe(run: Subscriber<SidePanelState>): Unsubscriber {
    run(cloneSidePanelState(currentState));
    listeners.add(run);
    return () => {
      listeners.delete(run);
    };
  }

  return {
    subscribe,
    show: (title, component, props) => {
      primary.show(title, component, props);
      shadow.show(title, component, props);
      compare('show');
    },
    hide: () => {
      primary.hide();
      shadow.hide();
      compare('hide');
    },
    toggle: () => {
      primary.toggle();
      shadow.toggle();
      compare('toggle');
    },
    setWidth: (width) => {
      primary.setWidth(width);
      shadow.setWidth(width);
      compare('setWidth');
    },
    startResize: () => {
      primary.startResize();
      shadow.startResize();
      compare('startResize');
    },
    endResize: () => {
      primary.endResize();
      shadow.endResize();
      compare('endResize');
    },
    setMobile: (isMobile) => {
      primary.setMobile(isMobile);
      shadow.setMobile(isMobile);
      compare('setMobile');
    },
    updateProps: (props) => {
      primary.updateProps(props);
      shadow.updateProps(props);
      compare('updateProps');
    },
    reset: () => {
      primary.reset();
      shadow.reset();
      compare('reset');
    },
    get: () => cloneSidePanelState(currentState),
  };
}

function createStore(): SidePanelStoreLike {
  const enableRunesSidePanelStore = config.isRunesSidePanelStoreEnabled();
  const enableShadowParity = config.isRunesStoreShadowParityEnabled();

  if (!enableRunesSidePanelStore && !enableShadowParity) {
    return createWritableSidePanelStore();
  }

  let handleScheduledParity = () => {};
  const schedulers = createPairedSchedulers(() => {
    handleScheduledParity();
  });

  const writableStore = createWritableSidePanelStore(
    undefined,
    enableRunesSidePanelStore ? schedulers.shadow : schedulers.primary
  );
  const runesStore = createRunesSidePanelStore(
    initialState,
    enableRunesSidePanelStore ? schedulers.primary : schedulers.shadow
  );

  const primaryStore = enableRunesSidePanelStore ? runesStore : writableStore;
  const shadowStore = enableRunesSidePanelStore ? writableStore : runesStore;

  if (!enableShadowParity) {
    return primaryStore;
  }

  const comparedStore = createParityAwareSidePanelStore(primaryStore, shadowStore, ({ action, primaryState, shadowState }) => {
    console.error('[side-panel.store parity mismatch]', {
      action,
      primaryState: normalizeSidePanelState(primaryState),
      shadowState: normalizeSidePanelState(shadowState),
    });
  });

  handleScheduledParity = () => {
    const primaryState = primaryStore.get();
    const shadowState = shadowStore.get();

    if (!areEqual(normalizeSidePanelState(primaryState), normalizeSidePanelState(shadowState))) {
      console.error('[side-panel.store parity mismatch]', {
        action: 'scheduled',
        primaryState: normalizeSidePanelState(primaryState),
        shadowState: normalizeSidePanelState(shadowState),
      });
    }
  };
  return comparedStore;
}

export const __internal = {
  cloneSidePanelState,
  normalizeSidePanelState,
  createWritableSidePanelStore,
  createParityAwareSidePanelStore,
  createPairedSchedulers,
  createRunesSidePanelStore,
};

export const sidePanel = createStore();

export const $sidePanel = initialState;
