/**
 * Entity Icons
 * Centralized mapping of entity types to their icons
 *
 * This ensures consistent icon usage across the application.
 * Supports both codicons (VS Code icons) and SVG icons (Icon component).
 *
 * @see https://microsoft.github.io/vscode-codicons/dist/codicon/codicon.html
 */

/**
 * Entity types used across the application
 */
export type EntityType =
  | 'task'
  | 'tasks'
  | 'spec'
  | 'specs'
  | 'requirement'
  | 'requirements'
  | 'conversation'
  | 'conversations'
  | 'glossary'
  | 'artifact'
  | 'artifacts'
  | 'project'
  | 'projects'
  | 'user'
  | 'users'
  | 'dashboard'
  | 'effort'
  | 'notification'
  | 'notifications'
  | 'activity'
  | 'activities'
  | 'audit'
  | 'health'
  | 'settings';

/**
 * Icon mapping for both codicon and SVG icon systems
 */
interface EntityIconMapping {
  /** VS Code Codicon name (used with iconType="codicon") */
  codicon: string;
  /** SVG icon name (used with Icon component) */
  svg: string;
}

/**
 * Complete entity icon mapping
 * Maps entity types to both codicon and SVG icon names
 */
export const ENTITY_ICON_MAP: Record<EntityType, EntityIconMapping> = {
  // Core entities
  task: { codicon: 'checklist', svg: 'check-square' },
  tasks: { codicon: 'checklist', svg: 'check-square' },
  spec: { codicon: 'file-code', svg: 'file-code' },
  specs: { codicon: 'file-code', svg: 'file-code' },
  requirement: { codicon: 'tasklist', svg: 'clipboard-list' },
  requirements: { codicon: 'tasklist', svg: 'clipboard-list' },
  conversation: { codicon: 'comment-discussion', svg: 'message-square' },
  conversations: { codicon: 'comment-discussion', svg: 'message-square' },
  glossary: { codicon: 'book', svg: 'book-open' },
  artifact: { codicon: 'package', svg: 'package' },
  artifacts: { codicon: 'package', svg: 'package' },

  // Organization
  project: { codicon: 'folder', svg: 'folder' },
  projects: { codicon: 'folder', svg: 'folder' },
  user: { codicon: 'person', svg: 'user' },
  users: { codicon: 'organization', svg: 'user' },

  // Navigation & Features
  dashboard: { codicon: 'dashboard', svg: 'layout-dashboard' },
  effort: { codicon: 'clock', svg: 'clock' },
  notification: { codicon: 'bell', svg: 'bell' },
  notifications: { codicon: 'bell', svg: 'bell' },
  activity: { codicon: 'pulse', svg: 'history' },
  activities: { codicon: 'pulse', svg: 'history' },
  audit: { codicon: 'history', svg: 'history' },
  health: { codicon: 'heart', svg: 'shield-check' },
  settings: { codicon: 'settings-gear', svg: 'settings' },
};

/**
 * Codicon names for entities (for use with iconType="codicon")
 * @example icon={ENTITY_ICONS.tasks} iconType="codicon"
 */
export const ENTITY_ICONS: Record<EntityType, string> = Object.fromEntries(
  Object.entries(ENTITY_ICON_MAP).map(([key, value]) => [key, value.codicon]),
) as Record<EntityType, string>;

/**
 * SVG icon names for entities (for use with Icon component)
 * @example <Icon name={ENTITY_SVG_ICONS.tasks} />
 */
export const ENTITY_SVG_ICONS: Record<EntityType, string> = Object.fromEntries(
  Object.entries(ENTITY_ICON_MAP).map(([key, value]) => [key, value.svg]),
) as Record<EntityType, string>;

/**
 * Get codicon name for an entity type
 * @param entityType - The entity type
 * @param fallback - Fallback icon if entity type not found (default: 'circle-outline')
 * @returns Codicon name
 */
export function getEntityIcon(entityType: string, fallback = 'circle-outline'): string {
  const normalizedType = entityType.toLowerCase().replace(/[-_\s]/g, '') as EntityType;
  return ENTITY_ICONS[normalizedType] ?? ENTITY_ICONS[entityType as EntityType] ?? fallback;
}

/**
 * Get SVG icon name for an entity type (for Icon component)
 * @param entityType - The entity type
 * @param fallback - Fallback icon if entity type not found (default: 'circle')
 * @returns SVG icon name
 */
export function getEntitySvgIcon(entityType: string, fallback = 'circle'): string {
  const normalizedType = entityType.toLowerCase().replace(/[-_\s]/g, '') as EntityType;
  return ENTITY_SVG_ICONS[normalizedType] ?? ENTITY_SVG_ICONS[entityType as EntityType] ?? fallback;
}

/**
 * Entity labels mapping
 * Maps entity types to their display labels
 */
export const ENTITY_LABELS: Record<EntityType, string> = {
  task: 'Task',
  tasks: 'Tasks',
  spec: 'Spec',
  specs: 'Specs',
  requirement: 'Requirement',
  requirements: 'Requirements',
  conversation: 'Conversation',
  conversations: 'Conversations',
  glossary: 'Glossary',
  artifact: 'Artifact',
  artifacts: 'Artifacts',
  project: 'Project',
  projects: 'Projects',
  user: 'User',
  users: 'Users',
  dashboard: 'Dashboard',
  effort: 'Effort',
  notification: 'Notification',
  notifications: 'Notifications',
  activity: 'Activity',
  activities: 'Activities',
  audit: 'Audit Log',
  health: 'Health',
  settings: 'Settings',
};

/**
 * Get display label for an entity type
 * @param entityType - The entity type
 * @param fallback - Fallback label if entity type not found
 * @returns Display label
 */
export function getEntityLabel(entityType: string, fallback?: string): string {
  return ENTITY_LABELS[entityType as EntityType] ?? fallback ?? entityType;
}

/**
 * Get both icon and label for an entity type
 * @param entityType - The entity type
 * @returns Object with icon and label
 */
export function getEntityMeta(entityType: string): { icon: string; label: string } {
  return {
    icon: getEntityIcon(entityType),
    label: getEntityLabel(entityType),
  };
}
