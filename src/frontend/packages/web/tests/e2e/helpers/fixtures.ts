/// <reference types="node" />
import { test as base } from '@playwright/test';
import * as fs from 'fs';
import * as path from 'path';
import { ensureAuthenticated } from './page-object.helpers';
import { E2E_API_BASE, TEST_USER } from './constants';

const IS_HEADED = !!process.env.HEADED;
const SCREENSHOT_DIR = path.join(process.cwd(), 'tests/e2e/.dashboard');

export const test = base.extend<{
  autoAuth: void;
  screenshotCapture: void;
}>({
  autoAuth: [
    async ({ page }, use) => {
      let preAuthed = false;
      const originalGoto = page.goto.bind(page);
      const gotoWithAutoAuth: typeof page.goto = async (url, opts) => {
        if (!preAuthed) {
          try {
            const resp = await page.request.post(`${E2E_API_BASE}/auth/login`, {
              data: {
                username: TEST_USER.username,
                password: TEST_USER.password,
              },
            });
            if (resp.ok()) preAuthed = true;
          } catch {
            console.warn('[autoAuth] API pre-auth failed, falling back to UI login');
          }
        }

        await page.evaluate(() => localStorage.clear()).catch(() => {});
        const resp = await originalGoto(url, opts);
        await page.waitForLoadState('networkidle').catch(() => {
          console.warn('[autoAuth] Page did not reach networkidle after navigation');
        });

        await ensureAuthenticated(page);
        return resp;
      };
      page.goto = gotoWithAutoAuth;

      await use();
      page.goto = originalGoto;
    },
    { auto: true },
  ],

  screenshotCapture: [
    async ({ page }, use) => {
      if (!IS_HEADED) {
        await use();
        return;
      }

      fs.mkdirSync(SCREENSHOT_DIR, { recursive: true });
      const screenshotPath = path.join(SCREENSHOT_DIR, 'current.png');
      const tempPath = path.join(SCREENSHOT_DIR, 'current.tmp.png');

      const interval = setInterval(async () => {
        try {
          await page.screenshot({ path: tempPath, timeout: 2000 });
          fs.renameSync(tempPath, screenshotPath);
        } catch (err) {
          if (err instanceof Error && !err.message.includes('Target closed')) {
            console.warn('[dashboard-screenshot] Screenshot failed:', err.message);
          }
        }
      }, 2000);

      await use();

      clearInterval(interval);

      try {
        await page.screenshot({ path: tempPath, timeout: 2000 });
        fs.renameSync(tempPath, screenshotPath);
      } catch {
      }
    },
    { auto: true },
  ],
});

export { expect } from '@playwright/test';
