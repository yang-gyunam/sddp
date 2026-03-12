/**
 * Menu Registry - Maps menu items to their corresponding components and props
 *
 * This module provides a centralized way to resolve which component should be
 * rendered for a given menu item, along with the appropriate props.
 */

import {
  ChannelView,
  ForumView,
} from '@sddp/activities/conversations';
import { SpecPanel } from '@sddp/activities/specs';
import { RequirementPanel } from '@sddp/activities/requirements';
import { GlossaryPanel } from '@sddp/activities/glossary';
import { MyDashboardPage, SystemDashboardPage } from '@sddp/activities/dashboard';
import { MyTasksPage, TaskListDetailPage } from '@sddp/activities/task/components/pages';
import {
  ProfileSettingsPage,
  PreferencesSettingsPage,
  // NotificationsSettingsPage, // Hidden: no backend notification infrastructure yet
  ProjectGeneralSettingsPage,
  ProjectMembersSettingsPage,
  ProjectRolesSettingsPage,
  ProjectIntegrationsSettingsPage,
  SystemUsersSettingsPage,
  SystemProjectsSettingsPage,
  SystemConfigSettingsPage,
  SystemAuditLogsPage,
  SystemHealthPage,
} from '@sddp/activities/settings/components/pages';
import {
  ProjectDashboardPage,
  ProjectConversationsPage,
  ProjectRequirementsPage,
  ProjectSpecsPage,
  ProjectTasksPage,
  ProjectGlossaryPage,
  ProjectArtifactsPage,
  ProjectEffortPage,
  ProjectTraceabilityPage,
} from '@sddp/activities/projects/components/pages';
import type { TreeNode } from '@sddp/shell/types';

// ============================================
// Types
// ============================================

/**
 * Context required to resolve menu components
 */
export interface MenuContext {
  tenantId: string;
  projectId: string;
}

/**
 * Result of resolving a menu item to a component
 */
export interface ResolvedMenuComponent {
  // Using 'unknown' for component to avoid complex generic type issues
  // The actual component will be validated at runtime by DynamicContentRenderer
  component: unknown;
  props: Record<string, unknown>;
}

/**
 * Menu resolver function type
 * Returns resolved component or null if this resolver doesn't handle the menu
 */
type MenuResolver = (menu: TreeNode, context: MenuContext) => ResolvedMenuComponent | null;

// ============================================
// Menu Type Patterns
// ============================================

/**
 * Patterns to identify menu types from their IDs or paths
 */
const MENU_PATTERNS = {
  // Conversation patterns (channels, forums, DMs)
  conversation: /^conversation-/,
  newConversation: /^new-conversation$/,
  forum: /^forum-/,
  topic: /^forum-topic-/,
  directMessage: /^dm-/,
  // Spec/Requirement/Glossary
  spec: /^spec-/,
  requirement: /^requirement-/,
  glossary: /^glossary-/,
  newSpec: /^new-spec$/,
  newRequirement: /^new-requirement$/,
  newGlossary: /^new-glossary$/,
  // Project page patterns (e.g., "project-{uuid}-dashboard")
  projectPage: /^project-(.+)-(dashboard|conversations|requirements|specs|tasks|glossary|artifacts|effort|traceability)$/,
  // Dashboard patterns
  dashboardMy: /^dashboard-(overview|my-tasks|recent|notifications)$/,
  // Task activity patterns
  taskMyTasks: /^tasks-my-tasks$/,
  taskCategory: /^tasks-category-(.+)$/,
  taskBacklog: /^tasks-backlog-(.+)$/,
  // Settings activity patterns (individual pages open as separate tabs)
  settings: /^settings-/,
} as const;

// ============================================
// Resolvers
// ============================================

/**
 * Extracts the conversation ID from menu.id (e.g., "conversation-{uuid}" -> "{uuid}")
 */
function extractConversationId(menuId: string): string | undefined {
  if (MENU_PATTERNS.newConversation.test(menuId)) {
    return undefined; // New conversation, no ID yet
  }
  const match = menuId.match(/^conversation-(.+)$/);
  return match ? match[1] : undefined;
}

