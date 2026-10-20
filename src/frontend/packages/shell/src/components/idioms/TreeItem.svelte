<!--
  TreeItem Component
  Individual tree node with expand/collapse, icon, and selection
-->
<script lang="ts">
  import type { Snippet } from 'svelte';
  import { Button, Icon } from '@sddp/ui';
  import type { TreeNode } from '../../types';

  interface Props {
    node: TreeNode;
    level?: number;
    expanded?: boolean;
    selected?: boolean;
    dragging?: boolean;
    dragOver?: boolean;
    class?: string;
    /** Flat, compact rows for explorer panes; existing trees retain their default style. */
    variant?: 'default' | 'flat';
    /** Lazy folders can be expandable before their children have loaded. */
    expandable?: boolean;
    toggleOnClick?: boolean;
    indentStep?: number;
    draggable?: boolean;
    tabindex?: number;
    title?: string;
    leading?: Snippet;
    label?: Snippet;
    trailing?: Snippet;
    onfocus?: (event: FocusEvent) => void;
    onkeydown?: (event: KeyboardEvent) => void;
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
    variant = 'default',
    expandable,
    toggleOnClick = false,
    indentStep = 16,
    draggable = true,
    tabindex = 0,
    title,
    leading,
    label,
    trailing,
    onfocus,
    onkeydown,
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
  const hasChildren = $derived(expandable ?? (isFolder && node.children!.length > 0));
  // Indent: 8px base + 16px per level (VS Code style - chevron/icon occupy same column)
  const indent = $derived(level * indentStep + 8);

  function handleClick(event: MouseEvent) {
    if (node.disabled) return;

    // If clicking on the chevron area, toggle instead of select
    const target = event.target as HTMLElement;
    if (isFolder && (target.closest('.chevron-area') || toggleOnClick)) {
      onToggle?.(node.id);
    } else {
      onSelect?.(node);
    }
  }

  function handleKeyDown(event: KeyboardEvent) {
    if (node.disabled) return;
    onkeydown?.(event);
    if (event.defaultPrevented) return;

    if (event.key === 'Enter' || event.key === ' ') {
      event.preventDefault();
      if (toggleOnClick && isFolder) onToggle?.(node.id);
      else onSelect?.(node);
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

<Button
  variant="unstyled"
  class="tree-item w-full text-left flex items-center cursor-pointer select-none transition-colors
    {variant === 'flat' ? 'tree-item--flat' : 'h-[24px] border rounded'}
    {selected
    ? 'border-[var(--color-accent-primary)]/50 bg-[var(--color-accent-primary)]/10 text-[var(--color-text-primary)]'
    : 'border-transparent hover:bg-[var(--color-bg-tertiary)]'}
    {node.disabled ? 'opacity-50 cursor-not-allowed' : ''}
    {dragging ? 'opacity-50' : ''}
    {dragOver
    ? 'bg-[var(--color-accent-primary)]/10 border-t border-t-[var(--color-accent-primary)]'
    : ''}
    {className}"
  style="padding-left: {indent}px"
  {title}
  disabled={node.disabled}
  role="treeitem"
  tabindex={node.disabled ? -1 : tabindex}
  aria-level={level + 1}
  aria-label={node.label}
  aria-selected={selected}
  aria-expanded={hasChildren ? expanded : undefined}
  aria-disabled={node.disabled}
  onclick={handleClick}
  onkeydown={handleKeyDown}
  {onfocus}
  oncontextmenu={handleContextMenu}
  draggable={draggable && !node.disabled}
  ondragstart={handleDragStart}
  ondragend={onDragEnd}
  ondragover={handleDragOver}
  ondragleave={onDragLeave}
  ondrop={(e) => onDrop?.(node, e)}
  data-node-id={node.id}
>
  <!-- Chevron/Icon area: VS Code style - same column for both -->
  <div
    class="chevron-area flex items-center justify-center {variant === 'flat'
      ? 'w-3'
      : 'w-4'} h-4 flex-shrink-0"
  >
    {#if isFolder}
      <!-- Folder: show chevron if has children -->
      {#if hasChildren}
        <div class="transition-transform duration-150 {expanded ? 'rotate-90' : ''}">
          <Icon name="chevron-right" size="xs" class="text-[var(--color-text-secondary)]" />
        </div>
      {/if}
    {:else if node.icon && !leading}
      <!-- File: show icon -->
      <Icon name={node.icon} size="xs" class="text-[var(--color-text-secondary)]" />
    {/if}
  </div>

  {#if leading}<span class="flex items-center flex-shrink-0">{@render leading()}</span>{/if}

  <!-- Label -->
  <span
    class="min-w-0 text-sm {variant === 'flat'
      ? 'flex-1'
      : `truncate ml-0.5 ${selected ? 'font-medium' : ''}`}"
  >
    {#if label}{@render label()}{:else}{node.label}{/if}
  </span>
  {#if trailing}{@render trailing()}{/if}
</Button>

<style>
  :global(.tree-item--flat) {
    width: 100%;
    height: var(--space-7);
    gap: var(--space-1-5);
    padding-right: var(--space-2-5);
    text-align: left;
    color: var(--color-text-primary);
  }
  :global(.tree-item:focus-visible) {
    outline: 1px solid var(--color-accent-primary);
    outline-offset: -1px;
  }
</style>
