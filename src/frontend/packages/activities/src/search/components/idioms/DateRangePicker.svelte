<script lang="ts">
  /**
   * Date Range Picker
   * Date range selection component
   */

  import { DatePicker, Radio } from '@sddp/ui';
  import { toLocalDateString } from '@sddp/shell';
  import type { DateValue } from '@internationalized/date';
  import type { DateRange } from '../../types';

  interface Props {
    value?: DateRange | undefined;
    onChange?: (range: DateRange | undefined) => void;
  }

  let { value: _value = undefined, onChange }: Props = $props();

  type PresetOption = 'any' | 'day' | 'week' | 'month' | 'custom';
  let selectedPreset = $state<PresetOption>('any');

  let customFrom = $state<DateValue | undefined>(undefined);
  let customTo = $state<DateValue | undefined>(undefined);

  function dateValueToString(dv: DateValue | undefined): string {
    if (!dv) return '';
    return `${dv.year}-${String(dv.month).padStart(2, '0')}-${String(dv.day).padStart(2, '0')}`;
  }

  function getDateRange(daysAgo: number): { from: string; to: string } {
    const now = Date.now();
    const fromTime = now - daysAgo * 24 * 60 * 60 * 1000;
    return {
      from: toLocalDateString(new Date(fromTime)),
      to: toLocalDateString(new Date(now)),
    };
  }

  function handlePresetChange(preset: PresetOption) {
    selectedPreset = preset;

    if (preset === 'any') {
      if (onChange) onChange(undefined);
      return;
    }

    if (preset === 'custom') {
      return; // Wait for user to input dates
    }

    const daysMap: Record<Exclude<PresetOption, 'any' | 'custom'>, number> = {
      day: 1,
      week: 7,
      month: 30,
    };

    if (onChange) {
      onChange(getDateRange(daysMap[preset]));
    }
  }

  function handleCustomDateChange() {
    const fromStr = dateValueToString(customFrom);
    const toStr = dateValueToString(customTo);
    if (fromStr && toStr && onChange) {
      onChange({
        from: fromStr,
        to: toStr,
      });
    }
  }
</script>

<div class="date-range-picker">
  <label class="preset-option">
    <Radio
      unstyled
      name="date-preset"
      value="any"
      checked={selectedPreset === 'any'}
      onchange={() => handlePresetChange('any')}
    />
    <span>Any time</span>
  </label>

  <label class="preset-option">
    <Radio
      unstyled
      name="date-preset"
      value="day"
      checked={selectedPreset === 'day'}
      onchange={() => handlePresetChange('day')}
    />
    <span>Last 24 hours</span>
  </label>

  <label class="preset-option">
    <Radio
      unstyled
      name="date-preset"
      value="week"
      checked={selectedPreset === 'week'}
      onchange={() => handlePresetChange('week')}
    />
    <span>Last 7 days</span>
  </label>

  <label class="preset-option">
    <Radio
      unstyled
      name="date-preset"
      value="month"
      checked={selectedPreset === 'month'}
      onchange={() => handlePresetChange('month')}
    />
    <span>Last 30 days</span>
  </label>

  <label class="preset-option">
    <Radio
      unstyled
      name="date-preset"
      value="custom"
      checked={selectedPreset === 'custom'}
      onchange={() => handlePresetChange('custom')}
    />
    <span>Custom range</span>
  </label>

  {#if selectedPreset === 'custom'}
    <div class="custom-range">
      <div class="date-input">
        <DatePicker label="From" bind:value={customFrom} onValueChange={() => handleCustomDateChange()} size="sm" />
      </div>
      <div class="date-input">
        <DatePicker label="To" bind:value={customTo} onValueChange={() => handleCustomDateChange()} size="sm" />
      </div>
    </div>
  {/if}
</div>

<style>
  .date-range-picker {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
  }

  .preset-option {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    padding: 0.375rem 0.5rem;
    cursor: pointer;
    font-size: 0.875rem;
  }

  .preset-option:hover {
    background: var(--hover-bg);
  }

  .custom-range {
    margin-left: 1.5rem;
    padding: 0.5rem;
    background: var(--bg-secondary);
    border-radius: 4px;
  }

  .date-input {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    margin-bottom: 0.5rem;
  }

  .date-input:last-child {
    margin-bottom: 0;
  }

</style>
