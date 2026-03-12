/**
 * Command Palette Store
 * Manages command palette state, commands registry, and execution
 *
 * L-3 migration note:
 * This module keeps the public store contract stable while shadow-validating
 * the runes implementation against the legacy writable implementation.
 */

import { writable, type Subscriber, type Unsubscriber } from 'svelte/store';
import { config } from '../core/services/ConfigurationManager';
import type {
  Command,
  CommandCategory,
  CommandPaletteState,
  CommandExecutionResult,
} from '../types/command.types';
import { tabActions, tabsStore } from './tabs.store';
import { toggleSidebar, setActiveActivityItem, openSidebar } from './layout.store';
import { panel } from './panel.store';
import { createRunesCommandPaletteStore } from './command.store.runes';

type TabsStateSnapshot = {
  groups: Array<{
    id: string;
    tabs: Array<{ id: string; closable?: boolean }>;
    activeTab: string;
  }>;
  activeGroupId: string;
};

type CommandEffectCall =
  | { target: 'tabs.closeTab'; args: [string, string?] }
  | { target: 'tabs.splitGroup'; args: ['horizontal' | 'vertical', string] }
  | { target: 'layout.toggleSidebar'; args: [] }
  | { target: 'layout.setActiveActivityItem'; args: [string] }
  | { target: 'layout.openSidebar'; args: [] }
  | { target: 'panel.toggle'; args: [] };

export interface CommandStoreDependencies {
  getTabsState: () => TabsStateSnapshot;
  closeTab: (tabId: string, groupId?: string) => void;
  splitGroup: (direction: 'horizontal' | 'vertical', groupId: string) => void;
  toggleSidebar: () => void;
  setActiveActivityItem: (activityId: string) => void;
  openSidebar: () => void;
  togglePanel: () => void;
}

export interface CommandPaletteStoreLike {
  subscribe: (run: Subscriber<CommandPaletteState>) => Unsubscriber;
  show: () => void;
  hide: () => void;
  toggle: () => void;
  setQuery: (query: string) => void;
  selectNext: () => void;
  selectPrevious: () => void;
  selectIndex: (index: number) => void;
  executeSelected: () => Promise<CommandExecutionResult>;
  executeCommand: (commandId: string) => Promise<CommandExecutionResult>;
  registerCommand: (command: Command) => void;
  unregisterCommand: (commandId: string) => void;
  getAllCommands: () => Command[];
  getCommandsByCategory: (categoryId: string) => Command[];
  getState: () => CommandPaletteState;
}

type CommandMismatchContext = {
  action: string;
  primaryState: CommandPaletteState;
  shadowState: CommandPaletteState;
  primaryRegistry: Command[];
  shadowRegistry: Command[];
  primaryEffects: CommandEffectCall[];
  shadowEffects: CommandEffectCall[];
  primaryResult?: CommandExecutionResult;
  shadowResult?: CommandExecutionResult;
};

const defaultCategories: CommandCategory[] = [
  { id: 'tab', label: 'Tab', icon: 'file', priority: 1 },
  { id: 'view', label: 'View', icon: 'eye', priority: 2 },
  { id: 'go', label: 'Go', icon: 'arrow-right', priority: 3 },
];

const builtInCommandIds = new Set([
  'tab.close',
  'tab.closeAll',
  'view.splitRight',
  'view.splitDown',
  'view.toggleSidebar',
  'view.togglePanel',
  'go.dashboard',
  'go.projects',
  'go.conversations',
  'go.tasks',
  'go.settings',
]);

const initialState: CommandPaletteState = {
  visible: false,
  query: '',
  selectedIndex: 0,
  filteredCommands: [],
  categories: defaultCategories,
};

const defaultDependencies: CommandStoreDependencies = {
  getTabsState: () => tabsStore.get() as TabsStateSnapshot,
  closeTab: (tabId, groupId) => {
    tabActions.closeTab(tabId, groupId);
  },
  splitGroup: (direction, groupId) => {
    tabActions.splitGroup(direction, groupId);
  },
  toggleSidebar: () => {
    toggleSidebar();
  },
  setActiveActivityItem: (activityId) => {
    setActiveActivityItem(activityId);
  },
  openSidebar: () => {
    openSidebar();
  },
  togglePanel: () => {
    panel.toggle();
  },
};

