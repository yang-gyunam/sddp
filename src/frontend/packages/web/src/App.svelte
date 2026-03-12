<script lang="ts">
  import { onMount, onDestroy } from 'svelte';
  // @sddp/shell - App Shell components and stores
  import {
    AppLayout,
    SplitEditorGroup,
    SidebarPanels,
    CommandPalette,
    QuickSwitcher,
    ToastContainer,
    GridOverlay,
    formatTime,
    AccessDeniedPage,
    truncate,
    showProblemsPanel,
  } from '@sddp/shell';
  import {
    tabActions,
    editorGroups,
    activeGroupId,
    layoutStore,
  } from '@sddp/shell/stores';
  import type { LayoutState, ActivityPanelMap, SidebarPanelConfig } from '@sddp/shell/types';
  // @sddp/ui - Design system
  import { Icon, IconButton, Spinner } from '@sddp/ui';
  // @sddp/auth - Authentication
  import { LoginScreen, ForcePasswordChangePage } from '@sddp/shell/auth/pages';
  import { authStore, refreshToken, logout, hasSession, setLoading, clearRequirePasswordChange, getAuthState } from '@sddp/shell/auth/stores';
  import type { AuthState } from '@sddp/shell/auth/types';
  import {
    conversationStoreInstance,
    getConversationStoreState,
    setChannels,
    setChannelsLoading,
    getConversationService,
    loadGlobalConversationsSidebar,
    setSelectedConversation,
    getSidebarState,
    type ConversationEntry,
  } from '@sddp/activities/conversations';
  // Router & Navigation
  import { navigationService } from '@sddp/shell/core/services';
  // Dashboard
  import {
    MyDashboardPanel,
    type DashboardMenuItem,
  } from '@sddp/activities/dashboard';
  // Settings activity
  import { SettingsPanelContent } from '@sddp/activities/settings/components/sections';
  import { canAccessSettingsProject, getVisibleSettingsProjects } from '@sddp/activities/settings/utils';
  // Conversation sidebar (full featured)
  import { ConversationSidebarPanel } from '@sddp/activities/conversations/components/sections';
  import { ConversationCreatePage, GlobalConversationsPage } from '@sddp/activities/conversations/components/pages';
  // Projects
  import {
    projectStore,
    getProjectState,
    setProjects,
    setProjectsLoading,
    setProjectsError,
    setSelectedProject,
    getProjectService,
    subscribeTimeline,
    loadTimelineByProject,
    type ProjectTreeNode,
    type ProjectPageType,
    type TimelineEvent,
  } from '@sddp/activities/projects';
  import { ProjectPanelContent, TimelinePanel } from '@sddp/activities/projects/components/sections';
  import { getProjectBadges } from '@sddp/activities/projects/types';
  // Tasks
  import {
    subscribeTask,
    getTaskState,
    setMyTasksStats,
    setBacklogSummary,
    loadCategories,
    addCategory,
  } from '@sddp/activities/task/stores';
  import { getMyTaskStats, getBacklogSummary } from '@sddp/activities/task/services/TaskService';
  import type { TaskCategory, TaskSidebarState } from '@sddp/activities/task/types';
  import { TaskCategoriesPanel, TaskBacklogPanel } from '@sddp/activities/task/components/sections';
  // Shared utilities
  import { problems } from '@sddp/shell/stores';
  // Local
  import { toggleTheme, isDarkMode } from './stores/theme.store';
  import { resetAppState, resetAppStatePreserveLayout } from './lib/resetAppState';
  import type { TreeNode } from './types';
  import {
    activityItems,
    activityBottomItems,
    activityPanels,
  } from './config/activity.config';
  import {
    resolveMenuComponent,
    type MenuContext,
  } from './config/menu-registry';
  // Hooks — orchestration composition
  import { useConversationToastRouting } from './hooks/useConversationToastRouting.svelte';
  import { useShellSelectionSync } from './hooks/useShellSelectionSync.svelte';
  import { useRealtimeBridge } from './hooks/useRealtimeBridge.svelte';
  import { useAppBootstrap } from './hooks/useAppBootstrap.svelte';

  // === State ===

  // Auth state
  let authState = $state<AuthState>({ user: null, isAuthenticated: false, isLoading: true });
  let authUnsubscribe: (() => void) | null = null;

  // App initialization state (prevents Welcome screen flicker during tab restoration)
  let appInitializing = $state(true);

  // Conversation state (sidebar lists - channels)
  let conversationState = $state(getConversationStoreState());
  let conversationUnsubscribe: (() => void) | null = null;

  // Project state (sidebar lists)
  let projectState = $state(getProjectState());
  let projectUnsubscribe: (() => void) | null = null;
  let lastProjectId = $state<string | null>(null);

  // Problems state (for status bar)
  let problemsErrorCount = $state(0);
  let problemsWarningCount = $state(0);
  let problemsUnsubscribe: (() => void) | null = null;

  // Layout state
  let layoutState = $state<LayoutState | null>(null);
  let layoutUnsubscribe: (() => void) | null = null;

  // Editor groups state
  let groups = $state($editorGroups);
  let currentActiveGroupId = $state($activeGroupId);
  let groupsUnsubscribe: (() => void) | null = null;
  let activeGroupIdUnsubscribe: (() => void) | null = null;

  // Settings: isAdmin derived from auth state
  const isAdmin = $derived(
    authState.user?.roles?.some((r: string) => r === 'Admin' || r === 'ADMIN') ?? false
  );
  const isProductOwner = $derived(
    authState.user?.roles?.some((r: string) => r === 'ProductOwner' || r === 'PRODUCT_OWNER') ?? false
  );
  const visibleSettingsProjects = $derived(
    getVisibleSettingsProjects({
      projects: projectState.projects,
      currentUserId: authState.user?.id ?? '',
      isAdmin,
      isProductOwner,
    })
  );

  // Dev tools: Grid overlay for responsive design verification
  let showGridOverlay = $state(false);

  function toggleGridOverlay() {
    showGridOverlay = !showGridOverlay;
  }

  // Clock state - synced to minute boundary
  let currentTime = $state(new Date());
  $effect(() => {
    let intervalId: ReturnType<typeof setInterval> | null = null;

    // Calculate delay until next minute boundary
    const now = new Date();
    const msUntilNextMinute = (60 - now.getSeconds()) * 1000 - now.getMilliseconds();

    // First update at next minute boundary, then every 60 seconds
    const timeoutId = setTimeout(() => {
      currentTime = new Date();
      intervalId = setInterval(() => {
        currentTime = new Date();
      }, 60000);
    }, msUntilNextMinute);

    return () => {
      clearTimeout(timeoutId);
      if (intervalId) clearInterval(intervalId);
    };
  });

  // Timeline events — connected to timeline store
  let timelineEvents = $state<TimelineEvent[]>([]);

  // Subscribe to timeline store so sidebar updates when data loads
  $effect(() => {
    const unsubscribe = subscribeTimeline((state) => {
      timelineEvents = state.events;
    });
    return unsubscribe;
  });

  // === Hook initialization ===

  const selectionSync = useShellSelectionSync();

  const realtimeBridge = useRealtimeBridge({
    onConversationSelect: handleConversationSelect,
    onDirectMessageSelect: handleDirectMessageSelect,
    onProjectNodeSelect: handleProjectNodeSelect,
    onLoadChannels: loadChannels,
    getSelectedProjectId: () => projectState.selectedProjectId ?? null,
  });

  const toastRouting = useConversationToastRouting({
    onConversationSelect: handleConversationSelect,
    onDirectMessageSelect: handleDirectMessageSelect,
    onOpenProjectDM: realtimeBridge.openProjectDirectMessageByContext,
    onOpenProjectConversation: realtimeBridge.openProjectConversationByContext,
  });

  const bootstrap = useAppBootstrap({
    onOpenTabFromPath: openTabFromPath,
    onOpenDirectMessageByContext: realtimeBridge.openDirectMessageByContext,
    onHandleDMConcludedById: realtimeBridge.handleDirectMessageConcludedById,
    onOpenTabWithComponent: openTabWithComponent,
  });

  // Cleanup references (set after auth-dependent initialization)
  let cleanupBootstrap: (() => void) | null = null;
  let cleanupToastRouting: (() => void) | null = null;
  let cleanupLifecycle: (() => void) | null = null;

  // Dynamic activity panels: projects & settings panels generated from loaded data
  const dynamicActivityPanels = $derived<ActivityPanelMap>({
    ...activityPanels,
    projects: (() => {
      const panels: SidebarPanelConfig[] = [];
      if (projectState.projects.length > 0) {
        for (const project of projectState.projects) {
          panels.push({
            id: `project-${project.id}`,
            title: project.name,
            icon: 'folder',
          });
        }
      } else if (!projectState.projectsLoading) {
        panels.push({
          id: 'no-projects',
          title: 'No Projects',
          icon: 'folder',
        });
      }
      // [Intentionally hidden] Timeline panel — role-based data policy
      // panels.push({ id: 'timeline', title: 'Timeline', icon: 'clock' });
      return panels;
    })(),
    settings: (() => {
      const panels: SidebarPanelConfig[] = [];
      // User settings panel (always)
      panels.push({ id: 'settings-user', title: 'User', icon: 'user' });
      // One panel per visible project settings scope
      if (visibleSettingsProjects.length > 0) {
        for (const project of visibleSettingsProjects) {
          panels.push({
            id: `settings-project-${project.id}`,
            title: project.name,
            icon: 'folder',
          });
        }
      }
      // System panel (admin only)
      if (isAdmin) {
        panels.push({ id: 'settings-system', title: 'System', icon: 'settings' });
      }
      return panels;
    })(),
  });

  $effect(() => {
    if (!authState.isAuthenticated || !authState.user?.tenantId) {
      lastProjectId = null;
      return;
    }

    const projectId = resolveProjectId();
    if (!projectId) {
      if (lastProjectId !== null) {
        lastProjectId = null;
        setChannels([]);
      }
      return;
    }

    if (projectId === lastProjectId) return;
    lastProjectId = projectId;
    void loadChannels(projectId);
  });


  // --- Task sidebar state ---
  let taskState = $state<TaskSidebarState>(getTaskState());
  let taskCategories = $state<TaskCategory[]>([]);
  let showCategoryInput = $state(false);

  $effect(() => {
    const unsubscribe = subscribeTask((state) => {
      taskState = state;
    });
    return unsubscribe;
  });

  // Load task sidebar data when switching to tasks activity
  let lastTaskTenantId = $state<string | null>(null);
  $effect(() => {
    if (!authState.isAuthenticated || !authState.user?.tenantId) return;
    if (currentActivity !== 'tasks') return;

    const tenantId = authState.user.tenantId;
    if (tenantId === lastTaskTenantId) return;
    lastTaskTenantId = tenantId;
    void loadTaskSidebarData(tenantId);
  });

  // === Lifecycle ===

  onMount(async () => {
    // Initialize lifecycle services (error handler, visibility sync)
    cleanupLifecycle = bootstrap.setupLifecycle();

    // Subscribe to problems store for status bar (errors + warnings)
    problemsUnsubscribe = problems.subscribe((state) => {
      problemsErrorCount = state.errorCount;
      problemsWarningCount = state.warningCount;
    });

    // Deep link redirect: /dashboard/recent → /?returnUrl=/dashboard/recent
    bootstrap.handleDeepLinkRedirect();

    // Subscribe to auth state changes
    authUnsubscribe = authStore.subscribe((state) => {
      authState = state;
    });

    // Subscribe to layout store
    layoutUnsubscribe = layoutStore.subscribe((state) => {
      layoutState = state;
    });

    conversationUnsubscribe = conversationStoreInstance.subscribe((state) => {
      conversationState = state;
    });

    projectUnsubscribe = projectStore.subscribe((state) => {
      projectState = state;

      const firstProjectId = state.projects[0]?.id ?? null;
      const selectedProjectId = state.selectedProjectId;
      const selectionIsValid = selectedProjectId
        ? state.projects.some((project) => project.id === selectedProjectId)
        : false;

      if (firstProjectId && !selectionIsValid) {
        setSelectedProject(firstProjectId);
      } else if (!firstProjectId && selectedProjectId) {
        setSelectedProject(null);
      }
    });

    // Subscribe to editor groups
    groupsUnsubscribe = editorGroups.subscribe((g) => {
      groups = g;
    });
    activeGroupIdUnsubscribe = activeGroupId.subscribe((id) => {
      currentActiveGroupId = id;
    });

    // Try to refresh token on app load (for returning users with valid refresh token cookie)
    try {
      let refreshed = false;
      if (hasSession()) {
        refreshed = await refreshToken();
      } else {
        setLoading(false);
      }
      if (refreshed) {
        // Preserve layout (sidebar position) on reload; only reset domain data
        resetAppStatePreserveLayout();

        cleanupBootstrap = bootstrap.initialize();
        cleanupToastRouting = toastRouting.start();

        // Load data before restoring returnUrl (project tabs need project names)
        await loadProjects();
        const tid = getAuthState().user?.tenantId;
        if (tid) await loadGlobalConversationsSidebar(tid);

        // Restore the URL the user originally accessed
        bootstrap.navigateToReturnUrl();

        // Replace the returnUrl history entry so browser back won't return to it
        window.history.replaceState(null, '', window.location.pathname);
      }
    } catch {
      // Token refresh failed, user needs to login
    } finally {
      // App initialization complete (auth check done, tabs restored if authenticated)
      appInitializing = false;
    }
  });

  onDestroy(() => {
    cleanupLifecycle?.();
    cleanupBootstrap?.();
    cleanupToastRouting?.();
    realtimeBridge.disconnect();
    selectionSync.destroy();
    authUnsubscribe?.();
    layoutUnsubscribe?.();
    conversationUnsubscribe?.();
    projectUnsubscribe?.();
    problemsUnsubscribe?.();
    groupsUnsubscribe?.();
    activeGroupIdUnsubscribe?.();
  });

  // === Functions ===

  // Handle successful login
  async function handleLoginSuccess() {
    // Ensure clean slate (in case of stale state from previous session)
    resetAppState();

    cleanupBootstrap = bootstrap.initialize();
    cleanupToastRouting = toastRouting.start();

    // Load data before restoring returnUrl (project tabs need project names)
    await loadProjects();
    const tid = getAuthState().user?.tenantId;
    if (tid) await loadGlobalConversationsSidebar(tid);

    // Restore the URL the user originally tried to access
    bootstrap.navigateToReturnUrl();

    // Replace the login screen history entry so browser back won't return to it
    window.history.replaceState(null, '', window.location.pathname);
  }

  // Handle logout
  async function handleLogout() {
    await logout();

    // Clean up auth-dependent hooks
    cleanupBootstrap?.();
    cleanupBootstrap = null;
    cleanupToastRouting?.();
    cleanupToastRouting = null;
    realtimeBridge.disconnect();

    // Reset all application state (tabs, stores, services)
    resetAppState();

    // Always navigate to root — previous user's path should not carry over
    window.history.replaceState(null, '', '/');
  }

  function resolveProjectId(projectIdOverride?: string | null): string | null {
    if (projectIdOverride) return projectIdOverride;
    if (projectState.selectedProjectId) return projectState.selectedProjectId;
    return projectState.projects[0]?.id ?? null;
  }

  // Load channels from API
  async function loadChannels(projectIdOverride?: string | null) {
    const user = authStore.get().user;
    const projectId = resolveProjectId(projectIdOverride);
    if (!user?.tenantId || !projectId) {
      setChannels([]);
      return;
    }

    const service = getConversationService();
    service.setContext(user.tenantId, projectId);

    setChannelsLoading(true);
    try {
      const channels = await service.getChannelList();
      setChannels(channels);
    } catch (error) {
      console.error('Failed to load channels:', error);
    }
  }

  // Load projects from API
  async function loadProjects() {
    const user = authStore.get().user;
    if (!user?.tenantId) return;

    const service = getProjectService();
    service.setTenantId(user.tenantId);

    setProjectsLoading(true);
    try {
      const projects = await service.getProjectsWithBadges();
      setProjects(projects);

      // Load timeline for all projects in this tenant
      loadTimelineByProject(user.tenantId).catch((err) => {
        console.error('Failed to load timeline:', err);
      });

      // Connect SignalR hubs for real-time updates
      realtimeBridge.connect();
    } catch (error) {
      console.error('Failed to load projects:', error);
      setProjectsError('Failed to load projects');
    } finally {
      setProjectsLoading(false);
    }
  }

  // Handle sidebar header refresh button click
  async function handleSidebarRefresh() {
    switch (currentActivity) {
      case 'dashboard':
        await loadProjects();
        break;
      case 'projects':
        await loadProjects();
        break;
      case 'conversations': {
        const tid = authStore.get().user?.tenantId;
        if (tid) await loadGlobalConversationsSidebar(tid);
        break;
      }
      case 'tasks':
        lastTaskTenantId = '';
        void loadTaskSidebarData(authStore.get().user?.tenantId || '');
        break;
      case 'settings':
        await loadProjects();
        break;
      default:
        break;
    }
  }

  async function loadTaskSidebarData(tenantId: string) {
    try {
      const [stats, summary] = await Promise.all([
        getMyTaskStats(tenantId),
        getBacklogSummary(tenantId),
      ]);
      setMyTasksStats(stats);
      setBacklogSummary(summary);
      // Load categories from API
      taskCategories = await loadCategories(tenantId);
    } catch (err) {
      console.error('Failed to load task sidebar data:', err);
    }
  }

  function handleCategorySelect(category: TaskCategory) {
    selectionSync.selectedCategoryId = category.id;
    selectionSync.selectedBacklogProjectId = null;
    setSelectedProject(null);
    openTabWithComponent(
      category.name,
      'tag',
      `/tasks/category/${category.id}`,
      `tasks-category-${category.id}`
    );
  }

  async function handleCategoryCreate(name: string) {
    const tenantId = authState.user?.tenantId;
    if (!tenantId) return;
    const category = await addCategory(tenantId, name);
    if (!category) return;
    taskCategories = [...taskCategories, category];
    showCategoryInput = false;
    handleCategorySelect(category);
  }

  function handleBacklogProjectSelect(projectId: string) {
    selectionSync.selectedCategoryId = null;
    selectionSync.selectedBacklogProjectId = projectId;
    setSelectedProject(projectId);
    const project = taskState.backlogSummary?.projects.find(p => p.projectId === projectId);
    const projectName = project?.projectName ?? 'Backlog';
    openTabWithComponent(
      projectName,
      'folder',
      `/tasks/backlog/${projectId}`,
      `tasks-backlog-${projectId}`
    );
  }

  // Menu context for component resolution
  // Uses tenantId from authenticated user, projectId from project selection
  let menuContext = $derived<MenuContext>({
    tenantId: authState.user?.tenantId ?? '',
    projectId: resolveProjectId() ?? '',
  });

  /**
   * Opens a tab with component resolution from menu registry
   */
  function openTabWithComponent(
    title: string,
    icon: string,
    path: string,
    menuId: string,
    options?: { meta?: string; additionalProps?: Record<string, unknown> }
  ) {
    // Create a menu-like node for resolution
    const menuNode: TreeNode = { id: menuId, label: title, icon };
    const resolved = resolveMenuComponent(menuNode, menuContext);

    tabActions.createTab({
      title,
      meta: options?.meta,
      icon,
      dirty: false,
      closable: true,
      component: resolved?.component ?? null,
      props: { ...(resolved?.props ?? {}), ...(options?.additionalProps ?? {}) },
      path,
      menuId,
    });
  }

  /**
   * Opens an Access Denied tab when the user lacks permission
   */
  function openAccessDeniedTab(
    title: string,
    icon: string,
    path: string,
    menuId: string,
    message: string,
    options?: { deniedIcon?: string }
  ) {
    tabActions.createTab({
      title,
      icon,
      dirty: false,
      closable: true,
      component: AccessDeniedPage,
      props: { icon: options?.deniedIcon ?? 'lock', message },
      path,
      menuId,
    });
  }

  /**
   * Open a tab from a URL path. Handles all path types:
   * - /project/{id}/{nodeType}  → project page tab (editable, within project context)
   * - /dashboard/{key}          → dashboard tab
   * - /spec/{id}, /conversation/{id}, /requirement/{id}, etc. → entity tab (always readonly)
   *
   * Entity tabs opened via URL path are ALWAYS readonly.
   * Editable entity views only exist within project context pages (sidebar + detail panel).
   */
  function openTabFromPath(path: string): boolean {
    const canonicalPath = navigationService.normalizePath(path);

    // Project paths: /project/{projectId}/{nodeType}
    const projectMatch = canonicalPath.match(/^\/project\/([^/]+)\/(.+)$/);
    if (projectMatch && projectMatch[1] && projectMatch[2]) {
      const targetProjectId = projectMatch[1];

      // Validate user has access to this project
      const hasAccess = projectState.projects.some(p => p.id === targetProjectId);
      if (!hasAccess) {
        openAccessDeniedTab(
          'Access Denied',
          'lock',
          canonicalPath,
          `access-denied-${targetProjectId}`,
          'You are not a member of this project. Contact a project administrator to request access.'
        );
        return true;
      }

      handleProjectNodeSelect(targetProjectId, projectMatch[2] as ProjectPageType);
      return true;
    }

    // Task paths: /tasks/my-tasks, /tasks/category/{id}, /tasks/backlog/{projectId}
    if (canonicalPath === '/tasks/my-tasks') {
      openTabWithComponent('My Tasks', 'check-square', canonicalPath, 'tasks-my-tasks');
      return true;
    }

    const taskCategoryMatch = canonicalPath.match(/^\/tasks\/category\/(.+)$/);
    if (taskCategoryMatch && taskCategoryMatch[1]) {
      const catId = taskCategoryMatch[1];
      const cat = taskCategories.find(c => c.id === catId);
      const catName = cat?.name ?? 'Category';
      openTabWithComponent(catName, 'tag', canonicalPath, `tasks-category-${catId}`);
      return true;
    }

    const taskBacklogMatch = canonicalPath.match(/^\/tasks\/backlog\/(.+)$/);
    if (taskBacklogMatch && taskBacklogMatch[1]) {
      const backlogProjectId = taskBacklogMatch[1];
      const backlogProject = taskState.backlogSummary?.projects.find(p => p.projectId === backlogProjectId);
      const backlogName = backlogProject?.projectName ?? 'Backlog';
      openTabWithComponent(backlogName, 'folder', canonicalPath, `tasks-backlog-${backlogProjectId}`);
      return true;
    }

    // Dashboard paths: /dashboard/{section}
    if (canonicalPath.startsWith('/dashboard/')) {
      const section = canonicalPath.replace('/dashboard/', '');
      const dashboardConfig: Record<string, { label: string; icon: string }> = {
        overview: { label: 'Overview', icon: 'layout-dashboard' },
        'my-tasks': { label: 'My Tasks', icon: 'check-square' },
        recent: { label: 'Recent Activity', icon: 'clock' },
        notifications: { label: 'Notifications', icon: 'bell' },
      };
        const config = dashboardConfig[section];
        if (config) {
        openTabWithComponent(config.label, config.icon, canonicalPath, `dashboard-${section}`);
        return true;
      }
      return false;
    }

    // Settings paths: /settings/{section}[/{subsection}]
    if (canonicalPath.startsWith('/settings/')) {
      const settingsPath = canonicalPath.replace('/settings/', '');

      // User settings: /settings/profile, /settings/preferences, /settings/notifications
      const userSettingsConfig: Record<string, { title: string; icon: string }> = {
        profile: { title: 'Profile', icon: 'account' },
        preferences: { title: 'Preferences', icon: 'sliders' },
        notifications: { title: 'Notifications', icon: 'bell' },
      };
      if (userSettingsConfig[settingsPath]) {
        const cfg = userSettingsConfig[settingsPath];
        openTabWithComponent(cfg.title, cfg.icon, canonicalPath, `settings-${settingsPath}`);
        return true;
      }

      // System settings: /settings/system/{type}
      const systemMatch = settingsPath.match(/^system\/(.+)$/);
      if (systemMatch && systemMatch[1]) {
        const systemType = systemMatch[1];
        const systemConfig: Record<string, { title: string; icon: string }> = {
          dashboard: { title: 'Dashboard', icon: 'bar-chart' },
          users: { title: 'Users', icon: 'user' },
          projects: { title: 'Projects', icon: 'project' },
          config: { title: 'System Config', icon: 'settings' },
          audit: { title: 'Audit Logs', icon: 'file-text' },
          health: { title: 'Health', icon: 'heart' },
        };
        const cfg = systemConfig[systemType];
        if (cfg) {
          if (!isAdmin) {
            openAccessDeniedTab(
              cfg.title,
              cfg.icon,
              canonicalPath,
              `settings-system-${systemType}`,
              'This page requires administrator privileges.',
              { deniedIcon: 'shield' }
            );
            return true;
          }
          openTabWithComponent(cfg.title, cfg.icon, canonicalPath, `settings-system-${systemType}`);
          return true;
        }
      }

      // Project settings: /settings/project/{projectId}/{type}
      const projectSettingsMatch = settingsPath.match(/^project\/([^/]+)\/(.+)$/);
      if (projectSettingsMatch && projectSettingsMatch[1] && projectSettingsMatch[2]) {
        const projId = projectSettingsMatch[1];
        const pageType = projectSettingsMatch[2];
        const projectConfig: Record<string, { title: string; icon: string }> = {
          general: { title: 'General', icon: 'file-text' },
          members: { title: 'Members', icon: 'user' },
          roles: { title: 'Roles', icon: 'shield' },
          integrations: { title: 'Integrations', icon: 'link' },
        };
        const cfg = projectConfig[pageType];
        if (cfg) {
          const canAccessProjectSettings = canAccessSettingsProject(visibleSettingsProjects, projId);
          if (!canAccessProjectSettings) {
            openAccessDeniedTab(
              cfg.title,
              cfg.icon,
              canonicalPath,
              `settings-project-${projId}-${pageType}`,
              'Project settings are available only to admins or project product owners.'
            );
            return true;
          }
          openTabWithComponent(cfg.title, cfg.icon, canonicalPath, `settings-project-${projId}-${pageType}`);
          return true;
        }
      }

      return false;
    }

    // Entity paths: /spec/{id}, /conversation/{id}, /forum/{id}, etc.
    // All entity tabs opened via URL are ALWAYS readonly (system-enforced).
    const parsed = navigationService.parsePath(canonicalPath);
    if (!parsed) return false;

    const shortId = truncate(parsed.id, 8, '…');
    // Look up conversation name from all available data sources:
    // 1. sidebar store (tenant-wide conversations)
    // 2. conversationState.channels (project-scoped channels)
    const sidebarState = getSidebarState();
    const convEntry = [...sidebarState.channels, ...sidebarState.privateChannels, ...sidebarState.topics]
      .find(c => c.id === parsed.id);
    const channelEntry = !convEntry ? conversationState.channels?.find(c => c.id === parsed.id) : null;
    const convName = convEntry?.name || channelEntry?.topic;
    // All entity views from URL path are readonly
    const readonlyProps = { additionalProps: { readonly: true } };

    switch (parsed.type) {
      case 'spec':
        openTabWithComponent(`Spec ${shortId} (view)`, 'file-signature', canonicalPath, `spec-${parsed.id}`, readonlyProps);
        return true;
      case 'conversation':
      case 'space': { // legacy fallback
        const convTitle = convName || shortId;
        openTabWithComponent(convTitle, 'hash', canonicalPath, `conversation-${parsed.id}`, {
          additionalProps: { conversationType: 'Channel', channelStatus: convEntry?.channelStatus ?? null },
        });
        return true;
      }
      case 'forum': {
        const forumTitle = convName || shortId;
        openTabWithComponent(forumTitle, 'list', canonicalPath, `forum-${parsed.id}`);
        return true;
      }
      case 'requirement':
        openTabWithComponent(`Requirement ${shortId} (view)`, 'clipboard-list', canonicalPath, `requirement-${parsed.id}`, readonlyProps);
        return true;
      case 'glossary':
        openTabWithComponent(`Term ${shortId} (view)`, 'book-open', canonicalPath, `glossary-${parsed.id}`, readonlyProps);
        return true;
      case 'dm':
        openTabWithComponent(`@${shortId}`, 'message-circle', canonicalPath, `dm-${parsed.id}`, undefined);
        return true;
      default:
        return false;
    }
  }

  function handleConversationCreate(type: 'channel' | 'forum') {
    const label = type === 'channel' ? 'New Channel' : 'New Forum';
    const icon = type === 'channel' ? 'hash' : 'list';
    tabActions.createTab({
      title: label,
      icon,
      dirty: false,
      closable: true,
      component: ConversationCreatePage,
      props: {
        createType: type,
        onCreated: async (id: string, convType: 'Channel' | 'Forum', name: string) => {
          const tid = getAuthState().user?.tenantId;
          if (tid) await loadGlobalConversationsSidebar(tid);
          handleConversationSelect({ id, name, type: convType, isPrivate: false });
        },
      },
      path: `/conversations/create-${type}`,
      menuId: `conversations-create-${type}`,
    });
  }

  function handleCreateDM() {
    tabActions.createTab({
      title: 'New Direct Message',
      icon: 'message-square',
      dirty: false,
      closable: true,
      component: GlobalConversationsPage,
      props: {
        initialCreateMode: 'dm',
      },
      path: '/conversations/create-dm',
      menuId: 'conversations-create-dm',
    });
  }

  /**
   * Handle project node selection from ProjectPanelContent
   */
  function handleProjectNodeSelect(projectId: string, nodeType: ProjectPageType) {
    if (projectId) {
      setSelectedProject(projectId);
      selectionSync.selectedProjectNodePath = `/${projectId}/${nodeType}`;
    }

    const project = projectState.projects.find(p => p.id === projectId);
    const projectName = project?.name || '';

    const nodeConfig: Record<ProjectTreeNode['type'], { icon: string; label: string }> = {
      dashboard: { icon: 'layout-dashboard', label: 'Dashboard' },
      conversations: { icon: 'message-square', label: 'Conversations' },
      requirements: { icon: 'file-text', label: 'Requirements' },
      specs: { icon: 'file-code', label: 'Specs' },
      tasks: { icon: 'check-square', label: 'Tasks' },
      glossary: { icon: 'book-open', label: 'Glossary' },
      artifacts: { icon: 'package', label: 'Artifacts' },
      effort: { icon: 'clock', label: 'Effort' },
      traceability: { icon: 'type-hierarchy', label: 'Traceability' },
    };

    const config = nodeConfig[nodeType];
    openTabWithComponent(
      config.label,
      config.icon,
      `/project/${projectId}/${nodeType}`,
      `project-${projectId}-${nodeType}`,
      { meta: projectName, additionalProps: { projectName } }
    );
  }

  /**
   * Handle conversation selection
   * - Channel: Open chat view directly
   * - Forum: Open topic list view
   */
  function handleConversationSelect(conversation: ConversationEntry) {
    setSelectedConversation(conversation.id);

    if (conversation.type === 'Channel') {
      openTabWithComponent(
        conversation.name,
        'hash',
        `/conversation/${conversation.id}`,
        `conversation-${conversation.id}`,
        {
          additionalProps: {
            projectId: conversation.projectId ?? '',
            conversationType: 'Channel',
            channelStatus: conversation.channelStatus ?? null,
          },
        }
      );
    } else {
      openTabWithComponent(
        conversation.name,
        'list',
        `/forum/${conversation.id}`,
        `forum-${conversation.id}`,
        { additionalProps: { projectId: conversation.projectId ?? '' } }
      );
    }
  }

  function handleDirectMessageSelect(item: {
    id: string;
    name: string;
    projectId?: string;
    channelStatus?: 'Active' | 'Concluded' | null;
  }) {
    setSelectedConversation(item.id);
    openTabWithComponent(
      `@${item.name}`,
      'message-circle',
      `/dm/${item.id}`,
      `dm-${item.id}`,
      {
        additionalProps: {
          projectId: item.projectId ?? '',
          channelStatus: item.channelStatus ?? null,
          conversationType: 'DirectMessage',
        },
      }
    );
  }

  /**
   * Handle Dashboard item selection (My Dashboard / System Dashboard panels)
   */
  function handleDashboardItemSelect(item: DashboardMenuItem) {
    selectionSync.selectedDashboardItemId = item.id;
    openTabWithComponent(
      item.label,
      item.icon,
      `/dashboard/${item.section}`,
      item.id
    );
  }

  const panelActions = {
    'my-tasks': [
      {
        id: 'new-category',
        icon: 'plus',
        label: 'New Category',
        onClick: () => { showCategoryInput = true; },
      },
    ],
  };

  let panelLoading = $derived({
    conversations: conversationState.channelsLoading,
  });

  // Status bar items
  // Left: user,,
  // Right:,, Sign Out
  let statusItems = $derived([
    {
      id: 'version',
      text: 'v0.1.0',
      icon: 'info',
      position: 'left' as const,
      tooltip: 'SDDP v0.1.0',
    },
    {
      id: 'user',
      text: authState.user?.displayName || authState.user?.username || '',
      icon: 'user',
      position: 'left' as const,
    },
    {
      id: 'errors',
      text: String(problemsErrorCount),
      icon: 'alert-circle',
      position: 'left' as const,
      tooltip: `${problemsErrorCount} Error(s) - Click to view Problems`,
      onClick: showProblemsPanel,
    },
    {
      id: 'warnings',
      text: String(problemsWarningCount),
      icon: 'alert-triangle',
      position: 'left' as const,
      tooltip: `${problemsWarningCount} Warning(s) - Click to view Problems`,
      onClick: showProblemsPanel,
    },
    // [Intentionally hidden] info count — status bar error/warning
    // {
    //   id: 'infos',
    //   text: String(problemsInfoCount),
    //   icon: 'info',
    //   position: 'left' as const,
    //   tooltip: `${problemsInfoCount} Info(s) - Click to view Problems`,
    //   onClick: showProblemsPanel,
    // },
    // Dev tool: Grid overlay toggle (only in development)
    ...(import.meta.env.DEV ? [{
      id: 'grid-overlay',
      text: showGridOverlay ? 'Grid: ON' : 'Grid',
      icon: 'grid',
      position: 'right' as const,
      tooltip: 'Toggle grid overlay for responsive design verification',
      onClick: toggleGridOverlay,
    }] : []),
    {
      id: 'theme',
      text: isDarkMode() ? 'Dark' : 'Light',
      icon: isDarkMode() ? 'moon' : 'sun',
      position: 'right' as const,
      onClick: toggleTheme,
    },
    {
      id: 'time',
      text: formatTime(currentTime, { hour: '2-digit', minute: '2-digit' }),
      icon: 'clock',
      position: 'right' as const,
    },
    {
      id: 'logout',
      text: 'Sign Out',
      icon: 'log-out',
      position: 'right' as const,
      onClick: handleLogout,
    },
  ]);

  // Current sidebar activity
  let currentActivity = $derived(layoutState?.sidebar.activePanel ?? 'conversations');
  let currentActivityLabel = $derived(
    [...activityItems, ...activityBottomItems].find((i) => i.id === currentActivity)?.label ?? ''
  );

  // Settings activity: no auto-open tab. Each panel item opens its own tab via onOpenTab.

  // Optional tree menu items (empty until wired to real data)
  const menuItems: TreeNode[] = [];

  // Handle menu selection
  function handleMenuSelect(menu: TreeNode) {
    // Only create tab for leaf nodes (non-folders)
    if (!menu.children || menu.children.length === 0) {
      // Try to resolve component from registry
      const resolved = resolveMenuComponent(menu, menuContext);

      tabActions.createTab({
        title: menu.label,
        icon: menu.icon,
        dirty: false,
        closable: true,
        component: resolved?.component ?? null,
        props: resolved?.props ?? {},
        path: `/${menu.id}`,
      });
    }
  }
