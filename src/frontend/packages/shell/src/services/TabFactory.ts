/**
 * TabFactory Service
 * Creates tabs from menu items and other navigation sources
 */

import type { Tab } from '../types/layout.types';
import type { MenuNode } from '../types/menu.types';
import { tabActions } from '../stores/tabs.store';
import { RouterService } from './RouterService';

/**
 * Tab Factory - Creates and manages tabs from various sources
 */
export class TabFactory {
  /**
   * Create a tab from a menu node
   * Handles deduplication and reuse logic
   */
  static createFromMenu(menuNode: MenuNode, groupId?: string): Tab | null {
    // Only create tabs for PAGE type menu items
    if (menuNode.type !== 'PAGE') {
      return null;
    }

    // Check if menu has tab configuration
    if (!menuNode.tabConfig) {
      console.warn(`Menu item ${menuNode.id} has no tabConfig`);
      return null;
    }

    const { createTab, reuseByPath, dedupKey: _dedupKey } = menuNode.tabConfig;
    const tabData = createTab();

    // If reuseByPath is enabled and tab has a path, try to reuse existing tab
    if (reuseByPath && tabData.path) {
      const existingTab = tabActions.openByPath(
        tabData.path,
        tabData,
        groupId
      );
      if (existingTab) {
        // Update URL to match the tab
        if (existingTab.path) {
          RouterService.replace(existingTab.path);
        }
        return existingTab;
      }
    }

    // Create new tab
    const newTab = tabActions.createTab(tabData, groupId);
    
    // Update URL to match the new tab
    if (newTab.path) {
      RouterService.replace(newTab.path);
    }
    
    return newTab;
  }

  /**
   * Create a tab from a path
   * Used for direct navigation (e.g., from URL or command palette)
   */
  static createFromPath(
    path: string,
    title: string,
    component: unknown,
    props?: Record<string, unknown>,
    groupId?: string
  ): Tab | null {
    const tab = tabActions.openByPath(
      path,
      {
        title,
        icon: 'file',
        dirty: false,
        closable: true,
        component,
        props: props || {},
        type: 'PAGE',
      },
      groupId
    );
    
    // Update URL to match the tab
    if (tab?.path) {
      RouterService.replace(tab.path);
    }
    
    return tab;
  }

  /**
   * Create a tab from a URL (external link)
   */
  static createFromUrl(
    url: string,
    title: string,
    groupId?: string
  ): Tab | null {
    return tabActions.createTab(
      {
        title,
        icon: 'link-external',
        dirty: false,
        closable: true,
        component: null, // External URLs don't have components
        props: {},
        url,
        type: 'EXTERNAL',
      },
      groupId
    );
  }

  /**
   * Create a tab with custom configuration
   */
  static createCustom(
    tabData: Omit<Tab, 'id'>,
    groupId?: string
  ): Tab {
    const tab = tabActions.createTab(tabData, groupId);
    
    // Update URL to match the tab
    if (tab.path) {
      RouterService.replace(tab.path);
    }
    
    return tab;
  }

  /**
   * Handle menu item click - main entry point for navigation
   */
  static handleMenuClick(menuNode: MenuNode, groupId?: string): void {
    switch (menuNode.type) {
      case 'PAGE':
        this.createFromMenu(menuNode, groupId);
        break;

      case 'EXTERNAL':
        if (menuNode.url) {
          // Open external URL in new browser tab
          window.open(menuNode.url, '_blank', 'noopener,noreferrer');
        }
        break;

      case 'FOLDER':
        // Folders don't create tabs, they just expand/collapse
        // This is handled by the menu component itself
        break;

      default:
        console.warn(`Unknown menu type: ${menuNode.type}`);
    }
  }
}

/**
 * Navigation Service - High-level navigation API
 */
export class NavigationService {
  /**
   * Navigate to a menu item
   */
  static navigateToMenu(menuNode: MenuNode, groupId?: string): void {
    TabFactory.handleMenuClick(menuNode, groupId);
  }

  /**
   * Navigate to a path
   */
  static navigateToPath(
    path: string,
    title: string,
    component: unknown,
    props?: Record<string, unknown>,
    groupId?: string
  ): void {
    TabFactory.createFromPath(path, title, component, props, groupId);
  }

  /**
   * Navigate to a URL (external)
   */
  static navigateToUrl(url: string, title: string, groupId?: string): void {
    TabFactory.createFromUrl(url, title, groupId);
  }

  /**
   * Open a new tab with custom configuration
   */
  static openTab(tabData: Omit<Tab, 'id'>, groupId?: string): Tab {
    return TabFactory.createCustom(tabData, groupId);
  }
}
