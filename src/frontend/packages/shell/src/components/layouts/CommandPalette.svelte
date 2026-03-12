<!--
  CommandPalette Component
  VS Code-style command palette for quick actions and navigation
-->
<script lang="ts">
  import type { Command, CommandGroup, CommandPaletteState, CommandCategory } from '../../types';
  import { commandPalette } from '../../stores/command.store';
  import { Icon, Button, Input } from '@sddp/ui';
  import EmptyState from '../idioms/EmptyState.svelte';

  interface Props {
    maxHeight?: number;
    placeholder?: string;
  }

  const DEFAULT_MAX_HEIGHT = 400;
  let { maxHeight = DEFAULT_MAX_HEIGHT, placeholder = 'Type a command or search...' }: Props = $props();

  let inputElement: HTMLInputElement | undefined = $state(undefined);
  let commandListElement: HTMLDivElement | undefined = $state(undefined);

  // State from store
  let visible = $state(false);
  let query = $state('');
  let selectedIndex = $state(0);
  let filteredCommands: Command[] = $state([]);
  let categories: CommandCategory[] = $state([]);

  // Subscribe to command palette state
  $effect(() => {
    const unsubscribe = commandPalette.subscribe((state: CommandPaletteState) => {
      visible = state.visible;
      query = state.query;
      selectedIndex = state.selectedIndex;
      filteredCommands = state.filteredCommands;
      categories = state.categories;
    });
    return unsubscribe;
  });

  // Focus input when palette becomes visible
  $effect(() => {
    if (visible && inputElement) {
      setTimeout(() => {
        inputElement?.focus();
      }, 0);
    }
  });

  // Scroll selected item into view
  $effect(() => {
    if (visible && commandListElement && filteredCommands.length > 0) {
      const items = commandListElement.querySelectorAll('[data-command-item]');
      const selectedElement = items[selectedIndex] as HTMLElement | undefined;
      if (selectedElement) {
        selectedElement.scrollIntoView({
          behavior: 'smooth',
          block: 'nearest',
        });
      }
    }
  });

  // Handle keyboard navigation
  function handleKeyDown(event: KeyboardEvent) {
    if (!visible) return;

    switch (event.key) {
      case 'Escape':
        event.preventDefault();
        commandPalette.hide();
        break;

      case 'ArrowDown':
        event.preventDefault();
        commandPalette.selectNext();
        break;

      case 'ArrowUp':
        event.preventDefault();
        commandPalette.selectPrevious();
        break;

      case 'Enter':
        event.preventDefault();
        if (filteredCommands.length > 0) {
          commandPalette.executeSelected();
        }
        break;

      case 'Tab':
        event.preventDefault();
        if (event.shiftKey) {
          commandPalette.selectPrevious();
        } else {
          commandPalette.selectNext();
        }
        inputElement?.focus();
        break;
    }
  }

  // Handle input change
  function handleInputChange(e: Event) {
    const target = e.target as HTMLInputElement;
    commandPalette.setQuery(target.value);
  }

  // Handle command click
  function handleCommandClick(index: number) {
    commandPalette.selectIndex(index);
    commandPalette.executeSelected();
  }

  // Handle backdrop click
  function handleBackdropClick(event: MouseEvent) {
    if (event.target === event.currentTarget) {
      commandPalette.hide();
    }
  }

  // Get category label
  function getCategoryLabel(categoryId: string): string {
    const category = categories.find((c: CommandCategory) => c.id === categoryId);
    return category?.label || categoryId;
  }

  // Get category icon
  function getCategoryIcon(categoryId: string): string {
    const category = categories.find((c: CommandCategory) => c.id === categoryId);
    return category?.icon || 'folder';
  }

  // Group commands by category
  const groupedCommands = $derived.by((): CommandGroup[] => {
    const groups: Record<string, Command[]> = {};

    filteredCommands.forEach((command: Command) => {
      if (!groups[command.category]) {
        groups[command.category] = [];
      }
      groups[command.category]!.push(command);
    });

    // Sort categories by priority
    const sortedCategories = Object.keys(groups).sort((a, b) => {
      const aPriority = categories.find((c: CommandCategory) => c.id === a)?.priority || 999;
      const bPriority = categories.find((c: CommandCategory) => c.id === b)?.priority || 999;
      return aPriority - bPriority;
    });

    return sortedCategories.map((categoryId) => ({
      categoryId,
      categoryLabel: getCategoryLabel(categoryId),
      categoryIcon: getCategoryIcon(categoryId),
      commands: groups[categoryId]!,
    }));
  });

  // Get flat command index for keyboard navigation
  function getFlatCommandIndex(categoryIndex: number, commandIndex: number): number {
    let flatIndex = 0;
    for (let i = 0; i < categoryIndex; i++) {
      flatIndex += groupedCommands[i]?.commands.length ?? 0;
    }
    return flatIndex + commandIndex;
  }

  // Check if command is selected
  function isCommandSelected(categoryIndex: number, commandIndex: number): boolean {
    return getFlatCommandIndex(categoryIndex, commandIndex) === selectedIndex;
  }

  // Resize
  const MIN_HEIGHT = 200;
  const MIN_WIDTH = 384;
  const DEFAULT_WIDTH = 672;
  let isResized = $state(false);
  let currentHeight = $state(DEFAULT_MAX_HEIGHT);
  let currentWidth = $state(DEFAULT_WIDTH);

  $effect(() => {
    if (!isResized) {
      currentHeight = maxHeight;
    }
  });

  function startResize(e: MouseEvent, type: 's' | 'e' | 'se') {
    e.preventDefault();
    e.stopPropagation();
    isResized = true;
    const startX = e.clientX;
    const startY = e.clientY;
    const startW = currentWidth;
    const startH = currentHeight;

    function onMove(ev: MouseEvent) {
      if (type === 's' || type === 'se') {
        currentHeight = Math.max(MIN_HEIGHT, Math.min(startH + (ev.clientY - startY), window.innerHeight - 100));
      }
      if (type === 'e' || type === 'se') {
        currentWidth = Math.max(MIN_WIDTH, Math.min(startW + (ev.clientX - startX) * 2, window.innerWidth - 32));
      }
    }

    function onUp() {
      document.removeEventListener('mousemove', onMove);
      document.removeEventListener('mouseup', onUp);
      document.body.style.cursor = '';
      document.body.style.userSelect = '';
    }

    document.body.style.cursor = type === 's' ? 's-resize' : type === 'e' ? 'e-resize' : 'se-resize';
    document.body.style.userSelect = 'none';
    document.addEventListener('mousemove', onMove);
    document.addEventListener('mouseup', onUp);
  }

  // Setup keyboard listeners
  $effect(() => {
    document.addEventListener('keydown', handleKeyDown);
    return () => document.removeEventListener('keydown', handleKeyDown);
  });
