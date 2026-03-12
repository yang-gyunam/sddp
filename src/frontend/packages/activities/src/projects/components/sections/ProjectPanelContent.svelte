<!-- Section: ProjectPanelContent -->
<!--
  ProjectPanelContent Component
  Tree navigation content for a single project panel (no CollapsiblePanel wrapper).
  Used inside SidebarPanels where the panel header is managed externally.
-->
<script lang="ts">
  import { Icon, Button } from '@sddp/ui';
  import type { Project, ProjectSidebarBadges, ProjectTreeNode, ProjectPageType } from '../../types';
  import { generateProjectTreeNodes } from '../../types';

  interface Props {
    project: Project;
    badges?: ProjectSidebarBadges;
    selectedNodePath?: string | null;
    onNodeSelect?: (projectId: string, nodeType: ProjectPageType) => void;
  }

  let {
    project,
    badges,
    selectedNodePath = null,
    onNodeSelect,
  }: Props = $props();

  const treeNodes = $derived(generateProjectTreeNodes(project, badges));

  function handleNodeClick(node: ProjectTreeNode) {
    onNodeSelect?.(project.id, node.type);
  }

</script>

<div class="project-tree">
  {#each treeNodes as node (node.id)}
    {@const isSelected = selectedNodePath === node.path}
    <Button
      variant="unstyled"
      class="
        flex items-center gap-2 w-full text-left px-2 py-1.5 cursor-pointer border rounded transition-colors
        {isSelected
          ? 'border-[var(--color-accent-primary)]/30 bg-[var(--color-accent-primary)]/10 text-[var(--color-text-primary)]'
          : 'border-transparent hover:bg-[var(--color-bg-tertiary)] text-[var(--color-text-secondary)]'}
      "
      onclick={() => handleNodeClick(node)}
    >
      <Icon
        name={node.icon}
        size="sm"
        class={isSelected ? 'text-[var(--color-accent-primary)]' : 'text-[var(--color-text-tertiary)]'}
      />
      <span class="flex-1 text-sm truncate">
        {node.label}
      </span>
      {#if node.badge !== undefined && node.badge > 0}
        <span
          class="
            px-1.5 py-0.5 text-[0.625rem] rounded-full min-w-[18px] text-center
            {isSelected
              ? 'bg-[var(--color-accent-primary)] text-white'
              : 'bg-[var(--color-bg-tertiary)] text-[var(--color-text-secondary)]'}
          "
        >
          {node.badge}
        </span>
      {/if}
    </Button>
  {/each}
</div>
