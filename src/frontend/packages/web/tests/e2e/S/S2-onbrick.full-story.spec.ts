/** Two bounded checks of the seeded OnBrick migration flow. */
import { randomUUID } from 'node:crypto';
import { test, expect } from '@playwright/test';
import { showcaseLogin, selectShowcaseChannel } from '../helpers/showcase.helpers';
import { navigateToProjectNode } from '../helpers/page-object.helpers';
import { E2E_API_BASE } from '../helpers/constants';

const tenantId = '00000000-0000-0000-0010-000000000001';
const projectId = '00000000-0000-0000-0020-000000000021';
const actor = {
  key: 'minjun', name: 'Minjun Park', username: 'minjun.park',
  password: 'Test@123!', role: 'PRODUCT_OWNER',
};

test.describe('OnBrick Brownfield Story - Smoke', () => {
  test('Minjun can create a migrated spec', async ({ request }) => {
    const login = await request.post(`${E2E_API_BASE}/auth/login`, {
      data: { username: actor.username, password: actor.password },
    });
    expect(login.status()).toBe(200);
    const token = (await login.json()).data.accessToken;
    expect(typeof token).toBe('string');
    const headers = {
      Authorization: `Bearer ${token}`, 'X-Tenant-Id': tenantId, 'X-Project-Id': projectId,
    };
    // Keep the seeded SPEC-PAY-001 and allow repeated runs without collisions.
    const input = {
      code: `SMOKE-PAY-${randomUUID()}`,
      title: 'Payment System Architecture (migrated from Confluence)',
      description: 'Legacy payment design imported into SDDP.',
      decision: 'Support percentage and fixed-amount partial refunds.',
    };
    const created = await request.post(`${E2E_API_BASE}/specs`, { headers, data: input });
    expect(created.status(), await created.text()).toBe(201);
    const spec = (await created.json()).data;
    expect(spec.id).toMatch(/^[0-9a-f-]{36}$/i);
    try {
      expect(spec).toMatchObject({ ...input, tenantId, projectId });
      const read = await request.get(`${E2E_API_BASE}/specs/${spec.id}`, { headers });
      expect(read.status()).toBe(200);
      expect((await read.json()).data).toMatchObject({ id: spec.id, ...input, tenantId, projectId });
    } finally {
      const removed = await request.delete(`${E2E_API_BASE}/specs/${spec.id}`, { headers });
      expect(removed.status(), await removed.text()).toBe(200);
    }
  });

  test('Minjun can access project conversations', async ({ page }) => {
    const serverErrors: string[] = [];
    page.on('response', (response) => {
      if (response.url().includes('/api/') && response.status() >= 500) {
        serverErrors.push(`${response.status()} ${new URL(response.url()).pathname}`);
      }
    });
    await showcaseLogin(page, actor);
    // Pin the intended project; do not fall back to a different available project.
    await navigateToProjectNode(page, 'Conversations', 'OnBrick Payment System');
    await expect(page.getByRole('button', { name: 'New Channel', exact: true })).toBeVisible();
    await selectShowcaseChannel(page, 'onbrick-general');
    await expect(page.getByPlaceholder('Message #onbrick-general')).toBeVisible();
    await selectShowcaseChannel(page, 'design-review');
    await expect(page.getByText('Partial refunds will support both percentage and fixed amounts', { exact: false })).toBeVisible();
    expect(serverErrors).toEqual([]);
  });
});
