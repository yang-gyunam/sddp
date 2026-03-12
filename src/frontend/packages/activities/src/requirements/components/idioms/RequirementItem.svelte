<!--
  RequirementItem Component
  Single requirement item in the sidebar list
-->
<script lang="ts">
  import { Icon, IconButton } from '@sddp/ui';
  import { ListItem } from '@sddp/shell';
  import type { RequirementSummary } from '../../types';
  import { REQUIREMENT_LEVEL_STYLES, REQUIREMENT_STATUS_STYLES } from '../../types';

  interface Props {
    requirement: RequirementSummary;
    selected?: boolean;
    expanded?: boolean;
    depth?: number;
    onSelect?: (id: string) => void;
    onToggleExpand?: (id: string) => void;
    class?: string;
  }

  let {
    requirement,
    selected = false,
    expanded = false,
    depth = 0,
    onSelect,
    onToggleExpand,
    class: className = '',
  }: Props = $props();

  const levelStyle = $derived(REQUIREMENT_LEVEL_STYLES[requirement.level]);
  const statusStyle = $derived(REQUIREMENT_STATUS_STYLES[requirement.status]);
  const paddingLeft = $derived(12 + depth * 16);

  function handleClick() {
    onSelect?.(requirement.id);
  }

  function handleToggleExpand(e: MouseEvent) {
    e.stopPropagation();
    onToggleExpand?.(requirement.id);
  }
</script>

<ListItem {selected} onclick={handleClick} role="treeitem"
  aria-selected={selected}
  aria-expanded={requirement.hasChildren ? expanded : undefined}
  style="padding-left: {paddingLeft}px"
  class="group {className}">
  <!-- Expand/Collapse Toggle -->
  {#if requirement.hasChildren}
    <IconButton
      icon={expanded ? 'chevron-down' : 'chevron-right'}
      size="sm"
      variant="ghost"
      onclick={handleToggleExpand}
      title={expanded ? 'Collapse' : 'Expand'}
    />
  {:else}
    <span class="w-5"></span>
  {/if}

  <!-- Level Badge -->
  <span
    class="flex-shrink-0 w-5 h-5 flex items-center justify-center text-xs font-semibold rounded
           {levelStyle.bgColor} {levelStyle.textColor}"
    title={levelStyle.label}
  >
    {requirement.level}
  </span>

  <!-- Requirement Info -->
  <div class="flex-1 min-w-0 flex items-center gap-2">
    <span class="text-xs text-[var(--vscode-descriptionForeground)] font-mono">
      {requirement.code}
    </span>
    <span class="truncate text-sm">
      {requirement.title}
    </span>
  </div>

  <!-- Status Icon -->
  <Icon
    name={statusStyle.icon}
    size="xs"
    class="flex-shrink-0 {statusStyle.textColor}"
    title={statusStyle.label}
  />

  <!-- Children Count -->
  {#if requirement.childrenCount > 0}
    <span
      class="flex-shrink-0 text-xs text-[var(--vscode-descriptionForeground)]
             bg-[var(--vscode-badge-background)] px-1.5 rounded-full"
    >
      {requirement.childrenCount}
    </span>
  {/if}
</ListItem>
