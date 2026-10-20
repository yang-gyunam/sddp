import { createLazyComponent } from '@sddp/shell/utils';

function lazyComponent(id: string, load: () => Promise<unknown>) {
  return createLazyComponent(id, async () => ({ default: await load() }));
}

export const activityComponents = {
  ChannelView: lazyComponent('ChannelView', async () =>
    import('@sddp/activities/conversations/components/sections').then(
      ({ ChannelView }) => ChannelView
    )
  ),
  ForumView: lazyComponent('ForumView', async () =>
    import('@sddp/activities/conversations/components/sections').then(({ ForumView }) => ForumView)
  ),
  ConversationSidebarPanel: lazyComponent('ConversationSidebarPanel', async () =>
    import('@sddp/activities/conversations/components/sections').then(
      ({ ConversationSidebarPanel }) => ConversationSidebarPanel
    )
  ),
  ConversationCreatePage: lazyComponent('ConversationCreatePage', async () =>
    import('@sddp/activities/conversations/components/pages').then(
      ({ ConversationCreatePage }) => ConversationCreatePage
    )
  ),
  GlobalConversationsPage: lazyComponent('GlobalConversationsPage', async () =>
    import('@sddp/activities/conversations/components/pages').then(
      ({ GlobalConversationsPage }) => GlobalConversationsPage
    )
  ),
  SpecPanel: lazyComponent('SpecPanel', async () =>
    import('@sddp/activities/specs').then(({ SpecPanel }) => SpecPanel)
  ),
  RequirementPanel: lazyComponent('RequirementPanel', async () =>
    import('@sddp/activities/requirements').then(({ RequirementPanel }) => RequirementPanel)
  ),
  GlossaryPanel: lazyComponent('GlossaryPanel', async () =>
    import('@sddp/activities/glossary').then(({ GlossaryPanel }) => GlossaryPanel)
  ),
  MyDashboardPage: lazyComponent('MyDashboardPage', async () =>
    import('@sddp/activities/dashboard').then(({ MyDashboardPage }) => MyDashboardPage)
  ),
  SystemDashboardPage: lazyComponent('SystemDashboardPage', async () =>
    import('@sddp/activities/dashboard').then(({ SystemDashboardPage }) => SystemDashboardPage)
  ),
  MyDashboardPanel: lazyComponent('MyDashboardPanel', async () =>
    import('@sddp/activities/dashboard').then(({ MyDashboardPanel }) => MyDashboardPanel)
  ),
  MyTasksPage: lazyComponent('MyTasksPage', async () =>
    import('@sddp/activities/task/components/pages').then(({ MyTasksPage }) => MyTasksPage)
  ),
  TaskListDetailPage: lazyComponent('TaskListDetailPage', async () =>
    import('@sddp/activities/task/components/pages').then(
      ({ TaskListDetailPage }) => TaskListDetailPage
    )
  ),
  TaskCategoriesPanel: lazyComponent('TaskCategoriesPanel', async () =>
    import('@sddp/activities/task/components/sections').then(
      ({ TaskCategoriesPanel }) => TaskCategoriesPanel
    )
  ),
  TaskBacklogPanel: lazyComponent('TaskBacklogPanel', async () =>
    import('@sddp/activities/task/components/sections').then(
      ({ TaskBacklogPanel }) => TaskBacklogPanel
    )
  ),
  SettingsPanelContent: lazyComponent('SettingsPanelContent', async () =>
    import('@sddp/activities/settings/components/sections').then(
      ({ SettingsPanelContent }) => SettingsPanelContent
    )
  ),
  ProfileSettingsPage: lazyComponent('ProfileSettingsPage', async () =>
    import('@sddp/activities/settings/components/pages').then(
      ({ ProfileSettingsPage }) => ProfileSettingsPage
    )
  ),
  PreferencesSettingsPage: lazyComponent('PreferencesSettingsPage', async () =>
    import('@sddp/activities/settings/components/pages').then(
      ({ PreferencesSettingsPage }) => PreferencesSettingsPage
    )
  ),
  ProjectGeneralSettingsPage: lazyComponent('ProjectGeneralSettingsPage', async () =>
    import('@sddp/activities/settings/components/pages').then(
      ({ ProjectGeneralSettingsPage }) => ProjectGeneralSettingsPage
    )
  ),
  ProjectMembersSettingsPage: lazyComponent('ProjectMembersSettingsPage', async () =>
    import('@sddp/activities/settings/components/pages').then(
      ({ ProjectMembersSettingsPage }) => ProjectMembersSettingsPage
    )
  ),
  ProjectRolesSettingsPage: lazyComponent('ProjectRolesSettingsPage', async () =>
    import('@sddp/activities/settings/components/pages').then(
      ({ ProjectRolesSettingsPage }) => ProjectRolesSettingsPage
    )
  ),
  ProjectIntegrationsSettingsPage: lazyComponent('ProjectIntegrationsSettingsPage', async () =>
    import('@sddp/activities/settings/components/pages').then(
      ({ ProjectIntegrationsSettingsPage }) => ProjectIntegrationsSettingsPage
    )
  ),
  SystemUsersSettingsPage: lazyComponent('SystemUsersSettingsPage', async () =>
    import('@sddp/activities/settings/components/pages').then(
      ({ SystemUsersSettingsPage }) => SystemUsersSettingsPage
    )
  ),
  SystemProjectsSettingsPage: lazyComponent('SystemProjectsSettingsPage', async () =>
    import('@sddp/activities/settings/components/pages').then(
      ({ SystemProjectsSettingsPage }) => SystemProjectsSettingsPage
    )
  ),
  SystemConfigSettingsPage: lazyComponent('SystemConfigSettingsPage', async () =>
    import('@sddp/activities/settings/components/pages').then(
      ({ SystemConfigSettingsPage }) => SystemConfigSettingsPage
    )
  ),
  SystemAuditLogsPage: lazyComponent('SystemAuditLogsPage', async () =>
    import('@sddp/activities/settings/components/pages').then(
      ({ SystemAuditLogsPage }) => SystemAuditLogsPage
    )
  ),
  SystemHealthPage: lazyComponent('SystemHealthPage', async () =>
    import('@sddp/activities/settings/components/pages').then(
      ({ SystemHealthPage }) => SystemHealthPage
    )
  ),
  ProjectDashboardPage: lazyComponent('ProjectDashboardPage', async () =>
    import('@sddp/activities/projects/pages/dashboard').then(({ default: page }) => page)
  ),
  ProjectConversationsPage: lazyComponent('ProjectConversationsPage', async () =>
    import('@sddp/activities/projects/pages/conversations').then(({ default: page }) => page)
  ),
  ProjectRequirementsPage: lazyComponent('ProjectRequirementsPage', async () =>
    import('@sddp/activities/projects/pages/requirements').then(({ default: page }) => page)
  ),
  ProjectSpecsPage: lazyComponent('ProjectSpecsPage', async () =>
    import('@sddp/activities/projects/pages/specs').then(({ default: page }) => page)
  ),
  ProjectTasksPage: lazyComponent('ProjectTasksPage', async () =>
    import('@sddp/activities/projects/pages/tasks').then(({ default: page }) => page)
  ),
  ProjectGlossaryPage: lazyComponent('ProjectGlossaryPage', async () =>
    import('@sddp/activities/projects/pages/glossary').then(({ default: page }) => page)
  ),
  ProjectKnowledgePage: lazyComponent('ProjectKnowledgePage', async () =>
    import('@sddp/activities/projects/pages/knowledge').then(({ default: page }) => page)
  ),
  ProjectArtifactsPage: lazyComponent('ProjectArtifactsPage', async () =>
    import('@sddp/activities/projects/pages/artifacts').then(({ default: page }) => page)
  ),
  ProjectFilesPage: lazyComponent('ProjectFilesPage', async () =>
    import('@sddp/activities/projects/pages/files').then(({ default: page }) => page)
  ),
  ProjectEffortPage: lazyComponent('ProjectEffortPage', async () =>
    import('@sddp/activities/projects/pages/effort').then(({ default: page }) => page)
  ),
  ProjectTraceabilityPage: lazyComponent('ProjectTraceabilityPage', async () =>
    import('@sddp/activities/projects/pages/traceability').then(({ default: page }) => page)
  ),
  ProjectPanelContent: lazyComponent('ProjectPanelContent', async () =>
    import('@sddp/activities/projects/components/sections').then(
      ({ ProjectPanelContent }) => ProjectPanelContent
    )
  ),
  TimelinePanel: lazyComponent('TimelinePanel', async () =>
    import('@sddp/activities/projects/components/sections').then(
      ({ TimelinePanel }) => TimelinePanel
    )
  ),
} as const;
