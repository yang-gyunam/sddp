/**
 * NavigationService - Tab-based Navigation API
 *
 * Provides a semantic navigation API that wraps the tab system.
 * Supports domain-specific navigation (Spec, Conversation, Requirement, Glossary).
 */

import { tabActions, globalActiveTab, allTabs } from '../../stores/tabs.store';
import { get } from 'svelte/store';
import type { Tab } from '../../types';

// ============================================
// Types
// ============================================

/** Svelte component type (using unknown for Svelte 5 compatibility) */
type SvelteComponent = unknown;

export interface NavigationTarget {
  /** Unique path identifier (e.g., '/spec/123', '/conversation/456') */
  path: string;
  /** Display title for the tab */
  title: string;
  /** Icon name for the tab */
  icon?: string;
  /** Svelte component to render */
  component?: SvelteComponent | null;
  /** Props to pass to the component */
  props?: Record<string, unknown>;
  /** Whether the tab can be closed */
  closable?: boolean;
  /** Whether the tab has unsaved changes */
  dirty?: boolean;
}

export interface NavigationOptions {
  /** Replace current tab instead of creating new one */
  replace?: boolean;
  /** Focus the tab after navigation */
  focus?: boolean;
  /** Group ID to open the tab in */
  groupId?: string;
}

export interface NavigationState {
  /** Current active path */
  currentPath: string | null;
  /** Navigation history */
  history: string[];
  /** Current position in history */
  historyIndex: number;
}

type NavigationCallback = (path: string, tab: Tab | null) => void;

// ============================================
// Navigation Service
// ============================================

class NavigationServiceImpl {
  private history: string[] = [];
  private historyIndex = -1;
  private listeners: Set<NavigationCallback> = new Set();

  /**
   * Navigate to a target
   */
  navigateTo(target: NavigationTarget, options: NavigationOptions = {}): Tab | null {
    const { replace: _replace = false, focus = true, groupId } = options;

    // Check if tab with same path already exists
    const existingTabs = get(allTabs);
    const existingTab = existingTabs.find((t) => t.path === target.path);

    if (existingTab) {
      // Tab exists, just switch to it
      if (focus) {
        tabActions.switchToTab(existingTab.id, groupId);
      }
      this.pushToHistory(target.path);
      this.notifyListeners(target.path, existingTab);
      return existingTab;
    }

    // Create new tab
    const newTab = tabActions.createTab(
      {
        title: target.title,
        icon: target.icon,
        path: target.path,
        component: target.component ?? null,
        props: target.props ?? {},
        closable: target.closable ?? true,
        dirty: target.dirty ?? false,
      },
      groupId
    );

    this.pushToHistory(target.path);
    this.notifyListeners(target.path, newTab);
    return newTab;
  }

  /**
   * Navigate to a Spec
   */
  navigateToSpec(specId: string, title?: string, component?: SvelteComponent, props?: Record<string, unknown>): Tab | null {
    return this.navigateTo({
      path: `/spec/${specId}`,
      title: title ?? `Spec ${specId.slice(0, 8)}`,
      icon: 'file-signature',
      component,
      props: { specId, ...props },
    });
  }

  /**
   * Navigate to a Conversation
   */
  navigateToConversation(conversationId: string, title?: string, component?: SvelteComponent, props?: Record<string, unknown>): Tab | null {
    return this.navigateTo({
      path: `/conversation/${conversationId}`,
      title: title ?? `Conversation ${conversationId.slice(0, 8)}`,
      icon: 'message-square',
      component,
      props: { conversationId, ...props },
    });
  }

  /**
   * Navigate to a Channel
   */
  navigateToChannel(channelId: string, channelName: string, channelType: 'Channel' | 'Forum', component?: SvelteComponent, props?: Record<string, unknown>): Tab | null {
    const isChat = channelType === 'Channel';
    return this.navigateTo({
      path: isChat ? `/channel/${channelId}` : `/forum/${channelId}`,
      title: channelName,
      icon: isChat ? 'hash' : 'list',
      component,
      props: { channelId, channelType, ...props },
    });
  }

