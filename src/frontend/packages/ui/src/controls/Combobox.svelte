<script lang="ts">
  import type { SelectSize, ComboboxOption } from '../types';

  interface Props {
    value?: string;
    options: ComboboxOption[];
    placeholder?: string;
    label?: string;
    hint?: string;
    error?: string;
    disabled?: boolean;
    required?: boolean;
    size?: SelectSize;
    id?: string;
    onchange?: (value: string) => void;
    /** Server-side search callback. When provided, client-side filtering is disabled. */
    onsearch?: (query: string) => void;
    /** Show loading indicator in dropdown (use with onsearch). */
    loading?: boolean;
    class?: string;
  }

  let {
    value = $bindable(''),
    options,
    placeholder = 'Search...',
    label,
    hint,
    error,
    disabled = false,
    required = false,
    size = 'md',
    id,
    onchange,
    onsearch,
    loading = false,
    class: className = '',
  }: Props = $props();

  const generatedId = `combobox-${Math.random().toString(36).substring(2, 9)}`;
  const inputId = $derived(id ?? generatedId);

  let query = $state('');
  let isOpen = $state(false);
  let highlightedIndex = $state(-1);
  let inputEl: HTMLInputElement | undefined = $state();
  let listEl: HTMLUListElement | undefined = $state();
  let containerEl: HTMLDivElement | undefined = $state();
  let dropdownStyle = $state('');

  // Find the selected option's label for display
  const selectedOption = $derived(options.find((o) => o.value === value));

  // Filter options: when onsearch is provided, show options as-is (server handles filtering)
  const filteredOptions = $derived(
    onsearch
      ? options.filter((o) => !o.disabled)
      : query.trim() === ''
        ? options.filter((o) => !o.disabled)
        : options.filter((o) => {
            if (o.disabled) return false;
            const q = query.toLowerCase();
            return (
              o.label.toLowerCase().includes(q) ||
              (o.description?.toLowerCase().includes(q) ?? false)
            );
          })
  );

  const sizeClasses: Record<SelectSize, string> = {
    sm: 'px-2 py-1 text-xs',
    md: 'px-3 py-2 text-sm',
    lg: 'px-4 py-2.5 text-base',
  };

  function selectOption(option: ComboboxOption): void {
    value = option.value;
    query = '';
    isOpen = false;
    highlightedIndex = -1;
    onchange?.(option.value);
  }

  function clearSelection(): void {
    value = '';
    query = '';
    isOpen = false;
    highlightedIndex = -1;
    onchange?.('');
    inputEl?.focus();
  }

  function handleInputFocus(): void {
    if (disabled) return;
    isOpen = true;
    updateDropdownPosition();
    // If there's a selection, show all options (user can type to filter)
    if (selectedOption) {
      query = '';
    }
  }

  function handleInputChange(e: Event): void {
    const target = e.target as HTMLInputElement;
    query = target.value;
    isOpen = true;
    updateDropdownPosition();
    highlightedIndex = -1;
    // Clear selection when user starts typing (they're searching for a new one)
    if (value) {
      value = '';
      onchange?.('');
    }
    // Notify parent for server-side search
    onsearch?.(query);
  }

  function handleKeydown(e: KeyboardEvent): void {
    if (!isOpen) {
      if (e.key === 'ArrowDown' || e.key === 'ArrowUp') {
        e.preventDefault();
        isOpen = true;
      }
      return;
    }

    switch (e.key) {
      case 'ArrowDown':
        e.preventDefault();
        highlightedIndex = Math.min(highlightedIndex + 1, filteredOptions.length - 1);
        scrollToHighlighted();
        break;
      case 'ArrowUp':
        e.preventDefault();
        highlightedIndex = Math.max(highlightedIndex - 1, 0);
        scrollToHighlighted();
        break;
      case 'Enter':
        e.preventDefault();
        if (highlightedIndex >= 0 && highlightedIndex < filteredOptions.length) {
          const option = filteredOptions[highlightedIndex];
          if (option) selectOption(option);
        }
        break;
      case 'Escape':
        e.preventDefault();
        isOpen = false;
        highlightedIndex = -1;
        break;
    }
  }

  function scrollToHighlighted(): void {
    if (!listEl || highlightedIndex < 0) return;
    const item = listEl.children[highlightedIndex] as HTMLElement | undefined;
    item?.scrollIntoView({ block: 'nearest' });
  }

  function updateDropdownPosition(): void {
    if (!containerEl) return;
    const wrapper = containerEl.querySelector('.relative');
    if (!wrapper) return;
    const rect = wrapper.getBoundingClientRect();
    const left = rect.left;
    const width = rect.width;
    const dropdownMaxH = 192; // 12rem
    const spaceBelow = window.innerHeight - rect.bottom - 10;

    if (spaceBelow >= 80) {
      // Open downward — clamp max-height to available space
      const maxH = Math.min(dropdownMaxH, spaceBelow);
      dropdownStyle = `position:fixed;top:${rect.bottom + 4}px;left:${left}px;width:${width}px;z-index:50;max-height:${maxH}px;`;
    } else {
      // Fallback: open upward only when <80px below
      const availableAbove = rect.top - 10;
      const maxH = Math.min(dropdownMaxH, availableAbove);
      dropdownStyle = `position:fixed;bottom:${window.innerHeight - rect.top + 4}px;left:${left}px;width:${width}px;z-index:50;max-height:${maxH}px;`;
    }
  }

  function handleClickOutside(e: MouseEvent): void {
    const target = e.target as Node;
    if (containerEl && !containerEl.contains(target) && (!listEl || !listEl.contains(target))) {
      isOpen = false;
      highlightedIndex = -1;
    }
  }

  $effect(() => {
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  });
</script>