function cloneCommandCategory(category: CommandCategory): CommandCategory {
  return { ...category };
}

function cloneCommand(command: Command): Command {
  return { ...command };
}

function cloneCommandPaletteState(state: CommandPaletteState): CommandPaletteState {
  return {
    ...state,
    filteredCommands: state.filteredCommands.map(cloneCommand),
    categories: state.categories.map(cloneCommandCategory),
  };
}

function normalizeCommand(command: Command): unknown {
  return {
    id: command.id,
    label: command.label,
    description: command.description ?? null,
    category: command.category,
    keybinding: command.keybinding ?? null,
    icon: command.icon ?? null,
    hasWhen: typeof command.when === 'function',
  };
}

function normalizeCommandPaletteState(state: CommandPaletteState): unknown {
  return {
    visible: state.visible,
    query: state.query,
    selectedIndex: state.selectedIndex,
    filteredCommands: state.filteredCommands.map(normalizeCommand),
    categories: state.categories.map((category) => ({
      id: category.id,
      label: category.label,
      icon: category.icon ?? null,
      priority: category.priority ?? null,
    })),
  };
}

function normalizeCommandRegistry(commands: Command[]): unknown {
  return commands.map(normalizeCommand);
}

function areEqual(left: unknown, right: unknown): boolean {
  return JSON.stringify(left) === JSON.stringify(right);
}

function createDefaultCommands(dependencies: CommandStoreDependencies): Command[] {
  return [
    {
      id: 'tab.close',
      label: 'Close Tab',
      description: 'Close the current tab',
      category: 'tab',
      keybinding: 'Ctrl+W',
      icon: 'x',
      action: () => {
        const state = dependencies.getTabsState();
        const group = state.groups.find((entry) => entry.id === state.activeGroupId);
        if (group?.activeTab) {
          dependencies.closeTab(group.activeTab, group.id);
        }
      },
      when: () => {
        const state = dependencies.getTabsState();
        const group = state.groups.find((entry) => entry.id === state.activeGroupId);
        return !!(group && group.tabs.length > 0);
      },
    },
    {
      id: 'tab.closeAll',
      label: 'Close All Tabs',
      description: 'Close all open tabs',
      category: 'tab',
      icon: 'x-circle',
      action: () => {
        const state = dependencies.getTabsState();
        state.groups.forEach((group) => {
          group.tabs.forEach((tab) => {
            if (tab.closable !== false) {
              dependencies.closeTab(tab.id, group.id);
            }
          });
        });
      },
      when: () => {
        const state = dependencies.getTabsState();
        return state.groups.some((group) => group.tabs.length > 0);
      },
    },
    {
      id: 'view.splitRight',
      label: 'Split Editor Right',
      description: 'Split the editor to the right',
      category: 'view',
      keybinding: 'Ctrl+\\',
      icon: 'columns',
      action: () => {
        const state = dependencies.getTabsState();
        dependencies.splitGroup('horizontal', state.activeGroupId);
      },
    },
    {
      id: 'view.splitDown',
      label: 'Split Editor Down',
      description: 'Split the editor downwards',
      category: 'view',
      keybinding: 'Ctrl+Shift+-',
      icon: 'rows',
      action: () => {
        const state = dependencies.getTabsState();
        dependencies.splitGroup('vertical', state.activeGroupId);
      },
    },
    {
      id: 'view.toggleSidebar',
      label: 'Toggle Sidebar',
      description: 'Show or hide the sidebar',
      category: 'view',
      keybinding: 'Ctrl+B',
      icon: 'sidebar',
      action: () => {
        dependencies.toggleSidebar();
      },
    },
    {
      id: 'view.togglePanel',
      label: 'Toggle Panel',
      description: 'Show or hide the bottom panel',
      category: 'view',
      keybinding: 'Ctrl+J',
      icon: 'panel-bottom',
      action: () => {
        dependencies.togglePanel();
      },
    },
    {
      id: 'go.dashboard',
      label: 'Go to Dashboard',
      description: 'Open Dashboard in sidebar',
      category: 'go',
      icon: 'layout-dashboard',
      action: () => {
        dependencies.openSidebar();
        dependencies.setActiveActivityItem('dashboard');
      },
    },
    {
      id: 'go.projects',
      label: 'Go to Projects',
      description: 'Open Projects panel in sidebar',
      category: 'go',
      icon: 'folder',
      action: () => {
        dependencies.openSidebar();
        dependencies.setActiveActivityItem('projects');
      },
    },
    {
      id: 'go.conversations',
      label: 'Go to Conversations',
      description: 'Open Conversations panel in sidebar',
      category: 'go',
      icon: 'message-square',
      action: () => {
        dependencies.openSidebar();
        dependencies.setActiveActivityItem('conversations');
      },
    },
    {
      id: 'go.tasks',
      label: 'Go to Tasks',
      description: 'Open Tasks panel in sidebar',
      category: 'go',
      icon: 'check-square',
      action: () => {
        dependencies.openSidebar();
        dependencies.setActiveActivityItem('tasks');
      },
    },
    {
      id: 'go.settings',
      label: 'Go to Settings',
      description: 'Open Settings panel in sidebar',
      category: 'go',
      icon: 'settings',
      action: () => {
        dependencies.openSidebar();
        dependencies.setActiveActivityItem('settings');
      },
    },
  ];
}

