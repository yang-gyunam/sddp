<!--
  TreeItem Component
  Individual tree node with expand/collapse, icon, and selection
-->
<script lang="ts">
  import { Icon } from '@sddp/ui';
  import type { TreeNode } from '../../types';

  interface Props {
    node: TreeNode;
    level?: number;
    expanded?: boolean;
    selected?: boolean;
    dragging?: boolean;
    dragOver?: boolean;
    class?: string;
    onSelect?: (node: TreeNode) => void;
    onToggle?: (nodeId: string) => void;
    onContextMenu?: (event: MouseEvent, node: TreeNode) => void;
    onDragStart?: (node: TreeNode, event: DragEvent) => void;
    onDragEnd?: (event: DragEvent) => void;
    onDragOver?: (node: TreeNode, event: DragEvent) => void;
    onDragLeave?: (event: DragEvent) => void;
    onDrop?: (node: TreeNode, event: DragEvent) => void;
  }

  let {
    node,
    level = 0,
    expanded = false,
    selected = false,
    dragging = false,
    dragOver = false,
    class: className = '',
    onSelect,
    onToggle,
    onContextMenu,
    onDragStart,
    onDragEnd,
    onDragOver,
    onDragLeave,
    onDrop,
  }: Props = $props();

  // A folder has children property defined (even if empty array)
  const isFolder = $derived(Array.isArray(node.children));
  // Has expandable children
  const hasChildren = $derived(isFolder && node.children!.length > 0);
  // Indent: 8px base + 16px per level (VS Code style - chevron/icon occupy same column)
  const indent = $derived(level * 16 + 8);

  function handleClick(event: MouseEvent) {
    if (node.disabled) return;

    // If clicking on the chevron area, toggle instead of select
    const target = event.target as HTMLElement;
    if (target.closest('.chevron-area')) {
      onToggle?.(node.id);
    } else {
      onSelect?.(node);
    }
  }

  function handleKeyDown(event: KeyboardEvent) {
    if (node.disabled) return;

    if (event.key === 'Enter' || event.key === ' ') {
      event.preventDefault();
      onSelect?.(node);
    } else if (event.key === 'ArrowRight' && hasChildren && !expanded) {
      event.preventDefault();
      onToggle?.(node.id);
    } else if (event.key === 'ArrowLeft' && hasChildren && expanded) {
      event.preventDefault();
      onToggle?.(node.id);
    }
  }

  function handleContextMenu(event: MouseEvent) {
    event.preventDefault();
    onContextMenu?.(event, node);
  }

  function handleDragStart(event: DragEvent) {
    onDragStart?.(node, event);
  }

  function handleDragOver(event: DragEvent) {
    event.preventDefault();
    onDragOver?.(node, event);
  }
</script>

<div
  class="tree-item flex items-center h-[24px] cursor-pointer select-none transition-colors
    border rounded
    {selected
      ? 'border-[var(--color-accent-primary)]/50 bg-[var(--color-accent-primary)]/10 text-[var(--color-text-primary)]'
      : 'border-transparent hover:bg-[var(--color-bg-tertiary)]'}
    {node.disabled ? 'opacity-50 cursor-not-allowed' : ''}
    {dragging ? 'opacity-50' : ''}
    {dragOver ? 'bg-[var(--color-accent-primary)]/10 border-t border-t-[var(--color-accent-primary)]' : ''}
    {className}"
  style="padding-left: {indent}px"
  role="treeitem"
  tabindex={node.disabled ? -1 : 0}
  aria-selected={selected}
  aria-expanded={hasChildren ? expanded : undefined}
  aria-disabled={node.disabled}
  onclick={handleClick}
  onkeydown={handleKeyDown}
  oncontextmenu={handleContextMenu}
  draggable={!node.disabled}
  ondragstart={handleDragStart}
  ondragend={onDragEnd}
  ondragover={handleDragOver}
  ondragleave={onDragLeave}
  ondrop={(e) => onDrop?.(node, e)}
  data-node-id={node.id}
>
  <!-- Chevron/Icon area: VS Code style - same column for both -->
  <div class="chevron-area flex items-center justify-center w-4 h-4 flex-shrink-0">
    {#if isFolder}
      <!-- Folder: show chevron if has children -->
      {#if hasChildren}
        <div class="transition-transform duration-150 {expanded ? 'rotate-90' : ''}">
          <Icon name="chevron-right" size="xs" class="text-[var(--color-text-secondary)]" />
        </div>
      {/if}
    {:else if node.icon}
      <!-- File: show icon -->
      <Icon name={node.icon} size="xs" class="text-[var(--color-text-secondary)]" />
    {/if}
  </div>

  <!-- Label -->
  <span class="truncate text-sm {selected ? 'font-medium' : ''} ml-0.5">
    {node.label}
  </span>
</div>
