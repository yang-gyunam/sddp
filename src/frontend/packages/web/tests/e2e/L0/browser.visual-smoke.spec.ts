import { test, expect } from '../helpers/fixtures';
import { sceneDelay } from '../helpers/page-object.helpers';

function activityTab(page: import('@playwright/test').Page, pattern: RegExp) {
  return page
    .locator('[role="tablist"][aria-label="Activity Bar"]')
    .first()
    .getByRole('tab', { name: pattern })
    .first();
}

async function clickAndVerifyActivity(
  page: import('@playwright/test').Page,
  pattern: RegExp,
  sidebarHints: RegExp
): Promise<void> {
  const tab = activityTab(page, pattern);
  await expect(tab).toBeVisible({ timeout: 10000 });
  await tab.click();
  await expect(tab).toHaveAttribute('aria-selected', 'true', { timeout: 5000 });

  const sidebar = page.locator('aside').first();
  await expect(sidebar).toBeVisible({ timeout: 5000 });
  await expect(sidebar).toContainText(sidebarHints, { timeout: 5000 });
  await sceneDelay(page, 1500);
}

test.describe('Visual Browser Smoke', () => {
  test('should visibly switch major activities', async ({ page }) => {
    await page.goto('/');

    const activityBar = page.locator('[role="tablist"][aria-label="Activity Bar"]').first();
    await expect(activityBar).toBeVisible({ timeout: 10000 });
    await sceneDelay(page, 1000);

    await clickAndVerifyActivity(page, /Conversations|Discussions/i, /Channels|Forums|Direct Messages/i);
    await clickAndVerifyActivity(page, /Projects|Specs|Requirements/i, /Project|Specs|Timeline|Artifacts/i);
    await clickAndVerifyActivity(page, /Tasks|Generator/i, /My Tasks|Backlog|Tasks/i);
    await clickAndVerifyActivity(page, /Settings/i, /User|Project|System|Profile|Preferences/i);
  });
});
