/**
 * E2E Page Object Helpers
 * Helpers for UI interaction, navigation, sidebar control,
 * automatic re-authentication, and pacing
 */

import { Page, Locator, APIRequestContext, expect } from '@playwright/test';
import { E2E_API_BASE, E2E_HEALTH_URL, TEST_PROJECT_ID, TEST_USER } from './constants';

// ── Speed configuration ──
// In headed mode, run slowly enough for humans to observe
const IS_HEADED = !!process.env.HEADED;

/** Typing delay (ms/char). Default: HEADED=1000, headless=0 */
export const TYPE_DELAY = parseInt(process.env.E2E_TYPE_DELAY ?? (IS_HEADED ? '1000' : '0'), 10);

/** Delay before clicking (ms). Default: HEADED=4000, headless=0 */
export const CLICK_DELAY = parseInt(process.env.E2E_CLICK_DELAY ?? (IS_HEADED ? '4000' : '0'), 10);

/** In headed mode, wait after major screen changes so users can observe them. */
const SCENE_DELAY = IS_HEADED ? 3000 : 0;

export async function sceneDelay(page: Page, ms = SCENE_DELAY): Promise<void> {
  if (ms > 0) await page.waitForTimeout(ms);
}

const ACTIVITY_ALIASES: Record<string, string[]> = {
  dashboard: ['Dashboard'],
  conversations: ['Conversations', 'Discussions'],
  discussions: ['Discussions', 'Conversations'],
  projects: ['Projects', 'Specs', 'Requirements'],
  tasks: ['Tasks', 'Generator'],
  search: ['Search', 'Generator'],
  settings: ['Settings'],
  specs: ['Specs', 'Projects'],
  requirements: ['Requirements', 'Projects'],
  glossary: ['Glossary'],
  generator: ['Generator', 'Search'],
  account: ['Account'],
};

function escapeRegex(input: string): string {
  return input.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
}

function getActivityCandidates(label: string): string[] {
  const key = label.trim().toLowerCase();
  return ACTIVITY_ALIASES[key] ?? [label];
}

function activityTabLocator(page: Page, label: string): Locator {
  const activityBar = page.locator('[role="tablist"][aria-label="Activity Bar"]').first();
  return activityBar.getByRole('tab', { name: new RegExp(`^${escapeRegex(label)}$`, 'i') }).first();
}

async function resolveActivityTab(
  page: Page,
  requestedLabel: string
): Promise<{ tab: Locator; matchedLabel: string }> {
  const candidates = getActivityCandidates(requestedLabel);

  // Wait for Activity Bar to have at least one tab rendered before checking candidates
  const activityBar = page.locator('[role="tablist"][aria-label="Activity Bar"]').first();
  await activityBar
    .locator('[role="tab"]')
    .first()
    .waitFor({ state: 'visible', timeout: 10000 })
    .catch(() => {
      // Activity Bar may not be rendered yet (pre-login or loading)
    });

  for (const label of candidates) {
    const candidate = activityTabLocator(page, label);
    const visible = await candidate.isVisible({ timeout: 2000 }).catch(() => false);
    if (visible) {
      return { tab: candidate, matchedLabel: label };
    }
  }

  const available = (
    await activityBar
      .locator('[role="tab"]')
      .allTextContents()
      .catch(() => [])
  )
    .map((v) => v.trim())
    .filter(Boolean);
  throw new Error(
    `Activity tab "${requestedLabel}" not found. candidates=[${candidates.join(', ')}], available=[${available.join(', ')}]`
  );
}

/**
 * Check if the backend API is reachable
 */
export async function isBackendAvailable(request: APIRequestContext): Promise<boolean> {
  try {
    const response = await request.get(E2E_HEALTH_URL, {
      failOnStatusCode: false,
      timeout: 5000,
    });
    return response.status() === 200;
  } catch {
    return false;
  }
}

/**
 * Wait for SignalR connection
 */
export async function waitForSignalR(page: Page, timeout = 5000): Promise<void> {
  // SignalR is websocket-based. Wait for hub websocket if it appears.
  await page
    .waitForEvent('websocket', {
      timeout,
      predicate: (ws) => /hub|signalr/i.test(ws.url()),
    })
    .catch(() => {
      console.warn('[waitForSignalR] No SignalR websocket detected within timeout');
    });
}

