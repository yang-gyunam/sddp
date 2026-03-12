/**
 * E2E Accessibility (a11y) Tests — WCAG 2.1 AA
 * axe-core WCAG 2.1 AA
 *
 * audit: T-24
 * : axe-core DOM. / QA.
 */

import { test as baseTest, expect as baseExpect } from '@playwright/test';
import { test, expect } from '../helpers/fixtures';
import AxeBuilder from '@axe-core/playwright';
import type { Page } from '@playwright/test';
import { navigateToActivity, waitForSidebarContent } from '../helpers/page-object.helpers';

/* * axe-core (AxeBuilder.analyze()) */
type AxeResults = Awaited<ReturnType<AxeBuilder['analyze']>>;

/**
 * axe-core WCAG 2.1 AA.
 * (tiptap).
 */
async function checkA11y(
  page: Page,
  options?: { exclude?: string[] }
): Promise<AxeResults> {
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  const builder = new AxeBuilder({ page } as any)
    .withTags(['wcag2a', 'wcag2aa'])
    .exclude('.tiptap-editor')
    .exclude('.ProseMirror');

  if (options?.exclude) {
    for (const selector of options.exclude) {
      builder.exclude(selector);
    }
  }

  const results = await builder.analyze();
  return results;
}

/* * */
function formatViolations(violations: AxeResults['violations']): string {
  return violations
    .map(
      (v) =>
        `[${v.impact}] ${v.id}: ${v.description} (${v.nodes.length} nodes)\n` +
        `  Help: ${v.helpUrl}\n` +
        v.nodes
          .slice(0, 3)
          .map((n) => `  - ${n.html.substring(0, 120)}`)
          .join('\n')
    )
    .join('\n\n');
}

// ── Login Screen (status) ──

baseTest.describe('A11y — Login Screen', () => {
  baseTest.beforeEach(async ({ page }) => {
    await page.context().clearCookies();
    await page.goto('/');
    await page.waitForLoadState('networkidle');

    // log
    await page
      .locator('input[type="password"]')
      .first()
      .waitFor({ state: 'visible', timeout: 10000 });
  });

  baseTest('login page should have no WCAG 2.1 AA violations', async ({ page }) => {
    const results = await checkA11y(page);

    if (results.violations.length > 0) {
      console.warn(
        `[a11y] Login page violations (${results.violations.length}):\n${formatViolations(results.violations)}`
      );
    }

    baseExpect(results.violations).toEqual([]);
  });
});

// ── (autoAuth fixture) ──

test.describe('A11y — App Shell', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/');
    // autoAuth fixture
    const activityBar = page
      .locator('[role="tablist"][aria-label="Activity Bar"]')
      .first();
    await expect(activityBar).toBeVisible({ timeout: 15000 });
  });

  test('app shell should have no WCAG 2.1 AA violations', async ({ page }) => {
    const results = await checkA11y(page);

    if (results.violations.length > 0) {
      console.warn(
        `[a11y] App shell violations (${results.violations.length}):\n${formatViolations(results.violations)}`
      );
    }

    expect(results.violations).toEqual([]);
  });
});

test.describe('A11y — Dashboard', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/');
    const activityBar = page
      .locator('[role="tablist"][aria-label="Activity Bar"]')
      .first();
    await expect(activityBar).toBeVisible({ timeout: 15000 });
  });

  test('dashboard should have no WCAG 2.1 AA violations', async ({ page }) => {
    await navigateToActivity(page, 'Dashboard');
    await waitForSidebarContent(page);

    const results = await checkA11y(page);

    if (results.violations.length > 0) {
      console.warn(
        `[a11y] Dashboard violations (${results.violations.length}):\n${formatViolations(results.violations)}`
      );
    }

    expect(results.violations).toEqual([]);
  });
});

test.describe('A11y — Settings', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/');
    const activityBar = page
      .locator('[role="tablist"][aria-label="Activity Bar"]')
      .first();
    await expect(activityBar).toBeVisible({ timeout: 15000 });
  });

  test('settings should have no WCAG 2.1 AA violations', async ({ page }) => {
    await navigateToActivity(page, 'Settings');
    await waitForSidebarContent(page);

    const results = await checkA11y(page);

    if (results.violations.length > 0) {
      console.warn(
        `[a11y] Settings violations (${results.violations.length}):\n${formatViolations(results.violations)}`
      );
    }

    expect(results.violations).toEqual([]);
  });
});

test.describe('A11y — Projects', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/');
    const activityBar = page
      .locator('[role="tablist"][aria-label="Activity Bar"]')
      .first();
    await expect(activityBar).toBeVisible({ timeout: 15000 });
  });

  test('projects should have no WCAG 2.1 AA violations', async ({ page }) => {
    await navigateToActivity(page, 'Projects');
    await waitForSidebarContent(page);

    const results = await checkA11y(page);

    if (results.violations.length > 0) {
      console.warn(
        `[a11y] Projects violations (${results.violations.length}):\n${formatViolations(results.violations)}`
      );
    }

    expect(results.violations).toEqual([]);
  });
});

test.describe('A11y — Conversations', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/');
    const activityBar = page
      .locator('[role="tablist"][aria-label="Activity Bar"]')
      .first();
    await expect(activityBar).toBeVisible({ timeout: 15000 });
  });

  test('conversations should have no WCAG 2.1 AA violations', async ({
    page,
  }) => {
    await navigateToActivity(page, 'Conversations');
    await waitForSidebarContent(page);

    const results = await checkA11y(page);

    if (results.violations.length > 0) {
      console.warn(
        `[a11y] Conversations violations (${results.violations.length}):\n${formatViolations(results.violations)}`
      );
    }

    expect(results.violations).toEqual([]);
  });
});
