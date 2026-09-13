// @sddp/auth - Authentication Types

/**
 * User entity
 */
export interface User {
  id: string;
  username: string;
  email: string;
  displayName: string;
  tenantId: string;
  roles: string[];
  permissions: string[];
  avatarUrl?: string;
  createdAt: string;
  updatedAt: string;
}

/**
 * Authentication state
 */
export interface AuthState {
  user: User | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  requirePasswordChange?: boolean;
  sessionStatus?: 'checking' | 'active' | 'anonymous' | 'expired' | 'unavailable';
}

/**
 * Login request
 */
export interface LoginRequest {
  username: string;
  password: string;
  tenantId?: string;
}

/**
 * Auth user info from login/refresh response
 */
export interface AuthUserInfo {
  id: string;
  username: string;
  displayName: string;
  tenantId: string;
  roles: string[];
  permissions: string[];
}

/**
 * Login response
 */
export interface LoginResponse {
  accessToken: string;
  expiresIn: number;
  user: AuthUserInfo;
  requirePasswordChange?: boolean;
}
