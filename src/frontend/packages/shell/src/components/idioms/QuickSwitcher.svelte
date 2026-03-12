<script lang="ts">
  import { get } from 'svelte/store';
  import { Icon, Button, Input } from '@sddp/ui';
  import { globalActiveTab, tabActions, editorGroups } from '../../stores';
  import type { Tab } from '../../types';

  interface Props {
    visible?: boolean;
    onclose?: () => void;
  }

  let {
    visible = $bindable(false),
    onclose,
  }: Props = $props();

  let searchQuery = $state('');
  let selectedIndex = $state(0);
  let inputElement: HTMLInputElement | undefined = $state(undefined);

  // Get all tabs from all editor groups
  let allTabs = $state<Tab[]>([]);
  let recentTabs = $state<Tab[]>([]);

  $effect(() => {
    const unsubscribe = editorGroups.subscribe((groups) => {
      const tabs: Tab[] = [];
      for (const group of groups) {
        tabs.push(...group.tabs);
      }
      allTabs = tabs;

      // Sort by most recently accessed (active tab first)
      const activeTab = get(globalActiveTab);
      const activeTabId = activeTab?.id;
      recentTabs = [...tabs].sort((a, b) => {
        // Active tab goes to top
        if (a.id === activeTabId) return -1;
        if (b.id === activeTabId) return 1;
        return 0;
      });
    });
    return unsubscribe;
  });

  // Filter tabs based on search query
  let filteredTabs = $derived(() => {
    if (!searchQuery.trim()) {
      return recentTabs;
    }
    const query = searchQuery.toLowerCase();
    return allTabs.filter((tab) =>
      tab.title.toLowerCase().includes(query) ||
      tab.path?.toLowerCase().includes(query)
    );
  });

  // Reset state when opened
  $effect(() => {
    if (visible) {
      searchQuery = '';
      selectedIndex = 0;
      // Focus input after a tick
      setTimeout(() => {
        inputElement?.focus();
      }, 0);
    }
  });

  function handleKeyDown(e: KeyboardEvent) {
    const tabs = filteredTabs();

    switch (e.key) {
      case 'ArrowDown':
        e.preventDefault();
        selectedIndex = Math.min(selectedIndex + 1, tabs.length - 1);
        break;
      case 'ArrowUp':
        e.preventDefault();
        selectedIndex = Math.max(selectedIndex - 1, 0);
        break;
      case 'Enter':
        e.preventDefault();
        if (tabs[selectedIndex]) {
          selectTab(tabs[selectedIndex]!);
        }
        break;
      case 'Escape':
        e.preventDefault();
        close();
        break;
      case 'Tab':
        e.preventDefault();
        if (e.shiftKey) {
          selectedIndex = Math.max(selectedIndex - 1, 0);
        } else {
          selectedIndex = Math.min(selectedIndex + 1, tabs.length - 1);
        }
        inputElement?.focus();
        break;
    }
  }

  function selectTab(tab: Tab) {
    tabActions.switchToTab(tab.id);
    close();
  }

  function close() {
    visible = false;
    onclose?.();
  }

  function handleBackdropClick(e: MouseEvent) {
    if (e.target === e.currentTarget) {
      close();
    }
  }
</script>

{#if visible}
  <div
    class="fixed inset-0 z-50 flex items-start justify-center pt-[15vh] bg-[var(--color-overlay)]/50"
    onclick={handleBackdropClick}
    onkeydown={handleKeyDown}
    role="dialog"
    aria-modal="true"
    aria-label="Quick Switcher"
    tabindex="-1"
  >
    <div class="w-full max-w-lg bg-[var(--color-bg-primary)] rounded-lg shadow-2xl border border-[var(--color-border)] overflow-hidden">
      <!-- Search Input -->
      <div class="flex items-center gap-3 px-4 py-3 border-b border-[var(--color-border)]">
        <Icon name="search" size="sm" class="text-[var(--color-text-tertiary)]" />
        <Input
          unstyled
          bind:element={inputElement}
          bind:value={searchQuery}
          placeholder="Search open files..."
          class="flex-1 bg-transparent border-none outline-none text-sm text-[var(--color-text-primary)] placeholder:text-[var(--color-text-tertiary)]"
        />
        <kbd class="px-1.5 py-0.5 text-xs bg-[var(--color-bg-tertiary)] text-[var(--color-text-tertiary)] rounded">ESC</kbd>
      </div>

      <!-- Results List -->
      <div class="max-h-80 overflow-y-auto">
        {#if filteredTabs().length === 0}
          <div class="px-4 py-8 text-center text-sm text-[var(--color-text-tertiary)]">
            {searchQuery ? 'No matching files found' : 'No open files'}
          </div>
        {:else}
          <ul class="py-2">
            {#each filteredTabs() as tab, index (tab.id)}
              <li>
                <Button
                  variant="unstyled"
                  onclick={() => selectTab(tab)}
                  class="w-full flex items-center gap-3 px-4 py-2 text-left transition-colors
                    {index === selectedIndex
                      ? 'bg-[var(--color-primary-500)]/10 text-[var(--color-primary-600)] dark:text-[var(--color-primary-400)]'
                      : 'text-[var(--color-text-primary)] hover:bg-[var(--color-bg-secondary)]'}"
                >
                  <Icon name={tab.icon || 'file'} size="sm" class="flex-shrink-0" />
                  <div class="flex-1 min-w-0">
                    <div class="text-sm font-medium truncate">
                      {tab.title}
                      {#if tab.dirty}
                        <span class="text-[var(--color-warning-500)]">●</span>
                      {/if}
                    </div>
                    {#if tab.path}
                      <div class="text-xs text-[var(--color-text-tertiary)] truncate">{tab.path}</div>
                    {/if}
                  </div>
                </Button>
              </li>
            {/each}
          </ul>
        {/if}
      </div>

      <!-- Footer -->
      <div class="flex items-center justify-between px-4 py-2 border-t border-[var(--color-border)] bg-[var(--color-bg-secondary)]">
        <div class="flex items-center gap-4 text-xs text-[var(--color-text-tertiary)]">
          <span class="flex items-center gap-1">
            <kbd class="px-1 py-0.5 bg-[var(--color-bg-tertiary)] rounded">↑</kbd>
            <kbd class="px-1 py-0.5 bg-[var(--color-bg-tertiary)] rounded">↓</kbd>
            Navigate
          </span>
          <span class="flex items-center gap-1">
            <kbd class="px-1 py-0.5 bg-[var(--color-bg-tertiary)] rounded">Enter</kbd>
            Select
          </span>
        </div>
        <span class="text-xs text-[var(--color-text-tertiary)]">
          {filteredTabs().length} file{filteredTabs().length !== 1 ? 's' : ''}
        </span>
      </div>
    </div>
  </div>
{/if}
