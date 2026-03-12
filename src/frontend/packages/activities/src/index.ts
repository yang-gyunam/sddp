// @sddp/activities - Activity-level Pages and Layouts
//
// This package contains the main Activity components that are rendered
// in the sidebar based on the selected ActivityBar item.
//
// IMPORTANT: Do NOT use `export *` here. It causes name collisions across domains
// (e.g., setFilterType, setProjectGroups, toggleProjectExpanded).
// All consumers should use subdomain imports: `@sddp/activities/conversations`
//
// This root barrel only re-exports Activity components and constants.
// For domain-specific imports, use subdomain paths:
//   import { ConversationActivity } from '@sddp/activities/conversations';
//   import { resetConversationStore } from '@sddp/activities/conversations';

// --- Activity Components & Constants ---

// Conversations Activity
export { ConversationActivity, CONVERSATIONS_ACTIVITY_ID } from './conversations';

// Requirements Activity
export { REQUIREMENTS_ACTIVITY_ID } from './requirements';

// Specs Activity
export { SPECS_ACTIVITY_ID } from './specs';

// Glossary Activity
export { GLOSSARY_ACTIVITY_ID } from './glossary';

// Artifact Activity
export { ArtifactActivity, ARTIFACT_ACTIVITY_ID } from './artifact';

// Search Activity
export { SEARCH_ACTIVITY_ID } from './search';

// Tasks Activity
export { TaskActivity, TASK_ACTIVITY_ID } from './task';

// Dashboard Activity
export { DashboardActivity, DASHBOARD_ACTIVITY_ID } from './dashboard';

// Settings Activity
export { SettingsActivity, SETTINGS_ACTIVITY_ID } from './settings';
