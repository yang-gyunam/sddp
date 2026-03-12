<!--
  ListItem Component
  Standardized list row container: accent background highlight on selection.
-->
<script lang="ts">
  import type { Snippet } from 'svelte';
  import { Button } from '@sddp/ui';

  interface Props {
    selected?: boolean;
    disabled?: boolean;
    onclick?: (event: MouseEvent) => void;
    class?: string;
    density?: 'compact' | 'regular';
    role?: string;
    'aria-selected'?: boolean;
    'aria-expanded'?: boolean;
    style?: string;
    children?: Snippet;
  }

  let {
    selected = false,
    disabled = false,
    onclick,
    class: className = '',
    density = 'regular',
    role: ariaRole,
    'aria-selected': ariaSelected,
    'aria-expanded': ariaExpanded,
    style,
    children,
  }: Props = $props();

  const paddingClass = $derived(density === 'compact' ? 'px-3 py-1.5' : 'px-3 py-2');

  const selectionClass = $derived(
    selected
      ? 'bg-[var(--color-accent-primary)]/10 text-[var(--color-text-primary)]'
      : 'hover:bg-[var(--color-bg-tertiary)]'
  );
</script>

<Button
  variant="unstyled"
  {disabled}
  {onclick}
  role={ariaRole}
  aria-selected={ariaSelected}
  aria-expanded={ariaExpanded}
  {style}
  class="list-item w-full text-left flex items-center gap-2 transition-colors
    border-b border-[var(--color-border-primary)]
    {paddingClass}
    {selectionClass}
    {disabled ? 'opacity-50 cursor-not-allowed' : ''}
    {className}"
>
  {#if children}
    {@render children()}
  {/if}
</Button>

<style>
  :global(.list-item:focus-visible) {
    outline: 1px solid var(--color-accent-primary);
    outline-offset: -1px;
  }
</style>
