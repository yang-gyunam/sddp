<!--
  ArtifactTypeGroup Component
  Collapsible group of artifacts by type
  Uses CollapsibleGroup from shell for consistent behavior and animation
-->
<script lang="ts">
  import { CollapsibleGroup } from '@sddp/shell';
  import type { ArtifactTypeGroup } from '../../types';
  import { ARTIFACT_TYPE_STYLES } from '../../types';
  import ArtifactItem from './ArtifactItem.svelte';

  interface Props {
    group: ArtifactTypeGroup;
    selectedArtifactId?: string | null;
    onToggle?: () => void;
    onSelectArtifact?: (artifactId: string) => void;
    class?: string;
  }

  let {
    group,
    selectedArtifactId = null,
    onToggle,
    onSelectArtifact,
    class: className = '',
  }: Props = $props();

  const typeStyle = $derived(ARTIFACT_TYPE_STYLES[group.type]);
</script>

<CollapsibleGroup
  title={typeStyle.label}
  icon={typeStyle.icon}
  iconClass={typeStyle.color}
  badge={group.artifacts.length}
  expanded={group.expanded}
  {onToggle}
  class={className}
>
  <div role="group" aria-label="{typeStyle.label} artifacts">
    {#each group.artifacts as artifact (artifact.id)}
      <ArtifactItem
        {artifact}
        selected={selectedArtifactId === artifact.id}
        onSelect={onSelectArtifact}
      />
    {/each}

    {#if group.artifacts.length === 0}
      <div class="px-4 py-3 text-sm text-[var(--color-text-tertiary)] italic">
        No artifacts match filter
      </div>
    {/if}
  </div>
</CollapsibleGroup>
