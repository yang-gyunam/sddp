<!--
  DateRangePicker Component
  A date range picker with VS Code styling using Bits UI
-->
<script lang="ts">
  import { DateRangePicker as BitsDateRangePicker } from 'bits-ui';
  import type { DateValue } from '@internationalized/date';
  import Icon from './Icon.svelte';

  interface DateRange {
    start: DateValue | undefined;
    end: DateValue | undefined;
  }

  interface Props {
    value?: DateRange;
    placeholder?: DateValue;
    label?: string;
    startLabel?: string;
    endLabel?: string;
    error?: string;
    disabled?: boolean;
    required?: boolean;
    minValue?: DateValue;
    maxValue?: DateValue;
    locale?: string;
    weekStartsOn?: 0 | 1 | 2 | 3 | 4 | 5 | 6;
    fixedWeeks?: boolean;
    numberOfMonths?: number;
    isDateDisabled?: (date: DateValue) => boolean;
    isDateUnavailable?: (date: DateValue) => boolean;
    onValueChange?: (value: DateRange | undefined) => void;
    class?: string;
    size?: 'sm' | 'md' | 'lg';
  }

  let {
    value = $bindable(),
    placeholder = $bindable(),
    label,
    startLabel = 'Start',
    endLabel = 'End',
    error,
    disabled = false,
    required = false,
    minValue,
    maxValue,
    locale = 'en',
    weekStartsOn = 0,
    fixedWeeks = true,
    numberOfMonths = 2,
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

  const wrapperClasses = $derived(
    `daterangepicker-inputs ${sizeClasses[size]}${error ? ' daterangepicker-inputs--error' : ''}${disabled ? ' daterangepicker-inputs--disabled' : ''}`
  );
</script>

<div class="daterangepicker-wrapper {className}">
  <BitsDateRangePicker.Root
    bind:value
    bind:placeholder
    {disabled}
    {minValue}
    {maxValue}
    {locale}
    {weekStartsOn}
    {fixedWeeks}
    {numberOfMonths}
    {isDateDisabled}
    {isDateUnavailable}
    {onValueChange}
  >
    {#if label}
      <BitsDateRangePicker.Label class="block text-sm font-medium text-[var(--color-text-secondary)] mb-1.5">
        {label}
        {#if required}
          <span class="text-[var(--color-error-500)]">*</span>
        {/if}
      </BitsDateRangePicker.Label>
    {/if}
    <div class={wrapperClasses}>
      <div class="daterangepicker-field">
        <span class="daterangepicker-field-label">{startLabel}</span>
        <BitsDateRangePicker.Input type="start" class="daterangepicker-input {inputSizeClasses[size]}">
          {#snippet children({ segments })}
            {#each segments as { part, value: segmentValue }, index (part + '-' + index)}
              <BitsDateRangePicker.Segment
                {part}
                class="daterangepicker-segment{part === 'literal' ? ' daterangepicker-segment--literal' : ''}"
              >
                {segmentValue}
              </BitsDateRangePicker.Segment>
            {/each}
          {/snippet}
        </BitsDateRangePicker.Input>
      </div>

      <div class="daterangepicker-separator">
        <Icon name="arrow-right" size="sm" />
      </div>

      <div class="daterangepicker-field">
        <span class="daterangepicker-field-label">{endLabel}</span>
        <BitsDateRangePicker.Input type="end" class="daterangepicker-input {inputSizeClasses[size]}">
          {#snippet children({ segments })}
            {#each segments as { part, value: segmentValue }, index (part + '-' + index)}
              <BitsDateRangePicker.Segment
                {part}
                class="daterangepicker-segment{part === 'literal' ? ' daterangepicker-segment--literal' : ''}"
              >
                {segmentValue}
              </BitsDateRangePicker.Segment>
            {/each}
          {/snippet}
        </BitsDateRangePicker.Input>
      </div>

      <BitsDateRangePicker.Trigger class="daterangepicker-trigger" {disabled}>
        <Icon name="calendar" size="sm" />
      </BitsDateRangePicker.Trigger>
    </div>

    <BitsDateRangePicker.Content class="daterangepicker-content" sideOffset={8}>
      <BitsDateRangePicker.Calendar>
        {#snippet children({ months, weekdays })}
          <header class="daterangepicker-header">
            <BitsDateRangePicker.PrevButton class="daterangepicker-nav-btn">
              <Icon name="chevron-left" size="sm" />
            </BitsDateRangePicker.PrevButton>
            <BitsDateRangePicker.Heading class="daterangepicker-heading" />
            <BitsDateRangePicker.NextButton class="daterangepicker-nav-btn">
              <Icon name="chevron-right" size="sm" />
            </BitsDateRangePicker.NextButton>
          </header>

          <div class="daterangepicker-months">
            {#each months as month (month.value)}
              <BitsDateRangePicker.Grid class="daterangepicker-grid">
                <BitsDateRangePicker.GridHead>
                  <BitsDateRangePicker.GridRow class="daterangepicker-grid-row">
                    {#each weekdays as day (day)}
                      <BitsDateRangePicker.HeadCell class="daterangepicker-head-cell">
                        {day.slice(0, 2)}
                      </BitsDateRangePicker.HeadCell>
                    {/each}
                  </BitsDateRangePicker.GridRow>
                </BitsDateRangePicker.GridHead>
                <BitsDateRangePicker.GridBody>
                  {#each month.weeks as weekDates, weekIndex (weekIndex)}
                    <BitsDateRangePicker.GridRow class="daterangepicker-grid-row">
                      {#each weekDates as date (date.toString())}
                        <BitsDateRangePicker.Cell {date} month={month.value} class="daterangepicker-cell">
                          <BitsDateRangePicker.Day class="daterangepicker-day" />
                        </BitsDateRangePicker.Cell>
                      {/each}
                    </BitsDateRangePicker.GridRow>
                  {/each}
                </BitsDateRangePicker.GridBody>
              </BitsDateRangePicker.Grid>
            {/each}
          </div>
        {/snippet}
      </BitsDateRangePicker.Calendar>
    </BitsDateRangePicker.Content>
  </BitsDateRangePicker.Root>

  {#if error}
    <p class="mt-1.5 text-sm text-[var(--color-error-600)]">{error}</p>
  {/if}
</div>

<style>
  .daterangepicker-wrapper {
    width: 100%;
  }

  .daterangepicker-inputs {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    padding: 0.25rem;
    border-radius: var(--radius-md, 6px);
    border: 1px solid var(--color-border);
    background-color: var(--color-bg-primary);
    transition: border-color 150ms ease-in-out, box-shadow 150ms ease-in-out;
  }

  .daterangepicker-inputs:focus-within {
    border-color: var(--color-accent-primary);
    box-shadow: 0 0 0 2px rgba(99, 102, 241, 0.2);
  }

  .daterangepicker-inputs--error {
    border-color: var(--color-error-500);
  }

  .daterangepicker-inputs--disabled {
    background-color: var(--color-bg-tertiary);
    opacity: 0.6;
  }

  .daterangepicker-field {
    display: flex;
    flex-direction: column;
    flex: 1;
  }

  .daterangepicker-field-label {
    font-size: var(--text-xs);
    color: var(--color-text-tertiary);
    margin-bottom: 0.125rem;
    padding-left: 0.25rem;
  }

  :global(.daterangepicker-input) {
    display: flex;
    align-items: center;
    gap: 0.125rem;
    color: var(--color-text-primary);
  }

  :global(.daterangepicker-segment) {
    padding: 0 1px;
    border-radius: 2px;
    outline: none;
  }

  :global(.daterangepicker-segment:focus) {
    background-color: var(--color-accent-primary);
    color: var(--color-text-on-accent);
  }

  :global(.daterangepicker-segment--literal) {
    color: var(--color-text-tertiary);
  }

  .daterangepicker-separator {
    color: var(--color-text-tertiary);
    padding: 0 0.25rem;
  }

  :global(.daterangepicker-trigger) {
    display: flex;
    align-items: center;
    justify-content: center;
    padding: 0.5rem;
    border-radius: var(--radius-sm, 4px);
    color: var(--color-text-secondary);
    transition: color 150ms ease-in-out, background-color 150ms ease-in-out;
  }

  :global(.daterangepicker-trigger:hover:not(:disabled)) {
    background-color: var(--color-bg-hover);
    color: var(--color-text-primary);
  }

  :global(.daterangepicker-content) {
    z-index: var(--z-popover, 1060);
    padding: 0.75rem;
    border-radius: var(--radius-lg, 8px);
    border: 1px solid var(--color-border);
    background-color: var(--color-bg-primary);
    box-shadow: var(--shadow-lg);
  }

  :global(.daterangepicker-header) {
    display: flex;
    align-items: center;
    justify-content: space-between;
    margin-bottom: 0.5rem;
  }

  :global(.daterangepicker-heading) {
    font-size: var(--text-sm);
    font-weight: 600;
    color: var(--color-text-primary);
  }

  :global(.daterangepicker-nav-btn) {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    width: 1.75rem;
    height: 1.75rem;
    border-radius: var(--radius-md, 6px);
    color: var(--color-text-secondary);
    transition: background-color 150ms ease-in-out, color 150ms ease-in-out;
  }

  :global(.daterangepicker-nav-btn:hover) {
    background-color: var(--color-bg-hover);
    color: var(--color-text-primary);
  }

  .daterangepicker-months {
    display: flex;
    gap: 1rem;
  }

  :global(.daterangepicker-grid) {
    width: 100%;
    border-collapse: collapse;
  }

  :global(.daterangepicker-grid-row) {
    display: flex;
  }

  :global(.daterangepicker-head-cell) {
    width: 2rem;
    height: 2rem;
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: var(--text-xs);
    font-weight: 500;
    color: var(--color-text-tertiary);
  }

  :global(.daterangepicker-cell) {
    padding: 1px;
  }

  :global(.daterangepicker-day) {
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

  :global(.daterangepicker-day:hover:not([data-disabled]):not([data-unavailable])) {
    background-color: var(--color-bg-hover);
  }

  :global(.daterangepicker-day[data-selected]) {
    background-color: var(--color-accent-primary);
    color: var(--color-text-on-accent);
  }

  :global(.daterangepicker-day[data-selection-start]),
  :global(.daterangepicker-day[data-selection-end]) {
    background-color: var(--color-accent-primary);
    color: var(--color-text-on-accent);
  }

  :global(.daterangepicker-day[data-highlighted]) {
    background-color: var(--color-accent-primary);
  }

  :global(.dark .daterangepicker-day[data-highlighted]) {
    background-color: rgba(99, 102, 241, 0.2);
  }

  :global(.daterangepicker-day[data-today]:not([data-selected])) {
    border: 1px solid var(--color-accent-primary);
  }

  :global(.daterangepicker-day[data-outside-month]) {
    color: var(--color-text-tertiary);
    opacity: 0.5;
  }

  :global(.daterangepicker-day[data-disabled]) {
    color: var(--color-text-tertiary);
    opacity: 0.4;
    cursor: not-allowed;
  }

  :global(.daterangepicker-day[data-unavailable]) {
    color: var(--color-error-500);
    text-decoration: line-through;
    cursor: not-allowed;
  }
</style>
