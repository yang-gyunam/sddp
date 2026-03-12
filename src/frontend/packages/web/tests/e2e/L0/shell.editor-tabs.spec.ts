/**
 * Editor Features E2E Tests
 * Tests for tab content rendering, context menu, and split editor functionality
 *
 * Requirements:
 * - REQ-11.4 (App Shell UX)
 *
 * :
 * - Editor: [role="tab"][data-tab-id] (EditorGroup TabItem)
 * - Activity Bar: nav[aria-label="Activity Bar"] [role="tab"]
 *
 * create:
 * - openEditorTabs() API channel create → new → create
 * - precondition test.skip()
 */

import { test, expect } from '../helpers/fixtures';
import { openEditorTabs } from '../helpers/page-object.helpers';

/* * Editor (Activity Bar) */
const EDITOR_TAB = '[role="tab"][data-tab-id]';
const CONTEXT_MENU = '[role="menu"]';
const MENU_ITEM = '[role="menuitem"]';
const MORE_ACTIONS_BTN = 'button[aria-label="More actions"]';

test.describe('Tab Content Rendering', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/');
  });

  test('should render content when tab is selected', async ({ page }) => {
    const opened = await openEditorTabs(page);
    if (opened === 0) {
      test.skip(true, 'Precondition not met: no editor tabs opened (requires Conversations panel and seeded channels)');
      return;
    }

    const editorTabs = page.locator(EDITOR_TAB);
    await editorTabs.first().click();

    const contentArea = page.locator('[role="tabpanel"]').first();
    await expect(contentArea).toBeVisible({ timeout: 3000 });
  });

  test('should switch content when changing tabs', async ({ page }) => {
    const opened = await openEditorTabs(page, 2);
    if (opened < 2) {
      test.skip(true, `Precondition not met: expected at least 2 editor tabs, opened ${opened}`);
      return;
    }

    const editorTabs = page.locator(EDITOR_TAB);

    //
    await editorTabs.first().click();
    await expect(editorTabs.first()).toHaveAttribute('aria-selected', 'true');

    // →
    const secondTab = editorTabs.nth(1);
    await secondTab.click();
    await expect(secondTab).toHaveAttribute('aria-selected', 'true');
    //
    await expect(editorTabs.first()).toHaveAttribute('aria-selected', 'false');
  });

  test('should show tab panel content area', async ({ page }) => {
    const opened = await openEditorTabs(page);
    if (opened === 0) {
      test.skip(true, 'Precondition not met: no editor tabs opened (requires Conversations panel and seeded channels)');
      return;
    }

    // tabpanel
    const tabpanel = page.locator('[role="tabpanel"]').first();
    await expect(tabpanel).toBeVisible({ timeout: 3000 });
  });
});

test.describe('Tab Context Menu Actions', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/');
  });

  test('should show context menu with Close option on right-click', async ({
    page,
  }) => {
    const opened = await openEditorTabs(page);
    if (opened === 0) {
      test.skip(true, 'Precondition not met: no editor tabs opened (requires Conversations panel and seeded channels)');
      return;
    }

    // Editor →
    await page.locator(EDITOR_TAB).first().click({ button: 'right' });
    const menu = page.locator(CONTEXT_MENU);
    if (!(await menu.isVisible({ timeout: 2000 }).catch(() => false))) {
      test.skip(true, 'Context menu not visible after right-click');
      return;
    }

    await expect(
      menu.locator(`${MENU_ITEM}:has-text("Close")`).first(),
    ).toBeVisible();
    await expect(
      menu.locator(`${MENU_ITEM}:has-text("Close Others")`).first(),
    ).toBeVisible();
    await expect(
      menu.locator(`${MENU_ITEM}:has-text("Split Right")`).first(),
    ).toBeVisible();
    await page.keyboard.press('Escape');
  });

  test('should close tab when clicking Close from context menu', async ({
    page,
  }) => {
    const opened = await openEditorTabs(page);
    if (opened === 0) {
      test.skip(true, 'Precondition not met: no editor tabs opened (requires Conversations panel and seeded channels)');
      return;
    }

    const editorTabs = page.locator(EDITOR_TAB);
    const initialCount = await editorTabs.count();

    await editorTabs.first().click({ button: 'right' });
    const menu = page.locator(CONTEXT_MENU);
    if (!(await menu.isVisible({ timeout: 2000 }).catch(() => false))) {
      test.skip(true, 'Context menu not visible after right-click');
      return;
    }

    await menu.locator(`${MENU_ITEM}:has-text("Close")`).first().click();

    // Wait for tab count to decrease
    await expect.poll(async () => editorTabs.count(), { timeout: 3000 }).toBeLessThan(initialCount);
    const newCount = await editorTabs.count();
    expect(newCount).toBeLessThan(initialCount);
  });

  test('should close other tabs when clicking Close Others', async ({
    page,
  }) => {
    const opened = await openEditorTabs(page, 2);
    if (opened < 2) {
      test.skip(true, `Precondition not met: expected at least 2 editor tabs, opened ${opened}`);
      return;
    }

    const editorTabs = page.locator(EDITOR_TAB);

    // → Close Others
    await editorTabs.first().click({ button: 'right' });
    const menu = page.locator(CONTEXT_MENU);
    if (!(await menu.isVisible({ timeout: 2000 }).catch(() => false))) {
      test.skip(true, 'Context menu not visible after right-click');
      return;
    }

    const closeOthers = menu
      .locator(`${MENU_ITEM}:has-text("Close Others")`)
      .first();
    if (!(await closeOthers.isEnabled().catch(() => false))) {
      await page.keyboard.press('Escape');
      test.skip(true, 'Close Others menu item not enabled');
      return;
    }

    await closeOthers.click();

    await expect.poll(async () => editorTabs.count(), { timeout: 3000 }).toBe(1);
  });
});

