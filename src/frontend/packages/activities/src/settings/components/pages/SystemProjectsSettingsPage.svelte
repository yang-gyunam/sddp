<!-- Activity: Settings > Nav: System Projects (settings-system-projects) -->
<script lang="ts">
  /**
   * System Projects Settings Page
   * Admin page for managing all projects — SidebarDetailLayout
   * Left sidebar: search + project list
   * Right main: detail/edit/create panel
   */

  import { untrack } from 'svelte';
  import { ProjectDetailPanel } from '../sections';
  import { toast, getAuthState, subscribeAuth, PageHeader, PageShell, SidebarDetailLayout, Dropdown, RichListItem, getTabState, setTabState } from '@sddp/shell';
  import { Icon, Input, IconButton, Button, Spinner } from '@sddp/ui';
  import type { Project, ProjectDetail } from '../../../projects/types/project.types';
  import { getProjectService } from '../../../projects/services/ProjectService';

  interface Props {
    tenantId?: string;
    tabId?: string;
  }

  let { tenantId = '', tabId = '' }: Props = $props();

  // If tenantId is not passed as prop, get from auth store
  let effectiveTenantId = $state('');

  $effect(() => {
    if (!tenantId) {
      effectiveTenantId = getAuthState().user?.tenantId || '';
      const unsubscribe = subscribeAuth((state) => {
        effectiveTenantId = state.user?.tenantId || '';
      });
      return unsubscribe;
    } else {
      effectiveTenantId = tenantId;
    }
  });

  const projectService = getProjectService();

  let projects = $state<Project[]>([]);
  let loading = $state(false);
  let loadingMore = $state(false);
  let error = $state<string | null>(null);
  let searchQuery = $state('');
  let pageNumber = $state(1);
  let hasMore = $state(false);
  const PAGE_SIZE = 20;
  let latestProjectsRequestId = $state(0);

  // Panel state
  type PanelMode = 'view' | 'edit' | 'create';
  let selectedProjectId = $state<string | null>(null);
  let selectedProject = $state<ProjectDetail | null>(null);
  let panelMode = $state<PanelMode | null>(null);
  let loadingDetail = $state(false);

  const showPanel = $derived(panelMode !== null);

  const filteredProjects = $derived(
    projects.filter((p) => {
      const query = searchQuery.toLowerCase();
      return (
        p.name.toLowerCase().includes(query) ||
        p.code.toLowerCase().includes(query)
      );
    })
  );

  // Tab State Persistence
  interface ProjectsTabState {
    searchQuery: string;
    selectedProjectId: string | null;
    panelMode: PanelMode | null;
  }

  const tabStateKey = $derived(tabId || 'settings-system-projects');
  let restoredKey = $state<string | null>(null);
  let isRestored = $state(false);

  $effect(() => {
    if (!tabStateKey || restoredKey === tabStateKey) return;
    const saved = getTabState<ProjectsTabState>(tabStateKey);
    if (saved) {
      searchQuery = saved.searchQuery ?? '';
      selectedProjectId = saved.selectedProjectId ?? null;
      panelMode = saved.panelMode ?? null;
    }
    restoredKey = tabStateKey;
    isRestored = true;
  });

  $effect(() => {
    if (!tabStateKey || !isRestored) return;
    setTabState<ProjectsTabState>(tabStateKey, {
      searchQuery,
      selectedProjectId,
      panelMode,
    });
  });

  // Load projects when tenantId changes (prevLoadKey guard)
  let prevLoadKey = $state<string | null>(null);
  $effect(() => {
    if (!effectiveTenantId) return;
    if (effectiveTenantId === prevLoadKey) return;
    prevLoadKey = effectiveTenantId;
    untrack(() => loadProjects());
  });

  // Load project detail when selectedProjectId changes (prevId guard)
  let prevSelectedProjectId = $state<string | null>(null);
  $effect(() => {
    if (!selectedProjectId) {
      prevSelectedProjectId = null;
      selectedProject = null;
      return;
    }
    if (selectedProjectId === prevSelectedProjectId) return;
    prevSelectedProjectId = selectedProjectId;
    untrack(() => loadProjectDetail(selectedProjectId!));
  });

  async function loadProjects(reset: boolean = true) {
    if (!reset && (loading || loadingMore || !hasMore)) return;

    if (!effectiveTenantId) {
      error = 'Tenant ID is required';
      return;
    }

    const requestId = ++latestProjectsRequestId;
    const nextPageNumber = reset ? 1 : pageNumber + 1;

    if (reset) {
      loading = true;
      error = null;
      hasMore = false;
      pageNumber = 1;
    } else {
      loadingMore = true;
    }

    try {
      projectService.setContext(effectiveTenantId);
      const page = await projectService.getProjectsPage(nextPageNumber, PAGE_SIZE);
      if (requestId !== latestProjectsRequestId) return;

      projects = reset ? page.items : [...projects, ...page.items];
      pageNumber = page.pageNumber ?? page.page ?? nextPageNumber;
      hasMore = page.hasNextPage ?? projects.length < page.totalCount;
    } catch (err) {
      if (requestId !== latestProjectsRequestId) return;
      error = err instanceof Error ? err.message : 'Failed to load projects';
      toast.error('Failed to load projects');
      if (reset) {
        projects = [];
      }
      hasMore = false;
    } finally {
      if (requestId === latestProjectsRequestId) {
        loading = false;
        loadingMore = false;
      }
    }
  }

  async function loadProjectDetail(projectId: string) {
    loadingDetail = true;
    try {
      projectService.setContext(effectiveTenantId);
      selectedProject = await projectService.getProjectById(projectId);
    } catch {
      toast.error('Failed to load project details');
      selectedProject = null;
    } finally {
      loadingDetail = false;
    }
  }

  function selectProject(project: Project) {
    selectedProjectId = project.id;
    panelMode = 'view';
  }

  function openCreate() {
    searchQuery = '';
    selectedProjectId = null;
    selectedProject = null;
    panelMode = 'create';
  }

  function openEdit() {
    panelMode = 'edit';
  }

  function closePanel() {
    if (panelMode === 'edit') {
      panelMode = 'view';
      return;
    }
    selectedProjectId = null;
    selectedProject = null;
    panelMode = null;
  }

  function handleCreated(project: Project) {
    projects = [...projects, project];
    selectedProjectId = project.id;
    panelMode = 'view';
    // Trigger detail load via prevId guard
    prevSelectedProjectId = null;
  }

  function handleSaved(detail: ProjectDetail) {
    projects = projects.map((p) => (p.id === detail.id ? { ...p, name: detail.name, description: detail.description, status: detail.status as 'planning' | 'active' | 'concluded' | 'archived' } : p));
    selectedProject = detail;
    panelMode = 'view';
  }

  function handleSearchInput(e: Event) {
    const target = e.target as HTMLInputElement;
    searchQuery = target.value;
  }

  function handleProjectListScroll(e: Event): void {
    if (loading || loadingMore || !hasMore) return;

    const target = e.currentTarget as HTMLElement;
    const nearBottom = target.scrollHeight - target.scrollTop - target.clientHeight <= 100;
    if (nearBottom) {
      loadProjects(false);
    }
  }
