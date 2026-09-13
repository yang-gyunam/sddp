/**
 * Auth Store - Authentication State Management
 * Stores access token in memory (NOT localStorage for security)
 * Refresh token is held only in a Secure, HttpOnly, SameSite cookie.
 */

import { createStore, config, type Store } from '../../core/services';
import type { User, AuthState, LoginRequest, LoginResponse } from '../types';

// Initial auth state - isLoading starts true until initial auth check completes
const initialAuthState: AuthState = {
  user: null,
  isAuthenticated: false,
  isLoading: true,
  sessionStatus: 'checking',
};

// Create the auth store (no persistence - access token stays in memory)
const authStore: Store<AuthState> = createStore<AuthState>(initialAuthState);

// Store access token in memory (not persisted)
let accessToken: string | null = null;
let tokenExpiresAt: number | null = null;
let refreshPromise: Promise<RefreshOutcome> | null = null;
const csrfCookieName = '__Host-sddp-csrf';

function getCsrfHeader(): Record<string, string> {
  if (typeof document === 'undefined') return {};

  const prefix = `${csrfCookieName}=`;
  const cookie = document.cookie
    .split(';')
    .map((value) => value.trim())
    .find((value) => value.startsWith(prefix));

  return cookie ? { 'X-CSRF-Token': decodeURIComponent(cookie.slice(prefix.length)) } : {};
}

/**
 * Get current auth state
 */
export function getAuthState(): AuthState {
  return authStore.get();
}

/**
 * Get access token (from memory)
 */
export function getAccessToken(): string | null {
  // Check if token is expired
  if (tokenExpiresAt && Date.now() > tokenExpiresAt) {
    accessToken = null;
    tokenExpiresAt = null;
  }
  return accessToken;
}

/**
 * Check if user is authenticated
 */
export function isAuthenticated(): boolean {
  return authStore.get().isAuthenticated && !!getAccessToken();
}

/**
 * Set loading state
 */
export function setLoading(loading: boolean): void {
  authStore.update((state) => ({
    ...state,
    isLoading: loading,
  }));
}

/**
 * Login with username and password
 */
export class TenantSelectionRequiredError extends Error {
  constructor(public readonly tenantIds: string[]) {
    super('Select a tenant to continue signing in.');
    this.name = 'TenantSelectionRequiredError';
  }
}

export async function login(credentials: LoginRequest): Promise<LoginResponse> {
  setLoading(true);

  try {
    const response = await fetch(`${config.getBaseApiUrl()}/auth/login`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      credentials: 'include',
      body: JSON.stringify(credentials),
    });

    if (!response.ok) {
      const error = (await response.json().catch(() => ({}))) as Record<string, unknown>;
      const selection = error.data as { tenantIds?: unknown } | undefined;
      if (response.status === 409 && error.errorCode === 'TENANT_SELECTION_REQUIRED' &&
          Array.isArray(selection?.tenantIds) && selection.tenantIds.length > 1 &&
          selection.tenantIds.every((id): id is string => typeof id === 'string')) {
        throw new TenantSelectionRequiredError(selection.tenantIds);
      }
      const retryAfter = response.headers?.get?.('Retry-After');
      const rateLimitMessage = retryAfter
        ? `Too many sign-in attempts. Try again in ${retryAfter} seconds.`
        : 'Too many sign-in attempts. Please try again later.';
      const message =
        (error.errorMessage as string | undefined) ||
        (error.message as string | undefined) ||
        (response.status === 429 ? rateLimitMessage : 'Login failed');
      throw new Error(message);
    }

    const json = (await response.json()) as Record<string, unknown>;

    // Handle both wrapped (ApiResponse) and direct response formats
    // Also handle both camelCase and PascalCase from backend
    const data = (json.data || json.Data) as Record<string, unknown> | undefined;

    if (!data) {
      console.error('Login response:', json);
      throw new Error('Invalid login response format');
    }

    // Handle both camelCase and PascalCase field names
    const accessTokenValue = (data.accessToken || data.AccessToken) as string | undefined;
    const expiresInValue = (data.expiresIn || data.ExpiresIn || 1800) as number;
    const userData = (data.user || data.User) as Record<string, unknown> | undefined;

    if (!accessTokenValue) {
      console.error('Login response data:', data);
      throw new Error('No access token in response');
    }

    // Store access token in memory (NOT localStorage)
    accessToken = accessTokenValue;
    tokenExpiresAt = Date.now() + expiresInValue * 1000;

    // Update auth state with user info
    // Handle both camelCase and PascalCase for user fields
    const u = userData ?? {};
    const user: User = {
      id: (u.id ?? u.Id ?? '') as string,
      username: (u.username ?? u.Username ?? '') as string,
      email: '', // Not provided in auth response
      displayName: (u.displayName ?? u.DisplayName ?? '') as string,
      tenantId: (u.tenantId ?? u.TenantId ?? '') as string,
      roles: (u.roles ?? u.Roles ?? []) as string[],
      permissions: (u.permissions ?? u.Permissions ?? []) as string[],
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
    };

    const requirePasswordChange = (data.requirePasswordChange ??
      data.RequirePasswordChange ??
      false) as boolean;

    authStore.set({
      user,
      isAuthenticated: true,
      isLoading: false,
      requirePasswordChange,
      sessionStatus: 'active',
    });

    return {
      accessToken: accessTokenValue,
      expiresIn: expiresInValue,
      user,
      requirePasswordChange,
    } as LoginResponse;
  } catch (error) {
    setLoading(false);
    throw error;
  }
}

