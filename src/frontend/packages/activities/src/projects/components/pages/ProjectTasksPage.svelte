<!-- Activity: Projects > Nav: Tasks (project-{id}-tasks) | Screen ID: PRJ-TASK-001 -->
<script lang="ts">
  import { untrack } from 'svelte';
  import { SvelteMap } from 'svelte/reactivity';
  import { Icon, IconButton, Button, Input, Select } from '@sddp/ui';
  import type { ComboboxOption } from '@sddp/ui';
  import {
    PageShell,
    PageHeader,
    PageBody,
    ContentDetailLayout,
    CONTENT_DETAIL_LAYOUT,
    getTabState,
    setTabState,
    toast,
  } from '@sddp/shell';
  import { KanbanBoard, TaskMetaPanel, TaskFormPanel, TimeLogPanel } from '../../../task/components/sections';
  import type { TaskFormData } from '../../../task/components/sections/TaskFormPanel.svelte';
  import type { TimeLogFormData } from '../../../task/components/sections/TimeLogPanel.svelte';
  import type { ProjectDetail } from '../../types';
  import type { TaskSummary, Task, TaskStatus } from '../../../task/types';
  import {
    getTasks,
    getTaskById,
    createTask,
    updateTask,
    deleteTask,
    updateTaskPosition,
    addTimeLog,
    mapToTaskSummary,
    mapToTask,
    type UpdateTaskRequest,
  } from '../../../task/services';
  import { getProjectById } from '../../services/ProjectService';
  import { getAuthState } from '@sddp/shell/auth';

  interface Props {
    projectId: string;
    projectName?: string;
    project?: ProjectDetail;
    tabId?: string;
    class?: string;
  }

  let { projectId, projectName = '', project, tabId = '', class: className = '' }: Props = $props();

  const pageTitle = 'Tasks';
  const pageMeta = $derived(project?.name || projectName || undefined);

  // Panel mode type
  type PanelMode = 'view' | 'create' | 'edit' | 'logTime';

  // State
  let selectedTaskId = $state<string | null>(null);
  let kanbanTasks = $state<TaskSummary[]>([]);
  let currentTask = $state<Task | null>(null);
  let loadingTask = $state(false);
  let loading = $state(true);
  let error = $state<string | null>(null);
  // Member candidates for assignee Combobox
  let fallbackMembers = $state<ComboboxOption[]>([]);
  const memberCandidates = $derived.by<ComboboxOption[]>(() => {
    const members = project?.members;
    if (members && members.length > 0) {
      return members.map((m) => ({
        value: m.userId,
        label: m.displayName,
        description: m.role,
      }));
    }
    return fallbackMembers;
  });

  // Fallback: load members directly when project prop is not provided
  $effect(() => {
    if (project?.members && project.members.length > 0) return;
    if (fallbackMembers.length > 0) return;
    if (!projectId) return;
    untrack(() => loadMembersFallback());
  });

  async function loadMembersFallback() {
    try {
      const tenantId = getAuthState().user?.tenantId;
      if (!tenantId) return;
      const detail = await getProjectById(tenantId, projectId);
      fallbackMembers = (detail.members ?? []).map((m) => ({
        value: m.userId,
        label: m.displayName,
        description: m.role,
      }));
    } catch (err) {
      console.error('Failed to load project members:', err);
    }
  }

  // Panel mode state
  let panelMode = $state<PanelMode>('view');
  let createTaskStatus = $state<TaskStatus>('ToDo');

  // Filter state
  let searchQuery = $state('');
  let assigneeFilter = $state<string>('all');
  let priorityFilter = $state<string>('all');

  interface ProjectTasksTabState {
    selectedTaskId: string | null;
    panelMode: PanelMode;
    createTaskStatus: TaskStatus;
    searchQuery: string;
    assigneeFilter: string;
    priorityFilter: string;
  }

  const tabStateKey = $derived(tabId || `project-${projectId}-tasks`);
  let restoredKey = $state<string | null>(null);
  let isRestored = $state(false);

  $effect(() => {
    if (!tabStateKey || restoredKey === tabStateKey) return;
    const saved = getTabState<ProjectTasksTabState>(tabStateKey);
    if (saved) {
      selectedTaskId = saved.selectedTaskId ?? null;
      panelMode = saved.panelMode ?? 'view';
      createTaskStatus = saved.createTaskStatus === 'Backlog' ? 'ToDo' : (saved.createTaskStatus ?? 'ToDo');
      searchQuery = saved.searchQuery ?? '';
      assigneeFilter = saved.assigneeFilter ?? 'all';
      priorityFilter = saved.priorityFilter ?? 'all';
    }
    restoredKey = tabStateKey;
    isRestored = true;
  });

  // Persist tab state
  $effect(() => {
    if (!tabStateKey || !isRestored) return;
    const state: ProjectTasksTabState = {
      selectedTaskId,
      panelMode,
      createTaskStatus,
      searchQuery,
      assigneeFilter,
      priorityFilter,
    };
    setTabState<ProjectTasksTabState>(tabStateKey, state);
  });

  // Derived: unique assignees from tasks
  const assignees = $derived.by(() => {
    const map = new SvelteMap<string, string>();
    for (const task of kanbanTasks) {
      if (task.assignee?.id && task.assignee?.name) {
        map.set(task.assignee.id, task.assignee.name);
      }
    }
    return Array.from(map.entries()).map(([id, name]) => ({ id, name }));
  });

  const assigneeOptions = $derived([
    { value: 'all', label: 'All Assignees' },
    ...assignees.map(a => ({ value: a.id, label: a.name })),
  ]);

  const priorityOptions = [
    { value: 'all', label: 'All Priorities' },
    { value: 'High', label: 'High' },
    { value: 'Medium', label: 'Medium' },
    { value: 'Low', label: 'Low' },
  ];

  // Derived: filtered tasks
  const filteredTasks = $derived.by(() => {
    let result = kanbanTasks;

    // Search filter
    if (searchQuery.trim()) {
      const query = searchQuery.toLowerCase();
      result = result.filter(t =>
        t.title.toLowerCase().includes(query) ||
        (t.assignee?.name ?? '').toLowerCase().includes(query)
      );
    }

    // Assignee filter
    if (assigneeFilter !== 'all') {
      result = result.filter(t => t.assignee?.id === assigneeFilter);
    }

    // Priority filter
    if (priorityFilter !== 'all') {
      result = result.filter(t => t.priority === priorityFilter);
    }

    return result;
  });

  // Check if any filter is active
  const hasActiveFilters = $derived(
    searchQuery.trim() !== '' || assigneeFilter !== 'all' || priorityFilter !== 'all'
  );

  function clearFilters() {
    searchQuery = '';
    assigneeFilter = 'all';
    priorityFilter = 'all';
  }

  // Fetch tasks from API
  async function fetchTasks() {
    loading = true;
    error = null;
    try {
      const response = await getTasks(undefined as unknown as string, {
        projectId,
        pageSize: 100, // Get all tasks for kanban view
      });
      kanbanTasks = response.items
        .filter((item) => item.status !== 'Backlog')
        .map((item) => mapToTaskSummary(item, project?.name || 'Project'));
    } catch (err) {
      console.error('Failed to fetch tasks:', err);
      error = err instanceof Error ? err.message : 'Failed to fetch tasks';
    } finally {
      loading = false;
    }
  }

  // Initialize
  $effect(() => {
    untrack(() => fetchTasks());
  });

  // Load task detail when selected
  let prevSelectedTaskId = $state<string | null>(null);
  $effect(() => {
    if (!selectedTaskId) {
      prevSelectedTaskId = null;
      currentTask = null;
      return;
    }
    if (selectedTaskId === prevSelectedTaskId) return;
    prevSelectedTaskId = selectedTaskId;
    const taskId = selectedTaskId;
    untrack(() => loadTaskDetail(taskId));
  });

  // Load task detail from API
  async function loadTaskDetail(taskId: string) {
    loadingTask = true;
    try {
      const detail = await getTaskById(undefined as unknown as string, taskId);
      currentTask = mapToTask(detail, project?.name || 'Project');
    } catch (err) {
      console.error('[ProjectTasksPage] Failed to load task detail:', err);
      currentTask = null;
    } finally {
      loadingTask = false;
    }
  }

  async function handleRefresh() {
    await fetchTasks();
  }

  // Handlers
  function handleSelectTask(taskId: string) {
    selectedTaskId = taskId;
    panelMode = 'view';
  }

  async function handleStatusChange(taskId: string, newStatus: TaskStatus, targetIndex?: number) {
    // Find the task being moved
    const taskToMove = kanbanTasks.find(t => t.id === taskId);
    if (!taskToMove) return;

    const oldStatus = taskToMove.status;
    void oldStatus;

    // Optimistic update for kanban with position handling
    kanbanTasks = kanbanTasks.filter(t => t.id !== taskId);
    const updatedTask = { ...taskToMove, status: newStatus };

    if (targetIndex !== undefined) {
      // Get tasks in target column (after removing the moved task)
      const targetColumnTasks = kanbanTasks.filter(t => t.status === newStatus);
      const otherTasks = kanbanTasks.filter(t => t.status !== newStatus);

      // Insert at specific position
      targetColumnTasks.splice(targetIndex, 0, updatedTask);
      kanbanTasks = [...otherTasks, ...targetColumnTasks];
    } else {
      // Default: add to end
      kanbanTasks = [...kanbanTasks, updatedTask];
    }

    // Optimistic update for detail panel
    if (currentTask && currentTask.id === taskId) {
      currentTask = { ...currentTask, status: newStatus };
    }

    try {
      // Call position API for both status and position changes
      const newPosition = targetIndex ?? 0;
      await updateTaskPosition(undefined as unknown as string, taskId, {
        newStatus,
        newPosition,
      });

      // Reload detail if this is the selected task
      if (selectedTaskId === taskId) {
        await loadTaskDetail(taskId);
      }
    } catch (err) {
      console.error('Failed to update task position:', err);
      // Revert on error
      await fetchTasks();
      if (selectedTaskId === taskId) {
        await loadTaskDetail(taskId);
      }
    }
  }

  function handleNewTask(status?: TaskStatus) {
    createTaskStatus = status || 'ToDo';
    panelMode = 'create';
    selectedTaskId = null;
  }

  function handleEdit() {
    if (currentTask) {
      panelMode = 'edit';
    }
  }

  function handleLogTime() {
    if (currentTask) {
      panelMode = 'logTime';
    }
  }

  async function handleSaveTask(data: TaskFormData) {
    try {
      if (panelMode === 'create') {
        await createTask(undefined as unknown as string, projectId, {
          title: data.title,
          description: data.description,
          status: data.status,
          priority: data.priority,
          assigneeId: data.assigneeId || undefined,
          estimatedHours: data.estimatedHours,
        });
      } else if (panelMode === 'edit' && currentTask) {
        const request: UpdateTaskRequest = {
          title: data.title,
          description: data.description,
          priority: data.priority,
          assigneeId: data.assigneeId || null,
          estimatedHours: data.estimatedHours || undefined,
        };
        await updateTask(undefined as unknown as string, currentTask.id, request);
        // Reload task detail to reflect changes
        await loadTaskDetail(currentTask.id);
      }
      panelMode = 'view';
      await fetchTasks();
    } catch (err) {
      console.error('Failed to save task:', err);
    }
  }

  async function handleSaveTimeLog(data: TimeLogFormData) {
    if (!selectedTaskId) return;

    try {
      await addTimeLog(undefined as unknown as string, selectedTaskId, {
        date: data.date,
        hours: data.hours,
        description: data.description,
      });
      panelMode = 'view';
      // Reload task detail to get updated time logs
      await loadTaskDetail(selectedTaskId);
      // Also refresh task list to update actual hours
      await fetchTasks();
    } catch (err) {
      console.error('Failed to save time log:', err);
    }
  }

  function handleCancelPanel() {
    panelMode = 'view';
  }

  async function handleDeleteTask(): Promise<void> {
    if (!selectedTaskId) return;
    if (!confirm('Are you sure you want to delete this task?')) return;
    const tenantId = getAuthState().user?.tenantId;
    if (!tenantId) return;
    try {
      await deleteTask(tenantId, selectedTaskId, projectId);
      selectedTaskId = null;
      currentTask = null;
      toast.success('Task deleted');
      await fetchTasks();
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Failed to delete task');
    }
  }
