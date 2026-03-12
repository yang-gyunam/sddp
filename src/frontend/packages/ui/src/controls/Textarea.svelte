<script lang="ts">
  interface Props {
    value?: string;
    placeholder?: string;
    label?: string;
    hint?: string;
    error?: string;
    disabled?: boolean;
    required?: boolean;
    mono?: boolean;
    rows?: number;
    maxLength?: number;
    resize?: 'none' | 'vertical' | 'horizontal' | 'both';
    id?: string;
    name?: string;
    /** Strip all built-in styles — class prop applies directly to the textarea element */
    unstyled?: boolean;
    /** Expose the underlying textarea element for direct DOM access (e.g. cursor position) */
    element?: HTMLTextAreaElement;
    oninput?: (e: Event) => void;
    onchange?: (e: Event) => void;
    onfocus?: (e: FocusEvent) => void;
    onblur?: (e: FocusEvent) => void;
    onkeydown?: (e: KeyboardEvent) => void;
    oncontextmenu?: (e: MouseEvent) => void;
    class?: string;
    style?: string;
  }

  let {
    value = $bindable(''),
    placeholder = '',
    label,
    hint,
    error,
    disabled = false,
    required = false,
    mono = false,
    rows = 3,
    maxLength,
    resize = 'vertical',
    id,
    name,
    unstyled = false,
    element = $bindable(undefined as HTMLTextAreaElement | undefined),
    oninput,
    onchange,
    onfocus,
    onblur,
    onkeydown,
    oncontextmenu,
    class: className = '',
    style,
  }: Props = $props();

  const generatedId = `textarea-${Math.random().toString(36).substring(2, 9)}`;
  const textareaId = $derived(id ?? generatedId);

  const baseClasses =
    'block w-full rounded-lg border px-3 py-2 text-sm transition-colors focus:outline-none disabled:bg-[var(--color-bg-tertiary)] disabled:cursor-not-allowed';

  const normalClasses =
    'border-[var(--color-border-secondary)] bg-[var(--color-surface-100)] text-[var(--color-text-primary)] placeholder:text-[var(--color-text-tertiary)] placeholder:opacity-60 focus:border-[var(--color-accent-primary)]';

  const errorClasses =
    'border-[var(--color-error-500)] bg-[var(--color-error-50)] text-[var(--color-text-primary)] focus:border-[var(--color-error-500)] dark:bg-[var(--color-error-900)]/10';

  const resizeClasses: Record<string, string> = {
    none: 'resize-none',
    vertical: 'resize-y',
    horizontal: 'resize-x',
    both: 'resize',
  };
</script>

{#if unstyled}
  <textarea
    id={id}
    {name}
    {placeholder}
    {disabled}
    {required}
    {rows}
    maxlength={maxLength}
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
  ></textarea>
{:else}
  <div class="w-full {className}">
    {#if label}
      <label for={textareaId} class="block text-xs text-[var(--color-text-muted)] mb-1">
        {label}
        {#if required}
          <span class="text-red-500">*</span>
        {/if}
      </label>
    {/if}

    <textarea
      id={textareaId}
      {name}
      {placeholder}
      {disabled}
      {required}
      {rows}
      maxlength={maxLength}
      bind:value
      bind:this={element}
      {oninput}
      {onchange}
      {onfocus}
      {onblur}
      {onkeydown}
      {oncontextmenu}
      class="{baseClasses} {error ? errorClasses : normalClasses} {resizeClasses[resize]} {mono ? 'font-mono' : ''}"
      {style}
      aria-required={required ? 'true' : undefined}
      aria-invalid={error ? 'true' : undefined}
      aria-describedby={error ? `${textareaId}-error` : undefined}
    ></textarea>

    <div class="flex justify-between items-center mt-1">
      {#if error}
        <p id="{textareaId}-error" class="text-sm text-[var(--color-error-600)]">
          {error}
        </p>
      {:else if hint}
        <p class="text-xs text-[var(--color-text-muted)]">
          {hint}
        </p>
      {:else}
        <span></span>
      {/if}
      {#if maxLength}
        <span class="text-xs text-[var(--color-text-tertiary)]">
          {value.length}/{maxLength}
        </span>
      {/if}
    </div>
  </div>
{/if}
