<!-- Section: ProjectsSidebar -->
<!--
  ProjectsSidebar Component
  Main sidebar container with project sections and timeline
-->
<script lang="ts">
  import { Icon, IconButton, Input } from '@sddp/ui';
  import type {
    Project,
    ProjectTreeNode,
    TimelineEvent as TimelineEventType,
    TimelineFilter,
  } from '../../types';
  import { getProjectBadges } from '../../types';
  import ProjectSection from './ProjectSection.svelte';
  import TimelineSection from './TimelineSection.svelte';

  interface Props {
    projects: Project[];
    timeline: TimelineEventType[];
    timelineFilter?: TimelineFilter;
    timelineLoading?: boolean;
    expandedProjects?: Set<string>;
    timelineExpanded?: boolean;
    selectedNodePath?: string | null;
    searchQuery?: string;
    onProjectToggle?: (projectId: string, expanded: boolean) => void;
    onNodeSelect?: (node: ProjectTreeNode) => void;
    onTimelineToggle?: (expanded: boolean) => void;
    onTimelineEventClick?: (event: TimelineEventType) => void;
    onTimelineRefresh?: () => void;
    onSearchChange?: (query: string) => void;
    class?: string;
  }

  let {
    projects,
    timeline,
    timelineFilter = { period: 'minutes-10' },
    timelineLoading = false,
    expandedProjects = new Set<string>(),
    timelineExpanded = true,
    selectedNodePath = null,
    searchQuery = '',
    onProjectToggle,
    onNodeSelect,
    onTimelineToggle,
    onTimelineEventClick,
    onTimelineRefresh,
    onSearchChange,
    class: className = '',
  }: Props = $props();

  // Filter projects by search query
  const filteredProjects = $derived(() => {
    if (!searchQuery) return projects;
    const query = searchQuery.toLowerCase();
    return projects.filter(
      (p) => p.name.toLowerCase().includes(query) || p.code.toLowerCase().includes(query)
    );
  });

  function handleSearchInput(event: Event) {
    const target = event.target as HTMLInputElement;
    onSearchChange?.(target.value);
  }

  function handleProjectToggle(projectId: string) {
    return (expanded: boolean) => {
      onProjectToggle?.(projectId, expanded);
    };
  }
</script>

<div class="projects-sidebar flex flex-col h-full bg-[var(--color-bg-secondary)] {className}">
  <!-- Search Input -->
  <div class="search-header flex-shrink-0 p-2 border-b border-[var(--color-border)]">
    <div class="relative">
      <Icon
        name="search"
        size="sm"
        class="absolute left-2 top-1/2 -translate-y-1/2 text-[var(--color-text-tertiary)]"
      />
      <Input
        unstyled
        placeholder="Filter projects..."
        value={searchQuery}
        oninput={handleSearchInput}
        class="
          w-full pl-8 pr-3 py-1.5 text-sm
          bg-[var(--color-bg-primary)] text-[var(--color-text-primary)]
          border border-[var(--color-border)] rounded
          focus:outline-none focus:border-[var(--color-accent-primary)]
          placeholder-[var(--color-text-tertiary)]
        "
      />
      {#if searchQuery}
        <IconButton
          icon="x"
          size="sm"
          variant="ghost"
          class="absolute right-2 top-1/2 -translate-y-1/2"
          onclick={() => onSearchChange?.('')}
          title="Clear search"
        />
      {/if}
    </div>
  </div>

  <!-- Scrollable Content -->
  <div class="flex-1 overflow-y-auto min-h-0 pb-1">
    <!-- Project Sections -->
    {#if filteredProjects().length === 0}
      <div class="flex flex-col items-center justify-center py-8 text-center px-4">
        <Icon name="folder" size="lg" class="text-[var(--color-text-tertiary)] mb-2" />
        <span class="text-sm text-[var(--color-text-tertiary)]">
          {#if searchQuery}
            No projects match "{searchQuery}"
          {:else}
            No projects found
          {/if}
        </span>
      </div>
    {:else}
      {#each filteredProjects() as project (project.id)}
        <ProjectSection
          {project}
          badges={getProjectBadges(project.id)}
          expanded={expandedProjects.has(project.id)}
          {selectedNodePath}
          onToggle={handleProjectToggle(project.id)}
          {onNodeSelect}
        />
      {/each}
    {/if}

    <!-- Timeline Section -->
    <TimelineSection
      events={timeline}
      filter={timelineFilter}
      expanded={timelineExpanded}
      loading={timelineLoading}
      onToggle={onTimelineToggle}
      onEventClick={onTimelineEventClick}
      onRefresh={onTimelineRefresh}
    />
  </div>
</div>
