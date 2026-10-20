import type { Project } from '../../projects/types';
import { getProjectCapabilities } from '../../shared/state/projectCapabilities';

export interface SettingsProjectSummary {
  id: string;
  name: string;
}

/** Settings uses the same scoped permission as the project update API. */
export async function getVisibleSettingsProjects(
  projects: Project[],
  tenantId: string,
  load = getProjectCapabilities
): Promise<SettingsProjectSummary[]> {
  if (!tenantId) return [];
  const visible = await Promise.all(
    projects.map(async (project) => {
      try {
        const authority = await load(project.id, tenantId);
        if (
          authority.tenantId !== tenantId ||
          authority.projectId !== project.id ||
          !authority.canWrite ||
          !authority.permissions.includes('project:update')
        )
          return null;
        return { id: project.id, name: project.name };
      } catch {
        return null;
      }
    })
  );
  return visible.filter((project): project is SettingsProjectSummary => project !== null);
}

export function canAccessSettingsProject(
  visibleProjects: SettingsProjectSummary[],
  projectId: string | null
): boolean {
  if (!projectId) return false;
  return visibleProjects.some((project) => project.id === projectId);
}
