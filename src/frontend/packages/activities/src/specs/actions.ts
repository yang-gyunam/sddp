/**
 * Spec Actions
 * Mediates between Spec stores and SpecService
 */

import { getSpecService } from './services';
import { getSpecSidebarState, setProjectGroups } from './stores';
import type { ProjectSpecGroup, SpecSummary } from './types';

export interface LoadSpecsSidebarOptions {
  append?: boolean;
  page?: number;
  pageSize?: number;
}

export interface SpecsSidebarPageResult {
  page: number;
  hasNextPage: boolean;
  totalCount: number;
}

function mapSpecSummary(spec: {
  id: string;
  code: string;
  title: string;
  status: string;
  version: string;
}): SpecSummary {
  return {
    id: spec.id,
    code: spec.code,
    title: spec.title,
    status: spec.status as SpecSummary['status'],
    version: spec.version,
    linkedRequirementCode: undefined,
  };
}

function mergeSummaries(existing: SpecSummary[], incoming: SpecSummary[]): SpecSummary[] {
  const map = new Map<string, SpecSummary>();
  for (const item of existing) {
    map.set(item.id, item);
  }
  for (const item of incoming) {
    map.set(item.id, item);
  }
  return Array.from(map.values());
}

function buildGroup(projectId: string, specs: SpecSummary[], totalCount: number): ProjectSpecGroup {
  return {
    projectId,
    projectName: projectId,
    projectCode: projectId,
    specs,
    totalCount,
    expanded: true,
  };
}

/**
 * Load spec sidebar data from API
 * Fetches specs and groups them by project
 */
export async function loadSpecsSidebar(
  tenantId: string,
  projectId: string,
  options?: LoadSpecsSidebarOptions
): Promise<SpecsSidebarPageResult> {
  try {
    const service = getSpecService();
    service.setContext(tenantId, projectId);

    const requestedPage = options?.page ?? 1;
    const requestedPageSize = options?.pageSize ?? 20;
    const append = options?.append ?? false;
    const isSinglePageLoad = options !== undefined;

    if (!isSinglePageLoad) {
      const collected: SpecSummary[] = [];
      let pageNumber = 1;
      let totalCount = 0;

      while (true) {
        const page = await service.getSpecs({ page: pageNumber, pageSize: 50 });
        totalCount = page.totalCount;
        collected.push(...page.items.map(mapSpecSummary));

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

    const page = await service.getSpecs({ page: requestedPage, pageSize: requestedPageSize });
    const summaries = page.items.map(mapSpecSummary);
    const state = getSpecSidebarState();
    const existingGroup = state.projectGroups.find((group) => group.projectId === projectId);
    const mergedSummaries = append && existingGroup
      ? mergeSummaries(existingGroup.specs, summaries)
      : summaries;

    setProjectGroups([buildGroup(projectId, mergedSummaries, page.totalCount)]);

    const hasNextPage = page.page < page.totalPages;
    return {
      page: page.page,
      hasNextPage,
      totalCount: page.totalCount,
    };
  } catch (error) {
    console.warn('Spec API failed, returning empty groups:', error);
    setProjectGroups([]);
    return {
      page: options?.page ?? 1,
      hasNextPage: false,
      totalCount: 0,
    };
  }
}