function filterCommands(query: string, commands: Command[]): Command[] {
  const availableCommands = commands.filter((command) => !command.when || command.when());

  if (!query.trim()) {
    return availableCommands;
  }

  const normalizedQuery = query.toLowerCase().trim();

  return availableCommands
    .filter((command) => {
      const labelMatch = command.label.toLowerCase().includes(normalizedQuery);
      const descriptionMatch = command.description?.toLowerCase().includes(normalizedQuery) || false;
      const categoryMatch = command.category.toLowerCase().includes(normalizedQuery);

      return labelMatch || descriptionMatch || categoryMatch;
    })
    .sort((left, right) => {
      const leftExact = left.label.toLowerCase().startsWith(normalizedQuery);
      const rightExact = right.label.toLowerCase().startsWith(normalizedQuery);

      if (leftExact && !rightExact) {
        return -1;
      }

      if (!leftExact && rightExact) {
        return 1;
      }

      const leftCategoryPriority = defaultCategories.find((category) => category.id === left.category)?.priority || 999;
      const rightCategoryPriority = defaultCategories.find((category) => category.id === right.category)?.priority || 999;

      if (leftCategoryPriority !== rightCategoryPriority) {
        return leftCategoryPriority - rightCategoryPriority;
      }

      return left.label.localeCompare(right.label);
    });
}

