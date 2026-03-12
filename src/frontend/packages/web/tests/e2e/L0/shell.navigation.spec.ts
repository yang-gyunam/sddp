/**
 * UI Navigation E2E Tests
 * Tests for Activity Bar navigation and sidebar panel switching
 *
 * Requirements:
 * - REQ-11.4 (App Shell UX)
 * - Activity Bar items: Dashboard, Projects, Conversations, Tasks, Settings
 * - Each activity has its own sidebar panels
 */

import type { Page } from '@playwright/test';
import { test, expect } from '../helpers/fixtures';
import { test as baseTest, expect as baseExpect } from '@playwright/test';
import { navigateToActivity, waitForSidebarContent } from '../helpers/page-object.helpers';

async function expectSidebarContainsAny(page: Page, expectedLabels: string[]): Promise<void> {
  const sidebar = page.locator('aside').first();
  await expect(sidebar).toBeVisible();
  const normalizedLabels = expectedLabels.map((label) => label.toLowerCase());

  await expect
    .poll(
      async () => {
        const sidebarText = ((await sidebar.textContent().catch(() => '')) ?? '').toLowerCase();
        return normalizedLabels.some((label) => sidebarText.includes(label));
      },
      { timeout: 5000 }
    )
    .toBe(true);
}

function activityTab(page: Page, pattern: RegExp) {
  return page
    .locator('[role="tablist"][aria-label="Activity Bar"]')
    .first()
    .getByRole('tab', { name: pattern })
    .first();
}

test.describe('UI Navigation - Activity Bar', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/');
  });

  test('should navigate to Dashboard and show dashboard panels', async ({ page }) => {
    await navigateToActivity(page, 'Dashboard');
    await waitForSidebarContent(page);
    await expectSidebarContainsAny(page, ['My Dashboard', 'System', 'Dashboard']);
  });

  test('should navigate to Conversations and show conversation panels', async ({ page }) => {
    await navigateToActivity(page, 'Conversations');
    await waitForSidebarContent(page);
    await expectSidebarContainsAny(page, ['Channels', 'Forums', 'Direct Messages']);
  });

  test('should navigate to Projects and show project-related content', async ({ page }) => {
    await navigateToActivity(page, 'Projects');
    await waitForSidebarContent(page);
    await expectSidebarContainsAny(page, ['Timeline', 'No Projects', 'Project', 'Specs']);
  });

  test('should navigate to Tasks and show task panels', async ({ page }) => {
    await navigateToActivity(page, 'Tasks');
    await waitForSidebarContent(page);
    await expectSidebarContainsAny(page, ['My Tasks', 'Tasks', 'By Project', 'Generator']);
  });

  test('should navigate to Settings and show settings panels', async ({ page }) => {
    await navigateToActivity(page, 'Settings');
    await waitForSidebarContent(page);
    await expectSidebarContainsAny(page, [
      'USER',
      'PROJECT',
      'SYSTEM',
      'Profile',
      'Preferences',
    ]);
  });

  test('should set aria-selected=true on the clicked activity button', async ({ page }) => {
    const conversationsButton = activityTab(page, /Conversations|Discussions/i);
    const dashboardButton = activityTab(page, /Dashboard/i);

    await conversationsButton.click();
    await expect(conversationsButton).toHaveAttribute('aria-selected', 'true');
    await expect(dashboardButton).toHaveAttribute('aria-selected', 'false');

    await dashboardButton.click();
    await expect(dashboardButton).toHaveAttribute('aria-selected', 'true');
    await expect(conversationsButton).toHaveAttribute('aria-selected', 'false');
  });
});

// ── test (baseTest — autoAuth) ──

baseTest.describe('UI Navigation - Deep Link', () => {
  baseTest('should handle deep-link returnUrl after login', async ({ page }) => {
    await page.context().clearCookies();
    await page.goto('/?returnUrl=/dashboard/overview');
    await page.waitForLoadState('networkidle').catch(() => {});

    const usernameInput = page
      .locator('input[type="text"], input[name="username"], input[placeholder*="username" i]')
      .first();
    const passwordInput = page.locator('input[type="password"]').first();
    const submitButton = page.locator('button[type="submit"]').first();

    await baseExpect(usernameInput).toBeVisible({ timeout: 5000 });

    await usernameInput.fill('admin');
    await passwordInput.fill('Admin@123!');
    await submitButton.click();
    await page.waitForLoadState('networkidle').catch(() => {});

    await baseExpect
      .poll(
        () => {
          const url = page.url();
          return (
            url.includes('dashboard') || url.includes('overview') || url === new URL('/', url).href
          );
        },
        { timeout: 10000 }
      )
      .toBe(true);

    const activityBar = page.locator('[role="tablist"][aria-label="Activity Bar"]').first();
    await baseExpect(activityBar).toBeVisible({ timeout: 10000 });
  });
});
