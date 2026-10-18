import { defineConfig } from 'vitest/config';
import { fileURLToPath, URL } from 'node:url';

/** Public error-handler unit tests; UI controls run in the ui workspace. */
export default defineConfig({
  resolve: {
    alias: {
      // Package aliases → source directories
      '@sddp/ui': fileURLToPath(new URL('../ui/src', import.meta.url)),
      '@sddp/shell': fileURLToPath(new URL('../shell/src', import.meta.url)),
      '@sddp/activities': fileURLToPath(new URL('../activities/src', import.meta.url)),
      // Shell internal aliases
      '$components': fileURLToPath(new URL('../shell/src/components', import.meta.url)),
      '$stores': fileURLToPath(new URL('./src/stores', import.meta.url)),
      '$utils': fileURLToPath(new URL('../shell/src/utils', import.meta.url)),
      '$types': fileURLToPath(new URL('../shell/src/types', import.meta.url)),
      // Web local aliases
      '$lib': fileURLToPath(new URL('./src/lib', import.meta.url)),
    },
  },
  test: {
    globals: true,
    environment: 'node',
    include: ['tests/unit/**/*.{test,spec}.{js,ts}'],
    passWithNoTests: false,
  },
});
