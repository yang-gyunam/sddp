// Settings Pages

// User Settings
export { default as ProfileSettingsPage } from './ProfileSettingsPage.svelte';
export { default as PreferencesSettingsPage } from './PreferencesSettingsPage.svelte';
// Hidden: no backend notification infrastructure yet (email server, Web Push, SignalR events)
// export { default as NotificationsSettingsPage } from './NotificationsSettingsPage.svelte';

// Project Settings
export { default as ProjectGeneralSettingsPage } from './ProjectGeneralSettingsPage.svelte';
export { default as ProjectMembersSettingsPage } from './ProjectMembersSettingsPage.svelte';
export { default as ProjectRolesSettingsPage } from './ProjectRolesSettingsPage.svelte';
export { default as ProjectIntegrationsSettingsPage } from './ProjectIntegrationsSettingsPage.svelte';

// System Settings (Admin)
export { default as SystemUsersSettingsPage } from './SystemUsersSettingsPage.svelte';
export { default as SystemProjectsSettingsPage } from './SystemProjectsSettingsPage.svelte';
export { default as SystemConfigSettingsPage } from './SystemConfigSettingsPage.svelte';
export { default as SystemAuditLogsPage } from './SystemAuditLogsPage.svelte';
export { default as SystemHealthPage } from './SystemHealthPage.svelte';

// Tab Content (sidebar-less wrapper for App shell integration)
export { default as SettingsTabPage } from './SettingsTabPage.svelte';
