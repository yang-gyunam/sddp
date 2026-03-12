<script lang="ts">
  import type { SwitchSize } from '../types';

  interface Props {
    checked?: boolean;
    disabled?: boolean;
    size?: SwitchSize;
    label?: string;
    description?: string;
    id?: string;
    name?: string;
    onchange?: (checked: boolean) => void;
    class?: string;
  }

  let {
    checked = $bindable(false),
    disabled = false,
    size = 'md',
    label,
    description,
    id,
    name,
    onchange,
    class: className = '',
  }: Props = $props();

  const generatedId = `switch-${Math.random().toString(36).substring(2, 9)}`;
  const switchId = $derived(id ?? generatedId);

  const sizeClasses: Record<SwitchSize, { track: string; thumb: string; translate: string; text: string }> = {
    sm: { track: 'h-4 w-7', thumb: 'h-3 w-3', translate: 'translate-x-3', text: 'text-sm' },
    md: { track: 'h-5 w-9', thumb: 'h-4 w-4', translate: 'translate-x-4', text: 'text-sm' },
    lg: { track: 'h-6 w-11', thumb: 'h-5 w-5', translate: 'translate-x-5', text: 'text-base' },
  };

  function handleChange() {
    if (!disabled) {
      checked = !checked;
      onchange?.(checked);
    }
  }

  function handleKeyDown(e: KeyboardEvent) {
    if (e.key === 'Enter' || e.key === ' ') {
      e.preventDefault();
      handleChange();
    }
  }
</script>

<div class="flex items-start gap-3 {className}">
  <button
    type="button"
    role="switch"
    id={switchId}
    aria-checked={checked}
    {disabled}
    onclick={handleChange}
    onkeydown={handleKeyDown}
    class="{sizeClasses[size].track} relative inline-flex items-center shrink-0 rounded-full transition-colors duration-200 ease-in-out focus:outline-none focus:border focus:border-[var(--color-accent-primary)]
      {disabled
        ? 'cursor-not-allowed opacity-50'
        : 'cursor-pointer'}
      {checked
        ? 'bg-[var(--color-accent-primary)]'
        : 'bg-[var(--color-neutral-300)] dark:bg-[var(--color-neutral-600)]'}"
  >
    <span class="sr-only">{label ?? 'Toggle'}</span>
    <span
      class="{sizeClasses[size].thumb} pointer-events-none inline-block rounded-full bg-white shadow-sm ring-0 transition-transform duration-200 ease-in-out
        {checked ? sizeClasses[size].translate : 'translate-x-0.5'}"
    ></span>
  </button>

  <input
    type="checkbox"
    {name}
    bind:checked
    class="sr-only"
    tabindex="-1"
  />

  {#if label || description}
    <label for={switchId} class="flex flex-col cursor-pointer {disabled ? 'cursor-not-allowed opacity-50' : ''}">
      {#if label}
        <span class="{sizeClasses[size].text} font-medium text-[var(--color-text-primary)]">{label}</span>
      {/if}
      {#if description}
        <span class="text-sm text-[var(--color-text-tertiary)]">{description}</span>
      {/if}
    </label>
  {/if}
</div>
