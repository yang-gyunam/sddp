<script lang="ts">
  import type { Snippet } from 'svelte';
  import type { SelectSize, SelectOption } from '../types';

  interface Props {
    value?: string;
    options?: SelectOption[];
    placeholder?: string;
    label?: string;
    hint?: string;
    error?: string;
    disabled?: boolean;
    required?: boolean;
    size?: SelectSize;
    id?: string;
    name?: string;
    onchange?: (value: string) => void;
    class?: string;
    style?: string;
    variant?: 'default' | 'flat';
    /** Strip all built-in styles — class prop applies directly to the select element */
    unstyled?: boolean;
    /** Children snippet for unstyled mode (renders <option> elements directly) */
    children?: Snippet;
  }

  let {
    value = $bindable(''),
    options = [],
    placeholder = 'Select...',
    label,
    hint,
    error,
    disabled = false,
    required = false,
    size = 'md',
    id,
    name,
    onchange,
    class: className = '',
    style,
    variant = 'default',
    unstyled = false,
    children,
  }: Props = $props();

  const generatedId = `select-${Math.random().toString(36).substring(2, 9)}`;
  const selectId = $derived(id ?? generatedId);

  const sizeClasses: Record<SelectSize, string> = {
    sm: 'px-2 py-1 text-xs',
    md: 'px-3 py-2 text-sm',
    lg: 'px-4 py-2.5 text-base',
  };

  const baseClasses = $derived(
    variant === 'flat'
      ? 'block w-full border appearance-none bg-no-repeat transition-colors focus:outline-none disabled:cursor-not-allowed'
      : 'block w-full rounded-lg border appearance-none bg-no-repeat transition-colors focus:outline-none disabled:bg-[var(--color-bg-tertiary)] disabled:cursor-not-allowed'
  );

  const normalClasses = $derived(
    variant === 'flat'
      ? 'border-[var(--color-border-secondary)] bg-transparent text-[var(--color-text-primary)] focus:border-[var(--color-accent-primary)]'
      : 'border-[var(--color-border-secondary)] bg-[var(--color-surface-100)] text-[var(--color-text-primary)] focus:border-[var(--color-accent-primary)]'
  );

  const errorClasses =
    'border-[var(--color-error-500)] bg-[var(--color-error-50)] text-[var(--color-text-primary)] focus:border-[var(--color-error-500)] dark:bg-[var(--color-error-900)]/10';

  function handleChange(e: Event) {
    const target = e.target as HTMLSelectElement;
    value = target.value;
    onchange?.(value);
  }
</script>

{#if unstyled}
  <select
    {id}
    {name}
    {disabled}
    {required}
    bind:value
    onchange={handleChange}
    class={className}
    {style}
  >
    {#if children}
      {@render children()}
    {/if}
  </select>
{:else}
<div class="w-full {className}">
  {#if label}
    <label for={selectId} class="block text-xs text-[var(--color-text-muted)] mb-1">
      {label}
      {#if required}
        <span class="text-red-500">*</span>
      {/if}
    </label>
  {/if}

  <div class="relative">
    <select
      id={selectId}
      {name}
      {disabled}
      {required}
      bind:value
      onchange={handleChange}
      class="{baseClasses} {sizeClasses[size]} {error ? errorClasses : normalClasses} pr-10 select-chevron"
      aria-required={required ? 'true' : undefined}
      aria-invalid={error ? 'true' : undefined}
      aria-describedby={error ? `${selectId}-error` : undefined}
    >
      {#if placeholder}
        <option value="" disabled>{placeholder}</option>
      {/if}
      {#each options as option (option.value)}
        <option value={option.value} disabled={option.disabled}>
          {option.label}
        </option>
      {/each}
    </select>
  </div>

  {#if error}
    <p id="{selectId}-error" class="mt-1 text-sm text-[var(--color-error-600)]">
      {error}
    </p>
  {:else if hint}
    <p class="text-xs text-[var(--color-text-muted)] mt-1">
      {hint}
    </p>
  {/if}
</div>
{/if}

<style>
  .select-chevron {
    background-image: url("data:image/svg+xml,%3csvg xmlns='http://www.w3.org/2000/svg' fill='none' viewBox='0 0 20 20'%3e%3cpath stroke='%236b7280' stroke-linecap='round' stroke-linejoin='round' stroke-width='1.5' d='M6 8l4 4 4-4'/%3e%3c/svg%3e");
    background-position: right 0.5rem center;
    background-size: 1.5em 1.5em;
  }
</style>
