/**
 * E2E Showcase Helpers
 * DemoController, actor sessions, and window layout utilities for headed demos
 */

/// <reference types="node" />
import { chromium, expect, Page } from '@playwright/test';
import {
  navigateToActivity as navigateShellActivity,
  navigateToProjectNode,
  waitForSidebarContent,
} from './page-object.helpers';
import { E2E_API_BASE } from './constants';

// ========== Types ==========

export type Actor = {
  key: string;
  name: string;
  username: string;
  password: string;
  role: string;
};

export type WindowSlot = {
  x: number;
  y: number;
  width: number;
  height: number;
};

export type ActorSession = {
  actor: Actor;
  page: Page;
  close: () => Promise<void>;
};

type ShowcaseAuthContext = {
  token: string;
  tenantId: string;
};

type ShowcaseProjectContext = ShowcaseAuthContext & {
  projectId: string;
  projectName: string;
};

// ========== Constants ==========

export const DEVFLOW_ACTORS: Actor[] = [
  { key: 'alex', name: 'Alex Kim', username: 'alex.kim', password: 'Test@123!', role: 'PRODUCT_OWNER' },
  { key: 'jordan', name: 'Jordan Lee', username: 'jordan.lee', password: 'Test@123!', role: 'DOMAIN_EXPERT' },
  { key: 'taylor', name: 'Taylor Park', username: 'taylor.park', password: 'Test@123!', role: 'DEVELOPER' },
  { key: 'morgan', name: 'Morgan Choi', username: 'morgan.choi', password: 'Test@123!', role: 'DEVELOPER' },
  { key: 'casey', name: 'Casey Seo', username: 'casey.seo', password: 'Test@123!', role: 'REVIEWER' },
  { key: 'riley', name: 'Riley Han', username: 'riley.han', password: 'Test@123!', role: 'QA_TESTER' },
];

const SHOWCASE_PROJECT_PREFERENCES = [
  'DevFlow MVP 1.0',
  'OnBrick Payment System',
  'E2E Test Project',
] as const;

// ========== Utilities ==========

export function parseEnvInt(name: string, fallback: number): number {
  const value = process.env[name];
  if (!value) {
    return fallback;
  }
  const parsed = Number.parseInt(value, 10);
  return Number.isNaN(parsed) ? fallback : parsed;
}

function escapeRegex(input: string): string {
  return input.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
}

export function buildWindowSlots(): WindowSlot[] {
  const width = parseEnvInt('DEMO_WINDOW_WIDTH', 620);
  const height = parseEnvInt('DEMO_WINDOW_HEIGHT', 470);
  const gapX = parseEnvInt('DEMO_WINDOW_GAP_X', 12);
  const gapY = parseEnvInt('DEMO_WINDOW_GAP_Y', 56);
  const startX = parseEnvInt('DEMO_WINDOW_START_X', 0);
  const startY = parseEnvInt('DEMO_WINDOW_START_Y', 0);

  // Use seven slots in a 3x3 grid layout (0-6)
  return [
    { x: startX, y: startY, width, height },
    { x: startX + width + gapX, y: startY, width, height },
    { x: startX + (width + gapX) * 2, y: startY, width, height },
    { x: startX, y: startY + height + gapY, width, height },
    { x: startX + width + gapX, y: startY + height + gapY, width, height },
    { x: startX + (width + gapX) * 2, y: startY + height + gapY, width, height },
    { x: startX, y: startY + (height + gapY) * 2, width, height },
  ];
}

// ========== DemoController ==========

/**
 * Controller for driving the demo flow.
 * Supports pause (p), next step (n), quit (q), and help (h) through keyboard input.
 */
