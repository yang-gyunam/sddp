<!--
  SingleColumnLayout Component
  Single-column content layout with full-width scrolling and centered max-width content

  Layout:
  ┌─────────────────────────────────────────────────────┐
  │  Scroll Container (full width, scrollbar at edge)   │
  │  ┌─────────────────────────────────────┐            │
  │  │  Content Area (max-width, centered) │            │
  │  │                                     │            │
  │  │                                     │            │
  │  └─────────────────────────────────────┘            │
  └─────────────────────────────────────────────────────┘
-->
<script lang="ts">
  import type { Snippet } from 'svelte';

  interface Props {
    /** Maximum width of the content area */
    maxWidth?: string;
    /** Additional CSS classes for the scroll container */
    class?: string;
    children: Snippet;
  }

  let { maxWidth = '1000px', class: className = '', children }: Props = $props();
</script>

<div class="single-column-scroll {className}">
  <div class="single-column-content" style:max-width={maxWidth}>
    {@render children()}
  </div>
</div>

<style>
  .single-column-scroll {
    flex: 1;
    min-height: 0;
    overflow-y: auto;
  }

  .single-column-content {
    padding: 1.5rem 2rem 2rem;
  }
</style>
