<!-- Section: DueDateWidget — Dashboard > My -->
<script lang="ts">
  /**
   * DueDateWidget
   * Timeline chart of task due dates with overdue and upcoming count badges.
   */
  import { setActiveActivityItem } from '@sddp/shell';
  import { TimelineChart } from '../idioms';
  import type { DueDateTimeline } from '../../types';

  interface Props {
    data: DueDateTimeline | null;
  }

  let { data }: Props = $props();

  const overdueCount = $derived(data?.overdueCount ?? 0);
  const upcomingCount = $derived(data?.upcomingCount ?? 0);
  const tasks = $derived(data?.tasks ?? []);

  function handleTaskClick(_taskId: string) {
    setActiveActivityItem('tasks', true);
  }
</script>

<div class="due-date-widget">
  <div class="widget-header">
    <h2 class="widget-title">Due Date Timeline</h2>
    <div class="badge-row">
      {#if overdueCount > 0}
        <span class="count-badge overdue-badge">
          {overdueCount} overdue
        </span>
      {/if}
      {#if upcomingCount > 0}
        <span class="count-badge upcoming-badge">
          {upcomingCount} upcoming
        </span>
      {/if}
    </div>
  </div>

  <TimelineChart {tasks} onTaskClick={handleTaskClick} />
</div>

<style>
  .due-date-widget {
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

  .badge-row {
    display: flex;
    align-items: center;
    gap: 0.375rem;
  }

  .count-badge {
    display: inline-flex;
    align-items: center;
    padding: 2px 8px;
    border-radius: 4px;
    font-size: 0.6875rem;
    font-weight: 600;
    white-space: nowrap;
  }

  .overdue-badge {
    background: var(--color-error-12);
    color: var(--color-error-500);
  }

  .upcoming-badge {
    background: var(--color-info-12);
    color: var(--color-info-500);
  }
</style>
