<!-- Activity: Tasks > Nav: My Tasks (tasks-my-tasks) | Screen ID: ACT-TASK-001 -->
<script lang="ts">
  import { untrack } from 'svelte';
  import { Icon, Button, Input, Select } from '@sddp/ui';
  import {
    getAuthState,
    subscribeAuth,
    toast,
    getTabState,
    setTabState,
    PageShell,
    PageHeader,
    PageBody,
  } from '@sddp/shell';
  import TaskSidebar from '../sections/TaskSidebar.svelte';
  import KanbanBoard from '../sections/KanbanBoard.svelte';
  import BacklogView from '../sections/BacklogView.svelte';
  import TaskMetaPanel from '../sections/TaskMetaPanel.svelte';
  import TaskFormPanel from '../sections/TaskFormPanel.svelte';
  import TimeLogPanel from '../sections/TimeLogPanel.svelte';

  import type { TaskFormData } from '../sections/TaskFormPanel.svelte';
  import type { TimeLogFormData } from '../sections/TimeLogPanel.svelte';
  import {
    subscribeTask,
    setSelectedTask,
    setTaskSearchQuery,
    setTaskFilterType,
    setTaskViewMode,
    selectBacklogProject,
    setBacklogSummary,
    setBacklogStats,
    setBacklogLoading,
    setMyTasksStats,
    setTaskProjectGroups,
  } from '../../stores';
  import {
    getTaskById,
    getTasks,
    createTask,
    updateTask,
    deleteTask,
    updateTaskStatus,
    addTimeLog,

    getBacklogSummary,
    getBacklogStats,
    getMyTaskStats,
    mapToTask,
    mapToTaskSummary,
    type CreateTaskRequest,
    type UpdateTaskRequest,
  } from '../../services/TaskService';
  import { getProjectById } from '../../../projects/services/ProjectService';
  import type { ComboboxOption } from '@sddp/ui';
  import type {
    TaskFilterType,
    TaskSummary,
    Task,
    TaskStatus,
    MyTasksStats,
    BacklogSummary,
    BacklogStats,
    ProjectTaskGroup,
  } from '../../types';

  // Panel mode type
  type PanelMode = 'view' | 'create' | 'edit' | 'logTime';

  interface Props {
    tenantId?: string;
    projectId?: string;
    tabId?: string;
    hideSidebar?: boolean;
    class?: string;
  }

  let { tenantId: propTenantId = '', projectId: propProjectId = '', tabId = '', hideSidebar = false, class: className = '' }: Props = $props();

  // Auth state
  let authState = $state(getAuthState());
  const tenantId = $derived(propTenantId || authState.user?.tenantId || '');

  // Subscribe to auth state
  $effect(() => {
    const unsubscribe = subscribeAuth((state) => {
      authState = state;
    });
    return unsubscribe;
  });

  // State
  let selectedTaskId = $state<string | null>(null);
  let searchQuery = $state('');
  let filterType = $state<TaskFilterType>('all');
  let viewMode = $state<'myTasks' | 'backlog'>('myTasks');
  let selectedProjectId = $state<string | null>(null);
  let myTasksStats = $state<MyTasksStats>({
    toDoCount: 0,
    inProgressCount: 0,
    doneCount: 0,
    blockedCount: 0,
    totalCount: 0,
  });

  // Backlog state
  let backlogSummary = $state<BacklogSummary | null>(null);
  let backlogStats = $state<BacklogStats | null>(null);
  let backlogTasks = $state<TaskSummary[]>([]);
  let backlogLoading = $state(false);

  // Kanban state (synced via $effect from projectGroups + filterType)
  let projectGroups = $state<ProjectTaskGroup[]>([]);
  let kanbanTasks = $state<TaskSummary[]>([]);
  let kanbanTitle = $state('My Tasks');

  // Member candidates for assignee Combobox
  let memberCandidates = $state<ComboboxOption[]>([]);

  // Filter bar state
  let localSearch = $state('');
  let priorityFilter = $state('all');

  const priorityOptions = [
    { value: 'all', label: 'All Priorities' },
    { value: 'High', label: 'High' },
    { value: 'Medium', label: 'Medium' },
    { value: 'Low', label: 'Low' },
  ];

  const filteredTasks = $derived.by(() => {
    let result = kanbanTasks;
    if (localSearch.trim()) {
      const q = localSearch.toLowerCase();
      result = result.filter((t) =>
        t.title.toLowerCase().includes(q) || (t.assignee?.name ?? '').toLowerCase().includes(q)
      );
    }
    if (priorityFilter !== 'all') {
      result = result.filter((t) => t.priority === priorityFilter);
    }
    return result;
  });

  const hasActiveFilters = $derived(localSearch.trim().length > 0 || priorityFilter !== 'all');

  function clearFilters() {
    localSearch = '';
    priorityFilter = 'all';
  }

  // Current task detail
  let currentTask = $state<Task | null>(null);
  let loadingTask = $state(false);

  // Panel mode state
  let panelMode = $state<PanelMode>('view');
  let createTaskStatus = $state<TaskStatus>('ToDo');


  // Tab State Persistence
  interface TasksTabState {
    selectedTaskId: string | null;
    searchQuery: string;
    filterType: TaskFilterType;
    viewMode: 'myTasks' | 'backlog';
    selectedProjectId: string | null;
    panelMode: PanelMode;
  }

  const tabStateKey = $derived(tabId || 'my-tasks');
  let restoredKey = $state<string | null>(null);
  let isRestored = $state(false);

  $effect(() => {
    if (!tabStateKey || restoredKey === tabStateKey) return;
    const saved = getTabState<TasksTabState>(tabStateKey);
    if (saved) {
      panelMode = saved.panelMode ?? 'view';
      if (saved.searchQuery) setTaskSearchQuery(saved.searchQuery);
      if (saved.filterType) setTaskFilterType(saved.filterType);
      if (saved.viewMode) setTaskViewMode(saved.viewMode);
      if (saved.selectedProjectId) selectBacklogProject(saved.selectedProjectId);
      if (saved.selectedTaskId) setSelectedTask(saved.selectedTaskId);
    }
    restoredKey = tabStateKey;
    isRestored = true;
  });

  $effect(() => {
    if (!tabStateKey || !isRestored) return;
    setTabState<TasksTabState>(tabStateKey, {
      selectedTaskId,
      searchQuery,
      filterType,
      viewMode,
      selectedProjectId,
      panelMode,
    });
  });

  // Subscribe to store
  $effect(() => {
    const unsubscribe = subscribeTask((state) => {
      selectedTaskId = state.selectedTaskId;
      searchQuery = state.searchQuery;
      filterType = state.filterType;
      viewMode = state.viewMode;
      selectedProjectId = state.selectedProjectId;
      myTasksStats = state.myTasksStats;
      projectGroups = state.projectGroups;
      backlogSummary = state.backlogSummary;
      backlogStats = state.backlogStats;
      backlogLoading = state.backlogLoading;
    });
    return unsubscribe;
  });

  // Sync kanban board with projectGroups data and filterType
  $effect(() => {
    const all = projectGroups.flatMap((g) => g.tasks);
    if (filterType === 'all') {
      kanbanTasks = all;
      kanbanTitle = 'My Tasks';
    } else if (filterType === 'todo') {
      kanbanTasks = all.filter((t) => t.status === 'ToDo');
      kanbanTitle = 'My Tasks - To Do';
    } else if (filterType === 'inProgress') {
      kanbanTasks = all.filter((t) => t.status === 'InProgress');
      kanbanTitle = 'My Tasks - In Progress';
    } else if (filterType === 'done') {
      kanbanTasks = all.filter((t) => t.status === 'Done');
      kanbanTitle = 'My Tasks - Done';
    } else if (filterType === 'blocked') {
      kanbanTasks = all.filter((t) => t.status === 'Blocked');
      kanbanTitle = 'My Tasks - Blocked';
    }
  });

  // Load initial task data on mount
  $effect(() => {
    if (tenantId) {
      untrack(() => loadInitialData());
    }
  });

  async function loadInitialData() {
    if (!tenantId) return;
    try {
      const [statsResult, tasksResult] = await Promise.allSettled([
        getMyTaskStats(tenantId),
        getTasks(tenantId, { myTasksOnly: true, pageSize: 100 }),
      ]);
      if (statsResult.status === 'fulfilled') {
        setMyTasksStats(statsResult.value);
      }
      if (tasksResult.status === 'rejected') {
        throw tasksResult.reason;
      }
      const tasksPage = tasksResult.value;

      // Group tasks by project (personal tasks grouped under 'personal')
      const groupedTasks: Record<string, TaskSummary[]> = {};
      for (const dto of tasksPage.items) {
        if (dto.status === 'Backlog') continue;
        const pid = dto.projectId ?? 'personal';
        groupedTasks[pid] ??= [];
        groupedTasks[pid].push(mapToTaskSummary(dto, ''));
      }

      const groups: import('../../types').ProjectTaskGroup[] = Object.entries(groupedTasks).map(([projId, tasks]) => ({
        projectId: projId,
        projectName: tasks[0]?.projectName ?? projId,
        expanded: true,
        stats: {
          projectId: projId,
          projectName: tasks[0]?.projectName ?? projId,
          toDoCount: tasks.filter((t) => t.status === 'ToDo').length,
          inProgressCount: tasks.filter((t) => t.status === 'InProgress').length,
          doneCount: tasks.filter((t) => t.status === 'Done').length,
          totalCount: tasks.length,
          activeCount: tasks.filter((t) => t.status !== 'Done').length,
        },
        tasks,
      }));

      setTaskProjectGroups(groups);
    } catch (err) {
      console.error('Failed to load task data:', err);
    }

    // Also load backlog summary
    await loadBacklogSummary();

    // Load member candidates for assignee combobox
    if (propProjectId) {
      try {
        const detail = await getProjectById(tenantId, propProjectId);
        memberCandidates = (detail.members ?? []).map((m) => ({
          value: m.userId,
          label: m.displayName,
          description: m.role,
        }));
      } catch {
        memberCandidates = [];
      }
    }
  }

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
    if (!tenantId) return;

    loadingTask = true;
    try {
      const taskDto = await getTaskById(tenantId, taskId);
      currentTask = mapToTask(taskDto);
    } catch (err) {
      console.error('Failed to load task:', err);
      toast.error('Failed to load task details');
      currentTask = null;
    } finally {
      loadingTask = false;
    }
  }

  // Handlers
  function handleSearch(query: string) {
    setTaskSearchQuery(query);
  }

  function handleFilterChange(filter: TaskFilterType) {
    setTaskFilterType(filter);
  }

  function handleViewModeChange(mode: 'myTasks' | 'backlog') {
    setTaskViewMode(mode);
  }

  async function handleSelectBacklogProject(projectId: string) {
    selectBacklogProject(projectId);
    await loadBacklogStats(projectId);
  }

  async function loadBacklogSummary() {
    if (!tenantId) return;
    try {
      const summary = await getBacklogSummary(tenantId);
      setBacklogSummary(summary);
    } catch (err) {
      console.error('Failed to load backlog summary:', err);
    }
  }

  async function loadBacklogStats(projectId: string) {
    if (!tenantId) return;
    setBacklogLoading(true);
    try {
      const [statsResult, tasksResult] = await Promise.allSettled([
        getBacklogStats(tenantId, projectId),
        getTasks(tenantId, { projectId, pageSize: 100, myTasksOnly: false }),
      ]);
      const stats = statsResult.status === 'fulfilled' ? statsResult.value : null;
      if (stats) setBacklogStats(stats);
      if (tasksResult.status === 'rejected') {
        throw tasksResult.reason;
      }
      const tasksPage = tasksResult.value;
      backlogTasks = tasksPage.items.map((t) => mapToTaskSummary(t, stats?.projectName ?? ''));
    } catch (err) {
      console.error('Failed to load backlog stats:', err);
      toast.error('Failed to load backlog data');
      setBacklogStats(null);
      backlogTasks = [];
    } finally {
      setBacklogLoading(false);
    }
  }

  function handleSelectTask(taskId: string) {
    setSelectedTask(taskId);
  }

  async function handleStatusChange(taskId: string, newStatus: TaskStatus) {
    if (!tenantId) return;

    try {
      await updateTaskStatus(tenantId, taskId, newStatus);
      toast.success(`Task status updated to ${newStatus}`);
      // Reload the task to get updated data
      if (selectedTaskId === taskId) {
        await loadTaskDetail(taskId);
      }
    } catch (err) {
      console.error('Failed to update task status:', err);
      toast.error('Failed to update task status');
    }
  }

  function handleNewTask(status?: TaskStatus) {
    const defaultStatus = viewMode === 'backlog' && selectedProjectId ? 'Backlog' : 'ToDo';
    createTaskStatus = status || defaultStatus;
    panelMode = 'create';
    setSelectedTask(null);
  }

  async function handleDeleteTask() {
    if (!selectedTaskId || !tenantId) return;
    if (!confirm('Are you sure you want to delete this task?')) return;

    try {
      const taskProjectId = propProjectId || currentTask?.projectId || undefined;
      await deleteTask(tenantId, selectedTaskId, taskProjectId);
      setSelectedTask(null);
      currentTask = null;
      toast.success('Task deleted');
      untrack(() => loadInitialData());
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Failed to delete task');
    }
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
    if (!tenantId) return;

    try {
      if (panelMode === 'create') {
        const request: CreateTaskRequest = {
          title: data.title,
          description: data.description,
          status: data.status,
          priority: data.priority,
          assigneeId: data.assigneeId || undefined,
          estimatedHours: data.estimatedHours || undefined,
        };
        const taskProjectId = propProjectId || data.projectId || undefined;
        const newTask = await createTask(tenantId, taskProjectId, request);
        toast.success('Task created successfully');
        await loadInitialData();
        setSelectedTask(newTask.id);
      } else if (panelMode === 'edit' && currentTask) {
        const request: UpdateTaskRequest = {
          title: data.title,
          description: data.description,
          priority: data.priority,
          assigneeId: data.assigneeId || null,
          estimatedHours: data.estimatedHours || undefined,
        };
        await updateTask(tenantId, currentTask.id, request);
        toast.success('Task updated successfully');
        await loadInitialData();
        await loadTaskDetail(currentTask.id);
      }
      panelMode = 'view';
    } catch (err) {
      console.error('Failed to save task:', err);
      toast.error('Failed to save task');
    }
  }

  async function handleSaveTimeLog(data: TimeLogFormData) {
    if (!tenantId || !currentTask) return;

    try {
      await addTimeLog(tenantId, currentTask.id, {
        date: data.date,
        hours: data.hours,
        description: data.description,
      });
      toast.success('Time log added successfully');
      // Reload task to see updated time logs
      await loadTaskDetail(currentTask.id);
      panelMode = 'view';
    } catch (err) {
      console.error('Failed to add time log:', err);
      toast.error('Failed to add time log');
    }
  }

  function handleCancelPanel() {
    panelMode = 'view';
  }
