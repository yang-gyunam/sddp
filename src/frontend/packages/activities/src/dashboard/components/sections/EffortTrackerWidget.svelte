<!-- Section: EffortTrackerWidget — Dashboard > My -->
<script lang="ts">
  /**
   * EffortTrackerWidget
   * Weekly effort summary with daily bar chart, week progress bar, and task distribution.
   */
  import { BarChart, ProgressBar } from '../idioms';
  import type { MyEffortTracker, DailyEffortSummary } from '../../types';

  interface Props {
    data: MyEffortTracker | null;
  }

  let { data }: Props = $props();

  const hasData = $derived(
    data !== null && (data.totalHoursThisWeek > 0 || data.dailyBreakdown.length > 0)
  );

  // Map DailyEffortSummary to BarChart's expected DataItem shape
  const barData = $derived(
    (data?.dailyBreakdown ?? []).map((d: DailyEffortSummary) => ({
      label: d.dayLabel,
      value: d.hours,
    }))
  );

  // Max hours for task distribution bars
  const maxTaskHours = $derived(() => {
    const dist = data?.taskDistribution ?? [];
    if (dist.length === 0) return 1;
    return Math.max(...dist.map((t) => t.hours), 1);
  });
</script>

<div class="effort-tracker-widget">
  <div class="widget-header">
    <h2 class="widget-title">Effort Tracker</h2>
    {#if data}
      <span class="total-hours">
        {data.totalHoursThisWeek.toFixed(1)}h / {data.targetHoursPerWeek}h this week
      </span>
    {/if}
  </div>

  {#if !hasData}
    <div class="empty-state">
      <span class="codicon codicon-clock"></span>
      <span>No time logs this week</span>
    </div>
  {:else}
    <!-- Week progress bar -->
    <div class="section">
      <ProgressBar
        value={data?.totalHoursThisWeek ?? 0}
        max={data?.targetHoursPerWeek ?? 40}
        label="Week Progress"
        showPercentage={true}
      />
    </div>

    <!-- Daily breakdown bar chart -->
    {#if barData.length > 0}
      <div class="section">
        <BarChart data={barData} title="Daily Breakdown" />
      </div>
    {/if}

    <!-- Task distribution list -->
    {#if (data?.taskDistribution ?? []).length > 0}
      <div class="section">
        <h3 class="section-title">Task Distribution</h3>
        <div class="task-list">
          {#each data?.taskDistribution ?? [] as task (task.taskId)}
            <div class="task-row">
              <span class="task-title">{task.taskTitle}</span>
              <div class="task-bar-track">
                <div
                  class="task-bar-fill"
                  style="width: {(task.hours / maxTaskHours()) * 100}%"
                ></div>
              </div>
              <span class="task-hours">{task.hours.toFixed(1)}h</span>
            </div>
          {/each}
        </div>
      </div>
    {/if}
  {/if}
</div>

<style>
  .effort-tracker-widget {
    padding: 1rem;
    background: var(--color-bg-secondary);
    border: 1px solid var(--color-border);
    border-radius: 8px;
  }

  .widget-header {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    margin-bottom: 0.875rem;
    flex-wrap: wrap;
  }

  .widget-title {
    margin: 0;
    font-size: 0.875rem;
    font-weight: 600;
    color: var(--color-text-primary);
    flex: 1;
  }

  .total-hours {
    font-size: 0.75rem;
    color: var(--color-text-tertiary);
    white-space: nowrap;
  }

  .section {
    margin-bottom: 0.875rem;
  }

  .section:last-child {
    margin-bottom: 0;
  }

  .section-title {
    margin: 0 0 0.5rem;
    font-size: 0.75rem;
    font-weight: 600;
    color: var(--color-text-tertiary);
    text-transform: uppercase;
    letter-spacing: 0.04em;
  }

  .task-list {
    display: flex;
    flex-direction: column;
    gap: 0.375rem;
  }

  .task-row {
    display: flex;
    align-items: center;
    gap: 0.5rem;
  }

  .task-title {
    min-width: 80px;
    max-width: 100px;
    font-size: 0.75rem;
    color: var(--color-text-secondary);
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
    flex-shrink: 0;
  }

  .task-bar-track {
    flex: 1;
    height: 8px;
    background: var(--color-bg-tertiary);
    border-radius: 4px;
    overflow: hidden;
  }

  .task-bar-fill {
    height: 100%;
    background: var(--color-accent-primary);
    border-radius: 4px;
    transition: width 0.3s ease;
    min-width: 4px;
  }

  .task-hours {
    font-size: 0.6875rem;
    font-weight: 600;
    color: var(--color-text-primary);
    white-space: nowrap;
    min-width: 2.5rem;
    text-align: right;
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