export class DemoController {
  private paused = false;
  private stepMode = false;
  private stopped = false;
  private keyboardEnabled = false;
  private readonly delayMs: number;
  private readonly onData = (buf: Buffer | string) => {
    const key = typeof buf === 'string' ? buf : buf.toString('utf8');
    if (key === 'p') {
      this.paused = !this.paused;
      console.log(this.paused ? '\n[DEMO] Paused' : '\n[DEMO] Resumed');
      return;
    }
    if (key === 'n') {
      this.stepMode = true;
      this.paused = false;
      console.log('\n[DEMO] Step once');
      return;
    }
    if (key === 'q') {
      this.stopped = true;
      this.paused = false;
      console.log('\n[DEMO] Quit requested');
      return;
    }
    if (key === 'h') {
      this.printHelp();
    }
  };

  constructor(delayMs?: number) {
    this.delayMs = delayMs ?? parseEnvInt('SHOWCASE_DELAY_MS', 3000);
  }

  setupKeyboard() {
    if (!process.stdin.isTTY) {
      console.log('[DEMO] Keyboard control disabled because stdin is not a TTY.');
      return;
    }
    this.keyboardEnabled = true;
    process.stdin.setEncoding('utf8');
    process.stdin.resume();
    if (typeof process.stdin.setRawMode === 'function') {
      process.stdin.setRawMode(true);
    }
    process.stdin.on('data', this.onData);
    this.printHelp();
  }

  teardownKeyboard() {
    if (!this.keyboardEnabled) {
      return;
    }
    process.stdin.off('data', this.onData);
    if (typeof process.stdin.setRawMode === 'function') {
      process.stdin.setRawMode(false);
    }
    process.stdin.pause();
  }

  private printHelp() {
    console.log('[DEMO] controls: p=pause/resume, n=next step, q=quit, h=help');
  }

  /**
   * Print a console banner on scene changes and apply the default delay.
   */
  async checkpoint(label: string) {
    console.log(`\n${'='.repeat(60)}`);
    console.log(`[SCENE] ${label}`);
    console.log(`${'='.repeat(60)}`);
    await this.delay(this.delayMs);
  }

  async delay(ms: number) {
    let remaining = ms;
    while (remaining > 0) {
      await this.waitIfNeeded();
      const tick = Math.min(remaining, 200);
      await new Promise((resolve) => setTimeout(resolve, tick));
      remaining -= tick;
    }
  }

  private async waitIfNeeded() {
    if (this.stopped) {
      throw new Error('Demo was stopped by operator');
    }

    if (this.stepMode) {
      this.stepMode = false;
      this.paused = true;
      return;
    }

    while (this.paused) {
      if (this.stopped) {
        throw new Error('Demo was stopped by operator');
      }
      await new Promise((resolve) => setTimeout(resolve, 120));
      if (this.stepMode) {
        this.stepMode = false;
        this.paused = true;
        break;
      }
    }
  }
}

// ========== Actor Session Management ==========

/**
 * Start an actor session in an independent browser window.
 */
export async function launchActorSession(actor: Actor, slot: WindowSlot): Promise<ActorSession> {
  console.log(`[LAUNCH] ${actor.key} (${actor.role}) — browser starting...`);
  const browser = await chromium.launch({
    headless: false,
    args: [
      `--window-size=${slot.width},${slot.height}`,
      `--window-position=${slot.x},${slot.y}`,
      '--disable-infobars',
    ],
  });

  const context = await browser.newContext({
    viewport: { width: slot.width, height: slot.height },
    baseURL: process.env.E2E_WEB_BASE_URL ?? 'http://localhost:3500',
  });
  const page = await context.newPage();
  await page.bringToFront();
  console.log(`[LAUNCH] ${actor.key} — browser ready at (${slot.x},${slot.y})`);

  return {
    actor,
    page,
    close: async () => {
      await context.close();
      await browser.close();
    },
  };
}

/**
 * key.
 */
export function byKey(sessions: ActorSession[], key: string): ActorSession {
  const found = sessions.find((s) => s.actor.key === key);
  if (!found) {
    throw new Error(`Session not found for ${key}`);
  }
  return found;
}

