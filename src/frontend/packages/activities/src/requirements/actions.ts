/**
 * Requirement Actions
 * Mediates between Requirement stores and RequirementService
 */

import { getRequirementService } from './services';
import { getRequirementSidebarState, setProjectGroups } from './stores';
import type { ProjectRequirementGroup, RequirementSummary } from './types';

export interface LoadRequirementsSidebarOptions {
  append?: boolean;
  page?: number;
  pageSize?: number;
}

export interface RequirementsSidebarPageResult {
  page: number;
  hasNextPage: boolean;
  totalCount: number;
}

function mapRequirementSummary(req: {
  id: string;
  code: string;
  title: string;
  level: string;
  status: string;
  childrenCount: number;
}): RequirementSummary {
  return {
    id: req.id,
    code: req.code,
    title: req.title,
    level: req.level as RequirementSummary['level'],
    status: req.status as RequirementSummary['status'],
    childrenCount: req.childrenCount,
    hasChildren: req.childrenCount > 0,
  };
}

function mergeSummaries(
  existing: RequirementSummary[],
  incoming: RequirementSummary[]
): RequirementSummary[] {
  const map = new Map<string, RequirementSummary>();
  for (const item of existing) {
    map.set(item.id, item);
  }
  for (const item of incoming) {
    map.set(item.id, item);
  }
  return Array.from(map.values());
}

function buildGroup(
  projectId: string,
  requirements: RequirementSummary[],
  totalCount: number
): ProjectRequirementGroup {
  return {
    projectId,
    projectName: projectId,
    projectCode: projectId,
    requirements,
    totalCount,
    expanded: true,
  };
}

/**
 * Load requirement sidebar data from API
 * Fetches requirements and groups them by project
 */
export async function loadRequirementsSidebar(
  tenantId: string,
  projectId: string,
  options?: LoadRequirementsSidebarOptions
): Promise<RequirementsSidebarPageResult> {
  try {
    const service = getRequirementService();
    service.setContext(tenantId, projectId);

    const requestedPage = options?.page ?? 1;
    const requestedPageSize = options?.pageSize ?? 20;
    const append = options?.append ?? false;
    const isSinglePageLoad = options !== undefined;

    if (!isSinglePageLoad) {
      const collected: RequirementSummary[] = [];
      let pageNumber = 1;
      let totalCount = 0;

      while (true) {
        const page = await service.getRequirements({ page: pageNumber, pageSize: 50 });
        totalCount = page.totalCount;
        collected.push(...page.items.map(mapRequirementSummary));

        const hasNextPage = page.page < page.totalPages;
        if (!hasNextPage) {
          break;
        }

        pageNumber += 1;
      }

      setProjectGroups([buildGroup(projectId, collected, totalCount)]);
      return {
        page: pageNumber,
        hasNextPage: false,
        totalCount,
      };
    }

    const page = await service.getRequirements({ page: requestedPage, pageSize: requestedPageSize });
    const summaries = page.items.map(mapRequirementSummary);
    const state = getRequirementSidebarState();
    const existingGroup = state.projectGroups.find((group) => group.projectId === projectId);
    const mergedSummaries = append && existingGroup
      ? mergeSummaries(existingGroup.requirements, summaries)
      : summaries;

    setProjectGroups([buildGroup(projectId, mergedSummaries, page.totalCount)]);

    const hasNextPage = page.page < page.totalPages;
    return {
      page: page.page,
      hasNextPage,
      totalCount: page.totalCount,
    };
  } catch (error) {
    const message = error instanceof Error ? error.message : 'Failed to load requirements';
    console.error('Requirement API failed:', message);
    setProjectGroups([]);
    return {
      page: options?.page ?? 1,
      hasNextPage: false,
      totalCount: 0,
    };
  }
}
