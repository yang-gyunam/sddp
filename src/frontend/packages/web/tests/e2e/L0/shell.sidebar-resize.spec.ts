/**
 * Sidebar Resize E2E Tests
 * Tests for sidebar resize functionality using PointerEvents
 *
 * Prerequisites: autoAuth fixture handles authentication.
 * Sidebar and resize handles are expected to be visible post-auth.
 */

import { test, expect } from '../helpers/fixtures';
import { navigateToActivity } from '../helpers/page-object.helpers';

test.describe('Sidebar Resize - Basic Functionality', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/');
    await page.waitForLoadState('networkidle');
  });

  test('should have resize handle on sidebar', async ({ page }) => {
    const sidebar = page.locator('aside').first();
    await expect(sidebar).toBeVisible();

    // Look for resize handle (typically on the right edge)
    const resizeHandle = sidebar.locator('[role="separator"], [class*="resize"]');
    const hasHandle = await resizeHandle.isVisible().catch(() => false);

    expect(hasHandle).toBe(true);
  });

  test('should change cursor to col-resize when hovering resize handle', async ({
    page,
  }) => {
    const sidebar = page.locator('aside').first();
    await expect(sidebar).toBeVisible();

    const resizeHandle = sidebar.locator('[role="separator"], [class*="resize"]').first();
    await expect(resizeHandle).toBeVisible();

    // Hover over resize handle
    await resizeHandle.hover();

    // Check cursor style
    await expect(resizeHandle).toHaveCSS('cursor', 'col-resize');
  });

  test('should resize sidebar when dragging the handle', async ({ page }) => {
    const sidebar = page.locator('aside').first();
    await expect(sidebar).toBeVisible();

    // Get initial sidebar width
    const initialBox = await sidebar.boundingBox();
    expect(initialBox, 'expected a sidebar bounding box').not.toBeNull();
    const initialWidth = initialBox!.width;

    // Find resize handle
    const resizeHandle = sidebar.locator('[role="separator"], [class*="resize"]').first();
    await expect(resizeHandle).toBeVisible();

    const handleBox = await resizeHandle.boundingBox();
    expect(handleBox, 'expected a resize handle bounding box').not.toBeNull();

    // Drag handle to the right by 50px
    await page.mouse.move(handleBox!.x + handleBox!.width / 2, handleBox!.y + handleBox!.height / 2);
    await page.mouse.down();
    await page.mouse.move(handleBox!.x + handleBox!.width / 2 + 50, handleBox!.y + handleBox!.height / 2, { steps: 10 });
    await page.mouse.up();

    // Wait for sidebar width to update
    await expect.poll(
      async () => {
        const box = await sidebar.boundingBox();
        return box?.width ?? 0;
      },
      { timeout: 3000 }
    ).toBeGreaterThan(initialWidth);

    // Get new sidebar width
    const newBox = await sidebar.boundingBox();
    const newWidth = newBox?.width ?? 0;

    // Width should have increased
    expect(newWidth).toBeGreaterThan(initialWidth);
  });

  test('should apply and clear drag interaction styles while resizing', async ({
    page,
  }) => {
    const sidebar = page.locator('aside').first();
    await expect(sidebar).toBeVisible();

    const resizeHandle = sidebar.locator('[role="separator"], [class*="resize"]').first();
    await expect(resizeHandle).toBeVisible();

    const handleBox = await resizeHandle.boundingBox();
    expect(handleBox, 'expected a resize handle bounding box').not.toBeNull();

    // Start dragging
    await page.mouse.move(handleBox!.x + handleBox!.width / 2, handleBox!.y + handleBox!.height / 2);
    await page.mouse.down();

    await expect
      .poll(
        () => page.evaluate(() => ({
          cursor: document.body.style.cursor,
          userSelect: document.body.style.userSelect,
        })),
        { timeout: 1000 }
      )
      .toEqual({ cursor: 'col-resize', userSelect: 'none' });

    // Move and release
    await page.mouse.move(handleBox!.x + handleBox!.width / 2 + 30, handleBox!.y + handleBox!.height / 2, { steps: 5 });
    await page.mouse.up();

    await expect.poll(
      () => page.evaluate(() => ({
        cursor: document.body.style.cursor,
        userSelect: document.body.style.userSelect,
      })),
      { timeout: 3000 }
    ).toEqual({ cursor: '', userSelect: '' });
  });
});

