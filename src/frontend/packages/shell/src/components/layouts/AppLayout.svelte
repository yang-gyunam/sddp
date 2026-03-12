<script lang="ts">
  import ActivityBar from './ActivityBar.svelte';
  import Sidebar from './Sidebar.svelte';
  import StatusBar from './StatusBar.svelte';
  import Panel from './Panel.svelte';
  import ErrorBoundary from '../idioms/ErrorBoundary.svelte';
  import {
    layoutStore,
    setActiveSidebarPanel,
    setSidebarWidth,
    toggleSidebar,
    setupFocusZoneKeyboard,
  } from '../../stores';
  import type { LayoutState, ActivityBarItem } from '../../types';
  import { APP_SIDEBAR } from '../../types';

  interface StatusItem {
    id: string;
    text: string;
    icon?: string;
    tooltip?: string;
    onClick?: () => void;
    position?: 'left' | 'right';
  }

  interface Props {
    // Activity Bar
    activityItems?: ActivityBarItem[];
    activityBottomItems?: ActivityBarItem[];

    // Sidebar
    sidebarTitle?: string;

    // Status Bar
    statusItems?: StatusItem[];
    statusBackgroundColor?: string;

    // Slots
    sidebarHeaderActions?: import('svelte').Snippet;
    sidebarContent?: import('svelte').Snippet;
    mainContent?: import('svelte').Snippet;
    statusContent?: import('svelte').Snippet;
  }

  let {
    // Activity Bar
    activityItems = [],
    activityBottomItems = [],

    // Sidebar
    sidebarTitle = '',

    // Status Bar
    statusItems = [],
    statusBackgroundColor,

    // Slots
    sidebarHeaderActions,
    sidebarContent,
    mainContent,
    statusContent,
  }: Props = $props();

  // Layout state from store
  let layoutState = $state<LayoutState | null>(null);

  $effect(() => {
    // Subscribe to layout store
    const unsub = layoutStore.subscribe((state) => {
      layoutState = state;
    });

    // Setup F6 focus zone navigation
    const cleanupFZ = setupFocusZoneKeyboard();

    return () => {
      unsub();
      cleanupFZ?.();
    };
  });

  // Derived values
  const activeActivityItem = $derived(layoutState?.activityBar.activeItemId ?? '');
  const sidebarWidth = $derived(layoutState?.sidebar.width ?? APP_SIDEBAR.width);
  const sidebarCollapsed = $derived(!layoutState?.sidebar.isOpen);
  const activeSidebarPanel = $derived(layoutState?.sidebar.activePanel);

  // Handlers
  function handleActivityItemClick(id: string) {
    setActiveSidebarPanel(id);
  }

  function handleSidebarResize(width: number) {
    setSidebarWidth(width);
  }

  function handleSidebarToggle() {
    toggleSidebar();
  }

  // Get title based on active panel
  const currentSidebarTitle = $derived(() => {
    if (!activeSidebarPanel) return sidebarTitle;
    const item = [...activityItems, ...activityBottomItems].find(
      (i) => i.id === activeSidebarPanel
    );
    return item?.label ?? sidebarTitle;
  });
</script>

<!-- Skip Link for keyboard users -->
<a
  href="#main-content"
  class="sr-only focus:not-sr-only focus:absolute focus:z-[100] focus:top-2 focus:left-2 focus:px-4 focus:py-2 focus:bg-[var(--color-primary-500)] focus:text-white focus:rounded-md focus:outline-none"
>
  Skip to main content
</a>

<div
  class="app-shell-grid h-screen w-screen overflow-hidden bg-[var(--color-bg-primary)]"
  style:--activity-bar-width="48px"
  style:--sidebar-width="{sidebarCollapsed ? '0px' : `${sidebarWidth}px`}"
  style:--statusbar-height="22px"
>
  <!-- Activity Bar (Navigation) -->
  <nav aria-label="Activity Bar" class="area-activity overflow-hidden" data-focus-zone="activity">
    <ActivityBar
      items={activityItems}
      bottomItems={activityBottomItems}
      activeItem={activeActivityItem}
      onItemClick={handleActivityItemClick}
    />
  </nav>

  <!-- Sidebar (Complementary region) -->
  <aside aria-label="Sidebar" class="area-sidebar overflow-hidden" data-focus-zone="sidebar">
    <Sidebar
      title={currentSidebarTitle()}
      width={sidebarWidth}
      collapsed={sidebarCollapsed}
      onResize={handleSidebarResize}
      onToggle={handleSidebarToggle}
      headerActions={sidebarHeaderActions}
    >
      {#if sidebarContent}
        {@render sidebarContent()}
      {/if}
    </Sidebar>
  </aside>

  <!-- Main content area with Bottom Panel -->
  <main id="main-content" class="area-main flex flex-col overflow-hidden">
    <ErrorBoundary>
      <div class="flex-1 overflow-hidden" data-focus-zone="editor">
        {#if mainContent}
          {@render mainContent()}
        {/if}
      </div>
    </ErrorBoundary>

    <!-- Bottom Panel -->
    <div data-focus-zone="panel">
      <Panel />
    </div>
  </main>

  <!-- Status Bar -->
  <footer class="area-status">
    <StatusBar items={statusItems} backgroundColor={statusBackgroundColor}>
      {#if statusContent}
        {@render statusContent()}
      {/if}
    </StatusBar>
  </footer>
</div>

<style>
  .app-shell-grid {
    display: grid;
    grid-template-columns: var(--activity-bar-width) var(--sidebar-width) 1fr;
    grid-template-rows: 1fr var(--statusbar-height);
    grid-template-areas:
      "activity sidebar main"
      "status   status  status";
  }

  .area-activity {
    grid-area: activity;
  }

  .area-sidebar {
    grid-area: sidebar;
  }

  .area-main {
    grid-area: main;
  }

  .area-status {
    grid-area: status;
  }
</style>
