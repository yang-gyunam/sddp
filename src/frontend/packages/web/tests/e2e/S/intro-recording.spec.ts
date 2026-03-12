/**
 * intro-recording.spec.ts
 * Playwright script to record animated GIFs for docs/intro.md
 *
 * Usage:
 *   cd src/frontend/packages/web
 *   HEADED=1 npx playwright test tests/e2e/S/intro-recording.spec.ts --project=browser
 *
 * Output: tests/e2e/.output/intro/*.webm → convert with scripts/mov2gif.sh
 */

/// <reference types="node" />
import { test, expect } from '@playwright/test';
import type { BrowserContext, Page } from '@playwright/test';

// ── Constants ──

const SARAH = { username: 'sarah.lee', password: 'Test@123!' };
const BASE_URL = process.env.E2E_WEB_BASE_URL ?? 'http://localhost:3500';
const VIDEO_DIR = 'tests/e2e/.output/intro';
const SCENE_PAUSE = 2500;
const SHORT_PAUSE = 1200;

// ── Helpers ──

async function pause(page: Page, ms = SCENE_PAUSE) {
  await page.waitForTimeout(ms);
}

async function login(page: Page) {
  await page.goto('/');
  await page.waitForLoadState('networkidle');

  const usernameInput = page
    .locator('input[type="text"], input[name="username"], input[placeholder*="username" i]')
    .first();
  const passwordInput = page.locator('input[type="password"]').first();
  const submitButton = page.locator('button[type="submit"]').first();

  await usernameInput.fill(SARAH.username);
  await passwordInput.fill(SARAH.password);
  await submitButton.click();

  const activityBar = page.locator('[role="tablist"][aria-label="Activity Bar"]').first();
  await expect(activityBar).toBeVisible({ timeout: 15000 });
  await page.waitForLoadState('networkidle');
  await pause(page, SHORT_PAUSE);
}

async function clickActivityTab(page: Page, label: string) {
  const activityBar = page.locator('[role="tablist"][aria-label="Activity Bar"]').first();
  const tab = activityBar.getByRole('tab', { name: new RegExp(label, 'i') }).first();

  if (await tab.isVisible({ timeout: 5000 }).catch(() => false)) {
    await tab.click();
  } else {
    const fallback = page.locator(`button:has-text("${label}")`).first();
    await fallback.click();
  }
  await page.waitForLoadState('networkidle');
  await pause(page, SHORT_PAUSE);
}

async function waitForSidebar(page: Page) {
  const sidebar = page.locator('aside').first();
  await sidebar.waitFor({ state: 'visible', timeout: 8000 }).catch(() => {});
  await page.waitForLoadState('networkidle');
}

/** Click an item in the sidebar (project tree nodes like Specs, Requirements, etc.) */
async function clickSidebarNode(page: Page, text: string) {
  const sidebar = page.locator('aside').first();
  const item = sidebar.locator(`button:has-text("${text}"), div[role="button"]:has-text("${text}")`).first();
  await item.waitFor({ state: 'visible', timeout: 8000 });
  await item.click();
  await page.waitForLoadState('networkidle');
}

/** Click a visible item in the spec/requirement LIST (main content area) */
async function clickListItem(page: Page, text: string, required = true) {
  // Use getByText with exact-ish matching to find the visible item
  // Filter to only visible elements to avoid hidden tab content
  const item = page
    .locator(`button:visible:has-text("${text}"), tr:visible:has-text("${text}"), div[role="button"]:visible:has-text("${text}")`)
    .first();
  const visible = await item.isVisible({ timeout: 5000 }).catch(() => false);
  if (!visible) {
    if (required) {
      await item.waitFor({ state: 'visible', timeout: 10000 });
    }
    console.log(`[intro-recording] List item "${text}" not found, skipping step`);
    return false;
  }
  await item.scrollIntoViewIfNeeded();
  await item.click();
  await page.waitForLoadState('networkidle');
  return true;
}

async function createRecordingContext(
  browser: import('@playwright/test').Browser,
  subdir: string,
): Promise<BrowserContext> {
  return browser.newContext({
    viewport: { width: 1680, height: 1050 },
    recordVideo: {
      dir: `${VIDEO_DIR}/${subdir}`,
      size: { width: 1680, height: 1050 },
    },
    baseURL: BASE_URL,
  });
}

// ── Test Suite ──