/**
 * Logout - clear tokens and state
 */
export async function logout(): Promise<void> {
  try {
    // Call logout endpoint to invalidate refresh token
    await fetch(`${config.getBaseApiUrl()}/auth/logout`, {
      method: 'POST',
      credentials: 'include',
      headers: {
        ...getCsrfHeader(),
      },
    });
  } catch {
    // Ignore errors during logout
  } finally {
    // Clear local state regardless of server response
    accessToken = null;
    tokenExpiresAt = null;
    // Use set() instead of reset() to avoid isLoading: true from initialAuthState
    // which would cause the Loading screen to show indefinitely
    authStore.set({
      user: null,
      isAuthenticated: false,
      isLoading: false,
      requirePasswordChange: false,
      sessionStatus: 'anonymous',
    });
  }
}

/**
 * Clear local auth state without calling logout API
 * Used when refresh fails - no session exists to invalidate
 */
function clearLocalAuthState(): void {
  const sessionStatus = authStore.get().isAuthenticated ? 'expired' : 'anonymous';
  accessToken = null;
  tokenExpiresAt = null;
  // Reset to initial state but with isLoading: false (auth check complete)
  authStore.set({
    user: null,
    isAuthenticated: false,
    isLoading: false,
    requirePasswordChange: false,
    sessionStatus,
  });
}

/**
 * Refresh the in-memory access token using the HttpOnly refresh cookie.
 * The readable CSRF cookie is reflected in a request header; the refresh token
 * itself is never available to JavaScript.
 */
export type RefreshOutcome = 'authenticated' | 'rejected' | 'unavailable';

/** Statuses that mean "we could not reach the server", not "your session is over". */
function isTransientStatus(status: number): boolean {
  // 408 request timeout, 429 rate limited, 5xx server-side. A 401 or 403 is a verdict on the
  // credential and belongs in 'rejected'; anything else unexpected is treated as a verdict too,
  // because keeping a session alive on an answer we cannot read is the riskier default.
  return status === 408 || status === 429 || status >= 500;
}

/**
 * Records that the server could not be reached, without touching the session.
 *
 * The access token has already expired by the time this runs, so requests keep failing -- but the
 * user stays signed in, their tabs stay open, and the next attempt (tab wake, next 401) can
 * succeed without a re-login.
 */
function markSessionUnavailable(): void {
  authStore.update((state) => ({
    ...state,
    isLoading: false,
    sessionStatus: state.isAuthenticated ? 'unavailable' : state.sessionStatus,
  }));
}

