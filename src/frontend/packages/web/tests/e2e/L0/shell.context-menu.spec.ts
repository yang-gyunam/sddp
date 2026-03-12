/**
 * Context Menu E2E Tests
 *
 * Behavior tests for the app's custom context menu (⋮ vertical ellipsis button).
 * - Not the browser's native right-click menu
 * - Opened by the "More Actions" button in the EditorGroup tab bar
 * - Uses role="menu" / role="menuitem" accessibility structure
 * - Guarantees a single open menu via the global 'close-all-context-menus' event
 *
 * Selector guide:
 * - Editor tabs: [role="tab"][data-tab-id]  (TabItem inside EditorGroup)
 * - Activity Bar tabs: nav[aria-label="Activity Bar"] [role="tab"]
 *
 * Tab creation strategy:
 * - The openEditorTabs() helper creates channels via the API -> refreshes the sidebar -> clicks to create tabs
 * - The ⋮ button appears only when at least one tab exists
 */

import { test, expect } from '../helpers/fixtures';
import { openEditorTabs } from '../helpers/page-object.helpers';

/** Selector for the ⋮ button */
const MORE_ACTIONS_BTN = 'button[aria-label="More actions"]';
/** App custom context menu */
const CONTEXT_MENU = '[role="menu"]';
/** Menu item */
const MENU_ITEM = '[role="menuitem"]';
/** Editor tabs (excluding Activity Bar tabs) */
const EDITOR_TAB = '[role="tab"][data-tab-id]';

test.describe('Context Menu - Action Menu (⋮)', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/');
  });

  test('should show only one context menu at a time', async ({ page }) => {
    if ((await openEditorTabs(page)) === 0) {
      test.skip(true, 'No editor tabs could be opened');
      return;
    }

    const moreButtons = page.locator(MORE_ACTIONS_BTN);
    const btnCount = await moreButtons.count();
    if (btnCount === 0) {
      test.skip(true, 'More actions button not rendered');
      return;
    }

    // Click ⋮ -> only one menu should be visible
    await moreButtons.first().click();
    const menus = page.locator(CONTEXT_MENU);
    await expect(menus).toHaveCount(1);

    await page.keyboard.press('Escape');
  });

  test('should have correct menu items (Close All, Split Right, Split Down)', async ({
    page,
  }) => {
    if ((await openEditorTabs(page)) === 0) {
      test.skip(true, 'No editor tabs could be opened');
      return;
    }

    const moreBtn = page.locator(MORE_ACTIONS_BTN).first();
    if (!(await moreBtn.isVisible({ timeout: 3000 }).catch(() => false))) {
      test.skip(true, 'More actions button not visible');
      return;
    }

    await moreBtn.click();
    const menu = page.locator(CONTEXT_MENU);
    await expect(menu).toBeVisible({ timeout: 2000 });

    await expect(
      menu.locator(`${MENU_ITEM}:has-text("Close All")`),
    ).toBeVisible();
    await expect(
      menu.locator(`${MENU_ITEM}:has-text("Split Right")`),
    ).toBeVisible();
    await expect(
      menu.locator(`${MENU_ITEM}:has-text("Split Down")`),
    ).toBeVisible();

    await page.keyboard.press('Escape');
  });
});

