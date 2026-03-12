/**
 * DashboardHubService - SignalR Client for Dashboard
 * Handles real-time dashboard updates (statistics, activities)
 */

import * as signalR from '@microsoft/signalr';
import { config } from '@sddp/shell/core';
import { reportWarning, reportError, reportInfo, showProblemsPanel } from '@sddp/shell/utils';
import { problems } from '@sddp/shell/stores';

// ============================================
// Types
// ============================================

export interface SpecStatusChangedPayload {
  specId: string;
  tenantId: string;
  projectId: string;
  fromStatus: string;
  toStatus: string;
  actorId: string;
  reason?: string;
  timestamp: string;
}

export interface NotificationPayload {
  id: string;
  type: string;
  title: string;
  message: string;
  isRead: boolean;
  entityType?: string;
  entityId?: string;
  actorName?: string;
  createdAt: string;
}

export interface DashboardHubCallbacks {
  onStatisticUpdated?: (payload: unknown) => void;
  onActivityUpdated?: (payload: unknown) => void;
  onSpecStatusChanged?: (payload: SpecStatusChangedPayload) => void;
  onNewNotification?: (payload: NotificationPayload) => void;
  onConnectionStateChange?: (connected: boolean) => void;
  onError?: (error: Error) => void;
  /** Called when onclose fires (all reconnect attempts exhausted) */
  onFinalDisconnect?: (error?: Error) => void;
}

// ============================================
// DashboardHubService
// ============================================

export class DashboardHubService {
  private connection: signalR.HubConnection | null = null;
  private currentView: string | null = null;
  private currentProjectId: string | null = null;
  private callbacks: DashboardHubCallbacks = {};
  private maxReconnectAttempts = 5;
  private isConnecting = false;

  /**
   * Get the hub URL
   */
  private getHubUrl(): string {
    const baseUrl = config.get('apiUrl').replace(/\/$/, '');
    return `${baseUrl}/hubs/dashboard`;
  }

  /**
   * Build the SignalR connection
   */
  private buildConnection(): signalR.HubConnection {
    return new signalR.HubConnectionBuilder()
      .withUrl(this.getHubUrl(), {
        // Hub auth uses HttpOnly cookie set by auth endpoints.
        withCredentials: true,
      })
      .withAutomaticReconnect({
        nextRetryDelayInMilliseconds: (retryContext: signalR.RetryContext): number | null => {
          if (retryContext.previousRetryCount >= this.maxReconnectAttempts) {
            return null;
          }
          return Math.min(1000 * Math.pow(2, retryContext.previousRetryCount), 30000);
        },
      })
      .configureLogging(config.isDebugEnabled() ? signalR.LogLevel.Debug : signalR.LogLevel.Warning)
      .build();
  }

  /**
   * Setup event handlers for the connection
   */
  private setupEventHandlers(): void {
    if (!this.connection) return;

    this.connection.on('StatisticUpdated', (payload: unknown) => {
      this.callbacks.onStatisticUpdated?.(payload);
    });

    this.connection.on('ActivityUpdated', (payload: unknown) => {
      this.callbacks.onActivityUpdated?.(payload);
    });

    this.connection.on('SpecStatusChanged', (payload: SpecStatusChangedPayload) => {
      this.callbacks.onSpecStatusChanged?.(payload);
    });

    this.connection.on('NewNotification', (payload: NotificationPayload) => {
      this.callbacks.onNewNotification?.(payload);
    });

    this.connection.onreconnecting((_error: Error | undefined) => {
      this.callbacks.onConnectionStateChange?.(false);

      // Report to problems panel
      problems.clearBySource('SignalR');
      reportWarning('Connection lost - reconnecting...', { source: 'SignalR', code: 'SIGNALR_RECONNECTING' });
      showProblemsPanel();
    });

    this.connection.onreconnected(() => {
      this.callbacks.onConnectionStateChange?.(true);

      // Update problems panel: warning → info
      problems.clearBySource('SignalR');
      reportInfo('Connection restored.', { source: 'SignalR', code: 'SIGNALR_RECONNECTED' });

      // Resubscribe after reconnection
      if (this.currentView) {
        this.subscribeDashboard(this.currentView, this.currentProjectId ?? undefined).catch(
          console.error
        );
      }
    });

    this.connection.onclose((error: Error | undefined) => {
      this.callbacks.onConnectionStateChange?.(false);

      if (error) {
        this.callbacks.onError?.(error);

        // Report to problems panel
        problems.clearBySource('SignalR');
        reportError(`Server connection closed: ${error.message}`, { source: 'SignalR', code: 'SIGNALR_CLOSED' });
        showProblemsPanel();
      }

      // Signal that all reconnect attempts are exhausted
      this.callbacks.onFinalDisconnect?.(error);
    });
  }