/**
 * Navigate to an activity by clicking its button in the Activity Bar.
 * Labels: "Dashboard", "Projects", "Search", "Conversations", "Tasks", "Settings"
 *
 * Handles the toggle behavior: if the sidebar is already showing the target
 * activity panel, clicking the button would close the sidebar. This function
 * detects that case and clicks again to reopen it.
 */
export async function navigateToActivity(page: Page, activityLabel: string): Promise<void> {
  // Token expired → re-authenticate before navigating
  await ensureAuthenticated(page);

  const { tab: button } = await resolveActivityTab(page, activityLabel);
  const sidebar = page.locator('aside').first();
  await button.waitFor({ state: 'visible', timeout: 5000 });

  // Check if the target activity is already selected (aria-selected="true").
  // Clicking an already-selected tab toggles the sidebar closed → re-open flicker.
  // Avoid this by only clicking when needed.
  const alreadySelected = (await button.getAttribute('aria-selected').catch(() => null)) === 'true';
  const sidebarOpen = await sidebar.isVisible().catch(() => false);

  if (alreadySelected && sidebarOpen) {
    // Already showing the correct panel — no action needed
    await sceneDelay(page);
    return;
  }

  if (alreadySelected && !sidebarOpen) {
    // Correct panel but sidebar is collapsed — click once to reopen
    await slowClick(button);
    await sidebar.waitFor({ state: 'visible', timeout: 5000 }).catch(() => {});
    await sceneDelay(page);
    return;
  }

  // Different panel — click to switch
  await slowClick(button);
  await expect(button).toHaveAttribute('aria-selected', 'true', { timeout: 5000 });

  // Verify sidebar is visible (should always open for a new panel)
  const isVisible = await sidebar.isVisible().catch(() => false);
  if (!isVisible) {
    await button.click();
    await sidebar.waitFor({ state: 'visible', timeout: 5000 }).catch(() => {});
  }

  await sceneDelay(page);
}

/**
 * Wait for sidebar panel content to render (spinner gone, content visible).
 *
 * The sidebar <aside> element is always in the DOM but may be hidden
 * (grid column width 0px) when collapsed. This function first ensures
 * the sidebar is open, then waits for content to settle.
 */
export async function waitForSidebarContent(page: Page): Promise<void> {
  const sidebar = page.locator('aside').first();

  // Wait for the <aside> to be attached to the DOM first
  await sidebar.waitFor({ state: 'attached', timeout: 5000 });

  // Check if the sidebar is already visible
  const isVisible = await sidebar.isVisible().catch(() => false);

  if (!isVisible) {
    // Sidebar is collapsed. Try to open it by clicking the currently
    // selected activity bar button (aria-selected="true").
    const activeButton = page.locator('button[role="tab"][aria-selected="true"]').first();
    const hasActiveButton = await activeButton.isVisible({ timeout: 2000 }).catch(() => false);

    if (hasActiveButton) {
      await activeButton.click();
    } else {
      // No active button — click the first available activity button
      const firstButton = page.locator('button[role="tab"]').first();
      const hasFirstButton = await firstButton.isVisible({ timeout: 2000 }).catch(() => false);
      if (hasFirstButton) {
        await firstButton.click();
      }
    }

    // Wait for the sidebar to become visible after the click
    await sidebar.waitFor({ state: 'visible', timeout: 5000 }).catch(() => {
      // If still not visible, the sidebar may be intentionally collapsed
      // or the viewport is too small — proceed without failing
    });
  }

  // Ensure the sidebar has meaningful content and is not just an empty container.
  await expect
    .poll(
      async () => {
        const text = (await sidebar.textContent().catch(() => ''))?.trim() ?? '';
        const controlCount = await sidebar
          .locator('button, [role="button"], a, input, [role="treeitem"]')
          .count()
          .catch(() => 0);
        return text.length > 0 || controlCount > 0;
      },
      { timeout: 5000, message: 'Sidebar content did not render in time' }
    )
    .toBe(true);

  await sceneDelay(page);
}

export async function waitForSettingsSidebar(page: Page): Promise<Locator> {
  await navigateToActivity(page, 'Settings');
  await waitForSidebarContent(page);

  const sidebar = page.locator('aside').first();
  await expect(sidebar).toBeVisible({ timeout: 5000 });
  await expect(sidebar.getByText(/^Settings$/).first()).toBeVisible({ timeout: 5000 });

  return sidebar;
}

export function getSettingsMain(page: Page): Locator {
  return page.getByRole('tabpanel').first();
}

