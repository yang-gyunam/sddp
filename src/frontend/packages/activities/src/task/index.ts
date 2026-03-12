// @sddp/activities - Task Activity
// Task management with Kanban board and effort tracking

// Activity Root
export { default as TaskActivity } from './components/TaskActivity.svelte';
export const TASK_ACTIVITY_ID = 'task';

// Types
export * from './types';

// Stores
export * from './stores';

// Services
export * from './services';

// Components (sections + pages only; idioms are internal — avoids name collision with types)
export * from './components/sections';
export * from './components/pages';