test.describe('Sidebar Resize - Constraints', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/');
    await page.waitForLoadState('networkidle');
  });

  test('should not resize below minimum width', async ({ page }) => {
    const sidebar = page.locator('aside').first();
    await expect(sidebar).toBeVisible();

    const resizeHandle = sidebar.locator('[role="separator"], [class*="resize"]').first();
    await expect(resizeHandle).toBeVisible();

    const handleBox = await resizeHandle.boundingBox();
    expect(handleBox, 'expected a resize handle bounding box').not.toBeNull();

    // Drag handle far to the left (attempt to make very narrow)
    await page.mouse.move(handleBox!.x + handleBox!.width / 2, handleBox!.y + handleBox!.height / 2);
    await page.mouse.down();
    await page.mouse.move(handleBox!.x - 300, handleBox!.y + handleBox!.height / 2, { steps: 10 });
    await page.mouse.up();

    // Get sidebar width
    const finalBox = await sidebar.boundingBox();
    const finalWidth = finalBox?.width ?? 0;

    // Width should be at least minWidth (200px as per spec)
    expect(finalWidth).toBeGreaterThanOrEqual(180); // Allow some tolerance
  });

  test('should not resize above maximum width', async ({ page }) => {
    const sidebar = page.locator('aside').first();
    await expect(sidebar).toBeVisible();

    const resizeHandle = sidebar.locator('[role="separator"], [class*="resize"]').first();
    await expect(resizeHandle).toBeVisible();

    const handleBox = await resizeHandle.boundingBox();
    const viewportSize = page.viewportSize();
    expect(handleBox, 'the resize handle must have a box to drag').not.toBeNull();
    expect(viewportSize, 'the page must report a viewport').not.toBeNull();

    // Drag handle far to the right (attempt to make very wide)
    await page.mouse.move(handleBox!.x + handleBox!.width / 2, handleBox!.y + handleBox!.height / 2);
    await page.mouse.down();
    await page.mouse.move(viewportSize!.width - 50, handleBox!.y + handleBox!.height / 2, { steps: 10 });
    await page.mouse.up();

    // Get sidebar width
    const finalBox = await sidebar.boundingBox();
    const finalWidth = finalBox?.width ?? 0;

    // Width should be at most 50% of viewport (or 600px as per spec)
    const maxWidth = Math.min(600, viewportSize!.width * 0.5);
    expect(finalWidth).toBeLessThanOrEqual(maxWidth + 20); // Allow some tolerance
  });
});

test.describe('Sidebar Panels Resize', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/');
    await page.waitForLoadState('networkidle');
  });

  test('should move panel separator in sync with pointer drag', async ({ page }) => {
    // Dragging the separator between panels needs two of them, and the default activity has one.
    // Projects renders a panel per project, so the seeded data makes the precondition real
    // instead of hoped for.
    await navigateToActivity(page, 'Projects');

    const sidebar = page.locator('aside').first();
    await expect(sidebar).toBeVisible();

    // Native <button>, so match the element rather than an explicit role attribute.
    const panelHeaders = sidebar.locator('button[aria-expanded]');
    const headerCount = await panelHeaders.count().catch(() => 0);
    expect(headerCount, 'the sidebar must render more than one collapsible panel')
      .toBeGreaterThanOrEqual(2);

    // This checks unconstrained movement, not the minimum-height clamp. With many
    // persisted projects all panels otherwise sit at their 120px minimum.
    const viewport = page.viewportSize();
    expect(viewport).not.toBeNull();
    await page.setViewportSize({
      width: viewport!.width,
      height: Math.max(viewport!.height, headerCount * 180 + 120),
    });

    for (let i = 0; i < 2; i += 1) {
      const expanded = await panelHeaders.nth(i).getAttribute('aria-expanded');
      if (expanded !== 'true') {
        await panelHeaders.nth(i).click();
      }
    }

    const resizeHandle = sidebar.locator('[aria-label="Resize panels"]').first();
    await expect(resizeHandle).toBeVisible();

    const handleBoxBefore = await resizeHandle.boundingBox();
    expect(handleBoxBefore, 'expected a panel resize handle bounding box before drag').not.toBeNull();

    const startX = handleBoxBefore!.x + handleBoxBefore!.width / 2;
    const startY = handleBoxBefore!.y + handleBoxBefore!.height / 2;
    const dragDistance = 30;

    await page.mouse.move(startX, startY);
    await page.mouse.down();
    await page.mouse.move(startX, startY + dragDistance, { steps: 10 });
    await page.mouse.up();

    const handleBoxAfter = await resizeHandle.boundingBox();
    expect(handleBoxAfter, 'expected a panel resize handle bounding box after drag').not.toBeNull();

    const deltaY = handleBoxAfter!.y - handleBoxBefore!.y;
    expect(Math.abs(deltaY - dragDistance)).toBeLessThanOrEqual(12);
  });
});

