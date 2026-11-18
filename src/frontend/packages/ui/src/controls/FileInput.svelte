<script lang="ts">
  interface Props {
    label: string;
    accept?: string;
    multiple?: boolean;
    disabled?: boolean;
    required?: boolean;
    id?: string;
    name?: string;
    hint?: string;
    error?: string;
    onselect?: (files: File[]) => void;
    class?: string;
  }

  let {
    label,
    accept,
    multiple = false,
    disabled = false,
    required = false,
    id,
    name,
    hint,
    error,
    onselect,
    class: className = '',
  }: Props = $props();
  const generatedId = $props.id();
  const inputId = $derived(id ?? generatedId);

  function select(event: Event) {
    const input = event.currentTarget as HTMLInputElement;
    const files = Array.from(input.files ?? []);
    // Clear the browser selection so choosing the same file again emits a change.
    // Consumers retain the File objects and own reading/validating their bytes.
    input.value = '';
    if (!disabled && files.length) onselect?.(files);
  }
</script>

<div class="w-full {className}">
  <label for={inputId} class="block text-xs text-[var(--color-text-muted)] mb-1">
    {label}{#if required}<span class="text-red-500" aria-hidden="true">*</span>{/if}
  </label>
  <input
    id={inputId}
    type="file"
    {name}
    {accept}
    {multiple}
    {disabled}
    {required}
    aria-invalid={error ? 'true' : undefined}
    aria-describedby={error ? `${inputId}-error` : hint ? `${inputId}-hint` : undefined}
    onchange={select}
    class="block w-full rounded-lg border px-3 py-2 text-sm text-[var(--color-text-primary)] bg-[var(--color-surface-100)] focus:outline-none focus:border-[var(--color-accent-primary)] disabled:cursor-not-allowed disabled:bg-[var(--color-bg-tertiary)] {error
      ? 'border-[var(--color-error-500)]'
      : 'border-[var(--color-border-secondary)]'}"
  />
  {#if error}
    <p id={`${inputId}-error`} class="mt-1 text-xs text-[var(--color-error-500)]" role="alert">
      {error}
    </p>
  {:else if hint}
    <p id={`${inputId}-hint`} class="mt-1 text-xs text-[var(--color-text-secondary)]">
      {hint}
    </p>
  {/if}
</div>
