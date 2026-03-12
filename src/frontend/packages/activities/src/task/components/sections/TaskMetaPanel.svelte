<!-- Section: TaskMetaPanel — Tasks > My Tasks/Category/Backlog, Projects > Tasks -->
<script lang="ts">
  import { Button, Checkbox, Icon, IconButton, Spinner } from '@sddp/ui';
  import DetailHeader from '../../../shared/components/idioms/DetailHeader.svelte';
  import DetailTitle from '../../../shared/components/idioms/DetailTitle.svelte';
  import { Dropdown, formatDate as formatDateUtil, formatNumber, formatPercent } from '@sddp/shell';
  import type { Task } from '../../types';
  import { calculateProgress } from '../../types';
  import TaskStatusBadge from '../idioms/TaskStatusBadge.svelte';
  import TaskPriorityBadge from '../idioms/TaskPriorityBadge.svelte';
  import { TraceGraphSection } from '../../../relationship/components/sections';

  interface Props {
    task: Task | null;
    projectId?: string;
    categoryName?: string;
    loading?: boolean;
    onEdit?: () => void;
    onLogTime?: () => void;
    onDelete?: () => void;
    onClose?: () => void;
    class?: string;
  }

  let {
    task,
    projectId = '',
    categoryName = '',
    loading = false,
    onEdit,
    onLogTime,
    onDelete,
    onClose,
    class: className = '',
  }: Props = $props();

  let showTimeLogs = $state(false);

  const progress = $derived(task ? calculateProgress(task.actualHours, task.estimatedHours) : 0);
  const progressLabel = $derived(
    formatPercent(Math.min(progress, 100) / 100, { maximumFractionDigits: 0 })
  );
  const remaining = $derived(task ? Math.max(task.estimatedHours - task.actualHours, 0) : 0);
  const isOverdue = $derived(task ? task.actualHours > task.estimatedHours : false);

  function formatDateStr(dateStr: string): string {
    return formatDateUtil(dateStr, { month: 'short', year: undefined });
  }
</script>

