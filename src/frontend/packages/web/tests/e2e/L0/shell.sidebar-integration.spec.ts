/**
 * Activity Bar and Sidebar Integration E2E Tests
 *
 * Selector guide:
 * - Activity Bar tabs: [role="tablist"][aria-label="Activity Bar"] button[role="tab"]
 * - Sidebar: aside
 * - Editor tabs: [role="tab"][data-tab-id]
 *
 * localStorage key: sddp-layout (sidebar.isOpen, sidebar.activePanel, activityBar.activeItemId)
 */

import { test, expect } from '../helpers/fixtures';
import { navigateToActivity, waitForSidebarContent } from '../helpers/page-object.helpers';

// ── Selectors ──
const ACTIVITY_BAR = '[role="tablist"][aria-label="Activity Bar"]';
const ACTIVITY_TAB = `${ACTIVITY_BAR} button[role="tab"]`;
const SIDEBAR = 'aside';

function activityTabByName(page: import('@playwright/test').Page, name: RegExp) {
  return page.locator(ACTIVITY_BAR).first().getByRole('tab', { name }).first();
}

// ============================================
// Activity Bar + Sidebar integration
// ============================================

test.describe('Activity Bar and Sidebar Integration', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/');
  });

  test('should have multiple activity items in the activity bar', async ({ page }) => {
    const activityBar = page.locator(ACTIVITY_BAR).first();
    await expect(activityBar).toBeVisible();

    const items = activityBar.locator('button[role="tab"]');
    const count = await items.count();
    expect(count).toBeGreaterThanOrEqual(4); // Dashboard, Conversations, Tasks, Settings, etc.
  });

  test('should set aria-selected=true on clicked activity item', async ({ page }) => {
    const items = page.locator(ACTIVITY_TAB);
    const count = await items.count();
    if (count < 2) { test.skip(true, 'Need at least two activity tabs'); return; }

    // Click the second tab
    const second = items.nth(1);
    await second.click();

    await expect(second).toHaveAttribute('aria-selected', 'true');

    // The first tab should be deselected
    const first = items.first();
    await expect(first).toHaveAttribute('aria-selected', 'false');
  });

  test('should change sidebar content when switching activities', async ({ page }) => {
    const sidebar = page.locator(SIDEBAR).first();
    await navigateToActivity(page, 'Dashboard');
    await waitForSidebarContent(page);
    await expect(sidebar).toBeVisible({ timeout: 5000 });
    const firstContent = await sidebar.textContent() ?? '';

    await navigateToActivity(page, 'Conversations');
    await waitForSidebarContent(page);
    await expect(sidebar).toBeVisible({ timeout: 5000 });
    const secondContent = await sidebar.textContent() ?? '';

    // Different activities should produce different sidebar content
    expect(firstContent).not.toBe(secondContent);
  });
});

// ============================================
// Sidebar toggle behavior
// ============================================

test.describe('Sidebar Toggle', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/');
  });

  test('should toggle sidebar by clicking same activity twice', async ({ page }) => {
    const sidebar = page.locator(SIDEBAR).first();
    const dashboardTab = activityTabByName(page, /Dashboard/i);

    await navigateToActivity(page, 'Dashboard');
    await waitForSidebarContent(page);
    await expect(sidebar).toBeVisible({ timeout: 5000 });

    await dashboardTab.click();
    await expect(sidebar).toBeHidden({ timeout: 5000 });

    await dashboardTab.click();
    await expect(sidebar).toBeVisible({ timeout: 5000 });
  });

  test('should open sidebar when clicking a different activity', async ({ page }) => {
    const sidebar = page.locator(SIDEBAR).first();
    const dashboardTab = activityTabByName(page, /Dashboard/i);
    const conversationsTab = activityTabByName(page, /Conversations|Discussions/i);

    await navigateToActivity(page, 'Dashboard');
    await waitForSidebarContent(page);
    await expect(sidebar).toBeVisible({ timeout: 5000 });

    await dashboardTab.click();
    await expect(sidebar).toBeHidden({ timeout: 5000 });

    await conversationsTab.click();
    await expect(sidebar).toBeVisible({ timeout: 5000 });
  });
});

// ============================================
// Layout State Persistence (localStorage)
// ============================================

test.describe('Layout State Persistence', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/');
  });

  test('should persist layout state to sddp-layout in localStorage', async ({ page }) => {
    await navigateToActivity(page, 'Conversations');
    await waitForSidebarContent(page);
    await expect(page.locator(SIDEBAR).first()).toBeVisible({ timeout: 5000 });

    // Verify sddp-layout in localStorage
    const stored = await page.evaluate(() => localStorage.getItem('sddp-layout'));
    expect(stored).not.toBeNull();

    const layout = JSON.parse(stored!);
    expect(layout.sidebar).toBeDefined();
    expect(layout.activityBar).toBeDefined();
    expect(layout.sidebar.isOpen).toBe(true);
  });

  test('should restore active panel after page reload', async ({ page }) => {
    const conversationsTab = activityTabByName(page, /Conversations|Discussions/i);
    const secondLabel = await conversationsTab.getAttribute('aria-label');

    await navigateToActivity(page, 'Conversations');
    await waitForSidebarContent(page);
    await expect(page.locator(SIDEBAR).first()).toBeVisible({ timeout: 5000 });

    // Reload
    await page.reload({ waitUntil: 'networkidle' });

    // The same tab should remain selected after reload
    const restoredActive = page.locator(`${ACTIVITY_TAB}[aria-selected="true"]`).first();
    const restoredLabel = await restoredActive.getAttribute('aria-label');
    expect(restoredLabel).toBe(secondLabel);
  });
});
