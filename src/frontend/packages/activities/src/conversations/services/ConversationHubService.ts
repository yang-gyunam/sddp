/**
 * ConversationHubService - SignalR Client for Conversations
 * Handles real-time communication with the backend
 */

import * as signalR from '@microsoft/signalr';
import { config } from '@sddp/shell/core';
import { reportWarning, reportError, reportInfo, showProblemsPanel } from '@sddp/shell/utils';
import {
  addMessage,
  updateMessage,
  removeMessage,
  setUserTyping,
  addOnlineUser,
  removeOnlineUser,
  setOnlineUsers,
  setHubConnected,
  setHubConnectionError,
  setDMOnlineStatus,
  removeDirectMessage,
  getDirectMessages,
} from '../stores';
import type {
  Message,
  UserJoinedEvent,
  UserLeftEvent,
  UserTypingEvent,
  ConversationClosedEvent,
  DMConcludedEvent,
  MemberJoinedEvent,
  MemberRemovedEvent,
  ProjectChannelCreatedEvent,
  ConversationInvitationEvent,
  InvitationResponseEvent,
  UserOnlineEvent,
  UserOfflineEvent,
  PresenceStateEvent,
} from '../types';

// ============================================
// Types
// ============================================

export interface ConversationHubCallbacks {
  onNewMessage?: (message: Message) => void;
  onMessageEdited?: (message: Message) => void;
  onMessageDeleted?: (messageId: string) => void;
  onUserJoined?: (event: UserJoinedEvent) => void;
  onUserLeft?: (event: UserLeftEvent) => void;
  onUserTyping?: (event: UserTypingEvent) => void;
  onConversationClosed?: (event: ConversationClosedEvent) => void;
  onDMConcluded?: (event: DMConcludedEvent) => void;
  onMemberJoined?: (event: MemberJoinedEvent) => void;
  onMemberRemoved?: (event: MemberRemovedEvent) => void;
  onProjectChannelCreated?: (event: ProjectChannelCreatedEvent) => void;
  onConversationInvitation?: (event: ConversationInvitationEvent) => void;
  onInvitationResponse?: (event: InvitationResponseEvent) => void;
  onUserOnline?: (event: UserOnlineEvent) => void;
  onUserOffline?: (event: UserOfflineEvent) => void;
  onPresenceState?: (event: PresenceStateEvent) => void;
  onConnectionStateChange?: (connected: boolean) => void;
  onError?: (error: Error) => void;
  /** Called when onclose fires (all reconnect attempts exhausted) */
  onFinalDisconnect?: (error?: Error) => void;
}

// ============================================
// ConversationHubService
// ============================================

export class ConversationHubService {
  private connection: signalR.HubConnection | null = null;
  private currentConversationId: string | null = null;
  private callbackSets: ConversationHubCallbacks[] = [];
  private maxReconnectAttempts = 5;
  private isConnecting = false;

  /**
   * Get the hub URL
   */
  private getHubUrl(): string {
    const baseUrl = config.get('apiUrl').replace(/\/$/, '');
    return `${baseUrl}/hubs/conversation`;
  }

