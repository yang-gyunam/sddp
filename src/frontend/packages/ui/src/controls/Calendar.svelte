<!--
  Calendar Component
  A standalone calendar with VS Code styling using Bits UI
  Useful for timesheet displays, date selection, etc.
-->
<script lang="ts">
  import { Calendar as BitsCalendar } from 'bits-ui';
  import type { DateValue } from '@internationalized/date';
  import Icon from './Icon.svelte';

  interface Props {
    value?: DateValue;
    placeholder?: DateValue;
    minValue?: DateValue;
    maxValue?: DateValue;
    locale?: string;
    weekStartsOn?: 0 | 1 | 2 | 3 | 4 | 5 | 6;
    fixedWeeks?: boolean;
    numberOfMonths?: number;
    pagedNavigation?: boolean;
    isDateDisabled?: (date: DateValue) => boolean;
    isDateUnavailable?: (date: DateValue) => boolean;
    onValueChange?: (value: DateValue | undefined) => void;
    onPlaceholderChange?: (placeholder: DateValue) => void;
    class?: string;
    /** Custom cell renderer */
    renderCell?: (date: DateValue) => { class?: string; content?: string };
  }

  let {
    value = $bindable(),
    placeholder = $bindable(),
    minValue,
    maxValue,
    locale = 'en',
    weekStartsOn = 0,
    fixedWeeks = true,
    numberOfMonths = 1,
    pagedNavigation = true,
    isDateDisabled,
    isDateUnavailable,
    onValueChange,
    onPlaceholderChange,
    class: className = '',
    renderCell,
  }: Props = $props();
</script>

<div class="calendar-wrapper {className}">
  <BitsCalendar.Root
    type="single"
    bind:value
    bind:placeholder
    {minValue}
    {maxValue}
    {locale}
    {weekStartsOn}
    {fixedWeeks}
    {numberOfMonths}
    {pagedNavigation}
    {isDateDisabled}
    {isDateUnavailable}
    {onValueChange}
    {onPlaceholderChange}
  >
    {#snippet children({ months, weekdays })}
      <header class="calendar-header">
        <BitsCalendar.PrevButton class="calendar-nav-btn">
          <Icon name="chevron-left" size="sm" />
        </BitsCalendar.PrevButton>
        <BitsCalendar.Heading class="calendar-heading" />
        <BitsCalendar.NextButton class="calendar-nav-btn">
          <Icon name="chevron-right" size="sm" />
        </BitsCalendar.NextButton>
      </header>

      <div class="calendar-months">
        {#each months as month (month.value)}
          <BitsCalendar.Grid class="calendar-grid">
            <BitsCalendar.GridHead>
              <BitsCalendar.GridRow class="calendar-grid-row">
                {#each weekdays as day (day)}
                  <BitsCalendar.HeadCell class="calendar-head-cell">
                    {day.slice(0, 2)}
                  </BitsCalendar.HeadCell>
                {/each}
              </BitsCalendar.GridRow>
            </BitsCalendar.GridHead>
            <BitsCalendar.GridBody>
              {#each month.weeks as weekDates, weekIndex (weekIndex)}
                <BitsCalendar.GridRow class="calendar-grid-row">
                  {#each weekDates as date (date.toString())}
                    {@const cellInfo = renderCell?.(date)}
                    <BitsCalendar.Cell
                      {date}
                      month={month.value}
                      class="calendar-cell{cellInfo?.class ? ` ${cellInfo.class}` : ''}"
                    >
                      <BitsCalendar.Day class="calendar-day">
                        {#if cellInfo?.content}
                          <span class="calendar-day-content">{cellInfo.content}</span>
                        {/if}
                      </BitsCalendar.Day>
                    </BitsCalendar.Cell>
                  {/each}
                </BitsCalendar.GridRow>
              {/each}
            </BitsCalendar.GridBody>
          </BitsCalendar.Grid>
        {/each}
      </div>
    {/snippet}
  </BitsCalendar.Root>
</div>

<style>
  .calendar-wrapper {
    display: inline-block;
    padding: 0.75rem;
    border-radius: var(--radius-lg, 8px);
    border: 1px solid var(--color-border);
    background-color: var(--color-bg-primary);
  }

  :global(.calendar-header) {
    display: flex;
    align-items: center;
    justify-content: space-between;
    margin-bottom: 0.75rem;
  }

  :global(.calendar-heading) {
    font-size: var(--text-sm);
    font-weight: 600;
    color: var(--color-text-primary);
  }

  :global(.calendar-nav-btn) {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    width: 1.75rem;
    height: 1.75rem;
    border-radius: var(--radius-md, 6px);
    color: var(--color-text-secondary);
    transition: background-color 150ms ease-in-out, color 150ms ease-in-out;
  }

  :global(.calendar-nav-btn:hover) {
    background-color: var(--color-bg-hover);
    color: var(--color-text-primary);
  }

  :global(.calendar-nav-btn:disabled) {
    opacity: 0.4;
    cursor: not-allowed;
  }

  .calendar-months {
    display: flex;
    gap: 1.5rem;
  }

  :global(.calendar-grid) {
    width: 100%;
    border-collapse: collapse;
  }

  :global(.calendar-grid-row) {
    display: flex;
  }

  :global(.calendar-head-cell) {
    width: 2.25rem;
    height: 2.25rem;
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: var(--text-xs);
    font-weight: 500;
    color: var(--color-text-tertiary);
  }

  :global(.calendar-cell) {
    padding: 2px;
  }

  :global(.calendar-day) {
    position: relative;
    width: 2.25rem;
    height: 2.25rem;
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: var(--text-sm);
    border-radius: var(--radius-md, 6px);
    color: var(--color-text-primary);
    transition: background-color 150ms ease-in-out, color 150ms ease-in-out;
    cursor: pointer;
  }

  :global(.calendar-day:hover:not([data-disabled]):not([data-unavailable])) {
    background-color: var(--color-bg-hover);
  }

  :global(.calendar-day[data-selected]) {
    background-color: var(--color-accent-primary);
    color: var(--color-text-on-accent);
  }

  :global(.calendar-day[data-today]:not([data-selected])) {
    border: 1px solid var(--color-accent-primary);
  }

  :global(.calendar-day[data-outside-month]) {
    color: var(--color-text-tertiary);
    opacity: 0.5;
  }

  :global(.calendar-day[data-disabled]) {
    color: var(--color-text-tertiary);
    opacity: 0.4;
    cursor: not-allowed;
  }

  :global(.calendar-day[data-unavailable]) {
    color: var(--color-error-500);
    text-decoration: line-through;
    cursor: not-allowed;
  }

  :global(.calendar-day-content) {
    position: absolute;
    bottom: 2px;
    font-size: 0.625rem;
    color: var(--color-text-tertiary);
  }
</style>
