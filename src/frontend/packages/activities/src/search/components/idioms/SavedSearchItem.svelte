<script lang="ts">
  /**
   * Saved Search Item
   * Display a saved search with actions
   */

  import { Button, IconButton } from '@sddp/ui';
  import type { SavedSearch } from '../../types';

  interface Props {
    search: SavedSearch;
    onSelect?: (search: SavedSearch) => void;
    onDelete?: (id: string) => void;
  }

  let { search, onSelect, onDelete }: Props = $props();

  function handleSelect() {
    if (onSelect) {
      onSelect(search);
    }
  }

  function handleDelete(event: MouseEvent) {
    event.stopPropagation();
    if (onDelete) {
      onDelete(search.id);
    }
  }
</script>

<Button variant="unstyled" class="saved-search-item" onclick={handleSelect} aria-label={search.name}>
  <span class="search-icon">⭐</span>
  <span class="search-name">{search.name}</span>
  <IconButton icon="trash" variant="danger" onclick={handleDelete} title="Delete saved search" size="sm" />
</Button>

<style>
  :global(.saved-search-item) {
    width: 100%;
    text-align: left;
    display: flex;
    align-items: center;
    gap: 0.5rem;
    padding: 0.5rem;
    cursor: pointer;
    font-size: 0.875rem;
    border-radius: 4px;
  }

  :global(.saved-search-item:hover) {
    background: var(--hover-bg);
  }

  .search-icon {
    font-size: 0.875rem;
  }

  .search-name {
    flex: 1;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
  }

  :global(.saved-search-item button) {
    display: none;
  }

  :global(.saved-search-item:hover button) {
    display: inline-flex;
  }
</style>
