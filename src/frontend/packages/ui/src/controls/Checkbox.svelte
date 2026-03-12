<script lang="ts">
  import type { CheckboxSize } from '../types';

  interface Props {
    checked?: boolean;
    indeterminate?: boolean;
    disabled?: boolean;
    size?: CheckboxSize;
    label?: string;
    description?: string;
    id?: string;
    name?: string;
    value?: string;
    onchange?: (checked: boolean) => void;
    class?: string;
    /** Strip all built-in styles — class prop applies directly to the input element */
    unstyled?: boolean;
  }

  let {
    checked = $bindable(false),
    indeterminate = false,
    disabled = false,
    size = 'md',
    label,
    description,
    id,
    name,
    value,
    onchange,
    class: className = '',
    unstyled = false,
  }: Props = $props();

  const generatedId = `checkbox-${Math.random().toString(36).substring(2, 9)}`;
  const checkboxId = $derived(id ?? generatedId);

  const sizeClasses: Record<CheckboxSize, { box: string; icon: string; text: string }> = {
    sm: { box: 'h-4 w-4', icon: 'h-3 w-3', text: 'text-sm' },
    md: { box: 'h-5 w-5', icon: 'h-3.5 w-3.5', text: 'text-sm' },
    lg: { box: 'h-6 w-6', icon: 'h-4 w-4', text: 'text-base' },
  };

  function handleChange(e: Event) {
    const target = e.target as HTMLInputElement;
    checked = target.checked;
    onchange?.(checked);
  }
</script>

{#if unstyled}
  <input
    type="checkbox"
    id={checkboxId}
    {name}
    {value}
    {disabled}
    bind:checked
    bind:indeterminate
    onchange={handleChange}
    class={className}
  />
{:else}
<div class="flex items-start gap-3 {className}">
  <div class="relative flex items-center">
    <input
      type="checkbox"
      id={checkboxId}
      {name}
      {value}
      {disabled}
      bind:checked
      bind:indeterminate
      onchange={handleChange}
      class="peer sr-only"
    />
    <div
      class="{sizeClasses[size].box} rounded border-2 flex items-center justify-center transition-colors
        {disabled
          ? 'border-[var(--color-neutral-300)] bg-[var(--color-neutral-100)] cursor-not-allowed dark:border-[var(--color-neutral-600)] dark:bg-[var(--color-neutral-800)]'
          : checked || indeterminate
            ? 'border-[var(--color-accent-primary)] bg-[var(--color-accent-primary)]'
            : 'border-[var(--color-neutral-400)] bg-[var(--color-bg-primary)] hover:border-[var(--color-accent-primary)]'}"
    >
      {#if checked && !indeterminate}
        <svg class="{sizeClasses[size].icon} text-white" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="3">
          <path stroke-linecap="round" stroke-linejoin="round" d="M5 13l4 4L19 7" />
        </svg>
      {:else if indeterminate}
        <svg class="{sizeClasses[size].icon} text-white" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="3">
          <path stroke-linecap="round" stroke-linejoin="round" d="M5 12h14" />
        </svg>
      {/if}
    </div>
  </div>

  {#if label || description}
    <label for={checkboxId} class="flex flex-col cursor-pointer {disabled ? 'cursor-not-allowed opacity-50' : ''}">
      {#if label}
        <span class="{sizeClasses[size].text} font-medium text-[var(--color-text-primary)]">{label}</span>
      {/if}
      {#if description}
        <span class="text-sm text-[var(--color-text-tertiary)]">{description}</span>
      {/if}
    </label>
  {/if}
</div>
{/if}
