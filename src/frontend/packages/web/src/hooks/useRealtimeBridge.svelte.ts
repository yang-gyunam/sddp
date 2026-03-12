/**
 * useRealtimeBridge — Phase 3
 * Manages SignalR hub lifecycle (DashboardHub + ConversationHub) + polling fallback
 */

import { get } from 'svelte/store';
import { toast, startPollingFallback, stopPollingFallback, getConnectionMode } from '@sddp/shell';
import { tabActions, tabsStore } from '@sddp/shell/stores';
import { authStore, getAuthState } from '@sddp/shell/auth/stores';
import {
  getConversationService,
  getConversationHubService,
  removeConversation,
  loadGlobalConversationsSidebar,
  getSidebarState,
  removeDirectMessage,
  type ConversationEntry,
} from '@sddp/activities/conversations';
import type {
  ConversationInvitationEvent,
  InvitationResponseEvent,
} from '@sddp/activities/conversations/types';
import {
  setPendingEntityId,
  mapAuditLogDtoToTimelineEvent,
  addTimelineEvent,
  type ProjectPageType,
  type AuditLogDto,
} from '@sddp/activities/projects';
import {
  getDashboardHubService,
  addNotification as addNotificationToStore,
  getMyNotifications,
  setNotifications,
} from '@sddp/activities/dashboard';
import { destroyPollingFallback } from '@sddp/shell';

type ProjectConversationSelectDetail = {
  projectId: string;
  conversationId: string;
  conversationKind?: 'channel' | 'forum' | 'dm';
  participantName?: string;
  channelStatus?: 'Active' | 'Concluded' | null;
};

export interface RealtimeBridgeParams {
  onConversationSelect: (conv: ConversationEntry) => void;
  onDirectMessageSelect: (item: {
    id: string;
    name: string;
    channelStatus?: 'Active' | 'Concluded' | null;
    projectId?: string;
  }) => void;
  onProjectNodeSelect: (projectId: string, nodeType: ProjectPageType) => void;
  onLoadChannels: (projectId: string) => Promise<void>;
  getSelectedProjectId: () => string | null;
}

