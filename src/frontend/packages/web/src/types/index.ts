// @sddp/web - Type Exports

// Re-export from @sddp packages
export type * from '@sddp/shell/core/types';
export type * from '@sddp/shell/types';
export type * from '@sddp/shell/auth/types';

/**
 * Theme type
 */
export type ThemeMode = 'light' | 'dark' | 'system';

/**
 * Error notification
 */
export interface ErrorNotification {
  id: string;
  message: string;
  code?: string;
  severity: 'error' | 'warning' | 'info';
  timestamp: string;
  dismissed: boolean;
}

// Note: ActivityBarItem and TreeNode are now exported from @sddp/shell/types
