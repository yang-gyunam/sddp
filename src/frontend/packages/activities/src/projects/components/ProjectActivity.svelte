<!--
  ProjectActivity Component
  Screen ID: ACT-PROJ-001
  Main activity container for Projects
-->
<script lang="ts">
  import { onMount, untrack } from 'svelte';
  import type { ProjectTreeNode, ProjectPageType, TimelineEvent as TimelineEventType, ProjectDetail } from '../types';
  import {
    getFilteredProjects,
    getExpandedProjects,
    getSelectedNodePath,
    toggleProjectExpanded,
    setSelectedNodePath,
    setSearchQuery,
    loadProjectDetail,
    getTimelineEvents,
    getTimelineFilter,
    isTimelineExpanded,
    setTimelineExpanded,
    setProjects,
    subscribeProjects,
    subscribeTimeline,
  } from '../stores';
  import { getAuthState } from '@sddp/shell/auth';
  import { getProjects as fetchProjects } from '../services/ProjectService';
  import { ProjectsSidebar } from './sections';
  import {
    ProjectDashboardPage,
    ProjectConversationsPage,
    ProjectRequirementsPage,
    ProjectSpecsPage,
    ProjectTasksPage,
    ProjectGlossaryPage,
    ProjectArtifactsPage,
    ProjectEffortPage,
    ProjectTraceabilityPage,
  } from './pages';
  import {
    navigateToSpec,
    navigateToRequirement,
    navigateToConversation,
    navigateToGlossary,
    toast,
  } from '@sddp/shell';
  import { refreshTimelineFromApi } from '../actions';

  interface Props {
    class?: string;
  }

  let { class: className = '' }: Props = $props();

  // Reactive state
  let projects = $state(getFilteredProjects());
  let expandedProjects = $state(getExpandedProjects());
  let selectedNodePath = $state(getSelectedNodePath());
  let timelineEvents = $state(getTimelineEvents());
  let timelineFilter = $state(getTimelineFilter());
  let timelineExpanded = $state(isTimelineExpanded());
  let searchQuery = $state('');
  let currentProject = $state<ProjectDetail | null>(null);

  // Derived state: parse selected node path to get projectId and pageType
  const selectedProjectId = $derived.by(() => {
    if (!selectedNodePath) return null;
    // path format: /{projectId}/{pageType}
    const parts = selectedNodePath.split('/').filter(Boolean);
    return parts.length >= 1 ? parts[0] : null;
  });

  const selectedPageType = $derived.by(() => {
    if (!selectedNodePath) return null;
    // path format: /{projectId}/{pageType}
    const parts = selectedNodePath.split('/').filter(Boolean);
    return parts.length >= 2 ? (parts[1] as ProjectPageType) : null;
  });

  // Load project detail when selected project changes
  let prevProjectId = $state<string | null>(null);
  $effect(() => {
    const pid = selectedProjectId;
    if (!pid) { prevProjectId = null; return; }
    if (pid === prevProjectId) return;
    const authState = getAuthState();
    const tenantId = authState.user?.tenantId;
    if (!tenantId) return;
    prevProjectId = pid;
    untrack(() => loadProjectDetail(pid, tenantId));
  });

  // Subscribe to store changes + initial load (onMount: setup-once with cleanup)
  onMount(() => {
    // Load real project data from API
    const authState = getAuthState();
    const tenantId = authState.user?.tenantId;
    if (tenantId) {
      fetchProjects(tenantId).then((data) => {
        setProjects(data);
        // project detail
        const pid = getSelectedNodePath()?.split('/').filter(Boolean)[0];
        if (pid) {
          loadProjectDetail(pid, tenantId);
        }
      }).catch((err) => {
        console.error('Failed to load projects:', err);
      });
    }

    // Update local state
    projects = getFilteredProjects();
    expandedProjects = getExpandedProjects();
    timelineEvents = getTimelineEvents();

    const unsubscribeProjects = subscribeProjects((state) => {
      projects = getFilteredProjects();
      expandedProjects = state.expandedProjects;
      selectedNodePath = state.selectedNodePath;
      searchQuery = state.searchQuery;
      currentProject = state.currentProject;
    });

    const unsubscribeTimeline = subscribeTimeline((state) => {
      timelineEvents = state.events;
      timelineFilter = state.filter;
      timelineExpanded = state.expanded;
    });

    return () => {
      unsubscribeProjects();
      unsubscribeTimeline();
    };
  });

  // Event handlers
  function handleProjectToggle(projectId: string, _expanded: boolean) {
    toggleProjectExpanded(projectId);
  }

  function handleNodeSelect(node: ProjectTreeNode) {
    setSelectedNodePath(node.path ?? null);
  }

  function handleTimelineToggle(expanded: boolean) {
    setTimelineExpanded(expanded);
  }

  function handleTimelineEventClick(event: TimelineEventType) {
    switch (event.entityType) {
      case 'spec':
        navigateToSpec(event.entityId);
        break;
      case 'requirement':
        navigateToRequirement(event.entityId);
        break;
      case 'conversation':
        navigateToConversation(event.entityId);
        break;
      case 'glossary':
        navigateToGlossary(event.entityId);
        break;
      case 'task':
        toast.info('Task navigation not yet implemented');
        break;
      case 'artifact':
        toast.info('Artifact navigation not yet implemented');
        break;
      default:
        console.warn('Unknown entity type:', event.entityType);
    }
  }

  function handleTimelineRefresh() {
    const tenantId = getAuthState().user?.tenantId;
    if (!tenantId) return;
    refreshTimelineFromApi(tenantId);
  }

  function handleSearchChange(query: string) {
    setSearchQuery(query);
  }