test.describe('Intro Page Recordings', () => {
  test.setTimeout(5 * 60 * 1000);

  // ────────────────────────────────────────────────────────
  // 4. Spec Status Flow: Draft → InReview → Approved → Locked
  // ────────────────────────────────────────────────────────
  test('04 — Spec status flow', async ({ browser }) => {
    const context = await createRecordingContext(browser, '04-spec-flow');
    const page = await context.newPage();

    try {
      await login(page);

      // Open Specs via Projects sidebar
      await clickActivityTab(page, 'Projects');
      await waitForSidebar(page);
      await clickSidebarNode(page, 'Specs');
      await pause(page);

      // ① Draft — SPEC-DATA-001 (click in the spec list, not sidebar)
      console.log('[04] Clicking SPEC-DATA-001 (Draft)');
      if (await clickListItem(page, 'SPEC-DATA-001', false)) {
        await pause(page, SCENE_PAUSE);
      }

      // ② InReview — SPEC-CACHE-001
      console.log('[04] Clicking SPEC-CACHE-001 (InReview)');
      if (await clickListItem(page, 'SPEC-CACHE-001', false)) {
        await pause(page, SCENE_PAUSE);
      }

      // ③ Approved — SPEC-AUTH-001
      console.log('[04] Clicking SPEC-AUTH-001 (Approved)');
      if (await clickListItem(page, 'SPEC-AUTH-001', false)) {
        await pause(page, SCENE_PAUSE);
      }

      // ④ Locked — SPEC-DB-001
      console.log('[04] Clicking SPEC-DB-001 (Locked)');
      if (await clickListItem(page, 'SPEC-DB-001', false)) {
        await pause(page, SCENE_PAUSE);
      }

      console.log('[04] Spec status flow recording complete');
    } finally {
      await context.close();
    }
  });

  // ────────────────────────────────────────────────────────
  // 5. SDDP Square: Conversation → Requirement → Spec → Code
  // ────────────────────────────────────────────────────────
  test('05 — SDDP square flow', async ({ browser }) => {
    const context = await createRecordingContext(browser, '05-sddp-square');
    const page = await context.newPage();

    try {
      await login(page);

      // ① Conversations (project-scoped: via Projects → Conversations)
      console.log('[05] Opening project Conversations');
      await clickActivityTab(page, 'Projects');
      await waitForSidebar(page);
      await clickSidebarNode(page, 'Conversations');
      await pause(page, SHORT_PAUSE);

      // Click architecture forum in the conversation list (main area)
      console.log('[05] Clicking architecture forum');
      if (await clickListItem(page, 'architecture', false)) {
        await pause(page, SHORT_PAUSE);
      }

      // Click JWT topic in the forum topics list
      console.log('[05] Clicking JWT topic');
      const jwtTopic = page
        .locator('button:has-text("JWT"), [role="button"]:has-text("JWT"), tr:has-text("JWT")')
        .first();
      if (await jwtTopic.isVisible({ timeout: 5000 }).catch(() => false)) {
        await jwtTopic.click();
        await page.waitForLoadState('networkidle');
      }
      await pause(page, SCENE_PAUSE);

      // ② Requirements → REQ-A-001
      console.log('[05] Opening Requirements');
      await clickSidebarNode(page, 'Requirements');
      await pause(page, SHORT_PAUSE);

      console.log('[05] Clicking REQ-A-001');
      if (await clickListItem(page, 'REQ-A-001', false)) {
        await pause(page, SCENE_PAUSE);
      }

      // ③ Specs → SPEC-AUTH-001
      console.log('[05] Opening Specs');
      await clickSidebarNode(page, 'Specs');
      await pause(page, SHORT_PAUSE);

      console.log('[05] Clicking SPEC-AUTH-001');
      if (await clickListItem(page, 'SPEC-AUTH-001', false)) {
        await pause(page, SCENE_PAUSE);
      }

      console.log('[05] SDDP square flow recording complete');
    } finally {
      await context.close();
    }
  });

  // ────────────────────────────────────────────────────────
  // 6. App Shell Overview
  // ────────────────────────────────────────────────────────
  test('06 — App shell overview', async ({ browser }) => {
    const context = await createRecordingContext(browser, '06-app-shell');
    const page = await context.newPage();

    try {
      await login(page);

      // ① Dashboard
      console.log('[06] Dashboard');
      await clickActivityTab(page, 'Dashboard');
      await waitForSidebar(page);
      await pause(page, SCENE_PAUSE);

      // ② Projects — click through sidebar menu items
      console.log('[06] Projects — sidebar tour');
      await clickActivityTab(page, 'Projects');
      await waitForSidebar(page);

      // Click various sidebar nodes to show the rich menu structure
      for (const node of ['Specs', 'Requirements', 'Conversations', 'Glossary', 'Artifacts', 'Tasks', 'Traceability']) {
        console.log(`[06] Clicking sidebar: ${node}`);
        await clickSidebarNode(page, node).catch(() => {
          console.log(`[06] Sidebar node "${node}" not found, skipping`);
        });
        await pause(page, 800);
      }

      // ③ Open multiple specs to show tab system
      console.log('[06] Opening Specs for tab demo');
      await clickSidebarNode(page, 'Specs');
      await pause(page, SHORT_PAUSE);

      for (const specCode of ['SPEC-AUTH-001', 'SPEC-API-001', 'SPEC-CACHE-001']) {
        console.log(`[06] Opening ${specCode}`);
        await clickListItem(page, specCode, false);
        await pause(page, SHORT_PAUSE);
      }
      await pause(page, SHORT_PAUSE);

      // ④ Reorder tabs by dragging (move 2nd tab to 1st position)
      console.log('[06] Reorder tabs by dragging');
      const allTabs = page.locator('[role="tab"][data-tab-id]');
      const tabCount = await allTabs.count();
      if (tabCount >= 2) {
        const srcTab = allTabs.nth(1);
        const dstTab = allTabs.nth(0);
        const srcBox = await srcTab.boundingBox();
        const dstBox = await dstTab.boundingBox();
        if (srcBox && dstBox) {
          await page.mouse.move(srcBox.x + srcBox.width / 2, srcBox.y + srcBox.height / 2);
          await page.mouse.down();
          await pause(page, 300);
          // Move slowly so drag is visible
          const steps = 10;
          for (let i = 1; i <= steps; i++) {
            const x = srcBox.x + (dstBox.x - srcBox.x) * (i / steps);
            const y = srcBox.y + srcBox.height / 2;
            await page.mouse.move(x, y);
            await page.waitForTimeout(50);
          }
          await page.mouse.up();
          await pause(page, SHORT_PAUSE);
        }
      }

      // ⑤ Right-click a tab → Split Right
      console.log('[06] Right-click tab → Split Right');
      const editorTab = allTabs.first();
      if (await editorTab.isVisible({ timeout: 3000 }).catch(() => false)) {
        await editorTab.click({ button: 'right' });
        await pause(page, SHORT_PAUSE);

        // Click "Split Right" in context menu
        const splitRight = page
          .locator('button:has-text("Split Right"), [role="menuitem"]:has-text("Split Right")')
          .first();
        if (await splitRight.isVisible({ timeout: 3000 }).catch(() => false)) {
          await splitRight.click();
          await page.waitForLoadState('networkidle');
        }
        await pause(page, SCENE_PAUSE);
      }

      // ⑥ Drag a tab from left group to right group (cross-panel move)
      console.log('[06] Drag tab between editor groups');
      // After split, there should be two tab bars. Drag from 1st group to 2nd group.
      const tabBars = page.locator('[role="tablist"]:not([aria-label="Activity Bar"])');
      const tabBarCount = await tabBars.count();
      if (tabBarCount >= 2) {
        const leftGroupTab = tabBars.nth(0).locator('[role="tab"][data-tab-id]').first();
        const rightGroupArea = tabBars.nth(1);
        const leftBox = await leftGroupTab.boundingBox();
        const rightBox = await rightGroupArea.boundingBox();
        if (leftBox && rightBox) {
          await page.mouse.move(leftBox.x + leftBox.width / 2, leftBox.y + leftBox.height / 2);
          await page.mouse.down();
          await pause(page, 300);
          const steps = 15;
          for (let i = 1; i <= steps; i++) {
            const targetX = rightBox.x + rightBox.width / 2;
            const x = leftBox.x + (targetX - leftBox.x) * (i / steps);
            const y = rightBox.y + rightBox.height / 2;
            await page.mouse.move(x, y);
            await page.waitForTimeout(50);
          }
          await page.mouse.up();
          await pause(page, SCENE_PAUSE);
        }
      }

      // ⑤ Conversations
      console.log('[06] Conversations');
      await clickActivityTab(page, 'Conversations');
      await waitForSidebar(page);
      await pause(page, SCENE_PAUSE);

      // ⑥ Settings
      console.log('[06] Settings');
      await clickActivityTab(page, 'Settings');
      await waitForSidebar(page);
      await pause(page, SCENE_PAUSE);

      console.log('[06] App shell overview recording complete');
    } finally {
      await context.close();
    }
  });
});
