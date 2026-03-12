<script lang="ts">
  /**
   * SettingGroupHeader - VS Code-style section header
   * Flat header with bottom border, no card background
   */

  interface Props {
    id?: string;
    title: string;
  }

  let { id, title }: Props = $props();
</script>

<div {id} class="setting-group-header">
  <h2 class="setting-group-title">{title}</h2>
</div>

<style>
  .setting-group-header {
    padding-top: 1rem;
    padding-bottom: 0.375rem;
    margin-bottom: 0.25rem;
  }

  .setting-group-title {
    margin: 0;
    font-size: 1.125rem;
    font-weight: 600;
    color: var(--color-text-primary, #111);
  }
</style>