test.describe('Sidebar Collapse Toggle', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/');
    await page.waitForLoadState('networkidle');
  });

  test('should have collapse button in sidebar header', async ({ page }) => {
    const sidebar = page.locator('aside').first();
    await expect(sidebar).toBeVisible();

    // Look for collapse button
    const collapseButton = sidebar.locator(
      'button[title*="Collapse"], button[aria-label*="Collapse"], button:has([class*="panel-left"])'
    ).first();
    const hasButton = await collapseButton.isVisible().catch(() => false);

    expect(hasButton).toBe(true);
  });

  test('should collapse sidebar when clicking collapse button', async ({
    page,
  }) => {
    const sidebar = page.locator('aside').first();
    await expect(sidebar).toBeVisible();

    const initialBox = await sidebar.boundingBox();
    expect(initialBox, 'expected a sidebar bounding box').not.toBeNull();
    const initialWidth = initialBox!.width;

    const collapseButton = sidebar.locator(
      'button[title*="Collapse"], button[aria-label*="Collapse"], button:has([class*="panel-left"])'
    ).first();
    await expect(collapseButton).toBeVisible();

    await collapseButton.click();

    // Check if sidebar collapsed
    const newBox = await sidebar.boundingBox().catch(() => null);
    const newWidth = newBox?.width ?? 0;

    // Width should decrease significantly or sidebar should be hidden
    expect(newWidth).toBeLessThan(initialWidth);
  });

  test('should show expand button when sidebar is collapsed', async ({
    page,
  }) => {
    const sidebar = page.locator('aside').first();
    await expect(sidebar).toBeVisible();

    const collapseButton = sidebar.locator(
      'button[title*="Collapse"], button[aria-label*="Collapse"], button:has([class*="panel-left"])'
    ).first();
    await expect(collapseButton).toBeVisible();

    // Collapse the sidebar
    await collapseButton.click();

    // Look for expand button
    const expandButton = page.locator(
      'button[title*="Expand"], button[aria-label*="Expand"], button:has([class*="panel-left-open"])'
    ).first();
    const hasExpandButton = await expandButton.isVisible().catch(() => false);

    expect(hasExpandButton).toBe(true);
  });
});

test.describe('Sidebar Header Actions', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/');
    await page.waitForLoadState('networkidle');
  });

  test('should have hover effect on header action buttons', async ({
    page,
  }) => {
    const sidebar = page.locator('aside').first();
    await expect(sidebar).toBeVisible();

    // Find header action buttons
    const headerButtons = sidebar.locator('button').first();
    await expect(headerButtons).toBeVisible();

    // Get initial background
    // NOTE: evaluate() is used here to capture the initial computed value for comparison.
    // Playwright's toHaveCSS() does not support "not equal to snapshot" assertions.
    const initialBg = await headerButtons.evaluate((el) => {
      return window.getComputedStyle(el).backgroundColor;
    });

    // Hover over button
    await headerButtons.hover();

    // Hover should change background color
    await expect.poll(
      async () => headerButtons.evaluate((el) => window.getComputedStyle(el).backgroundColor),
      { timeout: 3000 }
    ).not.toBe(initialBg);
  });

  test('should have cursor pointer on header action buttons', async ({
    page,
  }) => {
    const sidebar = page.locator('aside').first();
    await expect(sidebar).toBeVisible();

    const headerButtons = sidebar.locator('button').first();
    await expect(headerButtons).toBeVisible();

    await headerButtons.hover();

    await expect(headerButtons).toHaveCSS('cursor', 'pointer');
  });
});

test.describe('Sidebar Resize Accessibility', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/');
    await page.waitForLoadState('networkidle');
  });

  test('should have proper ARIA role on resize handle', async ({ page }) => {
    const sidebar = page.locator('aside').first();
    await expect(sidebar).toBeVisible();

    const resizeHandle = sidebar.locator('[role="separator"]').first();
    await expect(resizeHandle).toBeVisible();

    // Check for separator role
    const role = await resizeHandle.getAttribute('role');
    expect(role).toBe('separator');

    // Check for orientation
    const orientation = await resizeHandle.getAttribute('aria-orientation');
    expect(orientation).toBe('vertical');
  });

  test('should be keyboard accessible', async ({ page }) => {
    const sidebar = page.locator('aside').first();
    await expect(sidebar).toBeVisible();

    const resizeHandle = sidebar.locator('[role="separator"]').first();
    await expect(resizeHandle).toBeVisible();

    // Check if focusable
    const tabIndex = await resizeHandle.getAttribute('tabindex');
    expect(tabIndex).not.toBeNull();
  });
});