async function waitForSettingsEditorContent(page: Page): Promise<void> {
  const main = getSettingsMain(page);
  await expect(main).toBeVisible({ timeout: 8000 });

  await expect
    .poll(
      async () => {
        const loadingVisible = await main
          .getByRole('status', { name: /loading/i })
          .first()
          .isVisible()
          .catch(() => false);
        const text = ((await main.textContent().catch(() => '')) ?? '').trim();
        const controls = await main
          .locator('button, input, textarea, [role="button"], [role="heading"]')
          .count()
          .catch(() => 0);

        return !loadingVisible && (text.length > 0 || controls > 0);
      },
      {
        timeout: 10000,
        message: 'Settings editor content did not finish loading',
      }
    )
    .toBe(true);
}

export async function navigateToSettingsUserItem(
  page: Page,
  itemLabel: string
): Promise<Locator> {
  const sidebar = await waitForSettingsSidebar(page);
  const userPanel = sidebar.getByRole('button', { name: /^User$/i }).first();
  await userPanel.scrollIntoViewIfNeeded().catch(() => {});
  await expect(userPanel).toBeVisible({ timeout: 5000 });
  const isExpanded = (await userPanel.getAttribute('aria-expanded').catch(() => null)) === 'true';
  if (!isExpanded) {
    await slowClick(userPanel);
  }

  const item = sidebar.getByRole('button', { name: itemLabel, exact: true }).first();
  await item.scrollIntoViewIfNeeded().catch(() => {});
  await expect(item).toBeVisible({ timeout: 5000 });

  await slowClick(item);
  await waitForSettingsEditorContent(page);
  return sidebar;
}

export async function navigateToSettingsSystemItem(
  page: Page,
  itemLabel: string
): Promise<Locator> {
  const sidebar = await waitForSettingsSidebar(page);
  const systemPanel = sidebar.getByRole('button', { name: /^System$/i }).first();
  await systemPanel.scrollIntoViewIfNeeded().catch(() => {});
  await expect(systemPanel).toBeVisible({ timeout: 5000 });
  const isExpanded = (await systemPanel.getAttribute('aria-expanded').catch(() => null)) === 'true';
  if (!isExpanded) {
    await slowClick(systemPanel);
  }

  const item = sidebar.getByRole('button', { name: itemLabel, exact: true }).first();
  await item.scrollIntoViewIfNeeded().catch(() => {});
  await expect(item).toBeVisible({ timeout: 5000 });

  await slowClick(item);
  await waitForSettingsEditorContent(page);

  return sidebar;
}

export async function navigateToSettingsProjectItem(
  page: Page,
  itemLabel: string,
  projectName = 'E2E Test Project'
): Promise<Locator> {
  const sidebar = await waitForSettingsSidebar(page);

  const preferredProjectHeader = sidebar.getByRole('button', { name: projectName, exact: true }).first();
  const projectHeaders = sidebar.locator('button[aria-expanded]');
  const projectHeader =
    await preferredProjectHeader.isVisible({ timeout: 1000 }).catch(() => false)
      ? preferredProjectHeader
      : projectHeaders.filter({ hasNotText: /^User$|^System$/ }).first();

  await projectHeader.scrollIntoViewIfNeeded().catch(() => {});
  await expect(projectHeader).toBeVisible({ timeout: 5000 });

  const selectedProjectName = (await projectHeader.textContent())?.trim() || projectName;
  const projectRegion = sidebar
    .getByRole('region', { name: new RegExp(`^${escapeRegex(selectedProjectName)} panel$`, 'i') })
    .first();

  const isExpanded = (await projectHeader.getAttribute('aria-expanded').catch(() => null)) === 'true';
  if (!isExpanded) {
    await slowClick(projectHeader);
    await expect(projectHeader).toHaveAttribute('aria-expanded', 'true', { timeout: 5000 });
  }

  const item = projectRegion.getByRole('button', { name: itemLabel, exact: true }).first();
  await item.scrollIntoViewIfNeeded().catch(() => {});
  await expect(item).toBeVisible({ timeout: 5000 });
  await slowClick(item);
  await waitForSettingsEditorContent(page);

  return sidebar;
}

// ── Auto-authentication (token expiry recovery) ──

/**
 * Automatically logs in again when the JWT token expires.
 * If the login screen appears, it re-authenticates with the admin account.
 *
 * The autoAuth fixture performs API pre-authentication first
 * by setting the refresh-token cookie, so this usually returns immediately
 * because the Activity Bar is already visible.
 * It falls back to the UI login form only when API pre-authentication fails.
 */
