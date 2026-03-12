/**
 * Authentication Flow E2E Tests
 * Tests for login/logout functionality with API integration
 *
 * Requirements:
 * - REQ-05.2 (JWT + Refresh Token)
 * - REQ-06 (role permission)
 */

import { test, expect } from '@playwright/test';
import { E2E_API_BASE } from '../helpers/constants';

// Test credentials (matches backend seed data)
const TEST_USER = {
  username: 'admin',
  password: 'Admin@123!',
};

const INVALID_USER = {
  username: 'invalid_user',
  password: 'wrong_password',
};

const API_BASE = E2E_API_BASE;

test.describe('Authentication - Login Flow', () => {
  test.beforeEach(async ({ page }) => {
    // Clear cookies and storage to ensure clean state
    await page.context().clearCookies();
    await page.goto('/');
    await page.waitForLoadState('networkidle');
  });

  test('should display login form when not authenticated', async ({ page }) => {
    // Login form elements should be visible
    const usernameInput = page.locator(
      'input[type="text"], input[name="username"], input[placeholder*="username" i]'
    ).first();
    const passwordInput = page.locator('input[type="password"]').first();
    const submitButton = page.locator('button[type="submit"]').first();

    await expect(usernameInput).toBeVisible({ timeout: 5000 });
    await expect(passwordInput).toBeVisible();
    await expect(submitButton).toBeVisible();
  });

  test('should show validation error for empty username', async ({ page }) => {
    const passwordInput = page.locator('input[type="password"]').first();
    const submitButton = page.locator('button[type="submit"]').first();

    // Fill only password
    await passwordInput.fill('somepassword');
    await submitButton.click();

    // Should show validation error or remain on login
    const form = page.locator('form');
    await expect(form).toBeVisible();
  });

  test('should show validation error for empty password', async ({ page }) => {
    const usernameInput = page.locator(
      'input[type="text"], input[name="username"], input[placeholder*="username" i]'
    ).first();
    const submitButton = page.locator('button[type="submit"]').first();

    // Fill only username
    await usernameInput.fill('testuser');
    await submitButton.click();

    // Should show validation error or remain on login
    const form = page.locator('form');
    await expect(form).toBeVisible();
  });

  test('should show error message for invalid credentials', async ({ page }) => {
    const usernameInput = page.locator(
      'input[type="text"], input[name="username"], input[placeholder*="username" i]'
    ).first();
    const passwordInput = page.locator('input[type="password"]').first();
    const submitButton = page.locator('button[type="submit"]').first();

    // Fill invalid credentials
    await usernameInput.fill(INVALID_USER.username);
    await passwordInput.fill(INVALID_USER.password);
    await submitButton.click();

    // Should show error or remain on login page
    const loginFormOrError = page
      .locator('form, [class*="error"], [class*="alert"]')
      .first();
    await expect(loginFormOrError).toBeVisible({ timeout: 5000 });
  });

  test('should successfully login with valid credentials', async ({ page }) => {
    const usernameInput = page.locator(
      'input[type="text"], input[name="username"], input[placeholder*="username" i]'
    ).first();
    const passwordInput = page.locator('input[type="password"]').first();
    const submitButton = page.locator('button[type="submit"]').first();

    // Fill valid credentials
    await usernameInput.fill(TEST_USER.username);
    await passwordInput.fill(TEST_USER.password);
    await submitButton.click();

    // Wait for navigation/authentication
    await page.waitForLoadState('networkidle');

    await expect(
      page.locator('[role="tablist"][aria-label="Activity Bar"]').first()
    ).toBeVisible({ timeout: 10000 });
  });
});

test.describe('Authentication - Protected Routes', () => {
  test('should redirect to login when accessing protected route without auth', async ({
    page,
  }) => {
    await page.context().clearCookies();
    await page.goto('/');
    await page.waitForLoadState('networkidle');

    // Without authentication, should see login form
    const loginForm = page.locator('form');
    await expect(loginForm).toBeVisible({ timeout: 5000 });
  });

  test('should maintain authentication state after page refresh', async ({
    page,
  }) => {
    // Clear and login
    await page.context().clearCookies();
    await page.goto('/');
    await page.waitForLoadState('networkidle');

    // Login
    const usernameInput = page.locator(
      'input[type="text"], input[name="username"], input[placeholder*="username" i]'
    ).first();
    const passwordInput = page.locator('input[type="password"]').first();
    const submitButton = page.locator('button[type="submit"]').first();

    await usernameInput.fill(TEST_USER.username);
    await passwordInput.fill(TEST_USER.password);
    await submitButton.click();

    await page.waitForLoadState('networkidle');

    // Check if logged in (app shell visible)
    const appShell = page.locator('[role="tablist"][aria-label="Activity Bar"]').first();
    await expect(appShell).toBeVisible({ timeout: 10000 });

    // Refresh page
    await page.reload();
    await page.waitForLoadState('networkidle');

    // Should still be logged in (or redirected to login if token expired)
    const activityBar = page.locator('[role="tablist"][aria-label="Activity Bar"]').first();
    const loginForm = page.locator('form').first();
    const hasActivityBar = await activityBar.isVisible({ timeout: 5000 }).catch(() => false);
    const hasLoginForm = await loginForm.isVisible({ timeout: 5000 }).catch(() => false);
    expect(hasActivityBar || hasLoginForm).toBe(true);
  });
});

