<!-- Section: ProjectSection -->
<!--
  ProjectSection Component
  Collapsible project section with tree navigation
-->
<script lang="ts">
  import { Icon, Button } from '@sddp/ui';
  import { CollapsiblePanel, TabFactory, toast } from '@sddp/shell';
  import type { Project, ProjectSidebarBadges, ProjectTreeNode } from '../../types';
  import { generateProjectTreeNodes } from '../../types';

  export type NewItemType = 'spec' | 'requirement' | 'task' | 'conversation';

  interface Props {
    project: Project;
    badges?: ProjectSidebarBadges;
    expanded?: boolean;
    selectedNodePath?: string | null;
    onToggle?: (expanded: boolean) => void;
    onNodeSelect?: (node: ProjectTreeNode) => void;
    onRefresh?: (projectId: string) => void;
    onNewItem?: (projectId: string, itemType: NewItemType) => void;
    class?: string;
  }

  let {
    project,
    badges,
    expanded = true,
    selectedNodePath = null,
    onToggle,
    onNodeSelect,
    onRefresh,
    onNewItem,
    class: className = '',
  }: Props = $props();

  // Menu state
  let showContextMenu = $state(false);
  let showNewItemMenu = $state(false);
  // Close menus when clicking outside
  function closeMenus() {
    showContextMenu = false;
    showNewItemMenu = false;
  }

  const treeNodes = $derived(generateProjectTreeNodes(project, badges));

  const actions = $derived([
    {
      id: 'add',
      icon: 'plus',
      label: 'New Item',
      onClick: () => {
        showNewItemMenu = !showNewItemMenu;
        showContextMenu = false;
      },
    },
    {
      id: 'refresh',
      icon: 'refresh-cw',
      label: 'Refresh',
      onClick: () => {
        if (onRefresh) {
          onRefresh(project.id);
        } else {
          toast.info(`Refreshing ${project.name}...`);
        }
      },
    },
    {
      id: 'more',
      icon: 'more-horizontal',
      label: 'More Actions',
      onClick: () => {
        showContextMenu = !showContextMenu;
        showNewItemMenu = false;
      },
    },
  ]);

  // New item menu items
  const newItemMenuItems = [
    { id: 'spec', icon: 'file-text', label: 'New Spec', type: 'spec' as NewItemType },
    { id: 'requirement', icon: 'clipboard-list', label: 'New Requirement', type: 'requirement' as NewItemType },
    { id: 'task', icon: 'check-square', label: 'New Task', type: 'task' as NewItemType },
    { id: 'conversation', icon: 'message-square', label: 'New Conversation', type: 'conversation' as NewItemType },
  ];

  // Context menu items
  const contextMenuItems = [
    { id: 'settings', icon: 'settings', label: 'Project Settings' },
    { id: 'members', icon: 'user', label: 'Manage Members' },
    { id: 'divider', type: 'divider' as const },
    { id: 'archive', icon: 'archive', label: 'Archive Project' },
  ];

  function handleNewItemSelect(itemType: NewItemType) {
    showNewItemMenu = false;
    if (onNewItem) {
      onNewItem(project.id, itemType);
    } else {
      toast.info(`Create new ${itemType} in ${project.name}`);
    }
  }

  function handleContextMenuSelect(itemId: string) {
    showContextMenu = false;
    switch (itemId) {
      case 'settings':
        toast.info(`Open settings for ${project.name}`);
        break;
      case 'members':
        toast.info(`Manage members of ${project.name}`);
        break;
      case 'archive':
        toast.info(`Archive ${project.name}`);
        break;
    }
  }

  function handleNodeClick(node: ProjectTreeNode) {
    // Create tab using TabFactory if node has tabConfig
    if (node.tabConfig) {
      const menuNode = {
        id: node.id,
        name: node.label,
        icon: node.icon,
        order: 0,
        type: 'PAGE' as const,
        children: [],
        permissions: [],
        tabConfig: node.tabConfig,
      };
      TabFactory.handleMenuClick(menuNode);
    }
    
    // Also notify parent component
    onNodeSelect?.(node);
  }

