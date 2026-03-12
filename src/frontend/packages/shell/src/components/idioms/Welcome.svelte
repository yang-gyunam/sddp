<!--
  WelcomeEmptyState Component
  VSCode-style welcome screen with keyboard shortcuts
  Displayed when no tabs are open in the editor area
-->
<script lang="ts">
  import { Spinner } from '@sddp/ui';

  interface Props {
    /** Additional CSS classes */
    class?: string;
  }

  let {
    class: className = '',
  }: Props = $props();

  // Keyboard shortcuts to display (only actually registered ones)
  const shortcuts = [
    { keys: ['⌘', 'K'], label: 'Command Palette' },
    { keys: ['⌘', 'B'], label: 'Toggle Sidebar' },
    { keys: ['⌘', 'J'], label: 'Toggle Bottom Panel' },
  ];
</script>

<div class="flex flex-col items-center justify-center h-full p-8 text-center select-none {className}">
  <!-- Logo -->
  <div class="mb-8 opacity-60" style="transform: scale(2.5);">
    <Spinner variant="onbrick" size="xl" />
  </div>

  <!-- Keyboard Shortcuts -->
  <div class="max-w-sm text-left py-2">
    <div class="space-y-2">
      {#each shortcuts as shortcut (shortcut.label)}
        <div class="flex items-center justify-between gap-4 text-sm">
          <span class="text-[var(--color-text-secondary)]">{shortcut.label}</span>
          <div class="flex items-center gap-1">
            {#each shortcut.keys as key (key)}
              <kbd class="px-1.5 py-0.5 text-xs font-mono rounded
                         bg-[var(--color-bg-tertiary)] text-[var(--color-text-secondary)]
                         border border-[var(--color-border)]">
                {key}
              </kbd>
            {/each}
          </div>
        </div>
      {/each}
    </div>
  </div>
</div>
