/**
 * resetAppState - Resets all application state (stores + services)
 *
 * Called on:
 *   1. Explicit logout — clear previous user's data
 *   2. Login success (before loading data) — ensure clean slate
 */

// Shell stores
import { tabsStore, resetLayout } from '@sddp/shell/stores';
import { navigationService } from '@sddp/shell/core/services';

// Conversation
import {
  resetConversationStore,
  resetSidebarStore,
  resetConversationView,
  resetConversationService,
  resetConversationHubService,
} from '@sddp/activities/conversations';

// Requirement
import {
  resetRequirementStore,
  resetRequirementSidebarStore,
  resetRequirementService,
} from '@sddp/activities/requirements';

// Spec
import {
  resetSpecStore,
  resetSpecSidebarStore,
  resetSpecService,
} from '@sddp/activities/specs';

// Project
import {
  resetProjectsStore,
  resetTimelineStore,
  resetProjectService,
  resetTimelineService,
} from '@sddp/activities/projects';

// Glossary
import {
  resetGlossaryStore,
  resetGlossarySidebar,
  resetGlossaryService,
} from '@sddp/activities/glossary';

// Relationship
import {
  resetRelationshipStore,
  resetForceGraphStore,
  resetTraceGraphStore,
  resetRelationshipService,
} from '@sddp/activities/relationship';

// Task
import { resetTaskStore, resetTaskService } from '@sddp/activities/task';

// AI
import { resetAiStore, resetAiAnalysisService } from '@sddp/activities/ai';

// Search
import {
  resetSearchQuery,
  resetSearchResultsStore,
  resetSearchService,
} from '@sddp/activities/search';

// Artifact
import { resetArtifactStore, resetArtifactService } from '@sddp/activities/artifact';

// Dashboard
import {
  resetDashboardHubService,
  resetDashboardStore,
  resetDashboardViewStore,
  resetDashboardService,
} from '@sddp/activities/dashboard';

// Settings
import {
  resetUserManagementService,
  resetAuditLogService,
  resetSettingsNavigationStore,
  resetSettingsService,
  resetRoleService,
  resetSystemConfigService,
} from '@sddp/activities/settings';

// Effort
import { resetEffortStore, resetEffortService } from '@sddp/activities/effort';

/**
 * Reset app state but preserve layout (sidebar, panel positions).
 * Used on browser reload (token refresh) where layout should survive.
 */
export function resetAppStatePreserveLayout(): void {
  // Shell: tabs & navigation only (preserve layout)
  tabsStore.reset();
  navigationService.clearHistory();

  _resetDomainStores();
}

export function resetAppState(): void {
  // Shell: tabs, navigation & layout
  tabsStore.reset();
  navigationService.clearHistory();
  resetLayout();

  _resetDomainStores();
}

function _resetDomainStores(): void {
  // Conversation
  resetConversationStore();
  resetSidebarStore();
  resetConversationView();
  resetConversationService();
  resetConversationHubService();

  // Requirement
  resetRequirementStore();
  resetRequirementSidebarStore();
  resetRequirementService();

  // Spec
  resetSpecStore();
  resetSpecSidebarStore();
  resetSpecService();

  // Project
  resetProjectsStore();
  resetTimelineStore();
  resetProjectService();
  resetTimelineService();

  // Glossary
  resetGlossaryStore();
  resetGlossarySidebar();
  resetGlossaryService();

  // Relationship
  resetRelationshipStore();
  resetForceGraphStore();
  resetTraceGraphStore();
  resetRelationshipService();

  // Task
  resetTaskStore();
  resetTaskService();

  // AI
  resetAiStore();
  resetAiAnalysisService();

  // Search
  resetSearchQuery();
  resetSearchResultsStore();
  resetSearchService();

  // Artifact
  resetArtifactStore();
  resetArtifactService();

  // Dashboard
  resetDashboardStore();
  resetDashboardViewStore();
  resetDashboardService();
  resetDashboardHubService();

  // Settings
  resetUserManagementService();
  resetAuditLogService();
  resetSettingsNavigationStore();
  resetSettingsService();
  resetRoleService();
  resetSystemConfigService();

  // Effort
  resetEffortStore();
  resetEffortService();

  // Clear user-specific localStorage entries
  _clearUserLocalStorage();
}

function _clearUserLocalStorage(): void {
  const keysToRemove = [
    'sddp-expanded-projects',
    'settings-active-category',
    'settings-selected-project-id',
    'sddp-saved-searches',
    'sddp-notification-settings',
    'sddp-preferences',
  ];
  keysToRemove.forEach((key) => localStorage.removeItem(key));

  // Pattern-matched keys (sidebar-panels-*, task-categories-*)
  const prefixes = ['sidebar-panels-', 'task-categories-'];
  const allKeys = Object.keys(localStorage);
  for (const key of allKeys) {
    if (prefixes.some((prefix) => key.startsWith(prefix))) {
      localStorage.removeItem(key);
    }
  }
}