</script>

<PageShell class={className}>
  <PageHeader title={kanbanTitle}>
    {#snippet actions()}
      <!-- placeholder for future actions (refresh, filter, etc.) -->
    {/snippet}
  </PageHeader>

  <PageBody class="p-0">
    <div class="flex h-full">
      <!-- Sidebar (hidden when parent activity sidebar provides the same filters) -->
      {#if !hideSidebar}
        <div class="w-64 flex-shrink-0 border-r border-[var(--color-border-primary)]">
          <TaskSidebar
            {myTasksStats}
            {backlogSummary}
            {selectedProjectId}
            {searchQuery}
            {filterType}
            {viewMode}
            onSearch={handleSearch}
            onFilterChange={handleFilterChange}
            onViewModeChange={handleViewModeChange}
            onSelectBacklogProject={handleSelectBacklogProject}
            onNewTask={() => handleNewTask()}
          />
        </div>
      {/if}

      <!-- Main Content -->
      <div class="flex flex-col flex-1 min-w-0">
        {#if viewMode === 'backlog' && selectedProjectId}
          <BacklogView
            stats={backlogStats}
            tasks={backlogTasks}
            loading={backlogLoading}
            {selectedTaskId}
            onSelectTask={handleSelectTask}
          />
        {:else}
          <!-- Filter Bar -->
          <div class="flex-shrink-0 px-2 pt-3 pb-1">
            <div class="flex gap-2">
              <div class="flex-1 min-w-[240px]">
                <div class="relative">
                  <Icon name="search" size="sm" class="absolute left-2.5 top-1/2 -translate-y-1/2 text-[var(--color-text-tertiary)]" />
                  <Input
                    type="text"
                    placeholder="Search tasks..."
                    bind:value={localSearch}
                    variant="flat"
                    class="pl-8 w-full"
                    size="sm"
                  />
                </div>
              </div>
              <div class="flex-1 min-w-[240px] flex items-center gap-2">
                <Select
                  bind:value={priorityFilter}
                  options={priorityOptions}
                  placeholder=""
                  size="sm"
                  variant="flat"
                  class="flex-1"
                />
              </div>
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

          <!-- Kanban Board -->
          <div class="flex-1 min-h-0">
            <KanbanBoard
              tasks={filteredTasks}
              projectName={kanbanTitle}
              {selectedTaskId}
              showHeader={false}
              showProjectBadge={viewMode === 'myTasks'}
              onSelectTask={handleSelectTask}
              onStatusChange={handleStatusChange}
              onNewTask={handleNewTask}
            />
          </div>
        {/if}
      </div>

      <!-- Right Panel (Mode-based) -->
      <div class="w-80 flex-shrink-0">
        {#if panelMode === 'create'}
          <TaskFormPanel
            mode="create"
            projectId={propProjectId || (viewMode === 'backlog' ? (selectedProjectId ?? '') : (currentTask?.projectId || ''))}
            projectName=""
            defaultStatus={createTaskStatus}
            allowBacklogStatus={viewMode === 'backlog'}
            {memberCandidates}
            onSave={handleSaveTask}
            onCancel={handleCancelPanel}
          />
        {:else if panelMode === 'edit' && currentTask}
          <TaskFormPanel
            mode="edit"
            task={currentTask}
            allowBacklogStatus={currentTask.status === 'Backlog' || viewMode === 'backlog'}
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
            projectId={propProjectId || currentTask?.projectId || ''}
            loading={loadingTask}
            onEdit={handleEdit}
            onLogTime={handleLogTime}
            onDelete={handleDeleteTask}
          />
        {/if}
      </div>
    </div>
  </PageBody>
</PageShell>
