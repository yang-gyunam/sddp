/**
 * Layout Store - Layout State Management
 * Manages sidebar, panel, and activity bar states
 *
 * Note: Panel state is delegated to panel.store.ts for single source of truth.
 * The panel functions here are kept for backward compatibility with shortcuts and commands.
 */

import { createStore, type Store } from '../core/services';
import type { LayoutState, SidebarState, PanelState } from '../types';
import { APP_SIDEBAR } from '../types';
import { panel } from './panel.store';

const LAYOUT_STORAGE_KEY = 'sddp-layout';

// Default values - derived from layout constants
const DEFAULT_SIDEBAR: SidebarState = {
  isOpen: true,
  width: APP_SIDEBAR.width,
  minWidth: APP_SIDEBAR.minWidth,
  maxWidth: APP_SIDEBAR.maxWidth,
  activePanel: 'dashboard',
};

const DEFAULT_PANEL: PanelState = {
  isOpen: false,
  height: 200,
  minHeight: 100,
  maxHeight: 500,
  activeTab: 'terminal',
};

const DEFAULT_LAYOUT: LayoutState = {
  sidebar: DEFAULT_SIDEBAR,
  panel: DEFAULT_PANEL,
  activityBar: {
    activeItemId: 'dashboard',
  },
  isResizing: false,
  resizeTarget: null,
};

/**
 * Load initial layout from localStorage
 */
function loadInitialLayout(): LayoutState {
  if (typeof window === 'undefined') return DEFAULT_LAYOUT;

  try {
    const stored = localStorage.getItem(LAYOUT_STORAGE_KEY);
    if (stored) {
      const parsed = JSON.parse(stored) as Partial<LayoutState>;
      return {
        ...DEFAULT_LAYOUT,
        sidebar: { ...DEFAULT_SIDEBAR, ...parsed.sidebar },
        panel: { ...DEFAULT_PANEL, ...parsed.panel },
        activityBar: { ...DEFAULT_LAYOUT.activityBar, ...parsed.activityBar },
      };
    }
  } catch {
    // Invalid JSON, use default
  }

  return DEFAULT_LAYOUT;
}

/**
 * Persist layout to localStorage (only serializable parts)
 */
function persistLayout(state: LayoutState): void {
  if (typeof localStorage === 'undefined') return;

  const toStore = {
    sidebar: {
      isOpen: state.sidebar.isOpen,
      width: state.sidebar.width,
      activePanel: state.sidebar.activePanel,
    },
    panel: {
      isOpen: state.panel.isOpen,
      height: state.panel.height,
      activeTab: state.panel.activeTab,
    },
    activityBar: state.activityBar,
  };

  localStorage.setItem(LAYOUT_STORAGE_KEY, JSON.stringify(toStore));
}

// Create the layout store
const layoutStore: Store<LayoutState> = createStore<LayoutState>(loadInitialLayout());

// ============================================
// Sidebar Actions
// ============================================

/**
 * Toggle sidebar visibility
 */
export function toggleSidebar(): void {
  layoutStore.update((state) => {
    const newState = {
      ...state,
      sidebar: {
        ...state.sidebar,
        isOpen: !state.sidebar.isOpen,
      },
    };
    persistLayout(newState);
    return newState;
  });
}

/**
 * Open sidebar
 */
export function openSidebar(): void {
  layoutStore.update((state) => {
    const newState = {
      ...state,
      sidebar: {
        ...state.sidebar,
        isOpen: true,
      },
    };
    persistLayout(newState);
    return newState;
  });
}

/**
 * Close sidebar
 */
export function closeSidebar(): void {
  layoutStore.update((state) => {
    const newState = {
      ...state,
      sidebar: {
        ...state.sidebar,
        isOpen: false,
      },
    };
    persistLayout(newState);
    return newState;
  });
}

/**
 * Set sidebar width
 */
