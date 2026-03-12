<script lang="ts">
  import type { InputType } from '../types';

  interface Props {
    type?: InputType;
    value?: string | number;
    placeholder?: string;
    label?: string;
    hint?: string;
    error?: string;
    disabled?: boolean;
    required?: boolean;
    mono?: boolean;
    id?: string;
    name?: string;
    maxlength?: number;
    min?: string | number;
    max?: string | number;
    step?: string | number;
    autocomplete?: HTMLInputElement['autocomplete'];
    oninput?: (e: Event) => void;
    onchange?: (e: Event) => void;
    onfocus?: (e: FocusEvent) => void;
    onblur?: (e: FocusEvent) => void;
    onkeydown?: (e: KeyboardEvent) => void;
    oncontextmenu?: (e: MouseEvent) => void;
    class?: string;
    style?: string;
    size?: 'sm' | 'md' | 'lg';
    variant?: 'default' | 'flat';
    /** Strip all built-in styles — class prop applies directly to the input element */
    unstyled?: boolean;
    /** Expose the underlying input element for direct DOM access */
    element?: HTMLInputElement;
  }

  let {
    type = 'text',
    value = $bindable(''),
    placeholder = '',
    label,
    hint,
    error,
    disabled = false,
    required = false,
    mono = false,
    id,
    name,
    maxlength,
    min,
    max,
    step,
    autocomplete,
    oninput,
    onchange,
    onfocus,
    onblur,
    onkeydown,
    oncontextmenu,
    class: className = '',
    style,
    size = 'md',
    variant = 'default',
    unstyled = false,
    element = $bindable(undefined as HTMLInputElement | undefined),
  }: Props = $props();

  const generatedId = `input-${Math.random().toString(36).substring(2, 9)}`;
  const inputId = $derived(id ?? generatedId);

  const sizeClasses = {
    sm: 'px-2 py-1 text-xs',
    md: 'px-3 py-2 text-sm',
    lg: 'px-4 py-3 text-base',
  };

  const baseInputClasses = $derived(
    variant === 'flat'
      ? 'block w-full border transition-colors focus:outline-none disabled:cursor-not-allowed'
      : 'block w-full rounded-lg border transition-colors focus:outline-none disabled:bg-[var(--color-bg-tertiary)] disabled:cursor-not-allowed'
  );

  const normalClasses = $derived(
    variant === 'flat'
      ? 'border-[var(--color-border-secondary)] bg-transparent text-[var(--color-text-primary)] placeholder:text-[var(--color-text-tertiary)] placeholder:opacity-60 focus:border-[var(--color-accent-primary)]'
      : 'border-[var(--color-border-secondary)] bg-[var(--color-bg-primary)] text-[var(--color-text-primary)] placeholder:text-[var(--color-text-tertiary)] placeholder:opacity-60 focus:border-[var(--color-accent-primary)]'
  );

  const errorClasses =
    'border-[var(--color-error-500)] bg-[var(--color-error-50)] text-[var(--color-text-primary)] focus:border-[var(--color-error-500)] dark:bg-[var(--color-error-900)]/10';
</script>

{#if unstyled}
  <input
    {type}
    id={id}
    {name}
    {placeholder}
    {disabled}
    {required}
    {maxlength}
    {min}
    {max}
    {step}
    {autocomplete}
    bind:value
    bind:this={element}
    {oninput}
    {onchange}
    {onfocus}
    {onblur}
    {onkeydown}
    {oncontextmenu}
    class={className}
    {style}
  />
{:else}
  <div class="w-full">
    {#if label}
      <label for={inputId} class="block text-xs text-[var(--color-text-secondary)] mb-1">
        {label}
        {#if required}
          <span class="text-red-500">*</span>
        {/if}
      </label>
    {/if}

    <input
      {type}
      id={inputId}
      {name}
      {placeholder}
      {disabled}
      {required}
      {maxlength}
      {min}
      {max}
      {step}
      {autocomplete}
      bind:value
      bind:this={element}
      {oninput}
      {onchange}
      {onfocus}
      {onblur}
      {onkeydown}
      {oncontextmenu}
      class="{baseInputClasses} {sizeClasses[size]} {error ? errorClasses : normalClasses} {mono ? 'font-mono' : ''} {className}"
      {style}
      aria-required={required ? 'true' : undefined}
      aria-invalid={error ? 'true' : undefined}
      aria-describedby={error ? `${inputId}-error` : undefined}
    />

    {#if error}
      <p id="{inputId}-error" class="mt-1 text-sm text-[var(--color-error-600)]">
        {error}
      </p>
    {:else if hint}
      <p class="text-xs text-[var(--color-text-secondary)] mt-1">
        {hint}
      </p>
    {/if}
  </div>
{/if}