</script>

<!-- Loading Screen (auth check or app initialization in progress) -->
{#if authState.isLoading || (authState.isAuthenticated && appInitializing)}
  <div class="min-h-screen flex items-center justify-center bg-[var(--color-bg-primary)]">
    <Spinner size="xl" />
  </div>

<!-- Login Screen -->
{:else if !authState.isAuthenticated}
  <LoginScreen onLoginSuccess={handleLoginSuccess} />

<!-- Force Password Change -->
{:else if authState.requirePasswordChange}
  <ForcePasswordChangePage onChanged={clearRequirePasswordChange} />

<!-- Main Application -->
{:else}
  {#snippet dynamicPanelRenderer(panelId: string)}
    {#if panelId.startsWith('project-')}
      {@const projectId = panelId.replace('project-', '')}
      {@const project = projectState.projects.find(p => p.id === projectId)}
      {#if project}
        <ProjectPanelContent
          {project}
          badges={getProjectBadges(project.id)}
          selectedNodePath={selectionSync.selectedProjectNodePath}
          onNodeSelect={handleProjectNodeSelect}
        />
      {/if}
    {:else if panelId === 'no-projects'}
      <div class="flex flex-col items-center justify-center py-8 text-center px-4">
        <Icon name="folder" size="lg" class="text-[var(--color-text-tertiary)] mb-2" />
        <span class="text-sm text-[var(--color-text-tertiary)]">
          No projects assigned
        </span>
      </div>
    {:else if panelId === 'settings-user'}
      <SettingsPanelContent
        panelType="user"
        selectedId={selectionSync.selectedSettingsItemId}
        onOpenTab={(config) => openTabWithComponent(config.title, config.icon, config.path, config.menuId)}
      />
    {:else if panelId.startsWith('settings-project-')}
      {@const settingsProjectId = panelId.replace('settings-project-', '')}
      {@const settingsProject = visibleSettingsProjects.find(p => p.id === settingsProjectId)}
      <SettingsPanelContent
        panelType="project"
        projectId={settingsProjectId}
        projectName={settingsProject?.name ?? 'Project'}
        selectedId={selectionSync.selectedSettingsItemId}
        onOpenTab={(config) => openTabWithComponent(config.title, config.icon, config.path, config.menuId)}
      />
    {:else if panelId === 'settings-system'}
      <SettingsPanelContent
        panelType="system"
        selectedId={selectionSync.selectedSettingsItemId}
        onOpenTab={(config) => openTabWithComponent(config.title, config.icon, config.path, config.menuId)}
      />
    {/if}
  {/snippet}

  {#snippet timelinePanelSnippet()}
    <TimelinePanel events={timelineEvents} />
  {/snippet}

  {#snippet myTasksPanel()}
    <TaskCategoriesPanel
      categories={taskCategories}
      selectedCategoryId={selectionSync.selectedCategoryId}
      showInput={showCategoryInput}
      onCategorySelect={handleCategorySelect}
      onCategoryCreate={handleCategoryCreate}
      onCancelInput={() => { showCategoryInput = false; }}
    />
  {/snippet}

  {#snippet backlogPanel()}
    <TaskBacklogPanel
      backlogSummary={taskState.backlogSummary}
      selectedProjectId={selectionSync.selectedBacklogProjectId}
      onProjectSelect={handleBacklogProjectSelect}
    />
  {/snippet}

  {#snippet myDashboardPanel()}
    <MyDashboardPanel
      selectedId={selectionSync.selectedDashboardItemId}
      onSelect={handleDashboardItemSelect}
    />
  {/snippet}

 <!-- ConnectionBanner disabled: status Bottom Panel Problems warning/info -->
  <!-- <ConnectionBanner
    visible={showConnectionBanner}
    message={connectionBannerMessage}
    variant={connectionBannerVariant}
  /> -->

  <AppLayout
    {activityItems}
    {activityBottomItems}
    sidebarTitle={currentActivityLabel}
    {statusItems}
  >
    {#snippet sidebarHeaderActions()}
      <IconButton icon="refresh-cw" title="Refresh" onclick={handleSidebarRefresh} />
    {/snippet}

    {#snippet sidebarContent()}
      {#if currentActivity === 'conversations'}
        <ConversationSidebarPanel
          tenantId={authState.user?.tenantId ?? ''}
          onConversationSelect={handleConversationSelect}
          onDirectMessageSelect={(dm) => handleDirectMessageSelect(dm)}
          onCreateChannel={() => handleConversationCreate('channel')}
          onCreateForum={() => handleConversationCreate('forum')}
          onCreateDM={handleCreateDM}
        />
      {:else}
        <SidebarPanels
          activity={currentActivity}
          activityPanels={dynamicActivityPanels}
          panelContents={{
            'my-dashboard': myDashboardPanel,
            timeline: timelinePanelSnippet,
            'my-tasks': myTasksPanel,
            backlog: backlogPanel,
          }}
          panelContentRenderer={dynamicPanelRenderer}
          panelActions={panelActions}
          panelLoading={panelLoading}
          menus={menuItems}
          selectedMenuId={selectionSync.selectedMenuId}
          onMenuSelect={handleMenuSelect}
        />
      {/if}
    {/snippet}

    {#snippet mainContent()}
      <SplitEditorGroup
        {groups}
        activeGroupId={currentActiveGroupId}
        class="h-full"
      />
    {/snippet}
  </AppLayout>

  <!-- Command Palette -->
  <CommandPalette />

  <!-- Quick Switcher (Ctrl+Tab) -->
  <QuickSwitcher bind:visible={bootstrap.quickSwitcherVisible} />

  <!-- Toast Notifications -->
  <ToastContainer position="top-right" />

  <!-- Dev Tool: Grid Overlay for responsive design verification -->
  {#if showGridOverlay}
    <GridOverlay cols={4} gap="md" />
  {/if}
{/if}
