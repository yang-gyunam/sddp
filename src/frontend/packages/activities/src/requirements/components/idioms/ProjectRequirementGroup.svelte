<!--
  ProjectRequirementGroup Component
  Collapsible project section containing requirement items
  Uses CollapsibleGroup from shell for consistent behavior and animation
-->
<script lang="ts">
  import { CollapsibleGroup } from '@sddp/shell';
  import type { ProjectRequirementGroup } from '../../types';
  import RequirementItem from './RequirementItem.svelte';

  interface Props {
    group: ProjectRequirementGroup;
    selectedRequirementId?: string | null;
    expandedRequirements?: Set<string>;
    onToggleExpand?: () => void;
    onSelectRequirement?: (id: string) => void;
    onToggleRequirementExpand?: (id: string) => void;
    class?: string;
  }

  let {
    group,
    selectedRequirementId = null,
    expandedRequirements = new Set(),
    onToggleExpand,
    onSelectRequirement,
    onToggleRequirementExpand,
    class: className = '',
  }: Props = $props();
</script>

<CollapsibleGroup
  title={group.projectName}
  subtitle={group.projectCode}
  icon="clipboard-list"
  badge={group.totalCount}
  expanded={group.expanded}
  onToggle={onToggleExpand}
  class={className}
>
  <div class="py-1" role="group" aria-label="{group.projectName} requirements">
    {#each group.requirements as requirement (requirement.id)}
      <RequirementItem
        {requirement}
        selected={selectedRequirementId === requirement.id}
        expanded={expandedRequirements.has(requirement.id)}
        onSelect={onSelectRequirement}
        onToggleExpand={onToggleRequirementExpand}
      />
    {/each}

    {#if group.requirements.length === 0}
      <div class="px-4 py-3 text-sm text-[var(--color-text-tertiary)] italic">
        No requirements match filter
      </div>
    {/if}
  </div>
</CollapsibleGroup>
