import { defineConfig, devices } from '@playwright/test';

const isHeaded = !!process.env.HEADED;
const baseUrl = process.env.E2E_WEB_BASE_URL ?? 'http://localhost:3500';
const useExternalWeb = process.env.E2E_USE_EXTERNAL_WEB === '1';
const browserChannel = process.env.E2E_BROWSER_CHANNEL;

const STORAGE_STATE = 'tests/e2e/.auth/storage-state.json';

export default defineConfig({
  testDir: './tests/e2e',
  globalSetup: './tests/e2e/global-setup.ts',
  fullyParallel: !isHeaded,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: isHeaded ? 1 : (process.env.CI ? 1 : undefined),
  timeout: isHeaded ? 600_000 : 30_000,
  reporter: isHeaded
    ? [
        ['list'],
        ['html', { open: 'never' }]
      ]
    : 'html',
  use: {
    baseURL: baseUrl,
    trace: 'on-first-retry',
    screenshot: isHeaded ? 'on' : 'only-on-failure',
    video: isHeaded ? 'on' : 'off',
    ...(isHeaded && {
      headless: false,
      launchOptions: {
        args: ['--window-size=1000,900', '--window-position=430,25', '--disable-infobars'],
      },
      viewport: { width: 1000, height: 900 },
      actionTimeout: 30_000,
    }),
  },
  ...(isHeaded && { expect: { timeout: 30_000 } }),
  projects: [
    {
      name: 'api',
      testMatch: /\/L1\/.*\.spec\.ts$/,
      use: {
        ...devices['Desktop Chrome'],
        headless: true,
        launchOptions: {},
        video: 'off',
        screenshot: 'off',
      },
    },
    {
      name: 'browser',
      testIgnore: /\/L1\/.*\.spec\.ts$/,
      use: {
        ...devices['Desktop Chrome'],
        ...(browserChannel ? { channel: browserChannel } : {}),
        storageState: STORAGE_STATE,
      },
    },
  ],
  webServer: useExternalWeb
    ? undefined
    : [
        {
          command: 'cd ../../../backend && dotnet run --project Sddp.Api --urls http://localhost:5001',
          url: 'http://localhost:5001/health',
          reuseExistingServer: !process.env.CI,
          timeout: 180 * 1000,
        },
        {
          command: 'npm run dev',
          url: 'http://localhost:3500',
          reuseExistingServer: !process.env.CI,
          timeout: 120 * 1000,
        },
      ],
});
