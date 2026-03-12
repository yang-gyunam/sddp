<!--
  Panel Component
  VS Code-style bottom panel with tabs for terminal, problems, output
-->
<script lang="ts">
  import type { BottomPanelState, PanelTab } from '../../types';
  import { panel } from '../../stores/panel.store';
  import { clamp } from '../../utils/number.utils';
  import { IconButton, Button, ResizeHandle } from '@sddp/ui';
  import ProblemsContent from '../idioms/ProblemsContent.svelte';
  import OutputContent from '../idioms/OutputContent.svelte';

  interface Props {
    height?: number;
    onToggle?: () => void;
    onResize?: (height: number) => void;
  }

  let { height = $bindable(200), onToggle, onResize }: Props = $props();

  // State from store
  let collapsed = $state(true);
  let activeTab = $state('terminal');
  let tabs: PanelTab[] = $state([]);
  let resizing = $state(false);
  let minHeight = $state(100);
  let maxHeight = $state(600);

  // Subscribe to panel state
  $effect(() => {
    const unsubscribe = panel.subscribe((state: BottomPanelState) => {
      collapsed = state.collapsed;
      activeTab = state.activeTab;
      tabs = state.tabs;
      resizing = state.resizing;
      minHeight = state.minHeight;
      maxHeight = state.maxHeight;
      if (state.height !== height) {
        height = state.height;
      }
    });
    return unsubscribe;
  });

  // Handle panel resize
  function startResize(event: MouseEvent) {
    event.preventDefault();
    resizing = true;
    panel.startResize();

    const startY = event.clientY;
    const startHeight = height;

    function handleMouseMove(e: MouseEvent) {
      const deltaY = startY - e.clientY;
      const newHeight = clamp(startHeight + deltaY, minHeight, maxHeight);
      height = newHeight;
      panel.setHeight(newHeight);
      onResize?.(newHeight);
    }

    function handleMouseUp() {
      resizing = false;
      panel.endResize();
      document.removeEventListener('mousemove', handleMouseMove);
      document.removeEventListener('mouseup', handleMouseUp);
      document.body.style.cursor = '';
      document.body.style.userSelect = '';
    }

    document.addEventListener('mousemove', handleMouseMove);
    document.addEventListener('mouseup', handleMouseUp);
    document.body.style.cursor = 'row-resize';
    document.body.style.userSelect = 'none';
  }

  // Handle tab change
  function handleTabChange(tabId: string) {
    panel.setActiveTab(tabId);
  }

  // Handle panel toggle
  function handleToggle() {
    panel.toggle();
    onToggle?.();
  }

  // Handle close panel
  function handleClose() {
    panel.hide();
  }

  // Get current active tab
  const currentTab = $derived(tabs.find((t) => t.id === activeTab) || tabs[0]);

  // Keyboard shortcuts
  function handleKeyDown(e: KeyboardEvent) {
    // Ctrl+J to toggle panel
    if (e.ctrlKey && e.key === 'j') {
      e.preventDefault();
      handleToggle();
    }
  }

  $effect(() => {
    document.addEventListener('keydown', handleKeyDown);
    return () => document.removeEventListener('keydown', handleKeyDown);
  });
</script>

{#if !collapsed}
  <div class="relative flex flex-col border-t border-[var(--color-border)]">
    <!-- Resize Handle (overlays the border) -->
    <div class="absolute top-0 left-0 right-0 -translate-y-1/2 z-10">
      <ResizeHandle orientation="horizontal" onmousedown={startResize} isResizing={resizing} />
    </div>

    <!-- Panel Content -->
    <div class="bg-[var(--color-bg-panel)]" style="height: {height}px">
      <div class="h-full flex flex-col">
        <!-- Panel Header with Tabs (VS Code style) -->
        <div
          class="h-9 bg-[var(--color-bg-panel)] border-b border-[var(--color-border)] flex items-center justify-between pl-4 pr-2"
        >
          <!-- Tabs -->
          <div class="flex items-center h-full">
            {#each tabs as tab (tab.id)}
              <Button
                variant="unstyled"
                onclick={() => handleTabChange(tab.id)}
                class="h-full px-3 text-xs font-medium uppercase tracking-wide transition-colors flex items-center gap-2 border-b-2 -mb-px
                  {tab.id === activeTab
                  ? 'text-[var(--color-text-primary)] border-[var(--color-accent-primary)]'
                  : 'text-[var(--color-text-tertiary)] border-transparent hover:text-[var(--color-text-secondary)]'}"
              >
                <span>{tab.label}</span>
                {#if tab.badge && tab.badge > 0}
                  <span
                    class="px-1.5 py-0.5 text-xs bg-[var(--color-accent-primary)] text-[var(--color-text-on-accent)] rounded-full min-w-[1.25rem] text-center"
                  >
                    {tab.badge > 99 ? '99+' : tab.badge}
                  </span>
                {/if}
              </Button>
            {/each}
          </div>

          <!-- Action Buttons -->
          <div class="flex items-center gap-1">
            <IconButton icon="x" title="Close Panel" onclick={handleClose} size="sm" />
          </div>
        </div>

        <!-- Tab Content -->
        <div class="flex-1 overflow-hidden bg-[var(--color-bg-section)]">
          {#if currentTab}
            <div class="h-full">
              {#if currentTab.id === 'problems'}
                <ProblemsContent />
              {:else if currentTab.id === 'output'}
                <OutputContent />
              {:else}
                <div class="p-4 text-[var(--color-text-tertiary)]">
                  Select a tab to view content.
                </div>
              {/if}
            </div>
          {/if}
        </div>
      </div>
    </div>
  </div>
{/if}