/**
 * Extracts the spec ID from menu.id (e.g., "spec-{uuid}" -> "{uuid}")
 */
function extractSpecId(menuId: string): string | undefined {
  if (MENU_PATTERNS.newSpec.test(menuId)) {
    return undefined;
  }
  const match = menuId.match(/^spec-(.+)$/);
  return match ? match[1] : undefined;
}

/**
 * Extracts the requirement ID from menu.id (e.g., "requirement-{uuid}" -> "{uuid}")
 */
function extractRequirementId(menuId: string): string | undefined {
  if (MENU_PATTERNS.newRequirement.test(menuId)) {
    return undefined;
  }
  const match = menuId.match(/^requirement-(.+)$/);
  return match ? match[1] : undefined;
}

/**
 * Spec panel resolver
 */
const specResolver: MenuResolver = (menu, context) => {
  if (MENU_PATTERNS.spec.test(menu.id) || MENU_PATTERNS.newSpec.test(menu.id)) {
    const specId = extractSpecId(menu.id);
    return {
      component: SpecPanel,
      props: {
        tenantId: context.tenantId,
        projectId: context.projectId,
        specId,
      },
    };
  }
  return null;
};

/**
 * Requirement panel resolver
 */
const requirementResolver: MenuResolver = (menu, context) => {
  if (MENU_PATTERNS.requirement.test(menu.id) || MENU_PATTERNS.newRequirement.test(menu.id)) {
    const requirementId = extractRequirementId(menu.id);
    return {
      component: RequirementPanel,
      props: {
        tenantId: context.tenantId,
        projectId: context.projectId,
        requirementId,
      },
    };
  }
  return null;
};

/**
 * Extracts the glossary term ID from menu.id (e.g., "glossary-{uuid}" -> "{uuid}")
 */
function extractGlossaryTermId(menuId: string): string | undefined {
  if (MENU_PATTERNS.newGlossary.test(menuId)) {
    return undefined;
  }
  const match = menuId.match(/^glossary-(.+)$/);
  return match ? match[1] : undefined;
}

/**
 * Glossary panel resolver
 * Note: GlossaryPanel is initialized with empty terms array.
 * The panel manages its own data fetching via store/service.
 */
const glossaryResolver: MenuResolver = (menu) => {
  if (MENU_PATTERNS.glossary.test(menu.id) || MENU_PATTERNS.newGlossary.test(menu.id)) {
    const termId = extractGlossaryTermId(menu.id);
    return {
      component: GlossaryPanel,
      props: {
        terms: [],
        // termId can be used to initially select a term if needed
        initialTermId: termId,
      },
    };
  }
  return null;
};

// Note: RelationshipPanel requires additional required props
// (entityType/entityId for Relationship)
// These should be opened from specific contexts rather than directly from menu

// ============================================
// Project Page Resolvers
// ============================================

/**
 * Component map for project page types
 */
const PROJECT_PAGE_COMPONENTS: Record<string, unknown> = {
  dashboard: ProjectDashboardPage,
  conversations: ProjectConversationsPage,
  requirements: ProjectRequirementsPage,
  specs: ProjectSpecsPage,
  tasks: ProjectTasksPage,
  glossary: ProjectGlossaryPage,
  artifacts: ProjectArtifactsPage,
  effort: ProjectEffortPage,
  traceability: ProjectTraceabilityPage,
};

/**
 * Project page resolver
 * Handles menu IDs like "project-{uuid}-{pageType}"
 */
const projectPageResolver: MenuResolver = (menu) => {
  const match = menu.id.match(MENU_PATTERNS.projectPage);
  if (!match || !match[1] || !match[2]) return null;

  const projectId = match[1];
  const pageType = match[2];
  const component = PROJECT_PAGE_COMPONENTS[pageType];

  if (!component) return null;

  // Extract project name from menu label (format: "PageType - ProjectName")
  // or use the parent project node's label if available
  let projectName = '';
  if (menu.label) {
    // Menu label is typically "Dashboard - ProjectName" etc.
    const labelParts = menu.label.split(' - ');
    if (labelParts.length > 1) {
      projectName = labelParts.slice(1).join(' - '); // Handle project names with " - " in them
    }
  }

  return {
    component,
    props: { projectId, projectName },
  };
};

