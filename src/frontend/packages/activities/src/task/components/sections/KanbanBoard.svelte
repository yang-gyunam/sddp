<!-- Section: KanbanBoard — Tasks > My Tasks, Projects > Tasks -->
<script lang="ts">
  import { SvelteSet } from 'svelte/reactivity';
  import { Icon, Button, IconButton, Spinner } from '@sddp/ui';
  import { Dropdown, PriorityBadge } from '@sddp/shell';
  import type { Priority } from '@sddp/shell';
  import { TASK_STATUS_COLORS } from '../../../shared/constants/semanticColors';
  import type { TaskSummary, TaskStatus, TaskPriority } from '../../types';
  import { TASK_STATUS_STYLES } from '../../types';

  interface Props {
    tasks: TaskSummary[];
    projectName?: string;
    selectedTaskId?: string | null;
    loading?: boolean;
    showProjectBadge?: boolean;
    showHeader?: boolean;
    onSelectTask?: (taskId: string) => void;
    onStatusChange?: (taskId: string, newStatus: TaskStatus, targetIndex?: number) => void;
    onNewTask?: (status: TaskStatus) => void;
    class?: string;
  }

  let {
    tasks,
    projectName = 'My Tasks',
    selectedTaskId = null,
    loading = false,
    showProjectBadge = false,
    showHeader = true,
    onSelectTask,
    onStatusChange,
    onNewTask: _onNewTask,
    class: className = '',
  }: Props = $props();

  // Drag state
  let draggedTaskId = $state<string | null>(null);
  let draggedFromStatus = $state<TaskStatus | null>(null);
  let dropTargetStatus = $state<TaskStatus | null>(null);
  let dropTargetIndex = $state<number | null>(null);

  const columns: { status: TaskStatus; label: string; color: string }[] = [
    { status: 'ToDo', label: 'To Do', color: TASK_STATUS_COLORS.ToDo },
    { status: 'InProgress', label: 'In Progress', color: TASK_STATUS_COLORS.InProgress },
    { status: 'Done', label: 'Done', color: TASK_STATUS_COLORS.Done },
  ];

  // ── Column filter & sort ──
  type SortOption = 'default' | 'priority' | 'title' | 'effort';

  interface ColumnFilter {
    priorities: SvelteSet<TaskPriority>;
    sortBy: SortOption;
  }

  const priorityOptions: { value: TaskPriority; label: string; icon: string }[] = [
    { value: 'High', label: 'High', icon: 'arrow-up' },
    { value: 'Medium', label: 'Medium', icon: 'minus' },
    { value: 'Low', label: 'Low', icon: 'arrow-down' },
  ];

  const sortOptions: { value: SortOption; label: string; icon: string }[] = [
    { value: 'default', label: 'Default', icon: 'list' },
    { value: 'priority', label: 'Priority', icon: 'arrow-up-down' },
    { value: 'title', label: 'Title', icon: 'type' },
    { value: 'effort', label: 'Effort', icon: 'clock' },
  ];

  let columnFilters = $state<Record<string, ColumnFilter>>({
    ToDo: { priorities: new SvelteSet(), sortBy: 'default' },
    InProgress: { priorities: new SvelteSet(), sortBy: 'default' },
    Done: { priorities: new SvelteSet(), sortBy: 'default' },
  });

  function togglePriorityFilter(status: TaskStatus, priority: TaskPriority) {
    const filter = columnFilters[status];
    if (!filter) return;
    const next = new SvelteSet(filter.priorities);
    if (next.has(priority)) next.delete(priority);
    else next.add(priority);
    columnFilters = { ...columnFilters, [status]: { ...filter, priorities: next } };
  }

  function setSortOption(status: TaskStatus, sortBy: SortOption) {
    const filter = columnFilters[status];
    if (!filter) return;
    columnFilters = { ...columnFilters, [status]: { ...filter, sortBy } };
  }

  function hasActiveFilter(status: TaskStatus): boolean {
    const f = columnFilters[status];
    if (!f) return false;
    return f.priorities.size > 0 || f.sortBy !== 'default';
  }

  function getTasksByStatus(status: TaskStatus): TaskSummary[] {
    return tasks.filter((t) => t.status === status);
  }

  function getFilteredColumnTasks(status: TaskStatus): TaskSummary[] {
    let result = getTasksByStatus(status);
    const filter = columnFilters[status];
    if (!filter) return result;

    // Priority filter
    if (filter.priorities.size > 0) {
      result = result.filter((t) => filter.priorities.has(t.priority));
    }

    // Sort
    const priorityOrder: Record<TaskPriority, number> = { High: 0, Medium: 1, Low: 2 };
    if (filter.sortBy === 'priority') {
      result = [...result].sort((a, b) => (priorityOrder[a.priority] ?? 1) - (priorityOrder[b.priority] ?? 1));
    } else if (filter.sortBy === 'title') {
      result = [...result].sort((a, b) => a.title.localeCompare(b.title));
    } else if (filter.sortBy === 'effort') {
      result = [...result].sort((a, b) => b.estimatedHours - a.estimatedHours);
    }

    return result;
  }

  function getColumnStyle(status: TaskStatus) {
    return TASK_STATUS_STYLES[status];
  }

  // Drag handlers
  function handleDragStart(event: DragEvent, task: TaskSummary) {
    draggedTaskId = task.id;
    draggedFromStatus = task.status;
    if (event.dataTransfer) {
      event.dataTransfer.effectAllowed = 'move';
      event.dataTransfer.setData('text/plain', task.id);
    }
  }

  function handleDragOver(event: DragEvent, status: TaskStatus) {
    event.preventDefault();
    if (event.dataTransfer) {
      event.dataTransfer.dropEffect = 'move';
    }
    dropTargetStatus = status;
  }

  function handleDragOverCard(event: DragEvent, status: TaskStatus, cardIndex: number, cardElement: HTMLElement) {
    event.preventDefault();
    event.stopPropagation();
    if (event.dataTransfer) {
      event.dataTransfer.dropEffect = 'move';
    }
    dropTargetStatus = status;

    // Calculate if mouse is in top or bottom half of the card
    const rect = cardElement.getBoundingClientRect();
    const mouseY = event.clientY;
    const cardMiddle = rect.top + rect.height / 2;

    if (mouseY < cardMiddle) {
      dropTargetIndex = cardIndex;
    } else {
      dropTargetIndex = cardIndex + 1;
    }
  }

  function handleDragLeave() {
    dropTargetStatus = null;
    dropTargetIndex = null;
  }

  function handleDrop(event: DragEvent, targetStatus: TaskStatus) {
    event.preventDefault();

    if (draggedTaskId) {
      const columnTasks = getTasksByStatus(targetStatus);
      const draggedTask = tasks.find(t => t.id === draggedTaskId);

      // Calculate final target index
      let finalIndex = dropTargetIndex ?? columnTasks.length;

      // If moving within the same column, adjust index
      if (draggedTask && draggedTask.status === targetStatus) {
        const currentIndex = columnTasks.findIndex(t => t.id === draggedTaskId);
        if (currentIndex !== -1 && currentIndex < finalIndex) {
          finalIndex = Math.max(0, finalIndex - 1);
        }
        // Only call if position actually changed
        if (currentIndex !== finalIndex) {
          onStatusChange?.(draggedTaskId, targetStatus, finalIndex);
        }
      } else {
        // Moving to different column
        onStatusChange?.(draggedTaskId, targetStatus, finalIndex);
      }
    }

    // Reset drag state
    draggedTaskId = null;
    draggedFromStatus = null;
    dropTargetStatus = null;
    dropTargetIndex = null;
  }

  function handleDragEnd() {
    draggedTaskId = null;
    draggedFromStatus = null;
    dropTargetStatus = null;
    dropTargetIndex = null;
  }