</script>

<div class="project-activity flex h-full {className}">
  <!-- Sidebar -->
  <ProjectsSidebar
    {projects}
    timeline={timelineEvents}
    {timelineFilter}
    {expandedProjects}
    {timelineExpanded}
    {selectedNodePath}
    {searchQuery}
    onProjectToggle={handleProjectToggle}
    onNodeSelect={handleNodeSelect}
    onTimelineToggle={handleTimelineToggle}
    onTimelineEventClick={handleTimelineEventClick}
    onTimelineRefresh={handleTimelineRefresh}
    onSearchChange={handleSearchChange}
    class="w-64 flex-shrink-0"
  />

  <!-- Main Content Area -->
  <div class="flex-1 min-w-0 bg-[var(--color-bg-primary)] overflow-auto">
    {#if selectedProjectId && selectedPageType}
      {#if selectedPageType === 'dashboard'}
        <ProjectDashboardPage
          projectId={selectedProjectId}
        />
      {:else if selectedPageType === 'conversations'}
        <ProjectConversationsPage
          projectId={selectedProjectId}
          project={currentProject ?? undefined}
        />
      {:else if selectedPageType === 'requirements'}
        <ProjectRequirementsPage
          projectId={selectedProjectId}
          project={currentProject ?? undefined}
        />
      {:else if selectedPageType === 'specs'}
        <ProjectSpecsPage
          projectId={selectedProjectId}
          project={currentProject ?? undefined}
        />
      {:else if selectedPageType === 'glossary'}
        <ProjectGlossaryPage
          projectId={selectedProjectId}
          project={currentProject ?? undefined}
        />
      {:else if selectedPageType === 'artifacts'}
        <ProjectArtifactsPage
          projectId={selectedProjectId}
          project={currentProject ?? undefined}
        />
      {:else if selectedPageType === 'tasks'}
        <ProjectTasksPage
          projectId={selectedProjectId}
          project={currentProject ?? undefined}
        />
      {:else if selectedPageType === 'effort'}
        <ProjectEffortPage
          projectId={selectedProjectId}
          project={currentProject ?? undefined}
        />
      {:else if selectedPageType === 'traceability'}
        <ProjectTraceabilityPage
          projectId={selectedProjectId}
          project={currentProject ?? undefined}
        />
      {:else}
        <div class="flex items-center justify-center h-full text-[var(--color-text-tertiary)]">
          <p>Unknown page type: {selectedPageType}</p>
        </div>
      {/if}
    {:else}
      <div class="flex items-center justify-center h-full text-[var(--color-text-tertiary)]">
        <p>Select a page from the sidebar</p>
      </div>
    {/if}
  </div>
</div>
