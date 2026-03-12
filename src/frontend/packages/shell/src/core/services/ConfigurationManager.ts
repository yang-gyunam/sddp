/**
 * ConfigurationManager - Environment Configuration
 * Manages environment variables and application configuration
 */

export interface AppConfig {
  apiUrl: string;
  apiVersion: string;
  enableDebug: boolean;
  enableAnalytics: boolean;
  enableAiFeatures: boolean;
  enableArtifactMaintenance: boolean;
  enableProjectIntegrations: boolean;
  enableSpecSupplementaryTabs: boolean;
  enableSystemConfigAuthentication: boolean;
  enableSystemConfigPerformance: boolean;
  enableRunesToastStore: boolean;
  enableRunesPanelStore: boolean;
  enableRunesTabStateStore: boolean;
  enableRunesCommandStore: boolean;
  enableRunesMenuStore: boolean;
  enableRunesSidePanelStore: boolean;
  enableRunesPanelContentStore: boolean;
  enableRunesTabsStore: boolean;
  enableRunesStoreShadowParity: boolean;
  authTokenExpiry: number;
}

export class ConfigurationManager {
  private config: AppConfig;
  private static instance: ConfigurationManager | null = null;

  private constructor() {
    this.config = this.loadConfig();
  }

  /**
   * Get the singleton instance of ConfigurationManager
   */
  static getInstance(): ConfigurationManager {
    if (!ConfigurationManager.instance) {
      ConfigurationManager.instance = new ConfigurationManager();
    }
    return ConfigurationManager.instance;
  }

  /**
   * Reset the ConfigurationManager (useful for testing)
   */
  static reset(): void {
    ConfigurationManager.instance = null;
  }

  /**
   * Get the full configuration object
   */
  getConfig(): Readonly<AppConfig> {
    return Object.freeze({ ...this.config });
  }

  /**
   * Get a specific configuration value
   */
  get<K extends keyof AppConfig>(key: K): AppConfig[K] {
    return this.config[key];
  }

  /**
   * Get the API URL with version prefix
   */
  getApiEndpoint(path: string): string {
    const base = this.config.apiUrl.replace(/\/$/, '');
    const version = this.config.apiVersion;
    const cleanPath = path.startsWith('/') ? path : `/${path}`;
    return `${base}/api/${version}${cleanPath}`;
  }

  /**
   * Get the base API URL (without version)
   */
  getBaseApiUrl(): string {
    return `${this.config.apiUrl.replace(/\/$/, '')}/api`;
  }

  /**
   * Check if debug mode is enabled
   */
  isDebugEnabled(): boolean {
    return this.config.enableDebug;
  }

  /**
   * Check if analytics is enabled
   */
  isAnalyticsEnabled(): boolean {
    return this.config.enableAnalytics;
  }

  /**
   * Check if AI features are enabled
   */
  isAiFeaturesEnabled(): boolean {
    return this.config.enableAiFeatures;
  }

  /**
   * Check if artifact maintenance actions are enabled
   */
  isArtifactMaintenanceEnabled(): boolean {
    return this.config.enableArtifactMaintenance;
  }

  /**
   * Check if project integrations UI is enabled
   */
  isProjectIntegrationsEnabled(): boolean {
    return this.config.enableProjectIntegrations;
  }

  /**
   * Check if spec supplementary tabs UI is enabled
   */
  isSpecSupplementaryTabsEnabled(): boolean {
    return this.config.enableSpecSupplementaryTabs;
  }

  /**
   * Check if System Config > Authentication section is enabled
   */
  isSystemConfigAuthenticationEnabled(): boolean {
    return this.config.enableSystemConfigAuthentication;
  }

  /**
   * Check if System Config > Performance section is enabled
   */
  isSystemConfigPerformanceEnabled(): boolean {
    return this.config.enableSystemConfigPerformance;
  }

  /**
   * Check if runes implementation is enabled for toast.store
   */
  isRunesToastStoreEnabled(): boolean {
    return this.config.enableRunesToastStore;
  }

  /**
   * Check if runes implementation is enabled for panel.store
   */
  isRunesPanelStoreEnabled(): boolean {
    return this.config.enableRunesPanelStore;
  }

  /**
   * Check if runes implementation is enabled for tab-state.store
   */
  isRunesTabStateStoreEnabled(): boolean {
    return this.config.enableRunesTabStateStore;
  }

  /**
   * Check if runes implementation is enabled for command.store
   */
  isRunesCommandStoreEnabled(): boolean {
    return this.config.enableRunesCommandStore;
  }

