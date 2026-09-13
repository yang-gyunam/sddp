import { test, expect } from '../helpers/fixtures';
import { TEST_PROJECT_ID } from '../helpers/constants';

test.describe('Split editor — real project pages', () => {
  test('splits an opened project page into an independently rendered editor group', async ({
    page,
  }) => {
    await page.goto(`/project/${TEST_PROJECT_ID}/requirements`);

    const editorTab = page.locator('[role="tab"][data-tab-id]').filter({ hasText: 'Requirements' });
    await expect(editorTab.first()).toBeVisible({ timeout: 10_000 });

    const groups = page.getByRole('group', { name: /^Editor group \d+$/ });
    await expect(groups).toHaveCount(1);

    await groups.first().getByTitle('Split Right').click();

    await expect(groups).toHaveCount(2);
    await expect(
      page.locator('[role="separator"][aria-orientation="horizontal"]').first()
    ).toBeVisible();

    for (const group of await groups.all()) {
      await expect(group.getByRole('tabpanel')).toBeVisible();
      await expect(group.locator('[role="tab"][data-tab-id]').filter({ hasText: 'Requirements' }))
        .toBeVisible();
    }
  });
});
