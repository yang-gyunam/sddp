/** URL history belongs to the router; tabs retain pathname identity. */
import { get } from 'svelte/store';
import { tabActions, tabsStore, globalActiveTab, activeGroupId } from '../stores/tabs.store';

export class RouterService {
  private static initialized = false;
  private static unsubscribeTab: (() => void) | null = null;
  private static isNavigating = false;
  private static onNavigateToPath: ((path: string) => void) | null = null;
  private static searches = new Map<string, string>();
  private static pending: { path: string; search: string } | null = null;

  static setNavigateHandler(handler: (path: string) => void): void {
    this.onNavigateToPath = handler;
  }

  static initialize(): () => void {
    if (this.initialized) return () => {};
    this.initialized = true;
    this.pending = { path: window.location.pathname, search: window.location.search };
    window.addEventListener('popstate', this.handlePopState);
    // A restored tab must not overwrite a directly opened URL during subscription.
    this.isNavigating = true;
    try {
      let previousIdentity = '';
      this.unsubscribeTab = globalActiveTab.subscribe((tab) => {
        const identity = tab ? `${tab.id}:${tab.path || '/'}` : '';
        const changed = identity !== previousIdentity;
        previousIdentity = identity;
        // Content/read-state updates are not navigation. In particular, they must
        // not cancel an async destination lookup by restoring the source URL.
        if (!tab || this.isNavigating || !changed) return;
        const path = tab.path || '/';
        const search =
          this.pending?.path === path ? this.pending.search : this.searches.get(tab.id) || '';
        this.pending = null;
        this.writeUrl(path + search, false);
      });
      if (window.location.pathname !== '/')
        this.activatePath(window.location.pathname, window.history.state);
      this.rememberLocation();
      this.stampLocation();
    } finally {
      this.isNavigating = false;
    }
    return () => this.cleanup();
  }

  /** Only the active tab may consume the browser's detail selection. */
  static isActiveTab(tabId: string): boolean {
    return get(globalActiveTab)?.id === tabId;
  }

  private static rememberLocation(): void {
    const tab = get(globalActiveTab);
    if (tab?.path === window.location.pathname) {
      this.searches.set(tab.id, window.location.search);
      this.pending = null;
    } else {
      this.pending = { path: window.location.pathname, search: window.location.search };
    }
  }

  private static stampLocation(): void {
    const tab = get(globalActiveTab);
    if (tab?.path !== window.location.pathname) return;
    window.history.replaceState(
      { ...window.history.state, tabId: tab.id, groupId: get(activeGroupId) },
      '',
      window.location.href
    );
  }

  private static notify(): void {
    window.dispatchEvent(new Event('sddp:routechange'));
  }

  private static activatePath(path: string, owner?: { tabId?: string; groupId?: string }): void {
    const state = get(tabsStore);
    // Tab IDs survive moving between groups; prefer the recorded tab wherever it lives.
    const originalGroup = state.groups.find((g) =>
      g.tabs.some((t) => t.id === owner?.tabId && t.path === path)
    );
    const group = originalGroup || state.groups.find((g) => g.id === get(activeGroupId));
    const tab =
      originalGroup?.tabs.find((t) => t.id === owner?.tabId) ||
      group?.tabs.find((t) => t.path === path);
    if (tab && group) tabActions.switchToTab(tab.id, group.id);
    else this.onNavigateToPath?.(path);
  }

  private static handlePopState = (): void => {
    this.isNavigating = true;
    try {
      this.activatePath(window.location.pathname, window.history.state);
      this.rememberLocation();
      this.stampLocation();
    } finally {
      this.isNavigating = false;
    }
    this.notify();
  };

  private static writeUrl(path: string, replace: boolean): void {
    if (
      window.location.pathname + window.location.search === path &&
      window.history.state?.tabId === get(globalActiveTab)?.id
    )
      return;
    if (replace) window.history.replaceState({ path }, '', path);
    else window.history.pushState({ path }, '', path);
    this.rememberLocation();
    this.stampLocation();
    this.notify();
  }

  static navigate(path: string): void {
    this.isNavigating = true;
    try {
      this.writeUrl(path, false);
      this.activatePath(window.location.pathname);
      this.rememberLocation();
      this.stampLocation();
    } finally {
      this.isNavigating = false;
    }
    this.notify();
  }

  /** Set detail selection without changing the owning tab's pathname. */
  static selectItem(id: string | null, tabId?: string): void {
    if (tabId && !this.isActiveTab(tabId)) {
      const group = get(tabsStore).groups.find((g) => g.tabs.some((t) => t.id === tabId));
      if (!group) return;
      this.isNavigating = true;
      try {
        tabActions.switchToTab(tabId, group.id);
      } finally {
        this.isNavigating = false;
      }
    }
    const tab = get(globalActiveTab);
    const path = tab?.path || window.location.pathname;
    const query = new URLSearchParams(
      (!window.history.state?.tabId || window.history.state.tabId === tab?.id) &&
        window.location.pathname === path
        ? window.location.search
        : this.searches.get(tab?.id || '') || ''
    );
    if (id) query.set('id', id);
    else query.delete('id');
    const search = query.toString();
    this.writeUrl(path + (search ? `?${search}` : ''), false);
  }

  /** TabFactory may synchronize a pathname after tab activation. Keep its detail query. */
  static replace(path: string): void {
    const tab = get(globalActiveTab);
    const search =
      this.pending?.path === path
        ? this.pending.search
        : tab?.path === path
          ? this.searches.get(tab.id) || ''
          : '';
    const target = path.includes('?') ? path : path + search;
    this.writeUrl(target, true);
  }

  static back(): void {
    window.history.back();
  }
  static forward(): void {
    window.history.forward();
  }
  static getCurrentPath(): string {
    return window.location.pathname;
  }

  private static cleanup(): void {
    window.removeEventListener('popstate', this.handlePopState);
    this.unsubscribeTab?.();
    this.unsubscribeTab = null;
    this.initialized = false;
    this.isNavigating = false;
    this.searches.clear();
    this.pending = null;
    this.onNavigateToPath = null;
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
  static matchPath(pattern: string, path: string): Record<string, string> | null {
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
  static buildPath(pattern: string, params: Record<string, string>): string {
    let path = pattern;
    for (const [key, value] of Object.entries(params)) {
      path = path.replace(`:${key}`, value);
    }
    return path;
  }
}
