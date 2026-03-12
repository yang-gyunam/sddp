<!-- Section: ProjectSpotlightWidget — Dashboard > My -->
<script lang="ts">
  /**
   * ProjectSpotlightWidget
   * Highlights the most active project this week with key metrics and recent changes.
   */
  import { formatRelativeTime } from '@sddp/shell';
  import type { ProjectSpotlight } from '../../types';

  interface Props {
    data: ProjectSpotlight | null;
  }

  let { data }: Props = $props();

  const hasData = $derived(data !== null && data.projectId !== null);

  const recentChanges = $derived((data?.recentChanges ?? []).slice(0, 5));

  const stats = $derived([
    { label: 'Changes', value: data?.changeCount ?? 0, icon: 'git-commit' },
    { label: 'Team Activity', value: data?.teamActivityCount ?? 0, icon: 'activity' },
    { label: 'Members', value: data?.teamMemberCount ?? 0, icon: 'user' },
  ]);
</script>

<div class="project-spotlight-widget">
  <div class="widget-header">
    <h2 class="widget-title">Project Spotlight</h2>
    {#if hasData && data?.projectName}
      <span class="project-name-badge">{data.projectName}</span>
    {/if}
  </div>

  {#if !hasData}
    <div class="empty-state">
      <span class="codicon codicon-project"></span>
      <span>No project activity this week</span>
    </div>
  {:else}
    <!-- Mini stats row -->
    <div class="stats-row">
      {#each stats as stat (stat.label)}
        <div class="stat-item">
          <span class="stat-value">{stat.value}</span>
          <span class="stat-label">{stat.label}</span>
        </div>
      {/each}
    </div>

    <!-- Recent changes list -->
    {#if recentChanges.length > 0}
      <div class="changes-section">
        <h3 class="changes-title">Recent Changes</h3>
        <div class="changes-list">
          {#each recentChanges as change, i (i)}
            <div class="change-item">
              <span class="change-resource">{change.resourceType}</span>
              <span class="change-action">{change.action}</span>
              <span class="change-time">{formatRelativeTime(change.timestamp, undefined, { locale: 'en' })}</span>
            </div>
          {/each}
        </div>
      </div>
    {/if}
  {/if}
</div>

<style>
  .project-spotlight-widget {
    padding: 1rem;
    background: var(--color-bg-secondary);
    border: 1px solid var(--color-border);
    border-radius: 8px;
  }

  .widget-header {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    margin-bottom: 0.75rem;
    flex-wrap: wrap;
  }

  .widget-title {
    margin: 0;
    font-size: 0.875rem;
    font-weight: 600;
    color: var(--color-text-primary);
    flex: 1;
  }

  .project-name-badge {
    font-size: 0.6875rem;
    font-weight: 600;
    color: var(--color-accent-primary);
    background: color-mix(in srgb, var(--color-accent-primary) 10%, transparent);
    padding: 2px 8px;
    border-radius: 4px;
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
    max-width: 140px;
  }

  .stats-row {
    display: grid;
    grid-template-columns: repeat(3, 1fr);
    gap: 0.5rem;
    margin-bottom: 0.875rem;
  }

  .stat-item {
    display: flex;
    flex-direction: column;
    align-items: center;
    padding: 0.5rem 0.25rem;
    background: var(--color-bg-primary);
    border: 1px solid var(--color-border);
    border-radius: 6px;
    text-align: center;
  }

  .stat-value {
    font-size: 1.125rem;
    font-weight: 700;
    color: var(--color-text-primary);
    line-height: 1.2;
  }

  .stat-label {
    font-size: 0.6875rem;
    color: var(--color-text-tertiary);
    margin-top: 2px;
    white-space: nowrap;
  }

  .changes-section {
    display: flex;
    flex-direction: column;
    gap: 0.375rem;
  }

  .changes-title {
    margin: 0 0 0.375rem;
    font-size: 0.75rem;
    font-weight: 600;
    color: var(--color-text-tertiary);
    text-transform: uppercase;
    letter-spacing: 0.04em;
  }

  .changes-list {
    display: flex;
    flex-direction: column;
    gap: 0.25rem;
  }

  .change-item {
    display: flex;
    align-items: center;
    gap: 0.375rem;
    padding: 0.3125rem 0.5rem;
    background: var(--color-bg-primary);
    border-radius: 4px;
    font-size: 0.75rem;
    overflow: hidden;
  }

  .change-resource {
    color: var(--color-accent-primary);
    font-weight: 600;
    white-space: nowrap;
    flex-shrink: 0;
  }

  .change-action {
    color: var(--color-text-secondary);
    flex: 1;
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
  }

  .change-time {
    color: var(--color-text-tertiary);
    white-space: nowrap;
    flex-shrink: 0;
  }

  .empty-state {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    height: 120px;
    gap: 0.5rem;
    color: var(--color-text-tertiary);
    font-size: 0.875rem;
  }

  .empty-state .codicon {
    font-size: 1.75rem;
    opacity: 0.5;
  }
</style>
