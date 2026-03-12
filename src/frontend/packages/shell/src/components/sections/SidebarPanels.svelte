<!--
  Sidebar Panels Section
  VSCode-style collapsible panels for different activities
  Supports vertical resizing between panels
-->
<script lang="ts">
  import { untrack } from 'svelte';
  import { SvelteSet } from 'svelte/reactivity';
  import CollapsiblePanel from '../idioms/CollapsiblePanel.svelte';
  import TreeView from '../idioms/TreeView.svelte';
  import { Spinner, ResizeHandle } from '@sddp/ui';
  import EmptyState from '../idioms/EmptyState.svelte';
  import type {
    ActivityPanelMap,
    SidebarPanelActionMap,
    SidebarPanelContentMap,
    SidebarPanelLoadingMap,
    TreeNode,
  } from '../../types';

  interface SidebarPanelsProps {
    activity: string;
    activityPanels?: ActivityPanelMap;
    panelContents?: SidebarPanelContentMap;
    panelContentRenderer?: import('svelte').Snippet<[string]>;
    panelActions?: SidebarPanelActionMap;
    panelLoading?: SidebarPanelLoadingMap;
    menus?: TreeNode[];
    selectedMenuId?: string | null;
    onMenuSelect?: (menu: TreeNode) => void;
    onPanelToggle?: (panelId: string, expanded: boolean) => void;
    class?: string;
  }

  // Define panels for each activity - SDDP domain
  const DEFAULT_ACTIVITY_PANELS: ActivityPanelMap = {
    // Conversations activity
    conversations: [
      { id: 'channels', title: 'Channels', icon: 'message-square' },
      { id: 'direct-messages', title: 'Direct Messages', icon: 'message-circle' },
    ],
    // Specs activity
    specs: [
      { id: 'all-specs', title: 'All Specs', icon: 'file-signature' },
      { id: 'my-specs', title: 'My Specs', icon: 'user' },
      { id: 'recent', title: 'Recent', icon: 'history' },
    ],
    // Generator activity
    generator: [
      { id: 'targets', title: 'Generation Targets', icon: 'cpu' },
      { id: 'templates', title: 'Templates', icon: 'file-code' },
      { id: 'output', title: 'Output', icon: 'folder' },
    ],
    // Dashboard activity
    dashboard: [
      { id: 'overview', title: 'Overview', icon: 'layout-dashboard' },
      { id: 'activity', title: 'Activity', icon: 'clock' },
    ],
    // Settings activity
    settings: [
      { id: 'preferences', title: 'Preferences', icon: 'settings' },
      { id: 'shortcuts', title: 'Keyboard Shortcuts', icon: 'keyboard' },
      { id: 'themes', title: 'Themes', icon: 'palette' },
    ],
    // User/Account activity
    user: [
      { id: 'profile', title: 'Profile', icon: 'account' },
      { id: 'account', title: 'Account Settings', icon: 'settings' },
    ],
  };

  let {
    activity = 'explorer',
    activityPanels = DEFAULT_ACTIVITY_PANELS,
    panelContents = {},
    panelContentRenderer,
    panelActions = {},
    panelLoading = {},
    menus = [],
    selectedMenuId = null,
    onMenuSelect,
    onPanelToggle,
    class: className = '',
  }: SidebarPanelsProps = $props();

  // Panel height state (flex-grow values)
  let panelHeights = $state<Record<string, number>>({});

  // Resize state
  let isResizing = $state(false);
  let resizeIndex = $state<number | null>(null);
  let startY = $state(0);
  let startHeights = $state<{ above: number; below: number }>({ above: 1, below: 1 });
  let startTotalFlex = $state(0);
  let containerRef = $state<HTMLDivElement | undefined>();

  // Minimum height for panels (in flex units, relative)
  const MIN_FLEX = 0.2;

  // Local state for expanded panels
  let expandedPanels = new SvelteSet<string>();

  // Local state for expanded tree nodes
  let expandedTreeNodes = new SvelteSet<string>();

  // Track last expanded menu ID to avoid re-expanding on manual collapse
  let lastRevealedId: string | null = null;

  let panelConfigs = $derived(activityPanels);
  let panelContentMap = $derived(panelContents);
  let panelActionMap = $derived(panelActions);
  let panelLoadingMap = $derived(panelLoading);

  // Load panel state from localStorage
  function loadPanelState(activityId: string): Set<string> | null {
    try {
      const saved = localStorage.getItem(`sidebar-panels-${activityId}`);
      if (saved) {
        return new Set(JSON.parse(saved));
      }
    } catch (error) {
      console.warn('Failed to load panel state:', error);
    }
    return null;
  }

  // Save panel state to localStorage
  function savePanelState(activityId: string, panels: Set<string>) {
    try {
      localStorage.setItem(`sidebar-panels-${activityId}`, JSON.stringify([...panels]));
    } catch (error) {
      console.warn('Failed to save panel state:', error);
    }
  }

  // Initialize expanded panels when activity changes
  $effect(() => {
    const panels = panelConfigs[activity] || [];
    expandedPanels.clear();
    if (panels.length > 0) {
      const savedState = loadPanelState(activity);
      if (savedState !== null) {
        for (const id of savedState) {
          expandedPanels.add(id);
        }
      } else {
        // First time: expand all panels
        for (const p of panels) {
          expandedPanels.add(p.id);
        }
        savePanelState(activity, expandedPanels);
      }
    }
  });

  function handlePanelToggle(panelId: string, expanded: boolean) {
    if (expanded) {
      expandedPanels.add(panelId);
    } else {
      expandedPanels.delete(panelId);
    }

    savePanelState(activity, expandedPanels);
    onPanelToggle?.(panelId, expanded);
  }

  function handleMenuSelect(menu: TreeNode) {
    onMenuSelect?.(menu);
  }

  // Handle tree node toggle (expand/collapse)
  function handleTreeToggle(nodeId: string) {
    if (expandedTreeNodes.has(nodeId)) {
      expandedTreeNodes.delete(nodeId);
    } else {
      expandedTreeNodes.add(nodeId);
    }
  }

  // Find all ancestor node IDs for a given nodeId (from root to parent)
  function findAncestorIds(nodes: TreeNode[], targetId: string, path: string[] = []): string[] | null {
    for (const node of nodes) {
      if (node.id === targetId) {
        return path; // Found! Return the path (ancestors only, not including target)
      }
      if (node.children && node.children.length > 0) {
        const result = findAncestorIds(node.children, targetId, [...path, node.id]);
        if (result !== null) {
          return result;
        }
      }
    }
    return null; // Not found in this branch
  }

  // Auto-expand ancestors when selectedMenuId changes (reveal in tree)
  $effect(() => {
    const currentSelectedId = selectedMenuId;

    // Only react to selectedMenuId changes, untrack everything else
    untrack(() => {
      if (!currentSelectedId || currentSelectedId === lastRevealedId) {
        return;
      }

      lastRevealedId = currentSelectedId;

      const ancestorIds = findAncestorIds(menus, currentSelectedId);
      if (ancestorIds && ancestorIds.length > 0) {
        for (const ancestorId of ancestorIds) {
          expandedTreeNodes.add(ancestorId);
        }
      }
    });
  });

  // Get current panels
  let currentPanels = $derived(panelConfigs[activity] || []);

  // Get expanded panels for resize calculations
  let expandedPanelsList = $derived(
    currentPanels.filter(p => expandedPanels.has(p.id))
  );

  // Initialize panel heights when activity or expanded panels change
  $effect(() => {
    const panels = currentPanels;
    // Track expandedPanels for reactivity (value not used directly)
    void expandedPanels;

    untrack(() => {
      const newHeights: Record<string, number> = {};
      for (const panel of panels) {
        // Preserve existing height or default to 1
        newHeights[panel.id] = panelHeights[panel.id] ?? 1;
      }
      panelHeights = newHeights;
    });
  });

  // Get flex value for a panel
  function getPanelFlex(panelId: string): number {
    return panelHeights[panelId] ?? 1;
  }

  // Resize handlers
  function handleResizeStart(e: PointerEvent, index: number) {
    e.preventDefault();

    const expandedList = expandedPanelsList;
    if (index < 0 || index >= expandedList.length - 1) return;

    const abovePanel = expandedList[index];
    const belowPanel = expandedList[index + 1];
    if (!abovePanel || !belowPanel) return;

    isResizing = true;
    resizeIndex = index;
    startY = e.clientY;
    startHeights = {
      above: panelHeights[abovePanel.id] ?? 1,
      below: panelHeights[belowPanel.id] ?? 1,
    };
    startTotalFlex = expandedList.reduce(
      (sum, panel) => sum + (panelHeights[panel.id] ?? 1),
      0
    );

    const target = e.target as HTMLElement;
    target.setPointerCapture(e.pointerId);

    document.addEventListener('pointermove', handleResizeMove);
    document.addEventListener('pointerup', handleResizeEnd);
    document.body.style.cursor = 'row-resize';
    document.body.style.userSelect = 'none';
  }

  function handleResizeMove(e: PointerEvent) {
    if (!isResizing || resizeIndex === null || !containerRef) return;

    const expandedList = expandedPanelsList;
    const abovePanel = expandedList[resizeIndex];
    const belowPanel = expandedList[resizeIndex + 1];
    if (!abovePanel || !belowPanel) return;

    const containerHeight = containerRef.clientHeight;
    const deltaY = e.clientY - startY;
    if (containerHeight === 0 || startTotalFlex === 0) return;
    const deltaFlex = (deltaY / containerHeight) * startTotalFlex;

    let newAbove = startHeights.above + deltaFlex;
    let newBelow = startHeights.below - deltaFlex;

    // Enforce minimum heights
    if (newAbove < MIN_FLEX) {
      newBelow += (newAbove - MIN_FLEX);
      newAbove = MIN_FLEX;
    }
    if (newBelow < MIN_FLEX) {
      newAbove += (newBelow - MIN_FLEX);
      newBelow = MIN_FLEX;
    }

    // Clamp to valid range
    newAbove = Math.max(MIN_FLEX, newAbove);
    newBelow = Math.max(MIN_FLEX, newBelow);

    panelHeights[abovePanel.id] = newAbove;
    panelHeights[belowPanel.id] = newBelow;
    panelHeights = { ...panelHeights };
  }

  function handleResizeEnd(e: PointerEvent) {
    isResizing = false;
    resizeIndex = null;

    const target = e.target as HTMLElement;
    if (target.hasPointerCapture?.(e.pointerId)) {
      target.releasePointerCapture(e.pointerId);
    }

    document.removeEventListener('pointermove', handleResizeMove);
    document.removeEventListener('pointerup', handleResizeEnd);
    document.body.style.cursor = '';
    document.body.style.userSelect = '';
  }

  // Get the index in expanded panels list for a panel
  function getExpandedIndex(panelId: string): number {
    return expandedPanelsList.findIndex(p => p.id === panelId);
  }

  // Check if resize handle should be shown after this panel
  function shouldShowResizeHandle(panelId: string): boolean {
    const idx = getExpandedIndex(panelId);
    return idx >= 0 && idx < expandedPanelsList.length - 1;
  }
