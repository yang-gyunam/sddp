/**
 * Dashboard Store
 * Manages dashboard data and state
 */

import { createStore } from '@sddp/shell/core';
import type { MyDashboard, SystemDashboard, ProjectDashboard, ActivityLogEntry, DashboardWidgets, NotificationItem } from '../types';

interface DashboardState {
  myDashboard: MyDashboard | null;
  systemDashboard: SystemDashboard | null;
  projectDashboards: Map<string, ProjectDashboard>;
  loading: boolean;
  error: string | null;
  widgets: DashboardWidgets | null;
  widgetsLoading: boolean;
  widgetsError: string | null;
  notifications: NotificationItem[];
  unreadCount: number;
  notificationsLoading: boolean;
}

const initialState: DashboardState = {
  myDashboard: null,
  systemDashboard: null,
  projectDashboards: new Map(),
  loading: false,
  error: null,
  widgets: null,
  widgetsLoading: false,
  widgetsError: null,
  notifications: [],
  unreadCount: 0,
  notificationsLoading: false,
};

export const dashboardStore = createStore<DashboardState>(initialState);

// Actions
export function setMyDashboard(dashboard: MyDashboard): void {
  dashboardStore.update((state) => ({
    ...state,
    myDashboard: dashboard,
    loading: false,
    error: null,
  }));
}

export function setSystemDashboard(dashboard: SystemDashboard): void {
  dashboardStore.update((state) => ({
    ...state,
    systemDashboard: dashboard,
    loading: false,
    error: null,
  }));
}

export function setProjectDashboard(projectId: string, dashboard: ProjectDashboard): void {
  dashboardStore.update((state) => {
    const projectDashboards = new Map(state.projectDashboards);
    projectDashboards.set(projectId, dashboard);
    return {
      ...state,
      projectDashboards,
      loading: false,
      error: null,
    };
  });
}

export function setWidgets(widgets: DashboardWidgets): void {
  dashboardStore.update((state) => ({
    ...state,
    widgets,
    widgetsLoading: false,
    widgetsError: null,
  }));
}

export function setWidgetsLoading(loading: boolean): void {
  dashboardStore.update((state) => ({
    ...state,
    widgetsLoading: loading,
    widgetsError: loading ? null : state.widgetsError,
  }));
}

export function setWidgetsError(error: string | null): void {
  dashboardStore.update((state) => ({
    ...state,
    widgetsLoading: false,
    widgetsError: error,
  }));
}

export function setDashboardLoading(loading: boolean): void {
  dashboardStore.update((state) => ({
    ...state,
    loading,
    error: loading ? null : state.error,
  }));
}

export function setDashboardError(error: string | null): void {
  dashboardStore.update((state) => ({
    ...state,
    loading: false,
    error,
  }));
}

export function addActivity(activity: ActivityLogEntry): void {
  dashboardStore.update((state) => {
    // Add to My Dashboard
    if (state.myDashboard) {
      const recentActivities = [activity, ...state.myDashboard.recentActivities].slice(0, 10);
      return {
        ...state,
        myDashboard: {
          ...state.myDashboard,
          recentActivities,
        },
      };
    }

    // Add to System Dashboard
    if (state.systemDashboard) {
      const recentActivities = [activity, ...state.systemDashboard.recentActivities].slice(0, 10);
      return {
        ...state,
        systemDashboard: {
          ...state.systemDashboard,
          recentActivities,
        },
      };
    }

    // Add to Project Dashboard
    if (activity.projectId && state.projectDashboards.has(activity.projectId)) {
      const projectDashboard = state.projectDashboards.get(activity.projectId)!;
      const recentActivities = [activity, ...projectDashboard.recentActivities].slice(0, 10);
      const projectDashboards = new Map(state.projectDashboards);
      projectDashboards.set(activity.projectId, {
        ...projectDashboard,
        recentActivities,
      });
      return {
        ...state,
        projectDashboards,
      };
    }

    return state;
  });
}

// Notification Actions
export function setNotifications(notifications: NotificationItem[], unreadCount: number): void {
  dashboardStore.update((state) => ({
    ...state,
    notifications,
    unreadCount,
    notificationsLoading: false,
  }));
}

export function setNotificationsLoading(loading: boolean): void {
  dashboardStore.update((state) => ({
    ...state,
    notificationsLoading: loading,
  }));
}

export function addNotification(item: NotificationItem): void {
  dashboardStore.update((state) => ({
    ...state,
    notifications: [item, ...state.notifications],
    unreadCount: state.unreadCount + (item.isRead ? 0 : 1),
  }));
}

export function markNotificationRead(id: string): void {
  dashboardStore.update((state) => {
    const notifications = state.notifications.map((n) =>
      n.id === id ? { ...n, isRead: true } : n
    );
    const unreadCount = notifications.filter((n) => !n.isRead).length;
    return { ...state, notifications, unreadCount };
  });
}

export function markAllNotificationsRead(): void {
  dashboardStore.update((state) => ({
    ...state,
    notifications: state.notifications.map((n) => ({ ...n, isRead: true })),
    unreadCount: 0,
  }));
}

export function clearDashboardData(): void {
  dashboardStore.reset();
}

// Selectors
export function getMyDashboard(): MyDashboard | null {
  return dashboardStore.get().myDashboard;
}

export function getSystemDashboard(): SystemDashboard | null {
  return dashboardStore.get().systemDashboard;
}

export function getProjectDashboard(projectId: string): ProjectDashboard | null {
  return dashboardStore.get().projectDashboards.get(projectId) || null;
}

export function isDashboardLoading(): boolean {
  return dashboardStore.get().loading;
}

export function getDashboardError(): string | null {
  return dashboardStore.get().error;
}

export function getDashboardState(): DashboardState {
  return dashboardStore.get();
}

export function subscribeDashboard(
  listener: (state: DashboardState, prevState: DashboardState) => void
): () => void {
  return dashboardStore.subscribe(listener);
}

export function resetDashboardStore(): void {
  dashboardStore.reset();
}

export type { DashboardState };
