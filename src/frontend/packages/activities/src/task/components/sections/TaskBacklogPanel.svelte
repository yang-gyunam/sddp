<!-- Section: TaskBacklogPanel -->
<!--
  TaskBacklogPanel Component
  Backlog project list content for the "Backlog" sidebar panel.
  Used inside SidebarPanels where the panel header is managed externally.
-->
<script lang="ts">
  import { Button, Icon } from '@sddp/ui';
  import type { BacklogSummary } from '../../types';

  interface Props {
    backlogSummary?: BacklogSummary | null;
    selectedProjectId?: string | null;
    onProjectSelect?: (projectId: string) => void;
  }

  let {
    backlogSummary = null,
    selectedProjectId = null,
    onProjectSelect,
  }: Props = $props();
</script>

<div>
  {#if !backlogSummary || backlogSummary.projects.length === 0}
    <div class="flex flex-col items-center justify-center py-6 text-[var(--color-text-tertiary)]">
      <Icon name="inbox" size="lg" class="mb-2 opacity-50" />
      <p class="text-xs">No projects found</p>
    </div>
  {:else}
    <div>
      {#each backlogSummary.projects as project (project.projectId)}
        {@const isSelected = selectedProjectId === project.projectId}
        <Button
          variant="unstyled"
          class="w-full text-left flex items-center gap-2 px-2 py-1.5 rounded cursor-pointer border
            transition-colors duration-150
            {isSelected
              ? 'bg-[var(--color-accent-primary)]/10 border-[var(--color-accent-primary)]/30'
              : 'border-transparent hover:bg-[var(--color-bg-tertiary)]'}"
          onclick={() => onProjectSelect?.(project.projectId)}
        >
          <Icon
            name={project.isOwner ? 'shield' : 'folder'}
            size="sm"
            class={isSelected ? 'text-[var(--color-accent-primary)]' : 'text-[var(--color-text-tertiary)]'}
          />
          <span class="flex-1 min-w-0 text-sm truncate text-[var(--color-text-primary)]">
            {project.projectName}
          </span>
          <span class="flex-shrink-0 text-xs leading-none px-1 rounded-full bg-[var(--color-bg-tertiary)] text-[var(--color-text-tertiary)]">
            {project.activeTaskCount}
          </span>
        </Button>
      {/each}
    </div>
  {/if}
</div>
