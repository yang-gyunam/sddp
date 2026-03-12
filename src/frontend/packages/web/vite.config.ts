/// <reference types="vitest/config" />
import { defineConfig } from 'vite';
import { svelte } from '@sveltejs/vite-plugin-svelte';
import tailwindcss from '@tailwindcss/vite';
import { fileURLToPath, URL } from 'node:url';

// Source alias: dev (default) uses source for HMR, prod uses dist/ via node_modules
const useSource = process.env.SDDP_USE_SOURCE !== 'false';

// Shared aliases (always active)
const sharedAliases = {
  // Shell package internal aliases (for building from web)
  '$components': fileURLToPath(new URL('../shell/src/components', import.meta.url)),
  '$utils': fileURLToPath(new URL('../shell/src/utils', import.meta.url)),
  // Local aliases
  $lib: fileURLToPath(new URL('./src/lib', import.meta.url)),
  $stores: fileURLToPath(new URL('./src/stores', import.meta.url)),
  $types: fileURLToPath(new URL('./src/types', import.meta.url)),
};

// https://vite.dev/config/
export default defineConfig({
  plugins: [svelte(), tailwindcss()],
  resolve: {
    alias: useSource
      ? {
          // Dev: source alias for HMR
          '@sddp/ui': fileURLToPath(new URL('../ui/src', import.meta.url)),
          '@sddp/shell': fileURLToPath(new URL('../shell/src', import.meta.url)),
          '@sddp/activities': fileURLToPath(new URL('../activities/src', import.meta.url)),
          ...sharedAliases,
        }
      : {
          // Prod: no @sddp/* aliases → resolved via node_modules symlinks → dist/
          ...sharedAliases,
        },
  },
  server: {
    port: 3500,
    strictPort: true,
  },
  preview: {
    port: 3500,
  },
  test: {
    globals: true,
    environment: 'node',
    include: ['src/**/*.{test,spec}.{js,ts}'],
    passWithNoTests: true,
    coverage: {
      provider: 'v8',
      reporter: ['text', 'json', 'html'],
      include: ['src/stores/**/*.ts', 'src/utils/**/*.ts', 'src/lib/**/*.ts'],
      exclude: ['src/**/*.test.ts', 'src/**/*.d.ts', 'src/**/index.ts'],
      thresholds: {
        statements: 20,
        branches: 12,
        functions: 20,
        lines: 22,
      },
    },
  },
});