</script>

<!-- Command Palette Modal -->
{#if visible}
  <div
    class="fixed inset-0 z-50 flex items-start justify-center pt-20 bg-[var(--color-overlay)]/50"
    onclick={handleBackdropClick}
    onkeydown={(e) => e.key === 'Escape' && commandPalette.hide()}
    role="dialog"
    aria-modal="true"
    aria-label="Command Palette"
    tabindex="-1"
  >
    <div
      class="flex flex-col mx-4 bg-[var(--color-bg-secondary)] border border-[var(--color-border)] rounded-lg shadow-2xl overflow-hidden relative"
      style="height: {currentHeight}px; width: {currentWidth}px; max-width: calc(100vw - 32px);"
    >
      <!-- Search Input -->
      <div class="flex-shrink-0 p-4 border-b border-[var(--color-border)]">
        <div class="relative">
          <div class="absolute left-3 top-1/2 transform -translate-y-1/2 text-[var(--color-text-tertiary)]">
            <Icon name="search" size="sm" />
          </div>
          <Input
            unstyled
            bind:element={inputElement}
            value={query}
            {placeholder}
            oninput={handleInputChange}
            class="w-full pl-10 pr-4 py-2 bg-[var(--color-bg-primary)] border border-[var(--color-border)] rounded-md text-[var(--color-text-primary)] placeholder:text-[var(--color-text-tertiary)] focus:border-[var(--color-accent-primary)] focus:outline-none"
          />
        </div>
      </div>

      <!-- Commands List -->
      <div
        class="flex-1 min-h-0 overflow-y-auto"
        bind:this={commandListElement}
      >
        {#if filteredCommands.length === 0}
          {#if query.trim()}
            <EmptyState
              icon="search"
              heading="No commands found"
              subtext="Try a different search term"
              iconSize="lg"
            />
          {:else}
            <EmptyState
              icon="terminal"
              heading="No commands available"
              subtext="Commands will appear here when available"
              iconSize="lg"
            />
          {/if}
        {:else}
          {#each groupedCommands as group, categoryIndex (group.categoryId)}
            <div class="py-2">
              <!-- Category Header -->
              <div class="px-4 py-2 text-xs font-semibold text-[var(--color-text-tertiary)] uppercase tracking-wide flex items-center gap-2">
                <Icon name={group.categoryIcon} size="sm" />
                <span>{group.categoryLabel}</span>
              </div>

              <!-- Commands in Category -->
              {#each group.commands as command, commandIndex (command.id)}
                {@const isSelected = isCommandSelected(categoryIndex, commandIndex)}
                <div data-command-item>
                <Button
                  variant="unstyled"
                  class="w-full px-4 py-3 text-left transition-colors focus:outline-none {isSelected
                    ? 'bg-[var(--color-accent-primary)] text-[var(--color-text-on-accent)]'
                    : 'text-[var(--color-text-primary)] hover:bg-[var(--color-bg-tertiary)]'}"
                  onclick={() => handleCommandClick(getFlatCommandIndex(categoryIndex, commandIndex))}
                >
                  <div class="flex items-center justify-between">
                    <div class="flex items-center gap-3 min-w-0 flex-1">
                      {#if command.icon}
                        <Icon
                          name={command.icon}
                          size="sm"
                          class={isSelected ? 'text-[var(--color-text-on-accent)]' : 'text-[var(--color-text-tertiary)]'}
                        />
                      {/if}
                      <div class="min-w-0 flex-1">
                        <div class="font-medium truncate">
                          {command.label}
                        </div>
                        {#if command.description}
                          <div
                            class="text-sm truncate {isSelected
                              ? 'text-[var(--color-text-on-accent)]/80'
                              : 'text-[var(--color-text-tertiary)]'}"
                          >
                            {command.description}
                          </div>
                        {/if}
                      </div>
                    </div>

                    {#if command.keybinding}
                      <div class="ml-3 flex-shrink-0">
                        <kbd
                          class="px-2 py-1 text-xs font-mono rounded border {isSelected
                            ? 'bg-[var(--color-neutral-50)]/20 text-[var(--color-text-on-accent)] border-[var(--color-neutral-50)]/30'
                            : 'bg-[var(--color-bg-primary)] text-[var(--color-text-tertiary)] border-[var(--color-border)]'}"
                        >
                          {command.keybinding}
                        </kbd>
                      </div>
                    {/if}
                  </div>
                </Button>
                </div>
              {/each}
            </div>
          {/each}
        {/if}
      </div>

      <!-- Footer -->
      <div class="flex-shrink-0 px-4 py-3 bg-[var(--color-bg-primary)] border-t border-[var(--color-border)]">
        <div class="flex items-center justify-between text-xs text-[var(--color-text-tertiary)]">
          <div class="flex items-center gap-4">
            <div class="flex items-center gap-1">
              <kbd class="px-1 py-0.5 bg-[var(--color-bg-secondary)] rounded border border-[var(--color-border)] text-xs"
                >&#8593;&#8595;</kbd
              >
              <span>Navigate</span>
            </div>
            <div class="flex items-center gap-1">
              <kbd class="px-1 py-0.5 bg-[var(--color-bg-secondary)] rounded border border-[var(--color-border)] text-xs"
                >Enter</kbd
              >
              <span>Execute</span>
            </div>
            <div class="flex items-center gap-1">
              <kbd class="px-1 py-0.5 bg-[var(--color-bg-secondary)] rounded border border-[var(--color-border)] text-xs"
                >Esc</kbd
              >
              <span>Close</span>
            </div>
          </div>
          <div>
            {filteredCommands.length} command{filteredCommands.length !== 1 ? 's' : ''}
          </div>
        </div>
      </div>

      <!-- Resize handles -->
      <!-- svelte-ignore a11y_no_static_element_interactions -->
      <div
        class="absolute bottom-0 left-2 right-2 h-1.5 cursor-s-resize"
        onmousedown={(e) => startResize(e, 's')}
      ></div>
      <!-- svelte-ignore a11y_no_static_element_interactions -->
      <div
        class="absolute top-2 right-0 bottom-2 w-1.5 cursor-e-resize"
        onmousedown={(e) => startResize(e, 'e')}
      ></div>
      <!-- svelte-ignore a11y_no_static_element_interactions -->
      <div
        class="absolute bottom-0 right-0 w-3 h-3 cursor-se-resize"
        onmousedown={(e) => startResize(e, 'se')}
      ></div>
    </div>
  </div>
{/if}