export async function ensureAuthenticated(page: Page): Promise<void> {
  const activityBar = page.locator('[role="tablist"][aria-label="Activity Bar"]').first();
  const loginForm = page.locator('input[type="password"]').first();

  // Race Activity Bar (authenticated) vs login form (unauthenticated)
  try {
    await Promise.race([
      activityBar.waitFor({ state: 'visible', timeout: 5000 }),
      loginForm.waitFor({ state: 'visible', timeout: 5000 }),
    ]);
  } catch {
    console.warn('[ensureAuthenticated] Neither activity bar nor login form detected within timeout');
  }

  const needsLogin = await loginForm.isVisible().catch(() => false);
  if (!needsLogin) return;

  console.log('[AutoAuth] Fallback UI login — re-authenticating...');

  const usernameInput = page
    .locator('input[type="text"], input[name="username"], input[placeholder*="username" i]')
    .first();
  const passwordInput = page.locator('input[type="password"]').first();
  const submitButton = page.locator('button[type="submit"]').first();

  await usernameInput.fill(TEST_USER.username);
  await passwordInput.fill(TEST_USER.password);
  await submitButton.click();

  // If the Activity Bar appears, consider login successful
  await activityBar.waitFor({ state: 'visible', timeout: 15000 }).catch(() => {
    console.warn('[ensureAuthenticated] Activity bar not visible after login attempt');
  });
  // Skip sceneDelay here because autoAuth is a recovery path
}

// ── Project Tree Navigation ──

/**
 * Navigates to a project tree node from the Projects activity.
 * Automatically expands the CollapsiblePanel and clicks the target node.
 *
 * @param nodeName Tree node name (for example: 'Glossary', 'Specs', 'Requirements')
 */
export async function navigateToProjectNode(
  page: Page,
  nodeName: string,
  projectName = 'E2E Test Project',
): Promise<void> {
  await navigateToActivity(page, 'Projects');
  await waitForSidebarContent(page);

  const sidebar = page.locator('aside').first();
  const preferredProjectHeader = sidebar.getByRole('button', { name: projectName, exact: true }).first();
  const fallbackProjectHeader = sidebar.locator('button[aria-expanded]').first();
  const panelHeader = await preferredProjectHeader.isVisible({ timeout: 1000 }).catch(() => false)
    ? preferredProjectHeader
    : fallbackProjectHeader;

  await expect(panelHeader).toBeVisible({ timeout: 5000 });

  const selectedProjectName = (await panelHeader.textContent())?.trim() || projectName;
  const projectRegion = sidebar
    .getByRole('region', { name: new RegExp(`^${escapeRegex(selectedProjectName)} panel$`, 'i') })
    .first();

  const isExpanded = await panelHeader.getAttribute('aria-expanded');
  if (isExpanded !== 'true') {
    await slowClick(panelHeader);
    await expect(panelHeader).toHaveAttribute('aria-expanded', 'true', { timeout: 5000 });
  }

  const treeNode = projectRegion.getByRole('button', { name: nodeName, exact: true }).first();
  await expect(treeNode).toBeVisible({ timeout: 5000 });
  await slowClick(treeNode);

  await sceneDelay(page);
}

export async function openSpecsTab(
  page: Page,
  _projectName = 'E2E Test Project',
): Promise<Locator> {
  await page.goto(`/project/${TEST_PROJECT_ID}/specs`);

  const tabPanel = page.getByRole('tabpanel').first();
  await expect(tabPanel).toBeVisible({ timeout: 5000 });
  await expect(
    tabPanel.getByRole('button', { name: 'New Spec', exact: true }).first()
  ).toBeVisible({
    timeout: 5000,
  });

  return tabPanel;
}

export function specItems(page: Page): Locator {
  return page.getByRole('tabpanel').first().locator('button').filter({ hasText: /SPEC_/ });
}

export async function openSpecFilters(page: Page): Promise<Locator> {
  const tabPanel = await openSpecsTab(page);
  const filterTrigger = tabPanel.getByRole('button', { name: 'Toggle dropdown', exact: true }).first();

  await expect(filterTrigger).toBeVisible({ timeout: 5000 });
  await slowClick(filterTrigger);

  const menu = page.getByRole('menu', { name: 'Dropdown menu' }).first();
  await expect(menu).toBeVisible({ timeout: 5000 });

  return menu;
}

