/**
 * Search Types
 * Type definitions for Search Activity
 */

// Entity Types
export type EntityType =
  | 'conversation'
  | 'requirement'
  | 'spec'
  | 'task'
  | 'glossary'
  | 'artifact';

// Search Sort Options
export type SearchSort =
  | 'relevance'
  | 'date-desc'
  | 'date-asc'
  | 'title'
  | 'project';

// Date Range
export interface DateRange {
  from: string; // ISO8601
  to: string; // ISO8601
}

// Search Filters
export interface SearchFilters {
  types: EntityType[];
  projectIds: string[];
  statuses: string[];
  dateRange?: DateRange;
  authorId?: string;
  modifiedById?: string;
  hasAttachments?: boolean;
  hasRelationships?: boolean;
}

// Search Query
export interface SearchQuery {
  text: string;
  filters: SearchFilters;
  sort: SearchSort;
  page: number;
  pageSize: number;
}

// Search Result Item
export interface SearchResultItem {
  id: string;
  type: EntityType;
  projectId: string;
  projectName: string;
  title: string;
  snippet: string; // Highlighted text
  highlights: string[]; // Matched text fragments
  metadata: Record<string, unknown>; // Type-specific metadata
  score: number; // Relevance score
  createdAt: string;
  updatedAt: string;
}

// Search Facets (filter counts)
export interface SearchFacets {
  types: Partial<Record<EntityType, number>>;
  projects: Record<string, number>;
  statuses: Record<string, number>;
}

// Search Result
export interface SearchResult {
  total: number;
  results: SearchResultItem[];
  facets: SearchFacets;
}

// Saved Search
export interface SavedSearch {
  id: string;
  userId: string;
  name: string;
  query: SearchQuery;
  createdAt: string;
  lastUsedAt: string;
}

// Advanced Search Options
export interface AdvancedSearchOptions {
  allWords: string;
  exactPhrase: string;
  anyWords: string;
  noneWords: string;
  titleContains: string;
  authorId?: string;
  dateRange?: DateRange;
}
