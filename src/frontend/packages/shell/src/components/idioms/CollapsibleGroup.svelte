<!--
  CollapsibleGroup Component
  Reusable collapsible group for sidebar lists inside tab content (SidebarDetailLayout).
  Distinct from CollapsiblePanel (which is for the app-level sidebar).
  Uses CSS grid animation for smooth expand/collapse that works with flex-fill layouts.

  Usage:
    <CollapsibleGroup title="Entity" icon="box" iconClass="text-blue-500" badge={3} expanded onToggle={...}>
      {#each items as item}
        <ItemComponent {item} />
      {/each}
    </CollapsibleGroup>
-->
<script lang="ts">
  import { onMount } from 'svelte';
  import { Button, Icon, IconButton } from '@sddp/ui';
  import type { CollapsiblePanelAction } from '../../types';

  interface Props {
    /** Primary title text */
    title: string;
    /** Optional secondary text (e.g., project code) shown before title in mono font */
    subtitle?: string;
    /** Whether the group is expanded */
    expanded?: boolean;
    /** Leading icon name */
    icon?: string;
    /** CSS class for the leading icon color */
    iconClass?: string;
    /** Badge content (count) */
    badge?: number | string;
    /** CSS classes for badge styling override */
    badgeClass?: string;
    /** Visual variant: 'default' for sidebar panels, 'plain' for forms (no bg/border) */
    variant?: 'default' | 'plain';
    /** Action buttons shown on hover (same pattern as CollapsiblePanel) */
    actions?: CollapsiblePanelAction[];
    /** Toggle callback */
    onToggle?: () => void;
    /** Allow content overflow when expanded (for dropdowns/popovers inside) */
    allowOverflow?: boolean;
    /** Group content */
    children?: import('svelte').Snippet;
    /** Additional CSS classes */
    class?: string;
  }

  let {
    title,
    subtitle,
    expanded = true,
    icon,
    iconClass = 'text-[var(--color-text-secondary)]',
    badge,
    badgeClass = '',
    variant = 'default',
    actions = [],
    allowOverflow = false,
    onToggle,
    children,
    class: className = '',
  }: Props = $props();

  function handleActionClick(e: MouseEvent, action: CollapsiblePanelAction) {
    e.stopPropagation();
    action.onClick();
  }

  function handleHeaderClick() {
    onToggle?.();
  }

  // Skip transition on initial mount to prevent flash
  let mounted = $state(false);
  onMount(() => {
    requestAnimationFrame(() => { mounted = true; });
  });

  function handleHeaderKeyDown(e: KeyboardEvent) {
    if (e.key === 'ArrowLeft' && expanded) {
      e.preventDefault();
      onToggle?.();
    } else if (e.key === 'ArrowRight' && !expanded) {
      e.preventDefault();
      onToggle?.();
    }
  }
</script>

<div class="collapsible-group {className}">
  <!-- Header -->
  <Button
    variant="unstyled"
    class="group-header flex items-center gap-1 h-[26px] cursor-pointer transition-colors w-full
           {variant === 'plain'
             ? 'px-0 hover:bg-[var(--color-bg-tertiary)]/50'
             : 'px-2 bg-[var(--color-bg-secondary)] hover:bg-[var(--color-bg-tertiary)] border-b border-[var(--color-border-primary)]'}"
    aria-expanded={expanded}
    aria-label={title}
    onclick={handleHeaderClick}
    onkeydown={handleHeaderKeyDown}
  >
    <!-- Chevron -->
    <div
      class="flex items-center justify-center w-4 h-4 transition-transform duration-200
        {expanded ? 'rotate-90' : ''}"
    >
      <Icon
        name="chevron-right"
        size="xs"
        class="text-[var(--color-text-secondary)] opacity-70"
      />
    </div>

    <!-- Leading Icon -->
    {#if icon}
      <Icon name={icon} size="xs" class={iconClass} />
    {/if}

    <!-- Title Area -->
    <div class="flex-1 min-w-0 flex items-center gap-1">
      {#if subtitle}
        <span class="text-[0.625rem] font-mono text-[var(--color-text-tertiary)] flex-shrink-0">
          {subtitle}
        </span>
      {/if}
      <span class="truncate text-xs font-semibold uppercase tracking-[0.08em] text-[var(--color-text-secondary)]">
        {title}
      </span>
    </div>

    <!-- Badge -->
    {#if badge !== undefined && badge !== null && badge !== ''}
      <span
        class="text-[0.625rem] px-1.5 py-0.5 rounded-full min-w-5 text-center flex-shrink-0
               {badgeClass || 'text-[var(--color-text-tertiary)] bg-[var(--color-bg-tertiary)]'}"
      >
        {badge}
      </span>
    {/if}

    <!-- Actions (hover to show, same pattern as CollapsiblePanel) -->
    {#if actions.length > 0 && expanded}
      <div class="group-actions flex items-center ml-2 opacity-0 transition-opacity">
        {#each actions as action (action.id)}
          <IconButton
            icon={action.icon}
            size="sm"
            title={action.label}
            onclick={(e) => handleActionClick(e, action)}
          />
        {/each}
      </div>
    {/if}
  </Button>

  <!-- Content: CSS grid for height + opacity for smoothness -->
  <div class="group-content-wrapper" class:expanded class:animated={mounted} class:allow-overflow={allowOverflow}>
    <div class="group-content-inner">
      {#if children}
        {@render children()}
      {/if}
    </div>
  </div>
</div>

<style>
  .group-header:focus {
    outline: 1px solid var(--color-accent-primary);
    outline-offset: -1px;
  }

  .collapsible-group:hover .group-actions {
    opacity: 1;
  }

  .collapsible-group:focus-within .group-actions {
    opacity: 1;
  }

  .group-content-wrapper {
    display: grid;
    grid-template-rows: 0fr;
    opacity: 0;
  }

  .group-content-wrapper.animated {
    transition: grid-template-rows 300ms ease, opacity 200ms ease;
  }

  .group-content-wrapper.expanded {
    grid-template-rows: 1fr;
    opacity: 1;
  }

  .group-content-inner {
    min-height: 0;
    overflow: hidden;
  }

  .group-content-wrapper.expanded.allow-overflow .group-content-inner {
    overflow: visible;
  }
</style>
