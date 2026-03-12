<!--
  TaskItem Component
  Individual task item for sidebar and lists
-->
<script lang="ts">
  import { Button, Icon } from '@sddp/ui';
  import type { TaskSummary } from '../../types';
  import { TASK_STATUS_STYLES } from '../../types';
  import TaskPriorityBadge from './TaskPriorityBadge.svelte';

  interface Props {
    task: TaskSummary;
    selected?: boolean;
    showProject?: boolean;
    onSelect?: (taskId: string) => void;
    class?: string;
  }

  let {
    task,
    selected = false,
    showProject = false,
    onSelect,
    class: className = '',
  }: Props = $props();

  const statusStyle = $derived(TASK_STATUS_STYLES[task.status]);

  function handleClick() {
    onSelect?.(task.id);
  }
</script>

<Button
  variant="unstyled"
  class="w-full flex items-start gap-2 px-3 py-2 text-left transition-colors
    border-b border-[var(--color-border-primary)]
    {selected
      ? 'bg-[var(--color-accent-primary)]/10 text-[var(--color-text-primary)]'
      : 'hover:bg-[var(--color-bg-tertiary)] text-[var(--color-text-secondary)]'}
    {className}"
  onclick={handleClick}
>
  <!-- Status icon -->
  <span class="mt-0.5 {statusStyle.color}">
    <Icon name={statusStyle.icon} size="sm" />
  </span>

  <!-- Task info -->
  <div class="flex-1 min-w-0">
    <div class="flex items-center gap-1.5">
      <span class="font-medium text-sm truncate">
        {task.title}
      </span>
      <TaskPriorityBadge priority={task.priority} size="sm" />
    </div>

    <div class="flex items-center gap-2 text-xs text-[var(--color-text-tertiary)] mt-0.5">
      <span>@{task.assignee?.name}</span>

      {#if task.linkedItemCount > 0}
        <span class="flex items-center gap-0.5">
          <Icon name="link" size="xs" />
          {task.linkedItemCount}
        </span>
      {/if}

      {#if task.estimatedHours > 0}
        <span class="flex items-center gap-0.5 {task.hasOverdueEffort ? 'text-[var(--color-error-500)]' : ''}">
          <Icon name="clock" size="xs" />
          {task.actualHours}h/{task.estimatedHours}h
        </span>
      {/if}
    </div>

    {#if showProject}
      <div class="text-xs text-[var(--color-text-tertiary)] mt-0.5">
        {task.projectName}
      </div>
    {/if}
  </div>
</Button>
