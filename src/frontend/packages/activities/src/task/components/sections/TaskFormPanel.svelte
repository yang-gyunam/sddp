<!-- Section: TaskFormPanel — Tasks > My Tasks/Category/Backlog, Projects > Tasks -->
<script lang="ts">
  import { Button, Icon, IconButton, Input, Combobox, Textarea } from '@sddp/ui';
  import type { ComboboxOption } from '@sddp/ui';
  import type { Task, TaskStatus, TaskPriority } from '../../types';
  import { TASK_STATUS_STYLES, TASK_PRIORITY_STYLES } from '../../types';

  interface Props {
    task?: Task | null;
    mode: 'create' | 'edit';
    projectId?: string;
    projectName?: string;
    defaultStatus?: TaskStatus;
    allowBacklogStatus?: boolean;
    memberCandidates?: ComboboxOption[];
    onSave?: (data: TaskFormData) => void;
    onCancel?: () => void;
    class?: string;
  }

  export interface TaskFormData {
    title: string;
    description: string;
    status: TaskStatus;
    priority: TaskPriority;
    assigneeId: string;
    estimatedHours: number;
    projectId?: string;
  }

  let {
    task = null,
    mode,
    projectId = '',
    projectName = '',
    defaultStatus = 'ToDo',
    allowBacklogStatus = false,
    memberCandidates = [],
    onSave,
    onCancel,
    class: className = '',
  }: Props = $props();

  const formId = `task-form-${Math.random().toString(36).substring(2, 9)}`;
  const titleId = `${formId}-title`;
  const descriptionId = `${formId}-description`;
  const assigneeFieldId = `${formId}-assignee`;
  const estimatedHoursId = `${formId}-estimated-hours`;

  // Form state
  let title = $state('');
  let description = $state('');
  let status = $state<TaskStatus>('ToDo');
  let priority = $state<TaskPriority>('Medium');
  let assigneeId = $state('');
  let estimatedHours = $state(0);

  const statusOptions = $derived.by(() => {
    const base = ['ToDo', 'InProgress', 'Done'] as const;
    const includeBacklog =
      allowBacklogStatus || defaultStatus === 'Backlog' || task?.status === 'Backlog' || status === 'Backlog';
    return includeBacklog ? (['Backlog', ...base] as const) : base;
  });

  // Reset form when task changes
  $effect(() => {
    if (task) {
      title = task.title;
      description = task.description;
      status = task.status;
      priority = task.priority;
      assigneeId = task.assignee?.id ?? '';
      estimatedHours = task.estimatedHours;
    } else {
      title = '';
      description = '';
      status = defaultStatus === 'Backlog' && !allowBacklogStatus ? 'ToDo' : defaultStatus;
      priority = 'Medium';
      assigneeId = '';
      estimatedHours = 0;
    }
  });

  const isValid = $derived(title.trim().length > 0);

  function handleSubmit() {
    if (!isValid) return;

    onSave?.({
      title: title.trim(),
      description: description.trim(),
      status,
      priority,
      assigneeId,
      estimatedHours,
      projectId: task?.projectId || projectId || undefined,
    });
  }

  function handleKeydown(event: KeyboardEvent) {
    if (event.key === 'Escape') {
      onCancel?.();
    }
  }
</script>

<!-- svelte-ignore a11y_no_static_element_interactions -->
<div
  class="flex flex-col h-full bg-[var(--color-bg-secondary)] border-l border-[var(--color-border-primary)] {className}"
  onkeydown={handleKeydown}
