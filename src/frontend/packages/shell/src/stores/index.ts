// @sddp/shell - Store Exports (explicit to prevent name collisions)

// Layout
export {
  layoutStore,
  toggleSidebar,
  openSidebar,
  closeSidebar,
  setSidebarWidth,
  setActiveSidebarPanel,
  togglePanel,
  openPanel,
  closePanel,
  setPanelHeight,
  setActivePanelTab,
  startResize,
  endResize,
  setActiveActivityItem,
  getLayoutState,
  getSidebarState,
  getPanelState,
  isSidebarOpen,
  isPanelOpen,
  subscribeLayout,
  resetLayout,
  FOCUS_ZONES,
  getCurrentFocusZone,
  setCurrentFocusZone,
  focusNextZone,
  focusPreviousZone,
  focusZone,
  setupFocusZoneKeyboard,
} from './layout.store';
export type { FocusZone } from './layout.store';

// Tabs
export {
  tabsStore,
  editorGroups,
  activeGroup,
  activeGroupId,
  globalActiveTab,
  allTabs,
  tabActions,
  setupTabKeyboardShortcuts,
} from './tabs.store';

// Panel
export { panel, $panel, statusBar, $statusBar } from './panel.store';

// Panel Content
export { terminal, problems, output } from './panel-content.store';

// Side Panel
export { sidePanel, $sidePanel } from './side-panel.store';

// Command Palette
export { commandPaletteActions, commandPalette } from './command.store';

// Menu
export {
  menuStore,
  flatMenuItems,
  selectedMenuItem,
  isExpanded,
  menuLoading,
  menuError,
} from './menu.store';

// Toast
export { toastStore, toast } from './toast.store';
export type { ToastType, ToastActionVariant, ToastAction, ToastData, ToastDataState } from './toast.store';

// Tab State
export {
  tabStateStore,
  getTabState,
  setTabState,
  updateTabState,
  clearTabState,
  clearTabStates,
  copyTabState,
  resetTabStateStore,
} from './tab-state.store';