  /**
   * Build the SignalR connection
   */
  private buildConnection(): signalR.HubConnection {
    return new signalR.HubConnectionBuilder()
      .withUrl(this.getHubUrl(), {
        // Hub auth uses HttpOnly cookie set by auth endpoints.
        withCredentials: true,
        // Allow SignalR to negotiate and choose the best transport
        // (WebSockets, Server-Sent Events, or Long Polling)
      })
      .withAutomaticReconnect({
        nextRetryDelayInMilliseconds: (retryContext: signalR.RetryContext): number | null => {
          // Exponential backoff: 0, 2, 4, 8, 16, 32 seconds
          if (retryContext.previousRetryCount >= this.maxReconnectAttempts) {
            return null; // Stop reconnecting
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

    // New message received (backend sends MessageDto with conversationId)
    this.connection.on('NewMessage', (raw: Record<string, unknown>) => {
      const message: Message = {
        ...(raw as unknown as Message),
        conversationId: (raw.conversationId as string) ?? '',
        topicId: (raw.topicId as string) ?? undefined,
      };
      addMessage(message);
      this.invokeCallbacks('onNewMessage', message);
      if (typeof window !== 'undefined') {
        window.dispatchEvent(new CustomEvent('sddp:new-message', { detail: message }));
      }
    });

    // Message edited
    this.connection.on('MessageEdited', (message: Message) => {
      updateMessage(message.id, message);
      this.invokeCallbacks('onMessageEdited', message);
    });

    // Message deleted
    this.connection.on('MessageDeleted', (messageId: string) => {
      removeMessage(messageId);
      this.invokeCallbacks('onMessageDeleted', messageId);
    });

    // User joined conversation
    this.connection.on('UserJoined', (event: UserJoinedEvent) => {
      addOnlineUser(event.userId);
      this.invokeCallbacks('onUserJoined', event);
    });

    // User left conversation
    this.connection.on('UserLeft', (event: UserLeftEvent) => {
      removeOnlineUser(event.userId);
      this.invokeCallbacks('onUserLeft', event);
    });

    // User typing status
    this.connection.on('UserTyping', (event: UserTypingEvent) => {
      setUserTyping(event.userId, '', event.isTyping); // Display name will be resolved elsewhere
      this.invokeCallbacks('onUserTyping', event);
    });

    // Conversation closed
    this.connection.on('ConversationClosed', (event: ConversationClosedEvent) => {
      this.invokeCallbacks('onConversationClosed', event);
    });

    // DM concluded (user-targeted, removes from sidebar)
    this.connection.on('DMConcluded', (event: DMConcludedEvent) => {
      removeDirectMessage(event.conversationId);
      this.invokeCallbacks('onDMConcluded', event);
    });

    // Member joined conversation (auto-participation)
    this.connection.on('MemberJoined', (event: MemberJoinedEvent) => {
      this.invokeCallbacks('onMemberJoined', event);
    });

    // Member removed from conversation
    this.connection.on('MemberRemoved', (event: MemberRemovedEvent) => {
      this.invokeCallbacks('onMemberRemoved', event);
    });

    // Project-scoped channel created (user-targeted)
    this.connection.on('ProjectChannelCreated', (event: ProjectChannelCreatedEvent) => {
      this.invokeCallbacks('onProjectChannelCreated', event);
    });

    // Conversation invitation received
    this.connection.on('ConversationInvitation', (event: ConversationInvitationEvent) => {
      this.invokeCallbacks('onConversationInvitation', event);
    });

    // Invitation response received (for the inviter)
    this.connection.on('InvitationResponse', (event: InvitationResponseEvent) => {
      this.invokeCallbacks('onInvitationResponse', event);
    });

    // User came online (presence)
    this.connection.on('UserOnline', (event: UserOnlineEvent) => {
      addOnlineUser(event.userId);
      setDMOnlineStatus(event.userId, true);
      this.invokeCallbacks('onUserOnline', event);
    });

    // User went offline (presence)
    this.connection.on('UserOffline', (event: UserOfflineEvent) => {
      removeOnlineUser(event.userId);
      setDMOnlineStatus(event.userId, false);
      this.invokeCallbacks('onUserOffline', event);
    });

    // Initial presence state (sent on connect/reconnect)
    this.connection.on('PresenceState', (event: PresenceStateEvent) => {
      setOnlineUsers(event.onlineUserIds);
      // Update DM sidebar online status
      const dms = getDirectMessages();
      for (const dm of dms) {
        setDMOnlineStatus(dm.participantId, event.onlineUserIds.includes(dm.participantId));
      }
      this.invokeCallbacks('onPresenceState', event);
    });

    // Connection lifecycle events
    this.connection.onreconnecting((_error: Error | undefined) => {
      setHubConnected(false);
      this.invokeCallbacks('onConnectionStateChange', false);

      // Report to problems panel (accumulate, don't clear)
      reportWarning('Connection lost - reconnecting...', { source: 'SignalR', code: 'SIGNALR_RECONNECTING' });
      showProblemsPanel();
    });

    this.connection.onreconnected((connectionId: string | undefined) => {
      void connectionId;
      setHubConnected(true);
      this.invokeCallbacks('onConnectionStateChange', true);

      // Report to problems panel (accumulate, don't clear)
      reportInfo('Connection restored.', { source: 'SignalR', code: 'SIGNALR_RECONNECTED' });

      // Refresh presence state after reconnect
      this.connection?.invoke('GetOnlineUsers').catch(console.error);

      // Rejoin the conversation if we were in one
      if (this.currentConversationId) {
        this.joinConversation(this.currentConversationId).catch(console.error);
      }
    });

    this.connection.onclose((error: Error | undefined) => {
      setHubConnected(false);
      this.invokeCallbacks('onConnectionStateChange', false);

      if (error) {
        setHubConnectionError(error.message);
        this.invokeCallbacks('onError', error);

        // Report to problems panel (accumulate, don't clear)
        reportError(`Server connection closed: ${error.message}`, { source: 'SignalR', code: 'SIGNALR_CLOSED' });
        showProblemsPanel();
      }

      // Signal that all reconnect attempts are exhausted
      this.invokeCallbacks('onFinalDisconnect', error);
    });
  }

  /**
   * Invoke all registered callbacks for a given event
   */
  private invokeCallbacks<K extends keyof ConversationHubCallbacks>(
    event: K,
    ...args: Parameters<NonNullable<ConversationHubCallbacks[K]>>
  ): void {
    for (const callbacks of this.callbackSets) {
      const handler = callbacks[event];
      if (handler) {
        (handler as (...a: unknown[]) => void)(...args);
      }
    }
  }

  /**
   * Register callbacks for hub events.
   * Returns an unsubscribe function that removes these callbacks.
   */
  setCallbacks(callbacks: ConversationHubCallbacks): () => void {
    this.callbackSets.push(callbacks);
    return () => {
      const index = this.callbackSets.indexOf(callbacks);
      if (index !== -1) this.callbackSets.splice(index, 1);
    };
  }

  /**
   * Connect to the SignalR hub
   */
  async connect(): Promise<void> {
    // Already connected or connecting - skip
    if (this.connection?.state === signalR.HubConnectionState.Connected ||
        this.connection?.state === signalR.HubConnectionState.Connecting ||
        this.connection?.state === signalR.HubConnectionState.Reconnecting) {
      return;
    }

    if (this.isConnecting) {
      return; // Connection in progress
    }

    this.isConnecting = true;

    try {
      // Create new connection if needed
      if (!this.connection || this.connection.state === signalR.HubConnectionState.Disconnected) {
        this.connection = this.buildConnection();
        this.setupEventHandlers();
      }

      await this.connection.start();
      setHubConnected(true);
      this.invokeCallbacks('onConnectionStateChange', true);
    } catch (error) {
      const err = error instanceof Error ? error : new Error('Connection failed');
      setHubConnectionError(err.message);
      this.invokeCallbacks('onError', err);
      throw err;
    } finally {
      this.isConnecting = false;
    }
  }

  /**
   * Disconnect from the SignalR hub
   */
  async disconnect(): Promise<void> {
    if (this.currentConversationId) {
      await this.leaveConversation(this.currentConversationId).catch(() => {
        // Ignore errors when leaving during disconnect
      });
    }

    if (this.connection) {
      await this.connection.stop();
      this.connection = null;
    }

    setHubConnected(false);
    this.currentConversationId = null;
  }

  /**
   * Join a conversation
   */
  async joinConversation(conversationId: string): Promise<void> {
    if (!this.connection || this.connection.state !== signalR.HubConnectionState.Connected) {
      throw new Error('Not connected to hub');
    }

    // Leave current conversation if different
    if (this.currentConversationId && this.currentConversationId !== conversationId) {
      await this.leaveConversation(this.currentConversationId);
    }

    await this.connection.invoke('JoinConversation', conversationId);
    this.currentConversationId = conversationId;
  }

  /**
   * Leave a conversation
   */
  async leaveConversation(conversationId: string): Promise<void> {
    if (!this.connection || this.connection.state !== signalR.HubConnectionState.Connected) {
      return; // Not connected, nothing to do
    }

    await this.connection.invoke('LeaveConversation', conversationId);

    if (this.currentConversationId === conversationId) {
      this.currentConversationId = null;
    }
  }

  /**
   * Send typing start notification
   */
  async startTyping(conversationId: string): Promise<void> {
    if (!this.connection || this.connection.state !== signalR.HubConnectionState.Connected) {
      return;
    }

    await this.connection.invoke('StartTyping', conversationId);
  }

  /**
   * Send typing stop notification
   */
  async stopTyping(conversationId: string): Promise<void> {
    if (!this.connection || this.connection.state !== signalR.HubConnectionState.Connected) {
      return;
    }

    await this.connection.invoke('StopTyping', conversationId);
  }

  /**
   * Send conversation invitation to a user
   */
  async sendInvitation(conversationId: string, targetUserId: string, conversationName: string): Promise<void> {
    if (!this.connection || this.connection.state !== signalR.HubConnectionState.Connected) {
      throw new Error('Not connected to hub');
    }
    await this.connection.invoke('SendInvitation', conversationId, targetUserId, conversationName);
  }

  /**
   * Respond to a conversation invitation
   */
  async respondToInvitation(conversationId: string, inviterUserId: string, accepted: boolean): Promise<void> {
    if (!this.connection || this.connection.state !== signalR.HubConnectionState.Connected) {
      throw new Error('Not connected to hub');
    }
    await this.connection.invoke('RespondToInvitation', conversationId, inviterUserId, accepted);
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

  /**
   * Get current conversation ID
   */
  getCurrentConversationId(): string | null {
    return this.currentConversationId;
  }
}

// ============================================
// Singleton
// ============================================

let hubServiceInstance: ConversationHubService | null = null;

/**
 * Get the singleton ConversationHubService instance
 */
export function getConversationHubService(): ConversationHubService {
  if (!hubServiceInstance) {
    hubServiceInstance = new ConversationHubService();
  }
  return hubServiceInstance;
}

/**
 * Reset the singleton instance (for testing)
 */
export function resetConversationHubService(): void {
  if (hubServiceInstance) {
    hubServiceInstance.disconnect().catch(() => {
      // Ignore errors during reset
    });
  }
  hubServiceInstance = null;
}