</script>

<div bind:this={containerRef} class="flex flex-col h-full overflow-hidden {className}">
  {#if currentPanels.length > 0}
    {#each currentPanels as panel (panel.id)}
      {@const panelContent = panelContentMap[panel.id]}
      {@const panelActionItems = panelActionMap[panel.id] || []}
      {@const isLoading = panelLoadingMap[panel.id] || false}
      <!-- Panel wrapper with flex sizing for expanded panels -->
      <div
        class="flex flex-col min-h-0 {expandedPanels.has(panel.id) ? 'flex-1 overflow-hidden' : 'flex-shrink-0'} {isResizing ? '' : 'transition-[flex-grow] duration-300 ease-out'}"
        style="flex-grow: {expandedPanels.has(panel.id) ? getPanelFlex(panel.id) : 0}"
      >
        <CollapsiblePanel
          title={panel.title}
          expanded={expandedPanels.has(panel.id)}
          badge={panel.badge}
          actions={panelActionItems}
          skipSlide={true}
          onToggle={(expanded) => handlePanelToggle(panel.id, expanded)}
          class="h-full"
        >
          {#if panelContent}
            {@render panelContent()}
          {:else if panelContentRenderer}
            {@render panelContentRenderer(panel.id)}
          {:else if isLoading}
            <div class="flex items-center justify-center h-full py-6">
              <Spinner size="lg" />
            </div>
          {:else if (panel.id === 'channels' || panel.id === 'all-specs') && menus.length > 0}
            <!-- Tree view for navigable items -->
            <TreeView
              nodes={menus}
              selectedId={selectedMenuId}
              expandedIds={expandedTreeNodes}
              onSelect={handleMenuSelect}
              onToggle={handleTreeToggle}
            />
          {:else}
            <EmptyState
              icon={panel.icon || 'inbox'}
              heading="No items yet"
              subtext="Connect a data source to show content."
              class="py-6"
            />
          {/if}
        </CollapsiblePanel>
      </div>

      <!-- Resize handle between expanded panels -->
      {#if shouldShowResizeHandle(panel.id)}
        {@const handleIndex = getExpandedIndex(panel.id)}
        <ResizeHandle
          orientation="horizontal"
          onpointerdown={(e) => handleResizeStart(e, handleIndex)}
          isResizing={isResizing && resizeIndex === handleIndex}
          ariaLabel="Resize panels"
          class="flex-shrink-0 relative z-10"
        />
      {/if}
    {/each}
  {:else}
    <!-- No panels for this activity -->
    <EmptyState icon="inbox" heading="No panels available" class="flex-1" />
  {/if}
</div>
