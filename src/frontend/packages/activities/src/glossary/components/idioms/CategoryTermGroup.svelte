<!--
  CategoryTermGroup Component
  Collapsible group of terms by category
  Uses CollapsibleGroup from shell for consistent behavior and animation
-->
<script lang="ts">
  import { CollapsibleGroup } from '@sddp/shell';
  import type { CategoryTermGroup } from '../../types';
  import { TERM_CATEGORY_STYLES } from '../../types';
  import TermItem from './TermItem.svelte';

  interface Props {
    group: CategoryTermGroup;
    selectedTermId?: string | null;
    onToggle?: () => void;
    onSelectTerm?: (termId: string) => void;
    class?: string;
  }

  let {
    group,
    selectedTermId = null,
    onToggle,
    onSelectTerm,
    class: className = '',
  }: Props = $props();

  const categoryStyle = $derived(TERM_CATEGORY_STYLES[group.category]);
</script>

<CollapsibleGroup
  title={group.category}
  icon={categoryStyle?.icon ?? 'folder'}
  iconClass={categoryStyle?.color ?? ''}
  badge={group.terms.length}
  expanded={group.expanded}
  {onToggle}
  class={className}
>
  <div role="group" aria-label="{group.category} terms">
    {#each group.terms as term (term.id)}
      <TermItem
        {term}
        selected={selectedTermId === term.id}
        onSelect={onSelectTerm}
      />
    {/each}

    {#if group.terms.length === 0}
      <div class="px-4 py-3 text-sm text-[var(--color-text-tertiary)] italic">
        No terms match filter
      </div>
    {/if}
  </div>
</CollapsibleGroup>
