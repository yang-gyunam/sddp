<!--
  TreeView Component
  Hierarchical tree view with expand/collapse and selection
-->
<script lang="ts">
  import TreeItem from './TreeItem.svelte';
  import type { TreeNode } from '../../types';

  interface Props {
    nodes: TreeNode[];
    selectedId?: string | null;
    expandedIds?: Set<string>;
    class?: string;
    onSelect?: (node: TreeNode) => void;
    onToggle?: (nodeId: string) => void;
    onContextMenu?: (event: MouseEvent, node: TreeNode) => void;
    onReorder?: (draggedId: string, targetId: string, position: 'before' | 'after' | 'inside') => void;
  }

  let {
    nodes,
    selectedId = null,
    expandedIds = new Set<string>(),
    class: className = '',
    onSelect,
    onToggle,
    onContextMenu,
    onReorder,
  }: Props = $props();

  // Drag & drop state
  let draggedNode: TreeNode | null = $state(null);
  let dragOverNode: TreeNode | null = $state(null);
  let dropPosition: 'before' | 'after' | 'inside' | null = $state(null);

  // Flatten tree for rendering
  function flattenNodes(
    nodeList: TreeNode[],
    level: number = 0
  ): Array<{ node: TreeNode; level: number }> {
    const flattened: Array<{ node: TreeNode; level: number }> = [];

    for (const node of nodeList) {
      flattened.push({ node, level });

      if (node.children && expandedIds.has(node.id)) {
        flattened.push(...flattenNodes(node.children, level + 1));
      }
    }

    return flattened;
  }

  const flatNodes = $derived(flattenNodes(nodes));

  // Check if a node can be dropped on another node
  function canDropOn(dragged: TreeNode, target: TreeNode): boolean {
    if (!dragged || !target) return false;
    if (dragged.id === target.id) return false;

    // Check if target is a descendant of dragged
    const isDescendant = (parent: TreeNode, child: TreeNode): boolean => {
      if (parent.id === child.id) return true;
      return parent.children?.some((c) => isDescendant(c, child)) || false;
    };

    return !isDescendant(dragged, target);
  }

  // Handle drag start
  function handleDragStart(node: TreeNode, event: DragEvent) {
    draggedNode = node;
    if (event.dataTransfer) {
      event.dataTransfer.effectAllowed = 'move';
      event.dataTransfer.setData('text/plain', node.id);
    }
  }

  // Handle drag end
  function handleDragEnd() {
    draggedNode = null;
    dragOverNode = null;
    dropPosition = null;
  }

  // Handle drag over
  function handleDragOver(targetNode: TreeNode, event: DragEvent) {
    event.preventDefault();

    if (!draggedNode || !canDropOn(draggedNode, targetNode)) {
      if (event.dataTransfer) {
        event.dataTransfer.dropEffect = 'none';
      }
      return;
    }

    dragOverNode = targetNode;

    // Determine drop position based on mouse position
    const rect = (event.currentTarget as HTMLElement).getBoundingClientRect();
    const y = event.clientY - rect.top;
    const height = rect.height;

    if (y < height * 0.25) {
      dropPosition = 'before';
    } else if (y > height * 0.75) {
      dropPosition = 'after';
    } else {
      dropPosition = targetNode.children ? 'inside' : 'after';
    }

    if (event.dataTransfer) {
      event.dataTransfer.dropEffect = 'move';
    }
  }

  // Handle drag leave
  function handleDragLeave(event: DragEvent) {
    const relatedTarget = event.relatedTarget as HTMLElement;
    const currentTarget = event.currentTarget as HTMLElement;
    if (!relatedTarget || !currentTarget?.contains(relatedTarget)) {
      dragOverNode = null;
      dropPosition = null;
    }
  }

  // Handle drop
  function handleDrop(targetNode: TreeNode, event: DragEvent) {
    event.preventDefault();

    if (!draggedNode || !canDropOn(draggedNode, targetNode) || !dropPosition) {
      return;
    }

    onReorder?.(draggedNode.id, targetNode.id, dropPosition);

    // Reset drag state
    draggedNode = null;
    dragOverNode = null;
    dropPosition = null;
  }
</script>

<div class="tree-view {className}" role="tree" aria-label="Tree view">
  {#if flatNodes.length === 0}
    <div class="text-center py-4 text-[var(--color-text-tertiary)] text-sm">
      No items
    </div>
  {:else}
    {#each flatNodes as { node, level } (node.id)}
      <TreeItem
        {node}
        {level}
        expanded={expandedIds.has(node.id)}
        selected={selectedId === node.id}
        dragging={draggedNode?.id === node.id}
        dragOver={dragOverNode?.id === node.id}
        {onSelect}
        {onToggle}
        {onContextMenu}
        onDragStart={handleDragStart}
        onDragEnd={handleDragEnd}
        onDragOver={handleDragOver}
        onDragLeave={handleDragLeave}
        onDrop={handleDrop}
      />
    {/each}
  {/if}
</div>
