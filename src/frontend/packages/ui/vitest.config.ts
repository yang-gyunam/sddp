/// <reference types="vitest/config" />
import { defineConfig } from 'vitest/config';
import { svelte } from '@sveltejs/vite-plugin-svelte';

export default defineConfig({
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  plugins: [svelte({ hot: false }) as any],
  resolve: {
    conditions: ['browser'],
  },
  test: {
    globals: true,
    environment: 'jsdom',
    include: ['src/**/*.{test,spec}.{js,ts}'],
    passWithNoTests: true,
    server: {
      deps: {
        // Process @testing-library/svelte .svelte.js files through Svelte compiler
        inline: [/@testing-library\/svelte/],
      },
    },
    coverage: {
      provider: 'v8',
      reporter: ['text', 'json', 'html'],
      include: [
        'src/controls/**/*.ts',
        'src/layout/**/*.ts',
        'src/types/**/*.ts',
      ],
      exclude: [
        'src/**/*.test.ts',
        'src/**/*.spec.ts',
        'src/**/*.d.ts',
        'src/**/*.stories.ts',
        'src/**/index.ts',
      ],
      thresholds: {
        statements: 0,
        branches: 0,
        functions: 0,
        lines: 0,
      },
    },
  },
});
