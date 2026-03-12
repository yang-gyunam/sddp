<script lang="ts">
  import type { RadioSize } from '../types';
  import Radio from './Radio.svelte';

  interface RadioOption {
    value: string;
    label: string;
    description?: string;
  }

  interface Props {
    options: RadioOption[];
    value?: string;
    name?: string;
    disabled?: boolean;
    size?: RadioSize;
    orientation?: 'horizontal' | 'vertical';
    onchange?: (value: string) => void;
    class?: string;
  }

  let {
    options,
    value = $bindable(''),
    name,
    disabled = false,
    size = 'md',
    orientation = 'horizontal',
    onchange,
    class: className = '',
  }: Props = $props();

  const fallbackName = `radio-group-${Math.random().toString(36).substring(2, 9)}`;
  const groupName = $derived(name ?? fallbackName);

  function handleChange(optionValue: string) {
    value = optionValue;
    onchange?.(optionValue);
  }
</script>

<div
  class="radio-group radio-group--{orientation} {className}"
  role="radiogroup"
>
  {#each options as opt (opt.value)}
    <Radio
      name={groupName}
      value={opt.value}
      label={opt.label}
      description={opt.description}
      checked={value === opt.value}
      {disabled}
      {size}
      onchange={handleChange}
    />
  {/each}
</div>

<style>
  .radio-group {
    display: flex;
  }

  .radio-group--horizontal {
    flex-direction: row;
    gap: 1.5rem;
  }

  .radio-group--vertical {
    flex-direction: column;
    gap: 0.625rem;
  }
</style>
