<!--
  SearchField Control
  Debounced search input with search icon, clear button, Enter/Escape support.
  Consolidates repeated debounce search patterns across the codebase.
-->
<script lang="ts">
  import Icon from './Icon.svelte';
  import IconButton from './IconButton.svelte';

  interface Props {
    value?: string;
    placeholder?: string;
    debounceMs?: number;
    disabled?: boolean;
    size?: 'sm' | 'md';
    clearable?: boolean;
    onSearch?: (value: string) => void;
    class?: string;
  }

  let {
    value = $bindable(''),
    placeholder = 'Search...',
    debounceMs = 300,
    disabled = false,
    size = 'md',
    clearable = true,
    onSearch,
    class: className = '',
  }: Props = $props();

  let inputElement: HTMLInputElement | undefined = $state();
  let debounceTimer: ReturnType<typeof setTimeout> | undefined;

  const sizeClasses = $derived(
    size === 'sm'
      ? 'pl-8 pr-7 py-1.5 text-xs'
      : 'pl-9 pr-8 py-2 text-sm'
  );

  const iconSize = $derived(size === 'sm' ? 'xs' as const : 'sm' as const);
  const clearBtnSize = $derived(size === 'sm' ? 'xs' as const : 'xs' as const);

  function handleInput(e: Event) {
    value = (e.target as HTMLInputElement).value;
    if (debounceTimer) {
      clearTimeout(debounceTimer);
      debounceTimer = undefined;
    }
    debounceTimer = setTimeout(() => onSearch?.(value), debounceMs);
  }

  function handleKeydown(e: KeyboardEvent) {
    if (e.key === 'Enter') {
      if (debounceTimer) {
        clearTimeout(debounceTimer);
        debounceTimer = undefined;
      }
      onSearch?.(value);
    } else if (e.key === 'Escape') {
      if (debounceTimer) {
        clearTimeout(debounceTimer);
        debounceTimer = undefined;
      }
      value = '';
      onSearch?.('');
    }
  }

  function handleClear() {
    if (debounceTimer) {
      clearTimeout(debounceTimer);
      debounceTimer = undefined;
    }
    value = '';
    onSearch?.('');
    inputElement?.focus();
  }

  export function focus() {
    inputElement?.focus();
  }
</script>

<div class="relative flex items-center {className}">
  <Icon
    name="search"
    size={iconSize}
    class="absolute {size === 'sm' ? 'left-2' : 'left-3'} pointer-events-none text-[var(--color-text-muted)]"
  />
  <input
    bind:this={inputElement}
    type="text"
    {value}
    {placeholder}
    {disabled}
    oninput={handleInput}
    onkeydown={handleKeydown}
    class="w-full {sizeClasses} rounded-lg border border-[var(--color-border-secondary)]
      bg-[var(--color-bg-primary)] text-[var(--color-text-primary)]
      placeholder:text-[var(--color-text-muted)] placeholder:opacity-60
      focus:outline-none focus:border-[var(--color-accent-primary)]
      disabled:bg-[var(--color-bg-tertiary)] disabled:cursor-not-allowed
      transition-colors"
  />
  {#if clearable && value}
    <div class="absolute {size === 'sm' ? 'right-1' : 'right-1.5'}">
      <IconButton
        icon="x"
        size={clearBtnSize}
        onclick={handleClear}
        title="Clear search"
      />
    </div>
  {/if}
</div>
