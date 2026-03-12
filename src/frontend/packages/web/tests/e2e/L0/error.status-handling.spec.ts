import { test as baseTest, expect as baseExpect } from '@playwright/test';
import { loginAsAdmin, loginAsUser, getAuthHeaders } from '../helpers/auth.helpers';
import { E2E_API_BASE, TEST_USERS, ACME_TENANT_ID, ACME_PROJECT_ID } from '../helpers/constants';

const API = E2E_API_BASE;

async function loginViaUi(page: import('@playwright/test').Page, username: string, password: string): Promise<void> {
  const usernameInput = page
    .locator('input[type="text"], input[name="username"], input[placeholder*="username" i]')
    .first();
  const passwordInput = page.locator('input[type="password"]').first();
  const submitButton = page.locator('button[type="submit"]').first();

  await baseExpect(usernameInput).toBeVisible({ timeout: 10000 });
  await usernameInput.fill(username);
  await passwordInput.fill(password);
  await submitButton.click();
}

baseTest.describe('L0: Error Status Handling (400/401/403/404)', () => {
  baseTest('400 BadRequest for invalid system config group', async ({ request }) => {
    const adminToken = await loginAsAdmin(request);

    const response = await request.get(`${API}/system/config/groups/invalidGroup`, {
      headers: getAuthHeaders(adminToken, ACME_TENANT_ID, ACME_PROJECT_ID),
      failOnStatusCode: false,
    });

    baseExpect(response.status()).toBe(400);
    const body = await response.json();
    baseExpect(body.success).toBe(false);
  });

  baseTest('401 Unauthorized keeps login screen for unauthenticated user', async ({ page, request }) => {
    const response = await request.get(`${API}/users/me`, {
      failOnStatusCode: false,
    });

    baseExpect(response.status()).toBe(401);

    await page.context().clearCookies();
    await page.goto('/');
    await page.waitForLoadState('networkidle').catch(() => {});

    const loginForm = page.locator('form').first();
    await baseExpect(loginForm).toBeVisible({ timeout: 5000 });
  });

  baseTest('403 Forbidden opens AccessDenied page for non-admin deep-link', async ({ page }) => {
    await page.context().clearCookies();
    await page.goto('/?returnUrl=/settings/system/users');
    await page.waitForLoadState('networkidle').catch(() => {});

    await loginViaUi(page, TEST_USERS.john.username, TEST_USERS.john.password);
    await page.waitForLoadState('networkidle').catch(() => {});

    await baseExpect(page.getByRole('heading', { name: 'Access Denied' })).toBeVisible({ timeout: 10000 });
    await baseExpect(page.getByText('This page requires administrator privileges.')).toBeVisible();
  });

  baseTest('404 NotFound for missing system config key', async ({ request }) => {
    const adminToken = await loginAsAdmin(request);

    const response = await request.get(`${API}/system/config/groups/general/nonExistentKey`, {
      headers: getAuthHeaders(adminToken, ACME_TENANT_ID, ACME_PROJECT_ID),
      failOnStatusCode: false,
    });

    baseExpect(response.status()).toBe(404);
    const body = await response.json();
    baseExpect(body.success).toBe(false);
  });

  baseTest('403 Forbidden for non-admin system users API access', async ({ request }) => {
    const devToken = await loginAsUser(request, TEST_USERS.john.username, TEST_USERS.john.password);

    const response = await request.get(`${API}/users`, {
      headers: getAuthHeaders(devToken, ACME_TENANT_ID, ACME_PROJECT_ID),
      failOnStatusCode: false,
    });

    baseExpect(response.status()).toBe(403);
  });
});
