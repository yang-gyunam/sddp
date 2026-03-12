<script lang="ts">
  import type { GlossaryTermSuggestion, TermCategory, GlossaryTermStatus } from '../../types';
  import { TERM_CATEGORIES, TERM_CATEGORY_STYLES } from '../../types';
  import { Button, Icon, IconButton, Input, Select, Spinner } from '@sddp/ui';

  interface Props {
    value?: string;
    suggestions?: GlossaryTermSuggestion[];
    loadingSuggestions?: boolean;
    categoryFilter?: TermCategory | null;
    statusFilter?: GlossaryTermStatus | null;
    onSearch?: (query: string) => void;
    onQueryChange?: (query: string) => void;
    onSelectSuggestion?: (suggestion: GlossaryTermSuggestion) => void;
    onCategoryChange?: (category: TermCategory | null) => void;
    onStatusChange?: (status: GlossaryTermStatus | null) => void;
    class?: string;
  }

  let {
    value = '',
    suggestions = [],
    loadingSuggestions = false,
    categoryFilter = null,
    statusFilter = null,
    onSearch,
    onQueryChange,
    onSelectSuggestion,
    onCategoryChange,
    onStatusChange,
    class: className = '',
  }: Props = $props();

  let showSuggestions = $state(false);
  let inputRef = $state<HTMLInputElement | undefined>(undefined);

  const categories: (TermCategory | null)[] = [null, ...TERM_CATEGORIES];

  function handleInput(e: Event) {
    const target = e.target as HTMLInputElement;
    onQueryChange?.(target.value);
    showSuggestions = target.value.length > 0;
  }

  function handleKeydown(e: KeyboardEvent) {
    if (e.key === 'Enter') {
      e.preventDefault();
      onSearch?.(value);
      showSuggestions = false;
    } else if (e.key === 'Escape') {
      showSuggestions = false;
    }
  }

  function handleSelectSuggestion(suggestion: GlossaryTermSuggestion) {
    onSelectSuggestion?.(suggestion);
    showSuggestions = false;
  }

  function handleFocus() {
    if (value.length > 0) {
      showSuggestions = true;
    }
  }

  function handleBlur() {
    // Delay to allow click on suggestion
    setTimeout(() => {
      showSuggestions = false;
    }, 200);
  }

  function handleClear() {
    onQueryChange?.('');
    showSuggestions = false;
    inputRef?.focus();
  }
</script>

<div class="flex flex-col gap-3 {className}">
  <!-- Search Input -->
  <div class="relative">
    <div class="relative">
      <Icon
        name="search"
        size="sm"
        class="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400"
      />
      <Input
        unstyled
        bind:element={inputRef}
        {value}
        placeholder="Search terms..."
        class="w-full pl-9 pr-8 py-2 text-sm border border-gray-200 dark:border-gray-700 rounded-lg
          bg-white dark:bg-gray-800 text-gray-700 dark:text-gray-300
          focus:outline-none focus:border-blue-500"
        oninput={handleInput}
        onkeydown={handleKeydown}
        onfocus={handleFocus}
        onblur={handleBlur}
      />
      {#if value}
        <IconButton
          icon="x"
          size="sm"
          variant="ghost"
          class="absolute right-2 top-1/2 -translate-y-1/2 text-gray-400 hover:text-gray-600"
          title="Clear"
          onclick={handleClear}
        />
      {/if}
    </div>

    <!-- Suggestions Dropdown -->
    {#if showSuggestions && (suggestions.length > 0 || loadingSuggestions)}
      <div class="absolute z-50 w-full mt-1 py-1 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg shadow-lg max-h-64 overflow-y-auto">
        {#if loadingSuggestions}
          <div class="flex items-center gap-2 px-3 py-2 text-sm text-gray-500">
            <Spinner size="sm" />
            Searching...
          </div>
        {:else}
          {#each suggestions as suggestion (suggestion.id)}
            {@const style = TERM_CATEGORY_STYLES[suggestion.category]}
            <Button
              variant="unstyled"
              class="w-full flex items-start gap-3 px-3 py-2 hover:bg-gray-50 dark:hover:bg-gray-750 text-left"
              onclick={() => handleSelectSuggestion(suggestion)}
            >
              <div class="flex-1 min-w-0">
                <div class="flex items-center gap-2">
                  <span class="font-medium text-gray-800 dark:text-gray-200">
                    {suggestion.term}
                  </span>
                  {#if suggestion.abbreviation}
                    <span class="text-xs text-gray-500">
                      ({suggestion.abbreviation})
                    </span>
                  {/if}
                </div>
                <p class="text-xs text-gray-500 truncate mt-0.5">
                  {suggestion.definition}
                </p>
              </div>
              <span class="flex-shrink-0 px-1.5 py-0.5 text-xs rounded {style.bgColor} {style.color}">
                {style.label}
              </span>
            </Button>
          {/each}
        {/if}
      </div>
    {/if}
  </div>

  <!-- Filters -->
  <div class="flex items-center gap-4">
    <!-- Category Filter -->
    <div class="flex items-center gap-2">
      <label for="category-filter" class="text-xs font-medium text-gray-500 dark:text-gray-400">
        Category:
      </label>
      <Select
        unstyled
        id="category-filter"
        value={categoryFilter ?? ''}
        class="text-sm px-2 py-1 border border-gray-200 dark:border-gray-700 rounded
          bg-white dark:bg-gray-800 text-gray-700 dark:text-gray-300"
        onchange={(value) => {
          onCategoryChange?.(value ? value as TermCategory : null);
        }}
      >
        <option value="">All</option>
        {#each categories.slice(1) as cat (cat)}
          {#if cat}
            <option value={cat}>{TERM_CATEGORY_STYLES[cat].label}</option>
          {/if}
        {/each}
      </Select>
    </div>

    <!-- Status Filter -->
    <div class="flex items-center gap-2">
      <label for="status-filter" class="text-xs font-medium text-gray-500 dark:text-gray-400">
        Status:
      </label>
      <Select
        unstyled
        id="status-filter"
        value={statusFilter ?? ''}
        class="text-sm px-2 py-1 border border-gray-200 dark:border-gray-700 rounded
          bg-white dark:bg-gray-800 text-gray-700 dark:text-gray-300"
        onchange={(value) => {
          onStatusChange?.(value ? value as GlossaryTermStatus : null);
        }}
      >
        <option value="">All</option>
        <option value="Draft">Draft</option>
        <option value="Active">Active</option>
        <option value="Deprecated">Deprecated</option>
      </Select>
    </div>
  </div>
</div>
