<!-- Section: BacklogView — Tasks > My Tasks -->
<script lang="ts">
  import { Button, Icon, Spinner } from '@sddp/ui';
  import { Avatar, PriorityBadge } from '@sddp/shell';
  import { DonutChart, ProgressBar } from '../../../dashboard/components/idioms';
  import { StatCard } from '../../../shared/components/idioms';
  import type { BacklogStats, TaskSummary } from '../../types';
  import { TASK_STATUS_STYLES, TASK_PRIORITY_STYLES } from '../../types';

  interface Props {
    stats: BacklogStats | null;
    tasks?: TaskSummary[];
    loading?: boolean;
    selectedTaskId?: string | null;
    onSelectTask?: (taskId: string) => void;
    class?: string;
  }

  let {
    stats,
    tasks = [],
    loading = false,
    selectedTaskId = null,
    onSelectTask,
    class: className = '',
  }: Props = $props();

  // Priority chart data
  const priorityChartData = $derived(
    stats?.priorityDistribution.map((d) => ({
      label: TASK_PRIORITY_STYLES[d.priority]?.label ?? d.priority,
      value: d.count,
      color: d.priority === 'High'
        ? 'var(--color-error-500)'
        : d.priority === 'Medium'
          ? 'var(--color-warning-500)'
          : 'var(--color-neutral-500)',
    })) ?? []
  );

  // Completion rate (used in ProgressBar via stats.doneCount / stats.totalTasks)

</script>

