import { defineConfig, devices } from '@playwright/test';

// Deliberately independent of global-setup.ts: no FLUSHALL, shared fixtures or saved login.
export default defineConfig({
  testDir: './tests/e2e',
  testMatch: /\/S\/S2-onbrick\.full-story\.spec\.ts$/,
  fullyParallel: false,
  forbidOnly: true,
  retries: 0,
  workers: 1,
  timeout: 60_000,
  globalTimeout: 180_000,
  expect: { timeout: 10_000 },
  outputDir: process.env.ONBRICK_OUTPUT_DIR ?? 'test-results/onbrick-smoke',
  reporter: [['list'], ['json', { outputFile: process.env.ONBRICK_REPORT ?? 'test-results/onbrick-smoke.json' }]],
  use: {
    ...devices['Desktop Chrome'],
    baseURL: process.env.E2E_WEB_BASE_URL ?? 'http://localhost:9010',
    headless: true,
    trace: 'off',
    screenshot: 'only-on-failure',
    storageState: { cookies: [], origins: [] },
    actionTimeout: 15_000,
  },
});
