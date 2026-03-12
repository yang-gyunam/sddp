<!--
  ProjectTaskGroup Component
  Collapsible group of tasks by project
  Uses CollapsibleGroup from shell for consistent behavior and animation
-->
<script lang="ts">
  import { Icon } from '@sddp/ui';
  import { CollapsibleGroup } from '@sddp/shell';
  import type { ProjectTaskGroup } from '../../types';
  import TaskItem from './TaskItem.svelte';

  interface Props {
    group: ProjectTaskGroup;
    selectedTaskId?: string | null;
    onToggle?: () => void;
    onSelectTask?: (taskId: string) => void;
    class?: string;
  }

  let {
    group,
    selectedTaskId = null,
    onToggle,
    onSelectTask,
    class: className = '',
  }: Props = $props();

  const activeCount = $derived(group.stats.toDoCount + group.stats.inProgressCount);
</script>

<CollapsibleGroup
  title={group.projectName}
  icon="folder"
  badge={activeCount}
  badgeClass="text-[var(--color-accent-primary)] bg-[var(--color-accent-primary)]/10"
  expanded={group.expanded}
  {onToggle}
  class={className}
>
  <div class="py-1" role="group" aria-label="{group.projectName} tasks">
    <!-- Status summary -->
    <div class="flex items-center gap-3 px-3 py-1 text-xs text-[var(--color-text-tertiary)]">
      <span class="flex items-center gap-1">
        <Icon name="circle" size="xs" />
        To Do: {group.stats.toDoCount}
      </span>
      <span class="flex items-center gap-1 text-[var(--color-accent-primary)]">
        <Icon name="loader" size="xs" />
        In Progress: {group.stats.inProgressCount}
      </span>
      <span class="flex items-center gap-1 text-[var(--color-success-500)]">
        <Icon name="check-circle" size="xs" />
        Done: {group.stats.doneCount}
      </span>
    </div>

    <!-- Task items -->
    {#each group.tasks as task (task.id)}
      <TaskItem
        {task}
        selected={selectedTaskId === task.id}
        onSelect={onSelectTask}
      />
    {/each}

    {#if group.tasks.length === 0}
      <div class="px-4 py-3 text-sm text-[var(--color-text-tertiary)] italic">
        No tasks match filter
      </div>
    {/if}
  </div>
</CollapsibleGroup>