export async function openSpecCreateForm(page: Page): Promise<Locator> {
  const tabPanel = await openSpecsTab(page);
  const createButton = tabPanel.getByRole('button', { name: 'New Spec', exact: true }).first();

  await expect(createButton).toBeVisible({ timeout: 5000 });
  await slowClick(createButton);

  const form = page.locator('#spec-form, form#spec-form, form').first();
  await expect(form).toBeVisible({ timeout: 5000 });

  return form;
}

export async function openGlossaryTab(page: Page): Promise<Locator> {
  await page.goto(`/project/${TEST_PROJECT_ID}/glossary`);

  const tabPanel = page.getByRole('tabpanel').first();
  await expect(tabPanel).toBeVisible({ timeout: 5000 });
  await expect(tabPanel.getByPlaceholder(/Search terms/i).first()).toBeVisible({ timeout: 5000 });

  return tabPanel;
}

export function glossaryTermItems(page: Page): Locator {
  return page
    .getByRole('tabpanel')
    .first()
    .getByRole('treeitem')
    .filter({ hasText: /\S/ });
}

export async function openRequirementsTab(page: Page): Promise<Locator> {
  await page.goto(`/project/${TEST_PROJECT_ID}/requirements`);

  const tabPanel = page.getByRole('tabpanel').first();
  await expect(tabPanel).toBeVisible({ timeout: 5000 });
  await expect(tabPanel.getByPlaceholder(/Search requirements/i).first()).toBeVisible({ timeout: 5000 });

  return tabPanel;
}

export function requirementListItems(page: Page): Locator {
  return page
    .getByRole('tabpanel')
    .first()
    .getByRole('button')
    .filter({ hasText: /REQ_|Requirement|UI Test Requirement|Level [ABC]/i });
}

// ── Editor Tab helpers ──

/** Editor tab selector, distinct from Activity Bar tabs */
const EDITOR_TAB = '[role="tab"][data-tab-id]';

/**
 * Conversation sidebar channel item selector.
 * Targets the <button> rendered by App.svelte's channelsPanel snippet.
 * Each button contains a span.truncate, which helps distinguish it from other aside buttons.
 */
const CHANNEL_BUTTON = 'aside button[type="button"]:has(span.truncate)';

/**
 * Ensures that project-scoped test channels exist via the API, creating them if needed.
 *
 * Main flow:
 * 1. Log in to obtain accessToken + tenantId
 * 2. GET /projects to obtain the project ID for the tenant
 * 3. GET /conversations (X-Project-Id) to check project-scoped channels
 * 4. POST /conversations (X-Project-Id) when more channels are needed
 *
 * Because global-setup.ts pre-creates an E2E project in the System Tenant,
 * the admin user always has at least one project available.
 *
 * @returns Number of available channels
 */
async function ensureTestChannels(page: Page, count: number): Promise<number> {
  // 1. Login to get access token + tenantId
  const loginResp = await page.request.post(`${E2E_API_BASE}/auth/login`, {
    data: { username: TEST_USER.username, password: TEST_USER.password },
    failOnStatusCode: false,
  });
  if (!loginResp.ok()) return 0;

  const loginData = await loginResp.json();
  const data = loginData.data || loginData;
  const accessToken = data.accessToken;
  const tenantId = data.user?.tenantId;
  if (!accessToken || !tenantId) return 0;

  const baseHeaders = {
    Authorization: `Bearer ${accessToken}`,
    'X-Tenant-Id': tenantId,
  };

  // 2. Get the first project for this tenant
  const projectsResp = await page.request.get(`${E2E_API_BASE}/projects`, {
    headers: baseHeaders,
    failOnStatusCode: false,
  });
  if (!projectsResp.ok()) return 0;

  const projectsData = await projectsResp.json();
  const projects = projectsData.data || projectsData || [];
  if (!Array.isArray(projects) || projects.length === 0) return 0;

  const projectId = projects[0].id;
  if (!projectId) return 0;

  const headers = {
    ...baseHeaders,
    'X-Project-Id': projectId,
  };

  // 3. Check existing project-scoped channels
  let existingCount = 0;
  const convResp = await page.request.get(`${E2E_API_BASE}/conversations`, {
    headers,
    failOnStatusCode: false,
  });
  if (convResp.ok()) {
    const convData = await convResp.json();
    const conversations = convData.data || convData || [];
    const channels = Array.isArray(conversations)
      ? conversations.filter((c: { conversationType: string }) => c.conversationType === 'Channel')
      : [];
    existingCount = channels.length;
    if (existingCount >= count) return existingCount;
  }

  // 4. Create missing project-scoped channels
  const needed = Math.max(0, count - existingCount);
  for (let i = 0; i < needed; i++) {
    const resp = await page.request.post(`${E2E_API_BASE}/conversations`, {
      headers,
      data: {
        name: `e2e-channel-${Date.now()}-${i}`,
        conversationType: 'Channel',
        visibility: 'Public',
        description: 'Auto-created by E2E test',
      },
      failOnStatusCode: false,
    });
    if (!resp.ok()) {
      console.warn(`[ensureTestChannels] Failed to create channel (${resp.status()})`);
    }
  }

  return existingCount + needed;
}

