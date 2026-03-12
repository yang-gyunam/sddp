// Core Types

/**
 * Generic result type for operations that can fail
 */
export type Result<T, E = Error> =
  | { success: true; data: T }
  | { success: false; error: E };

/**
 * Create a success result
 */
export function ok<T>(data: T): Result<T, never> {
  return { success: true, data };
}

/**
 * Create a failure result
 */
export function err<E>(error: E): Result<never, E> {
  return { success: false, error };
}

/**
 * Async version of Result
 */
export type AsyncResult<T, E = Error> = Promise<Result<T, E>>;

/**
 * Nullable type helper
 */
export type Nullable<T> = T | null;

/**
 * Optional type helper
 */
export type Optional<T> = T | undefined;

/**
 * Deep partial type helper
 */
export type DeepPartial<T> = T extends object
  ? {
      [P in keyof T]?: DeepPartial<T[P]>;
    }
  : T;

/**
 * Dictionary type helper
 */
export type Dictionary<T> = Record<string, T>;

/**
 * Common ID type
 */
export type Id = string;

/**
 * Timestamp type (ISO 8601 string)
 */
export type Timestamp = string;

/**
 * Base entity interface
 */
export interface BaseEntity {
  id: Id;
  createdAt: Timestamp;
  updatedAt: Timestamp;
}

/**
 * Auditable entity interface
 */
export interface AuditableEntity extends BaseEntity {
  createdBy: Id;
  updatedBy: Id;
}

/**
 * Versioned entity interface
 */
export interface VersionedEntity extends BaseEntity {
  version: number;
}

/**
 * Pagination request
 */
export interface PaginationRequest {
  page: number;
  pageSize: number;
  sortBy?: string;
  sortOrder?: 'asc' | 'desc';
}

/**
 * Paginated response
 */
export interface PaginatedResponse<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

/**
 * API error response
 */
export interface ApiError {
  code: string;
  message: string;
  details?: Record<string, unknown>;
  traceId?: string;
}
