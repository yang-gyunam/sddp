<script lang="ts">
  /**
   * GlossarySuggestionList Component
   * Dropdown UI for glossary term autocomplete in TipTap editor
   */
  import { Icon, Button } from '@sddp/ui';
  import type { GlossaryTermSummary } from '../../../glossary/types';
  import { TERM_CATEGORY_STYLES } from '../../../glossary/types';

  interface Props {
    items: GlossaryTermSummary[];
    selectedIndex?: number;
    onSelect?: (item: GlossaryTermSummary) => void;
  }

  let { items, selectedIndex = 0, onSelect }: Props = $props();

  function handleSelect(item: GlossaryTermSummary) {
    onSelect?.(item);
  }

  function handleKeyDown(event: KeyboardEvent, item: GlossaryTermSummary) {
    if (event.key === 'Enter' || event.key === ' ') {
      event.preventDefault();
      handleSelect(item);
    }
  }

  function getCategoryStyle(category: GlossaryTermSummary['category']) {
    return TERM_CATEGORY_STYLES[category] || TERM_CATEGORY_STYLES.Technical;
  }
</script>

{#if items.length > 0}
  <div
    class="bg-[var(--color-bg-primary)] border border-[var(--color-border-primary)] rounded-lg shadow-lg max-h-64 overflow-auto"
  >
    {#each items as item, index (item.term)}
      <Button
        variant="unstyled"
        class="w-full text-left px-3 py-2 flex items-center gap-2 transition-colors
          {index === selectedIndex
          ? 'bg-[var(--color-accent-primary)]/10'
          : 'hover:bg-[var(--color-bg-hover)]'}"
        onclick={() => handleSelect(item)}
        onkeydown={(e: KeyboardEvent) => handleKeyDown(e, item)}
      >
        <!-- Category Badge -->
        <span
          class="text-xs px-1.5 py-0.5 rounded {getCategoryStyle(item.category)
            .bgColor} {getCategoryStyle(item.category).color}"
        >
          {item.category}
        </span>

        <!-- Term Name -->
        <span class="font-medium text-[var(--color-text-primary)]">
          {item.term}
        </span>

        <!-- Abbreviation -->
        {#if item.abbreviation}
          <span class="text-[var(--color-text-muted)] text-sm">
            ({item.abbreviation})
          </span>
        {/if}
      </Button>
    {/each}
  </div>
{:else}
  <div
    class="bg-[var(--color-bg-primary)] border border-[var(--color-border-primary)] rounded-lg shadow-lg p-3"
  >
    <div class="flex items-center gap-2 text-[var(--color-text-muted)] text-sm">
      <Icon name="search" size="sm" />
      <span>No matching terms found</span>
    </div>
  </div>
{/if}
