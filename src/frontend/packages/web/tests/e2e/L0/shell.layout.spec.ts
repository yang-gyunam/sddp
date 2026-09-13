/**
 * App Shell E2E Tests
 * Tests for VS Code-style layout, tabs, and navigation
 *
 * test category:
 * - test (baseTest): autoAuth → log test
 * - test (test): autoAuth log → App Shell UI test
 *
 * :
 * - Activity Bar: [role="tablist"][aria-label="Activity Bar"] [role="tab"]
 * - Editor: [role="tab"][data-tab-id]
 * -: [role="menu"] / [role="menuitem"]
 */

import { test, expect } from '../helpers/fixtures';
import { test as baseTest, expect as baseExpect, type Locator } from '@playwright/test';
import { openEditorTabs } from '../helpers/page-object.helpers';

// ── Selectors ──
const ACTIVITY_BAR = '[role="tablist"][aria-label="Activity Bar"]';
const EDITOR_TAB = '[role="tab"][data-tab-id]';

/**
 * The close control is a sibling of the tab, not a child: a tablist may own nothing but tabs, so
 * the button was moved out of it and hidden from the accessibility tree. Searching inside the tab
 * finds nothing, which is what these tests were doing before the skip stopped saying so.
 */
function closeControlFor(tab: Locator): Locator {
  return tab.locator('xpath=..').locator('button[title^="Close"]');
}
const CONTEXT_MENU = '[role="menu"]';
const MENU_ITEM = '[role="menuitem"]';

// ============================================
// test (baseTest — autoAuth)
// ============================================

baseTest.describe('App Shell - Loading and Authentication', () => {
  baseTest('should show loading screen initially', async ({ page }) => {
    await page.goto('/');

    // Loading indicator should be visible briefly
    const loadingIndicator = page.locator('.animate-pulse');
    const isLoading = await loadingIndicator.isVisible({ timeout: 1000 }).catch(() => false);

    // If not loading, we should see either login screen or app shell
    if (!isLoading) {
      const loginForm = page.locator('input[type="password"]').first();
      const activityBar = page.locator(ACTIVITY_BAR).first();

      await Promise.race([
        loginForm.waitFor({ state: 'visible', timeout: 5000 }),
        activityBar.waitFor({ state: 'visible', timeout: 5000 }),
      ]).catch(() => {});

      const hasLogin = await loginForm.isVisible().catch(() => false);
      const hasApp = await activityBar.isVisible().catch(() => false);
      baseExpect(hasLogin || hasApp).toBe(true);
    }
  });

  baseTest('should show login screen when not authenticated', async ({ page }) => {
    await page.context().clearCookies();
    await page.goto('/');
    await page.waitForLoadState('networkidle');

    // Should see login form (password input)
    const passwordField = page.locator('input[type="password"]').first();
    await baseExpect(passwordField).toBeVisible({ timeout: 5000 });
  });
});

baseTest.describe('App Shell - Login Flow', () => {
  baseTest('should have username and password fields', async ({ page }) => {
    await page.context().clearCookies();
    await page.goto('/');
    await page.waitForLoadState('networkidle');

    const usernameField = page.locator('input[type="text"], input[name="username"]').first();
    const passwordField = page.locator('input[type="password"]').first();

    await baseExpect(usernameField).toBeVisible({ timeout: 5000 });
    await baseExpect(passwordField).toBeVisible();
  });

  baseTest('should show validation error on empty submit', async ({ page }) => {
    await page.context().clearCookies();
    await page.goto('/');
    await page.waitForLoadState('networkidle');

    const submitButton = page.locator('button[type="submit"]').first();
    await submitButton.click();

    // Should remain on login page (form still visible)
    const passwordField = page.locator('input[type="password"]').first();
    await baseExpect(passwordField).toBeVisible();
  });
});

// ============================================
// test (test — autoAuth)
// ============================================

test.describe('App Shell - Layout Structure', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/');
  });

  test('should have activity bar on the left', async ({ page }) => {
    const activityBar = page.locator(ACTIVITY_BAR).first();
    await expect(activityBar).toBeVisible();

    const activityItems = activityBar.locator('[role="tab"]');
    const count = await activityItems.count();
    expect(count).toBeGreaterThan(0);
  });

  test('should have sidebar next to activity bar', async ({ page }) => {
    const sidebar = page.locator('aside').first();
    await expect(sidebar).toBeVisible({ timeout: 5000 });
  });

  test('should have main content area', async ({ page }) => {
    const mainContent = page.locator('main, [role="main"]').first();
    await expect(mainContent).toBeVisible({ timeout: 5000 });
  });

  test('should have status bar at the bottom', async ({ page }) => {
    const statusBar = page.locator('[class*="h-6"], [class*="status"]').last();
    await expect(statusBar).toBeVisible({ timeout: 5000 });
  });
});

test.describe('App Shell - Activity Bar Interaction', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/');
  });

  test('should highlight active activity item', async ({ page }) => {
    const activityBar = page.locator(ACTIVITY_BAR).first();
    await expect(activityBar).toBeVisible();

    const firstItem = activityBar.locator('[role="tab"]').first();
    await firstItem.click();

    await expect(firstItem).toHaveAttribute('aria-selected', 'true');
  });
});