async function performTokenRefresh(): Promise<RefreshOutcome> {
  // Login sets the readable CSRF cookie alongside the HttpOnly refresh cookie, so its absence
  // means there is no session to resume. Asking anyway is a guaranteed 403 — the server
  // rejects a refresh with no CSRF header — which logged a console error on every cold load
  // and buried real 403s in the noise.
  const csrfHeader = getCsrfHeader();
  if (Object.keys(csrfHeader).length === 0) {
    // Retrying will not produce a cookie. This is "there is no session", not "cannot reach it".
    clearLocalAuthState();
    return 'rejected';
  }

  try {
    const response = await fetch(`${config.getBaseApiUrl()}/auth/refresh`, {
      method: 'POST',
      headers: csrfHeader,
      credentials: 'include',
    });

    if (!response.ok) {
      if (isTransientStatus(response.status)) {
        markSessionUnavailable();
        return 'unavailable';
      }

      clearLocalAuthState();
      return 'rejected';
    }

    const json = (await response.json()) as Record<string, unknown>;

    // Handle both wrapped (ApiResponse) and direct response formats
    const data = (json.data || json.Data) as Record<string, unknown> | undefined;

    if (!data) {
      console.error('Refresh response:', json);
      clearLocalAuthState();
      return 'rejected';
    }

    // Handle both camelCase and PascalCase field names
    const accessTokenValue = (data.accessToken || data.AccessToken) as string | undefined;
    const expiresInValue = (data.expiresIn || data.ExpiresIn || 1800) as number;
    const userData = (data.user || data.User) as Record<string, unknown> | undefined;
    const requirePasswordChange = (data.requirePasswordChange ??
      data.RequirePasswordChange ??
      authStore.get().requirePasswordChange ??
      false) as boolean;

    if (!accessTokenValue) {
      console.error('Refresh response data:', data);
      clearLocalAuthState();
      return 'rejected';
    }

    // Update access token in memory
    accessToken = accessTokenValue;
    tokenExpiresAt = Date.now() + expiresInValue * 1000;

    // Update user if provided
    if (userData) {
      const user: User = {
        id: (userData.id as string) || (userData.Id as string) || '',
        username: (userData.username as string) || (userData.Username as string) || '',
        email: '', // Not provided in auth response
        displayName: (userData.displayName as string) || (userData.DisplayName as string) || '',
        tenantId: (userData.tenantId as string) || (userData.TenantId as string) || '',
        roles: (userData.roles as string[]) || (userData.Roles as string[]) || [],
        permissions: (userData.permissions as string[]) || (userData.Permissions as string[]) || [],
        createdAt: new Date().toISOString(),
        updatedAt: new Date().toISOString(),
      };

      authStore.set({
        user,
        isAuthenticated: true,
        isLoading: false,
        requirePasswordChange,
        sessionStatus: 'active',
      });
    } else {
      // No user data but token is valid - just mark as not loading
      authStore.update((state) => ({
        ...state,
        isLoading: false,
        requirePasswordChange,
        sessionStatus: 'active',
      }));
    }

    return 'authenticated';
  } catch {
    markSessionUnavailable();
    return 'unavailable';
  }
}

/** Coalesce every bootstrap, visibility and API-triggered refresh into one request. */
export function refreshToken(): Promise<RefreshOutcome> {
  if (!refreshPromise) {
    refreshPromise = performTokenRefresh().finally(() => {
      refreshPromise = null;
    });
  }

  return refreshPromise;
}

/**
 * Subscribe to auth state changes
 */
export function subscribeAuth(
  listener: (state: AuthState, prevState: AuthState) => void
): () => void {
  return authStore.subscribe(listener);
}

/**
 * Clear require password change flag (after successful password change)
 */
export function clearRequirePasswordChange(): void {
  authStore.update((state) => ({
    ...state,
    requirePasswordChange: false,
  }));
}

/**
 * Change current user's password.
 * If access token is expired, refresh once and retry.
 */
export async function changePassword(currentPassword: string, newPassword: string): Promise<void> {
  const doRequest = (): Promise<Response> => {
    const token = getAccessToken();
    return fetch(`${config.getBaseApiUrl()}/auth/change-password`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        ...(token ? { Authorization: `Bearer ${token}` } : {}),
      },
      credentials: 'include',
      body: JSON.stringify({ currentPassword, newPassword }),
    });
  };

  let response = await doRequest();

  if (response.status === 401) {
    if ((await refreshToken()) === 'authenticated') {
      response = await doRequest();
    }
  }

  if (!response.ok) {
    const data = await response.json().catch(() => ({}));
    const message = data.errorMessage || data.message || 'Failed to change password';
    throw new Error(message);
  }
}

/**
 * Update user profile
 */
export function updateUser(user: Partial<User>): void {
  authStore.update((state) => ({
    ...state,
    user: state.user ? { ...state.user, ...user } : null,
  }));
}

/**
 * Check if user has a specific permission
 */
export function hasPermission(permission: string): boolean {
  const { user } = authStore.get();
  return user?.permissions?.includes(permission) ?? false;
}

/**
 * Check if user has a specific role
 */
export function hasRole(role: string): boolean {
  const { user } = authStore.get();
  return user?.roles?.includes(role) ?? false;
}

// Export the store for direct access in Svelte components
export { authStore };
