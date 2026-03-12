/// <reference types="vitest/config" />
import { defineConfig } from 'vitest/config';
import { fileURLToPath, URL } from 'node:url';
import { svelte } from '@sveltejs/vite-plugin-svelte';

export default defineConfig({
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  plugins: [svelte({ hot: false }) as any],
  resolve: {
    alias: {
      $components: fileURLToPath(new URL('./src/components', import.meta.url)),
      $stores: fileURLToPath(new URL('./src/stores', import.meta.url)),
      $types: fileURLToPath(new URL('./src/types', import.meta.url)),
      $utils: fileURLToPath(new URL('./src/utils', import.meta.url)),
      '@sddp/ui': fileURLToPath(new URL('../ui/src', import.meta.url)),
    },
    conditions: ['browser'],
  },
  test: {
    globals: true,
    environment: 'jsdom',
    include: ['src/**/*.{test,spec}.{js,ts}'],
    passWithNoTests: true,
    server: {
      deps: {
        inline: [/@testing-library\/svelte/],
      },
    },
    coverage: {
      provider: 'v8',
      reporter: ['text', 'json', 'html'],
      include: [
        'src/stores/**/*.ts',
        'src/utils/**/*.ts',
        'src/core/services/**/*.ts',
      ],
      exclude: [
        'src/**/*.test.ts',
        'src/**/*.spec.ts',
        'src/**/*.d.ts',
        'src/**/index.ts',
      ],
      thresholds: {
        statements: 45,
        branches: 35,
        functions: 35,
        lines: 45,
      },
    },
  },
});
