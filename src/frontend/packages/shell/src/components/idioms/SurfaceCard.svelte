<!--
  SurfaceCard Component
  Standard gray card surface for consistent UI blocks

  Usage:
    <SurfaceCard>Content</SurfaceCard>
    <SurfaceCard as="button" interactive onclick={handler}>Clickable</SurfaceCard>
    <SurfaceCard selected>Selected card</SurfaceCard>
-->
<script lang="ts">
  import type { Snippet } from 'svelte';

  type ElementTag = keyof import('svelte/elements').SvelteHTMLElements;

  interface Props {
    as?: ElementTag;
    type?: 'button' | 'submit' | 'reset';
    padding?: 'none' | 'sm' | 'md' | 'lg';
    interactive?: boolean;
    selected?: boolean;
    onclick?: (event: MouseEvent) => void;
    class?: string;
    children?: Snippet;
  }

  let {
    as = 'div',
    type = 'button',
    padding = 'md',
    interactive = false,
    selected = false,
    onclick,
    class: className = '',
    children,
  }: Props = $props();

  const paddingClass = $derived({
    none: 'surface-card--pad-none',
    sm: 'surface-card--pad-sm',
    md: 'surface-card--pad-md',
    lg: 'surface-card--pad-lg',
  }[padding]);

  const needsButtonRole = $derived(Boolean(onclick) && as !== 'button');

  const classes = $derived([
    'surface-card',
    paddingClass,
    interactive && 'surface-card--interactive',
    selected && 'surface-card--selected',
    className,
  ].filter(Boolean).join(' '));

  function handleKeydown(event: KeyboardEvent) {
    if (!needsButtonRole) return;
    if (event.key === 'Enter' || event.key === ' ') {
      event.preventDefault();
      onclick?.(event as unknown as MouseEvent);
    }
  }
</script>

<svelte:element
  this={as}
  {...(as === 'button' ? { type } : {})}
  class={classes}
  {onclick}
  role={needsButtonRole ? 'button' : undefined}
  tabindex={needsButtonRole ? 0 : undefined}
  onkeydown={needsButtonRole ? handleKeydown : undefined}
>
  {#if children}
    {@render children()}
  {/if}
</svelte:element>

<style>
  .surface-card {
    background-color: var(--color-bg-card);
    border: 1px solid var(--color-border-secondary);
    border-radius: var(--radius-lg, 8px);
    color: var(--color-text-primary);
  }

  .surface-card--pad-none {
    padding: 0;
  }

  .surface-card--pad-sm {
    padding: 0.5rem;
  }

  .surface-card--pad-md {
    padding: 0.75rem;
  }

  .surface-card--pad-lg {
    padding: 1rem;
  }

  .surface-card--interactive {
    transition: background-color 0.15s ease, border-color 0.15s ease;
  }

  .surface-card--interactive:hover {
    background-color: var(--color-bg-card-hover);
  }

  .surface-card--selected {
    border-color: var(--color-accent-primary, var(--color-primary-500));
    background-color: color-mix(in srgb, var(--color-accent-primary, var(--color-primary-500)) 10%, transparent);
  }

  .surface-card--selected.surface-card--interactive:hover {
    background-color: color-mix(in srgb, var(--color-accent-primary, var(--color-primary-500)) 15%, transparent);
  }

  :global(.surface-card__label) {
    font-size: var(--text-xs);
    font-weight: 600;
    letter-spacing: 0.08em;
    text-transform: uppercase;
    color: var(--color-text-tertiary);
  }

  :global(.surface-card__title) {
    font-size: var(--text-sm);
    font-weight: 600;
    color: var(--color-text-secondary);
  }

  :global(.surface-card__value) {
    font-size: var(--text-2xl);
    font-weight: 600;
    color: var(--color-text-primary);
  }

  :global(.surface-card__value--sm) {
    font-size: var(--text-lg);
  }

  :global(.surface-card__meta) {
    font-size: var(--text-xs);
    color: var(--color-text-tertiary);
  }
</style>