export function setSidebarWidth(width: number): void {
  layoutStore.update((state) => {
    const clampedWidth = Math.max(
      state.sidebar.minWidth,
      Math.min(state.sidebar.maxWidth, width)
    );

    // Auto-close if below minimum threshold
    const isOpen = clampedWidth > state.sidebar.minWidth * 0.5;

    const newState = {
      ...state,
      sidebar: {
        ...state.sidebar,
        width: isOpen ? clampedWidth : state.sidebar.minWidth,
        isOpen,
      },
    };
    persistLayout(newState);
    return newState;
  });
}

/**
 * Set active sidebar panel
 * @param forceOpen If true, always open the sidebar without toggle behavior
 */
export function setActiveSidebarPanel(panelId: string | null, forceOpen = false): void {
  layoutStore.update((state) => {
    // If clicking the same panel, toggle sidebar (unless forceOpen)
    if (!forceOpen && state.sidebar.activePanel === panelId && state.sidebar.isOpen) {
      const newState = {
        ...state,
        sidebar: {
          ...state.sidebar,
          isOpen: false,
        },
        activityBar: {
          ...state.activityBar,
          activeItemId: panelId,
        },
      };
      persistLayout(newState);
      return newState;
    }

    // Open sidebar with new panel
    const newState = {
      ...state,
      sidebar: {
        ...state.sidebar,
        isOpen: true,
        activePanel: panelId,
      },
      activityBar: {
        ...state.activityBar,
        activeItemId: panelId,
      },
    };
    persistLayout(newState);
    return newState;
  });
}

// ============================================
// Panel Actions (delegated to panel.store.ts)
// ============================================

/**
 * Toggle bottom panel visibility
 * @deprecated Use panel.toggle() from panel.store.ts directly
 */
export function togglePanel(): void {
  panel.toggle();
}

/**
 * Open bottom panel
 * @deprecated Use panel.show() from panel.store.ts directly
 */
export function openPanel(): void {
  panel.show();
}

/**
 * Close bottom panel
 * @deprecated Use panel.hide() from panel.store.ts directly
 */
export function closePanel(): void {
  panel.hide();
}

/**
 * Set panel height
 * @deprecated Use panel.setHeight() from panel.store.ts directly
 */
export function setPanelHeight(height: number): void {
  panel.setHeight(height);
}

/**
 * Set active panel tab
 * @deprecated Use panel.setActiveTab() from panel.store.ts directly
 */
export function setActivePanelTab(tabId: string): void {
  panel.setActiveTab(tabId);
}

// ============================================
// Resize Actions
// ============================================

/**
 * Start resizing
 */
export function startResize(target: 'sidebar' | 'panel'): void {
  layoutStore.update((state) => ({
    ...state,
    isResizing: true,
    resizeTarget: target,
  }));

  // Add global styles during resize
  document.body.style.cursor = target === 'sidebar' ? 'col-resize' : 'row-resize';
  document.body.style.userSelect = 'none';
}

/**
 * End resizing
 */
export function endResize(): void {
  layoutStore.update((state) => ({
    ...state,
    isResizing: false,
    resizeTarget: null,
  }));

  // Remove global styles
  document.body.style.cursor = '';
  document.body.style.userSelect = '';
}

// ============================================
// Activity Bar Actions
// ============================================

/**
 * Set active activity bar item
 */
export function setActiveActivityItem(itemId: string | null, forceOpen = false): void {
  setActiveSidebarPanel(itemId, forceOpen);
}

// ============================================
// Getters
// ============================================

/**
 * Get current layout state
 */
export function getLayoutState(): LayoutState {
  return layoutStore.get();
}

/**
 * Get sidebar state
 */
export function getSidebarState(): SidebarState {
  return layoutStore.get().sidebar;
}

/**
 * Get panel state
 * @deprecated Use panel.get() from panel.store.ts directly
 */
export function getPanelState(): PanelState {
  const panelState = panel.get();
  return {
    isOpen: !panelState.collapsed,
    height: panelState.height,
    minHeight: panelState.minHeight,
    maxHeight: panelState.maxHeight,
    activeTab: panelState.activeTab,
  };
}

