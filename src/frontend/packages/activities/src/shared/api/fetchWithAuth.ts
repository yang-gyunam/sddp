/**
 * Shared API infrastructure for all activity services.
 * Provides authenticated fetch helper and common types.
 */

import { config } from '@sddp/shell/core';
import { getAccessToken, getAuthState, refreshToken } from '@sddp/shell/auth/stores';

// ============================================
// Types
// ============================================

export interface ApiResponse<T> {
  success: boolean;
  data: T;
  message?: string;
  errorCode?: string;
  errorMessage?: string;
  errors?: Record<string, string[]>;
}

export interface FetchOptions {
  method?: string;
  body?: unknown;
  /** Optional: If not provided, will be auto-resolved from auth state */
  tenantId?: string;
  projectId?: string;
}

// ============================================
// Error Classes
// ============================================

/** Thrown when the server returns 403 Forbidden */
export class AccessDeniedError extends Error {
  constructor(message: string = 'Access denied') {
    super(message);
    this.name = 'AccessDeniedError';
  }
}

/** Thrown when token refresh fails and the session is expired */
export class AuthExpiredError extends Error {
  constructor(message: string = 'Session expired') {
    super(message);
    this.name = 'AuthExpiredError';
  }
}

// ============================================
// Token Refresh Singleton
// ============================================

/** Shared promise to coalesce concurrent 401 refresh attempts */
let refreshPromise: Promise<boolean> | null = null;

function tryRefreshToken(): Promise<boolean> {
  if (!refreshPromise) {
    refreshPromise = refreshToken().finally(() => {
      refreshPromise = null;
    });
  }
  return refreshPromise;
}

// ============================================
// Helper Functions
// ============================================

/**
 * Execute a fetch request with the given headers.
 * Extracted to allow retry with fresh token.
 */
function doFetch(
  endpoint: string,
  method: string,
  headers: Record<string, string>,
  body: unknown | undefined
): Promise<Response> {
  return fetch(`${config.getBaseApiUrl()}${endpoint}`, {
    method,
    headers,
    credentials: 'include',
    body: body ? JSON.stringify(body) : undefined,
  });
}

/**
 * Build request headers with current auth token and tenant context.
 */
function buildHeaders(options: FetchOptions): Record<string, string> {
  const accessToken = getAccessToken();
  if (!accessToken) {
    throw new Error('Not authenticated');
  }

  const tenantId = options.tenantId || getAuthState().user?.tenantId || '';
  if (!tenantId) {
    throw new Error('No tenant ID available');
  }

  const headers: Record<string, string> = {
    'Content-Type': 'application/json',
    Authorization: `Bearer ${accessToken}`,
    'X-Tenant-Id': tenantId,
  };

  if (options.projectId) {
    headers['X-Project-Id'] = options.projectId;
  }

  return headers;
}

/**
 * Parse error response body safely.
 */
async function parseErrorResponse(response: Response): Promise<{ errorMessage?: string; message?: string }> {
  return response.json().catch(() => ({
    errorMessage: `Request failed: ${response.status}`,
  }));
}

/**
 * Make an authenticated API request.
 * Automatically includes JWT token, tenant and project context headers.
 * If tenantId is omitted, it will be auto-resolved from auth state.
 * If projectId is omitted, X-Project-Id header is not sent (global query).
 *
 * On 401 Unauthorized: automatically attempts token refresh and retries once.
 * If refresh fails, throws AuthExpiredError and clears auth state.
 */
export async function fetchWithAuth<T>(
  endpoint: string,
  options: FetchOptions = {}
): Promise<T> {
  const { method = 'GET', body } = options;
  const headers = buildHeaders(options);

  const response = await doFetch(endpoint, method, headers, body);

  // 401 Unauthorized → attempt token refresh and retry once
  if (response.status === 401) {
    const refreshed = await tryRefreshToken();
    if (refreshed) {
      // Retry with fresh token
      const retryHeaders = buildHeaders(options);
      const retryResponse = await doFetch(endpoint, method, retryHeaders, body);
      return handleResponse<T>(retryResponse);
    }
    // Refresh failed — session is expired
    throw new AuthExpiredError('Session expired. Please log in again.');
  }

  return handleResponse<T>(response);
}

/**
 * Handle response: check status, parse JSON, unwrap ApiResponse.
 */
async function handleResponse<T>(response: Response): Promise<T> {
  if (!response.ok) {
    const error = await parseErrorResponse(response);

    if (response.status === 403) {
      throw new AccessDeniedError(
        error.errorMessage || error.message || 'Access denied'
      );
    }

    throw new Error(error.errorMessage || error.message || `Request failed: ${response.status}`);
  }

  const json: ApiResponse<T> = await response.json();

  if (!json.success && (json.errorMessage || json.message)) {
    throw new Error(json.errorMessage || json.message);
  }

  return json.data;
}
