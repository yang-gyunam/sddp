import type { Subscriber, Unsubscriber } from 'svelte/store';
import type { Tab, EditorGroupData } from '../types';
import type { TabsState, TabsStateStoreLike } from './tabs.store';

function cloneTab(tab: Tab): Tab {
  return {
    ...tab,
    props: { ...tab.props },
  };
}

function cloneEditorGroup(group: EditorGroupData): EditorGroupData {
  return {
    ...group,
    tabs: group.tabs.map(cloneTab),
    tabHistory: [...group.tabHistory],
    position: { ...group.position },
  };
}

function cloneTabsState(state: TabsState): TabsState {
  return {
    activeGroupId: state.activeGroupId,
    groups: state.groups.map(cloneEditorGroup),
  };
}

export function createRunesTabsStateStore(initialState: TabsState): TabsStateStoreLike {
  let state = cloneTabsState(initialState);
  const subscribers = new Set<Subscriber<TabsState>>();

  function snapshot(): TabsState {
    return cloneTabsState(state);
  }

  function publish(): void {
    const latest = snapshot();
    for (const subscriber of subscribers) {
      subscriber(latest);
    }
  }

  return {
    subscribe: (run: Subscriber<TabsState>): Unsubscriber => {
      run(snapshot());
      subscribers.add(run);
      return () => {
        subscribers.delete(run);
      };
    },
    set: (nextState) => {
      state = cloneTabsState(nextState);
      publish();
    },
    update: (updater) => {
      state = cloneTabsState(updater(snapshot()));
      publish();
    },
    get: () => snapshot(),
  };
}