</script>

<div class="relative">
  <CollapsiblePanel
    title={project.name}
    {expanded}
    {actions}
    onToggle={onToggle}
    class={className}
  >
    <div class="project-tree px-1">
      {#each treeNodes as node (node.id)}
        {@const isSelected = selectedNodePath === node.path}
        <Button
          variant="unstyled"
          aria-label={node.label}
          class="
            w-full text-left flex items-center gap-2 px-2 py-1.5 rounded cursor-pointer
            {isSelected
              ? 'bg-[var(--color-accent-primary)] bg-opacity-20 text-[var(--color-text-primary)]'
              : 'hover:bg-[var(--color-bg-tertiary)] text-[var(--color-text-secondary)]'}
            transition-colors
          "
          onclick={() => handleNodeClick(node)}
        >
          <Icon
            name={node.icon}
            size="sm"
            class={isSelected ? 'text-[var(--color-accent-primary)]' : 'text-[var(--color-text-tertiary)]'}
          />
          <span class="flex-1 text-sm truncate">
            {node.label}
          </span>
          {#if node.badge !== undefined && node.badge > 0}
            <span
              class="
                px-1.5 py-0.5 text-[0.625rem] rounded-full min-w-[18px] text-center
                {isSelected
                  ? 'bg-[var(--color-accent-primary)] text-white'
                  : 'bg-[var(--color-bg-tertiary)] text-[var(--color-text-secondary)]'}
              "
            >
              {node.badge}
            </span>
          {/if}
        </Button>
      {/each}
    </div>
  </CollapsiblePanel>

  <!-- New Item Menu -->
  {#if showNewItemMenu}
    <Button
      variant="unstyled"
      class="fixed inset-0 z-40"
      onclick={closeMenus}
      onkeydown={(e) => e.key === 'Escape' && closeMenus()}
      tabindex={-1}
      aria-label="Close menu"
    ></Button>
    <div
      class="absolute z-50 right-0 top-8 min-w-44 bg-[var(--color-bg-secondary)] border border-[var(--color-border)] rounded-lg shadow-lg py-1"
      role="menu"
    >
      {#each newItemMenuItems as item (item.id)}
        <Button
          variant="unstyled"
          class="w-full flex items-center gap-2 px-3 py-2 text-sm text-[var(--color-text-primary)] hover:bg-[var(--color-bg-tertiary)] transition-colors"
          onclick={() => handleNewItemSelect(item.type)}
          role="menuitem"
        >
          <Icon name={item.icon} size="sm" class="text-[var(--color-text-tertiary)]" />
          {item.label}
        </Button>
      {/each}
    </div>
  {/if}

  <!-- Context Menu -->
  {#if showContextMenu}
    <Button
      variant="unstyled"
      class="fixed inset-0 z-40"
      onclick={closeMenus}
      onkeydown={(e) => e.key === 'Escape' && closeMenus()}
      tabindex={-1}
      aria-label="Close menu"
    ></Button>
    <div
      class="absolute z-50 right-0 top-8 min-w-44 bg-[var(--color-bg-secondary)] border border-[var(--color-border)] rounded-lg shadow-lg py-1"
      role="menu"
    >
      {#each contextMenuItems as item (item.id)}
        {#if item.type === 'divider'}
          <div class="my-1 border-t border-[var(--color-border)]"></div>
        {:else}
          <Button
            variant="unstyled"
            class="w-full flex items-center gap-2 px-3 py-2 text-sm text-[var(--color-text-primary)] hover:bg-[var(--color-bg-tertiary)] transition-colors"
            onclick={() => handleContextMenuSelect(item.id)}
            role="menuitem"
          >
            <Icon name={item.icon} size="sm" class="text-[var(--color-text-tertiary)]" />
            {item.label}
          </Button>
        {/if}
      {/each}
    </div>
  {/if}
</div>