export function useRealtimeBridge(params: RealtimeBridgeParams) {
  let dashboardHubDisconnected = false;
  let conversationHubDisconnected = false;
  let dashboardHubCleanup: (() => void) | null = null;
  let conversationHubCleanup: (() => void) | null = null;

  // --- Helper functions ---

  function parseDmProjectId(entityType?: string | null): string | null {
    if (!entityType) return null;
    const prefixes = ['dm:', 'direct_message:'];
    const matchedPrefix = prefixes.find((prefix) => entityType.startsWith(prefix));
    if (!matchedPrefix) return null;
    const candidate = entityType.slice(matchedPrefix.length).trim();
    return candidate.length > 0 ? candidate : null;
  }

  function dispatchProjectConversationSelect(
    projectId: string,
    conversationId: string,
    options?: {
      conversationKind?: 'channel' | 'forum' | 'dm';
      participantName?: string;
      channelStatus?: 'Active' | 'Concluded' | null;
    }
  ): void {
    if (typeof window === 'undefined') return;
    window.dispatchEvent(
      new CustomEvent<ProjectConversationSelectDetail>('sddp:project-conversation-select', {
        detail: {
          projectId,
          conversationId,
          conversationKind: options?.conversationKind ?? 'dm',
          participantName: options?.participantName,
          channelStatus: options?.channelStatus ?? null,
        },
      })
    );
  }

  function openProjectDirectMessageByContext(
    conversationId: string,
    projectId: string,
    options?: {
      participantName?: string;
      channelStatus?: 'Active' | 'Concluded' | null;
    }
  ): boolean {
    const normalizedProjectId = projectId.trim();
    if (!normalizedProjectId) return false;

    setPendingEntityId(conversationId);
    dispatchProjectConversationSelect(normalizedProjectId, conversationId, {
      ...options,
      conversationKind: 'dm',
    });
    params.onProjectNodeSelect(normalizedProjectId, 'conversations');
    return true;
  }

  function openProjectConversationByContext(
    conversationId: string,
    projectId: string,
    options?: {
      conversationKind?: 'channel' | 'forum';
      channelStatus?: 'Active' | 'Concluded' | null;
    }
  ): boolean {
    const normalizedProjectId = projectId.trim();
    if (!normalizedProjectId) return false;

    setPendingEntityId(conversationId);
    dispatchProjectConversationSelect(normalizedProjectId, conversationId, {
      conversationKind: options?.conversationKind ?? 'channel',
      channelStatus: options?.channelStatus ?? null,
    });
    params.onProjectNodeSelect(normalizedProjectId, 'conversations');
    return true;
  }

  async function syncDirectMessages(_conversationId?: string): Promise<void> {
    const tid = authStore.get().user?.tenantId;
    if (tid) await loadGlobalConversationsSidebar(tid);
  }

  function closeDirectMessageTabs(conversationId: string): void {
    const path = `/dm/${conversationId}`;
    const state = get(tabsStore);
    for (const group of state.groups) {
      for (const tab of group.tabs) {
        if (tab.path === path) {
          tabActions.closeTab(tab.id, group.id);
        }
      }
    }
  }

  function handleDirectMessageConcludedById(conversationId: string): void {
    removeDirectMessage(conversationId);
    closeDirectMessageTabs(conversationId);
    const tid = authStore.get().user?.tenantId;
    if (tid) void loadGlobalConversationsSidebar(tid);
  }

  async function openDirectMessageByContext(
    conversationId: string,
    participantName: string,
    projectId?: string | null,
    channelStatus?: 'Active' | 'Concluded' | null
  ): Promise<void> {
    const scopedProjectId = (projectId ?? '').trim();
    if (
      scopedProjectId &&
      openProjectDirectMessageByContext(conversationId, scopedProjectId, {
        participantName,
        channelStatus: channelStatus ?? 'Active',
      })
    ) {
      return;
    }

    await syncDirectMessages(conversationId);
    const sidebarDMs = getSidebarState().directMessages;
    const dm = sidebarDMs.find((d) => d.id === conversationId);
    params.onDirectMessageSelect({
      id: conversationId,
      name: dm?.participantName ?? participantName,
      channelStatus: dm?.channelStatus ?? channelStatus ?? null,
      projectId: '',
    });
  }

  async function acceptDmInvitation(
    notificationId: string,
    actorName?: string,
    projectScopeId?: string | null
  ): Promise<void> {
    const tenantId = getAuthState().user?.tenantId;
    if (!tenantId) {
      toast.error('No tenant context available');
      return;
    }

    const projectId = (projectScopeId ?? '').trim();
    try {
      const service = getConversationService();
      service.setContext(tenantId, projectId);
      const dmConversation = await service.acceptDMInvitation(notificationId);

      toast.success('Direct message invitation accepted');
      await openDirectMessageByContext(
        dmConversation.id,
        actorName ?? dmConversation.name ?? 'Direct Message',
        dmConversation.projectId ?? projectId,
        dmConversation.channelStatus ?? 'Active'
      );
    } catch (error) {
      const message = error instanceof Error ? error.message : 'Failed to accept invitation';
      toast.error(message);
      throw error;
    }
  }

  async function rejectDmInvitation(
    notificationId: string,
    projectScopeId?: string | null
  ): Promise<void> {
    const tenantId = getAuthState().user?.tenantId;
    if (!tenantId) {
      toast.error('No tenant context available');
      return;
    }

    const projectId = (projectScopeId ?? '').trim();
    try {
      const service = getConversationService();
      service.setContext(tenantId, projectId);
      await service.rejectDMInvitation(notificationId);
      toast.info('Direct message invitation declined');
    } catch (error) {
      const message = error instanceof Error ? error.message : 'Failed to reject invitation';
      toast.error(message);
      throw error;
    }
  }

  // --- Polling fallback ---

  function maybeStartPollingFallback() {
    if (!dashboardHubDisconnected && !conversationHubDisconnected) return;
    if (getConnectionMode() === 'polling') return;

    const tenantId = getAuthState().user?.tenantId;
    if (!tenantId) return;

    startPollingFallback({
      pollingIntervalMs: 30_000,
      reconnectRetryIntervalMs: 300_000,
      onPoll: async () => {
        try {
          const data = await getMyNotifications(tenantId, 1, 50);
          setNotifications(data.notifications, data.unreadCount);
        } catch {
          // Polling is best-effort
        }
      },
      onReconnectAttempt: async () => {
        const dashboardHub = getDashboardHubService();
        const conversationHub = getConversationHubService();
        let anySuccess = false;

        if (dashboardHubDisconnected) {
          try {
            await dashboardHub.connect();
            await dashboardHub.subscribeDashboard('my');
            dashboardHubDisconnected = false;
            anySuccess = true;
          } catch {
            /* keep polling */
          }
        }

        if (conversationHubDisconnected) {
          try {
            await conversationHub.connect();
            conversationHubDisconnected = false;
            anySuccess = true;
          } catch {
            /* keep polling */
          }
        }

        return anySuccess;
      },
    });
  }

  // --- Hub connections ---

  function connectDashboardHub() {
    const hubService = getDashboardHubService();
    hubService.setCallbacks({
      onActivityUpdated: (payload) => {
        const dto = payload as AuditLogDto;
        if (dto && dto.id) {
          const event = mapAuditLogDtoToTimelineEvent(dto, '');
          addTimelineEvent(event);
        }
      },
      onNewNotification: (payload) => {
        if (payload && payload.id) {
          addNotificationToStore({
            id: payload.id,
            type: payload.type,
            title: payload.title,
            message: payload.message,
            isRead: payload.isRead,
            entityType: payload.entityType,
            entityId: payload.entityId,
            actorName: payload.actorName,
            createdAt: payload.createdAt,
          });

          if (payload.type === 'dm_invite') {
            toast.info(payload.message, {
              title: 'Direct Message Invitation',
              duration: 0,
              closable: true,
              actions: [
                {
                  label: 'Accept',
                  variant: 'primary',
                  dismissOnClick: true,
                  onClick: () =>
                    acceptDmInvitation(payload.id, payload.actorName, payload.entityId),
                },
                {
                  label: 'Decline',
                  variant: 'ghost',
                  dismissOnClick: true,
                  onClick: () => rejectDmInvitation(payload.id, payload.entityId),
                },
              ],
            });
            return;
          }

          if (payload.type === 'dm_invite_accepted') {
            const projectId = parseDmProjectId(payload.entityType);
            const conversationId = payload.entityId;
            if (conversationId && !projectId) {
              void syncDirectMessages(conversationId);
            }
            toast.success(payload.message, {
              title: payload.title,
              duration: 0,
              actions: conversationId
                ? [
                    {
                      label: 'Open',
                      variant: 'primary',
                      onClick: () => {
                        void openDirectMessageByContext(
                          conversationId,
                          payload.actorName ?? 'Direct Message',
                          projectId,
                          'Active'
                        );
                      },
                    },
                  ]
                : undefined,
            });
            return;
          }

          if (payload.type === 'dm_invite_rejected') {
            toast.info(payload.message, {
              title: payload.title,
            });
          }
        }
      },
      onFinalDisconnect: () => {
        dashboardHubDisconnected = true;
        maybeStartPollingFallback();
      },
      onConnectionStateChange: (connected) => {
        if (connected) {
          dashboardHubDisconnected = false;
          if (!conversationHubDisconnected) {
            stopPollingFallback();
          }
        }
      },
    });
    hubService
      .connect()
      .then(() => {
        hubService.subscribeDashboard('my');
      })
      .catch((err) => {
        console.error('Failed to connect to DashboardHub:', err);
      });
    dashboardHubCleanup = () => {
      hubService
        .disconnect()
        .catch((err) => console.warn('[App] DashboardHub disconnect failed:', err));
    };
  }

  function connectConversationHub() {
    const hubService = getConversationHubService();
    hubService.setCallbacks({
      onDMConcluded: (event) => {
        handleDirectMessageConcludedById(event.conversationId);
        if (typeof window !== 'undefined') {
          window.dispatchEvent(
            new CustomEvent('sddp:dm-concluded', {
              detail: {
                conversationId: event.conversationId,
                origin: 'app-sync',
              },
            })
          );
        }
      },
      onMemberRemoved: (event) => {
        const currentUser = getAuthState().user;
        if (!currentUser || event.removedUserId !== currentUser.id) return;

        // Close any open tabs for this conversation and capture info
        const state = get(tabsStore);
        const paths = [
          `/conversation/${event.conversationId}`,
          `/dm/${event.conversationId}`,
          `/forum/${event.conversationId}`,
        ];
        let channelName = '';
        let isDm = false;
        for (const group of state.groups) {
          for (const tab of group.tabs) {
            if (tab.path && paths.includes(tab.path)) {
              channelName = channelName || tab.title;
              if (tab.path.startsWith('/dm/')) isDm = true;
              tabActions.closeTab(tab.id, group.id);
            }
          }
        }

        if (channelName) {
          toast.info(isDm ? `Left ${channelName}` : `You were removed from #${channelName}`);
        }

        // Remove from sidebar (channels + DMs) and refresh
        removeConversation(event.conversationId);
        removeDirectMessage(event.conversationId);
        const tid = currentUser.tenantId;
        if (tid) {
          loadGlobalConversationsSidebar(tid);
        }

        // Notify ProjectConversationsPage to clean up its local DM list
        if (isDm && typeof window !== 'undefined') {
          window.dispatchEvent(
            new CustomEvent('sddp:dm-concluded', {
              detail: { conversationId: event.conversationId, origin: 'member-removed' },
            })
          );
        }
      },
      onProjectChannelCreated: (event) => {
        if (event.projectId === params.getSelectedProjectId()) {
          void params.onLoadChannels(event.projectId);
        }

        if (event.createdByUserId !== getAuthState().user?.id) {
 toast.info(`#${event.channelName} channel create.`);
        }
      },
      onConversationInvitation: (event: ConversationInvitationEvent) => {
        toast.info(`${event.inviterName} invited you to #${event.conversationName}`, {
          title: 'Channel Invitation',
          duration: 0,
          closable: true,
          actions: [
            {
              label: 'Accept',
              variant: 'primary',
              dismissOnClick: true,
              onClick: async () => {
                try {
                  const hub = getConversationHubService();
                  await hub.respondToInvitation(event.conversationId, event.inviterUserId, true);
                  toast.success(`Joined #${event.conversationName}`);

                  // Refresh sidebar store so the new channel appears
                  const tid = getAuthState().user?.tenantId;
                  if (tid) await loadGlobalConversationsSidebar(tid);

                  // Open channel tab with proper component (same path as sidebar click)
                  params.onConversationSelect({
                    id: event.conversationId,
                    name: event.conversationName,
                    type: 'Channel',
                    isPrivate: false,
                    projectId: event.projectId ?? undefined,
                  });
                } catch (err) {
                  console.error('[App] Failed to accept channel invitation:', err);
                  toast.error('Failed to accept invitation. Please try again.');
                }
              },
            },
            {
              label: 'Decline',
              variant: 'ghost',
              dismissOnClick: true,
              onClick: async () => {
                try {
                  const hub = getConversationHubService();
                  await hub.respondToInvitation(event.conversationId, event.inviterUserId, false);
                } catch {
                  // Ignore errors on decline
                }
              },
            },
          ],
        });
      },
      onInvitationResponse: (event: InvitationResponseEvent) => {
        if (event.accepted) {
          toast.success(`${event.responderName} accepted the invitation`);
        } else {
          toast.info(`${event.responderName} declined the invitation`);
        }
        // Dispatch custom event so open pages can reload participants
        if (typeof window !== 'undefined') {
          window.dispatchEvent(new CustomEvent('sddp:invitation-response', { detail: event }));
        }
      },
      onFinalDisconnect: () => {
        conversationHubDisconnected = true;
        maybeStartPollingFallback();
      },
      onConnectionStateChange: (connected) => {
        if (connected) {
          conversationHubDisconnected = false;
          if (!dashboardHubDisconnected) {
            stopPollingFallback();
          }
        }
      },
    });
    hubService.connect().catch((err) => {
      console.warn('ConversationHub connection failed:', err);
    });
    conversationHubCleanup = () => {
      hubService
        .disconnect()
        .catch((err) => console.warn('[App] ConversationHub disconnect failed:', err));
    };
  }

  // --- Public API ---

  function connect() {
    connectDashboardHub();
    connectConversationHub();
  }

  function disconnect() {
    dashboardHubCleanup?.();
    dashboardHubCleanup = null;
    conversationHubCleanup?.();
    conversationHubCleanup = null;
    destroyPollingFallback();
    dashboardHubDisconnected = false;
    conversationHubDisconnected = false;
  }

  return {
    connect,
    disconnect,
    openDirectMessageByContext,
    openProjectDirectMessageByContext,
    openProjectConversationByContext,
    handleDirectMessageConcludedById,
    syncDirectMessages,
  };
}
