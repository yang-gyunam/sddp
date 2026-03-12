<!--
  DatePicker Component
  A date picker with VS Code styling using Bits UI
-->
<script lang="ts">
  import { DatePicker as BitsDatePicker } from 'bits-ui';
  import type { DateValue } from '@internationalized/date';
  import Icon from './Icon.svelte';

  interface Props {
    value?: DateValue;
    placeholder?: DateValue;
    label?: string;
    error?: string;
    disabled?: boolean;
    required?: boolean;
    minValue?: DateValue;
    maxValue?: DateValue;
    locale?: string;
    weekStartsOn?: 0 | 1 | 2 | 3 | 4 | 5 | 6;
    fixedWeeks?: boolean;
    isDateDisabled?: (date: DateValue) => boolean;
    isDateUnavailable?: (date: DateValue) => boolean;
    onValueChange?: (value: DateValue | undefined) => void;
    class?: string;
    size?: 'sm' | 'md' | 'lg';
  }

  let {
    value = $bindable(),
    placeholder = $bindable(),
    label,
    error,
    disabled = false,
    required = false,
    minValue,
    maxValue,
    locale = 'en',
    weekStartsOn = 0,
    fixedWeeks = true,
    isDateDisabled,
    isDateUnavailable,
    onValueChange,
    class: className = '',
    size = 'md',
  }: Props = $props();

  const sizeClasses = {
    sm: 'text-xs',
    md: 'text-sm',
    lg: 'text-base',
  };

  const inputSizeClasses = {
    sm: 'h-7 px-2',
    md: 'h-9 px-3',
    lg: 'h-11 px-4',
  };

  const inputClasses = $derived(
    `datepicker-input ${inputSizeClasses[size]} ${sizeClasses[size]}${error ? ' datepicker-input--error' : ''}${disabled ? ' datepicker-input--disabled' : ''}`
  );
</script>