</script>

<PageShell>
  {#if loading}
    <div class="flex-1 flex items-center justify-center pb-28"><Spinner size="lg" /></div>
  {:else if error}
    <div class="flex-1 flex items-center justify-center pb-28">
      <div class="p-12 text-center text-sm text-[var(--color-error-600)]">
        <p class="mb-4">{error}</p>
        <Button variant="secondary" onclick={() => loadProjects()}>Retry</Button>
      </div>
    </div>
  {:else}
  <PageHeader title="Projects" {loading}>
    {#snippet actions()}
      <IconButton icon="refresh-cw" title="Refresh" onclick={() => loadProjects()} />
      <IconButton icon="plus" title="New Project" onclick={openCreate} />
    {/snippet}
  </PageHeader>
    <SidebarDetailLayout
      showRightPanel={false}
      sidebarWidth={360}
      minSidebarWidth={280}
      maxSidebarWidth={520}
    >
      {#snippet sidebar()}
        <div class="flex flex-col h-full min-h-0 bg-[var(--color-bg-primary)]">
          <!-- Sidebar Header: Search -->
          <div class="flex-shrink-0 flex items-center p-2 min-h-12 border-b border-[var(--color-border-primary)]">
            <div class="flex items-center gap-1 w-full">
              <div class="relative flex-1">
                <Icon
                  name="search"
                  size="sm"
                  class="absolute left-2.5 top-1/2 -translate-y-1/2 text-[var(--color-text-tertiary)]"
                />
                <Input
                  type="text"
                  placeholder="Search projects..."
                  value={searchQuery}
                  oninput={handleSearchInput}
                  autocomplete="off"
                  variant="flat"
                  class="pl-8 w-full"
                  size="sm"
                />
              </div>

              <!-- Filter Dropdown -->
              <Dropdown position="bottom-right">
                {#snippet trigger()}
                  <IconButton icon="more-vertical" size="sm" variant="ghost" title="Filter options" />
                {/snippet}
                <div class="py-1 min-w-[180px]">
                  <Button
                    variant="unstyled"
                    class="w-full flex items-center gap-2 px-3 py-1.5 text-sm text-left hover:bg-[var(--color-bg-tertiary)] transition-colors"
                    onclick={() => {}}
                  >
                    <Icon name="folder" size="sm" class="text-[var(--color-text-tertiary)]" />
                    <span class="flex-1">All Projects</span>
                    <Icon name="check" size="sm" class="text-[var(--color-accent-primary)]" />
                  </Button>
                </div>
              </Dropdown>
            </div>
          </div>

          <!-- Project List -->
          <div class="flex-1 overflow-y-auto pb-1" onscroll={handleProjectListScroll}>
            {#each filteredProjects as project (project.id)}
              <RichListItem
                selected={selectedProjectId === project.id}
                title={project.name}
                meta={project.code}
                density="compact"
                onclick={() => selectProject(project)}
              >
                {#snippet leading()}
                  <div class="w-8 h-8 rounded-lg bg-[var(--color-accent-primary)] text-white flex items-center justify-center flex-shrink-0">
                    <Icon name="project" size="sm" />
                  </div>
                {/snippet}
                {#snippet footerMeta()}
                  <span class="w-1.5 h-1.5 rounded-full flex-shrink-0
                               {project.status === 'planning' ? 'bg-blue-500' : project.status === 'active' ? 'bg-green-500' : project.status === 'concluded' ? 'bg-yellow-500' : 'bg-[var(--color-text-tertiary)]'}"></span>
                {/snippet}
              </RichListItem>
            {:else}
              <div class="p-8 text-center text-sm text-[var(--color-text-tertiary)]">
                <Icon name="project" size="xl" class="mx-auto mb-2 opacity-50" />
                <p>No projects found</p>
              </div>
            {/each}

            {#if loadingMore}
              <div class="flex items-center justify-center py-3">
                <Spinner size="sm" />
              </div>
            {/if}
          </div>
        </div>
      {/snippet}

      <!-- Main Content: Detail / Edit / Create -->
      {#if showPanel}
        {#if loadingDetail && panelMode === 'view'}
          <div class="flex-1 flex items-center justify-center"><Spinner size="lg" /></div>
        {:else if panelMode === 'create'}
          <ProjectDetailPanel
            mode="create"
            project={null}
            onClose={closePanel}
            onCreated={handleCreated}
          />
        {:else if panelMode === 'edit' && selectedProject}
          <ProjectDetailPanel
            mode="edit"
            project={selectedProject}
            onClose={closePanel}
            onCreated={handleCreated}
            onSaved={handleSaved}
          />
        {:else if panelMode === 'view' && selectedProject}
          <ProjectDetailPanel
            mode="view"
            project={selectedProject}
            onClose={closePanel}
            onEdit={openEdit}
            onCreated={handleCreated}
            onSaved={handleSaved}
          />
        {/if}
      {:else}
        <div class="flex-1 flex items-center justify-center h-full">
          <div class="flex flex-col items-center gap-2 text-[var(--color-text-tertiary)]">
            <Icon name="project" size="lg" />
            <p class="text-sm">Select a project to view details</p>
            <p class="text-xs">or click + to create a new project</p>
          </div>
        </div>
      {/if}
    </SidebarDetailLayout>
  {/if}
</PageShell>
