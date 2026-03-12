<!--
  TimePicker Component
  A time picker with VS Code styling
-->
<script lang="ts">
  import Icon from './Icon.svelte';

  interface Props {
    value?: string; // HH:mm or HH:mm:ss format
    label?: string;
    error?: string;
    disabled?: boolean;
    required?: boolean;
    min?: string;
    max?: string;
    step?: number; // seconds (60 = 1 min, 3600 = 1 hour)
    showSeconds?: boolean;
    use24Hour?: boolean;
    onchange?: (value: string) => void;
    class?: string;
    size?: 'sm' | 'md' | 'lg';
  }

  let {
    value = $bindable(''),
    label,
    error,
    disabled = false,
    required = false,
    min,
    max,
    step = 60,
    showSeconds: _showSeconds = false,
    use24Hour: _use24Hour = true,
    onchange,
    class: className = '',
    size = 'md',
  }: Props = $props();

  const sizeClasses = {
    sm: 'h-7 px-2 text-xs',
    md: 'h-9 px-3 text-sm',
    lg: 'h-11 px-4 text-base',
  };

  function handleChange(e: Event) {
    const target = e.target as HTMLInputElement;
    value = target.value;
    onchange?.(value);
  }

  const generatedId = `timepicker-${Math.random().toString(36).substring(2, 9)}`;
</script>

<div class="timepicker-wrapper {className}">
  {#if label}
    <label for={generatedId} class="block text-sm font-medium text-[var(--color-text-secondary)] mb-1.5">
      {label}
      {#if required}
        <span class="text-[var(--color-error-500)]">*</span>
      {/if}
    </label>
  {/if}

  <div
    class="timepicker-input {sizeClasses[size]}"
    class:timepicker-input--error={!!error}
    class:timepicker-input--disabled={disabled}
  >
    <input
      id={generatedId}
      type="time"
      {value}
      {disabled}
      {required}
      {min}
      {max}
      {step}
      onchange={handleChange}
      class="timepicker-native-input"
    />
    <div class="timepicker-icon">
      <Icon name="clock" size="sm" />
    </div>
  </div>

  {#if error}
    <p class="mt-1.5 text-sm text-[var(--color-error-600)]">{error}</p>
  {/if}
</div>

<style>
  .timepicker-wrapper {
    width: 100%;
  }

  .timepicker-input {
    position: relative;
    display: flex;
    align-items: center;
    width: 100%;
    border-radius: var(--radius-md, 6px);
    border: 1px solid var(--color-border);
    background-color: var(--color-bg-primary);
    color: var(--color-text-primary);
    transition: border-color 150ms ease-in-out, box-shadow 150ms ease-in-out;
  }

  .timepicker-input:focus-within {
    border-color: var(--color-accent-primary);
    box-shadow: 0 0 0 2px rgba(99, 102, 241, 0.2);
    outline: none;
  }

  .timepicker-input--error {
    border-color: var(--color-error-500);
    background-color: var(--color-error-50);
  }

  :global(.dark) .timepicker-input--error {
    background-color: rgba(239, 68, 68, 0.1);
  }

  .timepicker-input--disabled {
    background-color: var(--color-bg-tertiary);
    cursor: not-allowed;
    opacity: 0.6;
  }

  .timepicker-native-input {
    flex: 1;
    background: transparent;
    border: none;
    outline: none;
    color: inherit;
    font-size: inherit;
    font-family: inherit;
  }

  .timepicker-native-input:disabled {
    cursor: not-allowed;
  }

  /* Hide native time picker icon in webkit browsers */
  .timepicker-native-input::-webkit-calendar-picker-indicator {
    opacity: 0;
    position: absolute;
    right: 0;
    width: 100%;
    height: 100%;
    cursor: pointer;
  }

  .timepicker-icon {
    display: flex;
    align-items: center;
    justify-content: center;
    padding: 0 0.5rem;
    color: var(--color-text-secondary);
    pointer-events: none;
  }
</style>
