/**
 * useShellSelectionSync — Phase 2
 * Syncs active tab path → sidebar selection state
 */

import { globalActiveTab } from '@sddp/shell/stores';
import { setSelectedConversation } from '@sddp/activities/conversations';
import { setSelectedProject } from '@sddp/activities/projects';

export function useShellSelectionSync() {
  let selectedMenuId = $state<string | null>(null);
  let selectedProjectNodePath = $state<string | null>(null);
  let selectedDashboardItemId = $state<string | null>(null);
  let selectedSettingsItemId = $state<string | null>(null);
  let selectedCategoryId = $state<string | null>(null);
  let selectedBacklogProjectId = $state<string | null>(null);

  const unsubscribe = globalActiveTab.subscribe((tab) => {
    if (!tab?.path) {
      selectedMenuId = null;
      setSelectedConversation(null);
      selectedProjectNodePath = null;
      selectedDashboardItemId = null;
      selectedSettingsItemId = null;
      return;
    }

    const path = tab.path;

    if (path.startsWith('/conversation/')) {
      setSelectedConversation(path.replace('/conversation/', ''));
      selectedMenuId = null;
      selectedProjectNodePath = null;
      selectedDashboardItemId = null;
      selectedSettingsItemId = null;
      return;
    }

    if (path.startsWith('/forum/')) {
      setSelectedConversation(path.replace('/forum/', ''));
      selectedMenuId = null;
      selectedProjectNodePath = null;
      selectedDashboardItemId = null;
      selectedSettingsItemId = null;
      return;
    }

    if (path.startsWith('/dm/')) {
      setSelectedConversation(path.replace('/dm/', ''));
      selectedMenuId = null;
      selectedProjectNodePath = null;
      selectedDashboardItemId = null;
      selectedSettingsItemId = null;
      return;
    }

    // Project page paths: /project/{projectId}/{nodeType} → /{projectId}/{nodeType}
    if (path.startsWith('/project/')) {
      const projectId = path.split('/')[2];
      if (projectId) {
        setSelectedProject(projectId);
      }
      selectedProjectNodePath = path.replace('/project', '');
      setSelectedConversation(null);
      selectedMenuId = null;
      selectedDashboardItemId = null;
      selectedSettingsItemId = null;
      return;
    }

    // Settings paths: /settings/profile → settings-profile, etc.
    if (path.startsWith('/settings/')) {
      selectedSettingsItemId = tab.menuId ?? null;
      setSelectedConversation(null);
      selectedMenuId = null;
      selectedProjectNodePath = null;
      selectedDashboardItemId = null;
      return;
    }

    // Dashboard paths: /dashboard/{section} → dashboard-{section}
    if (path.startsWith('/dashboard/')) {
      const section = path.replace('/dashboard/', '');
      selectedDashboardItemId = `dashboard-${section}`;
      setSelectedConversation(null);
      selectedMenuId = null;
      selectedProjectNodePath = null;
      selectedSettingsItemId = null;
      return;
    }

    // Task paths: /tasks/category/{id}, /tasks/backlog/{projectId}
    if (path.startsWith('/tasks/')) {
      const categoryMatch = path.match(/^\/tasks\/category\/(.+)$/);
      if (categoryMatch && categoryMatch[1]) {
        selectedCategoryId = categoryMatch[1];
        selectedBacklogProjectId = null;
      } else {
        const backlogMatch = path.match(/^\/tasks\/backlog\/(.+)$/);
        if (backlogMatch && backlogMatch[1]) {
          selectedBacklogProjectId = backlogMatch[1];
          selectedCategoryId = null;
        } else {
          selectedCategoryId = null;
          selectedBacklogProjectId = null;
        }
      }
      setSelectedConversation(null);
      selectedMenuId = null;
      selectedProjectNodePath = null;
      selectedDashboardItemId = null;
      selectedSettingsItemId = null;
      return;
    }

    setSelectedConversation(null);
    selectedProjectNodePath = null;
    selectedDashboardItemId = null;
    selectedSettingsItemId = null;
    // Extract menu ID from path (e.g., '/topic/{id}' → 'topic-{id}')
    selectedMenuId = path.replace(/^\//, '');
  });

  return {
    get selectedMenuId() {
      return selectedMenuId;
    },
    set selectedMenuId(v: string | null) {
      selectedMenuId = v;
    },
    get selectedProjectNodePath() {
      return selectedProjectNodePath;
    },
    set selectedProjectNodePath(v: string | null) {
      selectedProjectNodePath = v;
    },
    get selectedDashboardItemId() {
      return selectedDashboardItemId;
    },
    set selectedDashboardItemId(v: string | null) {
      selectedDashboardItemId = v;
    },
    get selectedSettingsItemId() {
      return selectedSettingsItemId;
    },
    set selectedSettingsItemId(v: string | null) {
      selectedSettingsItemId = v;
    },
    get selectedCategoryId() {
      return selectedCategoryId;
    },
    set selectedCategoryId(v: string | null) {
      selectedCategoryId = v;
    },
    get selectedBacklogProjectId() {
      return selectedBacklogProjectId;
    },
    set selectedBacklogProjectId(v: string | null) {
      selectedBacklogProjectId = v;
    },
    destroy: unsubscribe,
  };
}
