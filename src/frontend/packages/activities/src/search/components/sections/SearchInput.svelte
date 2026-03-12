<!-- Section: SearchInput — Search > Global/Tab -->
<script lang="ts">
  /**
   * Search Input
   * Main search input with advanced search button.
   * Uses SearchField for debounce, clear, Enter/Escape support.
   */

  import { Button, SearchField } from '@sddp/ui';

  interface Props {
    value?: string;
    placeholder?: string;
    onSearch?: (text: string) => void;
    onAdvanced?: () => void;
  }

  let {
    value = $bindable(''),
    placeholder = 'Search across all projects...',
    onSearch,
    onAdvanced,
  }: Props = $props();

  let searchFieldRef: { focus: () => void } | undefined = $state();

  function handleSearch(text: string) {
    onSearch?.(text);
  }

  function handleAdvanced() {
    onAdvanced?.();
  }

  export function focus() {
    searchFieldRef?.focus();
  }
</script>

<div class="flex gap-2 p-4 border-b border-[var(--color-border-primary)]">
  <SearchField
    bind:value
    bind:this={searchFieldRef}
    {placeholder}
    onSearch={handleSearch}
    class="flex-1"
  />
  <Button variant="ghost" onclick={handleAdvanced}>Advanced</Button>
</div>