/**
 * Check if sidebar is open
 */
export function isSidebarOpen(): boolean {
  return layoutStore.get().sidebar.isOpen;
}

/**
 * Check if panel is open
 * @deprecated Use !panel.get().collapsed from panel.store.ts directly
 */
export function isPanelOpen(): boolean {
  return !panel.get().collapsed;
}

/**
 * Subscribe to layout state changes
 */
export function subscribeLayout(
  listener: (state: LayoutState, prevState: LayoutState) => void
): () => void {
  return layoutStore.subscribe(listener);
}

/**
 * Reset layout to defaults
 */
export function resetLayout(): void {
  layoutStore.set(DEFAULT_LAYOUT);
  if (typeof localStorage !== 'undefined') {
    localStorage.removeItem(LAYOUT_STORAGE_KEY);
  }
}

// Export the store for direct access
export { layoutStore };

// ============================================
// Focus Zone Navigation (F6 cycling)
// ============================================

/**
 * Focus zone identifiers in order of navigation
 */
export const FOCUS_ZONES = ['activity', 'sidebar', 'editor', 'panel'] as const;
export type FocusZone = (typeof FOCUS_ZONES)[number];

let currentFocusZone: FocusZone = 'editor';

/**
 * Get the current focus zone
 */
export function getCurrentFocusZone(): FocusZone {
  return currentFocusZone;
}

/**
 * Set the current focus zone
 */
export function setCurrentFocusZone(zone: FocusZone): void {
  currentFocusZone = zone;
}

/**
 * Move focus to the next zone
 */
export function focusNextZone(): void {
  const currentIndex = FOCUS_ZONES.indexOf(currentFocusZone);
  const nextIndex = (currentIndex + 1) % FOCUS_ZONES.length;
  const nextZone = FOCUS_ZONES[nextIndex];
  if (nextZone) {
    focusZone(nextZone);
  }
}

/**
 * Move focus to the previous zone
 */
export function focusPreviousZone(): void {
  const currentIndex = FOCUS_ZONES.indexOf(currentFocusZone);
  const prevIndex = (currentIndex - 1 + FOCUS_ZONES.length) % FOCUS_ZONES.length;
  const prevZone = FOCUS_ZONES[prevIndex];
  if (prevZone) {
    focusZone(prevZone);
  }
}

/**
 * Focus a specific zone
 */
export function focusZone(zone: FocusZone): void {
  const element = document.querySelector(`[data-focus-zone="${zone}"]`) as HTMLElement | null;
  if (element) {
    // Find the first focusable element within the zone
    const focusable = element.querySelector<HTMLElement>(
      'button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])'
    );
    if (focusable) {
      focusable.focus();
    } else {
      // If no focusable element, focus the zone container itself
      element.setAttribute('tabindex', '-1');
      element.focus();
    }
    currentFocusZone = zone;
  }
}

/**
 * Setup F6 keyboard navigation for focus zones
 */
export function setupFocusZoneKeyboard(): () => void {
  const handleKeyDown = (e: KeyboardEvent) => {
    if (e.key === 'F6') {
      e.preventDefault();
      if (e.shiftKey) {
        focusPreviousZone();
      } else {
        focusNextZone();
      }
    }
  };

  // Track focus to update current zone
  const handleFocusIn = (e: FocusEvent) => {
    const target = e.target as HTMLElement | null;
    if (!target) return;

    for (const zone of FOCUS_ZONES) {
      const zoneElement = document.querySelector(`[data-focus-zone="${zone}"]`);
      if (zoneElement?.contains(target)) {
        currentFocusZone = zone;
        break;
      }
    }
  };

  document.addEventListener('keydown', handleKeyDown);
  document.addEventListener('focusin', handleFocusIn);

  return () => {
    document.removeEventListener('keydown', handleKeyDown);
    document.removeEventListener('focusin', handleFocusIn);
  };
}
