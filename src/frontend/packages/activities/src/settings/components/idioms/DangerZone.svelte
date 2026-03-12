<script lang="ts">
  /**
   * Danger Zone
   * Section for destructive actions
   */

  import type { Snippet } from 'svelte';

  interface Props {
    title?: string;
    children: Snippet;
  }

  let { title = 'Danger Zone', children }: Props = $props();
</script>

<div class="danger-zone">
  <div class="danger-header">
    <span class="danger-icon">⚠️</span>
    <h3 class="danger-title">{title}</h3>
  </div>
  <div class="danger-content">
    {@render children()}
  </div>
</div>

<style>
  .danger-zone {
    padding: 1.5rem;
    background: var(--bg-secondary);
    border: 1px solid var(--error-color, #ef4444);
    border-radius: 8px;
  }

  .danger-header {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    margin-bottom: 1rem;
  }

  .danger-icon {
    font-size: 1.25rem;
  }

  .danger-title {
    margin: 0;
    font-size: 1rem;
    font-weight: 600;
    color: var(--error-color, #ef4444);
  }

  .danger-content {
    display: flex;
    flex-direction: column;
    gap: 1rem;
  }

  .danger-content :global(.danger-item) {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 1rem;
    background: var(--bg-primary);
    border-radius: 4px;
  }

  .danger-content :global(.danger-item-info) {
    flex: 1;
  }

  .danger-content :global(.danger-item-title) {
    margin: 0;
    font-size: 0.875rem;
    font-weight: 500;
    color: var(--text-primary);
  }

  .danger-content :global(.danger-item-description) {
    margin: 0.25rem 0 0;
    font-size: 0.8125rem;
    color: var(--text-secondary);
  }

  .danger-content :global(.btn-danger) {
    padding: 0.5rem 1rem;
    background: var(--error-color, #ef4444);
    border: none;
    border-radius: 4px;
    color: white;
    font-size: 0.875rem;
    font-weight: 500;
    cursor: pointer;
  }

  .danger-content :global(.btn-danger:hover) {
    opacity: 0.9;
  }
</style>
