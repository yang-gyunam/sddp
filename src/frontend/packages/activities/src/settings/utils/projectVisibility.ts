import type { Project } from '../../projects/types';

export interface SettingsProjectSummary {
  id: string;
  name: string;
}

export interface ProjectVisibilityParams {
  projects: Project[];
  currentUserId: string;
  isAdmin: boolean;
  isProductOwner: boolean;
}

function resolveProjectOwnerId(project: Project): string {
  const ownerRef = (project as Project & { owner?: { id?: string } }).owner;
  return project.ownerId || ownerRef?.id || '';
}

/**
 * Returns projects that can be shown under Settings > Project.
 * - Admin: all projects
 * - Non-admin Product Owner: only projects owned by current user
 * - Other non-admin roles: none
 */
export function getVisibleSettingsProjects({
  projects,
  currentUserId,
  isAdmin,
  isProductOwner,
}: ProjectVisibilityParams): SettingsProjectSummary[] {
  if (isAdmin) {
    return projects.map(({ id, name }) => ({ id, name }));
  }

  if (!isProductOwner || !currentUserId) {
    return [];
  }

  const visibleProjects = projects.filter((project) => resolveProjectOwnerId(project) === currentUserId);

  return visibleProjects.map(({ id, name }) => ({ id, name }));
}

export function canAccessSettingsProject(
  visibleProjects: SettingsProjectSummary[],
  projectId: string | null
): boolean {
  if (!projectId) return false;
  return visibleProjects.some((project) => project.id === projectId);
}
