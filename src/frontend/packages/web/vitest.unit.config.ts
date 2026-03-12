import { defineConfig } from 'vitest/config';
import { fileURLToPath, URL } from 'node:url';

/**
 * unit test settings.
 *
 * activities, shell, web unit test tests/unit/.
 * .
 *
 * :
 *   npx vitest --config vitest.unit.config.ts
 *   npx vitest --config vitest.unit.config.ts --coverage
 *   npx vitest --config vitest.unit.config.ts tests/unit/activities/
 *   npx vitest --config vitest.unit.config.ts tests/unit/shell/
 */
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
    passWithNoTests: true,
    coverage: {
      provider: 'v8',
      reporter: ['text', 'json', 'html'],
      include: [
        '../activities/src/**/services/**/*.ts',
        '../activities/src/**/stores/**/*.ts',
        '../activities/src/shared/**/*.ts',
        '../shell/src/stores/**/*.ts',
        '../shell/src/utils/**/*.ts',
        '../shell/src/core/services/**/*.ts',
        'src/stores/**/*.ts',
        'src/lib/**/*.ts',
      ],
      exclude: ['**/*.test.ts', '**/*.spec.ts', '**/*.d.ts', '**/index.ts'],
      thresholds: {
        statements: 20,
        branches: 15,
        functions: 20,
        lines: 20,
      },
    },
  },
});
