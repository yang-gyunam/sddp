<script lang="ts">
  /**
   * Filter Section
   * Collapsible filter section with checkbox options
   */

  import { Button, Checkbox } from '@sddp/ui';

  interface Props {
    title: string;
    expanded?: boolean;
    options?: Array<{ value: string; label: string; count?: number }>;
    selected?: string[];
    onToggle?: (value: string) => void;
    children?: import('svelte').Snippet;
  }

  let { title, expanded = $bindable(true), options = [], selected = [], onToggle, children }: Props = $props();

  function handleToggle(value: string) {
    if (onToggle) {
      onToggle(value);
    }
  }

  function toggleExpanded() {
    expanded = !expanded;
  }
</script>

<div class="filter-section">
  <Button variant="unstyled" class="filter-header" onclick={toggleExpanded}>
    <span class="expand-icon">{expanded ? '▼' : '▶'}</span>
    <span class="filter-title">{title}</span>
  </Button>

  {#if expanded}
    <fieldset class="contents">
      <legend class="sr-only">{title}</legend>
      <div class="filter-options">
        {#each options as option (option.value)}
          <label class="filter-option">
            <Checkbox
              unstyled
              checked={selected.includes(option.value)}
              onchange={() => handleToggle(option.value)}
            />
            <span class="option-label">{option.label}</span>
            {#if option.count !== undefined}
              <span class="option-count">({option.count})</span>
            {/if}
          </label>
        {/each}
        {#if children}
          {@render children()}
        {/if}
      </div>
    </fieldset>
  {/if}
</div>

<style>
  .filter-section {
    margin-bottom: 1rem;
  }

  :global(.filter-header) {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    width: 100%;
    padding: 0.5rem;
    background: transparent;
    border: none;
    cursor: pointer;
    font-weight: 600;
    font-size: 0.75rem;
    text-transform: uppercase;
    letter-spacing: 0.05em;
    color: var(--text-secondary);
  }

  :global(.filter-header:hover) {
    background: var(--hover-bg);
  }

  .expand-icon {
    font-size: 0.625rem;
  }

  .filter-title {
    flex: 1;
    text-align: left;
  }

  .filter-options {
    padding: 0.25rem 0.5rem;
  }

  .filter-option {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    padding: 0.375rem 0.5rem;
    cursor: pointer;
    font-size: 0.875rem;
  }

  .filter-option:hover {
    background: var(--hover-bg);
  }

  .option-label {
    flex: 1;
  }

  .option-count {
    color: var(--text-tertiary);
    font-size: 0.75rem;
  }

</style>
