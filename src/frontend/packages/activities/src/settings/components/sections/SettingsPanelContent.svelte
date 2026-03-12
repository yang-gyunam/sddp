<!-- Section: SettingsPanelContent -->
<script lang="ts">
  /**
   * SettingsPanelContent - Tree items inside a collapsible sidebar panel.
   * Each item click fires onOpenTab to open an independent editor tab.
   */
  import { Icon, Button } from '@sddp/ui';
  import { config as appConfig } from '@sddp/shell';

  interface SettingsTabConfig {
    title: string;
    icon: string;
    path: string;
    menuId: string;
  }

  interface SettingsTreeNode {
    id: string;
    label: string;
    icon: string;
    tabConfig: SettingsTabConfig;
  }

  interface Props {
    panelType: 'user' | 'project' | 'system';
    projectId?: string;
    projectName?: string;
    selectedId?: string | null;
    onOpenTab?: (config: SettingsTabConfig) => void;
  }

  let { panelType, projectId = '', selectedId = null, onOpenTab }: Props = $props();
  const projectIntegrationsEnabled = appConfig.get('enableProjectIntegrations');

  const userNodes: SettingsTreeNode[] = [
    {
      id: 'profile',
      label: 'Profile',
      icon: 'user-round',
      tabConfig: { title: 'Profile', icon: 'user-round', path: '/settings/profile', menuId: 'settings-profile' },
    },
    {
      id: 'preferences',
      label: 'Preferences',
      icon: 'sliders',
      tabConfig: { title: 'Preferences', icon: 'sliders', path: '/settings/preferences', menuId: 'settings-preferences' },
    },
    // Hidden: no backend notification infrastructure yet (email server, Web Push, SignalR events)
    // {
    //   id: 'notifications',
    //   label: 'Notifications',
    //   icon: 'bell',
    //   tabConfig: { title: 'Notifications', icon: 'bell', path: '/settings/notifications', menuId: 'settings-notifications' },
    // },
  ];

  const projectNodes = $derived<SettingsTreeNode[]>([
    {
      id: 'general',
      label: 'General',
      icon: 'file-text',
      tabConfig: { title: 'General', icon: 'file-text', path: `/settings/project/${projectId}/general`, menuId: `settings-project-${projectId}-general` },
    },
    {
      id: 'members',
      label: 'Members',
      icon: 'circle-user-round',
      tabConfig: { title: 'Members', icon: 'circle-user-round', path: `/settings/project/${projectId}/members`, menuId: `settings-project-${projectId}-members` },
    },
    {
      id: 'roles',
      label: 'Roles',
      icon: 'shield',
      tabConfig: { title: 'Roles', icon: 'shield', path: `/settings/project/${projectId}/roles`, menuId: `settings-project-${projectId}-roles` },
    },
    ...(projectIntegrationsEnabled
      ? [{
          id: 'integrations',
          label: 'Integrations',
          icon: 'link',
          tabConfig: {
            title: 'Integrations',
            icon: 'link',
            path: `/settings/project/${projectId}/integrations`,
            menuId: `settings-project-${projectId}-integrations`,
          },
        }]
      : []),
  ]);

  const systemNodes: SettingsTreeNode[] = [
    {
      id: 'dashboard',
      label: 'Dashboard',
      icon: 'bar-chart',
      tabConfig: { title: 'Dashboard', icon: 'bar-chart', path: '/settings/system/dashboard', menuId: 'settings-system-dashboard' },
    },
    {
      id: 'users',
      label: 'Users',
      icon: 'user',
      tabConfig: { title: 'Users', icon: 'user', path: '/settings/system/users', menuId: 'settings-system-users' },
    },
    {
      id: 'projects',
      label: 'Projects',
      icon: 'project',
      tabConfig: { title: 'Projects', icon: 'project', path: '/settings/system/projects', menuId: 'settings-system-projects' },
    },
    {
      id: 'config',
      label: 'Config',
      icon: 'settings',
      tabConfig: { title: 'Config', icon: 'settings', path: '/settings/system/config', menuId: 'settings-system-config' },
    },
    {
      id: 'audit',
      label: 'Audit Logs',
      icon: 'file-text',
      tabConfig: { title: 'Audit Logs', icon: 'file-text', path: '/settings/system/audit', menuId: 'settings-system-audit' },
    },
    {
      id: 'health',
      label: 'Health',
      icon: 'heart',
      tabConfig: { title: 'Health', icon: 'heart', path: '/settings/system/health', menuId: 'settings-system-health' },
    },
  ];

  const nodes = $derived(
    panelType === 'user' ? userNodes :
    panelType === 'project' ? projectNodes :
    systemNodes
  );

  function handleNodeClick(node: SettingsTreeNode) {
    onOpenTab?.(node.tabConfig);
  }
</script>

<div class="settings-panel-tree">
  {#each nodes as node (node.id)}
    {@const isSelected = selectedId === node.tabConfig.menuId}
    <Button
      variant="unstyled"
      class="flex items-center gap-2 w-full text-left px-2 py-1.5 cursor-pointer border rounded transition-colors
        {isSelected
          ? 'bg-[var(--color-accent-primary)]/10 border-[var(--color-accent-primary)]/30 text-[var(--color-text-primary)]'
          : 'border-transparent hover:bg-[var(--color-bg-tertiary)] text-[var(--color-text-secondary)]'}"
      onclick={() => handleNodeClick(node)}
    >
      <Icon
        name={node.icon}
        size="sm"
        class={isSelected ? 'text-[var(--color-accent-primary)]' : 'text-[var(--color-text-tertiary)]'}
      />
      <span class="flex-1 text-sm truncate">{node.label}</span>
    </Button>
  {/each}
</div>

<style>
  .settings-panel-tree {
    display: flex;
    flex-direction: column;
    gap: 1px;
    /* padding: 0.25rem; */
  }
</style>
