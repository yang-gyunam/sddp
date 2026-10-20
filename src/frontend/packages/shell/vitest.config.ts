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
    include: ['src/**/*.{test,spec}.{js,ts}', 'tests/unit/**/*.{test,spec}.{js,ts}'],
    passWithNoTests: true,
    server: {
      deps: {
        inline: [/@testing-library\/svelte/],
      },
    },
  },
});