</script>

<div class="flex flex-col h-full {className}">
  <!-- Header (optional) -->
  {#if showHeader}
    <div class="flex-shrink-0 px-4 py-3 border-b border-[var(--color-border-primary)]">
      <div class="flex items-center justify-between">
        <div>
          <h2 class="text-lg font-semibold text-[var(--color-text-primary)]">
            {projectName}
          </h2>
          <p class="text-sm text-[var(--color-text-tertiary)]">
            {tasks.length} tasks ({getTasksByStatus('ToDo').length} to do, {getTasksByStatus('InProgress').length} in progress)
          </p>
        </div>
        <div class="flex items-center gap-2">
          <Button size="sm" variant="ghost">
            <Icon name="sliders" size="sm" />
            <span class="ml-1">Filter</span>
          </Button>
          <Button size="sm" variant="ghost">
            <Icon name="layout" size="sm" />
            <span class="ml-1">View</span>
          </Button>
        </div>
      </div>
    </div>
  {/if}

  <!-- Kanban Columns -->
  <div class="flex-1 overflow-x-auto px-2 py-1">
    {#if loading}
      <div class="flex-1 flex items-center justify-center">
        <div class="flex flex-col items-center gap-3 text-[var(--color-text-tertiary)]">
          <Spinner size="lg" />
          <span class="text-sm">Loading tasks...</span>
        </div>
      </div>
    {:else}
      <div class="flex gap-2 h-full">
        {#each columns as column (column.status)}
          {@const allColumnTasks = getTasksByStatus(column.status)}
          {@const columnTasks = getFilteredColumnTasks(column.status)}
          {@const columnStyle = getColumnStyle(column.status)}
          {@const isDropTarget = dropTargetStatus === column.status && draggedFromStatus !== column.status}
          {@const isFiltered = hasActiveFilter(column.status)}
          {@const colFilter = columnFilters[column.status]}

          <!-- svelte-ignore a11y_no_static_element_interactions -->
          <div
            class="flex flex-col flex-1 min-w-[240px] rounded-lg shadow-sm transition-colors
              {isDropTarget ? 'bg-[var(--color-accent-primary)]/5 ring-2 ring-[var(--color-accent-primary)]/30' : 'bg-[var(--color-bg-primary)]'}"
            ondragover={(e) => handleDragOver(e, column.status)}
            ondragleave={handleDragLeave}
            ondrop={(e) => handleDrop(e, column.status)}
          >
            <!-- Column Header -->
            <div
              class="flex items-center justify-between px-3 py-2 border-b-2"
              style="border-color: {column.color};"
            >
              <div class="flex items-center gap-2">
                <div
                  class="w-2 h-2 rounded-full"
                  style="background-color: {column.color};"
                ></div>
                <span class="font-medium text-sm text-[var(--color-text-primary)]">
                  {column.label}
                </span>
                <span class="text-xs px-1.5 py-0.5 rounded-full bg-[var(--color-bg-tertiary)] text-[var(--color-text-secondary)]">
                  {#if isFiltered}
                    {columnTasks.length}/{allColumnTasks.length}
                  {:else}
                    {columnTasks.length}
                  {/if}
                </span>
              </div>
              <div class="flex items-center gap-0.5">
                <!-- Column filter dropdown -->
                <Dropdown position="bottom-right" closeOnSelect={false}>
                  {#snippet trigger()}
                    <IconButton
                      icon="more-vertical"
                      size="sm"
                      variant={isFiltered ? 'primary' : 'ghost'}
                      title="Filter & Sort"
                    />
                  {/snippet}

                  <div class="py-1 min-w-[160px]">
                    <!-- Priority filter -->
                    <div class="px-3 py-1 text-[0.625rem] font-semibold text-[var(--color-text-tertiary)] uppercase tracking-wider">
                      Priority
                    </div>
                    {#each priorityOptions as p (p.value)}
                      {@const checked = colFilter?.priorities.has(p.value) ?? false}
                      <Button
                        variant="unstyled"
                        class="w-full flex items-center gap-2 px-3 py-1.5 text-sm text-left hover:bg-[var(--color-bg-tertiary)] transition-colors
                          {checked ? 'text-[var(--color-text-primary)]' : 'text-[var(--color-text-secondary)]'}"
                        onclick={() => togglePriorityFilter(column.status, p.value)}
                      >
                        <Icon name={checked ? 'check-square' : 'square'} size="sm" />
                        <Icon name={p.icon} size="xs" />
                        <span>{p.label}</span>
                      </Button>
                    {/each}

                    <!-- Separator -->
                    <div class="border-t border-[var(--color-border)] my-1"></div>

                    <!-- Sort options -->
                    <div class="px-3 py-1 text-[0.625rem] font-semibold text-[var(--color-text-tertiary)] uppercase tracking-wider">
                      Sort by
                    </div>
                    {#each sortOptions as opt (opt.value)}
                      {@const active = (colFilter?.sortBy ?? 'default') === opt.value}
                      <Button
                        variant="unstyled"
                        class="w-full flex items-center gap-2 px-3 py-1.5 text-sm text-left hover:bg-[var(--color-bg-tertiary)] transition-colors
                          {active ? 'text-[var(--color-accent-primary)] font-medium' : 'text-[var(--color-text-secondary)]'}"
                        onclick={() => setSortOption(column.status, opt.value)}
                      >
                        <Icon name={active ? 'check' : opt.icon} size="sm" class={active ? '' : 'opacity-0'} />
                        <Icon name={opt.icon} size="xs" />
                        <span>{opt.label}</span>
                      </Button>
                    {/each}
                  </div>
                </Dropdown>
              </div>
            </div>

            <!-- Task Cards -->
            <div
              class="flex flex-col flex-1 overflow-y-auto p-2 gap-2"
              ondragover={(e) => {
                // When dragging over empty space below cards, show indicator at end
                if (draggedTaskId) {
                  e.preventDefault();
                  dropTargetStatus = column.status;
                  dropTargetIndex = columnTasks.length;
                }
              }}
            >
              {#each columnTasks as task, taskIndex (task.id)}
                {@const isDragging = draggedTaskId === task.id}
                {@const isSelected = selectedTaskId === task.id}
                {@const showDropIndicatorBefore = dropTargetStatus === column.status && dropTargetIndex === taskIndex && draggedTaskId !== task.id}
                {@const showDropIndicatorAfter = dropTargetStatus === column.status && dropTargetIndex === taskIndex + 1 && taskIndex === columnTasks.length - 1 && draggedTaskId !== task.id}
                <div
                  draggable="true"
                  role="button"
                  tabindex="0"
                  aria-label={task.title}
                  class="relative p-3 rounded-lg border cursor-grab transition-all
                    {isDragging ? 'opacity-50 cursor-grabbing' : ''}
                    {isSelected
                      ? 'bg-[var(--color-accent-primary)]/10 border-[var(--color-border-primary)]'
                      : 'bg-[var(--color-bg-primary)] border-[var(--color-border-primary)] hover:border-[var(--color-border-secondary)]'}"
                  ondragstart={(e) => handleDragStart(e, task)}
                  ondragend={handleDragEnd}
                  ondragover={(e) => handleDragOverCard(e, column.status, taskIndex, e.currentTarget as HTMLElement)}
                  onclick={() => onSelectTask?.(task.id)}
                  onkeydown={(e) => {
                    if (e.key === 'Enter' || e.key === ' ') {
                      e.preventDefault();
                      onSelectTask?.(task.id);
                    }
                  }}
                >
                  <!-- Drop indicator at top edge -->
                  {#if showDropIndicatorBefore}
                    <div class="absolute -top-1 left-0 right-0 h-0.5 bg-[var(--color-accent-primary)] rounded-full"></div>
                  {/if}
                  <!-- Drop indicator at bottom edge (for last card) -->
                  {#if showDropIndicatorAfter}
                    <div class="absolute -bottom-1 left-0 right-0 h-0.5 bg-[var(--color-accent-primary)] rounded-full"></div>
                  {/if}
                  <!-- Priority Badge & Menu -->
                  <div class="flex items-center justify-between mb-2">
                    <PriorityBadge priority={(task.priority ?? 'Medium') as Priority} size="sm" />
                    <IconButton
                      icon="more-horizontal"
                      size="sm"
                      variant="ghost"
                      title="More options"
                      onclick={(e) => { e.stopPropagation(); }}
                    />
                  </div>

                  <!-- Title -->
                  <div class="font-medium text-sm text-[var(--color-text-primary)] mb-2 line-clamp-2">
                    {task.title}
                  </div>

                  <!-- Assignee -->
                  <div class="flex items-center gap-1 mb-2 text-xs text-[var(--color-text-secondary)]">
                    <Icon name="user" size="xs" />
                    <span>@{task.assignee?.name}</span>
                  </div>

                  <!-- Links & Effort -->
                  <div class="flex items-center justify-between text-xs text-[var(--color-text-tertiary)]">
                    {#if task.linkedItemCount > 0}
                      <span class="flex items-center gap-1">
                        <Icon name="link" size="xs" />
                        {task.linkedItemCount}
                      </span>
                    {:else}
                      <span></span>
                    {/if}

                    {#if task.estimatedHours > 0}
                      <span class="flex items-center gap-1 {task.hasOverdueEffort ? 'text-red-500' : ''}">
                        <Icon name="clock" size="xs" />
                        {task.actualHours}h / {task.estimatedHours}h
                      </span>
                    {/if}
                  </div>

                  <!-- Project badge (for My Tasks integrated view) -->
                  {#if showProjectBadge}
                    <div class="mt-2 pt-2 border-t border-[var(--color-border-primary)]">
                      <span class="text-xs text-[var(--color-text-tertiary)] px-1.5 py-0.5 bg-[var(--color-bg-tertiary)] rounded">
                        {task.projectName}
                      </span>
                    </div>
                  {/if}
                </div>
              {/each}

              <!-- Empty state for column -->
              {#if columnTasks.length === 0}
                <!-- Drop indicator for empty column (at top) -->
                {#if dropTargetStatus === column.status && draggedTaskId}
                  <div class="h-0.5 bg-[var(--color-accent-primary)] rounded-full transition-all"></div>
                {/if}
                <div class="flex flex-col items-center justify-center flex-1 text-[var(--color-text-tertiary)]">
                  <Icon name={columnStyle.icon} size="lg" class="mb-2 opacity-30" />
                  <p class="text-xs">No tasks</p>
                </div>
              {/if}
            </div>

          </div>
        {/each}
      </div>
    {/if}
  </div>
</div>