>
  <!-- Header -->
  <div class="flex-shrink-0 flex items-center justify-between min-h-12 px-4 bg-[var(--color-bg-primary)] border-b border-[var(--color-border-primary)]">
    <div class="flex items-center gap-2 min-w-0">
      <Icon name="check-square" size="sm" class="text-[var(--color-text-tertiary)]" />
      <span class="text-sm font-medium text-[var(--color-text-primary)] truncate">
        {mode === 'create' ? 'New Task' : 'Edit Task'}
      </span>
      {#if projectName}
        <span class="text-xs text-[var(--color-text-tertiary)] truncate">{projectName}</span>
      {/if}
    </div>
    <div class="flex items-center gap-1 flex-shrink-0">
      <IconButton icon="check" variant="success" size="sm" title={mode === 'create' ? 'Create' : 'Save'} onclick={handleSubmit} disabled={!isValid} />
      <IconButton icon="x" variant="ghost" size="sm" title="Cancel" onclick={() => onCancel?.()} />
    </div>
  </div>

  <!-- Form -->
  <div class="flex-1 overflow-y-auto p-3 space-y-3">
    <!-- Title -->
    <div>
      <label for={titleId} class="block text-sm font-medium text-[var(--color-text-secondary)] mb-1">
        Title <span class="text-red-500">*</span>
      </label>
      <Input
        type="text"
        id={titleId}
        bind:value={title}
        placeholder="Enter task title"
        class="w-full"
      />
    </div>

    <!-- Description -->
    <div>
      <label for={descriptionId} class="block text-sm font-medium text-[var(--color-text-secondary)] mb-1">
        Description
      </label>
      <Textarea
        id={descriptionId}
        bind:value={description}
        placeholder="Enter task description"
        rows={3}
        resize="none"
      />
    </div>

    <!-- Status -->
    <div>
      <span class="block text-sm font-medium text-[var(--color-text-secondary)] mb-2">
        Status
      </span>
      <div class="flex flex-wrap gap-2">
        {#each statusOptions as s (s)}
          {@const style = TASK_STATUS_STYLES[s]}
          <Button
            variant="unstyled"
            class="flex items-center gap-1.5 px-3 py-1.5 text-sm rounded-lg border transition-colors
              {status === s
                ? `${style.bgColor} ${style.color} ${style.borderColor}`
                : 'border-[var(--color-border-primary)] bg-[var(--color-bg-primary)] text-[var(--color-text-secondary)] hover:bg-[var(--color-bg-tertiary)]'}"
            onclick={() => status = s}
          >
            <Icon name={style.icon} size="sm" />
            {style.label}
          </Button>
        {/each}
      </div>
    </div>

    <!-- Priority -->
    <div>
      <span class="block text-sm font-medium text-[var(--color-text-secondary)] mb-2">
        Priority
      </span>
      <div class="flex flex-wrap gap-2">
        {#each (['High', 'Medium', 'Low'] as const) as p (p)}
          {@const style = TASK_PRIORITY_STYLES[p]}
          <Button
            variant="unstyled"
            class="flex items-center gap-1.5 px-3 py-1.5 text-sm rounded-lg border transition-colors
              {priority === p
                ? `${style.bgColor} ${style.color} ${style.borderColor}`
                : 'border-[var(--color-border-primary)] bg-[var(--color-bg-primary)] text-[var(--color-text-secondary)] hover:bg-[var(--color-bg-tertiary)]'}"
            onclick={() => priority = p}
          >
            <Icon name={style.icon} size="sm" />
            {style.label}
          </Button>
        {/each}
      </div>
    </div>

    <!-- Assignee -->
    <div>
      <Combobox
        label="Assignee"
        id={assigneeFieldId}
        options={memberCandidates}
        bind:value={assigneeId}
        placeholder="Search by name..."
        hint="Person responsible for this task"
      />
    </div>

    <!-- Estimated Hours -->
    <div>
      <label for={estimatedHoursId} class="block text-sm font-medium text-[var(--color-text-secondary)] mb-1">
        Estimated Hours
      </label>
      <div class="flex items-center gap-2">
        <Input
          type="number"
          id={estimatedHoursId}
          bind:value={estimatedHours}
          min="0"
          step="0.5"
          class="w-24"
        />
        <span class="text-sm text-[var(--color-text-tertiary)]">hours</span>
      </div>
    </div>
  </div>

</div>