  /**
   * Set callbacks for hub events
   */
  setCallbacks(callbacks: DashboardHubCallbacks): void {
    this.callbacks = { ...this.callbacks, ...callbacks };
  }

  /**
   * Connect to the SignalR hub
   */
  async connect(): Promise<void> {
    if (
      this.connection?.state === signalR.HubConnectionState.Connected ||
      this.connection?.state === signalR.HubConnectionState.Connecting ||
      this.connection?.state === signalR.HubConnectionState.Reconnecting
    ) {
      return;
    }

    if (this.isConnecting) {
      return;
    }

    this.isConnecting = true;

    try {
      if (!this.connection || this.connection.state === signalR.HubConnectionState.Disconnected) {
        this.connection = this.buildConnection();
        this.setupEventHandlers();
      }

      await this.connection.start();
      this.callbacks.onConnectionStateChange?.(true);
    } catch (error) {
      const err = error instanceof Error ? error : new Error('Connection failed');
      this.callbacks.onError?.(err);
      throw err;
    } finally {
      this.isConnecting = false;
    }
  }

  /**
   * Disconnect from the SignalR hub
   */
  async disconnect(): Promise<void> {
    if (this.currentView) {
      await this.unsubscribeDashboard().catch(() => {
        // Ignore errors when unsubscribing during disconnect
      });
    }

    if (this.connection) {
      await this.connection.stop();
      this.connection = null;
    }

    this.currentView = null;
    this.currentProjectId = null;
  }

  /**
   * Subscribe to a dashboard view
   */
  async subscribeDashboard(view: string, projectId?: string): Promise<void> {
    if (!this.connection || this.connection.state !== signalR.HubConnectionState.Connected) {
      throw new Error('Not connected to hub');
    }

    // Unsubscribe from current if different
    if (this.currentView && (this.currentView !== view || this.currentProjectId !== (projectId ?? null))) {
      await this.unsubscribeDashboard();
    }

    await this.connection.invoke('SubscribeDashboard', view, projectId ?? null);
    this.currentView = view;
    this.currentProjectId = projectId ?? null;
  }

  /**
   * Unsubscribe from the current dashboard view
   */
  async unsubscribeDashboard(): Promise<void> {
    if (!this.connection || this.connection.state !== signalR.HubConnectionState.Connected) {
      return;
    }

    if (this.currentView) {
      await this.connection.invoke(
        'UnsubscribeDashboard',
        this.currentView,
        this.currentProjectId ?? null
      );
      this.currentView = null;
      this.currentProjectId = null;
    }
  }

  /**
   * Get current connection state
   */
  getConnectionState(): signalR.HubConnectionState {
    return this.connection?.state ?? signalR.HubConnectionState.Disconnected;
  }

  /**
   * Check if connected
   */
  isConnected(): boolean {
    return this.connection?.state === signalR.HubConnectionState.Connected;
  }
}

// ============================================
// Singleton
// ============================================

let hubServiceInstance: DashboardHubService | null = null;

/**
 * Get the singleton DashboardHubService instance
 */
export function getDashboardHubService(): DashboardHubService {
  if (!hubServiceInstance) {
    hubServiceInstance = new DashboardHubService();
  }
  return hubServiceInstance;
}

/**
 * Reset the singleton instance (for testing)
 */
export function resetDashboardHubService(): void {
  if (hubServiceInstance) {
    hubServiceInstance.disconnect().catch(() => {
      // Ignore errors during reset
    });
  }
  hubServiceInstance = null;
}
