<!-- Section: TimeLogPanel — Tasks > My Tasks/Category/Backlog, Projects > Tasks -->
<script lang="ts">
  import { Button, Icon, IconButton, Input, Textarea } from '@sddp/ui';
  import { formatDateWithOptions, toLocalDateString } from '@sddp/shell';
  import type { Task } from '../../types';
  import { calculateProgress } from '../../types';

  interface Props {
    task: Task;
    onLogTime?: (data: TimeLogFormData) => void;
    onCancel?: () => void;
    class?: string;
  }

  export interface TimeLogFormData {
    taskId: string;
    date: string;
    hours: number;
    description: string;
  }

  let {
    task,
    onLogTime,
    onCancel,
    class: className = '',
  }: Props = $props();

  const formId = `time-log-${Math.random().toString(36).substring(2, 9)}`;
  const dateId = `${formId}-date`;
  const hoursId = `${formId}-hours`;
  const descriptionId = `${formId}-description`;

  // Form state
  let date = $state(toLocalDateString(new Date()));
  let hours = $state(1);
  let description = $state('');

  const isValid = $derived(hours > 0 && date.length > 0);

  const progress = $derived(calculateProgress(task.actualHours, task.estimatedHours));
  const remaining = $derived(Math.max(task.estimatedHours - task.actualHours, 0));
  const isOverdue = $derived(task.actualHours > task.estimatedHours);

  // Projected values after logging
  const projectedActual = $derived(task.actualHours + hours);
  const projectedProgress = $derived(calculateProgress(projectedActual, task.estimatedHours));
  const projectedRemaining = $derived(Math.max(task.estimatedHours - projectedActual, 0));
  const willBeOverdue = $derived(projectedActual > task.estimatedHours);

  function handleSubmit() {
    if (!isValid) return;

    onLogTime?.({
      taskId: task.id,
      date,
      hours,
      description: description.trim(),
    });
  }

  function handleKeydown(event: KeyboardEvent) {
    if (event.key === 'Escape') {
      onCancel?.();
    }
  }

  function formatDate(dateStr: string): string {
    return formatDateWithOptions(dateStr, { locale: 'en-US', month: 'short', day: 'numeric' });
  }
</script>

<!-- svelte-ignore a11y_no_static_element_interactions -->
<div
  class="flex flex-col h-full bg-[var(--color-bg-secondary)] border-l border-[var(--color-border-primary)] {className}"
  onkeydown={handleKeydown}
