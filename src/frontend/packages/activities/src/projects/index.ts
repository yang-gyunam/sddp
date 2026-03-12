// Projects Activity - Exports

// Types
export * from './types/project.types';

// Stores
export {
  projectStore,
  getProjectState,
  setProjects,
  setProjectsLoading,
  setProjectsError,
  toggleProjectExpanded,
  setProjectExpanded,
  setSelectedProject,
  setProjectDetail,
  subscribeProject,
} from './stores/project.store';

export {
  setPendingEntityId,
  consumePendingEntityId,
} from './stores/projects.store';

// Timeline store
export {
  subscribeTimeline,
  addTimelineEvent,
  getTimelineEvents,
} from './stores/timeline.store';

// Timeline actions
export { loadTimelineByProject, mapAuditLogDtoToTimelineEvent } from './actions';
export type { AuditLogDto } from './services/TimelineService';

// Store resets (for session cleanup)
export { resetProjectsStore } from './stores/projects.store';
export { resetTimelineStore } from './stores/timeline.store';

// Services
export {
  getProjectService,
  ProjectService,
  getProjects,
  getProjectsWithBadges,
  getProjectById,
  createProject,
  updateProject,
} from './services/ProjectService';
export type { CreateProjectParams } from './services/ProjectService';

// Service resets (for session cleanup)
export { resetProjectService } from './services/ProjectService';
export { resetTimelineService } from './services/TimelineService';

// Components
export { ProjectsSidebar } from './components/sections';
export { default as TimelineEventItem } from './components/idioms/TimelineEventItem.svelte';