  /**
   * Navigate to a Requirement
   */
  navigateToRequirement(requirementId: string, title?: string, component?: SvelteComponent, props?: Record<string, unknown>): Tab | null {
    return this.navigateTo({
      path: `/requirement/${requirementId}`,
      title: title ?? `Requirement ${requirementId.slice(0, 8)}`,
      icon: 'clipboard-list',
      component,
      props: { requirementId, ...props },
    });
  }

  /**
   * Navigate to a Glossary Term
   */
  navigateToGlossary(termId: string, term?: string, component?: SvelteComponent, props?: Record<string, unknown>): Tab | null {
    return this.navigateTo({
      path: `/glossary/${termId}`,
      title: term ?? `Term ${termId.slice(0, 8)}`,
      icon: 'book-open',
      component,
      props: { termId, ...props },
    });
  }

  /**
   * Navigate to an Artifact
   */
  navigateToArtifact(artifactId: string, title?: string, component?: SvelteComponent, props?: Record<string, unknown>): Tab | null {
    return this.navigateTo({
      path: `/artifact/${artifactId}`,
      title: title ?? `Artifact ${artifactId.slice(0, 8)}`,
      icon: 'package',
      component,
      props: { artifactId, ...props },
    });
  }

  /**
   * Navigate to Direct Message
   */
  navigateToDM(userId: string, userName: string, component?: SvelteComponent, props?: Record<string, unknown>): Tab | null {
    return this.navigateTo({
      path: `/dm/${userId}`,
      title: `@${userName}`,
      icon: 'message-circle',
      component,
      props: { userId, userName, ...props },
    });
  }

  /**
   * Navigate back in history
   */
  navigateBack(): boolean {
    if (this.historyIndex <= 0) return false;

    this.historyIndex--;
    const path = this.history[this.historyIndex];
    if (!path) return false;

    const tabs = get(allTabs);
    const tab = tabs.find((t) => t.path === path);
    if (tab) {
      tabActions.switchToTab(tab.id);
      this.notifyListeners(path, tab);
      return true;
    }

    return false;
  }

  /**
   * Navigate forward in history
   */
  navigateForward(): boolean {
    if (this.historyIndex >= this.history.length - 1) return false;

    this.historyIndex++;
    const path = this.history[this.historyIndex];
    if (!path) return false;

    const tabs = get(allTabs);
    const tab = tabs.find((t) => t.path === path);
    if (tab) {
      tabActions.switchToTab(tab.id);
      this.notifyListeners(path, tab);
      return true;
    }

    return false;
  }

  /**
   * Close current tab and navigate to previous
   */
  closeAndNavigateBack(): void {
    const currentTab = get(globalActiveTab);
    if (currentTab) {
      tabActions.closeTab(currentTab.id);
    }
    this.navigateBack();
  }

  /**
   * Get current navigation state
   */
  getState(): NavigationState {
    const currentTab = get(globalActiveTab);
    return {
      currentPath: currentTab?.path ?? null,
      history: [...this.history],
      historyIndex: this.historyIndex,
    };
  }

  /**
   * Check if can navigate back
   */
  canGoBack(): boolean {
    return this.historyIndex > 0;
  }

  /**
   * Check if can navigate forward
   */
  canGoForward(): boolean {
    return this.historyIndex < this.history.length - 1;
  }

  /**
   * Subscribe to navigation changes
   */
  subscribe(callback: NavigationCallback): () => void {
    this.listeners.add(callback);
    return () => {
      this.listeners.delete(callback);
    };
  }

  /**
   * Normalize route aliases to canonical conversation paths.
   */
  normalizePath(path: string): string {
    const trimmed = path.trim();
    if (!trimmed.startsWith('/')) {
      return trimmed;
    }

    const channels = trimmed.match(/^\/channels\/(.+)$/);
    if (channels?.[1]) {
      return `/conversation/${channels[1]}`;
    }

    const spaces = trimmed.match(/^\/spaces?\/(.+)$/);
    if (spaces?.[1]) {
      return `/conversation/${spaces[1]}`;
    }

    const forums = trimmed.match(/^\/forums\/(.+)$/);
    if (forums?.[1]) {
      return `/forum/${forums[1]}`;
    }

    const topics = trimmed.match(/^\/topics\/(.+)$/);
    if (topics?.[1]) {
      return `/topic/${topics[1]}`;
    }

    const threads = trimmed.match(/^\/threads\/(.+)$/);
    if (threads?.[1]) {
      return `/topic/${threads[1]}`;
    }

    const thread = trimmed.match(/^\/thread\/(.+)$/);
    if (thread?.[1]) {
      return `/topic/${thread[1]}`;
    }

    const directMessages = trimmed.match(/^\/direct-messages\/(.+)$/);
    if (directMessages?.[1]) {
      return `/dm/${directMessages[1]}`;
    }

    return trimmed;
  }

