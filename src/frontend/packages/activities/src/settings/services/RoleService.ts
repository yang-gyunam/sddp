/**
 * RoleService - API Client for Role Management
 * Handles role listing and detail retrieval for admin settings
 */

import { fetchWithAuth } from '../../shared/api';

// ============================================
// Types
// ============================================

export interface Role {
  id: string;
  name: string;
  description: string;
  type: string;
  isSystemRole: boolean;
  permissions: string[];
}

// ============================================
// Role API Functions
// ============================================

/**
 * Get list of all roles (admin only)
 */
export async function getRoles(tenantId: string): Promise<Role[]> {
  const result = await fetchWithAuth<Role[]>('/roles', { tenantId });
  return result ?? [];
}

/**
 * Get role by ID (admin only)
 */
export async function getRoleById(tenantId: string, roleId: string): Promise<Role> {
  return fetchWithAuth<Role>(`/roles/${roleId}`, { tenantId });
}

// ============================================
// Singleton Service Class
// ============================================

export class RoleService {
  private tenantId = '';

  setContext(tenantId: string): void {
    this.tenantId = tenantId;
  }

  getRoles(): Promise<Role[]> {
    return getRoles(this.tenantId);
  }

  getRoleById(roleId: string): Promise<Role> {
    return getRoleById(this.tenantId, roleId);
  }
}

// ============================================
// Singleton Management
// ============================================

let instance: RoleService | null = null;

export function getRoleService(): RoleService {
  if (!instance) {
    instance = new RoleService();
  }
  return instance;
}

export function resetRoleService(): void {
  instance = null;
}