<div class="flex flex-col h-full bg-[var(--color-bg-secondary)] border-l border-[var(--color-border-primary)] {className}">
  {#if loading}
    <div class="flex-1 flex items-center justify-center">
      <Spinner size="lg" />
    </div>
  {:else if !task}
    <div class="flex items-center justify-center h-full">
      <div class="flex flex-col items-center gap-3 text-[var(--color-text-tertiary)]">
        <Icon name="clipboard-list" size="xl" class="opacity-50" />
        <p class="text-sm">Select a task to view details</p>
      </div>
    </div>
  {:else}
    <!-- Header -->
    <DetailHeader>
      {#snippet leading()}
        <Icon name="clipboard-list" size="md" class="text-[var(--color-text-tertiary)]" />
      {/snippet}
      <DetailTitle title={task.title} code={`#${task.id.slice(-4)}`} />
      {#snippet actions()}
        {#if onEdit}
          <IconButton icon="edit" variant="ghost" size="sm" title="Edit" onclick={onEdit} />
        {/if}
        {#if onClose}
          <IconButton icon="x" variant="ghost" size="sm" title="Close" onclick={onClose} />
        {/if}
        {#if onDelete}
          <Dropdown position="bottom-right">
            {#snippet trigger()}
              <IconButton icon="more-vertical" variant="ghost" size="sm" title="More actions" />
            {/snippet}
            <div class="py-1 min-w-[160px]">
              <Button
                variant="unstyled"
                onclick={onDelete}
                class="w-full text-left px-3 py-1.5 text-xs font-medium hover:bg-[var(--color-surface-200)] transition-colors text-[var(--color-error-600)] flex items-center gap-2"
              >
                <Icon name="trash" size="xs" />
                Delete
              </Button>
            </div>
          </Dropdown>
        {/if}
      {/snippet}
    </DetailHeader>

    <!-- Content -->
    <div class="flex-1 overflow-y-auto">
      <!-- Properties -->
      <div class="px-4 py-3 border-b border-[var(--color-border-primary)]">
        <div class="grid grid-cols-2 gap-x-4 gap-y-2">
          <div class="col-span-2">
            <span class="text-xs text-[var(--color-text-tertiary)]">Description</span>
            {#if task.description}
              <p class="text-sm text-[var(--color-text-secondary)] whitespace-pre-wrap mt-0.5">{task.description}</p>
            {:else}
              <p class="text-xs text-[var(--color-text-tertiary)] italic mt-0.5">No description</p>
            {/if}
          </div>
          <div class="flex items-center justify-between">
            <span class="text-xs text-[var(--color-text-tertiary)]">Status</span>
            <TaskStatusBadge status={task.status} />
          </div>
          <div class="flex items-center justify-between">
            <span class="text-xs text-[var(--color-text-tertiary)]">Priority</span>
            <TaskPriorityBadge priority={task.priority} showLabel />
          </div>
          <div class="flex items-center justify-between">
            <span class="text-xs text-[var(--color-text-tertiary)]">Assignee</span>
            <span class="text-sm text-[var(--color-text-secondary)]">@{task.assignee?.name || task.assignee?.id || 'Unassigned'}</span>
          </div>
          {#if categoryName}
            <div class="flex items-center justify-between">
              <span class="text-xs text-[var(--color-text-tertiary)]">Category</span>
              <span class="text-sm text-[var(--color-text-secondary)] flex items-center gap-1">
                <Icon name="tag" size="xs" class="text-[var(--color-text-tertiary)]" />
                {categoryName}
              </span>
            </div>
          {:else}
            <div></div>
          {/if}
          <div class="flex items-center justify-between">
            <span class="text-xs text-[var(--color-text-tertiary)]">Created by</span>
            <span class="text-sm text-[var(--color-text-secondary)]">{task.createdBy?.name || '—'}</span>
          </div>
          <div class="flex items-center justify-between">
            <span class="text-xs text-[var(--color-text-tertiary)]">Created</span>
            <span class="text-sm text-[var(--color-text-secondary)]">{formatDateStr(task.createdAt)}</span>
          </div>
          <div class="flex items-center justify-between">
            <span class="text-xs text-[var(--color-text-tertiary)]">Updated by</span>
            <span class="text-sm text-[var(--color-text-secondary)]">{task.updatedBy?.name || '—'}</span>
          </div>
          <div class="flex items-center justify-between">
            <span class="text-xs text-[var(--color-text-tertiary)]">Updated</span>
            <span class="text-sm text-[var(--color-text-secondary)]">{formatDateStr(task.updatedAt)}</span>
          </div>
        </div>
      </div>

      <!-- Effort Tracking -->
      <div class="p-4 border-b border-[var(--color-border-primary)]">
        <div class="flex items-center justify-between mb-3">
          <h4 class="text-xs font-semibold text-[var(--color-text-tertiary)] uppercase tracking-wider flex items-center gap-1">
            <Icon name="clock" size="sm" />
            Effort Tracking
          </h4>
          {#if onLogTime}
            <IconButton icon="plus" variant="ghost" size="sm" title="Add Effort" onclick={onLogTime} />
          {/if}
        </div>

        <div class="grid grid-cols-3 gap-2 text-center mb-3">
          <div class="p-2 bg-[var(--color-bg-primary)] rounded-lg">
            <div class="text-lg font-bold text-[var(--color-text-primary)]">
              {formatNumber(task.estimatedHours)}h
            </div>
            <div class="text-xs text-[var(--color-text-tertiary)]">Estimated</div>
          </div>
          <Button
            variant="unstyled"
            class="p-2 bg-[var(--color-bg-primary)] rounded-lg cursor-pointer hover:ring-1 hover:ring-[var(--color-accent-primary)] transition-all"
            title="Click to view time log details"
            onclick={() => showTimeLogs = !showTimeLogs}
          >
            <div class="text-lg font-bold {isOverdue ? 'text-red-500' : 'text-[var(--color-text-primary)]'}">
              {formatNumber(task.actualHours)}h
            </div>
            <div class="text-xs text-[var(--color-text-tertiary)] flex items-center justify-center gap-1">
              Actual
              {#if task.timeLogs.length > 0}
                <Icon name={showTimeLogs ? 'chevron-up' : 'chevron-down'} size="xs" />
              {/if}
            </div>
          </Button>
          <div class="p-2 bg-[var(--color-bg-primary)] rounded-lg">
            <div class="text-lg font-bold text-[var(--color-text-primary)]">
              {formatNumber(remaining)}h
            </div>
            <div class="text-xs text-[var(--color-text-tertiary)]">Remaining</div>
          </div>
        </div>

        <!-- Progress bar -->
        <div class="h-2 bg-[var(--color-bg-tertiary)] rounded-full overflow-hidden">
          <div
            class="h-full transition-all duration-300
              {isOverdue ? 'bg-red-500' : progress >= 100 ? 'bg-green-500' : 'bg-[var(--color-accent-primary)]'}"
            style="width: {Math.min(progress, 100)}%"
          ></div>
        </div>
        <div class="text-xs text-center text-[var(--color-text-tertiary)] mt-1">
          {progressLabel} complete
        </div>

        <!-- Time Log Details (toggle) -->
        {#if showTimeLogs && task.timeLogs.length > 0}
          <div class="mt-3 border border-[var(--color-border-primary)] rounded-lg overflow-hidden">
            <div class="text-xs font-semibold text-[var(--color-text-tertiary)] uppercase tracking-wider px-3 py-2 bg-[var(--color-bg-tertiary)]">
              Time Logs ({task.timeLogs.length})
            </div>
            <div class="divide-y divide-[var(--color-border-primary)]">
              {#each task.timeLogs as log (log.id)}
                <div class="px-3 py-2 text-sm">
                  <div class="flex items-center justify-between">
                    <span class="font-medium text-[var(--color-text-primary)]">{formatNumber(log.hours)}h</span>
                    <span class="text-xs text-[var(--color-text-tertiary)]">{formatDateStr(log.date)}</span>
                  </div>
                  {#if log.description}
                    <p class="text-xs text-[var(--color-text-secondary)] mt-0.5 line-clamp-2">{log.description}</p>
                  {/if}
                  {#if log.user?.name}
                    <span class="text-xs text-[var(--color-text-tertiary)]">@{log.user.name}</span>
                  {/if}
                </div>
              {/each}
            </div>
          </div>
        {/if}
      </div>

      <!-- Acceptance Criteria -->
      {#if task.acceptanceCriteria.length > 0}
        <div class="p-4 border-b border-[var(--color-border-primary)]">
          <h4 class="text-xs font-semibold text-[var(--color-text-tertiary)] uppercase tracking-wider mb-3 flex items-center gap-1">
            <Icon name="check-square" size="sm" />
            Acceptance Criteria
          </h4>
          <div class="space-y-2">
            {#each task.acceptanceCriteria as criterion (criterion.id)}
              <label class="flex items-start gap-2 text-sm">
                <Checkbox
                  unstyled
                  checked={criterion.completed}
                  class="mt-0.5 rounded border-[var(--color-border-secondary)]"
                  disabled
                />
                <span class="text-[var(--color-text-secondary)] {criterion.completed ? 'line-through opacity-60' : ''}">
                  {criterion.description}
                </span>
              </label>
            {/each}
          </div>
        </div>
      {/if}

      <!-- Trace Graph (Related Items) -->
      <TraceGraphSection
        entityType="Task"
        entityId={task.id}
        entityCode={`Task #${task.id.slice(-4)}`}
        {projectId}
        hidePrimaryFlow
        hideFilter
      />
    </div>
  {/if}
</div>
