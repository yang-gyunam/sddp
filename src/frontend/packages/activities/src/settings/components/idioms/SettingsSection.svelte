<script lang="ts">
  /**
   * Settings Section
   * Reusable section container for settings pages
   */

  import type { Snippet } from 'svelte';

  interface Props {
    title: string;
    description?: string;
    children: Snippet;
    actions?: Snippet;
  }

  let { title, description, children, actions }: Props = $props();
</script>

<div class="settings-section">
  <div class="section-header">
    <div class="section-info">
      <h2 class="section-title">{title}</h2>
      {#if description}
        <p class="section-description">{description}</p>
      {/if}
    </div>
    {#if actions}
      <div class="section-actions">
        {@render actions()}
      </div>
    {/if}
  </div>
  <div class="section-content">
    {@render children()}
  </div>
</div>

<style>
  .settings-section {
    padding: 1.5rem;
    background: var(--bg-secondary);
    border: 1px solid var(--border-color);
    border-radius: 8px;
  }

  .section-header {
    display: flex;
    justify-content: space-between;
    align-items: flex-start;
    margin-bottom: 1rem;
  }

  .section-info {
    flex: 1;
  }

  .section-title {
    margin: 0;
    font-size: 1.125rem;
    font-weight: 600;
    color: var(--text-primary);
  }

  .section-description {
    margin: 0.25rem 0 0;
    font-size: 0.875rem;
    color: var(--text-secondary);
  }

  .section-actions {
    flex-shrink: 0;
    margin-left: 1rem;
  }

  .section-content {
    display: flex;
    flex-direction: column;
    gap: 1rem;
  }
</style>
