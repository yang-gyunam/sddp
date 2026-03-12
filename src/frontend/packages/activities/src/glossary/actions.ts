/**
 * Glossary Actions
 * Mediates between Glossary stores and GlossaryService
 */

import { getTerms } from './services/GlossaryService';
import { getGlossarySidebarState, setCategoryGroups } from './stores';
import { TERM_CATEGORIES } from './types';
import type { CategoryTermGroup, TermSummaryItem, GlossaryTermStatus } from './types';
import type { TermCategory } from './types';

export interface LoadGlossarySidebarOptions {
  append?: boolean;
  page?: number;
  pageSize?: number;
}

export interface GlossarySidebarPageResult {
  page: number;
  hasNextPage: boolean;
  totalCount: number;
}

function mapTermSummary(term: {
  id: string;
  term: string;
  abbreviation: string | null;
  category: TermCategory;
  status: string;
  version: string;
}): TermSummaryItem {
  return {
    id: term.id,
    term: term.term,
    abbreviation: term.abbreviation,
    category: term.category,
    status: term.status as GlossaryTermStatus,
    version: term.version,
  };
}

function buildCategoryGroups(groupMap: Map<TermCategory, TermSummaryItem[]>): CategoryTermGroup[] {
  return TERM_CATEGORIES
    .filter((category) => groupMap.has(category))
    .map((category) => {
      const terms = groupMap.get(category) ?? [];
      return {
        category,
        terms,
        totalCount: terms.length,
        expanded: true,
      };
    });
}

function appendTerms(
  base: Map<TermCategory, TermSummaryItem[]>,
  incoming: Map<TermCategory, TermSummaryItem[]>
): Map<TermCategory, TermSummaryItem[]> {
  const merged = new Map<TermCategory, TermSummaryItem[]>();

  for (const [category, terms] of base) {
    merged.set(category, [...terms]);
  }

  for (const [category, terms] of incoming) {
    const existing = merged.get(category) ?? [];
    const deduped = new Map(existing.map((term) => [term.id, term]));
    for (const term of terms) {
      deduped.set(term.id, term);
    }
    merged.set(category, Array.from(deduped.values()));
  }

  return merged;
}

/**
 * Load glossary sidebar data from API
 * Fetches terms and groups them by category
 */
export async function loadGlossarySidebar(
  tenantId: string,
  projectId?: string | null,
  options?: LoadGlossarySidebarOptions
): Promise<GlossarySidebarPageResult> {
  try {
    const resolvedProjectId = projectId && projectId.trim().length > 0 ? projectId : null;
    const requestedPage = options?.page ?? 1;
    const requestedPageSize = options?.pageSize ?? 20;
    const append = options?.append ?? false;
    const isSinglePageLoad = options !== undefined;

    if (!isSinglePageLoad) {
      const combinedMap = new Map<TermCategory, TermSummaryItem[]>();
      let pageNumber = 1;
      let totalCount = 0;

      while (true) {
        const page = await getTerms(tenantId, resolvedProjectId, {
          page: pageNumber,
          pageSize: 50,
        });
        totalCount = page.totalCount;

        const pageMap = new Map<TermCategory, TermSummaryItem[]>();
        for (const term of page.items) {
          const category = term.category;
          const items = pageMap.get(category) ?? [];
          items.push(mapTermSummary(term));
          pageMap.set(category, items);
        }

        const merged = appendTerms(combinedMap, pageMap);
        combinedMap.clear();
        for (const [category, terms] of merged) {
          combinedMap.set(category, terms);
        }

        const hasNextPage = page.page < page.totalPages;
        if (!hasNextPage) {
          break;
        }

        pageNumber += 1;
      }

      setCategoryGroups(buildCategoryGroups(combinedMap));
      return {
        page: pageNumber,
        hasNextPage: false,
        totalCount,
      };
    }

    const page = await getTerms(tenantId, resolvedProjectId, {
      page: requestedPage,
      pageSize: requestedPageSize,
    });

    // Group terms by category
    const groupMap = new Map<TermCategory, TermSummaryItem[]>();

    for (const term of page.items) {
      const cat = term.category;
      const terms = groupMap.get(cat) ?? [];
      terms.push(mapTermSummary(term));
      groupMap.set(cat, terms);
    }

    const state = getGlossarySidebarState();
    const existingMap = new Map<TermCategory, TermSummaryItem[]>(
      state.categoryGroups.map((group) => [group.category, group.terms])
    );
    const mergedMap = append && state.categoryGroups.length > 0
      ? appendTerms(existingMap, groupMap)
      : groupMap;

    setCategoryGroups(buildCategoryGroups(mergedMap));

    const hasNextPage = page.page < page.totalPages;
    return {
      page: page.page,
      hasNextPage,
      totalCount: page.totalCount,
    };
  } catch (error) {
    console.warn('Glossary API failed, returning empty groups:', error);
    setCategoryGroups([]);
    return {
      page: options?.page ?? 1,
      hasNextPage: false,
      totalCount: 0,
    };
  }
}
