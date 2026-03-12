/**
 * Search Service
 * Handles search API calls with fallback to mock data
 */

import { fetchWithAuth } from '../../shared/api';
import type {
  EntityType,
  SearchFacets,
  SearchQuery,
  SearchResult,
  SearchResultItem,
  SavedSearch,
} from '../types';

// ============================================
// API Response Types
// ============================================

interface VectorSearchResultDto {
  id: string;
  sourceType: string;
  sourceId: string;
  chunkText: string;
  score: number;
  conversationId?: string;
  specVersion?: string;
  messageType?: string;
}

// ============================================
// Service
// ============================================

class SearchService {
  private tenantId = '';
  private projectId?: string;

  setContext(tenantId: string, projectId?: string): void {
    this.tenantId = tenantId;
    this.projectId = projectId;
  }

  /**
   * Execute search query
   */
  async search(query: SearchQuery): Promise<SearchResult> {
    if (!query.text.trim()) {
      return { total: 0, results: [], facets: { types: {}, projects: {}, statuses: {} } };
    }

    try {
      const sourceTypes = query.filters.types.length > 0 ? query.filters.types : undefined;

      const results = await fetchWithAuth<VectorSearchResultDto[]>('/search', {
        method: 'POST',
        tenantId: this.tenantId,
        projectId: this.projectId,
        body: {
          text: query.text,
          sourceTypes,
          topK: query.pageSize,
          threshold: 0.5,
        },
      });

      // Transform vector results to SearchResultItem format
      const items: SearchResultItem[] = results.map((r) => ({
        id: r.sourceId,
        type: (r.sourceType as SearchResultItem['type']) || 'spec',
        projectId: this.projectId ?? '',
        projectName: '',
        title: r.chunkText.substring(0, 80),
        snippet: r.chunkText,
        highlights: [],
        metadata: {
          status: '',
          version: r.specVersion,
          messageType: r.messageType,
        },
        score: r.score,
        createdAt: new Date().toISOString(),
        updatedAt: new Date().toISOString(),
      }));

      const facets = this.calculateFacets(items);

      return {
        total: items.length,
        results: items,
        facets,
      };
    } catch (error) {
      const message = error instanceof Error ? error.message : 'Search failed';
      console.error('Search API failed:', message);
      return { total: 0, results: [], facets: { types: {}, projects: {}, statuses: {} } };
    }
  }

  /**
   * Get saved searches (localStorage-based for now)
   */
  async getSavedSearches(): Promise<SavedSearch[]> {
    try {
      const stored = localStorage.getItem('sddp-saved-searches');
      if (stored) {
        return JSON.parse(stored);
      }
    } catch {
      // Ignore parse errors
    }
    return [];
  }

  /**
   * Save a search query
   */
  async saveSearch(name: string, query: SearchQuery): Promise<SavedSearch> {
    const saved: SavedSearch = {
      id: `saved-${Date.now()}`,
      userId: 'current-user',
      name,
      query,
      createdAt: new Date().toISOString(),
      lastUsedAt: new Date().toISOString(),
    };

    const existing = await this.getSavedSearches();
    existing.push(saved);
    localStorage.setItem('sddp-saved-searches', JSON.stringify(existing));

    return saved;
  }

  /**
   * Delete a saved search
   */
  async deleteSavedSearch(id: string): Promise<void> {
    const existing = await this.getSavedSearches();
    const filtered = existing.filter((s) => s.id !== id);
    localStorage.setItem('sddp-saved-searches', JSON.stringify(filtered));
  }

  /**
   * Update saved search last used timestamp
   */
  async updateSavedSearchLastUsed(id: string): Promise<void> {
    const existing = await this.getSavedSearches();
    const search = existing.find((s) => s.id === id);
    if (search) {
      search.lastUsedAt = new Date().toISOString();
      localStorage.setItem('sddp-saved-searches', JSON.stringify(existing));
    }
  }

  // ============================================
  // Helpers
  // ============================================

  private calculateFacets(results: SearchResultItem[]): SearchFacets {
    const types: Partial<Record<EntityType, number>> = {};
    const projects: Record<string, number> = {};
    const statuses: Record<string, number> = {};

    results.forEach((result) => {
      types[result.type] = (types[result.type] || 0) + 1;
      projects[result.projectId] = (projects[result.projectId] || 0) + 1;
      const status = result.metadata.status as string | undefined;
      if (status) {
        statuses[status] = (statuses[status] || 0) + 1;
      }
    });

    return { types, projects, statuses };
  }

}

// Singleton instance
let searchServiceInstance: SearchService | null = null;

/**
 * Get the singleton SearchService instance
 */
export function getSearchService(): SearchService {
  if (!searchServiceInstance) {
    searchServiceInstance = new SearchService();
  }
  return searchServiceInstance;
}

/**
 * Reset the singleton instance (for testing/logout)
 */
export function resetSearchService(): void {
  searchServiceInstance = null;
}

export { SearchService };
export default getSearchService();