function createWritableCommandPaletteStore(
  dependencies: CommandStoreDependencies
): CommandPaletteStoreLike {
  const { subscribe, update } = writable<CommandPaletteState>(cloneCommandPaletteState(initialState));

  let currentState = cloneCommandPaletteState(initialState);
  let registeredCommands = createDefaultCommands(dependencies);

  subscribe((state) => {
    currentState = cloneCommandPaletteState(state);
  });

  return {
    subscribe,
    show: () => {
      update((state) => ({
        ...state,
        visible: true,
        query: '',
        selectedIndex: 0,
        filteredCommands: filterCommands('', registeredCommands),
      }));
    },
    hide: () => {
      update((state) => ({
        ...state,
        visible: false,
        query: '',
        selectedIndex: 0,
        filteredCommands: [],
      }));
    },
    toggle: () => {
      update((state) => {
        if (state.visible) {
          return {
            ...state,
            visible: false,
            query: '',
            selectedIndex: 0,
            filteredCommands: [],
          };
        }

        return {
          ...state,
          visible: true,
          query: '',
          selectedIndex: 0,
          filteredCommands: filterCommands('', registeredCommands),
        };
      });
    },
    setQuery: (query) => {
      update((state) => ({
        ...state,
        query,
        selectedIndex: 0,
        filteredCommands: filterCommands(query, registeredCommands),
      }));
    },
    selectNext: () => {
      update((state) => ({
        ...state,
        selectedIndex: Math.min(state.selectedIndex + 1, state.filteredCommands.length - 1),
      }));
    },
    selectPrevious: () => {
      update((state) => ({
        ...state,
        selectedIndex: Math.max(state.selectedIndex - 1, 0),
      }));
    },
    selectIndex: (index) => {
      update((state) => ({
        ...state,
        selectedIndex: Math.max(0, Math.min(index, state.filteredCommands.length - 1)),
      }));
    },
    executeSelected: async () => {
      let result: CommandExecutionResult = { success: false };
      const selectedCommand = currentState.filteredCommands[currentState.selectedIndex];

      if (selectedCommand) {
        try {
          const actionResult = selectedCommand.action();
          if (actionResult instanceof Promise) {
            await actionResult;
          }
          result = { success: true, message: `Executed: ${selectedCommand.label}` };
        } catch (error) {
          result = {
            success: false,
            error: error instanceof Error ? error.message : 'Unknown error',
          };
        }
      }

      update((state) => ({
        ...state,
        visible: false,
        query: '',
        selectedIndex: 0,
        filteredCommands: [],
      }));

      return result;
    },
    executeCommand: async (commandId) => {
      const command = registeredCommands.find((entry) => entry.id === commandId);
      if (!command) {
        return { success: false, error: `Command not found: ${commandId}` };
      }

      if (command.when && !command.when()) {
        return { success: false, error: `Command not available: ${command.label}` };
      }

      try {
        const actionResult = command.action();
        if (actionResult instanceof Promise) {
          await actionResult;
        }
        return { success: true, message: `Executed: ${command.label}` };
      } catch (error) {
        return {
          success: false,
          error: error instanceof Error ? error.message : 'Unknown error',
        };
      }
    },
    registerCommand: (command) => {
      const existingIndex = registeredCommands.findIndex((entry) => entry.id === command.id);
      if (existingIndex >= 0) {
        registeredCommands[existingIndex] = command;
        return;
      }

      registeredCommands.push(command);
    },
    unregisterCommand: (commandId) => {
      registeredCommands = registeredCommands.filter((command) => command.id !== commandId);
    },
    getAllCommands: () => [...registeredCommands],
    getCommandsByCategory: (categoryId) => registeredCommands.filter((command) => command.category === categoryId),
    getState: () => cloneCommandPaletteState(currentState),
  };
}

function createRecordingCommandDependencies(
  mode: 'active' | 'shadow',
  effectLog: CommandEffectCall[]
): CommandStoreDependencies {
  return {
    getTabsState: defaultDependencies.getTabsState,
    closeTab: (tabId, groupId) => {
      effectLog.push({ target: 'tabs.closeTab', args: [tabId, groupId] });
      if (mode === 'active') {
        defaultDependencies.closeTab(tabId, groupId);
      }
    },
    splitGroup: (direction, groupId) => {
      effectLog.push({ target: 'tabs.splitGroup', args: [direction, groupId] });
      if (mode === 'active') {
        defaultDependencies.splitGroup(direction, groupId);
      }
    },
    toggleSidebar: () => {
      effectLog.push({ target: 'layout.toggleSidebar', args: [] });
      if (mode === 'active') {
        defaultDependencies.toggleSidebar();
      }
    },
    setActiveActivityItem: (activityId) => {
      effectLog.push({ target: 'layout.setActiveActivityItem', args: [activityId] });
      if (mode === 'active') {
        defaultDependencies.setActiveActivityItem(activityId);
      }
    },
    openSidebar: () => {
      effectLog.push({ target: 'layout.openSidebar', args: [] });
      if (mode === 'active') {
        defaultDependencies.openSidebar();
      }
    },
    togglePanel: () => {
      effectLog.push({ target: 'panel.toggle', args: [] });
      if (mode === 'active') {
        defaultDependencies.togglePanel();
      }
    },
  };
}

