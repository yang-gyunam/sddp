/**
 * E2E Authentication Helpers
 * log/log,, create
 */

import { Page, APIRequestContext } from '@playwright/test';
import { TEST_USER, API_BASE, TEST_TENANT_ID, TEST_PROJECT_ID } from './constants';
import { sceneDelay, TYPE_DELAY } from './page-object.helpers';
import { withRetry } from './retry.helpers';

/**
 * Login and get access token
 */
export async function loginAsAdmin(request: APIRequestContext): Promise<string> {
  const response = await withRetry(
    () => request.post(`${API_BASE}/auth/login`, {
      data: {
        username: TEST_USER.username,
        password: TEST_USER.password,
      },
      failOnStatusCode: false,
    })
  );

  if (response.status() !== 200) {
    const errorBody = await response.text();
    throw new Error(`Login failed with status ${response.status()}: ${errorBody}`);
  }

  const responseData = await response.json();
  const data = responseData.data || responseData;
  const token = data.accessToken || data.token;

  if (!token) {
    throw new Error('No access token in login response');
  }

  return token;
}

/**
 * Login as any user and get access token
 */
export async function loginAsUser(
  request: APIRequestContext,
  username: string,
  password: string
): Promise<string> {
  const response = await withRetry(
    () => request.post(`${API_BASE}/auth/login`, {
      data: { username, password },
      failOnStatusCode: false,
    })
  );

  if (response.status() !== 200) {
    const errorBody = await response.text();
    throw new Error(`Login failed for ${username} with status ${response.status()}: ${errorBody}`);
  }

  const responseData = await response.json();
  const data = responseData.data || responseData;
  const token = data.accessToken || data.token;

  if (!token) {
    throw new Error(`No access token in login response for ${username}`);
  }

  return token;
}

/**
 * Login via UI
 */
export async function loginViaUI(page: Page): Promise<void> {
  await page.goto('/');
  await page.waitForLoadState('networkidle').catch((e) => {
    console.warn(`[loginViaUI] networkidle timeout: ${(e as Error).message}`);
  });

  const activityBar = page.locator('[role="tablist"][aria-label="Activity Bar"]').first();
  if (await activityBar.isVisible({ timeout: 1500 }).catch(() => false)) {
    await sceneDelay(page);
    return;
  }

  const usernameInput = page
    .locator('input[type="text"], input[name="username"], input[placeholder*="username" i]')
    .first();
  const passwordInput = page.locator('input[type="password"]').first();
  const submitButton = page.locator('button[type="submit"]').first();

  await usernameInput.waitFor({ state: 'visible', timeout: 10000 });
  await passwordInput.waitFor({ state: 'visible', timeout: 10000 });

  if (TYPE_DELAY > 0) {
    await usernameInput.pressSequentially(TEST_USER.username, { delay: TYPE_DELAY });
    await passwordInput.pressSequentially(TEST_USER.password, { delay: TYPE_DELAY });
  } else {
    await usernameInput.fill(TEST_USER.username);
    await passwordInput.fill(TEST_USER.password);
  }

  await submitButton.click();
  await page.waitForLoadState('networkidle').catch((e) => {
    console.warn(`[loginViaUI] post-login networkidle timeout: ${(e as Error).message}`);
  });
  await activityBar.waitFor({ state: 'visible', timeout: 15000 });
  await sceneDelay(page);
}

/**
 * Get auth headers with token
 */
export function getAuthHeaders(token: string, tenantId?: string, projectId?: string) {
  return {
    Authorization: `Bearer ${token}`,
    'X-Tenant-Id': tenantId || TEST_TENANT_ID,
    'X-Project-Id': projectId || TEST_PROJECT_ID,
    'Content-Type': 'application/json',
  };
}

/**
 * Login via UI and wait for App Shell to be fully loaded.
 * Returns true if login succeeded, false if backend is not running.
 */
export async function loginAndWaitForShell(page: Page): Promise<boolean> {
  await page.goto('/');
  await page.waitForLoadState('networkidle').catch((e) => {
    console.warn(`[loginAndWaitForShell] networkidle timeout: ${(e as Error).message}`);
  });

  const usernameInput = page
    .locator('input[type="text"], input[name="username"], input[placeholder*="username" i]')
    .first();
  const passwordInput = page.locator('input[type="password"]').first();
  const submitButton = page.locator('button[type="submit"]').first();
  const activityBar = page.locator('[role="tablist"][aria-label="Activity Bar"]').first();

  if (await activityBar.isVisible({ timeout: 1500 }).catch(() => false)) {
    await sceneDelay(page);
    return true;
  }

  const hasLoginForm = await usernameInput.isVisible({ timeout: 5000 }).catch(() => false);
  if (!hasLoginForm) return false;

  if (TYPE_DELAY > 0) {
    await usernameInput.pressSequentially(TEST_USER.username, { delay: TYPE_DELAY });
    await passwordInput.pressSequentially(TEST_USER.password, { delay: TYPE_DELAY });
    await submitButton.click();
  } else {
    await usernameInput.fill(TEST_USER.username);
    await passwordInput.fill(TEST_USER.password);
    await submitButton.click();
  }

  await page.waitForLoadState('networkidle').catch((e) => {
    console.warn(`[loginAndWaitForShell] post-login networkidle timeout: ${(e as Error).message}`);
  });

  // Verify app shell loaded (activity bar visible)
  const loaded = await activityBar.isVisible({ timeout: 10000 }).catch(() => false);
  if (loaded) await sceneDelay(page);
  return loaded;
}
