<script lang="ts">
  /**
   * SettingItem - VS Code-style individual setting item
   * Flat layout: category:title, description, control
   */

  import type { Snippet } from 'svelte';

  interface Props {
    category: string;
    title: string;
    description?: string;
    note?: string;
    children: Snippet;
  }

  let { category, title, description = '', note, children }: Props = $props();
</script>

<div class="setting-item">
  <div class="setting-item-header">
    <span class="setting-item-category">{category}:</span>
    <span class="setting-item-title">{title}</span>
  </div>
  {#if description}
    <div class="setting-item-description">{description}</div>
  {/if}
  {#if note}
    <div class="setting-item-note">
      <span class="note-label">Note:</span> {note}
    </div>
  {/if}
  <div class="setting-item-control">
    {@render children()}
  </div>
</div>

<style>
  .setting-item {
    padding: 0.75rem 0;
  }

  .setting-item-header {
    margin-bottom: 0.25rem;
    font-size: 0.875rem;
    line-height: 1.4;
  }

  .setting-item-category {
    color: var(--color-text-secondary, #6b7280);
  }

  .setting-item-title {
    font-weight: 600;
    color: var(--color-text-primary, #111);
  }

  .setting-item-description {
    margin-bottom: 0.5rem;
    font-size: 0.8125rem;
    line-height: 1.5;
    color: var(--color-text-secondary, #6b7280);
  }

  .setting-item-note {
    margin-bottom: 0.5rem;
    padding: 0.375rem 0.5rem;
    font-size: 0.8125rem;
    line-height: 1.5;
    color: var(--color-text-secondary, #6b7280);
    background: var(--color-bg-tertiary, #f3f4f6);
    border-radius: 4px;
  }

  .note-label {
    font-weight: 600;
    color: var(--color-text-primary, #111);
  }

  .setting-item-control {
    max-width: 400px;
  }
</style>
