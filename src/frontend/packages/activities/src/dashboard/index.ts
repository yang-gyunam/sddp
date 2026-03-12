// @sddp/activities/dashboard - Dashboard Activity
//
// :
//   dashboard/
// ├── types/ #
// ├── services/ # API (DashboardService)
// ├── stores/ # status (dashboard, dashboardView)
// ├── components/ #
// │ ├── idioms/ # default/ (StatBadge, LineChart)
// │ ├── sections/ # (DashboardSidebar, ActivityLog)
// │ ├── pages/ # (MyDashboardPage, SystemDashboardPage)
// │ └── DashboardActivity.svelte # Activity
//
// Pages (UI Definition):
// - MyDashboardPage      (DASH-MY-001)
// - SystemDashboardPage  (DASH-SYS-001)
// - ProjectDashboardPage (DASH-PRJ-001)

// Types
export * from './types';

// Services (explicit: avoids name collision with store getters of same name)
export {
  getMyDashboard as fetchMyDashboard,
  getSystemDashboard as fetchSystemDashboard,
  getProjectDashboard as fetchProjectDashboard,
  getActivities,
  refreshDashboard,
  getMyOverview,
  getMyActivities,
  getSystemStats,
  getSystemAuditLogs,
  getSystemHealth,
  getMyNotifications,
  markNotificationRead as apiMarkNotificationRead,
  markAllNotificationsRead as apiMarkAllNotificationsRead,
  DashboardService,
  getDashboardService,
  resetDashboardService,
} from './services/DashboardService';
export {
  DashboardHubService,
  getDashboardHubService,
  resetDashboardHubService,
} from './services/DashboardHubService';
export type { SpecStatusChangedPayload, NotificationPayload, DashboardHubCallbacks } from './services/DashboardHubService';

// Stores
export * from './stores';

// Components (sections + pages only; idioms are internal)
export * from './components/sections';
export * from './components/pages';

// Activity
export { default as DashboardActivity } from './components/DashboardActivity.svelte';

// Constants
export const DASHBOARD_ACTIVITY_ID = 'dashboard';
