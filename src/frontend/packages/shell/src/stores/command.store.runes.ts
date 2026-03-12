import type { Subscriber, Unsubscriber } from 'svelte/store';
import type { Command, CommandPaletteState, CommandExecutionResult } from '../types/command.types';
import type { CommandPaletteStoreLike, CommandStoreDependencies } from './command.store';

function cloneCommand(command: Command): Command {
  return { ...command };
}

function cloneCommandPaletteState(state: CommandPaletteState): CommandPaletteState {
  return {
    ...state,
    filteredCommands: state.filteredCommands.map(cloneCommand),
    categories: state.categories.map((category) => ({ ...category })),
  };
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
  const priorities = new Map([
    ['tab', 1],
    ['view', 2],
    ['go', 3],
  ]);

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

      const leftPriority = priorities.get(left.category) ?? 999;
      const rightPriority = priorities.get(right.category) ?? 999;
      if (leftPriority !== rightPriority) {
        return leftPriority - rightPriority;
      }

      return left.label.localeCompare(right.label);
    });
}

export function createRunesCommandPaletteStore(
  dependencies: CommandStoreDependencies
): CommandPaletteStoreLike {
  let state = cloneCommandPaletteState({
    visible: false,
    query: '',
    selectedIndex: 0,
    filteredCommands: [],
    categories: [
      { id: 'tab', label: 'Tab', icon: 'file', priority: 1 },
      { id: 'view', label: 'View', icon: 'eye', priority: 2 },
      { id: 'go', label: 'Go', icon: 'arrow-right', priority: 3 },
    ],
  });
  let registeredCommands = createDefaultCommands(dependencies);
  const subscribers = new Set<Subscriber<CommandPaletteState>>();

  function snapshot(): CommandPaletteState {
    return cloneCommandPaletteState(state);
  }

  function publish(): void {
    const latest = snapshot();
    for (const subscriber of subscribers) {
      subscriber(latest);
    }
  }

  function commit(nextState: CommandPaletteState): void {
    state = cloneCommandPaletteState(nextState);
    publish();
  }

  function hidePalette(): void {
    commit({
      ...state,
      visible: false,
      query: '',
      selectedIndex: 0,
      filteredCommands: [],
    });
  }

  return {
    subscribe: (run: Subscriber<CommandPaletteState>): Unsubscriber => {
      run(snapshot());
      subscribers.add(run);
      return () => {
        subscribers.delete(run);
      };
    },
    show: () => {
      commit({
        ...state,
        visible: true,
        query: '',
        selectedIndex: 0,
        filteredCommands: filterCommands('', registeredCommands),
      });
    },
    hide: () => {
      hidePalette();
    },
    toggle: () => {
      if (state.visible) {
        hidePalette();
        return;
      }

      commit({
        ...state,
        visible: true,
        query: '',
        selectedIndex: 0,
        filteredCommands: filterCommands('', registeredCommands),
      });
    },
    setQuery: (query) => {
      commit({
        ...state,
        query,
        selectedIndex: 0,
        filteredCommands: filterCommands(query, registeredCommands),
      });
    },
    selectNext: () => {
      commit({
        ...state,
        selectedIndex: Math.min(state.selectedIndex + 1, state.filteredCommands.length - 1),
      });
    },
    selectPrevious: () => {
      commit({
        ...state,
        selectedIndex: Math.max(state.selectedIndex - 1, 0),
      });
    },
    selectIndex: (index) => {
      commit({
        ...state,
        selectedIndex: Math.max(0, Math.min(index, state.filteredCommands.length - 1)),
      });
    },
    executeSelected: async (): Promise<CommandExecutionResult> => {
      let result: CommandExecutionResult = { success: false };
      const selectedCommand = state.filteredCommands[state.selectedIndex];

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

      hidePalette();
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
    getState: () => snapshot(),
  };
}
