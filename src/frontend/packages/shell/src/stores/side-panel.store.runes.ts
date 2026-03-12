import type { Subscriber, Unsubscriber } from 'svelte/store';
import type { SidePanelState } from '../types';
import { clamp } from '../utils/number.utils';
import type { SidePanelScheduler, SidePanelStoreLike } from './side-panel.store';

const ANIMATION_DELAY_MS = 300;

function cloneSidePanelState(state: SidePanelState): SidePanelState {
  return {
    ...state,
    props: { ...state.props },
  };
}

export function createRunesSidePanelStore(
  initialState: SidePanelState,
  scheduler: SidePanelScheduler
): SidePanelStoreLike {
  let state = cloneSidePanelState(initialState);
  const subscribers = new Set<Subscriber<SidePanelState>>();

  function snapshot(): SidePanelState {
    return cloneSidePanelState(state);
  }

  function commit(nextState: SidePanelState): void {
    state = cloneSidePanelState(nextState);
    const latest = snapshot();
    for (const subscriber of subscribers) {
      subscriber(latest);
    }
  }

  return {
    subscribe: (run: Subscriber<SidePanelState>): Unsubscriber => {
      run(snapshot());
      subscribers.add(run);
      return () => {
        subscribers.delete(run);
      };
    },
    show: (title, component, props = {}) => {
      commit({
        ...state,
        visible: true,
        title,
        component,
        props,
        animating: true,
      });

      scheduler.schedule(() => {
        commit({ ...state, animating: false });
      }, ANIMATION_DELAY_MS);
    },
    hide: () => {
      commit({
        ...state,
        animating: true,
      });

      scheduler.schedule(() => {
        commit({
          ...state,
          visible: false,
          animating: false,
          component: null,
          props: {},
        });
      }, ANIMATION_DELAY_MS);
    },
    toggle: () => {
      if (state.visible) {
        commit({ ...state, animating: true });
        scheduler.schedule(() => {
          commit({
            ...state,
            visible: false,
            animating: false,
          });
        }, ANIMATION_DELAY_MS);
        return;
      }

      commit({
        ...state,
        visible: true,
        animating: true,
      });
      scheduler.schedule(() => {
        commit({ ...state, animating: false });
      }, ANIMATION_DELAY_MS);
    },
    setWidth: (width) => {
      commit({
        ...state,
        width: clamp(width, state.minWidth, state.maxWidth),
      });
    },
    startResize: () => {
      commit({ ...state, resizing: true });
    },
    endResize: () => {
      commit({ ...state, resizing: false });
    },
    setMobile: (isMobile) => {
      commit({ ...state, isMobile });
    },
    updateProps: (props) => {
      commit({
        ...state,
        props: { ...state.props, ...props },
      });
    },
    reset: () => {
      commit(initialState);
    },
    get: () => snapshot(),
  };
}