/**
 * Clicks sidebar channel items to create editor tabs.
 *
 * Preconditions (global-setup.ts):
 * - An E2E project must exist in the System Tenant
 *
 * Flow:
 * 1. Create project-scoped channels via the API if needed
 * 2. Reload the page so App.svelte reloads the project and channels
 * 3. Navigate to Conversations so the channel list is shown
 * 4. Click a channel to create an editor tab
 *
 * @returns Number of tabs created
 */
export async function openEditorTabs(page: Page, count = 1): Promise<number> {
  // 1. Ensure project-scoped channels exist via API
  const available = await ensureTestChannels(page, count);
  if (available === 0) return 0;

  // 2. Reload page so App.svelte re-fetches projects + conversations
  await page.reload({ waitUntil: 'networkidle' }).catch(() => {
    console.warn('[openEditorTabs] Page reload did not reach networkidle');
  });
  await ensureAuthenticated(page);

  // 3. Navigate to Conversations (handles sidebar toggle)
  //    Some environments hide Conversations/Discussions in Activity Bar.
  //    In that case, return 0 so caller tests can skip with a clear precondition.
  const navigatedToConversations = await navigateToActivity(page, 'Conversations')
    .then(() => true)
    .catch((error) => {
      console.warn(
        `[openEditorTabs] Unable to navigate to Conversations: ${error instanceof Error ? error.message : String(error)}`
      );
      return false;
    });
  if (!navigatedToConversations) return 0;

  await waitForSidebarContent(page).catch((error) => {
    console.warn(
      `[openEditorTabs] Sidebar content did not settle: ${error instanceof Error ? error.message : String(error)}`
    );
  });

  // 4. Wait for channel items to appear in sidebar
  //    App.svelte channelsPanel renders <button> with span.truncate
  const items = page.locator(CHANNEL_BUTTON);
  await items
    .first()
    .waitFor({ state: 'visible', timeout: 10000 })
    .catch(() => {});
  const itemCount = await items.count();
  if (itemCount === 0) return 0;

  // 5. Click channel items to create editor tabs
  let tabsCreated = 0;
  for (let i = 0; i < Math.min(count, itemCount); i++) {
    await items.nth(i).click();
    await page
      .locator(EDITOR_TAB)
      .first()
      .waitFor({ state: 'visible', timeout: 3000 })
      .catch(() => {});
    tabsCreated = await page.locator(EDITOR_TAB).count();
    if (tabsCreated >= count) break;
  }

  return tabsCreated;
}

// ── Slow interaction helpers ──

/**
 * Types text at an observable speed.
 * HEADED mode: 1 character per second by default (configurable through E2E_TYPE_DELAY).
 * Headless mode: uses fill() immediately.
 */
export async function slowType(locator: Locator, text: string, page?: Page): Promise<void> {
  if (TYPE_DELAY > 0) {
    await locator.pressSequentially(text, { delay: TYPE_DELAY });
    if (page) await page.waitForTimeout(300);
  } else {
    await locator.fill(text);
  }
}

/**
 * Clicks at an observable speed.
 * HEADED mode: waits 4 seconds before clicking by default (configurable through E2E_CLICK_DELAY).
 * Headless mode: clicks immediately.
 */
export async function slowClick(locator: Locator): Promise<void> {
  if (CLICK_DELAY > 0) {
    await locator.page().waitForTimeout(CLICK_DELAY);
  }
  await locator.scrollIntoViewIfNeeded().catch(() => {});

  try {
    await locator.click({ timeout: 3000 });
  } catch {
    // Headed E2E runs occasionally see transient panel/footer overlays intercept
    // pointer events even after the target is visible and stable.
    await locator.click({ force: true, timeout: 3000 });
  }
}
