<!-- Activity: Tasks > Nav: Category / Backlog (tasks-category-{id}, tasks-backlog-{id}) | Screen ID: ACT-TASK-002 -->
<script lang="ts">
  import { untrack } from 'svelte';
  import { Icon, IconButton, Input, Spinner } from '@sddp/ui';
  import {
    getAuthState,
    subscribeAuth,
    toast,
    getTabState,
    setTabState,
    PageShell,
    PageHeader,
    PageBody,
    SidebarDetailLayout,
    SIDEBAR_DETAIL_LAYOUT,
  } from '@sddp/shell';
  import TaskItem from '../idioms/TaskItem.svelte';
  import TaskMetaPanel from '../sections/TaskMetaPanel.svelte';
  import TaskFormPanel from '../sections/TaskFormPanel.svelte';
  import TimeLogPanel from '../sections/TimeLogPanel.svelte';
  import type { TaskFormData } from '../sections/TaskFormPanel.svelte';
  import type { TimeLogFormData } from '../sections/TimeLogPanel.svelte';
  import {
    getTaskById,
    getTasks,
    createTask,
    updateTask,
    deleteTask,
    addTimeLog,
    mapToTask,
    mapToTaskSummary,
    type CreateTaskRequest,
    type UpdateTaskRequest,
  } from '../../services/TaskService';
  import { getProjectById } from '../../../projects/services/ProjectService';
  import type { ComboboxOption } from '@sddp/ui';
  import type { Task, TaskSummary } from '../../types';

  type PanelMode = 'view' | 'create' | 'edit' | 'logTime';

  interface Props {
    tenantId?: string;
    projectId?: string;
    categoryId?: string;
    categoryName?: string;
    tabId?: string;
    class?: string;
  }

  let {
    tenantId: propTenantId = '',
    projectId = '',
    categoryId = '',
    categoryName = '',
    tabId = '',
    class: className = '',
  }: Props = $props();

  // Auth state
  let authState = $state(getAuthState());
  const tenantId = $derived(propTenantId || authState.user?.tenantId || '');

  $effect(() => {
    const unsubscribe = subscribeAuth((state) => {
      authState = state;
    });
    return unsubscribe;
  });

  // Page title
  const pageTitle = $derived(categoryName || 'Tasks');
  const isBacklogContext = $derived(Boolean(projectId && !categoryId));

  // Task list state
  let tasks = $state<TaskSummary[]>([]);
  let loading = $state(false);
  let loadingMore = $state(false);
  let searchQuery = $state('');
  let pageNumber = $state(1);
  let hasMore = $state(false);
  const TASKS_PAGE_SIZE = 20;
  let latestTasksRequestId = $state(0);

  // Selected task
  let selectedTaskId = $state<string | null>(null);
  let currentTask = $state<Task | null>(null);
  let loadingTask = $state(false);

  // Panel mode
  let panelMode = $state<PanelMode>('view');
  let formResetKey = $state(0);

  // Member candidates for assignee Combobox
  let memberCandidates = $state<ComboboxOption[]>([]);

  // Filtered tasks
  const filteredTasks = $derived.by(() => {
    if (!searchQuery.trim()) return tasks;
    const q = searchQuery.toLowerCase();
    return tasks.filter(
      (t) => t.title.toLowerCase().includes(q) || (t.assignee?.name ?? '').toLowerCase().includes(q)
    );
  });

  // Tab State Persistence
  interface TaskListTabState {
    searchQuery: string;
    selectedTaskId: string | null;
    panelMode: PanelMode;
  }

  const tabStateKey = $derived(tabId || `task-list-${categoryId || projectId}`);
  let restoredKey = $state<string | null>(null);
  let isRestored = $state(false);

  $effect(() => {
    if (!tabStateKey || restoredKey === tabStateKey) return;
    const saved = getTabState<TaskListTabState>(tabStateKey);
    if (saved) {
      searchQuery = saved.searchQuery ?? '';
      selectedTaskId = saved.selectedTaskId ?? null;
      panelMode = saved.panelMode ?? 'view';
    }
    restoredKey = tabStateKey;
    isRestored = true;
  });

  $effect(() => {
    if (!tabStateKey || !isRestored) return;
    setTabState<TaskListTabState>(tabStateKey, {
      searchQuery,
      selectedTaskId,
      panelMode,
    });
  });

  // Load tasks and members on mount
  $effect(() => {
    if (tenantId) {
      untrack(() => {
        loadTasks();
        loadMembers();
      });
    }
  });

  async function loadTasks(reset: boolean = true) {
    if (!tenantId) return;
    if (!reset && (loading || loadingMore || !hasMore)) return;

    const requestId = ++latestTasksRequestId;
    const nextPageNumber = reset ? 1 : pageNumber + 1;

    if (reset) {
      loading = true;
      pageNumber = 1;
      hasMore = false;
    } else {
      loadingMore = true;
    }

    try {
      const options: {
        projectId?: string;
        myTasksOnly?: boolean;
        page?: number;
        pageSize?: number;
        categoryId?: string;
      } = {
        page: nextPageNumber,
        pageSize: TASKS_PAGE_SIZE,
      };
      if (projectId) {
        options.projectId = projectId;
        options.myTasksOnly = false;
      } else {
        options.myTasksOnly = true;
      }
      if (categoryId) {
        options.categoryId = categoryId;
      }

      const result = await getTasks(tenantId, options);
      if (requestId !== latestTasksRequestId) return;

      const mapped = result.items.map((dto) => mapToTaskSummary(dto, ''));
      tasks = reset ? mapped : [...tasks, ...mapped];
      pageNumber = result.page ?? nextPageNumber;
      hasMore = result.page < result.totalPages || tasks.length < result.totalCount;
    } catch (err) {
      if (requestId !== latestTasksRequestId) return;
      console.error('Failed to load tasks:', err);
      hasMore = false;
      if (reset) {
        tasks = [];
      }
    } finally {
      if (requestId === latestTasksRequestId) {
        loading = false;
        loadingMore = false;
      }
    }
  }

  function handleTaskListScroll(e: Event): void {
    if (loading || loadingMore || !hasMore) return;

    const target = e.currentTarget as HTMLElement;
    const nearBottom = target.scrollHeight - target.scrollTop - target.clientHeight <= 100;
    if (nearBottom) {
      void loadTasks(false);
    }
  }

  async function loadMembers() {
    if (!tenantId || !projectId) return;
    try {
      const detail = await getProjectById(tenantId, projectId);
      memberCandidates = (detail.members ?? []).map((m) => ({
        value: m.userId,
        label: m.displayName,
        description: m.role,
      }));
    } catch (err) {
      console.error('Failed to load project members:', err);
      memberCandidates = [];
    }
  }

  // Load task detail when selected (prevId guard)
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
  function handleSelectTask(taskId: string) {
    selectedTaskId = taskId;
    panelMode = 'view';
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

  function handleNewTask() {
    panelMode = 'create';
    selectedTaskId = null;
    currentTask = null;
  }

  async function handleDeleteTask() {
    if (!selectedTaskId || !tenantId) return;
    if (!confirm('Are you sure you want to delete this task?')) return;

    try {
      await deleteTask(tenantId, selectedTaskId, projectId || undefined);
      tasks = tasks.filter((t) => t.id !== selectedTaskId);
      selectedTaskId = null;
      currentTask = null;
      toast.success('Task deleted');
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Failed to delete task');
    }
  }

  async function handleSaveTask(data: TaskFormData) {
    if (!tenantId) return;

    try {
      if (panelMode === 'create') {
        const taskProjectId = projectId || data.projectId || undefined;
        const request: CreateTaskRequest = {
          title: data.title,
          description: data.description,
          status: data.status,
          priority: data.priority,
          assigneeId: data.assigneeId || undefined,
          estimatedHours: data.estimatedHours || undefined,
          categoryId: categoryId || undefined,
        };
        const newTask = await createTask(tenantId, taskProjectId, request);
        toast.success('Task created successfully');
        selectedTaskId = newTask.id;
        await loadTasks();
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
        await loadTaskDetail(currentTask.id);
        await loadTasks();
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
      toast.success('Time log added');
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

  async function handleRefresh() {
    if (panelMode === 'create') {
      formResetKey++;
    } else if (panelMode === 'edit' && currentTask) {
      await loadTaskDetail(currentTask.id);
    }
    await loadTasks();
  }
</script>

<PageShell class="h-full {className}">
  <PageHeader title={pageTitle} {loading}>
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
        onclick={handleNewTask}
      />
    {/snippet}
  </PageHeader>

  <PageBody class="p-0">
    <SidebarDetailLayout
      sidebarWidth={SIDEBAR_DETAIL_LAYOUT.sidebarWidth}
      minSidebarWidth={SIDEBAR_DETAIL_LAYOUT.minSidebarWidth}
      maxSidebarWidth={SIDEBAR_DETAIL_LAYOUT.maxSidebarWidth}
    >
      {#snippet sidebar()}
        <div class="flex flex-col h-full">
          <!-- Search -->
          <div class="flex-shrink-0 flex items-center p-2 min-h-12 border-b border-[var(--color-border-primary)]">
            <div class="relative flex-1">
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

          <!-- Task List -->
          <div class="flex-1 overflow-y-auto" onscroll={handleTaskListScroll}>
            {#if filteredTasks.length === 0 && !loading}
              <div class="flex flex-col items-center justify-center py-8 text-center px-4">
                <Icon name="inbox" size="lg" class="text-[var(--color-text-tertiary)] mb-2 opacity-50" />
                <span class="text-sm text-[var(--color-text-tertiary)]">
                  {searchQuery.trim() ? 'No matching tasks' : 'No tasks yet'}
                </span>
              </div>
            {:else}
              {#each filteredTasks as task (task.id)}
                <TaskItem
                  {task}
                  selected={selectedTaskId === task.id}
                  showProject={!projectId}
                  onSelect={handleSelectTask}
                />
              {/each}
            {/if}

            {#if loadingMore}
              <div class="flex items-center justify-center py-3">
                <Spinner size="sm" />
              </div>
            {/if}
          </div>

          <!-- Footer -->
          <div class="flex-shrink-0 px-2 py-1.5 border-t border-[var(--color-border-primary)]">
            <span class="text-xs text-[var(--color-text-tertiary)]">
              {filteredTasks.length} task{filteredTasks.length !== 1 ? 's' : ''}
            </span>
          </div>
        </div>
      {/snippet}

      <!-- Main Content: Task Detail / Form / Empty -->
      {#if panelMode === 'create'}
        {#key formResetKey}
          <TaskFormPanel
            mode="create"
            projectId={projectId || currentTask?.projectId || ''}
            projectName=""
            defaultStatus={isBacklogContext ? 'Backlog' : 'ToDo'}
            allowBacklogStatus={isBacklogContext}
            {memberCandidates}
            onSave={handleSaveTask}
            onCancel={handleCancelPanel}
          />
        {/key}
      {:else if panelMode === 'edit' && currentTask}
        <TaskFormPanel
          mode="edit"
          task={currentTask}
          allowBacklogStatus={isBacklogContext || currentTask.status === 'Backlog'}
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
      {:else if currentTask}
        <TaskMetaPanel
          task={currentTask}
          projectId={projectId || currentTask?.projectId || ''}
          categoryName={!projectId ? categoryName : ''}
          loading={loadingTask}
          onEdit={handleEdit}
          onLogTime={handleLogTime}
          onDelete={handleDeleteTask}
          onClose={() => { selectedTaskId = null; currentTask = null; }}
        />
      {:else}
        <div class="flex flex-col items-center justify-center h-full text-center px-8">
          <Icon name="check-square" size="xl" class="text-[var(--color-text-tertiary)] mb-3 opacity-30" />
          <p class="text-sm text-[var(--color-text-tertiary)]">
            Select a task from the list to view details
          </p>
        </div>
      {/if}
    </SidebarDetailLayout>
  </PageBody>
</PageShell>