>
  <!-- Header -->
  <div class="flex-shrink-0 flex items-center justify-between min-h-12 px-4 border-b border-[var(--color-border-primary)]">
    <div class="flex items-center gap-2 min-w-0">
      <Icon name="clock" size="sm" class="text-[var(--color-text-tertiary)]" />
      <span class="text-sm font-medium text-[var(--color-text-primary)] truncate">Log Time</span>
      <span class="text-xs text-[var(--color-text-tertiary)] truncate">{task.title}</span>
    </div>
    <div class="flex items-center gap-1 flex-shrink-0">
      <IconButton icon="check" variant="success" size="sm" title="Log {hours}h" onclick={handleSubmit} disabled={!isValid} />
      <IconButton icon="x" variant="ghost" size="sm" title="Cancel" onclick={() => onCancel?.()} />
    </div>
  </div>

  <!-- Form -->
  <div class="flex-1 overflow-y-auto p-3 space-y-3">
    <!-- Current Effort Summary -->
    <div class="p-3 bg-[var(--color-bg-primary)] rounded-lg border border-[var(--color-border-primary)]">
      <h4 class="text-xs font-semibold text-[var(--color-text-tertiary)] uppercase tracking-wider mb-2">
        Current Effort
      </h4>
      <div class="grid grid-cols-3 gap-2 text-center text-sm">
        <div>
          <div class="font-semibold text-[var(--color-text-primary)]">{task.estimatedHours}h</div>
          <div class="text-xs text-[var(--color-text-tertiary)]">Estimated</div>
        </div>
        <div>
          <div class="font-semibold {isOverdue ? 'text-red-500' : 'text-[var(--color-text-primary)]'}">{task.actualHours}h</div>
          <div class="text-xs text-[var(--color-text-tertiary)]">Actual</div>
        </div>
        <div>
          <div class="font-semibold text-[var(--color-text-primary)]">{remaining}h</div>
          <div class="text-xs text-[var(--color-text-tertiary)]">Remaining</div>
        </div>
      </div>
      <div class="mt-2 h-1.5 bg-[var(--color-bg-tertiary)] rounded-full overflow-hidden">
        <div
          class="h-full transition-all {isOverdue ? 'bg-red-500' : 'bg-[var(--color-accent-primary)]'}"
          style="width: {Math.min(progress, 100)}%"
        ></div>
      </div>
    </div>

    <!-- Date -->
    <div>
      <label for={dateId} class="block text-sm font-medium text-[var(--color-text-secondary)] mb-1">
        Date <span class="text-red-500">*</span>
      </label>
      <Input
        type="date"
        id={dateId}
        bind:value={date}
        class="w-full"
      />
    </div>

    <!-- Hours -->
    <div>
      <label for={hoursId} class="block text-sm font-medium text-[var(--color-text-secondary)] mb-1">
        Hours <span class="text-red-500">*</span>
      </label>
      <div class="flex items-center gap-2">
        <Input
          type="number"
          id={hoursId}
          bind:value={hours}
          min="0.5"
          step="0.5"
          class="w-24"
        />
        <span class="text-sm text-[var(--color-text-tertiary)]">hours</span>
      </div>
      <div class="flex gap-2 mt-2">
        {#each [0.5, 1, 2, 4, 8] as preset (preset)}
          <Button
            variant="unstyled"
            class="px-2 py-1 text-xs rounded border transition-colors
              {hours === preset
                ? 'bg-[var(--color-accent-primary)]/10 border-[var(--color-accent-primary)]/30 text-[var(--color-accent-primary)]'
                : 'border-[var(--color-border-primary)] text-[var(--color-text-tertiary)] hover:bg-[var(--color-bg-tertiary)]'}"
            onclick={() => hours = preset}
          >
            {preset}h
          </Button>
        {/each}
      </div>
    </div>

    <!-- Description -->
    <div>
      <label for={descriptionId} class="block text-sm font-medium text-[var(--color-text-secondary)] mb-1">
        Description
      </label>
      <Textarea
        id={descriptionId}
        bind:value={description}
        placeholder="What did you work on?"
        rows={2}
        resize="none"
      />
    </div>

    <!-- Projected Values -->
    {#if hours > 0}
      <div class="p-3 bg-[var(--color-bg-tertiary)] rounded-lg border border-dashed border-[var(--color-border-secondary)]">
        <h4 class="text-xs font-semibold text-[var(--color-text-tertiary)] uppercase tracking-wider mb-2">
          After Logging {hours}h
        </h4>
        <div class="grid grid-cols-3 gap-2 text-center text-sm">
          <div>
            <div class="font-semibold text-[var(--color-text-primary)]">{task.estimatedHours}h</div>
            <div class="text-xs text-[var(--color-text-tertiary)]">Estimated</div>
          </div>
          <div>
            <div class="font-semibold {willBeOverdue ? 'text-red-500' : 'text-[var(--color-text-primary)]'}">
              {projectedActual}h
            </div>
            <div class="text-xs text-[var(--color-text-tertiary)]">Actual</div>
          </div>
          <div>
            <div class="font-semibold {willBeOverdue ? 'text-red-500' : 'text-[var(--color-text-primary)]'}">
              {projectedRemaining}h
            </div>
            <div class="text-xs text-[var(--color-text-tertiary)]">Remaining</div>
          </div>
        </div>
        <div class="mt-2 h-1.5 bg-[var(--color-bg-secondary)] rounded-full overflow-hidden">
          <div
            class="h-full transition-all {willBeOverdue ? 'bg-red-500' : 'bg-green-500'}"
            style="width: {Math.min(projectedProgress, 100)}%"
          ></div>
        </div>
        {#if willBeOverdue && !isOverdue}
          <p class="text-xs text-red-500 mt-2 flex items-center gap-1">
            <Icon name="alert-triangle" size="sm" />
            This will exceed the estimated hours
          </p>
        {/if}
      </div>
    {/if}

    <!-- Recent Time Logs -->
    {#if task.timeLogs.length > 0}
      <div>
        <h4 class="text-xs font-semibold text-[var(--color-text-tertiary)] uppercase tracking-wider mb-2">
          Recent Logs
        </h4>
        <div class="space-y-2">
          {#each task.timeLogs.slice(0, 5) as log (log.id)}
            <div class="flex items-center gap-2 text-sm p-2 bg-[var(--color-bg-primary)] rounded-lg">
              <div class="flex-1 min-w-0">
                <div class="text-[var(--color-text-secondary)] truncate">
                  {log.description || 'No description'}
                </div>
                <div class="text-xs text-[var(--color-text-tertiary)]">
                  {formatDate(log.date)} · {log.user?.name || 'Unknown'}
                </div>
              </div>
              <span class="text-[var(--color-text-primary)] font-medium">
                {log.hours}h
              </span>
            </div>
          {/each}
        </div>
      </div>
    {/if}
  </div>

</div>
