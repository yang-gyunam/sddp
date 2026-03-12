<!--
  ArtifactItem Component
  Individual artifact item in sidebar
-->
<script lang="ts">
  import { Icon } from '@sddp/ui';
  import { ListItem } from '@sddp/shell';
  import type { ArtifactSummary } from '../../types';
  import { ARTIFACT_STATUS_STYLES, getFileName } from '../../types';

  interface Props {
    artifact: ArtifactSummary;
    selected?: boolean;
    onSelect?: (artifactId: string) => void;
    class?: string;
  }

  let { artifact, selected = false, onSelect, class: className = '' }: Props = $props();

  const statusStyle = $derived(ARTIFACT_STATUS_STYLES[artifact.status] ?? ARTIFACT_STATUS_STYLES.Unverified);
  const fileName = $derived(getFileName(artifact.artifactPath));

  function handleClick() {
    onSelect?.(artifact.id);
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
      {fileName}
    </span>
    <span class="flex-shrink-0 text-xs text-[var(--color-text-tertiary)] opacity-70">
      {artifact.entityName}
    </span>
  </div>
</ListItem>
