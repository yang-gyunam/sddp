/**
 * Shared API infrastructure for all activity services.
 * Provides authenticated fetch helper and common types.
 */

import { config } from '@sddp/shell/core';
import {
  getAccessToken,
  getAuthState,
  refreshToken,
  type RefreshOutcome,
} from '@sddp/shell/auth/stores';

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
  /** Additional request headers such as If-Match. Auth and scope headers cannot be overridden. */
  headers?: Record<string, string>;
  /** Multipart upload body. When set, Content-Type is left to the browser (boundary) and `body` is ignored. */
  formData?: FormData;
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

/** Thrown when the server rejected the refresh credential: the session really is over. */
export class AuthExpiredError extends Error {
  constructor(message: string = 'Session expired') {
    super(message);
    this.name = 'AuthExpiredError';
  }
}

export class AuthUnavailableError extends Error {
  constructor(message: string = 'Cannot reach the server. Your session is still open.') {
    super(message);
    this.name = 'AuthUnavailableError';
  }
}

/** Structured non-authentication API failure with the server error contract attached. */
export class ApiRequestError extends Error {
  constructor(
    message: string,
    public readonly status: number,
    public readonly errorCode?: string,
    public readonly details?: Record<string, unknown>
  ) {
    super(message);
    this.name = 'ApiRequestError';
  }
}

function tryRefreshToken(): Promise<RefreshOutcome> {
  return refreshToken();
}

/** Turns a non-success outcome into the error that says which of the two things happened. */
function refreshFailure(outcome: RefreshOutcome): Error {
  return outcome === 'unavailable'
    ? new AuthUnavailableError()
    : new AuthExpiredError('Session expired. Please log in again.');
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
  body: unknown | undefined,
  formData?: FormData
): Promise<Response> {
  return fetch(`${config.getBaseApiUrl()}${endpoint}`, {
    method,
    headers,
    credentials: 'include',
    body: formData ?? (body !== undefined ? JSON.stringify(body) : undefined),
  });
}

/**
 * Build request headers with current auth token and tenant context.
 */
function buildHeaders(options: FetchOptions, accessToken: string): Record<string, string> {
  const tenantId = options.tenantId || getAuthState().user?.tenantId || '';
  if (!tenantId) {
    throw new Error('No tenant ID available');
  }

  const headers: Record<string, string> = {
    ...options.headers,
    Authorization: `Bearer ${accessToken}`,
    'X-Tenant-Id': tenantId,
  };

  // For multipart uploads the browser must set Content-Type (with boundary) itself
  if (!options.formData) {
    headers['Content-Type'] = 'application/json';
  }

  if (options.projectId) {
    headers['X-Project-Id'] = options.projectId;
  }

  return headers;
}

async function getAuthenticatedHeaders(options: FetchOptions): Promise<Record<string, string>> {
  let accessToken = getAccessToken();

  if (!accessToken) {
    const outcome = await tryRefreshToken();
    if (outcome !== 'authenticated') {
      throw refreshFailure(outcome);
    }

    accessToken = getAccessToken();
    if (!accessToken) {
      throw new AuthExpiredError('Session refresh did not provide an access token.');
    }
  }

  return buildHeaders(options, accessToken);
}

/**
 * Parse error response body safely.
 */
async function parseErrorResponse(response: Response): Promise<{
  errorMessage?: string;
  message?: string;
  errorCode?: string;
  details?: Record<string, unknown>;
}> {
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
export async function fetchWithAuth<T>(endpoint: string, options: FetchOptions = {}): Promise<T> {
  const { method = 'GET', body, formData } = options;
  const headers = await getAuthenticatedHeaders(options);

  const response = await doFetch(endpoint, method, headers, body, formData);

  // 401 Unauthorized → attempt token refresh and retry once
  if (response.status === 401) {
    const outcome = await tryRefreshToken();
    if (outcome === 'authenticated') {
      // Retry with fresh token
      const retryHeaders = await getAuthenticatedHeaders(options);
      const retryResponse = await doFetch(endpoint, method, retryHeaders, body, formData);
      return handleResponse<T>(retryResponse);
    }
    throw refreshFailure(outcome);
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
      throw new AccessDeniedError(error.errorMessage || error.message || 'Access denied');
    }

    throw new ApiRequestError(
      error.errorMessage || error.message || `Request failed: ${response.status}`,
      response.status,
      error.errorCode,
      error.details
    );
  }

  if (response.status === 204 || response.status === 205) {
    return undefined as T;
  }

  const responseText = await response.text();
  if (!responseText.trim()) {
    return undefined as T;
  }

  let json: ApiResponse<T>;
  try {
    json = JSON.parse(responseText) as ApiResponse<T>;
  } catch {
    throw new Error(`Invalid JSON response: ${response.status}`);
  }

  if (!json.success && (json.errorMessage || json.message)) {
    throw new Error(json.errorMessage || json.message);
  }

  return json.data;
}
