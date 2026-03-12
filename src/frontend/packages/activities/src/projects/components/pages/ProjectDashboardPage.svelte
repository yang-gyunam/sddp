<!-- Activity: Projects > Nav: Dashboard (project-{id}-dashboard) | Screen ID: PRJ-DASH-001 -->
<script lang="ts">
  import { untrack } from 'svelte';
  import { Icon, Button, IconButton, CardGrid, Spinner } from '@sddp/ui';
  import { PageShell, PageHeader, PageBody, RouterService, Avatar, toast } from '@sddp/shell';
  import { getAuthState } from '@sddp/shell/auth';
  import type { ProjectDetail, OwnershipItem } from '../../types';
  import { getProjectService } from '../../services/ProjectService';
  import { StatCard } from '../../../shared/components/idioms';
  import { ActivityLog } from '../../../dashboard/components/sections';
  import type { ActivityLogEntry } from '../../../dashboard/types';
  import { loadRecentActivity, mapAuditLogDtoToActivityLogEntry } from '../../actions';
  import { subscribeConversation, getConversationStoreState } from '../../../conversations/stores';
  import OwnershipTreemap from '../sections/OwnershipTreemap.svelte';

  interface Props {
    projectId: string;
    projectName?: string;
    tabId?: string;
    class?: string;
  }

  // _tabId: accepted for tab system compatibility but unused — no persistent UI state to save
  let { projectId, projectName = '', tabId: _tabId = '', class: className = '' }: Props = $props();

  let project = $state<ProjectDetail | null>(null);
  let loading = $state(true);
  let error = $state<string | null>(null);
  let recentActivities = $state<ActivityLogEntry[]>([]);
  let activitiesLoading = $state(false);
  let ownershipItems = $state<OwnershipItem[]>([]);
  let ownershipLoading = $state(false);

  const stats = $derived(project?.statistics);
  const pageTitle = 'Dashboard';
  const pageMeta = $derived(project?.name || projectName || undefined);

  // Online users from ConversationHub presence tracking
  let onlineUsers = $state<Set<string>>(getConversationStoreState().onlineUsers);

  $effect(() => {
    const unsubscribe = subscribeConversation((state) => {
      onlineUsers = state.onlineUsers;
    });
    return unsubscribe;
  });

  function isAiMember(member: { displayName: string; role?: string }): boolean {
    return member.role === 'AI_AGENT' || member.displayName.toLowerCase().includes('ai agent');
  }

  function getMemberAvatarUrl(member: { avatarUrl?: string | null }): string | null {
    return member.avatarUrl ?? null;
  }

  function navigateToProjectPage(pageType: string) {
    RouterService.navigate(`/project/${projectId}/${pageType}`);
  }

  $effect(() => {
    untrack(() => {
      loadProject();
      loadMyRecentActivity();
      loadOwnership();
    });
  });

  async function loadProject() {
    loading = true;
    error = null;
    try {
      const authState = getAuthState();
      if (!authState.user?.tenantId) {
        throw new Error('User not authenticated or missing tenant');
      }
      const service = getProjectService();
      service.setTenantId(authState.user.tenantId);
      const detail = await service.getProjectById(projectId);
      project = detail;
    } catch (err) {
      console.error('Failed to load project:', err);
      toast.error(err instanceof Error ? err.message : 'Failed to load project data');
      error = 'Failed to load project data';
    } finally {
      loading = false;
    }
  }

  async function loadMyRecentActivity() {
    const authState = getAuthState();
    const tenantId = authState.user?.tenantId;
    const userId = authState.user?.id;
    if (!tenantId || !userId) return;

    activitiesLoading = true;
    try {
      const entries = await loadRecentActivity(tenantId, userId, projectId);
      recentActivities = entries.map((dto) =>
        mapAuditLogDtoToActivityLogEntry(dto, project?.name ?? '')
      );
    } catch {
      recentActivities = [];
    } finally {
      activitiesLoading = false;
    }
  }

  async function loadOwnership() {
    const authState = getAuthState();
    const tenantId = authState.user?.tenantId;
    if (!tenantId) return;

    ownershipLoading = true;
    try {
      const service = getProjectService();
      service.setTenantId(tenantId);
      const data = await service.getProjectOwnership(projectId);
      ownershipItems = data.items;
    } catch {
      ownershipItems = [];
    } finally {
      ownershipLoading = false;
    }
  }

  function handleRefresh() {
    loadProject();
    loadMyRecentActivity();
    loadOwnership();
  }
</script>