// ============================================
// Conversation Resolvers (Channels, Forums, DMs)
// ============================================

/**
 * Extracts the forum ID from menu.id (e.g., "forum-{id}" -> "{id}")
 */
function extractForumId(menuId: string): string | undefined {
  const match = menuId.match(/^forum-(.+)$/);
  return match ? match[1] : undefined;
}

/**
 * Extracts the topic ID from menu.id (e.g., "forum-topic-{id}" -> "{id}")
 */
function extractTopicId(menuId: string): string | undefined {
  const match = menuId.match(/^forum-topic-(.+)$/);
  return match ? match[1] : undefined;
}

/**
 * Extracts the DM ID from menu.id (e.g., "dm-{id}" -> "{id}")
 */
function extractDirectMessageId(menuId: string): string | undefined {
  const match = menuId.match(/^dm-(.+)$/);
  return match ? match[1] : undefined;
}

/**
 * Channel conversation resolver - resolves to ChannelView
 */
const conversationChannelResolver: MenuResolver = (menu, context) => {
  if (MENU_PATTERNS.conversation.test(menu.id) || MENU_PATTERNS.newConversation.test(menu.id)) {
    const conversationId = extractConversationId(menu.id);
    if (!conversationId) return null;

    const conversationName = (menu.label || 'Unknown').replace(/^#/, '');

    return {
      component: ChannelView,
      props: {
        conversationId,
        conversationName,
        headerIcon: 'hash',
        sourceType: 'conversation',
        conversationType: 'Channel',
        tenantId: context.tenantId,
        projectId: context.projectId,
      },
    };
  }
  return null;
};

/**
 * Forum resolver - resolves to ForumView
 */
const forumResolver: MenuResolver = (menu, context) => {
  if (MENU_PATTERNS.forum.test(menu.id)) {
    const conversationId = extractForumId(menu.id);
    if (!conversationId) return null;

    const conversationName = (menu.label || 'Unknown').replace(/^#/, '');

    return {
      component: ForumView,
      props: {
        tenantId: context.tenantId,
        projectId: context.projectId,
        conversationId,
        conversationName,
      },
    };
  }
  return null;
};

/**
 * Forum topic resolver - resolves to ForumView with selected topic
 * Note: Forum topics require conversationId which should be passed via menu metadata.
 */
const topicResolver: MenuResolver = (menu, context) => {
  if (MENU_PATTERNS.topic.test(menu.id)) {
    const topicId = extractTopicId(menu.id);
    if (!topicId) return null;

    const conversationId = (menu.data as { conversationId?: string } | undefined)?.conversationId;
    if (!conversationId) {
      console.warn(`Forum topic ${topicId} cannot be resolved without conversationId. Open from forum instead.`);
      return null;
    }

    return {
      component: ForumView,
      props: {
        tenantId: context.tenantId,
        projectId: context.projectId,
        conversationId,
        conversationName: menu.label || 'Topic',
        selectedTopicId: topicId,
      },
    };
  }
  return null;
};

/**
 * Direct message resolver - resolves to ChannelView
 */
const directMessageResolver: MenuResolver = (menu, context) => {
  if (MENU_PATTERNS.directMessage.test(menu.id)) {
    const dmId = extractDirectMessageId(menu.id);
    if (!dmId) return null;

    const dmName = menu.label || 'Unknown';
    const menuData = menu.data as { projectId?: string } | undefined;
    const dmProjectId = typeof menuData?.projectId === 'string' ? menuData.projectId : '';

    return {
      component: ChannelView,
      props: {
        conversationId: dmId,
        conversationName: dmName,
        headerIcon: 'message-circle',
        sourceType: 'conversation',
        conversationType: 'DirectMessage',
        tenantId: context.tenantId,
        projectId: dmProjectId,
      },
    };
  }
  return null;
};

// ============================================
// Dashboard Resolvers
// ============================================

/**
 * My Dashboard resolver - handles overview, my-tasks, recent, notifications
 */
const myDashboardResolver: MenuResolver = (menu) => {
  if (MENU_PATTERNS.dashboardMy.test(menu.id)) {
    const match = menu.id.match(MENU_PATTERNS.dashboardMy);
    const section = match?.[1] ?? 'overview';
    return {
      component: MyDashboardPage,
      props: {
        section,
      },
    };
  }
  return null;
};

// ============================================
// Task Resolvers
// ============================================

/**
 * My Tasks resolver - kanban board with task filters
 */
const myTasksResolver: MenuResolver = (menu, context) => {
  if (MENU_PATTERNS.taskMyTasks.test(menu.id)) {
    return {
      component: MyTasksPage,
      props: {
        tenantId: context?.tenantId ?? '',
        hideSidebar: true,
      },
    };
  }
  return null;
};

/**
 * Task category resolver - 2-panel list+detail for a user-defined category
 */
const taskCategoryResolver: MenuResolver = (menu, context) => {
  const match = menu.id.match(MENU_PATTERNS.taskCategory);
  if (match && match[1]) {
    return {
      component: TaskListDetailPage,
      props: {
        tenantId: context?.tenantId ?? '',
        categoryId: match[1],
        categoryName: menu.label || 'Category',
      },
    };
  }
  return null;
};

/**
 * Task backlog resolver - 2-panel list+detail for a backlog project
 */
const taskBacklogResolver: MenuResolver = (menu, context) => {
  const match = menu.id.match(MENU_PATTERNS.taskBacklog);
  if (match && match[1]) {
    return {
      component: TaskListDetailPage,
      props: {
        tenantId: context?.tenantId ?? '',
        projectId: match[1],
        categoryName: menu.label || 'Backlog',
      },
    };
  }
  return null;
};

// ============================================
// Settings Resolver
// ============================================

/**
 * Project settings component map
 */
const PROJECT_SETTINGS_COMPONENTS: Record<string, unknown> = {
  general: ProjectGeneralSettingsPage,
  members: ProjectMembersSettingsPage,
  roles: ProjectRolesSettingsPage,
  integrations: ProjectIntegrationsSettingsPage,
};

/**
 * System settings component map
 */
const SYSTEM_SETTINGS_COMPONENTS: Record<string, unknown> = {
  dashboard: SystemDashboardPage,
  users: SystemUsersSettingsPage,
  projects: SystemProjectsSettingsPage,
  config: SystemConfigSettingsPage,
  audit: SystemAuditLogsPage,
  health: SystemHealthPage,
};

/**
 * Settings resolver - each settings page opens as its own editor tab
 */
const settingsResolver: MenuResolver = (menu, context) => {
  if (!MENU_PATTERNS.settings.test(menu.id)) return null;

  // User settings
  if (menu.id === 'settings-profile') {
    return { component: ProfileSettingsPage, props: {} };
  }
  if (menu.id === 'settings-preferences') {
    return { component: PreferencesSettingsPage, props: {} };
  }
  // Hidden: no backend notification infrastructure yet
  // if (menu.id === 'settings-notifications') {
  //   return { component: NotificationsSettingsPage, props: {} };
  // }

  // System settings: settings-system-{type}
  const systemMatch = menu.id.match(/^settings-system-(.+)$/);
  if (systemMatch && systemMatch[1]) {
    const pageType = systemMatch[1];
    const component = SYSTEM_SETTINGS_COMPONENTS[pageType];
    if (!component) return null;

    // Dashboard and Health pages use SystemDashboardPage with section prop
    if (pageType === 'dashboard') {
      return { component, props: { section: 'system-stats' } };
    }
    if (pageType === 'health') {
      return { component, props: {} };
    }
    return { component, props: {} };
  }

  // Project settings: settings-project-{projectId}-{type}
  const projectMatch = menu.id.match(/^settings-project-(.+)-(general|members|roles|integrations)$/);
  if (projectMatch && projectMatch[1] && projectMatch[2]) {
    const projectId = projectMatch[1];
    const pageType = projectMatch[2];
    const component = PROJECT_SETTINGS_COMPONENTS[pageType];
    if (!component) return null;

    // Extract project name from menu label if available
    let projectName = '';
    if (menu.label) {
      projectName = menu.label;
    }

    return {
      component,
      props: {
        tenantId: context.tenantId,
        projectId,
        projectName,
      },
    };
  }

  return null;
};

/**
 * All registered resolvers in priority order
 */
const resolvers: MenuResolver[] = [
  // Project page resolver (highest priority - matches specific pattern)
  projectPageResolver,
  // Dashboard resolver
  myDashboardResolver,
  // Task resolvers
  myTasksResolver,
  taskCategoryResolver,
  taskBacklogResolver,
  // Settings resolver
  settingsResolver,
  // Conversation resolvers (channels, forums, DMs)
  conversationChannelResolver,
  forumResolver,
  topicResolver,
  directMessageResolver,
  // Domain resolvers
  specResolver,
  requirementResolver,
  glossaryResolver,
];

// ============================================
// Public API
// ============================================

/**
 * Resolves a menu item to its corresponding component and props
 *
 * @param menu - The menu node to resolve
 * @param context - Context containing tenantId and projectId
 * @returns Resolved component and props, or null if no resolver matches
 *
 * @example
 * ```ts
 * const result = resolveMenuComponent(
 *   { id: 'conversation-123', label: 'API Design' },
 *   { tenantId: 'tenant-1', projectId: 'project-1' }
 * );
 * // result = { component: ChannelView, props: { conversationId, conversationName, ... } }
 * ```
 */
export function resolveMenuComponent(
  menu: TreeNode,
  context: MenuContext
): ResolvedMenuComponent | null {
  for (const resolver of resolvers) {
    const result = resolver(menu, context);
    if (result) {
      return result;
    }
  }
  return null;
}

/**
 * Checks if a menu item has a registered component
 */
export function hasMenuComponent(menu: TreeNode): boolean {
  // Quick check using patterns without full resolution
  const patterns = Object.values(MENU_PATTERNS);
  return patterns.some((pattern) => pattern.test(menu.id));
}

/**
 * Gets the menu type from a menu item
 */
export function getMenuType(
  menu: TreeNode
): 'conversation' | 'spec' | 'requirement' | 'glossary' | 'forum' | 'topic' | null {
  // Conversation patterns (channels, forums, DMs)
  if (MENU_PATTERNS.conversation.test(menu.id) || MENU_PATTERNS.newConversation.test(menu.id)) {
    return 'conversation';
  }
  if (MENU_PATTERNS.forum.test(menu.id)) {
    return 'forum';
  }
  if (MENU_PATTERNS.topic.test(menu.id)) {
    return 'topic';
  }
  if (MENU_PATTERNS.directMessage.test(menu.id)) {
    return 'conversation';
  }
  if (MENU_PATTERNS.spec.test(menu.id) || MENU_PATTERNS.newSpec.test(menu.id)) {
    return 'spec';
  }
  if (MENU_PATTERNS.requirement.test(menu.id) || MENU_PATTERNS.newRequirement.test(menu.id)) {
    return 'requirement';
  }
  if (MENU_PATTERNS.glossary.test(menu.id) || MENU_PATTERNS.newGlossary.test(menu.id)) {
    return 'glossary';
  }
  return null;
}

/**
 * Registers a custom resolver (for extensions/plugins)
 */
export function registerResolver(resolver: MenuResolver): void {
  resolvers.push(resolver);
}

/**
 * Default icons for menu types
 */
export const MENU_TYPE_ICONS: Record<string, string> = {
  conversation: 'hash',
  forum: 'list',
  topic: 'message-circle',
  directMessage: 'message-circle',
  spec: 'file-signature',
  requirement: 'clipboard-list',
  relationship: 'git-branch',
  glossary: 'book-open',
};
