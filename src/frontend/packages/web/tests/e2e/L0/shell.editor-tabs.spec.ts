
import { test, expect } from '../helpers/fixtures';
import { openEditorTabs } from '../helpers/page-object.helpers';

/* * Editor (Activity Bar) */
const EDITOR_TAB = '[role="tab"][data-tab-id]';
const CONTEXT_MENU = '[role="menu"]';
const MENU_ITEM = '[role="menuitem"]';
// IconButton exposes its `title` as the accessible name, and the product's is
// "More Actions". The old selector said "More actions" and matched nothing -- which
// only ever showed up as a skip.
const MORE_ACTIONS_BTN = 'button[aria-label="More Actions"]';

test.describe('Tab Content Rendering', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/');
  });

  test('should render content when tab is selected', async ({ page }) => {
    const opened = await openEditorTabs(page);
    expect(opened, 'the Conversations panel must be able to open an editor tab').toBeGreaterThan(0);

    const editorTabs = page.locator(EDITOR_TAB);
    await editorTabs.first().click();

    const contentArea = page.locator('[role="tabpanel"]').first();
    await expect(contentArea).toBeVisible({ timeout: 3000 });
  });

  test('should switch content when changing tabs', async ({ page }) => {
    const opened = await openEditorTabs(page, 2);
    expect(opened).toBeGreaterThanOrEqual(2);

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
    expect(opened, 'the Conversations panel must be able to open an editor tab').toBeGreaterThan(0);

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
    expect(opened, 'the Conversations panel must be able to open an editor tab').toBeGreaterThan(0);

    // Editor →
    await page.locator(EDITOR_TAB).first().click({ button: 'right' });
    const menu = page.locator(CONTEXT_MENU);
    await expect(menu).toBeVisible();

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
    expect(opened, 'the Conversations panel must be able to open an editor tab').toBeGreaterThan(0);

    const editorTabs = page.locator(EDITOR_TAB);
    const initialCount = await editorTabs.count();

    await editorTabs.first().click({ button: 'right' });
    const menu = page.locator(CONTEXT_MENU);
    await expect(menu).toBeVisible();

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
    expect(opened).toBeGreaterThanOrEqual(2);

    const editorTabs = page.locator(EDITOR_TAB);

    // → Close Others
    await editorTabs.first().click({ button: 'right' });
    const menu = page.locator(CONTEXT_MENU);
    await expect(menu).toBeVisible();

    const closeOthers = menu
      .locator(`${MENU_ITEM}:has-text("Close Others")`)
      .first();
    await expect(closeOthers).toBeEnabled();

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
    expect(opened, 'the Conversations panel must be able to open an editor tab').toBeGreaterThan(0);

    const moreBtn = page.locator(MORE_ACTIONS_BTN).first();
    await expect(moreBtn).toBeVisible();

    const editorGroups = page.locator('[aria-label*="Editor group"]');
    const initialGroupCount = await editorGroups.count().catch(() => 1);

    await moreBtn.click();
    const menu = page.locator(CONTEXT_MENU);
    const splitRight = menu
      .locator(`${MENU_ITEM}:has-text("Split Right")`)
      .first();
    await expect(splitRight).toBeVisible();

    await splitRight.click();

    await expect.poll(
      async () => editorGroups.count().catch(() => 1),
      { timeout: 3000 }
    ).toBeGreaterThan(initialGroupCount);
  });

  test('should split editor down via action menu', async ({ page }) => {
    const opened = await openEditorTabs(page);
    expect(opened, 'the Conversations panel must be able to open an editor tab').toBeGreaterThan(0);

    const moreBtn = page.locator(MORE_ACTIONS_BTN).first();
    await expect(moreBtn).toBeVisible();

    const editorGroups = page.locator('[aria-label*="Editor group"]');
    const initialGroupCount = await editorGroups.count().catch(() => 1);

    await moreBtn.click();
    const menu = page.locator(CONTEXT_MENU);
    const splitDown = menu
      .locator(`${MENU_ITEM}:has-text("Split Down")`)
      .first();
    await expect(splitDown).toBeVisible();

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
    expect(opened, 'the Conversations panel must be able to open an editor tab').toBeGreaterThan(0);

    const moreBtn = page.locator(MORE_ACTIONS_BTN).first();
    await expect(moreBtn).toBeVisible({ timeout: 3000 });
  });
});
