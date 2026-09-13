import { resetConversationStore } from './conversations/stores/conversation.store';
import { resetSidebarStore } from './conversations/stores/conversation-sidebar.store';
import { resetConversationView } from './conversations/stores/conversation-view.store';
import { resetConversationService } from './conversations/services/ConversationService';
import { resetConversationHubService } from './conversations/services/ConversationHubService';
import { resetRequirementStore } from './requirements/stores/requirement.store';
import { resetRequirementSidebarStore } from './requirements/stores/requirement-sidebar.store';
import { resetRequirementService } from './requirements/services/RequirementService';
import { resetSpecStore } from './specs/stores/spec.store';
import { resetSpecSidebarStore } from './specs/stores/spec-sidebar.store';
import { resetSpecService } from './specs/services/SpecService';
import { resetProjectsStore } from './projects/stores/projects.store';
import { resetTimelineStore } from './projects/stores/timeline.store';
import { resetProjectService } from './projects/services/ProjectService';
import { resetTimelineService } from './projects/services/TimelineService';
import { resetGlossaryStore } from './glossary/stores/glossary.store';
import { resetGlossarySidebar } from './glossary/stores/glossary-sidebar.store';
import { resetGlossaryService } from './glossary/services/GlossaryService';
import { resetRelationshipStore } from './relationship/stores/relationship.store';
import { resetForceGraphStore } from './relationship/stores/forceGraph.store';
import { resetTraceGraphStore } from './relationship/stores/traceGraph.store';
import { resetRelationshipService } from './relationship/services/RelationshipService';
import { resetTaskStore } from './task/stores/task.store';
import { resetTaskService } from './task/services/TaskService';
import { resetAiStore } from './ai/stores/ai.store';
import { resetAiAnalysisService } from './ai/services/AiAnalysisService';
import { resetSearchQuery } from './search/stores/searchQuery.store';
import { resetSearchResultsStore } from './search/stores/searchResults.store';
import { resetSearchService } from './search/services/SearchService';
import { resetArtifactStore } from './artifact/stores/artifact.store';
import { resetArtifactService } from './artifact/services/ArtifactService';
import { resetDashboardStore } from './dashboard/stores/dashboard.store';
import { resetDashboardViewStore } from './dashboard/stores/dashboardView.store';
import { resetDashboardService } from './dashboard/services/DashboardService';
import { resetDashboardHubService } from './dashboard/services/DashboardHubService';
import { resetUserManagementService } from './settings/services/UserManagementService';
import { resetAuditLogService } from './settings/services/AuditLogService';
import { resetSettingsNavigationStore } from './settings/stores/settingsNavigation.store';
import { resetSettingsService } from './settings/services/SettingsService';
import { resetRoleService } from './settings/services/RoleService';
import { resetSystemConfigService } from './settings/services/SystemConfigService';
import { resetEffortStore } from './effort/stores/effort.store';
import { resetEffortService } from './effort/services/effort.service';

export const activityResetters = [
  resetConversationStore,
  resetSidebarStore,
  resetConversationView,
  resetConversationService,
  resetConversationHubService,
  resetRequirementStore,
  resetRequirementSidebarStore,
  resetRequirementService,
  resetSpecStore,
  resetSpecSidebarStore,
  resetSpecService,
  resetProjectsStore,
  resetTimelineStore,
  resetProjectService,
  resetTimelineService,
  resetGlossaryStore,
  resetGlossarySidebar,
  resetGlossaryService,
  resetRelationshipStore,
  resetForceGraphStore,
  resetTraceGraphStore,
  resetRelationshipService,
  resetTaskStore,
  resetTaskService,
  resetAiStore,
  resetAiAnalysisService,
  resetSearchQuery,
  resetSearchResultsStore,
  resetSearchService,
  resetArtifactStore,
  resetArtifactService,
  resetDashboardStore,
  resetDashboardViewStore,
  resetDashboardService,
  resetDashboardHubService,
  resetUserManagementService,
  resetAuditLogService,
  resetSettingsNavigationStore,
  resetSettingsService,
  resetRoleService,
  resetSystemConfigService,
  resetEffortStore,
  resetEffortService,
] as const;

export function resetActivityState(): void {
  for (const reset of activityResetters) reset();
}