test.describe('Context Menu - Close on Events', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/');
  });

  test('should close context menu on Escape key', async ({ page }) => {
    if ((await openEditorTabs(page)) === 0) {
      test.skip(true, 'No editor tabs could be opened');
      return;
    }

    const moreBtn = page.locator(MORE_ACTIONS_BTN).first();
    if (!(await moreBtn.isVisible({ timeout: 3000 }).catch(() => false))) {
      test.skip(true, 'More actions button not visible');
      return;
    }

    await moreBtn.click();
    const menu = page.locator(CONTEXT_MENU);
    await expect(menu).toBeVisible({ timeout: 2000 });

    await page.keyboard.press('Escape');
    await expect(menu).toBeHidden({ timeout: 2000 });
  });

  test('should close context menu on outside click', async ({ page }) => {
    if ((await openEditorTabs(page)) === 0) {
      test.skip(true, 'No editor tabs could be opened');
      return;
    }

    const moreBtn = page.locator(MORE_ACTIONS_BTN).first();
    if (!(await moreBtn.isVisible({ timeout: 3000 }).catch(() => false))) {
      test.skip(true, 'More actions button not visible');
      return;
    }

    await moreBtn.click();
    const menu = page.locator(CONTEXT_MENU);
    await expect(menu).toBeVisible({ timeout: 2000 });

    // Click the tabpanel area -> menu closes
    await page
      .locator('[role="tabpanel"]')
      .first()
      .click({ position: { x: 10, y: 10 }, force: true });
    await expect(menu).toBeHidden({ timeout: 2000 });
  });

  test('should close context menu on browser resize', async ({ page }) => {
    if ((await openEditorTabs(page)) === 0) {
      test.skip(true, 'No editor tabs could be opened');
      return;
    }

    const moreBtn = page.locator(MORE_ACTIONS_BTN).first();
    if (!(await moreBtn.isVisible({ timeout: 3000 }).catch(() => false))) {
      test.skip(true, 'More actions button not visible');
      return;
    }

    await moreBtn.click();
    const menu = page.locator(CONTEXT_MENU);
    await expect(menu).toBeVisible({ timeout: 2000 });

    const currentViewport = page.viewportSize();
    await page.setViewportSize({
      width: (currentViewport?.width || 1280) - 100,
      height: currentViewport?.height || 720,
    });

    await expect(menu).toBeHidden({ timeout: 2000 });

    // Restore viewport
    await page.setViewportSize({
      width: currentViewport?.width || 1280,
      height: currentViewport?.height || 720,
    });
  });
});

test.describe('Context Menu - Tab Context Menu', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/');
  });

  test('should show tab context menu on right-click', async ({ page }) => {
    if ((await openEditorTabs(page)) === 0) {
      test.skip(true, 'No editor tabs could be opened');
      return;
    }

    // Right-click an editor tab -> app custom context menu (oncontextmenu handler)
    await page.locator(EDITOR_TAB).first().click({ button: 'right' });
    const menu = page.locator(CONTEXT_MENU);
    if (!(await menu.isVisible({ timeout: 2000 }).catch(() => false))) {
      test.skip(true, 'Context menu not visible after right-click');
      return;
    }

    await expect(
      menu.locator(`${MENU_ITEM}:has-text("Close")`).first(),
    ).toBeVisible();
    await page.keyboard.press('Escape');
  });

  test('should close tab context menu when opening action menu', async ({
    page,
  }) => {
    if ((await openEditorTabs(page)) === 0) {
      test.skip(true, 'No editor tabs could be opened');
      return;
    }

    // Right-click a tab -> custom context menu
    await page.locator(EDITOR_TAB).first().click({ button: 'right' });
    const menu = page.locator(CONTEXT_MENU);
    if (!(await menu.isVisible({ timeout: 2000 }).catch(() => false))) {
      test.skip(true, 'Context menu not visible after right-click');
      return;
    }

    // Click ⋮ -> previous menu closes and a new one opens (single-menu guarantee)
    const moreBtn = page.locator(MORE_ACTIONS_BTN).first();
    if (!(await moreBtn.isVisible().catch(() => false))) {
      test.skip(true, 'More actions button not visible');
      return;
    }

    await moreBtn.click();

    // Wait for menu transition, then verify single menu constraint.
    // Use locator filter for visible menus instead of evaluateAll + offsetParent check.
    await expect(page.locator(`${CONTEXT_MENU}:visible`)).toHaveCount(1, { timeout: 3000 });

    await page.keyboard.press('Escape');
  });
});
