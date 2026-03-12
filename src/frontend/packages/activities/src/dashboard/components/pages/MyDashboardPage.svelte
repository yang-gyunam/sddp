<!-- Activity: Dashboard > Nav: My Dashboard (dashboard-overview) -->
<script lang="ts">
  /**
   * My Dashboard Page
   * Personal dashboard with section-based content
   * Uses split API endpoints for each section with caching
   */

  import { untrack } from 'svelte';
  import { Button, CardGrid, Icon, IconButton } from '@sddp/ui';
  import { ENTITY_SVG_ICONS, PageShell, PageHeader, PageBody, output, setActiveActivityItem, toast, getTabState, setTabState, formatDateByPreference } from '@sddp/shell';
  import { ActivityLog, SpecHealthWidget, SignOffQueueWidget, ContributionWidget, ProjectSpotlightWidget, DueDateWidget, EffortTrackerWidget } from '../sections';
  import { StatCard } from '../../../shared/components/idioms';
  import { LineChart } from '../idioms';
  import { getDashboardService } from '../../services';
  import {
    subscribeDashboard,
    setMyDashboard,
    setDashboardLoading,
    setDashboardError,
    setWidgets,
    setWidgetsLoading,
    setWidgetsError,
    setNotifications,
    setNotificationsLoading,
    markNotificationRead as storeMarkRead,
    markAllNotificationsRead as storeMarkAllRead,
  } from '../../stores/dashboard.store';
  import type { MyDashboard, MyStatistics, ActivityLogEntry as ActivityLogType, DashboardWidgets, NotificationItem } from '../../types';

  // Navigation handlers for statistics cards (forceOpen to avoid toggle)
  function navigateToTasks() {
    setActiveActivityItem('tasks', true);
  }

  function navigateToConversations() {
    setActiveActivityItem('conversations', true);
  }

  function navigateToSpecs() {
    setActiveActivityItem('projects', true);
  }

  function navigateToRequirements() {
    setActiveActivityItem('projects', true);
  }

  function navigateToEffort() {
    setActiveActivityItem('projects', true);
  }

  type DashboardSection = 'overview' | 'my-tasks' | 'recent' | 'notifications';
  type NotificationOpenDirectMessageDetail = {
    conversationId: string;
    projectId?: string | null;
    participantName?: string;
    channelStatus?: 'Active' | 'Concluded' | null;
  };

  interface Props {
    section?: DashboardSection;
    tabId?: string;
  }

  let { section: initialSection = 'overview', tabId = '' }: Props = $props();
  let section = $state<DashboardSection>('overview');

  // Tab State Persistence
  interface DashboardTabState {
    section: DashboardSection;
  }

  const tabStateKey = $derived(tabId || 'my-dashboard');
  let restoredKey = $state<string | null>(null);
  let isRestored = $state(false);

  $effect(() => {
    if (!tabStateKey || restoredKey === tabStateKey) return;
    const saved = getTabState<DashboardTabState>(tabStateKey);
    if (saved) {
      section = saved.section ?? initialSection;
    } else {
      section = initialSection;
    }
    restoredKey = tabStateKey;
    isRestored = true;
  });

  $effect(() => {
    if (!tabStateKey || !isRestored) return;
    setTabState<DashboardTabState>(tabStateKey, { section });
  });

  const dashboardService = getDashboardService();

  let myDashboard = $state<MyDashboard | null>(null);
  let loading = $state(false);

  // Section-specific data
  let overviewStats = $state<MyStatistics | null>(null);
  let recentActivities = $state<ActivityLogType[]>([]);
  let activitiesTotal = $state(0);
  let widgets = $state<DashboardWidgets | null>(null);
  let widgetsLoading = $state(false);

  // Track which sections have been loaded (cache)
  let loadedSections = $state<Set<DashboardSection>>(new Set());

  // Notifications state (connected to store)
  let notifications = $state<NotificationItem[]>([]);
  let unreadCount = $state(0);
  let notificationsLoading = $state(false);

  function getNotificationIcon(type: string): string {
    switch (type) {
      case 'task_assigned': return 'clipboard-list';
      case 'new_message': return 'message-circle';
      default: return 'bell';
    }
  }

  function getNotificationIconColor(type: string): string {
    switch (type) {
      case 'task_assigned': return 'var(--color-info-500)';
      case 'new_message': return 'var(--color-accent-primary)';
      default: return 'var(--color-text-tertiary)';
    }
  }

  function formatNotificationMeta(n: NotificationItem): string {
    const actor = n.actorName ?? '';
    const time = formatDateByPreference(n.createdAt);
    return actor ? `${actor} • ${time}` : time;
  }

  async function markAsRead(id: string) {
    const notification = notifications.find(n => n.id === id);
    if (notification && !notification.isRead) {
      // Optimistic update
      storeMarkRead(id);
      try {
        await dashboardService.markNotificationRead(id);
      } catch (err) {
        console.error('Failed to mark notification as read:', err);
      }
    }
  }

  async function markAllAsRead() {
    if (unreadCount > 0) {
      // Optimistic update
      storeMarkAllRead();
      try {
        await dashboardService.markAllNotificationsRead();
        output.info('Notifications', 'Marked all notifications as read');
      } catch (err) {
        console.error('Failed to mark all as read:', err);
      }
    }
  }

  function parseDmProjectId(entityType?: string): string | null {
    if (!entityType) return null;
    const prefixes = ['dm:', 'direct_message:'];
    const matchedPrefix = prefixes.find((prefix) => entityType.startsWith(prefix));
    if (!matchedPrefix) return null;
    const candidate = entityType.slice(matchedPrefix.length).trim();
    return candidate.length > 0 ? candidate : null;
  }

  function handleNotificationClick(notification: NotificationItem) {
    markAsRead(notification.id);

    const dmConversationId = notification.type === 'dm_invite_accepted'
      ? notification.entityId
      : null;
    if (dmConversationId && typeof window !== 'undefined') {
      const projectId = parseDmProjectId(notification.entityType);
      window.dispatchEvent(
        new CustomEvent<NotificationOpenDirectMessageDetail>('sddp:open-dm-from-notification', {
          detail: {
            conversationId: dmConversationId,
            projectId,
            participantName: notification.actorName ?? 'Direct Message',
            channelStatus: 'Active',
          },
        })
      );
      return;
    }

    // Navigate based on entityType
    const entityType = notification.entityType?.toLowerCase();
    if (entityType === 'task') {
      setActiveActivityItem('tasks', true);
    } else if (entityType === 'conversation') {
      setActiveActivityItem('conversations', true);
    } else if (entityType === 'spec') {
      setActiveActivityItem('projects', true);
    } else if (entityType === 'requirement') {
      setActiveActivityItem('projects', true);
    }
  }

  $effect(() => {
    const unsubscribe = subscribeDashboard((state) => {
      myDashboard = state.myDashboard;
      loading = state.loading;
      widgets = state.widgets;
      widgetsLoading = state.widgetsLoading;
      notifications = state.notifications;
      unreadCount = state.unreadCount;
      notificationsLoading = state.notificationsLoading;
    });
    return unsubscribe;
  });

  $effect(() => {
    // Only load if section hasn't been loaded yet
    if (!loadedSections.has(section)) {
      untrack(() => loadSectionData(section));
    }
  });

  async function loadSectionData(currentSection: DashboardSection, forceRefresh = false) {
    // Skip if already loaded and not forcing refresh
    if (!forceRefresh && loadedSections.has(currentSection)) {
      return;
    }

    try {
      setDashboardLoading(true);
      setDashboardError(null);

      switch (currentSection) {
        case 'overview': {
          // Use split endpoint for overview stats
          overviewStats = await dashboardService.getMyOverview();
          // Update store with combined data for chart
          const fullData = await dashboardService.getMyDashboard();
          setMyDashboard(fullData);
          // Load Phase 1 widgets (parallel, non-blocking)
          setWidgetsLoading(true);
          dashboardService.getMyDashboardWidgets()
            .then((w) => setWidgets(w))
            .catch((err) => {
              const msg = err instanceof Error ? err.message : 'Failed to load widgets';
              setWidgetsError(msg);
              console.error('Failed to load dashboard widgets:', err);
            });
          break;
        }

        case 'my-tasks': {
          // Tasks data comes from overview stats (reuse if already loaded)
          if (!overviewStats) {
            overviewStats = await dashboardService.getMyOverview();
          }
          // Load widgets if not already loaded from overview
          if (!widgets) {
            setWidgetsLoading(true);
            dashboardService.getMyDashboardWidgets()
              .then((w) => setWidgets(w))
              .catch((err) => {
                const msg = err instanceof Error ? err.message : 'Failed to load widgets';
                setWidgetsError(msg);
                console.error('Failed to load dashboard widgets:', err);
              });
          }
          break;
        }

        case 'recent': {
          // Use split endpoint for activities
          const activitiesResult = await dashboardService.getMyActivities(1, 20);
          recentActivities = activitiesResult.activities;
          activitiesTotal = activitiesResult.totalCount;
          // Get chart data (reuse if already loaded)
          if (!myDashboard) {
            const chartData = await dashboardService.getMyDashboard();
            setMyDashboard(chartData);
          }
          break;
        }

        case 'notifications': {
          setNotificationsLoading(true);
          const notifData = await dashboardService.getMyNotifications(1, 50);
          setNotifications(notifData.notifications, notifData.unreadCount);
          break;
        }
      }

      // Mark section as loaded
      loadedSections = new Set([...loadedSections, currentSection]);
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to load dashboard';
      setDashboardError(message);
      toast.error(message);
      console.error('Failed to load My Dashboard section:', err);
    } finally {
      setDashboardLoading(false);
    }
  }

  async function handleRefresh() {
    // Force refresh current section
    await loadSectionData(section, true);
  }

  const sectionTitles: Record<DashboardSection, string> = {
    overview: 'Overview',
    'my-tasks': 'My Tasks',
    recent: 'Recent Activity',
    notifications: 'Notifications',
  };
