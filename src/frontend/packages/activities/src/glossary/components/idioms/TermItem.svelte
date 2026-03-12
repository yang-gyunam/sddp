<!--
  TermItem Component
  Individual glossary term item in sidebar
-->
<script lang="ts">
  import { Icon } from '@sddp/ui';
  import { ListItem } from '@sddp/shell';
  import type { TermSummaryItem } from '../../types';
  import { TERM_STATUS_STYLES } from '../../types/glossary.types';

  interface Props {
    term: TermSummaryItem;
    selected?: boolean;
    onSelect?: (termId: string) => void;
    class?: string;
  }

  let { term, selected = false, onSelect, class: className = '' }: Props = $props();

  const statusStyle = $derived(TERM_STATUS_STYLES[term.status]);

  function handleClick() {
    onSelect?.(term.id);
  }
</script>

<ListItem {selected} onclick={handleClick} role="treeitem"
  aria-selected={selected} class="group {className}">
  <div class="flex items-center gap-2 w-full min-w-0">
    <Icon
      name={statusStyle.icon}
      size="xs"
      class="flex-shrink-0 {statusStyle.color}"
      title={statusStyle.label}
    />
    <span class="flex-1 truncate text-sm text-[var(--color-text-primary)]">
      {term.term}
    </span>
    <span class="flex-shrink-0 text-xs text-[var(--color-text-tertiary)] opacity-70">
      v{term.version}
    </span>
  </div>
</ListItem>
