<script lang="ts">
  import type { CheckboxSize } from '../types';
  import Checkbox from './Checkbox.svelte';

  interface CheckboxListOption {
    value: string;
    label: string;
    description?: string;
  }

  interface Props {
    options: CheckboxListOption[];
    selected?: string[];
    disabled?: boolean;
    orientation?: 'horizontal' | 'vertical';
    size?: CheckboxSize;
    onchange?: (selected: string[]) => void;
    class?: string;
  }

  let {
    options,
    selected = $bindable([]),
    disabled = false,
    orientation = 'vertical',
    size = 'md',
    onchange,
    class: className = '',
  }: Props = $props();

  function handleToggle(value: string, checked: boolean) {
    if (checked) {
      selected = [...selected, value];
    } else {
      selected = selected.filter((v) => v !== value);
    }
    onchange?.(selected);
  }
</script>

<div
  class="flex gap-1 {orientation === 'horizontal' ? 'flex-wrap flex-row gap-x-4' : 'flex-col'} {className}"
>
  {#each options as option (option.value)}
    <Checkbox
      label={option.label}
      description={option.description}
      checked={selected.includes(option.value)}
      {disabled}
      {size}
      onchange={(checked) => handleToggle(option.value, checked)}
    />
  {/each}
</div>
