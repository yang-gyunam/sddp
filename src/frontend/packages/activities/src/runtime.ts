// Web composition runtime. This entry intentionally exports no Svelte components.

export {
  conversationStoreInstance,
  getConversationStoreState,
  setChannels,
  setChannelsLoading,
} from './conversations/stores/conversation.store';
export {
  getSidebarState,
  removeConversation,
  removeDirectMessage,
  setSelectedConversation,
} from './conversations/stores/conversation-sidebar.store';
export { getActiveConversationContext } from './conversations/stores/active-conversation-context.store';
export { createConversationService } from './conversations/services/ConversationService';
export { getConversationHubService } from './conversations/services/ConversationHubService';
export { loadGlobalConversationsSidebar } from './conversations/actions';
export type { ConversationEntry, Message } from './conversations/types';

export {
  projectStore,
  getProjectState,
  setProjects,
  setProjectsLoading,
  setProjectsError,
  setSelectedProject,
} from './projects/stores/project.store';
export { subscribeTimeline, addTimelineEvent } from './projects/stores/timeline.store';
export { setPendingEntityId } from './projects/stores/projects.store';
export { createProjectService } from './projects/services/ProjectService';
export { loadTimelineByProject, mapAuditLogDtoToTimelineEvent } from './projects/actions';
export type { ProjectPageType, ProjectTreeNode, TimelineEvent } from './projects/types';
export type { AuditLogDto } from './projects/services/TimelineService';

export { getDashboardHubService } from './dashboard/services/DashboardHubService';
export { getMyNotifications } from './dashboard/services/DashboardService';
export { addNotification, setNotifications } from './dashboard/stores/dashboard.store';

export { aiCapabilityStore, bindAiCapabilityState } from './ai/stores/ai-capability.store';
