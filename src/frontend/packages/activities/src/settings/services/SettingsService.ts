/**
 * Settings Service
 * Handles settings API calls with fallback to local storage
 */

import { fetchWithAuth } from '../../shared/api';
import type { UserProfile, UserPreferences } from '../types';

// ============================================
// API Response Types
// ============================================

interface UserDto {
  id: string;
  username: string;
  email: string;
  displayName: string;
  avatarUrl: string | null;
  isActive: boolean;
  lastLoginAt: string | null;
  createdAt: string;
}

interface UpdateProfileDto {
  displayName: string;
  email: string;
  avatarUrl: string | null;
}

// ============================================
// Service
// ============================================

class SettingsService {
  private tenantId = '';

  setContext(tenantId: string): void {
    this.tenantId = tenantId;
  }

  /**
   * Get current user profile
   */
  async getUserProfile(): Promise<UserProfile> {
    try {
      const dto = await fetchWithAuth<UserDto>('/users/me', {
        tenantId: this.tenantId,
      });
      return this.mapUserProfile(dto);
    } catch (error) {
      const message = error instanceof Error ? error.message : 'Profile fetch failed';
      console.warn('Profile API failed, using fallback:', message);
      return this.getFallbackProfile();
    }
  }

  /**
   * Change current user password
   */
  async changePassword(currentPassword: string, newPassword: string): Promise<void> {
    await fetchWithAuth('/auth/change-password', {
      method: 'POST',
      tenantId: this.tenantId,
      body: { currentPassword, newPassword },
    });
  }

  /**
   * Save user profile (display name, email)
   */
  async saveUserProfile(profile: UserProfile): Promise<void> {
    try {
      const body: UpdateProfileDto = {
        displayName: profile.name,
        email: profile.email,
        avatarUrl: profile.avatarUrl ?? null,
      };

      await fetchWithAuth<UserDto>('/users/me/profile', {
        method: 'PUT',
        tenantId: this.tenantId,
        body,
      });
    } catch (error) {
      const message = error instanceof Error ? error.message : 'Profile save failed';
      console.warn('Profile save API failed:', message);
      throw error;
    }
  }

  /**
   * Get user preferences from server (fallback to localStorage)
   */
  async getUserPreferences(): Promise<UserPreferences> {
    try {
      const dto = await fetchWithAuth<{ preferences: Record<string, unknown> | null }>(
        '/users/me/preferences',
        { tenantId: this.tenantId },
      );
      if (dto.preferences) {
        const prefs = dto.preferences as unknown as UserPreferences;
        localStorage.setItem('sddp-preferences', JSON.stringify(prefs));
        return prefs;
      }
    } catch {
      // Server failed, fall back to localStorage
    }

    try {
      const stored = localStorage.getItem('sddp-preferences');
      if (stored) {
        return JSON.parse(stored);
      }
    } catch {
      // Ignore parse errors
    }
    return this.getDefaultPreferences();
  }

  /**
   * Save user preferences to server (+ localStorage sync)
   */
  async saveUserPreferences(preferences: UserPreferences): Promise<void> {
    await fetchWithAuth('/users/me/preferences', {
      method: 'PUT',
      tenantId: this.tenantId,
      body: { preferences },
    });
    localStorage.setItem('sddp-preferences', JSON.stringify(preferences));
  }

  // ============================================
  // Transform
  // ============================================

  private mapUserProfile(dto: UserDto): UserProfile {
    return {
      id: dto.id,
      name: dto.displayName,
      email: dto.email,
      username: dto.username,
      avatarUrl: dto.avatarUrl ?? undefined,
    };
  }

  // ============================================
  // Fallback Data
  // ============================================

  private getFallbackProfile(): UserProfile {
    return {
      id: 'unknown',
      name: 'Unknown User',
      email: '',
      username: 'unknown',
    };
  }

  /**
   * Get default preferences for reset functionality
   */
  getDefaultPreferences(): UserPreferences {
    return {
      theme: 'dark',
      accentColor: 'indigo',
      toastDuration: 3000,
      dateFormat: 'relative',
    };
  }
}

// Singleton instance
let settingsServiceInstance: SettingsService | null = null;

/**
 * Get the singleton SettingsService instance
 */
export function getSettingsService(): SettingsService {
  if (!settingsServiceInstance) {
    settingsServiceInstance = new SettingsService();
  }
  return settingsServiceInstance;
}

/**
 * Reset the singleton instance (for testing/logout)
 */
export function resetSettingsService(): void {
  settingsServiceInstance = null;
}

export { SettingsService };
export default getSettingsService();