function createParityAwareCommandPaletteStore(
  primary: CommandPaletteStoreLike,
  shadow: CommandPaletteStoreLike,
  primaryEffectsLog: CommandEffectCall[],
  shadowEffectsLog: CommandEffectCall[],
  reportMismatch: (context: CommandMismatchContext) => void
): CommandPaletteStoreLike {
  const listeners = new Set<Subscriber<CommandPaletteState>>();
  let currentState = primary.getState();

  primary.subscribe((state) => {
    currentState = cloneCommandPaletteState(state);
    for (const listener of listeners) {
      listener(cloneCommandPaletteState(state));
    }
  });

  function subscribe(run: Subscriber<CommandPaletteState>): Unsubscriber {
    run(cloneCommandPaletteState(currentState));
    listeners.add(run);
    return () => {
      listeners.delete(run);
    };
  }

  function compare(
    action: string,
    primaryResult?: CommandExecutionResult,
    shadowResult?: CommandExecutionResult
  ): void {
    const primaryState = primary.getState();
    const shadowState = shadow.getState();
    const primaryRegistry = primary.getAllCommands();
    const shadowRegistry = shadow.getAllCommands();
    const primaryEffects = [...primaryEffectsLog];
    const shadowEffects = [...shadowEffectsLog];

    if (
      !areEqual(normalizeCommandPaletteState(primaryState), normalizeCommandPaletteState(shadowState))
      || !areEqual(normalizeCommandRegistry(primaryRegistry), normalizeCommandRegistry(shadowRegistry))
      || !areEqual(primaryEffects, shadowEffects)
      || (primaryResult && shadowResult && !areEqual(primaryResult, shadowResult))
    ) {
      reportMismatch({
        action,
        primaryState,
        shadowState,
        primaryRegistry,
        shadowRegistry,
        primaryEffects,
        shadowEffects,
        primaryResult,
        shadowResult,
      });
    }
  }

  function resetEffectLogs(): void {
    primaryEffectsLog.length = 0;
    shadowEffectsLog.length = 0;
  }

  return {
    subscribe,
    show: () => {
      resetEffectLogs();
      primary.show();
      shadow.show();
      compare('show');
    },
    hide: () => {
      resetEffectLogs();
      primary.hide();
      shadow.hide();
      compare('hide');
    },
    toggle: () => {
      resetEffectLogs();
      primary.toggle();
      shadow.toggle();
      compare('toggle');
    },
    setQuery: (query) => {
      resetEffectLogs();
      primary.setQuery(query);
      shadow.setQuery(query);
      compare('setQuery');
    },
    selectNext: () => {
      resetEffectLogs();
      primary.selectNext();
      shadow.selectNext();
      compare('selectNext');
    },
    selectPrevious: () => {
      resetEffectLogs();
      primary.selectPrevious();
      shadow.selectPrevious();
      compare('selectPrevious');
    },
    selectIndex: (index) => {
      resetEffectLogs();
      primary.selectIndex(index);
      shadow.selectIndex(index);
      compare('selectIndex');
    },
    executeSelected: async () => {
      resetEffectLogs();
      const selectedCommand = primary.getState().filteredCommands[primary.getState().selectedIndex];
      const shouldShadowExecute = selectedCommand ? builtInCommandIds.has(selectedCommand.id) : false;
      const primaryResult = await primary.executeSelected();

      let shadowResult: CommandExecutionResult | undefined;
      if (shouldShadowExecute) {
        shadowResult = await shadow.executeSelected();
      } else {
        shadow.hide();
      }

      compare('executeSelected', primaryResult, shadowResult);
      return primaryResult;
    },
    executeCommand: async (commandId) => {
      resetEffectLogs();
      const shouldShadowExecute = builtInCommandIds.has(commandId);
      const primaryResult = await primary.executeCommand(commandId);
      const shadowResult = shouldShadowExecute ? await shadow.executeCommand(commandId) : undefined;
      compare('executeCommand', primaryResult, shadowResult);
      return primaryResult;
    },
    registerCommand: (command) => {
      resetEffectLogs();
      primary.registerCommand(command);
      shadow.registerCommand(command);
      compare('registerCommand');
    },
    unregisterCommand: (commandId) => {
      resetEffectLogs();
      primary.unregisterCommand(commandId);
      shadow.unregisterCommand(commandId);
      compare('unregisterCommand');
    },
    getAllCommands: () => primary.getAllCommands(),
    getCommandsByCategory: (categoryId) => primary.getCommandsByCategory(categoryId),
    getState: () => cloneCommandPaletteState(currentState),
  };
}

