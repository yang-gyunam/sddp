/**
 * UserManagementService - API Client for System User Management
 * Handles user listing and detail retrieval for admin settings
 */

import { fetchWithAuth } from '../../shared/api';
import type { SystemUser, CreateUserParams } from '../types';

// ============================================
// Types
// ============================================

export interface GetUsersParams {
  search?: string;
  role?: string;
  status?: 'active' | 'inactive';
  page?: number;
  size?: number;
}

export interface PagedUsers {
  items: SystemUser[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

// ============================================
// Tenant Member Types (lightweight, no admin permission required)
// ============================================

export interface TenantMember {
  id: string;
  name: string;
  email: string;
}

// ============================================
// Tenant Member API Functions (any authenticated user)
// ============================================

/**
 * Get tenant members for DM candidate selection (no admin permission required)
 */
export async function getTenantMembers(
  tenantId: string,
  search?: string
): Promise<TenantMember[]> {
  const searchParams = new URLSearchParams();
  if (search) searchParams.set('search', search);

  const queryString = searchParams.toString();
  const endpoint = `/users/tenant-members${queryString ? `?${queryString}` : ''}`;

  return fetchWithAuth<TenantMember[]>(endpoint, { tenantId });
}

// ============================================
// User Management API Functions (admin only)
// ============================================

/**
 * Get list of system users (admin only)
 */
export async function getSystemUsers(
  tenantId: string,
  params?: GetUsersParams
): Promise<PagedUsers> {
  const searchParams = new URLSearchParams();
  if (params?.search) searchParams.set('search', params.search);
  if (params?.role) searchParams.set('role', params.role);
  if (params?.status) searchParams.set('status', params.status);
  if (params?.page) searchParams.set('pageNumber', params.page.toString());
  if (params?.size) searchParams.set('pageSize', params.size.toString());

  const queryString = searchParams.toString();
  const endpoint = `/users/system${queryString ? `?${queryString}` : ''}`;

  const result = await fetchWithAuth<{
    items: SystemUser[];
    totalCount: number;
    pageNumber?: number;
    pageSize?: number;
    totalPages?: number;
    hasNextPage?: boolean;
    hasPreviousPage?: boolean;
  }>(endpoint, {
    tenantId,
  });

  const items = result.items ?? [];
  const totalCount = result.totalCount ?? 0;
  const pageNumber = result.pageNumber ?? params?.page ?? 1;
  const pageSize = result.pageSize ?? params?.size ?? items.length;
  const totalPages = result.totalPages ?? (pageSize > 0 ? Math.ceil(totalCount / pageSize) : 0);

  return {
    items,
    totalCount,
    pageNumber,
    pageSize,
    totalPages,
    hasNextPage: result.hasNextPage ?? (pageSize > 0 ? pageNumber < totalPages : false),
    hasPreviousPage: result.hasPreviousPage ?? pageNumber > 1,
  };
}

/**
 * Get system user by ID (admin only)
 */
export async function getSystemUserById(
  tenantId: string,
  userId: string
): Promise<SystemUser> {
  return fetchWithAuth<SystemUser>(`/users/system/${userId}`, { tenantId });
}

/**
 * Create a new user (admin only)
 */
export async function createSystemUser(
  tenantId: string,
  params: CreateUserParams
): Promise<SystemUser> {
  return fetchWithAuth<SystemUser>('/users', {
    tenantId,
    method: 'POST',
    body: params,
  });
}

/**
 * Update a user (admin only)
 */
export async function updateSystemUser(
  tenantId: string,
  userId: string,
  params: { displayName: string; email: string; globalRole?: string }
): Promise<SystemUser> {
  return fetchWithAuth<SystemUser>(`/users/${userId}`, {
    tenantId,
    method: 'PUT',
    body: params,
  });
}

/**
 * Deactivate a user (admin only)
 */
export async function deactivateUser(
  tenantId: string,
  userId: string
): Promise<void> {
  await fetchWithAuth(`/users/${userId}/deactivate`, {
    tenantId,
    method: 'PUT',
  });
}

/**
 * Reset a user's password (admin only)
 */
export async function resetUserPassword(
  tenantId: string,
  userId: string
): Promise<{ temporaryPassword: string }> {
  return fetchWithAuth<{ temporaryPassword: string }>(`/users/${userId}/reset-password`, {
    tenantId,
    method: 'POST',
  });
}

/**
 * Activate a user (admin only)
 */
export async function activateUser(
  tenantId: string,
  userId: string
): Promise<void> {
  await fetchWithAuth(`/users/${userId}/activate`, {
    tenantId,
    method: 'PUT',
  });
}

// ============================================
// Singleton Service Class
// ============================================

export class UserManagementService {
  private tenantId = '';

  setContext(tenantId: string): void {
    this.tenantId = tenantId;
  }

  getUsers(params?: GetUsersParams): Promise<PagedUsers> {
    return getSystemUsers(this.tenantId, params);
  }

  getUserById(userId: string): Promise<SystemUser> {
    return getSystemUserById(this.tenantId, userId);
  }

  createUser(params: CreateUserParams): Promise<SystemUser> {
    return createSystemUser(this.tenantId, params);
  }

  updateUser(userId: string, params: { displayName: string; email: string; globalRole?: string }): Promise<SystemUser> {
    return updateSystemUser(this.tenantId, userId, params);
  }

  deactivateUser(userId: string): Promise<void> {
    return deactivateUser(this.tenantId, userId);
  }

  activateUser(userId: string): Promise<void> {
    return activateUser(this.tenantId, userId);
  }

  resetPassword(userId: string): Promise<{ temporaryPassword: string }> {
    return resetUserPassword(this.tenantId, userId);
  }
}

// ============================================
// Singleton Management
// ============================================

let instance: UserManagementService | null = null;

export function getUserManagementService(): UserManagementService {
  if (!instance) {
    instance = new UserManagementService();
  }
  return instance;
}

export function resetUserManagementService(): void {
  instance = null;
}
