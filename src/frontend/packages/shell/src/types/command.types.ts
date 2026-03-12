/**
 * Command Palette type definitions
 */

/**
 * Command definition for the command palette
 */
export interface Command {
  /** Unique identifier (e.g., 'file.new', 'view.splitRight') */
  id: string;
  /** Display label */
  label: string;
  /** Optional description */
  description?: string;
  /** Category ID for grouping */
  category: string;
  /** Keyboard shortcut (e.g., 'Ctrl+N', 'Ctrl+Shift+P') */
  keybinding?: string;
  /** Icon name */
  icon?: string;
  /** Action to execute */
  action: () => void | Promise<void>;
  /** Conditional availability check */
  when?: () => boolean;
}

/**
 * Command category for grouping commands
 */
export interface CommandCategory {
  /** Unique identifier */
  id: string;
  /** Display label */
  label: string;
  /** Icon name */
  icon?: string;
  /** Sort priority (lower = higher priority) */
  priority: number;
}

/**
 * Command palette state
 */
export interface CommandPaletteState {
  /** Whether the palette is visible */
  visible: boolean;
  /** Current search query */
  query: string;
  /** Currently selected command index */
  selectedIndex: number;
  /** Filtered commands based on query */
  filteredCommands: Command[];
  /** Available categories */
  categories: CommandCategory[];
}

/**
 * Result of command execution
 */
export interface CommandExecutionResult {
  /** Whether execution was successful */
  success: boolean;
  /** Success message */
  message?: string;
  /** Error message if failed */
  error?: string;
}

/**
 * Grouped commands by category
 */
export interface CommandGroup {
  categoryId: string;
  categoryLabel: string;
  categoryIcon: string;
  commands: Command[];
}
