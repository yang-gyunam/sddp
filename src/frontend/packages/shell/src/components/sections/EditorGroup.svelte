<!--
  EditorGroup Section Component
  Complete editor group with tab bar, context menu, and dynamic content rendering
-->
<script lang="ts">
  import type { EditorGroupData } from '../../types/layout.types';
  import type { ContextMenuItem } from '../../types';
  import { Icon, IconButton } from '@sddp/ui';
  import ContextMenu from '../idioms/ContextMenu.svelte';
  import TabItem from '../idioms/TabItem.svelte';
  import DynamicContentRenderer from '../idioms/DynamicContentRenderer.svelte';
  import ErrorBoundary from '../idioms/ErrorBoundary.svelte';
  import Welcome from '../idioms/Welcome.svelte';
  import { tabActions, globalActiveTab } from '../../stores/tabs.store';

  interface EditorGroupProps {
    group: EditorGroupData;
    onTabChange?: (tabId: string) => void;
    onTabClose?: (tabId: string) => void;
    onSplit?: (direction: 'horizontal' | 'vertical') => void;
    class?: string;
  }

  let {
    group,
    onTabChange,
    onTabClose,
    onSplit,
    class: className = '',
  }: EditorGroupProps = $props();

  // Tab context menu state
  let contextMenuVisible = $state(false);
  let contextMenuX = $state(0);
  let contextMenuY = $state(0);
  let contextMenuTabId = $state<string | null>(null);

  // Tab bar action menu state
  let actionMenuVisible = $state(false);
  let actionMenuX = $state(0);
  let actionMenuY = $state(0);

  // Build context menu items for the selected tab
  function getContextMenuItems(): ContextMenuItem[] {
    if (!contextMenuTabId) return [];

    const tab = group.tabs.find(t => t.id === contextMenuTabId);
    if (!tab) return [];

    const tabId = contextMenuTabId;
    const groupIdRef = group.id;

    return [
      {
        id: 'close',
        label: 'Close',
        icon: 'x',
        shortcut: 'Ctrl+W',
        action: () => {
          tabActions.closeTab(tabId, groupIdRef);
          onTabClose?.(tabId);
        },
        disabled: tab.closable === false,
      },
      {
        id: 'close-others',
        label: 'Close Others',
        icon: 'x-circle',
        action: () => {
          tabActions.closeOtherTabs(tabId, groupIdRef);
        },
        disabled: group.tabs.length <= 1,
      },
      {
        id: 'close-all',
        label: 'Close All',
        icon: 'x-square',
        action: () => {
          group.tabs.forEach(t => {
            if (t.closable !== false) {
              tabActions.closeTab(t.id, groupIdRef);
            }
          });
        },
      },
      { id: 'sep1', label: '', separator: true },
      {
        id: 'split-right',
        label: 'Split Right',
        icon: 'columns',
        shortcut: 'Ctrl+\\',
        action: () => {
          tabActions.splitRight(tabId, groupIdRef);
        },
      },
      {
        id: 'split-down',
        label: 'Split Down',
        icon: 'rows',
        shortcut: 'Ctrl+Shift+\\',
        action: () => {
          tabActions.splitGroup('vertical', groupIdRef);
        },
      },
    ];
  }

  // Compute context menu items
  let contextMenuItems = $derived(getContextMenuItems());

  // Build action menu items for tab bar ellipsis button
  function getActionMenuItems(): ContextMenuItem[] {
    const groupIdRef = group.id;

    return [
      {
        id: 'close-all',
        label: 'Close All',
        icon: 'x-square',
        action: () => {
          group.tabs.forEach(t => {
            if (t.closable !== false) {
              tabActions.closeTab(t.id, groupIdRef);
            }
          });
        },
        disabled: group.tabs.length === 0,
      },
      { id: 'sep1', label: '', separator: true },
      {
        id: 'split-right',
        label: 'Split Right',
        icon: 'columns',
        action: () => {
          onSplit?.('horizontal');
        },
      },
      {
        id: 'split-down',
        label: 'Split Down',
        icon: 'rows',
        action: () => {
          onSplit?.('vertical');
        },
      },
    ];
  }

  // Compute action menu items
  let actionMenuItems = $derived(getActionMenuItems());

  // Handle action menu (ellipsis button)
  function handleActionMenu(e: MouseEvent) {
    e.preventDefault();
    e.stopPropagation();
    // EditorGroup
    document.dispatchEvent(new CustomEvent('close-all-context-menus'));
    const button = e.currentTarget as HTMLElement;
    const rect = button.getBoundingClientRect();
    actionMenuX = rect.right;
    actionMenuY = rect.bottom;
    actionMenuVisible = true;
  }

  //
  $effect(() => {
    function handleCloseAllContextMenus() {
      closeContextMenu();
      closeActionMenu();
    }

    function handleResize() {
      closeContextMenu();
      closeActionMenu();
    }

    document.addEventListener('close-all-context-menus', handleCloseAllContextMenus);
    window.addEventListener('resize', handleResize);

    return () => {
      document.removeEventListener('close-all-context-menus', handleCloseAllContextMenus);
      window.removeEventListener('resize', handleResize);
    };
  });

  // Close action menu
  function closeActionMenu() {
    actionMenuVisible = false;
  }

  // Get active tab from group
  let activeTab = $derived(
    group.activeTab ? group.tabs.find((tab) => tab.id === group.activeTab) : null
  );

  // Drag state for tab reordering (same group and cross-group)
  let draggedTabId = $state<string | null>(null);
  let dragOverTabId = $state<string | null>(null);
  let dropPosition = $state<'left' | 'right' | null>(null);

  // Cross-group drag state
  let crossGroupDragTabId = $state<string | null>(null);

  // Trailing empty area drop state (for dropping after the last tab)
  let dragOverTrailingArea = $state(false);

  // Tabs container refs for scroll (bind:this doesn't need $state)
  // svelte-ignore non_reactive_update
  let tabsWrapperElement: HTMLDivElement;
  // svelte-ignore non_reactive_update
  let tabsInnerElement: HTMLDivElement;

  // Scroll state
  let hasOverflow = $state(false);
  let scrollPosition = $state(0);
  let maxScrollPosition = $state(0);
  let prevActiveTab = $state<string | null>(null);

  // Check if tabs overflow and calculate max scroll
  function checkOverflow() {
    if (!tabsWrapperElement || !tabsInnerElement) return;

    const wrapperWidth = tabsWrapperElement.clientWidth;
    const innerWidth = tabsInnerElement.scrollWidth;

    hasOverflow = innerWidth > wrapperWidth;
    maxScrollPosition = Math.max(0, innerWidth - wrapperWidth);

    // Clamp current scroll position
    if (scrollPosition > maxScrollPosition) {
      scrollPosition = maxScrollPosition;
    }
  }

  // Scroll left by step amount
  function scrollLeft() {
    const step = 150;
    scrollPosition = Math.max(0, scrollPosition - step);
  }

  // Scroll right by step amount
  function scrollRight() {
    const step = 150;
    scrollPosition = Math.min(maxScrollPosition, scrollPosition + step);
  }

  // Scroll to make active tab visible
  function scrollToActiveTab() {
    if (!tabsInnerElement || !tabsWrapperElement || !group.activeTab) return;

    const activeTabElement = tabsInnerElement.querySelector(`[data-tab-id="${group.activeTab}"]`) as HTMLElement;
    if (!activeTabElement) return;

    const wrapperWidth = tabsWrapperElement.clientWidth;
    const tabLeft = activeTabElement.offsetLeft;
    const tabRight = tabLeft + activeTabElement.offsetWidth;

    // If tab is out of view on the left
    if (tabLeft < scrollPosition) {
      scrollPosition = Math.max(0, tabLeft - 8);
    }
    // If tab is out of view on the right
    else if (tabRight > scrollPosition + wrapperWidth) {
      scrollPosition = Math.min(maxScrollPosition, tabRight - wrapperWidth + 8);
    }
  }

  // Handle wheel scroll on tabs container
  function handleWheel(e: WheelEvent) {
    if (!hasOverflow) return;

    e.preventDefault();
    const delta = e.deltaY || e.deltaX;
    scrollPosition = Math.max(0, Math.min(maxScrollPosition, scrollPosition + delta));
  }

  // Set up ResizeObserver to detect overflow changes
  $effect(() => {
    if (!tabsWrapperElement || !tabsInnerElement) return;

    let rafId: number | null = null;

    const resizeObserver = new ResizeObserver(() => {
      // Use requestAnimationFrame to avoid ResizeObserver loop
      if (rafId !== null) {
        cancelAnimationFrame(rafId);
      }
      rafId = requestAnimationFrame(() => {
        checkOverflow();
        rafId = null;
      });
    });

    resizeObserver.observe(tabsWrapperElement);
    resizeObserver.observe(tabsInnerElement);

    // Initial check
    checkOverflow();

    return () => {
      if (rafId !== null) {
        cancelAnimationFrame(rafId);
      }
      resizeObserver.disconnect();
    };
  });

  // Watch for tab changes
  $effect(() => {
    const currentActiveTab = group.activeTab;

    // Use microtask to ensure DOM is updated
    queueMicrotask(() => {
      checkOverflow();

      // Scroll to active tab only when tab actually changes
      if (currentActiveTab !== prevActiveTab) {
        scrollToActiveTab();
        prevActiveTab = currentActiveTab;
      }
    });
  });

  // Watch for tabs array changes
  $effect(() => {
    // Track tabs length as dependency
    void group.tabs.length;
    queueMicrotask(() => {
      checkOverflow();
    });
  });

  // Handle tab click
  function handleTabClick(tabId: string) {
    tabActions.switchToTab(tabId, group.id);
    onTabChange?.(tabId);
  }

  // Handle tab context menu
  function handleContextMenu(e: MouseEvent, tabId: string) {
    e.preventDefault();
    // EditorGroup
    document.dispatchEvent(new CustomEvent('close-all-context-menus'));
    contextMenuTabId = tabId;
    contextMenuX = e.clientX;
    contextMenuY = e.clientY;
    contextMenuVisible = true;
  }

  // Close context menu
  function closeContextMenu() {
    contextMenuVisible = false;
    contextMenuTabId = null;
  }

  // Handle split
  function handleSplit(direction: 'horizontal' | 'vertical') {
    onSplit?.(direction);
  }

  // Drag and Drop handlers for tab reordering
  function handleDragStart(e: DragEvent, tabId: string) {
    draggedTabId = tabId;
    if (e.dataTransfer) {
      e.dataTransfer.effectAllowed = 'copyMove';
      e.dataTransfer.setData('tab-id', tabId);
      e.dataTransfer.setData('source-group-id', group.id);
      const tab = group.tabs.find((t) => t.id === tabId);
      if (tab) {
        e.dataTransfer.setData('tab-title', tab.title);
        e.dataTransfer.setData('tab-path', tab.path || '');
      }

      // Set drag image to appear at bottom-right of cursor
      // so the drop position indicator is visible
      const target = e.target as HTMLElement;
      if (target) {
        e.dataTransfer.setDragImage(target, -12, -12);
      }
    }
  }

  function handleDragOver(e: DragEvent, tabId: string) {
    e.preventDefault();
    e.stopPropagation();

    // Check for cross-group drag via dataTransfer
    const hasTabData = e.dataTransfer?.types?.includes('tab-id');

    // For cross-group drag, set the marker if not already set
    // This is needed because stopPropagation prevents handleGroupDragOver from being called
    if (hasTabData && !draggedTabId && !crossGroupDragTabId) {
      crossGroupDragTabId = 'cross-group-drag';
    }

    const dragTabId = hasTabData
      ? (crossGroupDragTabId || draggedTabId)
      : draggedTabId;

    if (!dragTabId || dragTabId === tabId) return;

    // Clear trailing area state when hovering over a specific tab
    dragOverTrailingArea = false;
    dragOverTabId = tabId;

    const target = e.currentTarget as HTMLElement;
    const rect = target.getBoundingClientRect();
    const midpoint = rect.left + rect.width / 2;
    dropPosition = e.clientX < midpoint ? 'left' : 'right';

    if (e.dataTransfer) {
      const isCopyOperation = e.ctrlKey || e.metaKey;
      e.dataTransfer.dropEffect = isCopyOperation ? 'copy' : 'move';
    }
  }

  function handleDragLeave(e: DragEvent) {
    const relatedTarget = e.relatedTarget as HTMLElement | null;
    const currentTarget = e.currentTarget as HTMLElement;
    if (!relatedTarget || !currentTarget.contains(relatedTarget)) {
      dragOverTabId = null;
      dropPosition = null;
    }
  }

  // Handle drag over the trailing empty area (after the last tab)
  function handleTrailingAreaDragOver(e: DragEvent) {
    e.preventDefault();
    e.stopPropagation();

    const hasTabData = e.dataTransfer?.types?.includes('tab-id');
    const dragTabId = draggedTabId || (hasTabData ? 'cross-group-drag' : null);

    if (!dragTabId) return;

    // Clear tab-specific hover state
    dragOverTabId = null;
    dropPosition = null;
    dragOverTrailingArea = true;

    if (e.dataTransfer) {
      const isCopyOperation = e.ctrlKey || e.metaKey;
      e.dataTransfer.dropEffect = isCopyOperation ? 'copy' : 'move';
    }
  }

  function handleTrailingAreaDragLeave(e: DragEvent) {
    const relatedTarget = e.relatedTarget as HTMLElement | null;
    const currentTarget = e.currentTarget as HTMLElement;
    if (!relatedTarget || !currentTarget.contains(relatedTarget)) {
      dragOverTrailingArea = false;
    }
  }

  function handleTrailingAreaDrop(e: DragEvent) {
    e.preventDefault();
    e.stopPropagation();

    const dragTabIdFromData = e.dataTransfer?.getData('tab-id');
    const sourceGroupId = e.dataTransfer?.getData('source-group-id');
    const isCrossGroupDrop = sourceGroupId && sourceGroupId !== group.id;
    const effectiveTabId = isCrossGroupDrop ? dragTabIdFromData : draggedTabId;

    if (!effectiveTabId) {
      resetDragState();
      return;
    }

    // Drop at the end (after all tabs)
    const toIndex = group.tabs.length;

    if (isCrossGroupDrop && sourceGroupId) {
      const isCopyOperation = e.ctrlKey || e.metaKey;
      if (isCopyOperation) {
        tabActions.copyTabToGroup(effectiveTabId, group.id, sourceGroupId, toIndex);
      } else {
        tabActions.moveTabToGroup(effectiveTabId, group.id, sourceGroupId, toIndex);
      }
    } else {
      // Same group - move to end
      const fromIndex = group.tabs.findIndex((t) => t.id === effectiveTabId);
      if (fromIndex !== -1 && fromIndex !== group.tabs.length - 1) {
        tabActions.reorderTabs(fromIndex, group.tabs.length - 1, group.id);
      }
    }

    resetDragState();
  }

  function handleDrop(e: DragEvent, targetTabId: string) {
    e.preventDefault();
    e.stopPropagation();

    // Get drag info from dataTransfer for cross-group drops
    const dragTabIdFromData = e.dataTransfer?.getData('tab-id');
    const sourceGroupId = e.dataTransfer?.getData('source-group-id');

    // Determine if this is a cross-group drop
    const isCrossGroupDrop = sourceGroupId && sourceGroupId !== group.id;
    const effectiveTabId = isCrossGroupDrop ? dragTabIdFromData : draggedTabId;

    if (!effectiveTabId || effectiveTabId === targetTabId) {
      resetDragState();
      return;
    }

    // Calculate target index
    let toIndex = group.tabs.findIndex((t) => t.id === targetTabId);
    if (toIndex === -1) {
      resetDragState();
      return;
    }

    if (dropPosition === 'right') {
      toIndex = toIndex + 1;
    }

    if (isCrossGroupDrop && sourceGroupId) {
      // Cross-group drop with position
      const isCopyOperation = e.ctrlKey || e.metaKey;
      if (isCopyOperation) {
        tabActions.copyTabToGroup(effectiveTabId, group.id, sourceGroupId, toIndex);
      } else {
        tabActions.moveTabToGroup(effectiveTabId, group.id, sourceGroupId, toIndex);
      }
    } else {
      // Same group reorder
      const fromIndex = group.tabs.findIndex((t) => t.id === effectiveTabId);
      if (fromIndex === -1) {
        resetDragState();
        return;
      }

      // Adjust toIndex for same-group reorder
      if (fromIndex < toIndex) {
        toIndex = toIndex - 1;
      }
      toIndex = Math.max(0, Math.min(toIndex, group.tabs.length - 1));

      if (fromIndex !== toIndex) {
        tabActions.reorderTabs(fromIndex, toIndex, group.id);
      }
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
    crossGroupDragTabId = null;
    dragOverTrailingArea = false;
  }

  // Cross-group drag and drop handlers
  function handleGroupDragOver(e: DragEvent) {
    const hasTabData = e.dataTransfer?.types?.includes('tab-id');
    if (!hasTabData) return;

    e.preventDefault();

    const isCopyOperation = e.ctrlKey || e.metaKey;
    e.dataTransfer!.dropEffect = isCopyOperation ? 'copy' : 'move';

    // Track cross-group drag for placeholder rendering
    if (!crossGroupDragTabId) {
      crossGroupDragTabId = 'cross-group-drag'; // Marker for cross-group drag
    }
  }

  function handleGroupDragLeave(_e: DragEvent) {
    // Reserved for future visual feedback on drag leave
  }

  function handleGroupDrop(e: DragEvent) {
    e.preventDefault();

    const draggedTabIdFromData = e.dataTransfer?.getData('tab-id');
    const sourceGroupId = e.dataTransfer?.getData('source-group-id');

    if (!draggedTabIdFromData || !sourceGroupId) return;
    if (sourceGroupId === group.id) return;

    const isCopyOperation = e.ctrlKey || e.metaKey;

    if (isCopyOperation) {
      tabActions.copyTabToGroup(draggedTabIdFromData, group.id, sourceGroupId);
    } else {
      tabActions.moveTabToGroup(draggedTabIdFromData, group.id, sourceGroupId);
    }
  }

</script>

<div
  class="flex-1 flex flex-col bg-[var(--color-bg-primary)] min-w-0 relative {className}"
  role="region"
  aria-label="Editor group"
  ondragover={handleGroupDragOver}
  ondragleave={handleGroupDragLeave}
  ondrop={handleGroupDrop}
>
  <!-- Tab Bar -->
  {#if group.tabs.length > 0}
    <div
      class="relative flex items-stretch bg-[var(--color-bg-secondary)] border-b border-[var(--color-border)]"
      style="height: var(--tab-height);"
    >
      <!-- Left scroll button -->
      {#if hasOverflow}
        <IconButton
          icon="chevron-left"
          size="sm"
          onclick={scrollLeft}
          title="Scroll tabs left"
          class="flex-shrink-0 w-6 h-full rounded-none [&_svg]:translate-y-px"
        />
      {/if}

      <!-- Tabs scroll container with translateX -->
      <div
        bind:this={tabsWrapperElement}
        class="flex-1 min-w-0 overflow-x-clip overflow-y-visible h-full"
        role="tablist"
        onwheel={handleWheel}
      >
        <div
          bind:this={tabsInnerElement}
          class="flex items-stretch h-full transition-transform duration-150 ease-out"
          style="transform: translateX(-{scrollPosition}px)"
        >
          {#each group.tabs as tab, index (tab.id)}
            {@const isFirstTab = index === 0}
            {@const isLastTab = index === group.tabs.length - 1}
            {@const nextTab = index < group.tabs.length - 1 ? group.tabs[index + 1] : null}
            {@const showLeftIndicator = isFirstTab && dragOverTabId === tab.id && dropPosition === 'left'}
            {@const showRightIndicator =
              (dragOverTabId === tab.id && dropPosition === 'right') ||
              (nextTab && dragOverTabId === nextTab.id && dropPosition === 'left') ||
              (isLastTab && dragOverTrailingArea)}
            <div
              class="relative flex-shrink-0 h-full"
              role="presentation"
              data-tab-id={tab.id}
              ondragover={(e) => handleDragOver(e, tab.id)}
              ondragleave={handleDragLeave}
              ondrop={(e) => handleDrop(e, tab.id)}
            >
              {#if showLeftIndicator}
                <div class="absolute left-0 top-0 bottom-0 w-0.5 bg-[var(--color-accent-primary)] z-10"></div>
              {/if}
              {#if showRightIndicator}
                <div class="absolute right-0 top-0 bottom-0 w-0.5 bg-[var(--color-accent-primary)] z-10"></div>
              {/if}
              <TabItem
                id={tab.id}
                title={tab.title}
                meta={tab.meta}
                icon={tab.icon || 'file-text'}
                active={group.activeTab === tab.id}
                localActive={$globalActiveTab?.id === tab.id}
                dirty={tab.dirty}
                closable={tab.closable !== false}
                draggable={true}
                isDragging={draggedTabId === tab.id}
                onSelect={() => handleTabClick(tab.id)}
                onClose={() => {
                  tabActions.closeTab(tab.id, group.id);
                  onTabClose?.(tab.id);
                }}
                ondragstart={(e) => handleDragStart(e, tab.id)}
                ondragend={handleDragEnd}
                oncontextmenu={(e) => handleContextMenu(e, tab.id)}
              />
            </div>
          {/each}
          <!-- Trailing empty area for drop after last tab -->
          <div
            class="flex-1 min-w-[40px] h-full"
            role="region"
            aria-label="Drop zone for tabs"
            ondragover={handleTrailingAreaDragOver}
            ondragleave={handleTrailingAreaDragLeave}
            ondrop={handleTrailingAreaDrop}
          ></div>
        </div>
      </div>

      <!-- Right scroll button -->
      {#if hasOverflow}
        <IconButton
          icon="chevron-right"
          size="sm"
          onclick={scrollRight}
          title="Scroll tabs right"
          class="flex-shrink-0 w-6 h-full rounded-none [&_svg]:translate-y-px"
        />
      {/if}

      <!-- Tab bar actions -->
      <div class="flex-shrink-0 flex items-center gap-1 px-2">
        <IconButton
          icon="columns"
          size="sm"
          onclick={() => handleSplit('horizontal')}
          title="Split Right"
        />
        <IconButton
          icon="more-horizontal"
          size="sm"
          onclick={handleActionMenu}
          title="More Actions"
        />
      </div>
    </div>
  {/if}

  <!-- Editor Content -->
  <div
    class="flex-1 overflow-hidden relative"
    role="tabpanel"
    tabindex="0"
    aria-labelledby="tab-{activeTab?.id || 'none'}"
  >
    {#if group.tabs.length === 0}
      <!-- Welcome Empty State -->
      <Welcome />
    {:else if !activeTab}
      <!-- No active tab -->
      <div class="h-full flex items-center justify-center p-8">
        <div class="text-center space-y-4">
          <div
            class="w-12 h-12 mx-auto bg-[var(--color-bg-secondary)] rounded-lg flex items-center justify-center"
          >
            <Icon name="maximize-2" size="md" class="text-[var(--color-text-tertiary)]" />
          </div>
          <div>
            <h3 class="text-lg font-medium text-[var(--color-text-primary)] mb-2">
              No Active Tab
            </h3>
            <p class="text-[var(--color-text-secondary)]">
              Select a tab from the tab bar
            </p>
          </div>
        </div>
      </div>
    {:else}
      <!-- Render tabs with only active one visible -->
      {#each group.tabs as tab (tab.id)}
        <div
          class="absolute inset-0 h-full w-full"
          style="display: {tab.id === activeTab.id ? 'block' : 'none'}"
        >
          <ErrorBoundary>
            <DynamicContentRenderer {tab} groupId={group.id} class="h-full" />
          </ErrorBoundary>
        </div>
      {/each}
    {/if}
  </div>
</div>

<!-- Tab Context Menu -->
<ContextMenu
  visible={contextMenuVisible}
  x={contextMenuX}
  y={contextMenuY}
  items={contextMenuItems}
  onClose={closeContextMenu}
/>

<!-- Tab Bar Action Menu -->
<ContextMenu
  visible={actionMenuVisible}
  x={actionMenuX}
  y={actionMenuY}
  items={actionMenuItems}
  onClose={closeActionMenu}
/>
