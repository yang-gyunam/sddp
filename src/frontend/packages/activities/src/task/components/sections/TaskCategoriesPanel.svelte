<!-- Section: TaskCategoriesPanel -->
<!--
  TaskCategoriesPanel Component
  Category list content for the "My Tasks" sidebar panel.
  Used inside SidebarPanels where the panel header is managed externally.
-->
<script lang="ts">
  import { Button, Icon } from '@sddp/ui';
  import type { TaskCategory } from '../../types';
  import InlineCategoryInput from '../idioms/InlineCategoryInput.svelte';

  interface Props {
    categories: TaskCategory[];
    selectedCategoryId?: string | null;
    showInput?: boolean;
    onCategorySelect?: (category: TaskCategory) => void;
    onCategoryCreate?: (name: string) => void;
    onCancelInput?: () => void;
  }

  let {
    categories,
    selectedCategoryId = null,
    showInput = false,
    onCategorySelect,
    onCategoryCreate,
    onCancelInput,
  }: Props = $props();
</script>

<div>
  {#if showInput}
    <InlineCategoryInput
      onSubmit={(name) => onCategoryCreate?.(name)}
      onCancel={() => onCancelInput?.()}
    />
  {/if}
  {#if categories.length > 0}
    <div>
      {#each categories as category (category.id)}
        {@const isSelected = selectedCategoryId === category.id}
        <Button
          variant="unstyled"
          class="w-full flex items-center gap-2 px-2 py-1.5 rounded cursor-pointer border
            transition-colors duration-150
            {isSelected
              ? 'bg-[var(--color-accent-primary)]/10 border-[var(--color-accent-primary)]/30'
              : 'border-transparent hover:bg-[var(--color-bg-tertiary)]'}"
          onclick={() => onCategorySelect?.(category)}
        >
          <Icon
            name="tag"
            size="sm"
            class={isSelected ? 'text-[var(--color-accent-primary)]' : 'text-[var(--color-text-tertiary)]'}
          />
          <span class="flex-1 text-left text-sm truncate text-[var(--color-text-primary)]">
            {category.name}
          </span>
        </Button>
      {/each}
    </div>
  {:else if !showInput}
    <div class="flex flex-col items-center justify-center py-6 text-[var(--color-text-tertiary)]">
      <Icon name="tag" size="lg" class="mb-2 opacity-50" />
      <p class="text-xs">No categories yet</p>
      <p class="text-xs mt-1 opacity-70">Click + to create one</p>
    </div>
  {/if}
</div>
