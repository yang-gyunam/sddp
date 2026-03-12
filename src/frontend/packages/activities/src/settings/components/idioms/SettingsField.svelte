<script lang="ts">
  /**
   * Settings Field
   * Reusable field component for settings forms
   */

  import type { Snippet } from 'svelte';

  interface Props {
    label: string;
    hint?: string;
    error?: string;
    required?: boolean;
    children: Snippet;
  }

  let { label, hint, error, required = false, children }: Props = $props();
</script>

<div class="settings-field" class:has-error={!!error}>
  <span class="field-label">
    {label}
    {#if required}
      <span class="required">*</span>
    {/if}
  </span>
  <div class="field-input">
    {@render children()}
  </div>
  {#if error}
    <p class="field-error">{error}</p>
  {:else if hint}
    <p class="field-hint">{hint}</p>
  {/if}
</div>

<style>
  .settings-field {
    display: flex;
    flex-direction: column;
    gap: 0.375rem;
  }

  .field-label {
    display: block;
    font-size: 0.875rem;
    font-weight: 500;
    color: var(--text-secondary);
  }

  .required {
    color: var(--error-color, #ef4444);
    margin-left: 0.125rem;
  }

  .field-input {
    display: flex;
    flex-direction: column;
  }

  .field-input :global(input),
  .field-input :global(select),
  .field-input :global(textarea) {
    width: 100%;
    padding: 0.5rem 0.75rem;
    background: var(--bg-primary);
    border: 1px solid var(--border-color);
    border-radius: 4px;
    color: var(--text-primary);
    font-size: 0.875rem;
  }

  .field-input :global(input:focus),
  .field-input :global(select:focus),
  .field-input :global(textarea:focus) {
    outline: none;
    border-color: var(--accent-color);
  }

  .has-error .field-input :global(input),
  .has-error .field-input :global(select),
  .has-error .field-input :global(textarea) {
    border-color: var(--error-color, #ef4444);
  }

  .field-hint {
    margin: 0;
    font-size: 0.8125rem;
    color: var(--text-tertiary);
  }

  .field-error {
    margin: 0;
    font-size: 0.8125rem;
    color: var(--error-color, #ef4444);
  }
</style>