  /**
   * Parse path to extract type and ID
   */
  parsePath(path: string): { type: string; id: string } | null {
    const normalized = this.normalizePath(path);
    const match = normalized.match(/^\/([a-zA-Z]+)\/(.+)$/);
    if (!match || !match[1] || !match[2]) return null;
    return { type: match[1], id: match[2] };
  }

  /**
   * Navigate to a path by parsing it and calling the appropriate navigate method.
   * Returns true if navigation was handled, false if the path couldn't be resolved.
   */
  navigateToPath(path: string): boolean {
    const normalizedPath = this.normalizePath(path);

    // Dashboard paths are menu-based, not handled here
    if (normalizedPath.startsWith('/dashboard/')) {
      return false;
    }

    const parsed = this.parsePath(normalizedPath);
    if (!parsed) return false;

    switch (parsed.type) {
      case 'spec':
        this.navigateToSpec(parsed.id);
        return true;
      case 'conversation':
        this.navigateToConversation(parsed.id);
        return true;
      case 'space':
      case 'channel':
        this.navigateToChannel(parsed.id, parsed.id, 'Channel');
        return true;
      case 'forum':
        this.navigateToChannel(parsed.id, parsed.id, 'Forum');
        return true;
      case 'topic':
        this.navigateToConversation(parsed.id);
        return true;
      case 'requirement':
        this.navigateToRequirement(parsed.id);
        return true;
      case 'glossary':
        this.navigateToGlossary(parsed.id);
        return true;
      case 'artifact':
        this.navigateToArtifact(parsed.id);
        return true;
      case 'dm':
        this.navigateToDM(parsed.id, parsed.id);
        return true;
      default:
        return false;
    }
  }

  /**
   * Clear navigation history
   */
  clearHistory(): void {
    this.history = [];
    this.historyIndex = -1;
  }

  // ============================================
  // Private Methods
  // ============================================

  private pushToHistory(path: string): void {
    // Remove forward history when navigating to new path
    if (this.historyIndex < this.history.length - 1) {
      this.history = this.history.slice(0, this.historyIndex + 1);
    }

    // Don't add duplicate consecutive paths
    if (this.history[this.historyIndex] !== path) {
      this.history.push(path);
      this.historyIndex = this.history.length - 1;
    }

    // Limit history size
    const maxHistorySize = 50;
    if (this.history.length > maxHistorySize) {
      const removed = this.history.length - maxHistorySize;
      this.history = this.history.slice(removed);
      this.historyIndex = Math.max(0, this.historyIndex - removed);
    }
  }

  private notifyListeners(path: string, tab: Tab | null): void {
    this.listeners.forEach((callback) => {
      try {
        callback(path, tab);
      } catch (error) {
        console.error('Navigation listener error:', error);
      }
    });
  }
}

// ============================================
// Singleton Export
// ============================================

export const navigationService = new NavigationServiceImpl();

// ============================================
// Convenience Functions
// ============================================

export const navigateTo = navigationService.navigateTo.bind(navigationService);
export const navigateToSpec = navigationService.navigateToSpec.bind(navigationService);
export const navigateToConversation = navigationService.navigateToConversation.bind(navigationService);
export const navigateToChannel = navigationService.navigateToChannel.bind(navigationService);
export const navigateToRequirement = navigationService.navigateToRequirement.bind(navigationService);
export const navigateToGlossary = navigationService.navigateToGlossary.bind(navigationService);
export const navigateToArtifact = navigationService.navigateToArtifact.bind(navigationService);
export const navigateToDM = navigationService.navigateToDM.bind(navigationService);
export const navigateToPath = navigationService.navigateToPath.bind(navigationService);
export const navigateBack = navigationService.navigateBack.bind(navigationService);
export const navigateForward = navigationService.navigateForward.bind(navigationService);