<div class="datepicker-wrapper {className}">
  <BitsDatePicker.Root
    bind:value
    bind:placeholder
    {disabled}
    {minValue}
    {maxValue}
    {locale}
    {weekStartsOn}
    {fixedWeeks}
    {isDateDisabled}
    {isDateUnavailable}
    {onValueChange}
  >
    {#if label}
      <BitsDatePicker.Label class="block text-sm font-medium text-[var(--color-text-secondary)] mb-1.5">
        {label}
        {#if required}
          <span class="text-[var(--color-error-500)]">*</span>
        {/if}
      </BitsDatePicker.Label>
    {/if}
    <BitsDatePicker.Input class={inputClasses}>
      {#snippet children({ segments })}
        {#each segments as { part, value: segmentValue }, index (part + '-' + index)}
          <BitsDatePicker.Segment
            {part}
            class="datepicker-segment{part === 'literal' ? ' datepicker-segment--literal' : ''}"
          >
            {segmentValue}
          </BitsDatePicker.Segment>
        {/each}
        <BitsDatePicker.Trigger class="datepicker-trigger" {disabled}>
          <Icon name="calendar" size="sm" />
        </BitsDatePicker.Trigger>
      {/snippet}
    </BitsDatePicker.Input>

    <BitsDatePicker.Content class="datepicker-content" sideOffset={8}>
      <BitsDatePicker.Calendar>
        {#snippet children({ months, weekdays })}
          <header class="datepicker-header">
            <BitsDatePicker.PrevButton class="datepicker-nav-btn">
              <Icon name="chevron-left" size="sm" />
            </BitsDatePicker.PrevButton>
            <BitsDatePicker.Heading class="datepicker-heading" />
            <BitsDatePicker.NextButton class="datepicker-nav-btn">
              <Icon name="chevron-right" size="sm" />
            </BitsDatePicker.NextButton>
          </header>

          {#each months as month (month.value)}
            <BitsDatePicker.Grid class="datepicker-grid">
              <BitsDatePicker.GridHead>
                <BitsDatePicker.GridRow class="datepicker-grid-row">
                  {#each weekdays as day (day)}
                    <BitsDatePicker.HeadCell class="datepicker-head-cell">
                      {day.slice(0, 2)}
                    </BitsDatePicker.HeadCell>
                  {/each}
                </BitsDatePicker.GridRow>
              </BitsDatePicker.GridHead>
              <BitsDatePicker.GridBody>
                {#each month.weeks as weekDates, weekIndex (weekIndex)}
                  <BitsDatePicker.GridRow class="datepicker-grid-row">
                    {#each weekDates as date (date.toString())}
                      <BitsDatePicker.Cell {date} month={month.value} class="datepicker-cell">
                        <BitsDatePicker.Day class="datepicker-day" />
                      </BitsDatePicker.Cell>
                    {/each}
                  </BitsDatePicker.GridRow>
                {/each}
              </BitsDatePicker.GridBody>
            </BitsDatePicker.Grid>
          {/each}
        {/snippet}
      </BitsDatePicker.Calendar>
    </BitsDatePicker.Content>
  </BitsDatePicker.Root>

  {#if error}
    <p class="mt-1.5 text-sm text-[var(--color-error-600)]">{error}</p>
  {/if}
</div>

<style>
  .datepicker-wrapper {
    width: 100%;
  }

  :global(.datepicker-input) {
    display: inline-flex;
    align-items: center;
    gap: 0.25rem;
    width: 100%;
    border-radius: var(--radius-md, 6px);
    border: 1px solid var(--color-border);
    background-color: var(--color-bg-primary);
    color: var(--color-text-primary);
    transition: border-color 150ms ease-in-out, box-shadow 150ms ease-in-out;
  }

  :global(.datepicker-input:focus-within) {
    border-color: var(--color-accent-primary);
    box-shadow: 0 0 0 2px var(--color-accent-primary) / 0.2;
    outline: none;
  }

  :global(.datepicker-input--error) {
    border-color: var(--color-error-500);
    background-color: var(--color-error-50);
  }

  :global(.dark .datepicker-input--error) {
    background-color: rgba(239, 68, 68, 0.1);
  }

  :global(.datepicker-input--disabled) {
    background-color: var(--color-bg-tertiary);
    cursor: not-allowed;
    opacity: 0.6;
  }

  :global(.datepicker-segment) {
    padding: 0 1px;
    border-radius: 2px;
    outline: none;
  }

  :global(.datepicker-segment:focus) {
    background-color: var(--color-accent-primary);
    color: var(--color-text-on-accent);
  }

  :global(.datepicker-segment--literal) {
    color: var(--color-text-tertiary);
  }

  :global(.datepicker-trigger) {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    margin-left: auto;
    padding: 0.25rem;
    border-radius: var(--radius-sm, 4px);
    color: var(--color-text-secondary);
    transition: color 150ms ease-in-out, background-color 150ms ease-in-out;
  }

  :global(.datepicker-trigger:hover:not(:disabled)) {
    background-color: var(--color-bg-hover);
    color: var(--color-text-primary);
  }

  :global(.datepicker-trigger:disabled) {
    cursor: not-allowed;
    opacity: 0.5;
  }

  :global(.datepicker-content) {
    z-index: var(--z-popover, 1060);
    padding: 0.75rem;
    border-radius: var(--radius-lg, 8px);
    border: 1px solid var(--color-border);
    background-color: var(--color-bg-primary);
    box-shadow: var(--shadow-lg);
  }

  :global(.datepicker-header) {
    display: flex;
    align-items: center;
    justify-content: space-between;
    margin-bottom: 0.5rem;
  }

  :global(.datepicker-heading) {
    font-size: var(--text-sm);
    font-weight: 600;
    color: var(--color-text-primary);
  }

  :global(.datepicker-nav-btn) {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    width: 1.75rem;
    height: 1.75rem;
    border-radius: var(--radius-md, 6px);
    color: var(--color-text-secondary);
    transition: background-color 150ms ease-in-out, color 150ms ease-in-out;
  }

  :global(.datepicker-nav-btn:hover) {
    background-color: var(--color-bg-hover);
    color: var(--color-text-primary);
  }

  :global(.datepicker-nav-btn:disabled) {
    opacity: 0.4;
    cursor: not-allowed;
  }

  :global(.datepicker-grid) {
    width: 100%;
    border-collapse: collapse;
  }

  :global(.datepicker-grid-row) {
    display: flex;
  }

  :global(.datepicker-head-cell) {
    width: 2rem;
    height: 2rem;
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: var(--text-xs);
    font-weight: 500;
    color: var(--color-text-tertiary);
  }

  :global(.datepicker-cell) {
    padding: 1px;
  }

  :global(.datepicker-day) {
    width: 2rem;
    height: 2rem;
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: var(--text-sm);
    border-radius: var(--radius-md, 6px);
    color: var(--color-text-primary);
    transition: background-color 150ms ease-in-out, color 150ms ease-in-out;
  }

  :global(.datepicker-day:hover:not([data-disabled]):not([data-unavailable])) {
    background-color: var(--color-bg-hover);
  }

  :global(.datepicker-day[data-selected]) {
    background-color: var(--color-accent-primary);
    color: var(--color-text-on-accent);
  }

  :global(.datepicker-day[data-today]:not([data-selected])) {
    border: 1px solid var(--color-accent-primary);
  }

  :global(.datepicker-day[data-outside-month]) {
    color: var(--color-text-tertiary);
    opacity: 0.5;
  }

  :global(.datepicker-day[data-disabled]) {
    color: var(--color-text-tertiary);
    opacity: 0.4;
    cursor: not-allowed;
  }

  :global(.datepicker-day[data-unavailable]) {
    color: var(--color-error-500);
    text-decoration: line-through;
    cursor: not-allowed;
  }
</style>
