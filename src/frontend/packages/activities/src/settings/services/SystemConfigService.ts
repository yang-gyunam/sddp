/**
 * System Configuration Service
 * API client for system configuration management
 */

import { fetchWithAuth } from '../../shared/api';
import type { SystemConfig } from '../types';

// API Response types matching backend DTOs
export interface SystemConfigApiResponse {
  general: GeneralConfigResponse;
  auth: AuthConfigResponse;
  storage: StorageConfigResponse;
  performance: PerformanceConfigResponse;
  aiAgent: AiAgentConfigResponse;
}

export interface GeneralConfigResponse {
  siteName: string;
  siteUrl: string;
  adminEmail: string;
  defaultLocale: string;
  defaultTimezone: string;
}

export interface AuthConfigResponse {
  sessionTimeout: number;
  strongPassword: boolean;
  passwordMinLength: number;
  twoFactorAuth: boolean;
  ssoEnabled: boolean;
  maxFailedLogins: number;
  lockoutDuration: number;
}

export interface StorageConfigResponse {
  database: string;
  storageUsed: number;
  storageLimit: number;
  maxUploadSize: number;
}

export interface PerformanceConfigResponse {
  cacheEnabled: boolean;
  cacheTtlSeconds: number;
  cdnEnabled: boolean;
  compressionEnabled: boolean;
  rateLimitEnabled: boolean;
  rateLimitPerMinute: number;
}

export interface AiAgentConfigResponse {
  enabled: boolean;
  provider: string;
  model: string;
  endpoint: string;
  apiKey?: string;
  maxTokens: number;
  temperature: number;
}

// Update request types
export interface UpdateSystemConfigRequest {
  general?: Partial<GeneralConfigResponse>;
  auth?: Partial<AuthConfigResponse>;
  storage?: { maxUploadSize?: number };
  performance?: Partial<PerformanceConfigResponse>;
  aiAgent?: Partial<AiAgentConfigResponse>;
}

/**
 * Convert API response to frontend SystemConfig type
 */
function toSystemConfig(response: SystemConfigApiResponse): SystemConfig {
  return {
    general: {
      siteName: response.general.siteName,
      siteUrl: response.general.siteUrl,
      adminEmail: response.general.adminEmail,
    },
    auth: {
      sessionTimeout: response.auth.sessionTimeout,
      strongPassword: response.auth.strongPassword,
      twoFactorAuth: response.auth.twoFactorAuth,
      ssoEnabled: response.auth.ssoEnabled,
    },
    storage: {
      database: response.storage.database,
      storageUsed: response.storage.storageUsed,
      storageLimit: response.storage.storageLimit,
    },
    performance: {
      cacheEnabled: response.performance.cacheEnabled,
      cdnEnabled: response.performance.cdnEnabled,
      compressionEnabled: response.performance.compressionEnabled,
    },
    aiAgent: {
      enabled: response.aiAgent.enabled,
      provider: response.aiAgent.provider,
      model: response.aiAgent.model,
      endpoint: response.aiAgent.endpoint,
    },
  };
}

/**
 * Convert frontend SystemConfig to API update request
 */
function toUpdateRequest(config: SystemConfig): UpdateSystemConfigRequest {
  return {
    general: {
      adminEmail: config.general.adminEmail,
    },
    auth: {
      sessionTimeout: config.auth.sessionTimeout,
      strongPassword: config.auth.strongPassword,
      twoFactorAuth: config.auth.twoFactorAuth,
      ssoEnabled: config.auth.ssoEnabled,
    },
    performance: {
      cacheEnabled: config.performance.cacheEnabled,
      cdnEnabled: config.performance.cdnEnabled,
      compressionEnabled: config.performance.compressionEnabled,
    },
    aiAgent: {
      enabled: config.aiAgent.enabled,
      provider: config.aiAgent.provider,
      model: config.aiAgent.model,
      endpoint: config.aiAgent.endpoint,
    },
  };
}

/**
 * System Config Service
 */
class SystemConfigService {
  private tenantId: string = '';
  private projectId: string = '';

  setContext(tenantId: string, projectId?: string) {
    this.tenantId = tenantId;
    this.projectId = projectId || '';
  }

  /**
   * Get system configuration
   */
  async getConfig(): Promise<SystemConfig> {
    const response = await fetchWithAuth<SystemConfigApiResponse>('/system/config', {
      tenantId: this.tenantId || undefined,
      projectId: this.projectId || undefined,
    });

    if (!response) {
      throw new Error('Failed to fetch system configuration');
    }

    return toSystemConfig(response);
  }

  /**
   * Save system configuration
   */
  async saveConfig(config: SystemConfig): Promise<SystemConfig> {
    const request = toUpdateRequest(config);

    const response = await fetchWithAuth<SystemConfigApiResponse>('/system/config', {
      method: 'PUT',
      body: request,
      tenantId: this.tenantId || undefined,
      projectId: this.projectId || undefined,
    });

    if (!response) {
      throw new Error('Failed to save system configuration');
    }

    return toSystemConfig(response);
  }

  /**
   * Save a single config value
   */
  async saveConfigValue(group: string, key: string, value: string): Promise<void> {
    await fetchWithAuth(`/system/config/groups/${group}/${key}`, {
      method: 'PUT',
      body: { value },
      tenantId: this.tenantId || undefined,
      projectId: this.projectId || undefined,
    });
  }

  /**
   * Save a config group (all values in the group at once)
   */
  async saveConfigGroup(group: string, values: Record<string, string>): Promise<void> {
    await fetchWithAuth(`/system/config/groups/${group}`, {
      method: 'PUT',
      body: { values },
      tenantId: this.tenantId || undefined,
      projectId: this.projectId || undefined,
    });
  }

  /**
   * Reset configuration to defaults
   */
  async resetToDefault(): Promise<SystemConfig> {
    const response = await fetchWithAuth<SystemConfigApiResponse>('/system/config/reset', {
      method: 'POST',
      tenantId: this.tenantId || undefined,
      projectId: this.projectId || undefined,
    });

    if (!response) {
      throw new Error('Failed to reset system configuration');
    }

    return toSystemConfig(response);
  }
}

// Singleton instance
let instance: SystemConfigService | null = null;

export function getSystemConfigService(): SystemConfigService {
  if (!instance) {
    instance = new SystemConfigService();
  }
  return instance;
}

/**
 * Reset the singleton instance (for testing/logout)
 */
export function resetSystemConfigService(): void {
  instance = null;
}

export type { SystemConfigService };
