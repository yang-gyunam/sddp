<!--
  SidebarMenuTree Component
  Sidebar-specific tree navigation with icons, labels, and active highlighting
-->
<script lang="ts">
  import { Icon, IconButton } from '@sddp/ui';
  import type { MenuNode } from '../../types';

  interface Props {
    items: MenuNode[];
    selectedId?: string | null;
    expandedIds?: Set<string>;
    class?: string;
    onSelect?: (item: MenuNode) => void;
    onToggle?: (itemId: string) => void;
    onNavigate?: (item: MenuNode) => void;
  }

  let {
    items,
    selectedId = null,
    expandedIds = new Set<string>(),
    class: className = '',
    onSelect,
    onToggle,
    onNavigate,
  }: Props = $props();

  // Get icon based on item type and state
  function getIcon(item: MenuNode, isExpanded: boolean): string {
    if (item.icon) return item.icon;

    switch (item.type) {
      case 'FOLDER':
        return isExpanded ? 'folder-open' : 'folder';
      case 'EXTERNAL':
        return 'external-link';
      case 'PAGE':
      default:
        return 'file-text';
    }
  }

  // Get icon color based on item type
  function getIconColor(item: MenuNode): string {
    switch (item.type) {
      case 'FOLDER':
        return 'text-[var(--color-warning-500)]';
      case 'EXTERNAL':
        return 'text-[var(--color-info-500)]';
      case 'PAGE':
      default:
        return 'text-[var(--color-text-secondary)]';
    }
  }

  // Handle item click
  function handleItemClick(item: MenuNode, event: MouseEvent) {
    const target = event.target as HTMLElement;

    // If clicking on chevron, toggle expand/collapse
    if (target.closest('.chevron-trigger')) {
      onToggle?.(item.id);
      return;
    }

    // Select the item
    onSelect?.(item);

    // Navigate if it's a page or external link
    if (item.type === 'PAGE' || item.type === 'EXTERNAL') {
      onNavigate?.(item);
    }

    // If it's a folder with children, also toggle
    if (item.type === 'FOLDER' && item.children.length > 0) {
      onToggle?.(item.id);
    }
  }

  // Handle keyboard navigation
  function handleKeyDown(item: MenuNode, event: KeyboardEvent) {
    const isExpanded = expandedIds.has(item.id);
    const hasChildren = item.children.length > 0;

    switch (event.key) {
      case 'Enter':
      case ' ':
        event.preventDefault();
        onSelect?.(item);
        if (item.type === 'PAGE' || item.type === 'EXTERNAL') {
          onNavigate?.(item);
        } else if (item.type === 'FOLDER' && hasChildren) {
          onToggle?.(item.id);
        }
        break;
      case 'ArrowRight':
        if (hasChildren && !isExpanded) {
          event.preventDefault();
          onToggle?.(item.id);
        }
        break;
      case 'ArrowLeft':
        if (hasChildren && isExpanded) {
          event.preventDefault();
          onToggle?.(item.id);
        }
        break;
    }
  }
</script>

<!-- Recursive menu item renderer -->
{#snippet menuItem(item: MenuNode, level: number = 0)}
  {@const isExpanded = expandedIds.has(item.id)}
  {@const isSelected = selectedId === item.id}
  {@const hasChildren = item.children.length > 0}
  {@const indent = level * 12}

  <div
    class="menu-item group flex items-center h-7 px-2 cursor-pointer select-none transition-colors rounded-sm mx-1
      {isSelected
        ? 'bg-[var(--color-accent-primary)]/10 text-[var(--color-text-primary)]'
        : 'hover:bg-[var(--color-bg-tertiary)] text-[var(--color-text-secondary)]'}
      {item.isActive ? 'font-medium' : ''}"
    style="padding-left: {8 + indent}px"
    role="treeitem"
    tabindex="0"
    aria-selected={isSelected}
    aria-expanded={hasChildren ? isExpanded : undefined}
    onclick={(e) => handleItemClick(item, e)}
    onkeydown={(e) => handleKeyDown(item, e)}
    data-menu-id={item.id}
  >
    <!-- Chevron for folders with children -->
    {#if hasChildren}
      <IconButton
        icon="chevron-right"
        size="sm"
        title={isExpanded ? 'Collapse' : 'Expand'}
        class="chevron-trigger !w-4 !h-4 !p-0 mr-1 transition-transform duration-150 {isExpanded ? 'rotate-90' : ''}"
      />
    {:else}
      <div class="w-4 h-4 mr-1 flex-shrink-0"></div>
    {/if}

    <!-- Icon -->
    <Icon
      name={getIcon(item, isExpanded)}
      size="sm"
      class="mr-2 flex-shrink-0 {getIconColor(item)}"
    />

    <!-- Label -->
    <span class="truncate text-sm flex-1">
      {item.name}
    </span>

    <!-- External link indicator -->
    {#if item.type === 'EXTERNAL'}
      <Icon name="external-link" size="xs" class="ml-1 opacity-50 flex-shrink-0" />
    {/if}

    <!-- Active indicator -->
    {#if item.isActive}
      <div class="w-1.5 h-1.5 rounded-full bg-[var(--color-accent-primary)] ml-2 flex-shrink-0"></div>
    {/if}
  </div>

  <!-- Children (recursive) -->
  {#if hasChildren && isExpanded}
    <div class="children" role="group">
      {#each item.children.sort((a, b) => a.order - b.order) as child (child.id)}
        {@render menuItem(child, level + 1)}
      {/each}
    </div>
  {/if}
{/snippet}

<!-- Main tree container -->
<div
  class="sidebar-menu-tree py-1 {className}"
  role="tree"
  aria-label="Sidebar navigation"
>
  {#if items.length === 0}
    <div class="text-center py-4 px-3 text-[var(--color-text-tertiary)] text-sm">
      No items
    </div>
  {:else}
    {#each items.sort((a, b) => a.order - b.order) as item (item.id)}
      {@render menuItem(item, 0)}
    {/each}
  {/if}
</div>