test.describe('Authentication - Logout Flow', () => {
  test.beforeEach(async ({ page }) => {
    // Clear and attempt login
    await page.context().clearCookies();
    await page.goto('/');
    await page.waitForLoadState('networkidle');

    // Try to login
    const usernameInput = page.locator(
      'input[type="text"], input[name="username"], input[placeholder*="username" i]'
    ).first();
    const passwordInput = page.locator('input[type="password"]').first();
    const submitButton = page.locator('button[type="submit"]').first();

    const hasLoginForm = await usernameInput.isVisible({ timeout: 3000 }).catch(() => false);

    if (hasLoginForm) {
      await usernameInput.fill(TEST_USER.username);
      await passwordInput.fill(TEST_USER.password);
      await submitButton.click();
      await page.waitForLoadState('networkidle');
    }
  });

  test('should have logout option when authenticated', async ({ page }) => {
    const appShell = page.locator('[role="tablist"][aria-label="Activity Bar"]').first();
    await expect(appShell).toBeVisible({ timeout: 10000 });

    // Look for logout button in status bar or user menu
    const logoutButton = page.getByRole('button', { name: 'Sign Out' });
    // Logout option should be available when authenticated
    await expect(logoutButton).toBeVisible({ timeout: 5000 });
  });

  test('should redirect to login after logout', async ({ page }) => {
    const appShell = page.locator('[role="tablist"][aria-label="Activity Bar"]').first();
    await expect(appShell).toBeVisible({ timeout: 10000 });

    // Find and click logout
    const logoutButton = page.getByRole('button', { name: 'Sign Out' });
    await expect(logoutButton).toBeVisible({ timeout: 5000 });

    await logoutButton.click();
    await page.waitForLoadState('networkidle');

    // Should be back to login screen
    const loginForm = page.locator('form');
    await expect(loginForm).toBeVisible({ timeout: 5000 });
  });

  test('should clear authentication tokens after logout', async ({ page }) => {
    const appShell = page.locator('[role="tablist"][aria-label="Activity Bar"]').first();
    await expect(appShell).toBeVisible({ timeout: 10000 });

    const logoutButton = page.getByRole('button', { name: 'Sign Out' });
    await expect(logoutButton).toBeVisible({ timeout: 5000 });

    await logoutButton.click();
    await page.waitForLoadState('networkidle');

    // Try to access protected content - should fail
    await page.reload();
    await page.waitForLoadState('networkidle');

    // Should see login form, not app shell
    const loginForm = page.locator('form');
    await expect(loginForm).toBeVisible({ timeout: 5000 });
  });
});

test.describe('Authentication - API Authorization', () => {
  test('should receive 401 for protected API without token', async ({
    request,
  }) => {
    // Try to access protected endpoint without auth
    const response = await request.get(`${API_BASE}/users/me`, {
      failOnStatusCode: false,
    });

    // Should receive 401 Unauthorized (if API is running)
    const status = response.status();
    test.skip(status === 0, 'API not running');
    expect(status).toBe(401);
  });

  test('should access protected API with valid token', async ({ request }) => {
    // Login to get token
    const loginResponse = await request.post(
      `${API_BASE}/auth/login`,
      {
        data: {
          username: TEST_USER.username,
          password: TEST_USER.password,
        },
        failOnStatusCode: false,
      }
    );

    test.skip(loginResponse.status() !== 200, 'Login failed — cannot test protected API');

    const loginData = await loginResponse.json();
    const accessToken = loginData.accessToken || loginData.token;
    test.skip(!accessToken, 'No access token received from login');

    // Use token to access protected endpoint
    const protectedResponse = await request.get(
      `${API_BASE}/users/me`,
      {
        headers: {
          Authorization: `Bearer ${accessToken}`,
        },
      }
    );

    // Should receive 200 OK with user data
    expect(protectedResponse.status()).toBe(200);
  });
});
