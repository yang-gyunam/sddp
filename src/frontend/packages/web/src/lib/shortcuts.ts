/**
 * Keyboard Shortcuts Manager
 * Global keyboard shortcut handling for the application
 */

import { get } from 'svelte/store';
import { toggleSidebar, togglePanel, sidePanel, tabActions, globalActiveTab, commandPalette } from '$stores';

export interface Shortcut {
  key: string;
  ctrl?: boolean;
  shift?: boolean;
  alt?: boolean;
  meta?: boolean;
  action: () => void;
  description: string;
  category?: string;
}

// Helper to close the current active tab
function closeCurrentTab() {
  const activeTab = get(globalActiveTab);
  if (activeTab?.id) {
    tabActions.closeTab(activeTab.id);
  }
}

function triggerSave() {
  if (typeof document === 'undefined') return;
  const activeElement = document.activeElement as HTMLElement | null;
  const form = activeElement?.closest('form');
  if (form) {
    const htmlForm = form as HTMLFormElement;
    if (typeof htmlForm.requestSubmit === 'function') {
      htmlForm.requestSubmit();
    } else {
      htmlForm.submit();
    }
    return;
  }

  if (typeof window !== 'undefined') {
    window.dispatchEvent(new CustomEvent('sddp:save'));
  }
}

// Default shortcuts
const defaultShortcuts: Shortcut[] = [
  // Layout shortcuts
  {
    key: 'b',
    ctrl: true,
    action: toggleSidebar,
    description: 'Toggle Sidebar',
    category: 'Layout',
  },
  {
    key: 'j',
    ctrl: true,
    action: togglePanel,
    description: 'Toggle Panel',
    category: 'Layout',
  },
  {
    key: 'b',
    ctrl: true,
    alt: true,
    action: () => sidePanel.toggle(),
    description: 'Toggle Right Panel',
    category: 'Layout',
  },
  // Tab shortcuts
  {
    key: 'w',
    ctrl: true,
    action: closeCurrentTab,
    description: 'Close Current Tab',
    category: 'Tabs',
  },
  {
    key: 'k',
    ctrl: true,
    action: () => commandPalette.toggle(),
    description: 'Open Command Palette',
    category: 'Navigation',
  },
  {
    key: 's',
    ctrl: true,
    action: triggerSave,
    description: 'Save',
    category: 'General',
  },
];

// Registered shortcuts
let shortcuts: Shortcut[] = [...defaultShortcuts];
let isEnabled = true;

/**
 * Check if a keyboard event matches a shortcut
 */
function matchesShortcut(event: KeyboardEvent, shortcut: Shortcut): boolean {
  const key = event.key.toLowerCase();
  const shortcutKey = shortcut.key.toLowerCase();

  // Check key
  if (key !== shortcutKey) return false;

  // Check modifiers
  const ctrlOrMeta = event.ctrlKey || event.metaKey;
  if (shortcut.ctrl && !ctrlOrMeta) return false;
  if (!shortcut.ctrl && ctrlOrMeta) return false;

  if (shortcut.shift && !event.shiftKey) return false;
  if (!shortcut.shift && event.shiftKey) return false;

  if (shortcut.alt && !event.altKey) return false;
  if (!shortcut.alt && event.altKey) return false;

  return true;
}

/**
 * Handle keydown event
 */
function handleKeydown(event: KeyboardEvent): void {
  if (!isEnabled) return;

  // Skip if in input, textarea, or contenteditable
  const target = event.target as HTMLElement;
  const isSaveCombo = (event.ctrlKey || event.metaKey) &&
    !event.shiftKey &&
    !event.altKey &&
    event.key.toLowerCase() === 's';
  if (
    target.tagName === 'INPUT' ||
    target.tagName === 'TEXTAREA' ||
    target.isContentEditable
  ) {
    if (!isSaveCombo) {
      return;
    }
  }

  // Find matching shortcut
  for (const shortcut of shortcuts) {
    if (matchesShortcut(event, shortcut)) {
      event.preventDefault();
      event.stopPropagation();
      shortcut.action();
      return;
    }
  }
}

/**
 * Initialize keyboard shortcuts
 */
export function initShortcuts(): () => void {
  document.addEventListener('keydown', handleKeydown);

  return () => {
    document.removeEventListener('keydown', handleKeydown);
  };
}

/**
 * Register a new shortcut
 */
export function registerShortcut(shortcut: Shortcut): void {
  // Check for duplicates
  const existing = shortcuts.find((s) => {
    return (
      s.key === shortcut.key &&
      s.ctrl === shortcut.ctrl &&
      s.shift === shortcut.shift &&
      s.alt === shortcut.alt
    );
  });

  if (existing) {
    console.warn(`Shortcut ${formatShortcut(shortcut)} already exists. Overwriting.`);
    shortcuts = shortcuts.filter((s) => s !== existing);
  }

  shortcuts.push(shortcut);
}

/**
 * Unregister a shortcut
 */
export function unregisterShortcut(key: string, modifiers?: { ctrl?: boolean; shift?: boolean; alt?: boolean }): void {
  shortcuts = shortcuts.filter((s) => {
    if (s.key !== key) return true;
    if (modifiers?.ctrl !== undefined && s.ctrl !== modifiers.ctrl) return true;
    if (modifiers?.shift !== undefined && s.shift !== modifiers.shift) return true;
    if (modifiers?.alt !== undefined && s.alt !== modifiers.alt) return true;
    return false;
  });
}

/**
 * Get all registered shortcuts
 */
export function getShortcuts(): Shortcut[] {
  return [...shortcuts];
}

/**
 * Get shortcuts by category
 */
export function getShortcutsByCategory(): Record<string, Shortcut[]> {
  const grouped: Record<string, Shortcut[]> = {};

  for (const shortcut of shortcuts) {
    const category = shortcut.category || 'General';
    if (!grouped[category]) {
      grouped[category] = [];
    }
    grouped[category].push(shortcut);
  }

  return grouped;
}

/**
 * Enable shortcuts
 */
export function enableShortcuts(): void {
  isEnabled = true;
}

/**
 * Disable shortcuts
 */
export function disableShortcuts(): void {
  isEnabled = false;
}

/**
 * Check if shortcuts are enabled
 */
export function areShortcutsEnabled(): boolean {
  return isEnabled;
}

/**
 * Format shortcut for display
 */
export function formatShortcut(shortcut: Shortcut): string {
  const parts: string[] = [];

  // Use platform-specific modifier symbols
  const isMac = typeof navigator !== 'undefined' && /Mac|iPhone|iPad|iPod/.test(navigator.userAgent);

  if (shortcut.ctrl) {
    parts.push(isMac ? '⌘' : 'Ctrl');
  }
  if (shortcut.shift) {
    parts.push(isMac ? '⇧' : 'Shift');
  }
  if (shortcut.alt) {
    parts.push(isMac ? '⌥' : 'Alt');
  }

  parts.push(shortcut.key.toUpperCase());

  return parts.join(isMac ? '' : '+');
}

/**
 * Reset to default shortcuts
 */
export function resetShortcuts(): void {
  shortcuts = [...defaultShortcuts];
}