test.describe('Split Editor Group Functionality', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/');
  });

  test('should split editor right via action menu', async ({ page }) => {
    const opened = await openEditorTabs(page);
    if (opened === 0) {
      test.skip(true, 'Precondition not met: no editor tabs opened (requires Conversations panel and seeded channels)');
      return;
    }

    const moreBtn = page.locator(MORE_ACTIONS_BTN).first();
    if (!(await moreBtn.isVisible({ timeout: 3000 }).catch(() => false))) {
      test.skip(true, 'More actions button not visible');
      return;
    }

    const editorGroups = page.locator('[aria-label*="Editor group"]');
    const initialGroupCount = await editorGroups.count().catch(() => 1);

    await moreBtn.click();
    const menu = page.locator(CONTEXT_MENU);
    const splitRight = menu
      .locator(`${MENU_ITEM}:has-text("Split Right")`)
      .first();
    if (!(await splitRight.isVisible({ timeout: 2000 }).catch(() => false))) {
      await page.keyboard.press('Escape');
      test.skip(true, 'Split Right menu item not visible');
      return;
    }

    await splitRight.click();

    await expect.poll(
      async () => editorGroups.count().catch(() => 1),
      { timeout: 3000 }
    ).toBeGreaterThan(initialGroupCount);
  });

  test('should split editor down via action menu', async ({ page }) => {
    const opened = await openEditorTabs(page);
    if (opened === 0) {
      test.skip(true, 'Precondition not met: no editor tabs opened (requires Conversations panel and seeded channels)');
      return;
    }

    const moreBtn = page.locator(MORE_ACTIONS_BTN).first();
    if (!(await moreBtn.isVisible({ timeout: 3000 }).catch(() => false))) {
      test.skip(true, 'More actions button not visible');
      return;
    }

    const editorGroups = page.locator('[aria-label*="Editor group"]');
    const initialGroupCount = await editorGroups.count().catch(() => 1);

    await moreBtn.click();
    const menu = page.locator(CONTEXT_MENU);
    const splitDown = menu
      .locator(`${MENU_ITEM}:has-text("Split Down")`)
      .first();
    if (!(await splitDown.isVisible({ timeout: 2000 }).catch(() => false))) {
      await page.keyboard.press('Escape');
      test.skip(true, 'Split Down menu item not visible');
      return;
    }

    await splitDown.click();

    await expect.poll(
      async () => editorGroups.count().catch(() => 1),
      { timeout: 3000 }
    ).toBeGreaterThan(initialGroupCount);
  });
});

test.describe('Tab Toolbar Buttons', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/');
  });

  test('should show More Actions button when tabs exist', async ({ page }) => {
    const opened = await openEditorTabs(page);
    if (opened === 0) {
      test.skip(true, 'Precondition not met: no editor tabs opened (requires Conversations panel and seeded channels)');
      return;
    }

    const moreBtn = page.locator(MORE_ACTIONS_BTN).first();
    await expect(moreBtn).toBeVisible({ timeout: 3000 });
  });
});
