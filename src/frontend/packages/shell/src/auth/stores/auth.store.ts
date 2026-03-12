/**
 * Auth Store - Authentication State Management
 * Stores access token in memory (NOT localStorage for security)
 * Refresh token is stored in sessionStorage for per-tab isolation
 * (sessionStorage is tab-scoped, preventing cross-tab session contamination)
 */

import { createStore, config, type Store } from '../../core/services';
import type { User, AuthState, LoginRequest, LoginResponse } from '../types';

// Initial auth state - isLoading starts true until initial auth check completes
const initialAuthState: AuthState = {
  user: null,
  isAuthenticated: false,
  isLoading: true,
};

// Create the auth store (no persistence - access token stays in memory)
const authStore: Store<AuthState> = createStore<AuthState>(initialAuthState);

// Store access token in memory (not persisted)
let accessToken: string | null = null;
let tokenExpiresAt: number | null = null;

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
 * Check if a valid session exists for this tab.
 * sessionStorage is per-tab and cleared when the tab closes,
 * so only this tab can attempt silent refresh.
 */
export function hasSession(): boolean {
  return sessionStorage.getItem('sddp-session') === 'active';
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
      const error = await response.json().catch(() => ({ message: 'Login failed' }));
      throw new Error(error.message || 'Login failed');
    }

    const json = await response.json() as Record<string, unknown>;

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

    // Store refresh token in sessionStorage (tab-scoped)
    // Each tab keeps its own refresh token, preventing cross-tab session contamination
    const refreshTokenValue = (data.refreshToken || data.RefreshToken) as string | undefined;
    if (refreshTokenValue) {
      sessionStorage.setItem('sddp-refresh-token', refreshTokenValue);
    }

    // Mark session active in sessionStorage (tab-scoped guard)
    sessionStorage.setItem('sddp-session', 'active');
    sessionStorage.setItem('sddp-session-user', (userData?.id || userData?.Id || '') as string);

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

    const requirePasswordChange = (data.requirePasswordChange ?? data.RequirePasswordChange ?? false) as boolean;

    authStore.set({
      user,
      isAuthenticated: true,
      isLoading: false,
      requirePasswordChange,
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
    const storedRefreshToken = sessionStorage.getItem('sddp-refresh-token');

    // Call logout endpoint to invalidate refresh token
    await fetch(`${config.getBaseApiUrl()}/auth/logout`, {
      method: 'POST',
      credentials: 'include',
      headers: {
        'Content-Type': 'application/json',
        Authorization: accessToken ? `Bearer ${accessToken}` : '',
      },
      body: storedRefreshToken
        ? JSON.stringify({ refreshToken: storedRefreshToken })
        : undefined,
    });
  } catch {
    // Ignore errors during logout
  } finally {
    // Clear local state regardless of server response
    accessToken = null;
    tokenExpiresAt = null;
    sessionStorage.removeItem('sddp-refresh-token');
    sessionStorage.removeItem('sddp-session');
    sessionStorage.removeItem('sddp-session-user');
    // Use set() instead of reset() to avoid isLoading: true from initialAuthState
    // which would cause the Loading screen to show indefinitely
    authStore.set({
      user: null,
      isAuthenticated: false,
      isLoading: false,
      requirePasswordChange: false,
    });
  }
}

/**
 * Clear local auth state without calling logout API
 * Used when refresh fails - no session exists to invalidate
 */
function clearLocalAuthState(): void {
  accessToken = null;
  tokenExpiresAt = null;
  sessionStorage.removeItem('sddp-refresh-token');
  sessionStorage.removeItem('sddp-session');
  sessionStorage.removeItem('sddp-session-user');
  // Reset to initial state but with isLoading: false (auth check complete)
  authStore.set({
    user: null,
    isAuthenticated: false,
    isLoading: false,
    requirePasswordChange: false,
  });
}

/**
 * Refresh access token using refresh token stored in sessionStorage (per-tab).
 * Sends refresh token via request body instead of relying on HttpOnly cookie,
 * ensuring each tab uses its own refresh token for session isolation.
 */
export async function refreshToken(): Promise<boolean> {
  try {
    // Read this tab's refresh token from sessionStorage
    const storedRefreshToken = sessionStorage.getItem('sddp-refresh-token');
    if (!storedRefreshToken) {
      clearLocalAuthState();
      return false;
    }

    const response = await fetch(`${config.getBaseApiUrl()}/auth/refresh`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      credentials: 'include',
      body: JSON.stringify({ refreshToken: storedRefreshToken }),
    });

    if (!response.ok) {
      clearLocalAuthState();
      return false;
    }

    const json = await response.json() as Record<string, unknown>;

    // Handle both wrapped (ApiResponse) and direct response formats
    const data = (json.data || json.Data) as Record<string, unknown> | undefined;

    if (!data) {
      console.error('Refresh response:', json);
      clearLocalAuthState();
      return false;
    }

    // Handle both camelCase and PascalCase field names
    const accessTokenValue = (data.accessToken || data.AccessToken) as string | undefined;
    const expiresInValue = (data.expiresIn || data.ExpiresIn || 1800) as number;
    const userData = (data.user || data.User) as Record<string, unknown> | undefined;
    const newRefreshToken = (data.refreshToken || data.RefreshToken) as string | undefined;
    const requirePasswordChange = (
      data.requirePasswordChange
      ?? data.RequirePasswordChange
      ?? authStore.get().requirePasswordChange
      ?? false
    ) as boolean;

    if (!accessTokenValue) {
      console.error('Refresh response data:', data);
      clearLocalAuthState();
      return false;
    }

    // Update access token in memory
    accessToken = accessTokenValue;
    tokenExpiresAt = Date.now() + expiresInValue * 1000;

    // Store rotated refresh token in sessionStorage (per-tab)
    if (newRefreshToken) {
      sessionStorage.setItem('sddp-refresh-token', newRefreshToken);
    }

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

      // Update session user ID for consistency
      sessionStorage.setItem('sddp-session-user', user.id);

      authStore.set({
        user,
        isAuthenticated: true,
        isLoading: false,
        requirePasswordChange,
      });
    } else {
      // No user data but token is valid - just mark as not loading
      authStore.update((state) => ({
        ...state,
        isLoading: false,
        requirePasswordChange,
      }));
    }

    return true;
  } catch {
    // Network error or unexpected failure - just clear local state
    clearLocalAuthState();
    return false;
  }
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
export async function changePassword(
  currentPassword: string,
  newPassword: string
): Promise<void> {
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
    const refreshed = await refreshToken();
    if (refreshed) {
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
