/**
 * Layout and UI state type definitions
 * Based on VSCode-style layout system
 */

// Component props base
export interface ComponentProps {
  class?: string;
  id?: string;
  'data-testid'?: string;
}

// Layout configuration
export interface LayoutConfig {
  sidebarWidth: number;
  panelHeight: number;
  activityBarVisible: boolean;
  sidebarCollapsed: boolean;
  panelCollapsed: boolean;
  theme: 'light' | 'dark';
}

// Activity bar types
export interface Activity {
  id: string;
  icon: string;
  label: string;
  badge?: number;
  tooltip?: string;
  component?: unknown;
}

// Tab system types
export interface Tab {
  id: string;
  title: string;
  meta?: string;
  icon?: string;
  dirty: boolean;
  closable: boolean;
  component: unknown;
  props: Record<string, unknown>;
  path?: string;
  url?: string;
  menuId?: string;
  type?: 'PAGE' | 'FOLDER' | 'EXTERNAL';
}

export interface EditorGroupData {
  id: string;
  tabs: Tab[];
  activeTab: string;
  tabHistory: string[];
  splitDirection?: 'horizontal' | 'vertical';
  position: {
    x: number;
    y: number;
    width: number;
    height: number;
  };
}

// Layout specific menu types (for UI only)
export interface LayoutMenuNode {
  id: string;
  name: string;
  url?: string;
  icon?: string;
  order: number;
  type: 'PAGE' | 'FOLDER' | 'EXTERNAL';
  parentId?: string;
  children: LayoutMenuNode[];
  permissions: string[];
  isActive: boolean;
  expanded?: boolean;
}

// Panel types
export interface PanelConfig {
  id: string;
  title: string;
  icon?: string;
  component: unknown;
  visible: boolean;
  height?: number;
}

// Layout component props
export interface MainLayoutProps extends ComponentProps {
  user?: unknown;
  theme: 'light' | 'dark';
  config: LayoutConfig;
}

export interface ActivityBarProps extends ComponentProps {
  activities: Activity[];
  activeActivity: string;
  sidebarCollapsed: boolean;
  width?: number;
  onActivityChange: (activityId: string) => void;
}

export interface SidebarProps extends ComponentProps {
  activity?: Activity;
  width: number;
  collapsed: boolean;
  isMobile: boolean;
  onToggle: () => void;
  onResize?: (width: number) => void;
}

export interface EditorGroupProps extends ComponentProps {
  group: EditorGroupData;
  onTabChange: (tabId: string) => void;
  onTabClose: (tabId: string) => void;
  onSplit: (direction: 'horizontal' | 'vertical') => void;
}

export interface TabProps extends ComponentProps {
  tab: Tab;
  active: boolean;
  localActive?: boolean;
  onSelect: () => void;
  onClose: () => void;
  onContextMenu?: (event: MouseEvent) => void;
}

// Activity bar item (simplified for external use)
export interface ActivityBarItem {
  id: string;
  icon: string;
  label: string;
  badge?: number;
}

// Sidebar panel definition per activity
export interface SidebarPanelConfig {
  id: string;
  title: string;
  icon?: string;
  badge?: string | number;
}

export type ActivityPanelMap = Record<string, SidebarPanelConfig[]>;

// Tree node for hierarchical data
export interface TreeNode {
  id: string;
  label: string;
  icon?: string;
  children?: TreeNode[];
  expanded?: boolean;
  selected?: boolean;
  disabled?: boolean;
  data?: unknown;
}

// Collapsible panel action button
export interface CollapsiblePanelAction {
  id: string;
  icon: string;
  label: string;
  onClick: () => void;
}

export type SidebarPanelContent = import('svelte').Snippet;

export type SidebarPanelContentMap = Record<string, SidebarPanelContent>;

export type SidebarPanelActionMap = Record<string, CollapsiblePanelAction[]>;

export type SidebarPanelLoadingMap = Record<string, boolean>;

// Modal size variants
export type ModalSize = 'sm' | 'md' | 'lg' | 'xl' | 'full';

// Responsive breakpoints
export type Breakpoint = 'mobile' | 'tablet' | 'desktop';

// ===========================================
// Layout Dimension Constants (Single Source of Truth)
// ===========================================

/**
 * App-level sidebar (Activity Bar side).
 * Used by layout.store.ts and AppLayout.svelte.
 */
export const APP_SIDEBAR = {
  width: 250,
  minWidth: 200,
  maxWidth: 600,
} as const;

/**
 * Default values for SidebarDetailLayout component.
 * Used by Glossary, Artifacts, Specs, Requirements, Conversations, etc.
 */
export const SIDEBAR_DETAIL_LAYOUT = {
  /** Default sidebar (left list panel) width */
  sidebarWidth: 300,
  /** Minimum sidebar width */
  minSidebarWidth: 240,
  /** Maximum sidebar width */
  maxSidebarWidth: 480,
  /** Default right panel width */
  rightPanelWidth: 250,
  /** Minimum right panel width */
  minRightPanelWidth: 200,
  /** Maximum right panel width */
  maxRightPanelWidth: 400,
} as const;

/**
 * Default values for ContentDetailLayout component.
 * Used by Tasks (Kanban) and similar 2-column pages.
 */
export const CONTENT_DETAIL_LAYOUT = {
  /** Default main content width */
  mainContentWidth: 720,
  /** Minimum main content width (3 Kanban columns × 240px) */
  minMainContentWidth: 900,
  /** Maximum main content width */
  maxMainContentWidth: 1200,
  /** Minimum detail panel width (prevents disappearing) */
  minDetailPanelWidth: 280,
} as const;

export interface ResponsiveConfig {
  breakpoint: Breakpoint;
  isMobile: boolean;
  isTablet: boolean;
  isDesktop: boolean;
}

// Layout state types (for stores)
export interface SidebarState {
  isOpen: boolean;
  width: number;
  minWidth: number;
  maxWidth: number;
  activePanel: string | null;
}

export interface PanelState {
  isOpen: boolean;
  height: number;
  minHeight: number;
  maxHeight: number;
  activeTab: string;
}

export interface LayoutState {
  sidebar: SidebarState;
  panel: PanelState;
  activityBar: {
    activeItemId: string | null;
  };
  isResizing: boolean;
  resizeTarget: 'sidebar' | 'panel' | null;
}