  /**
   * Check if runes implementation is enabled for menu.store
   */
  isRunesMenuStoreEnabled(): boolean {
    return this.config.enableRunesMenuStore;
  }

  /**
   * Check if runes implementation is enabled for side-panel.store
   */
  isRunesSidePanelStoreEnabled(): boolean {
    return this.config.enableRunesSidePanelStore;
  }

  /**
   * Check if runes implementation is enabled for panel-content.store
   */
  isRunesPanelContentStoreEnabled(): boolean {
    return this.config.enableRunesPanelContentStore;
  }

  /**
   * Check if runes implementation is enabled for tabs.store
   */
  isRunesTabsStoreEnabled(): boolean {
    return this.config.enableRunesTabsStore;
  }

  /**
   * Check if shadow parity validation is enabled for runes store migration
   */
  isRunesStoreShadowParityEnabled(): boolean {
    return this.config.enableRunesStoreShadowParity;
  }

  /**
   * Check if running in development mode
   */
  isDevelopment(): boolean {
    return import.meta.env.DEV;
  }

  /**
   * Check if running in production mode
   */
  isProduction(): boolean {
    return import.meta.env.PROD;
  }

  private loadConfig(): AppConfig {
    return {
      apiUrl: this.getEnvString('VITE_API_URL', 'http://localhost:5001'),
      apiVersion: this.getEnvString('VITE_API_VERSION', 'v1'),
      enableDebug: this.getEnvBoolean('VITE_ENABLE_DEBUG', false),
      enableAnalytics: this.getEnvBoolean('VITE_ENABLE_ANALYTICS', false),
      enableAiFeatures: this.getEnvBoolean('VITE_ENABLE_AI_FEATURES', false),
      enableArtifactMaintenance: this.getEnvBoolean('VITE_ENABLE_ARTIFACT_MAINTENANCE', false),
      enableProjectIntegrations: this.getEnvBoolean('VITE_ENABLE_PROJECT_INTEGRATIONS', false),
      enableSpecSupplementaryTabs: this.getEnvBoolean('VITE_ENABLE_SPEC_SUPPLEMENTARY_TABS', false),
      enableSystemConfigAuthentication: this.getEnvBoolean('VITE_ENABLE_SYSTEM_CONFIG_AUTHENTICATION', false),
      enableSystemConfigPerformance: this.getEnvBoolean('VITE_ENABLE_SYSTEM_CONFIG_PERFORMANCE', false),
      enableRunesToastStore: this.getEnvBoolean('VITE_ENABLE_RUNES_TOAST_STORE', false),
      enableRunesPanelStore: this.getEnvBoolean('VITE_ENABLE_RUNES_PANEL_STORE', false),
      enableRunesTabStateStore: this.getEnvBoolean('VITE_ENABLE_RUNES_TAB_STATE_STORE', false),
      enableRunesCommandStore: this.getEnvBoolean('VITE_ENABLE_RUNES_COMMAND_STORE', false),
      enableRunesMenuStore: this.getEnvBoolean('VITE_ENABLE_RUNES_MENU_STORE', false),
      enableRunesSidePanelStore: this.getEnvBoolean('VITE_ENABLE_RUNES_SIDE_PANEL_STORE', false),
      enableRunesPanelContentStore: this.getEnvBoolean('VITE_ENABLE_RUNES_PANEL_CONTENT_STORE', false),
      enableRunesTabsStore: this.getEnvBoolean('VITE_ENABLE_RUNES_TABS_STORE', false),
      enableRunesStoreShadowParity: this.getEnvBoolean('VITE_ENABLE_RUNES_STORE_SHADOW_PARITY', true),
      authTokenExpiry: this.getEnvNumber('VITE_AUTH_TOKEN_EXPIRY', 900),
    };
  }

  private getEnvString(key: string, defaultValue: string): string {
    const value = import.meta.env[key];
    return typeof value === 'string' ? value : defaultValue;
  }

  private getEnvBoolean(key: string, defaultValue: boolean): boolean {
    const value = import.meta.env[key];
    if (typeof value === 'string') {
      return value.toLowerCase() === 'true' || value === '1';
    }
    return defaultValue;
  }

  private getEnvNumber(key: string, defaultValue: number): number {
    const value = import.meta.env[key];
    if (typeof value === 'string') {
      const parsed = parseInt(value, 10);
      return isNaN(parsed) ? defaultValue : parsed;
    }
    return defaultValue;
  }
}

// Export a default instance
export const config = ConfigurationManager.getInstance();
