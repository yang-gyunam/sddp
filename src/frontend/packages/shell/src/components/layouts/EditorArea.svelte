<script lang="ts">
  import { Icon, Typography, IconButton } from '@sddp/ui';

  interface Tab {
    id: string;
    title: string;
    icon?: string;
    dirty?: boolean;
    closable?: boolean;
  }

  interface Props {
    tabs?: Tab[];
    activeTabId?: string;
    onTabClick?: (id: string) => void;
    onTabClose?: (id: string) => void;
    onTabReorder?: (fromIndex: number, toIndex: number) => void;
    onSplit?: (direction: 'horizontal' | 'vertical') => void;
    emptyMessage?: string;
    children?: import('svelte').Snippet;
  }

  let {
    tabs = [],
    activeTabId = $bindable(''),
    onTabClick,
    onTabClose,
    onTabReorder,
    onSplit,
    emptyMessage = 'No file is open',
    children,
  }: Props = $props();

  // Drag state
  let draggedTabId = $state<string | null>(null);
  let dragOverTabId = $state<string | null>(null);
  let dropPosition = $state<'left' | 'right' | null>(null);

  function handleTabClick(id: string) {
    activeTabId = id;
    onTabClick?.(id);
  }

  function handleTabClose(e: MouseEvent, id: string) {
    e.stopPropagation();
    onTabClose?.(id);
  }

  function handleSplit(direction: 'horizontal' | 'vertical') {
    onSplit?.(direction);
  }

  // Drag and Drop handlers
  function handleDragStart(e: DragEvent, tabId: string) {
    draggedTabId = tabId;
    if (e.dataTransfer) {
      e.dataTransfer.effectAllowed = 'move';
      e.dataTransfer.setData('text/plain', tabId);
    }
  }

  function handleDragOver(e: DragEvent, tabId: string) {
    e.preventDefault();
    if (!draggedTabId || draggedTabId === tabId) return;

    dragOverTabId = tabId;

    // Determine drop position based on mouse position
    const target = e.currentTarget as HTMLElement;
    const rect = target.getBoundingClientRect();
    const midpoint = rect.left + rect.width / 2;
    dropPosition = e.clientX < midpoint ? 'left' : 'right';

    if (e.dataTransfer) {
      e.dataTransfer.dropEffect = 'move';
    }
  }

  function handleDragLeave(e: DragEvent) {
    // Only reset if leaving the tab entirely
    const relatedTarget = e.relatedTarget as HTMLElement | null;
    const currentTarget = e.currentTarget as HTMLElement;
    if (!relatedTarget || !currentTarget.contains(relatedTarget)) {
      dragOverTabId = null;
      dropPosition = null;
    }
  }

  function handleDrop(e: DragEvent, targetTabId: string) {
    e.preventDefault();

    if (!draggedTabId || draggedTabId === targetTabId || !onTabReorder) {
      resetDragState();
      return;
    }

    const fromIndex = tabs.findIndex((t) => t.id === draggedTabId);
    let toIndex = tabs.findIndex((t) => t.id === targetTabId);

    if (fromIndex === -1 || toIndex === -1) {
      resetDragState();
      return;
    }

    // Adjust target index based on drop position
    if (dropPosition === 'right') {
      toIndex = fromIndex < toIndex ? toIndex : toIndex + 1;
    } else {
      toIndex = fromIndex < toIndex ? toIndex - 1 : toIndex;
    }

    // Ensure toIndex is within bounds
    toIndex = Math.max(0, Math.min(toIndex, tabs.length - 1));

    if (fromIndex !== toIndex) {
      onTabReorder(fromIndex, toIndex);
    }

    resetDragState();
  }

  function handleDragEnd() {
    resetDragState();
  }

  function resetDragState() {
    draggedTabId = null;
    dragOverTabId = null;
    dropPosition = null;
  }

  // Compute drop indicator style
  function getDropIndicatorClass(tabId: string): string {
    if (dragOverTabId !== tabId || !dropPosition) return '';
    return dropPosition === 'left'
      ? 'before:absolute before:left-0 before:top-0 before:bottom-0 before:w-0.5 before:bg-[var(--color-accent-primary)]'
      : 'after:absolute after:right-0 after:top-0 after:bottom-0 after:w-0.5 after:bg-[var(--color-accent-primary)]';
  }
</script>

<div class="flex flex-col h-full bg-[var(--color-bg-primary)]">
  <!-- Tabs bar -->
  {#if tabs.length > 0}
    <div
      class="flex items-center h-[var(--tab-height)] bg-[var(--color-bg-secondary)] border-b border-[var(--color-border)]"
    >
      <!-- Tabs container -->
      <div class="flex items-center flex-1 overflow-x-auto" role="tablist">
        {#each tabs as tab, index (tab.id)}
          <div
            draggable="true"
            ondragstart={(e) => handleDragStart(e, tab.id)}
            ondragover={(e) => handleDragOver(e, tab.id)}
            ondragleave={handleDragLeave}
            ondrop={(e) => handleDrop(e, tab.id)}
            ondragend={handleDragEnd}
            onclick={() => handleTabClick(tab.id)}
            onkeydown={(e) => e.key === 'Enter' && handleTabClick(tab.id)}
            class="relative flex items-center h-full px-3 gap-2 border-r border-[var(--color-border)] transition-colors cursor-grab active:cursor-grabbing select-none
              {activeTabId === tab.id
              ? 'bg-[var(--color-bg-primary)] text-[var(--color-text-primary)]'
              : 'bg-[var(--color-bg-secondary)] text-[var(--color-text-secondary)] hover:bg-[var(--color-bg-tertiary)]'}
              {draggedTabId === tab.id ? 'opacity-50' : ''}
              {getDropIndicatorClass(tab.id)}
            "
            aria-selected={activeTabId === tab.id}
            role="tab"
            tabindex="0"
            data-tab-id={tab.id}
            data-tab-index={index}
          >
            {#if tab.icon}
              <Icon name={tab.icon} size="sm" />
            {:else}
              <Icon name="file-text" size="sm" />
            {/if}

            <span class="text-sm whitespace-nowrap max-w-[150px] truncate">
              {tab.title}
            </span>

            {#if tab.dirty}
              <span class="w-2 h-2 rounded-full bg-[var(--color-text-tertiary)]" title="Unsaved changes"
              ></span>
            {/if}

            {#if tab.closable !== false}
              <IconButton
                icon="x"
                size="sm"
                onclick={(e) => handleTabClose(e, tab.id)}
                title="Close"
              />
            {/if}
          </div>
        {/each}
      </div>

      <!-- Tab bar actions (Split buttons) -->
      {#if onSplit}
        <div class="flex items-center gap-1 px-2">
          <IconButton
            icon="split-horizontal"
            size="sm"
            onclick={() => handleSplit('horizontal')}
            title="Split Right"
          />
          <IconButton
            icon="split-vertical"
            size="sm"
            onclick={() => handleSplit('vertical')}
            title="Split Down"
          />
          <IconButton
            icon="more-horizontal"
            size="sm"
            title="More actions"
          />
        </div>
      {/if}
    </div>
  {/if}

  <!-- Editor content -->
  <div class="flex-1 overflow-auto">
    {#if tabs.length === 0}
      <div class="flex flex-col items-center justify-center h-full text-[var(--color-text-tertiary)]">
        <Icon name="file" size="xl" class="mb-4 opacity-50" />
        <Typography variant="body2" color="tertiary">
          {emptyMessage}
        </Typography>
      </div>
    {:else if children}
      {@render children()}
    {/if}
  </div>
</div>
