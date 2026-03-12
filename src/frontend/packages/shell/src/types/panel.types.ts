/**
 * Side Panel and Bottom Panel type definitions
 */

// Side Panel State
export interface SidePanelState {
  visible: boolean;
  title: string;
  component: string | null;
  props: Record<string, unknown>;
  width: number;
  minWidth: number;
  maxWidth: number;
  animating: boolean;
  resizing: boolean;
  isMobile: boolean;
}

// Side Panel Config
export interface SidePanelConfig {
  defaultWidth?: number;
  minWidth?: number;
  maxWidth?: number;
}

// Bottom Panel Tab
export interface PanelTab {
  id: string;
  label: string;
  icon?: string;
  badge?: number;
  component?: unknown;
  props?: Record<string, unknown>;
}

// Bottom Panel State
export interface BottomPanelState {
  collapsed: boolean;
  height: number;
  minHeight: number;
  maxHeight: number;
  activeTab: string;
  tabs: PanelTab[];
  resizing: boolean;
}

// Status Bar Item
export interface StatusBarItem {
  id: string;
  text: string;
  icon?: string;
  tooltip?: string;
  priority: number;
  alignment: 'left' | 'right';
  color?: string;
  command?: string;
  visible?: boolean;
}

// Status Bar State
export interface StatusBarState {
  items: StatusBarItem[];
  currentUser: {
    name: string;
    email: string;
  } | null;
  visible: boolean;
}

// ============================================
// Panel Content Types
// ============================================

// Terminal Entry
export interface TerminalEntry {
  id: string;
  type: 'input' | 'output' | 'error' | 'system';
  content: string;
  timestamp: Date;
}

// Terminal State
export interface TerminalState {
  entries: TerminalEntry[];
  isRunning: boolean;
  currentCommand: string;
}

// Problem Severity
export type ProblemSeverity = 'error' | 'warning' | 'info' | 'hint';

// Problem Type (source category)
export type ProblemType = 'build' | 'lint' | 'runtime' | 'api';

// Problem Entry
export interface ProblemEntry {
  id: string;
  severity: ProblemSeverity;
  type?: ProblemType;
  message: string;
  source: string;
  file?: string;
  line?: number;
  column?: number;
  code?: string;
  stack?: string;
  component?: string;
  timestamp: Date;
}

// Problems State
export interface ProblemsState {
  entries: ProblemEntry[];
  errorCount: number;
  warningCount: number;
  infoCount: number;
}

// Output Log Level
export type OutputLogLevel = 'debug' | 'info' | 'warn' | 'error';

// Output Entry
export interface OutputEntry {
  id: string;
  level: OutputLogLevel;
  source: string;
  message: string;
  timestamp: Date;
  data?: unknown;
}

// Output State
export interface OutputState {
  entries: OutputEntry[];
  filter: OutputLogLevel | 'all';
  sources: string[];
}