<div class="w-full {className}" bind:this={containerEl}>
  {#if label}
    <label for={inputId} class="block text-xs text-[var(--color-text-muted)] mb-1">
      {label}
      {#if required}
        <span class="text-red-500">*</span>
      {/if}
    </label>
  {/if}

  <div class="relative">
    <!-- Input / Selected display -->
    <div
      class="flex items-center gap-1 w-full rounded-lg border transition-colors
        {error
          ? 'border-[var(--color-error-500)] bg-[var(--color-error-50)] dark:bg-[var(--color-error-900)]/10 focus-within:border-[var(--color-error-500)]'
          : 'border-[var(--color-border-secondary)] bg-[var(--color-bg-primary)] focus-within:border-[var(--color-accent-primary)]'}
        {disabled ? 'bg-[var(--color-bg-tertiary)] cursor-not-allowed' : ''}
        {sizeClasses[size]}"
    >
      {#if selectedOption && !isOpen}
        <!-- Selected state: show label + clear button -->
        <button
          type="button"
          class="flex-1 text-left text-[var(--color-text-primary)] truncate"
          onclick={() => { if (!disabled) handleInputFocus(); }}
          {disabled}
        >
          {selectedOption.label}
        </button>
        {#if !disabled}
          <button
            type="button"
            class="flex-shrink-0 p-0.5 rounded hover:bg-[var(--color-bg-tertiary)] text-[var(--color-text-muted)] hover:text-[var(--color-text-primary)] transition-colors"
            onclick={(e) => { e.stopPropagation(); clearSelection(); }}
            aria-label="Clear selection"
          >
            <svg class="w-3.5 h-3.5" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
              <line x1="18" y1="6" x2="6" y2="18"></line>
              <line x1="6" y1="6" x2="18" y2="18"></line>
            </svg>
          </button>
        {/if}
      {:else}
        <!-- Search state: show input -->
        <input
          bind:this={inputEl}
          id={inputId}
          type="text"
          value={query}
          oninput={handleInputChange}
          onfocus={handleInputFocus}
          onkeydown={handleKeydown}
          {placeholder}
          {disabled}
          autocomplete="off"
          role="combobox"
          aria-expanded={isOpen}
          aria-controls="{inputId}-listbox"
          aria-autocomplete="list"
          aria-activedescendant={highlightedIndex >= 0 ? `${inputId}-option-${highlightedIndex}` : undefined}
          class="flex-1 bg-transparent outline-none text-[var(--color-text-primary)] placeholder:text-[var(--color-text-muted)] min-w-0"
        />
        <svg class="w-4 h-4 flex-shrink-0 text-[var(--color-text-muted)]" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
          <circle cx="11" cy="11" r="8"></circle>
          <line x1="21" y1="21" x2="16.65" y2="16.65"></line>
        </svg>
      {/if}
    </div>

    <!-- Dropdown -->
    {#if isOpen}
      <ul
        bind:this={listEl}
        id="{inputId}-listbox"
        role="listbox"
        style={dropdownStyle}
        class="overflow-y-auto rounded-lg border border-[var(--color-border-secondary)] bg-[var(--color-surface-100)] shadow-lg"
      >
        {#if loading}
          <li class="px-3 py-2 text-sm text-[var(--color-text-muted)]">
            Searching...
          </li>
        {:else if filteredOptions.length === 0}
          <li class="px-3 py-2 text-sm text-[var(--color-text-muted)]">
            {onsearch && query.trim() === '' ? 'Type to search...' : 'No results found'}
          </li>
        {:else}
          {#each filteredOptions as option, i (option.value)}
            <li
              id="{inputId}-option-{i}"
              role="option"
              aria-selected={option.value === value}
              class="px-3 py-2 cursor-pointer transition-colors
                {i === highlightedIndex
                  ? 'bg-[var(--color-accent-primary)]/10 text-[var(--color-text-primary)]'
                  : 'text-[var(--color-text-primary)] hover:bg-[var(--color-bg-tertiary)]'}"
              onmousedown={(e) => { e.preventDefault(); selectOption(option); }}
              onmouseenter={() => { highlightedIndex = i; }}
            >
              <div class="text-sm truncate">{option.label}</div>
              {#if option.description}
                <div class="text-xs text-[var(--color-text-muted)] truncate">{option.description}</div>
              {/if}
            </li>
          {/each}
        {/if}
      </ul>
    {/if}
  </div>

  {#if error}
    <p id="{inputId}-error" class="mt-1 text-sm text-[var(--color-error-600)]">
      {error}
    </p>
  {:else if hint}
    <p class="text-xs text-[var(--color-text-muted)] mt-1">
      {hint}
    </p>
  {/if}
</div>
