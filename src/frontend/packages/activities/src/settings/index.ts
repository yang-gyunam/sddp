// @sddp/activities/settings - Settings Activity
//
// Structure:
//   settings/
//   ├── types/           # Type definitions
//   ├── services/        # API services (SettingsService)
//   ├── stores/          # State management (settingsNavigation)
//   ├── components/      # Reusable components
//   │   ├── sections/    # Page sections (SettingsSidebar)
//   │   ├── pages/       # Page components (ProfileSettingsPage, etc.)
//   │   └── SettingsActivity.svelte  # Activity root
//
// Pages (UI Definition):
// - ProfileSettingsPage        (SET-USER-001)
// - PreferencesSettingsPage    (SET-USER-002)
// - NotificationsSettingsPage  (SET-USER-003)
// - ProjectGeneralSettingsPage (SET-PROJ-001)
// - ProjectMembersSettingsPage (SET-PROJ-002)
// - ProjectRolesSettingsPage   (SET-PROJ-003)
// - ProjectIntegrationsPage    (SET-PROJ-004)
// - SystemUsersSettingsPage    (SET-SYS-001)
// - SystemProjectsSettingsPage (SET-SYS-002)
// - SystemConfigSettingsPage   (SET-SYS-003)
// - SystemAuditLogsPage        (SET-SYS-004)
// - SystemHealthPage           (SET-SYS-005)

// Types
export * from './types';

// Services
export * from './services';

// Stores
export * from './stores';

// Utils
export * from './utils';

// Components (sections + pages only; idioms are internal — avoids name collision with types)
export * from './components/sections';
export * from './components/pages';

// Activity
export { default as SettingsActivity } from './components/SettingsActivity.svelte';

// Constants
export const SETTINGS_ACTIVITY_ID = 'settings';
