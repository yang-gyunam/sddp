/**
 * RouterService - URL routing and tab synchronization
 * Syncs browser URL with active tab state
 */

import { get } from 'svelte/store';
import { tabActions, tabsStore, globalActiveTab, activeGroupId } from '../stores/tabs.store';
import type { Tab as _Tab } from '../types/layout.types';

/**
 * Router Service - Manages URL routing and tab synchronization
 */
export class RouterService {
  private static initialized = false;
  private static unsubscribeTab: (() => void) | null = null;
  private static isNavigating = false;
  private static onNavigateToPath: ((path: string) => void) | null = null;

  /**
   * Set a handler for path-based navigation (called when a tab needs to be created from a URL path)
   */
  static setNavigateHandler(handler: (path: string) => void): void {
    this.onNavigateToPath = handler;
  }

  /**
   * Initialize router - sets up URL sync with tabs
   */
  static initialize(): () => void {
    if (this.initialized) {
      console.warn('RouterService already initialized');
      return () => {};
    }

    this.initialized = true;

    // Listen to browser back/forward navigation
    window.addEventListener('popstate', this.handlePopState);

    // Listen to tab changes and update URL
    this.unsubscribeTab = globalActiveTab.subscribe((tab) => {
      if (!tab || this.isNavigating) return;
      this.updateUrl(tab.path || '/');
    });

    // Handle initial URL on load
    this.handleInitialUrl();

    // Return cleanup function
    return () => this.cleanup();
  }

  /**
   * Handle initial URL when app loads
   */
  private static handleInitialUrl(): void {
    const path = window.location.pathname;
    if (path && path !== '/') {
      this.onNavigateToPath?.(path);
    }
  }

  /**
   * Handle browser back/forward navigation
   */
  private static handlePopState = (_event: PopStateEvent): void => {
    const path = window.location.pathname;

    // Prevent recursive navigation
    this.isNavigating = true;

    // Find existing tab with this path
    const state = get(tabsStore);
    const group = state.groups.find((g) => g.id === get(activeGroupId));

    if (group) {
      const existingTab = group.tabs.find((t) => t.path === path);
      if (existingTab) {
        // Switch to existing tab
        tabActions.switchToTab(existingTab.id, group.id);
      } else {
        this.onNavigateToPath?.(path);
      }
    }

    // Reset navigation flag after a short delay
    setTimeout(() => {
      this.isNavigating = false;
    }, 100);
  };

  /**
   * Update browser URL without triggering navigation
   */
  private static updateUrl(path: string): void {
    if (window.location.pathname === path) return;

    // Prevent recursive navigation
    this.isNavigating = true;

    // Update URL without reload
    window.history.pushState({ path }, '', path);

    // Reset navigation flag after a short delay
    setTimeout(() => {
      this.isNavigating = false;
    }, 100);
  }

  /**
   * Navigate to a path (programmatic navigation)
   */
  static navigate(path: string): void {
    this.isNavigating = true;

    // Update URL
    window.history.pushState({ path }, '', path);

    // Find or create tab with this path
    const state = get(tabsStore);
    const groupId = get(activeGroupId);
    const group = state.groups.find((g) => g.id === groupId);

    if (group) {
      const existingTab = group.tabs.find((t) => t.path === path);
      if (existingTab) {
        tabActions.switchToTab(existingTab.id, groupId);
      } else {
        this.onNavigateToPath?.(path);
      }
    }

    setTimeout(() => {
      this.isNavigating = false;
    }, 100);
  }

  /**
   * Replace current URL without adding to history
   */
  static replace(path: string): void {
    this.isNavigating = true;
    window.history.replaceState({ path }, '', path);
    setTimeout(() => {
      this.isNavigating = false;
    }, 100);
  }

  /**
   * Go back in history
   */
  static back(): void {
    window.history.back();
  }

  /**
   * Go forward in history
   */
  static forward(): void {
    window.history.forward();
  }

  /**
   * Get current path
   */
  static getCurrentPath(): string {
    return window.location.pathname;
  }

  /**
   * Cleanup - remove event listeners
   */
  private static cleanup(): void {
    window.removeEventListener('popstate', this.handlePopState);
    this.unsubscribeTab?.();
    this.unsubscribeTab = null;
    this.initialized = false;
  }
}

/**
 * Path matcher utility
 */
export class PathMatcher {
  /**
   * Match a path pattern with parameters
   * Example: matchPath('/project/:id/specs', '/project/123/specs')
   * Returns: { id: '123' }
   */
  static matchPath(
    pattern: string,
    path: string
  ): Record<string, string> | null {
    const patternParts = pattern.split('/').filter(Boolean);
    const pathParts = path.split('/').filter(Boolean);

    if (patternParts.length !== pathParts.length) {
      return null;
    }

    const params: Record<string, string> = {};

    for (let i = 0; i < patternParts.length; i++) {
      const patternPart = patternParts[i];
      const pathPart = pathParts[i];

      if (!patternPart || !pathPart) continue;

      if (patternPart.startsWith(':')) {
        // Parameter
        const paramName = patternPart.slice(1);
        params[paramName] = pathPart;
      } else if (patternPart !== pathPart) {
        // Literal mismatch
        return null;
      }
    }

    return params;
  }

  /**
   * Build a path from pattern and parameters
   * Example: buildPath('/project/:id/specs', { id: '123' })
   * Returns: '/project/123/specs'
   */
  static buildPath(
    pattern: string,
    params: Record<string, string>
  ): string {
    let path = pattern;
    for (const [key, value] of Object.entries(params)) {
      path = path.replace(`:${key}`, value);
    }
    return path;
  }
}
