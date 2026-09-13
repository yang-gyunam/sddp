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
  },
});
