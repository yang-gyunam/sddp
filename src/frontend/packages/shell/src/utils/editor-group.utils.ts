/**
 * Editor Group Utility Functions
 * Logic extracted from EditorGroup component for testability
 */

import type { Tab, EditorGroupData } from '../types/layout.types';
import type { ContextMenuItem } from '../types';

export interface ContextMenuContext {
  tabId: string;
  groupId: string;
  tabs: Tab[];
}

/**
 * Gets the active tab from a group
 * Returns null if activeTab is empty string or not found in tabs
 */
export function getActiveTab(group: EditorGroupData): Tab | null {
  if (!group.activeTab || group.activeTab === '') return null;
  return group.tabs.find((tab) => tab.id === group.activeTab) ?? null;
}

/**
 * Builds context menu items for a tab
 */
export function buildContextMenuItems(
  context: ContextMenuContext,
  actions: {
    onClose: (tabId: string, groupId: string) => void;
    onCloseOthers: (tabId: string, groupId: string) => void;
    onCloseAll: (tabs: Tab[], groupId: string) => void;
    onSplitRight: (tabId: string, groupId: string) => void;
    onSplitDown: (groupId: string) => void;
  }
): ContextMenuItem[] {
  const { tabId, groupId, tabs } = context;
  const tab = tabs.find((t) => t.id === tabId);

  if (!tab) return [];

  return [
    {
      id: 'close',
      label: 'Close',
      icon: 'x',
      shortcut: 'Ctrl+W',
      action: () => actions.onClose(tabId, groupId),
      disabled: tab.closable === false,
    },
    {
      id: 'close-others',
      label: 'Close Others',
      icon: 'x-circle',
      action: () => actions.onCloseOthers(tabId, groupId),
      disabled: tabs.length <= 1,
    },
    {
      id: 'close-all',
      label: 'Close All',
      icon: 'x-square',
      action: () => actions.onCloseAll(tabs, groupId),
    },
    { id: 'sep1', label: '', separator: true },
    {
      id: 'split-right',
      label: 'Split Right',
      icon: 'columns',
      shortcut: 'Ctrl+\\',
      action: () => actions.onSplitRight(tabId, groupId),
    },
    {
      id: 'split-down',
      label: 'Split Down',
      icon: 'rows',
      shortcut: 'Ctrl+Shift+\\',
      action: () => actions.onSplitDown(groupId),
    },
  ];
}

/**
 * Calculates drop position based on mouse position
 */
export function calculateDropPosition(
  clientX: number,
  rectLeft: number,
  rectWidth: number
): 'left' | 'right' {
  const midpoint = rectLeft + rectWidth / 2;
  return clientX < midpoint ? 'left' : 'right';
}

/**
 * Gets drop indicator CSS class
 */
export function getDropIndicatorClass(
  tabId: string,
  dragOverTabId: string | null,
  dropPosition: 'left' | 'right' | null
): string {
  if (dragOverTabId !== tabId || !dropPosition) return '';

  return dropPosition === 'left'
    ? 'before:absolute before:left-0 before:top-0 before:bottom-0 before:w-0.5 before:bg-[var(--color-accent-primary)]'
    : 'after:absolute after:right-0 after:top-0 after:bottom-0 after:w-0.5 after:bg-[var(--color-accent-primary)]';
}

/**
 * Calculates the target index for tab reordering
 */
export function calculateReorderIndex(
  tabs: Tab[],
  targetTabId: string,
  fromTabId: string,
  dropPosition: 'left' | 'right'
): { toIndex: number; fromIndex: number } | null {
  const toIndex = tabs.findIndex((t) => t.id === targetTabId);
  const fromIndex = tabs.findIndex((t) => t.id === fromTabId);

  if (toIndex === -1 || fromIndex === -1) {
    return null;
  }

  let adjustedToIndex = dropPosition === 'right' ? toIndex + 1 : toIndex;

  // Adjust for same-group reorder
  if (fromIndex < adjustedToIndex) {
    adjustedToIndex = adjustedToIndex - 1;
  }

  adjustedToIndex = Math.max(0, Math.min(adjustedToIndex, tabs.length - 1));

  return { toIndex: adjustedToIndex, fromIndex };
}

/**
 * Calculates target index for cross-group drop
 */
export function calculateCrossGroupDropIndex(
  tabs: Tab[],
  targetTabId: string,
  dropPosition: 'left' | 'right'
): number {
  const toIndex = tabs.findIndex((t) => t.id === targetTabId);

  if (toIndex === -1) {
    return tabs.length; // Append to end
  }

  return dropPosition === 'right' ? toIndex + 1 : toIndex;
}