function createCommandPaletteStore(): CommandPaletteStoreLike {
  const enableRunesCommandStore = config.isRunesCommandStoreEnabled();
  const enableShadowParity = config.isRunesStoreShadowParityEnabled();

  if (!enableRunesCommandStore && !enableShadowParity) {
    return createWritableCommandPaletteStore(defaultDependencies);
  }

  const primaryEffectsLog: CommandEffectCall[] = [];
  const shadowEffectsLog: CommandEffectCall[] = [];

  const writableStore = createWritableCommandPaletteStore(
    createRecordingCommandDependencies(enableRunesCommandStore ? 'shadow' : 'active', enableRunesCommandStore ? shadowEffectsLog : primaryEffectsLog)
  );
  const runesStore = createRunesCommandPaletteStore(
    createRecordingCommandDependencies(enableRunesCommandStore ? 'active' : 'shadow', enableRunesCommandStore ? primaryEffectsLog : shadowEffectsLog)
  );

  const primaryStore = enableRunesCommandStore ? runesStore : writableStore;
  const shadowStore = enableRunesCommandStore ? writableStore : runesStore;

  if (!enableShadowParity) {
    return primaryStore;
  }

  return createParityAwareCommandPaletteStore(primaryStore, shadowStore, primaryEffectsLog, shadowEffectsLog, ({
    action,
    primaryState,
    shadowState,
    primaryRegistry,
    shadowRegistry,
    primaryEffects,
    shadowEffects,
    primaryResult,
    shadowResult,
  }) => {
    console.error('[command.store parity mismatch]', {
      action,
      primaryState: normalizeCommandPaletteState(primaryState),
      shadowState: normalizeCommandPaletteState(shadowState),
      primaryRegistry: normalizeCommandRegistry(primaryRegistry),
      shadowRegistry: normalizeCommandRegistry(shadowRegistry),
      primaryEffects,
      shadowEffects,
      primaryResult,
      shadowResult,
    });
  });
}

const commandPaletteStore = createCommandPaletteStore();

export const __internal = {
  cloneCommandPaletteState,
  normalizeCommandPaletteState,
  normalizeCommandRegistry,
  createDefaultCommands,
  createWritableCommandPaletteStore,
  createRecordingCommandDependencies,
  createParityAwareCommandPaletteStore,
  createRunesCommandPaletteStore,
};

export const commandPaletteActions = {
  show: commandPaletteStore.show,
  hide: commandPaletteStore.hide,
  toggle: commandPaletteStore.toggle,
  setQuery: commandPaletteStore.setQuery,
  selectNext: commandPaletteStore.selectNext,
  selectPrevious: commandPaletteStore.selectPrevious,
  selectIndex: commandPaletteStore.selectIndex,
  executeSelected: commandPaletteStore.executeSelected,
  executeCommand: commandPaletteStore.executeCommand,
  registerCommand: commandPaletteStore.registerCommand,
  unregisterCommand: commandPaletteStore.unregisterCommand,
  getAllCommands: commandPaletteStore.getAllCommands,
  getCommandsByCategory: commandPaletteStore.getCommandsByCategory,
  getState: commandPaletteStore.getState,
};

export const commandPalette = {
  subscribe: commandPaletteStore.subscribe,
  ...commandPaletteActions,
};

export default commandPalette;
