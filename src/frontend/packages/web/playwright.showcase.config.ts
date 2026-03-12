import { defineConfig } from '@playwright/test';
import baseConfig from './playwright.config';

/**
 * Showcase mode runs the headed E2E flow for live walkthroughs.
 *
 * Usage:
 *   npx playwright test --config=playwright.showcase.config.ts
 *   npx playwright test --config=playwright.showcase.config.ts S2-onbrick
 */
export default defineConfig({
  ...baseConfig,
  testMatch: /\/S\/S2-.*\.spec\.ts$/,
  use: {
    ...baseConfig.use,
    headless: false,
    launchOptions: {
      slowMo: parseInt(process.env.SHOWCASE_SLOW_MO ?? '1500', 10),
      args: [
        `--window-size=${process.env.SHOWCASE_WINDOW_WIDTH ?? '1440'},${process.env.SHOWCASE_WINDOW_HEIGHT ?? '900'}`,
        '--disable-infobars',
      ],
    },
    viewport: {
      width: parseInt(process.env.SHOWCASE_WINDOW_WIDTH ?? '1440', 10),
      height: parseInt(process.env.SHOWCASE_WINDOW_HEIGHT ?? '900', 10),
    },
    video: (process.env.SHOWCASE_VIDEO ?? 'on') === 'on' ? 'on' : 'off',
    trace: 'on',
    screenshot: 'on',
    actionTimeout: 15_000,
  },
  workers: 1,
  timeout: 300_000,
  expect: {
    timeout: 15_000,
  },
  reporter: [['list'], ['html', { open: 'never' }]],
});
