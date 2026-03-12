/**
 * OnBrick Brownfield Migration Story E2E Tests
 */

import { test, expect } from '@playwright/test';
import type { Page } from '@playwright/test';
import { type Actor, showcaseLogin, openActivity } from '../helpers/showcase.helpers';
import { E2E_API_BASE } from '../helpers/constants';

const ONBRICK_USERS = {
  minjun: { username: 'minjun.park', password: 'Test@123!', role: 'PRODUCT_OWNER', userId: '00000000-0000-0000-0005-000000000301' },
  soojin: { username: 'soojin.yoon', password: 'Test@123!', role: 'REVIEWER', userId: '00000000-0000-0000-0005-000000000302' },
  hyunwoo: { username: 'hyunwoo.kim', password: 'Test@123!', role: 'DEVELOPER', userId: '00000000-0000-0000-0005-000000000303' },
  jieun: { username: 'jieun.lee', password: 'Test@123!', role: 'DEVELOPER', userId: '00000000-0000-0000-0005-000000000304' },
} as const;

const ONBRICK_TENANT_ID = '00000000-0000-0000-0010-000000000001';
const ONBRICK_PROJECT_ID = '00000000-0000-0000-0020-000000000021';

const ACTOR_DISPLAY_NAMES: Record<string, string> = {
  minjun: 'Minjun Park',
  soojin: 'Soojin Yoon',
  hyunwoo: 'Hyunwoo Kim',
  jieun: 'Jieun Lee',
};

type OnBrickUserKey = keyof typeof ONBRICK_USERS;

function toActor(key: OnBrickUserKey): Actor {
  const user = ONBRICK_USERS[key];
  return {
    key,
    name: ACTOR_DISPLAY_NAMES[key] ?? key,
    username: user.username,
    password: user.password,
    role: user.role,
  };
}

const API = E2E_API_BASE;

async function apiLogin(request: import('@playwright/test').APIRequestContext, username: string, password: string): Promise<string> {
  const resp = await request.post(`${API}/auth/login`, {
    data: { username, password },
    failOnStatusCode: false,
  });
  const result = (await resp.json()) as Record<string, unknown>;
  const data = (result.data || result) as Record<string, unknown>;
  const token = data.accessToken;
  if (!token) throw new Error(`Login failed for ${username}: no accessToken in response`);
  return token as string;
}

function authHeaders(token: string) {
  return {
    Authorization: `Bearer ${token}`,
    'X-Tenant-Id': ONBRICK_TENANT_ID,
    'X-Project-Id': ONBRICK_PROJECT_ID,
    'Content-Type': 'application/json',
  };
}

async function login(page: Page, username: string, password: string) {
  await showcaseLogin(page, { key: username, name: username, username, password, role: '' });
}

async function navigateToConversations(page: Page) {
  await openActivity(page, 'conversations');
  await page.waitForLoadState('networkidle');
}

async function ensureChannelExists(page: Page, channelName: string, description: string) {
  const channelItem = page
    .locator(`[data-testid="channel-${channelName}"], button:has-text("${channelName}")`)
    .first();
  if (await channelItem.isVisible({ timeout: 1000 }).catch(() => false)) {
    return;
  }

  const newChannelButton = page
    .locator('button:has-text("New Channel"), button:has-text("New Conversation"), [data-testid="new-channel-button"]')
    .first();
  await newChannelButton.click();

  const channelNameInput = page
    .locator('input[name="name"], input[name="channelName"], input[placeholder*="name" i]')
    .first();
  const channelDescInput = page
    .locator('textarea[name="description"], textarea[placeholder*="description" i]')
    .first();
  const createButton = page.locator('button:has-text("Create"), button[type="submit"]').first();

  await channelNameInput.fill(channelName);
  if (await channelDescInput.isVisible({ timeout: 1000 }).catch(() => false)) {
    await channelDescInput.fill(description);
  }
  await createButton.click();

  await expect(
    page.locator(`button:has-text("${channelName}"), [data-testid="channel-${channelName}"]`).first()
  ).toBeVisible({ timeout: 10000 });
}

test.describe('OnBrick Brownfield Story - Smoke', () => {
  test('Minjun can create a migrated spec', async ({ request }) => {
    const token = await apiLogin(request, ONBRICK_USERS.minjun.username, ONBRICK_USERS.minjun.password);

    const resp = await request.post(`${API}/specs`, {
      headers: authHeaders(token),
      data: {
        code: 'SPEC-PAY-001',
        title: 'Payment System Architecture (migrated from Confluence)',
        description: 'Legacy payment design imported into SDDP.',
      },
    });

    expect(resp.ok()).toBeTruthy();
  });

  test('Minjun can access project conversations', async ({ page }) => {
    await login(page, ONBRICK_USERS.minjun.username, ONBRICK_USERS.minjun.password);
    await navigateToConversations(page);
    await ensureChannelExists(page, 'onbrick-general', 'OnBrick general channel');
  });
});