async function loginActorApi(page: Page, actor: Actor): Promise<ShowcaseAuthContext> {
  const loginResp = await page.request.post(`${E2E_API_BASE}/auth/login`, {
    data: { username: actor.username, password: actor.password },
    failOnStatusCode: false,
  });
  if (!loginResp.ok()) {
    throw new Error(`Showcase API login failed for ${actor.username} (${loginResp.status()})`);
  }

  const loginJson = await loginResp.json();
  const loginData = loginJson.data || loginJson || {};
  const token = loginData.accessToken || loginData.token;
  const tenantId = loginData.user?.tenantId || loginData.tenantId;
  if (!token || !tenantId) {
    throw new Error(`Showcase API login missing token/tenantId for ${actor.username}`);
  }

  return { token, tenantId };
}

async function resolveProjectContext(
  page: Page,
  actor: Actor,
  preferredProjectNames = SHOWCASE_PROJECT_PREFERENCES,
): Promise<ShowcaseProjectContext> {
  const auth = await loginActorApi(page, actor);
  const projectResp = await page.request.get(`${E2E_API_BASE}/projects`, {
    headers: {
      Authorization: `Bearer ${auth.token}`,
      'X-Tenant-Id': auth.tenantId,
    },
    failOnStatusCode: false,
  });

  if (!projectResp.ok()) {
    throw new Error(`Project lookup failed for ${actor.username} (${projectResp.status()})`);
  }

  const projectJson = await projectResp.json();
  const projectData = projectJson.data || projectJson || [];
  const projects = Array.isArray(projectData)
    ? projectData
    : Array.isArray(projectData.items)
      ? projectData.items
      : [];

  const selectedProject =
    preferredProjectNames
      .map((name) => projects.find((project: Record<string, unknown>) => project.name === name))
      .find(Boolean) ??
    projects[0];

  if (!selectedProject?.id || !selectedProject?.name) {
    throw new Error(`No showcase project available for ${actor.username}`);
  }

  return {
    ...auth,
    projectId: selectedProject.id as string,
    projectName: selectedProject.name as string,
  };
}

export async function ensureShowcaseChannel(
  page: Page,
  actor: Actor,
  channelName: string,
  description: string,
): Promise<void> {
  const project = await resolveProjectContext(page, actor);

  const headers = {
    Authorization: `Bearer ${project.token}`,
    'X-Tenant-Id': project.tenantId,
    'X-Project-Id': project.projectId,
    'Content-Type': 'application/json',
  };

  const listResp = await page.request.get(`${E2E_API_BASE}/conversations?pageNumber=1&pageSize=100`, {
    headers,
    failOnStatusCode: false,
  });
  if (!listResp.ok()) {
    throw new Error(`Conversation lookup failed for ${channelName} (${listResp.status()})`);
  }

  const listJson = await listResp.json();
  const listData = listJson.data || listJson || [];
  const conversations = Array.isArray(listData)
    ? listData
    : Array.isArray(listData.items)
      ? listData.items
      : [];

  const existing = conversations.find(
    (conversation: Record<string, unknown>) =>
      String(conversation.name || '').toLowerCase() === channelName.toLowerCase() &&
      String(conversation.conversationType || '') === 'Channel'
  );
  if (existing) {
    return;
  }

  const createResp = await page.request.post(`${E2E_API_BASE}/conversations`, {
    headers,
    data: {
      name: channelName,
      description,
      conversationType: 'Channel',
      visibility: 'Public',
    },
    failOnStatusCode: false,
  });

  if (!createResp.ok()) {
    throw new Error(`Channel create failed for ${channelName} (${createResp.status()})`);
  }
}

export async function selectShowcaseChannel(page: Page, channelName: string): Promise<void> {
  const main = page.locator('main').first();
  const channelItem = main
    .getByRole('button', {
      name: new RegExp(`^(?:#\\s*)?${escapeRegex(channelName)}(?:\\s+\\d+)?$`, 'i'),
    })
    .first();

  await expect(channelItem).toBeVisible({ timeout: 10000 });
  await channelItem.click();
  await page.waitForLoadState('networkidle').catch(() => undefined);
}

// ========== Showcase UI Helpers ==========

/**
 * log (showcase)
 */
