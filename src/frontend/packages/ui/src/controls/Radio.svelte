<script lang="ts">
  import type { RadioSize } from '../types';

  interface Props {
    checked?: boolean;
    disabled?: boolean;
    size?: RadioSize;
    label?: string;
    description?: string;
    id?: string;
    name?: string;
    value?: string;
    onchange?: (value: string) => void;
    class?: string;
    /** Strip all built-in styles — class prop applies directly to the input element */
    unstyled?: boolean;
  }

  let {
    checked = $bindable(false),
    disabled = false,
    size = 'md',
    label,
    description,
    id,
    name,
    value = '',
    onchange,
    class: className = '',
    unstyled = false,
  }: Props = $props();

  const generatedId = `radio-${Math.random().toString(36).substring(2, 9)}`;
  const radioId = $derived(id ?? generatedId);

  const sizeClasses: Record<RadioSize, { box: string; dot: string; text: string }> = {
    sm: { box: 'h-4 w-4', dot: 'h-1.5 w-1.5', text: 'text-sm' },
    md: { box: 'h-5 w-5', dot: 'h-2 w-2', text: 'text-sm' },
    lg: { box: 'h-6 w-6', dot: 'h-2.5 w-2.5', text: 'text-base' },
  };

  function handleChange(e: Event) {
    const target = e.target as HTMLInputElement;
    if (target.checked) {
      checked = true;
      onchange?.(value);
    }
  }
</script>

{#if unstyled}
  <input
    type="radio"
    id={radioId}
    {name}
    {value}
    {disabled}
    checked={checked}
    onchange={handleChange}
    class={className}
  />
{:else}
<div class="flex items-start gap-3 {className}">
  <label for={radioId} class="relative flex items-center {disabled ? 'cursor-not-allowed' : 'cursor-pointer'}">
    <input
      type="radio"
      id={radioId}
      {name}
      {value}
      {disabled}
      checked={checked}
      onchange={handleChange}
      class="peer sr-only"
    />
    <div
      class="{sizeClasses[size].box} rounded-full border-2 flex items-center justify-center transition-colors
        {disabled
          ? 'border-[var(--color-neutral-300)] bg-[var(--color-neutral-100)] cursor-not-allowed dark:border-[var(--color-neutral-600)] dark:bg-[var(--color-neutral-800)]'
          : checked
            ? 'border-[var(--color-accent-primary)] bg-[var(--color-bg-primary)]'
            : 'border-[var(--color-neutral-400)] bg-[var(--color-bg-primary)] hover:border-[var(--color-accent-primary)]'}"
    >
      {#if checked}
        <div class="{sizeClasses[size].dot} rounded-full bg-[var(--color-accent-primary)]"></div>
      {/if}
    </div>
  </label>

  {#if label || description}
    <label for={radioId} class="flex flex-col cursor-pointer {disabled ? 'cursor-not-allowed opacity-50' : ''}">
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