</script>

<PageShell class={className}>
  <PageHeader title={pageTitle} meta={pageMeta} {loading}>
    {#snippet actions()}
      <IconButton
        icon="refresh-cw"
        size="sm"
        variant="ghost"
        title="Refresh tasks"
        onclick={handleRefresh}
      />
      <IconButton
        icon="plus"
        size="sm"
        variant="ghost"
        title="New Task"
        onclick={() => handleNewTask('ToDo')}
      />
    {/snippet}
  </PageHeader>

  <PageBody class="p-0">
    {#if loading}
      <div class="flex-1 flex items-center justify-center">
        <!-- <Spinner size="lg" /> -->
      </div>
    {:else if error}
      <div class="flex flex-col items-center justify-center h-full gap-4">
        <Icon name="alert-circle" size="lg" class="text-red-500" />
        <p class="text-[var(--color-text-secondary)]">{error}</p>
        <Button variant="secondary" size="sm" onclick={handleRefresh}>
          Retry
        </Button>
      </div>
    {:else}
      <ContentDetailLayout
        showDetailPanel={true}
        mainContentWidth={CONTENT_DETAIL_LAYOUT.mainContentWidth}
        minMainContentWidth={CONTENT_DETAIL_LAYOUT.minMainContentWidth}
        maxMainContentWidth={CONTENT_DETAIL_LAYOUT.maxMainContentWidth}
        minDetailPanelWidth={CONTENT_DETAIL_LAYOUT.minDetailPanelWidth}
      >
        <!-- Main Content: Filter Bar + Kanban Board -->
        <div class="flex flex-col h-full">
          <!-- Filter Bar (aligned with Kanban columns) -->
          <div class="flex-shrink-0 px-2 pt-3 pb-1">
            <!-- Use same flex layout as Kanban columns: 3 equal columns with gap-4 -->
            <div class="flex gap-2">
              <!-- Column 1: Search (aligned with To Do) -->
              <div class="flex-1 min-w-[240px]">
                <div class="relative">
                  <Icon name="search" size="sm" class="absolute left-2.5 top-1/2 -translate-y-1/2 text-[var(--color-text-tertiary)]" />
                  <Input
                    type="text"
                    placeholder="Search tasks..."
                    bind:value={searchQuery}
                    variant="flat"
                    class="pl-8 w-full"
                    size="sm"
                  />
                </div>
              </div>

              <!-- Column 2: Assignee + Priority filters (aligned with In Progress) -->
              <div class="flex-1 min-w-[240px] flex items-center gap-2">
                <Select
                  bind:value={assigneeFilter}
                  options={assigneeOptions}
                  placeholder=""
                  size="sm"
                  variant="flat"
                  class="flex-1"
                />
                <Select
                  bind:value={priorityFilter}
                  options={priorityOptions}
                  placeholder=""
                  size="sm"
                  variant="flat"
                  class="flex-1"
                />
              </div>

              <!-- Column 3: Clear + Count (aligned with Done) -->
              <div class="flex-1 min-w-[240px] flex items-center justify-end gap-2">
                {#if hasActiveFilters}
                  <Button variant="ghost" size="sm" onclick={clearFilters} class="!text-xs !px-2 !py-1">
                    <Icon name="x" size="xs" />
                    Clear
                  </Button>
                {/if}
                <span class="text-xs text-[var(--color-text-tertiary)]">
                  {filteredTasks.length} / {kanbanTasks.length} tasks
                </span>
              </div>
            </div>
          </div>

          <!-- Kanban Board (lanes as cards) -->
          <div class="flex-1 min-h-0">
            <KanbanBoard
              tasks={filteredTasks}
              projectName={project?.name || 'Project Tasks'}
              {selectedTaskId}
              loading={false}
              showProjectBadge={false}
              showHeader={false}
              onSelectTask={handleSelectTask}
              onStatusChange={handleStatusChange}
              onNewTask={handleNewTask}
            />
          </div>
        </div>

        <!-- Detail Panel -->
        {#snippet detailPanel()}
          {#if panelMode === 'create'}
            <TaskFormPanel
              mode="create"
              {projectId}
              projectName={project?.name || ''}
              defaultStatus={createTaskStatus}
              allowBacklogStatus={false}
              {memberCandidates}
              onSave={handleSaveTask}
              onCancel={handleCancelPanel}
            />
          {:else if panelMode === 'edit' && currentTask}
            <TaskFormPanel
              mode="edit"
              task={currentTask}
              allowBacklogStatus={false}
              {memberCandidates}
              onSave={handleSaveTask}
              onCancel={handleCancelPanel}
            />
          {:else if panelMode === 'logTime' && currentTask}
            <TimeLogPanel
              task={currentTask}
              onLogTime={handleSaveTimeLog}
              onCancel={handleCancelPanel}
            />
          {:else}
            <TaskMetaPanel
              task={currentTask}
              {projectId}
              loading={loadingTask}
              onEdit={handleEdit}
              onLogTime={handleLogTime}
              onDelete={handleDeleteTask}
              onClose={() => { selectedTaskId = null; currentTask = null; }}
            />
          {/if}
        {/snippet}
      </ContentDetailLayout>
    {/if}
  </PageBody>
</PageShell>
