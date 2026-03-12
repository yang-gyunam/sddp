<!--
  CollapsiblePanel Component
  VSCode-style collapsible panel for sidebar sections
-->
<script lang="ts">
  import { slide } from 'svelte/transition';
  import { Button, Icon, IconButton } from '@sddp/ui';
  import type { CollapsiblePanelAction } from '../../types';

  interface Props {
    title: string;
    expanded?: boolean;
    badge?: string | number;
    actions?: CollapsiblePanelAction[];
    skipSlide?: boolean;
    class?: string;
    onToggle?: (expanded: boolean) => void;
    children?: import('svelte').Snippet;
  }

  let {
    title,
    expanded = true,
    badge,
    actions = [],
    skipSlide = false,
    class: className = '',
    onToggle,
    children,
  }: Props = $props();

  // Use prop directly for expanded state - parent manages state via onToggle callback
  function handleToggle() {
    const newExpanded = !expanded;
    onToggle?.(newExpanded);
  }

  function handleKeyDown(event: KeyboardEvent) {
    if (event.key === 'ArrowLeft' && expanded) {
      event.preventDefault();
      handleToggle();
    } else if (event.key === 'ArrowRight' && !expanded) {
      event.preventDefault();
      handleToggle();
    }
  }

  function handleActionClick(event: MouseEvent, action: CollapsiblePanelAction) {
    event.stopPropagation();
    action.onClick();
  }
</script>

<div
  class="collapsible-panel flex flex-col bg-[var(--color-bg-secondary)] border-b border-[var(--color-border)] {className}"
  role="region"
  aria-label="{title} panel"
>
  <!-- Header -->
  <div
    class="panel-header flex-shrink-0 flex items-center justify-between w-full px-2 h-[26px] hover:bg-[var(--color-bg-tertiary)] transition-colors select-none"
  >
    <Button
      variant="unstyled"
      class="flex items-center gap-1 flex-1 min-w-0 text-left"
      onclick={handleToggle}
      onkeydown={handleKeyDown}
      aria-label={title}
      aria-expanded={expanded}
    >
      <!-- Chevron Icon -->
      <div
        class="flex items-center justify-center w-4 h-4 transition-transform duration-200
          {expanded ? 'rotate-90' : ''}"
      >
        <Icon name="chevron-right" size="sm" class="text-[var(--color-text-secondary)]" />
      </div>

      <!-- Title -->
      <span
        class="flex-1 pl-1.5 overflow-hidden text-ellipsis whitespace-nowrap text-xs font-semibold uppercase tracking-[0.08em] text-[var(--color-text-secondary)]"
      >
        {title}
      </span>

      <!-- Badge -->
      {#if badge !== undefined && badge !== null && badge !== ''}
        <span
          class="ml-1 px-1.5 py-0.5 text-[0.625rem] bg-[var(--color-bg-tertiary)] text-[var(--color-text-secondary)] rounded-full min-w-5 text-center flex-shrink-0"
        >
          {badge}
        </span>
      {/if}
    </Button>

    <!-- Actions -->
    {#if actions.length > 0 && expanded}
      <div class="panel-actions flex items-center ml-2 opacity-0 group-hover:opacity-100 transition-opacity">
        {#each actions as action, index (action.id + index)}
          <IconButton
            icon={action.icon}
            size="sm"
            title={action.label}
            onclick={(e) => handleActionClick(e, action)}
          />
        {/each}
      </div>
    {/if}
  </div>

  <!-- Content -->
  {#if expanded}
    <div class="panel-content flex-1 overflow-auto min-h-0 mt-1" transition:slide={{ duration: skipSlide ? 0 : 150 }}>
      {#if children}
        {@render children()}
      {/if}
    </div>
  {/if}
</div>

<style>
  .collapsible-panel {
    position: relative;
  }

  .panel-header:focus {
    outline: 1px solid var(--color-accent-primary);
    outline-offset: -1px;
  }

  .collapsible-panel:hover .panel-actions {
    opacity: 1;
  }

  .collapsible-panel:focus-within .panel-actions {
    opacity: 1;
  }
</style>
