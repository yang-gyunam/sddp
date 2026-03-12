import js from '@eslint/js';
import tseslint from 'typescript-eslint';
import svelte from 'eslint-plugin-svelte';
import globals from 'globals';

export default tseslint.config(
  js.configs.recommended,
  ...tseslint.configs.recommended,
  ...svelte.configs['flat/recommended'],
  {
    languageOptions: {
      globals: {
        ...globals.browser,
        ...globals.node,
      },
    },
  },
  {
    files: ['**/*.svelte'],
    languageOptions: {
      parserOptions: {
        parser: tseslint.parser,
      },
    },
  },
  {
    files: ['**/*.svelte.ts'],
    languageOptions: {
      parser: tseslint.parser,
    },
    rules: {
      'svelte/prefer-svelte-reactivity': 'off',
    },
  },
  {
    rules: {
      '@typescript-eslint/no-unused-vars': [
        'error',
        {
          argsIgnorePattern: '^_',
          varsIgnorePattern: '^_',
        },
      ],
      '@typescript-eslint/explicit-function-return-type': 'off',
      '@typescript-eslint/explicit-module-boundary-types': 'off',
      '@typescript-eslint/no-explicit-any': 'warn',
      'svelte/no-at-html-tags': 'warn',
    },
  },
  {
    files: ['**/*.svelte'],
    rules: {
      'svelte/no-restricted-html-elements': [
        'error',
        { elements: ['button'], message: 'Use Button or IconButton from @sddp/ui' },
        { elements: ['input'], message: 'Use Input, Checkbox, or Radio from @sddp/ui' },
        { elements: ['select'], message: 'Use Select from @sddp/ui' },
        { elements: ['textarea'], message: 'Use Textarea from @sddp/ui' },
      ],
    },
  },
  {
    ignores: ['node_modules/', 'dist/', 'build/', '.svelte-kit/', 'playwright-report/', 'coverage/', '**/*.d.ts'],
  }
);