<div class="flex flex-col h-full bg-[var(--color-bg-primary)] {className}">
  {#if loading}
    <div class="flex-1 flex items-center justify-center">
      <div class="text-center">
        <Spinner size="lg" />
        <p class="mt-2 text-sm text-[var(--color-text-tertiary)]">Loading backlog...</p>
      </div>
    </div>
  {:else if stats}
    <!-- Header -->
    <div class="flex-shrink-0 p-4 border-b border-[var(--color-border-primary)]">
      <div class="flex items-center justify-between">
        <div class="flex items-center gap-2">
          <Icon
            name={stats.isOwner ? 'shield' : 'folder'}
            size="md"
            class={stats.isOwner ? 'text-[var(--color-warning-500)]' : 'text-[var(--color-text-tertiary)]'}
          />
          <h1 class="text-lg font-semibold text-[var(--color-text-primary)]">
            {stats.projectName}
          </h1>
        </div>
        <span class="text-sm text-[var(--color-text-tertiary)]">
          Total: {stats.totalTasks} tasks
        </span>
      </div>
    </div>

    <!-- Content -->
    <div class="flex-1 overflow-y-auto p-4 space-y-6">
      <!-- Status Cards -->
      <div class="grid grid-cols-5 gap-3">
        <StatCard
          title="Backlog"
          value={stats.backlogCount}
          icon="archive"
        />
        <StatCard
          title="To Do"
          value={stats.toDoCount}
          icon="circle"
        />
        <StatCard
          title="In Progress"
          value={stats.inProgressCount}
          icon="loader"
        />
        <StatCard
          title="Done"
          value={stats.doneCount}
          icon="check-circle"
        />
        <StatCard
          title="Blocked"
          value={stats.blockedCount}
          icon="x-circle"
        />
      </div>

      <!-- Charts Row -->
      <div class="grid grid-cols-2 gap-4">
        <!-- Priority Distribution -->
        <DonutChart
          data={priorityChartData}
          title="Priority Distribution"
          size={120}
          strokeWidth={20}
        />

        <!-- Completion & Effort -->
        <div class="p-4 rounded-lg border border-[var(--color-border)] bg-[var(--color-bg-secondary)] space-y-4">
          <h3 class="text-sm font-semibold text-[var(--color-text-primary)]">Progress</h3>

          <div>
            <div class="text-xs text-[var(--color-text-tertiary)] mb-1">Completion</div>
            <ProgressBar value={stats.doneCount} max={stats.totalTasks} label="Done" />
          </div>

          <div>
            <div class="text-xs text-[var(--color-text-tertiary)] mb-1">Effort (hours)</div>
            <ProgressBar
              value={stats.totalActualHours}
              max={stats.totalEstimatedHours || 1}
              label="{stats.totalActualHours}h / {stats.totalEstimatedHours}h"
            />
          </div>
        </div>
      </div>

      <!-- Team Workload (Owner only) -->
      {#if stats.isOwner && stats.assigneeStats.length > 0}
        <div class="rounded-lg border border-[var(--color-border)] bg-[var(--color-bg-secondary)]">
          <div class="px-4 py-3 border-b border-[var(--color-border)]">
            <h3 class="text-sm font-semibold text-[var(--color-text-primary)] flex items-center gap-2">
              <Icon name="user" size="sm" />
              Team Workload
            </h3>
          </div>
          <div class="divide-y divide-[var(--color-border)]">
            {#each stats.assigneeStats as assignee (assignee.assignee.id)}
              <div class="flex items-center gap-3 px-4 py-2.5">
                <Avatar name={assignee.assignee.name ?? ''} size="xs" />
                <span class="flex-1 min-w-0 text-sm text-[var(--color-text-primary)] truncate">
                  {assignee.assignee.name}
                </span>
                <div class="flex items-center gap-2 text-xs">
                  {#if assignee.backlogCount > 0}
                    <span class="text-[var(--color-neutral-500)]" title="Backlog">{assignee.backlogCount}</span>
                  {/if}
                  <span class="text-[var(--color-text-tertiary)]" title="To Do">{assignee.toDoCount}</span>
                  <span class="text-[var(--color-info-500)]" title="In Progress">{assignee.inProgressCount}</span>
                  <span class="text-[var(--color-success-500)]" title="Done">{assignee.doneCount}</span>
                  {#if assignee.blockedCount > 0}
                    <span class="text-[var(--color-error-500)]" title="Blocked">{assignee.blockedCount}</span>
                  {/if}
                  <span class="font-medium text-[var(--color-text-primary)] ml-1">
                    = {assignee.totalCount}
                  </span>
                </div>
              </div>
            {/each}
          </div>
        </div>
      {/if}

      <!-- All Tasks Table -->
      {#if tasks.length > 0}
        <div class="rounded-lg border border-[var(--color-border)] bg-[var(--color-bg-secondary)]">
          <div class="px-4 py-3 border-b border-[var(--color-border)]">
            <h3 class="text-sm font-semibold text-[var(--color-text-primary)] flex items-center gap-2">
              <Icon name="list" size="sm" />
              All Tasks ({tasks.length})
            </h3>
          </div>
          <div class="divide-y divide-[var(--color-border)]">
            {#each tasks as task (task.id)}
              <Button
                variant="unstyled"
                class="w-full text-left px-4 py-2.5 flex items-center gap-3 transition-colors
                  {selectedTaskId === task.id
                    ? 'bg-[var(--color-accent-primary)]/10'
                    : 'hover:bg-[var(--color-bg-tertiary)]'}"
                onclick={() => onSelectTask?.(task.id)}
              >
                <!-- Status icon -->
                <Icon
                  name={TASK_STATUS_STYLES[task.status]?.icon ?? 'circle'}
                  size="sm"
                  class={TASK_STATUS_STYLES[task.status]?.color ?? ''}
                />

                <!-- Title -->
                <span class="flex-1 min-w-0 text-sm text-[var(--color-text-primary)] truncate">
                  {task.title}
                </span>

                <!-- Priority -->
                <PriorityBadge priority={task.priority} />

                <!-- Assignee -->
                {#if task.assignee?.name}
                  <span class="text-xs text-[var(--color-text-tertiary)] truncate max-w-[80px]">
                    {task.assignee.name}
                  </span>
                {/if}

                <!-- Effort -->
                {#if task.estimatedHours > 0}
                  <span class="text-xs text-[var(--color-text-tertiary)] flex-shrink-0">
                    {task.actualHours}/{task.estimatedHours}h
                  </span>
                {/if}
              </Button>
            {/each}
          </div>
        </div>
      {/if}
    </div>
  {:else}
    <!-- Empty State -->
    <div class="flex-1 flex items-center justify-center">
      <div class="text-center">
        <Icon name="inbox" size="xl" class="mx-auto mb-3 text-[var(--color-text-tertiary)] opacity-50" />
        <p class="text-sm font-medium text-[var(--color-text-primary)]">Select a project</p>
        <p class="text-xs text-[var(--color-text-secondary)] mt-1">
          Choose a project from the sidebar to view backlog statistics.
        </p>
      </div>
    </div>
  {/if}
</div>