<PageShell class={className}>
  <PageHeader title={pageTitle} meta={pageMeta} {loading}>
    {#snippet actions()}
      <IconButton
        icon="refresh-cw"
        size="sm"
        variant="ghost"
        title="Refresh dashboard"
        onclick={handleRefresh}
      />
    {/snippet}
  </PageHeader>

  <PageBody>
    {#if loading}
      <div class="flex-1 flex items-center justify-center">
        <!-- <Spinner size="lg" /> -->
      </div>
    {:else if error}
      <div class="flex flex-col items-center justify-center h-64">
        <Icon name="alert-circle" size="lg" class="text-red-500 mb-4" />
        <p class="text-[var(--color-text-tertiary)]">{error}</p>
        <Button variant="ghost" size="sm" class="mt-4" onclick={handleRefresh}>
          Retry
        </Button>
      </div>
    {:else if project && stats}
      <div class="dashboard-content mx-3">
      <!-- Statistics Grid -->
      <CardGrid cols={4} gap="md">
        <StatCard
          title="Conversations"
          value={stats.conversations?.total ?? 0}
          subtitle="{stats.conversations?.secondary ?? 0} active"
          icon="message-square"
          onClick={() => navigateToProjectPage('conversations')}
        />
        <StatCard
          title="Requirements"
          value={stats.requirements?.total ?? 0}
          subtitle="{stats.requirements?.secondary ?? 0} pending"
          icon="file-text"
          onClick={() => navigateToProjectPage('requirements')}
        />
        <StatCard
          title="Specs"
          value={stats.specs?.total ?? 0}
          subtitle="{stats.specs?.secondary ?? 0} in review"
          icon="file-code"
          onClick={() => navigateToProjectPage('specs')}
        />
        <StatCard
          title="Glossary"
          value={stats.glossary?.total ?? 0}
          subtitle="{stats.glossary?.secondary ?? 0} pending"
          icon="book-open"
          onClick={() => navigateToProjectPage('glossary')}
        />
        <StatCard
          title="Artifacts"
          value={stats.artifacts?.total ?? 0}
          subtitle="{stats.artifacts?.secondary ?? 0} recent"
          icon="package"
          onClick={() => navigateToProjectPage('artifacts')}
        />
        <StatCard
          title="Tasks"
          value={stats.tasks?.total ?? 0}
          subtitle="{stats.tasks?.secondary ?? 0} in progress"
          icon="check-square"
          onClick={() => navigateToProjectPage('tasks')}
        />
        <StatCard
          title="Effort"
          value="{stats.effort?.total ?? 0}h"
          subtitle="{stats.effort?.secondary ?? 0} contributors"
          icon="clock"
          onClick={() => navigateToProjectPage('effort')}
        />
      </CardGrid>

      <!-- Team Members -->
      {#if project.members && project.members.length > 0}
        <div>
          <h3 class="text-sm font-semibold text-[var(--color-text-secondary)] mb-3">
            Team Members
          </h3>
          <div class="flex flex-wrap gap-2">
            {#each project.members as member (member.userId)}
              <div
                class="flex items-center gap-2 px-3 py-1.5 rounded-full bg-[var(--color-bg-tertiary)] border border-[var(--color-border)]"
              >
                <Avatar
                  name={member.displayName}
                  avatarUrl={getMemberAvatarUrl(member)}
                  isAI={isAiMember(member)}
                  size="sm"
                />
                <span class="text-sm text-[var(--color-text-primary)]">
                  {member.displayName}
                </span>
                {#if onlineUsers.has(member.userId)}
                  <span class="w-2 h-2 rounded-full bg-green-500"></span>
                {/if}
              </div>
            {/each}
          </div>
        </div>
      {/if}

      <!-- Ownership Map -->
      {#if ownershipLoading}
        <div>
          <h3 class="text-sm font-semibold text-[var(--color-text-secondary)] mb-3">
            Ownership Map
          </h3>
          <div class="flex items-center justify-center py-8">
            <Spinner size="sm" />
          </div>
        </div>
      {:else if ownershipItems.length > 0}
        <div>
          <h3 class="text-sm font-semibold text-[var(--color-text-secondary)] mb-3">
            Ownership Map
          </h3>
          <OwnershipTreemap items={ownershipItems} members={project.members} />
        </div>
      {/if}

      <!-- Recent Activity (Today) -->
      <div>
        <h3 class="text-sm font-semibold text-[var(--color-text-secondary)] mb-3">
          Recent Activity (Today)
        </h3>
        {#if activitiesLoading}
          <div class="flex items-center justify-center py-8">
            <Spinner size="sm" />
          </div>
        {:else}
          <ActivityLog activities={recentActivities} />
        {/if}
      </div>
      </div>
    {:else}
      <div class="flex flex-col items-center justify-center h-64">
        <Icon name="layout-dashboard" size="lg" class="text-[var(--color-text-tertiary)] mb-4" />
        <p class="text-[var(--color-text-tertiary)]">No statistics available</p>
      </div>
    {/if}
  </PageBody>
</PageShell>

<style>
  .dashboard-content {
    display: flex;
    flex-direction: column;
    gap: var(--space-3);
    padding-top: var(--space-3);
  }
</style>