export async function showcaseLogin(page: Page, actor: Actor) {
  console.log(`[LOGIN] ${actor.key} — navigating to /`);
  await page.goto('/');
  await page.waitForLoadState('networkidle');
  console.log(`[LOGIN] ${actor.key} — page loaded, filling credentials`);

  const usernameInput = page.locator('input[type="text"], input[name="username"], input[placeholder*="username" i]').first();
  const passwordInput = page.locator('input[type="password"]').first();
  const submitButton = page.locator('button[type="submit"]').first();

  await usernameInput.fill(actor.username);
  await passwordInput.fill(actor.password);
  await submitButton.click();
  console.log(`[LOGIN] ${actor.key} — submitted, waiting for networkidle`);

  await page.waitForLoadState('networkidle');
  const appShell = page.locator('[role="tablist"][aria-label="Activity Bar"]').first();
  await expect(appShell).toBeVisible({ timeout: 10000 });
  console.log(`[LOGIN] ${actor.key} — login complete`);
}

/**
 * (showcase)
 */
export async function openActivity(page: Page, activity: 'conversations' | 'requirements' | 'specs' | 'tasks') {
  console.log(`[ACTIVITY] Opening "${activity}"...`);
  if (activity === 'tasks') {
    await navigateShellActivity(page, 'Tasks');
    await waitForSidebarContent(page);
    await page.waitForLoadState('networkidle').catch(() => undefined);
    console.log(`[ACTIVITY] "${activity}" opened`);
    return;
  }

  const nodeName =
    activity === 'conversations'
      ? 'Conversations'
      : activity === 'requirements'
        ? 'Requirements'
        : 'Specs';

  for (const projectName of SHOWCASE_PROJECT_PREFERENCES) {
    try {
      await navigateToProjectNode(page, nodeName, projectName);
      await page.waitForLoadState('networkidle').catch(() => undefined);
      console.log(`[ACTIVITY] "${activity}" opened via project "${projectName}"`);
      return;
    } catch {
      // Try the next project candidate.
    }
  }

  const sidebar = page.locator('aside').first();
  await navigateShellActivity(page, 'Projects');
  await waitForSidebarContent(page);

  const projectHeaders = sidebar.locator('button[aria-expanded]');
  const headerCount = await projectHeaders.count();

  for (let i = 0; i < headerCount; i += 1) {
    const header = projectHeaders.nth(i);
    const projectName = ((await header.textContent()) ?? '').trim();
    if (!projectName) continue;

    const isExpanded = (await header.getAttribute('aria-expanded').catch(() => null)) === 'true';
    if (!isExpanded) {
      await header.click();
    }

    const region = sidebar
      .getByRole('region', { name: new RegExp(`^${escapeRegex(projectName)} panel$`, 'i') })
      .first();
    const node = region.getByRole('button', { name: nodeName, exact: true }).first();
    const visible = await node.isVisible({ timeout: 1000 }).catch(() => false);
    if (!visible) continue;

    await node.click();
    await page.waitForLoadState('networkidle').catch(() => undefined);
    console.log(`[ACTIVITY] "${activity}" opened via detected project "${projectName}"`);
    return;
  }

  console.log(`[ACTIVITY] WARNING: "${activity}" node not found in any project`);
}

/**
 * message (showcase)
 */
export async function showcaseSendMessage(page: Page, text: string, type: 'proposal' | 'decision' | 'none' = 'none') {
  console.log(`[MSG] Sending ${type !== 'none' ? `[${type}] ` : ''}${text.slice(0, 60)}...`);
  const input = page.locator('textarea[placeholder*="Message #" i], textarea[placeholder*="message" i]').first();
  const sendButton = page.locator('button[title="Send (Enter)"]').first();

  await expect(input).toBeVisible({ timeout: 5000 });
  await input.fill(text);
  const sendVisible = await sendButton.isVisible({ timeout: 1000 }).catch(() => false);
  if (sendVisible) {
    try {
      await sendButton.click({ timeout: 3000, force: true });
    } catch {
      await input.press('Enter');
    }
  } else {
    await input.press('Enter');
  }
  console.log(`[MSG] Sent`);
}
