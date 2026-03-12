/**
 * Menu and Navigation type definitions
 */

import type { Tab } from './layout.types';

// Tab configuration for menu items
export interface TabConfig {
  // Tab creation function - returns tab data without id
  createTab: () => Omit<Tab, 'id'>;
  // Optional: reuse existing tab with same path
  reuseByPath?: boolean;
  // Optional: custom deduplication key
  dedupKey?: string;
}

// Menu node for sidebar tree
export interface MenuNode {
  id: string;
  name: string;
  url?: string;
  icon?: string;
  order: number;
  type: 'PAGE' | 'FOLDER' | 'EXTERNAL';
  parentId?: string;
  children: MenuNode[];
  permissions: string[];
  isActive?: boolean;
  expanded?: boolean;
  // Tab configuration for navigation
  tabConfig?: TabConfig;
}

// Menu state
export interface MenuState {
  items: MenuNode[];
  expandedIds: Set<string>;
  selectedId: string | null;
  loading: boolean;
  error: string | null;
}

// Menu item click handler
export type MenuItemClickHandler = (item: MenuNode) => void;

// Menu context
export interface MenuContext {
  items: MenuNode[];
  onItemClick: MenuItemClickHandler;
  onItemExpand: (itemId: string) => void;
  onItemCollapse: (itemId: string) => void;
}

// Context menu item for right-click menus
export interface ContextMenuItem {
  id: string;
  label: string;
  icon?: string;
  shortcut?: string;
  separator?: boolean;
  disabled?: boolean;
  action?: () => void;
}
