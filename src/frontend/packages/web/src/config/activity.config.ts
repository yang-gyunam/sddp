import type { ActivityBarItem, ActivityPanelMap } from '@sddp/shell/types';

export const activityItems: ActivityBarItem[] = [
  { id: 'dashboard', icon: 'dashboard', label: 'Dashboard' },
  { id: 'projects', icon: 'multiple-windows', label: 'Projects' },
  { id: 'conversations', icon: 'comment-discussion', label: 'Conversations' },
  { id: 'tasks', icon: 'tasklist', label: 'Tasks' },
];

export const activityBottomItems: ActivityBarItem[] = [
  { id: 'settings', icon: 'settings-gear', label: 'Settings' },
];

export const activityPanels: ActivityPanelMap = {
  // projects, settings: App.svelte dynamicActivityPanels create ()
  conversations: [
    { id: 'channels', title: 'Channels', icon: 'hash' },
    { id: 'forums', title: 'Forums', icon: 'list' },
  ],
  tasks: [
    { id: 'my-tasks', title: 'My Tasks', icon: 'check-square' },
    { id: 'backlog', title: 'Backlog', icon: 'inbox' },
  ],
  dashboard: [
    { id: 'my-dashboard', title: 'My Dashboard', icon: 'user' },
  ],
};