</script>

<PageShell>
  <PageHeader title={sectionTitles[section]} meta={section === 'notifications' && unreadCount > 0 ? `${unreadCount} unread` : undefined} {loading}>
    {#snippet actions()}
      {#if section === 'notifications' && unreadCount > 0}
        <IconButton icon="check" size="sm" variant="ghost" title="Mark all as read" onclick={markAllAsRead} />
      {/if}
      <IconButton icon="refresh-cw" size="sm" variant="ghost" title="Refresh" onclick={handleRefresh} />
    {/snippet}
  </PageHeader>
  <PageBody>
    {#if loading}
      <div class="loading-state">Loading...</div>
    {:else if section === 'overview'}
    <!-- Overview Section -->
    {#if overviewStats || myDashboard}
      {@const stats = overviewStats || myDashboard?.statistics}
      <div class="section-content mx-3">
        <CardGrid cols={4} gap="md">
          <StatCard
            title="My Conversations"
            value={stats?.conversations.total ?? 0}
            subtitle="{stats?.conversations.active ?? 0} active"
            icon={ENTITY_SVG_ICONS.conversations}
            onClick={navigateToConversations}
          />
          <StatCard
            title="My Requirements"
            value={stats?.requirements.total ?? 0}
            subtitle="{stats?.requirements.draft ?? 0} draft"
            icon={ENTITY_SVG_ICONS.requirements}
            onClick={navigateToRequirements}
          />
          <StatCard
            title="My Specs"
            value={stats?.specs.total ?? 0}
            subtitle="{stats?.specs.inReview ?? 0} in review"
            icon={ENTITY_SVG_ICONS.specs}
            onClick={navigateToSpecs}
          />
          <StatCard
            title="My Glossary"
            value={stats?.glossary?.total ?? 0}
            subtitle="{stats?.glossary?.draft ?? 0} draft"
            icon={ENTITY_SVG_ICONS.glossary}
            onClick={() => setActiveActivityItem('projects', true)}
          />
          <StatCard
            title="My Artifacts"
            value={stats?.artifacts?.total ?? 0}
            subtitle="{stats?.artifacts?.recent ?? 0} recent"
            icon={ENTITY_SVG_ICONS.artifacts}
            onClick={() => setActiveActivityItem('projects', true)}
          />
          <StatCard
            title="My Tasks"
            value={stats?.tasks.total ?? 0}
            subtitle="{stats?.tasks.toDo ?? 0} to do"
            icon={ENTITY_SVG_ICONS.tasks}
            onClick={navigateToTasks}
          />
          <StatCard
            title="My Effort"
            value="{stats?.effort.used ?? 0}h"
            subtitle="{stats?.effort.total ?? 0}h total"
            icon={ENTITY_SVG_ICONS.effort}
            onClick={navigateToEffort}
          />
        </CardGrid>

        {#if myDashboard}
          <div class="chart-section">
            <LineChart data={myDashboard.myActivity} title="My Activity (Last 30 days)" />
          </div>
        {/if}

        <!-- Phase 1 Widgets: Overview -->
        <div class="widgets-section">
          <h2 class="widgets-section-title">Insights</h2>
          {#if widgetsLoading}
            <div class="widgets-loading">Loading widgets...</div>
          {:else if widgets}
            <CardGrid cols={2} gap="md">
              <SpecHealthWidget data={widgets.specHealth} />
              <SignOffQueueWidget data={widgets.signOffQueue} />
              <ContributionWidget data={widgets.contributionHeatmap} />
              <ProjectSpotlightWidget data={widgets.projectSpotlight} />
            </CardGrid>
          {/if}
        </div>
      </div>

    {:else}
      <div class="empty-state">No dashboard data available</div>
    {/if}

  {:else if section === 'my-tasks'}
    <!-- My Tasks Section -->
    <div class="section-content mx-3">
      {#if overviewStats || myDashboard}
        {@const stats = overviewStats || myDashboard?.statistics}
        <CardGrid cols={4} gap="md">
          <StatCard
            title="Total Tasks"
            value={stats?.tasks.total ?? 0}
            subtitle=""
            icon={ENTITY_SVG_ICONS.tasks}
                      />
          <StatCard
            title="To Do"
            value={stats?.tasks.toDo ?? 0}
            subtitle=""
            icon="circle"
          />
          <StatCard
            title="In Progress"
            value={stats?.tasks.inProgress ?? 0}
            subtitle=""
            icon="loader"
          />
          <StatCard
            title="Done"
            value={stats?.tasks.done ?? 0}
            subtitle=""
            icon="check-circle"
          />
        </CardGrid>

        <!-- Phase 1 Widgets: My Tasks -->
        {#if widgetsLoading}
          <div class="widgets-loading">Loading task widgets...</div>
        {:else if widgets}
          <div class="widgets-section">
            <DueDateWidget data={widgets.dueDateTimeline} />
          </div>
          <div class="widgets-section">
            <EffortTrackerWidget data={widgets.effortTracker} />
          </div>
        {/if}

        <div class="task-list-section">
          <h2>Task List</h2>
          <div class="placeholder-content">
            <p>Task list will be displayed here.</p>
            <p class="hint">Click on a task to view details or update status.</p>
          </div>
        </div>
      {:else}
        <div class="empty-state">No task data available</div>
      {/if}
    </div>

  {:else if section === 'recent'}
    <!-- Recent Activity Section -->
    <div class="section-content mx-3">
      {#if recentActivities.length > 0 || myDashboard}
        {#if myDashboard}
          <div class="chart-section">
            <LineChart data={myDashboard.myActivity} title="Activity Trend (Last 30 days)" />
          </div>
        {/if}

        <div class="activities-section">
          <h2>All Recent Activities {#if activitiesTotal > 0}({activitiesTotal}){/if}</h2>
          <ActivityLog activities={recentActivities.length > 0 ? recentActivities : (myDashboard?.recentActivities ?? [])} />
        </div>
      {:else}
        <div class="empty-state">No activity data available</div>
      {/if}
    </div>

  {:else if section === 'notifications'}
    <!-- Notifications Section -->
    <div class="section-content mx-3">
      {#if notificationsLoading}
        <div class="loading-state">Loading notifications...</div>
      {:else if notifications.length === 0}
        <div class="empty-state">No notifications</div>
      {:else}
        <div class="notification-list">
          {#each notifications as notification (notification.id)}
            <Button
              variant="unstyled"
              class="notification-item {notification.isRead ? '' : 'unread'}"
              onclick={() => handleNotificationClick(notification)}
            >
              <div class="notification-icon">
                <Icon name={getNotificationIcon(notification.type)} size="md" style="color: {getNotificationIconColor(notification.type)}" />
              </div>
              <div class="notification-content">
                <div class="notification-title">{notification.title}</div>
                <div class="notification-meta">{formatNotificationMeta(notification)}</div>
              </div>
              {#if !notification.isRead}
                <div class="notification-actions">
                  <IconButton
                    icon="check"
                    title="Mark as read"
                    onclick={(e) => { e.stopPropagation(); markAsRead(notification.id); }}
                  />
                </div>
              {/if}
            </Button>
          {/each}
        </div>
      {/if}
    </div>
  {/if}
  </PageBody>
</PageShell>

<style>
  .loading-state {
    padding: var(--space-16);
    text-align: center;
    color: var(--color-text-secondary);
  }

  .activities-section h2,
  .task-list-section h2 {
    margin: 0 0 var(--space-3);
    font-size: var(--text-xl);
    font-weight: var(--font-semibold);
  }

  .section-content {
    display: flex;
    flex-direction: column;
    gap: var(--space-3);
    padding-top: var(--space-3);
  }

  .widgets-section-title {
    margin: 0 0 var(--space-3);
    font-size: var(--text-xl);
    font-weight: var(--font-semibold);
  }

  .widgets-loading {
    padding: var(--space-8);
    text-align: center;
    color: var(--color-text-tertiary);
    font-size: var(--text-sm);
  }

  .empty-state {
    padding: var(--space-16);
    text-align: center;
    color: var(--color-text-tertiary);
  }

  .placeholder-content {
    padding: var(--space-12);
    text-align: center;
    background: var(--color-bg-secondary);
    border: 1px dashed var(--color-border);
    border-radius: var(--radius-lg);
  }

  .placeholder-content p {
    margin: 0;
    color: var(--color-text-secondary);
  }

  .placeholder-content .hint {
    margin-top: var(--space-2);
    font-size: var(--text-sm);
    color: var(--color-text-tertiary);
  }

  /* Notifications styles */
  .notification-list {
    display: flex;
    flex-direction: column;
    gap: var(--space-3);
  }

  :global(.notification-item) {
    display: flex;
    align-items: center;
    gap: var(--space-4);
    width: 100%;
    padding: var(--space-4);
    background: var(--color-bg-secondary);
    border: 1px solid var(--color-border);
    border-radius: var(--radius-lg);
    transition: background var(--transition-fast);
    cursor: pointer;
    text-align: left;
  }

  :global(.notification-item:hover) {
    background: var(--color-bg-tertiary);
  }

  :global(.notification-item:hover .notification-actions) {
    opacity: 1;
  }

  :global(.notification-item.unread) {
    background: color-mix(in srgb, var(--color-accent-primary) 5%, var(--color-bg-secondary));
    border-left: 3px solid var(--color-accent-primary);
  }

  .notification-actions {
    opacity: 0;
    transition: opacity var(--transition-fast);
    flex-shrink: 0;
  }

  .notification-icon {
    display: flex;
    align-items: center;
    justify-content: center;
    width: var(--space-8);
    height: var(--space-8);
    flex-shrink: 0;
  }

  .notification-content {
    flex: 1;
  }

  .notification-title {
    font-size: var(--text-sm);
    color: var(--color-text-primary);
    margin-bottom: var(--space-1);
  }

  .notification-meta {
    font-size: var(--text-xs);
    color: var(--color-text-tertiary);
  }
</style>
