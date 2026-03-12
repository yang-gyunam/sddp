<!--
  ProjectSpecGroup Component
  Collapsible project section containing spec items
  Uses CollapsibleGroup from shell for consistent behavior and animation
-->
<script lang="ts">
  import { CollapsibleGroup } from '@sddp/shell';
  import type { ProjectSpecGroup } from '../../types';
  import SpecItem from './SpecItem.svelte';

  interface Props {
    group: ProjectSpecGroup;
    selectedSpecId?: string | null;
    onToggleExpand?: () => void;
    onSelectSpec?: (id: string) => void;
    class?: string;
  }

  let {
    group,
    selectedSpecId = null,
    onToggleExpand,
    onSelectSpec,
    class: className = '',
  }: Props = $props();
</script>

<CollapsibleGroup
  title={group.projectName}
  subtitle={group.projectCode}
  icon="file-text"
  badge={group.totalCount}
  expanded={group.expanded}
  onToggle={onToggleExpand}
  class={className}
>
  <div class="py-1" role="group" aria-label="{group.projectName} specs">
    {#each group.specs as spec (spec.id)}
      <SpecItem
        {spec}
        selected={selectedSpecId === spec.id}
        onSelect={onSelectSpec}
      />
    {/each}

    {#if group.specs.length === 0}
      <div class="px-4 py-3 text-sm text-[var(--color-text-tertiary)] italic">
        No specs match filter
      </div>
    {/if}
  </div>
</CollapsibleGroup>
