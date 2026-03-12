/**
 * Settings Types
 * Type definitions for Settings Activity
 */

import type { UserRef } from '../../shared/types';

// Settings Category
export type SettingsSection = 'user' | 'project' | 'system';

export interface SettingsCategory {
  id: string;
  label: string;
  icon: string;
  section: SettingsSection;
  children?: SettingsCategory[];
  requiredRole?: 'owner' | 'admin';
}

// ============================================================================
// User Settings
// ============================================================================

export interface UserProfile {
  id: string;
  name: string;
  email: string;
  username: string;
  bio?: string;
  avatarUrl?: string;
}

export type AccentColor =
  | 'blue'
  | 'indigo'
  | 'purple'
  | 'cyan'
  | 'teal'
  | 'rose'
  | 'orange'
  | 'sky'
  | 'pink'
  | 'lime'
  | 'amber'
  | 'seoul-green-aurora'
  | 'seoul-skycoral';

export type ToastDurationPreference = 2000 | 3000 | 5000;
export type DateFormatPreference = 'relative' | 'absolute' | 'iso';

export interface UserPreferences {
  theme: 'light' | 'dark' | 'auto';
  accentColor: AccentColor;
  toastDuration: ToastDurationPreference;
  dateFormat: DateFormatPreference;
}

export interface NotificationSettings {
  email: {
    mentions: boolean;
    conversations: boolean;
    specApprovals: boolean;
    taskAssignments: boolean;
    dailyDigest: boolean;
  };
  browser: {
    enabled: boolean;
    sound: boolean;
    preview: boolean;
  };
  channels: {
    default: 'all' | 'mentions' | 'nothing';
    custom: Record<string, 'all' | 'mentions' | 'nothing'>;
  };
}

// ============================================================================
// Project Settings
// ============================================================================

export interface ProjectSettings {
  id: string;
  name: string;
  description: string;
  status: 'planning' | 'active' | 'concluded' | 'archived';
}

export interface ProjectSnapshot {
  id: string;
  projectId: string;
  name: string;
  description?: string;
  snapshotType: 'manual' | 'pre_restore';
  status: 'completed' | 'in_progress' | 'failed';
  tableCounts: Record<string, number>;
  dataSizeBytes: number;
  createdBy: UserRef;
  createdAt: string;
}

export interface CreateSnapshotParams {
  name: string;
  description?: string;
}

export interface ProjectMember {
  userId: string;
  name: string;
  email: string;
  role: string;
  joinedAt: string;
}

export interface MemberInvitation {
  email: string;
  role: string;
  message?: string;
}

export interface ProjectIntegration {
  type: 'git' | 'jira' | 'slack' | 'webhook';
  enabled: boolean;
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  config: Record<string, any>;
}

export interface Webhook {
  id: string;
  url: string;
  events: string[];
  secret?: string;
  enabled: boolean;
}

// ============================================================================
// System Settings
// ============================================================================

export interface SystemUser {
  id: string;
  name: string;
  email: string;
  username: string;
  globalRole: string;
  status: 'active' | 'inactive';
  isBuiltIn: boolean;
  projects: {
    projectId: string;
    projectName: string;
    role: string;
  }[];
  createdAt: string;
  lastLoginAt?: string;
}

export interface CreateUserParams {
  username: string;
  email: string;
  displayName: string;
  password: string;
}

export interface SystemConfig {
  general: {
    siteName: string;
    siteUrl: string;
    adminEmail: string;
  };
  auth: {
    sessionTimeout: number;
    strongPassword: boolean;
    twoFactorAuth: boolean;
    ssoEnabled: boolean;
  };
  storage: {
    database: string;
    storageUsed: number;
    storageLimit: number;
  };
  performance: {
    cacheEnabled: boolean;
    cdnEnabled: boolean;
    compressionEnabled: boolean;
  };
  aiAgent: {
    enabled: boolean;
    provider: string;
    model: string;
    endpoint: string;
  };
}

export interface AuditLog {
  id: string;
  timestamp: string;
  userId: string;
  userName: string;
  action: string;
  resourceType: string;
  resourceId: string;
  ipAddress: string;
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  details: Record<string, any>;
}
