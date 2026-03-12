<!-- Section: TaskSidebar — Tasks > My Tasks -->
<script lang="ts">
  import { Button, Icon, Input } from '@sddp/ui';
  import type { TaskFilterType, MyTasksStats, BacklogSummary } from '../../types';

  interface Props {
    myTasksStats: MyTasksStats;
    backlogSummary?: BacklogSummary | null;
    selectedProjectId?: string | null;
    searchQuery?: string;
    filterType?: TaskFilterType;
    viewMode?: 'myTasks' | 'backlog';
    onSearch?: (query: string) => void;
    onFilterChange?: (filter: TaskFilterType) => void;
    onViewModeChange?: (mode: 'myTasks' | 'backlog') => void;
    onSelectBacklogProject?: (projectId: string) => void;
    onNewTask?: () => void;
    class?: string;
  }

  let {
    myTasksStats,
    backlogSummary = null,
    selectedProjectId = null,
    searchQuery = '',
    filterType = 'all',
    viewMode: _viewMode = 'myTasks',
    onSearch,
    onFilterChange,
    onViewModeChange,
    onSelectBacklogProject,
    onNewTask,
    class: className = '',
  }: Props = $props();

  function handleSearchInput(e: Event) {
    const target = e.target as HTMLInputElement;
    onSearch?.(target.value);
  }
</script>

<div class="flex flex-col h-full bg-[var(--color-bg-secondary)] {className}">
  <!-- Header -->
  <div class="flex-shrink-0 p-2 border-b border-[var(--color-border-primary)]">
    <div class="flex items-center justify-between mb-3">
      <h2 class="text-sm font-semibold text-[var(--color-text-secondary)]">
        Tasks
      </h2>
      {#if onNewTask}
        <Button size="sm" variant="ghost" onclick={onNewTask}>
          <Icon name="plus" size="sm" />
          <span class="ml-1">New Task</span>
        </Button>
      {/if}
    </div>

    <!-- Search -->
    <div class="relative">
      <Icon
        name="search"
        size="sm"
        class="absolute left-2.5 top-1/2 -translate-y-1/2 text-[var(--color-text-tertiary)]"
      />
      <Input
        type="text"
        placeholder="Search tasks..."
        value={searchQuery}
        oninput={handleSearchInput}
        variant="flat"
        class="pl-8 w-full"
        size="sm"
      />
    </div>
  </div>

  <!-- MY TASKS Section -->
  <div class="flex-shrink-0 p-2 border-b border-[var(--color-border-primary)]">
    <Button
      variant="unstyled"
      class="w-full text-left text-xs font-semibold text-[var(--color-text-tertiary)] uppercase tracking-wider mb-2
        hover:text-[var(--color-text-primary)]"
      onclick={() => onViewModeChange?.('myTasks')}
    >
      MY TASKS
    </Button>

    <div>
      {#each [
        { key: 'todo' as TaskFilterType, label: 'To Do', icon: 'circle', count: myTasksStats.toDoCount },
        { key: 'inProgress' as TaskFilterType, label: 'In Progress', icon: 'loader', count: myTasksStats.inProgressCount },
        { key: 'done' as TaskFilterType, label: 'Done', icon: 'check-circle', count: myTasksStats.doneCount },
        { key: 'blocked' as TaskFilterType, label: 'Blocked', icon: 'x-circle', count: myTasksStats.blockedCount },
      ] as item (item.key)}
        {@const isSelected = filterType === item.key}
        <Button
          variant="unstyled"
          class="w-full flex items-center gap-2 px-2 py-1.5 rounded cursor-pointer border
            transition-colors duration-150
            {isSelected
              ? 'bg-[var(--color-accent-primary)]/10 border-[var(--color-accent-primary)]/30'
              : 'border-transparent hover:bg-[var(--color-bg-tertiary)]'}"
          onclick={() => onFilterChange?.(item.key)}
        >
          <Icon
            name={item.icon}
            size="sm"
            class={isSelected ? 'text-[var(--color-accent-primary)]' : 'text-[var(--color-text-tertiary)]'}
          />
          <span class="flex-1 text-left text-sm truncate text-[var(--color-text-primary)]">
            {item.label}
          </span>
          {#if item.count > 0}
            <span class="min-w-[18px] h-[18px] px-1 flex items-center justify-center text-xs text-[var(--color-text-tertiary)] rounded-full bg-[var(--color-bg-tertiary)]">
              {item.count}
            </span>
          {/if}
        </Button>
      {/each}
    </div>
  </div>

  <!-- BACKLOG Section -->
  <div class="flex-1 overflow-y-auto pt-2 pb-1">
    <div class="px-2 mb-2">
      <Button
        variant="unstyled"
        class="w-full text-left text-xs font-semibold text-[var(--color-text-tertiary)] uppercase tracking-wider
          hover:text-[var(--color-text-primary)]"
        onclick={() => onViewModeChange?.('backlog')}
      >
        BACKLOG
      </Button>
    </div>

    {#if !backlogSummary || backlogSummary.projects.length === 0}
      <div class="flex flex-col items-center justify-center py-8 text-[var(--color-text-tertiary)]">
        <Icon name="inbox" size="lg" class="mb-2 opacity-50" />
        <p class="text-sm">No projects found</p>
      </div>
    {:else}
      <div>
        {#each backlogSummary.projects as project (project.projectId)}
          {@const isSelected = selectedProjectId === project.projectId}
          <Button
            variant="unstyled"
            class="w-full text-left px-2 py-1.5 rounded-lg transition-colors flex items-center gap-2
              {isSelected
                ? 'bg-[var(--color-accent-primary)]/10'
                : 'hover:bg-[var(--color-bg-tertiary)]'}"
            onclick={() => onSelectBacklogProject?.(project.projectId)}
          >
            <Icon
              name={project.isOwner ? 'shield' : 'folder'}
              size="sm"
              class={isSelected ? 'text-[var(--color-accent-primary)]' : 'text-[var(--color-text-tertiary)]'}
            />
            <span class="flex-1 min-w-0 text-sm truncate text-[var(--color-text-primary)]">
              {project.projectName}
            </span>
            <span class="flex-shrink-0 text-xs px-1.5 py-0.5 rounded-full bg-[var(--color-bg-tertiary)] text-[var(--color-text-tertiary)]">
              {project.activeTaskCount}
            </span>
          </Button>
        {/each}
      </div>

      <!-- Backlog Summary -->
      <div class="mt-3 px-3 py-2 border-t border-[var(--color-border-primary)]">
        <div class="flex items-center justify-between text-xs text-[var(--color-text-tertiary)]">
          <span>{backlogSummary.totalProjects} projects</span>
          <span>{backlogSummary.totalTasks} tasks</span>
        </div>
      </div>
    {/if}
  </div>
</div>
