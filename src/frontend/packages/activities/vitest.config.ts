/// <reference types="vitest/config" />
import { defineConfig } from 'vitest/config';
import { fileURLToPath, URL } from 'node:url';
import { svelte } from '@sveltejs/vite-plugin-svelte';

export default defineConfig({
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  plugins: [svelte({ hot: false }) as any],
  resolve: {
    alias: {
      '@activities-src': fileURLToPath(new URL('./src', import.meta.url)),
      '@sddp/ui': fileURLToPath(new URL('../ui/src', import.meta.url)),
      '@sddp/shell': fileURLToPath(new URL('../shell/src', import.meta.url)),
    },
    conditions: ['browser'],
  },
  test: {
    globals: true,
    environment: 'jsdom',
    include: ['tests/unit/**/*.{test,spec}.{js,ts}'],
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
        'src/**/services/**/*.ts',
        'src/**/stores/**/*.ts',
        'src/shared/**/*.ts',
      ],
      exclude: [
        'tests/unit/**/*.test.ts',
        'tests/unit/**/*.spec.ts',
        'src/**/*.test.ts',
        'src/**/*.spec.ts',
        'src/**/*.d.ts',
        'src/**/index.ts',
        'src/**/components/**',
        'src/**/__tests__/**',
      ],
      thresholds: {
        statements: 20,
        branches: 15,
        functions: 20,
        lines: 20,
      },
    },
  },
});
