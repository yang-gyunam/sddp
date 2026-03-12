/**
 * Vitest Integration Test Configuration
 *
 * Integration tests verify the full flow from API service → fetch → mock server → response handling.
 * Uses MSW (Mock Service Worker) for realistic HTTP mocking.
 */
import { defineConfig } from 'vitest/config';
import { fileURLToPath } from 'node:url';

export default defineConfig({
  resolve: {
    alias: {
      '@sddp/ui': fileURLToPath(new URL('../ui/src', import.meta.url)),
      '@sddp/shell': fileURLToPath(new URL('../shell/src', import.meta.url)),
      '@sddp/activities': fileURLToPath(new URL('../activities/src', import.meta.url)),
      '$components': fileURLToPath(new URL('../shell/src/components', import.meta.url)),
      '$stores': fileURLToPath(new URL('./src/stores', import.meta.url)),
      '$utils': fileURLToPath(new URL('../shell/src/utils', import.meta.url)),
      '$types': fileURLToPath(new URL('../shell/src/types', import.meta.url)),
      '$lib': fileURLToPath(new URL('./src/lib', import.meta.url)),
    },
  },
  test: {
    globals: true,
    environment: 'node',
    include: ['tests/integration/**/*.{test,spec}.{js,ts}'],
    setupFiles: ['tests/integration/api/setup.ts'],
    passWithNoTests: true,
    coverage: {
      provider: 'v8',
      reporter: ['text', 'lcov'],
      reportsDirectory: 'coverage/integration',
    },
  },
});