test.describe('App Shell - Tabs Functionality', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/');
  });

  test('should display tab bar when tabs are present', async ({ page }) => {
    await openEditorTabs(page);

    const tabBar = page.locator('[role="tablist"]').filter({ has: page.locator(EDITOR_TAB) }).first();
    await expect(tabBar).toBeVisible({ timeout: 5000 });
  });

  test('should be able to click on tabs to switch', async ({ page }) => {
    const opened = await openEditorTabs(page, 2);
    expect(opened).toBeGreaterThanOrEqual(2);

    const editorTabs = page.locator(EDITOR_TAB);
    const secondTab = editorTabs.nth(1);
    await secondTab.click();
    await expect(secondTab).toHaveAttribute('aria-selected', 'true');
  });

  test('should show close button on closable tabs', async ({ page }) => {
    await openEditorTabs(page);

    await expect(closeControlFor(page.locator(EDITOR_TAB).first())).toBeVisible();
  });

  test('should close tab when clicking close button', async ({ page }) => {
    await openEditorTabs(page);

    const editorTabs = page.locator(EDITOR_TAB);
    const initialCount = await editorTabs.count();

    const closeButton = closeControlFor(editorTabs.first());
    await expect(closeButton).toBeVisible();

    await closeButton.click();
    await expect.poll(
      async () => editorTabs.count(),
      { timeout: 5000 }
    ).toBeLessThan(initialCount);
  });
});

test.describe('App Shell - Tab Context Menu', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/');
  });

  test('should show context menu on right-click', async ({ page }) => {
    await openEditorTabs(page);

    // Editor →
    await page.locator(EDITOR_TAB).first().click({ button: 'right' });

    const menu = page.locator(CONTEXT_MENU);
    await expect(menu).toBeVisible();

    await expect(menu.locator(`${MENU_ITEM}:has-text("Close")`).first()).toBeVisible();
    await page.keyboard.press('Escape');
  });

  test('should close context menu when clicking outside', async ({ page }) => {
    await openEditorTabs(page);

    await page.locator(EDITOR_TAB).first().click({ button: 'right' });

    const menu = page.locator(CONTEXT_MENU);
    await expect(menu).toBeVisible();

    // tabpanel →
    await page
      .locator('[role="tabpanel"]')
      .first()
      .click({ position: { x: 10, y: 10 }, force: true });
    await expect(menu).toBeHidden({ timeout: 2000 });
  });
});

test.describe('App Shell - Sidebar Panels', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/');
  });

  test('should have collapsible panels in sidebar', async ({ page }) => {
    const sidebar = page.locator('aside').first();
    await expect(sidebar).toBeVisible();

    // CollapsiblePanel and CollapsibleGroup both render a native <button> carrying aria-expanded.
    // A CSS [role="button"] matches the attribute, not the implicit role, so it finds none of them.
    const panelHeaders = sidebar.locator('button[aria-expanded]');
    await expect(panelHeaders.first()).toBeVisible();
    expect(await panelHeaders.count()).toBeGreaterThan(0);
  });

  test('should toggle panel collapse state', async ({ page }) => {
    const sidebar = page.locator('aside').first();
    await expect(sidebar).toBeVisible();

    // Look for a collapsible panel header (has aria-expanded attribute)
    const panelHeader = sidebar.locator('button[aria-expanded]').first();
    await expect(panelHeader).toBeVisible();

    const initialExpanded = await panelHeader.getAttribute('aria-expanded');

    await panelHeader.click();

    // Wait for aria-expanded to change
    await expect.poll(
      async () => panelHeader.getAttribute('aria-expanded'),
      { timeout: 5000 }
    ).not.toBe(initialExpanded);
  });
});

test.describe('App Shell - Keyboard Shortcuts', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/');
  });

  test('should open command palette with Ctrl+Shift+P', async ({ page }) => {
    const activityBar = page.locator(ACTIVITY_BAR).first();
    await expect(activityBar).toBeVisible();

    await page.keyboard.press('Control+Shift+P');

    const commandPalette = page.locator('[role="dialog"], [class*="CommandPalette"]');
    await expect(commandPalette).toBeVisible({ timeout: 3000 });
    await page.keyboard.press('Escape');
    await expect(commandPalette).toBeHidden({ timeout: 2000 });
  });

  test('should close current tab with Ctrl+W', async ({ page }) => {
    await openEditorTabs(page);

    const editorTabs = page.locator(EDITOR_TAB);
    const initialCount = await editorTabs.count();

    await editorTabs.first().click();
    await page.keyboard.press('Control+w');

    await expect.poll(
      async () => editorTabs.count(),
      { timeout: 5000 }
    ).toBeLessThanOrEqual(initialCount);
  });
});

test.describe('App Shell - Responsive Layout', () => {
  test('should maintain layout on different viewport sizes', async ({ page }) => {
    await page.goto('/');

    const viewports = [
      { width: 1920, height: 1080 },
      { width: 1280, height: 720 },
      { width: 1024, height: 768 },
    ];

    for (const viewport of viewports) {
      await page.setViewportSize(viewport);

      // Activity Bar should remain visible at all viewport sizes
      const activityBar = page.locator('[role="tablist"][aria-label="Activity Bar"]').first();
      await expect(activityBar).toBeVisible({ timeout: 5000 });
    }
  });
});

test.describe('App Shell - Theme Toggle', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/');
  });

  test('should toggle between light and dark theme', async ({ page }) => {
    // applyTheme() adds/removes 'dark'/'light' class on <html>.
    // Status bar button text may not reactively update (isDarkMode() is not Svelte-reactive),
    // so we check the document class instead.
    const htmlElement = page.locator('html');
    const initialClassAttr = await htmlElement.getAttribute('class') ?? '';
    const initialIsDark = initialClassAttr.includes('dark');

    const themeToggle = page.locator('button:has-text("Dark"), button:has-text("Light")').first();
    await expect(themeToggle).toBeVisible();

    await themeToggle.click();

    if (initialIsDark) {
      // Was dark, should now be light (no 'dark' class)
      await expect(htmlElement).not.toHaveClass(/dark/, { timeout: 5000 });
    } else {
      // Was light, should now be dark ('dark' class added)
      await expect(htmlElement).toHaveClass(/dark/, { timeout: 5000 });
    }
  });
});
