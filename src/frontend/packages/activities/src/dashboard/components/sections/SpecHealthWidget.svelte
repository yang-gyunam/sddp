<!-- Section: SpecHealthWidget — Dashboard > My -->
<script lang="ts">
  /**
   * SpecHealthWidget
   * Donut chart widget showing spec status distribution (Draft, InReview, Approved, Locked).
   */
  import { setActiveActivityItem } from '@sddp/shell';
  import { DonutChart } from '../idioms';
  import type { SpecHealthRadar } from '../../types';

  interface Props {
    data: SpecHealthRadar | null;
  }

  let { data }: Props = $props();

  const chartData = $derived(
    data
      ? [
          { label: 'Draft', value: data.draft, color: 'var(--color-accent-primary)' },
          { label: 'In Review', value: data.inReview, color: 'var(--color-warning-500)' },
          { label: 'Approved', value: data.approved, color: 'var(--color-success-500)' },
          { label: 'Locked', value: data.locked, color: 'var(--color-info-500)' },
        ]
      : []
  );

  const total = $derived(data?.total ?? 0);

  function handleSegmentClick(_label: string, _value: number) {
    setActiveActivityItem('projects', true);
  }
</script>

<div class="spec-health-widget">
  <div class="widget-header">
    <h2 class="widget-title">Spec Health</h2>
  </div>

  {#if total === 0}
    <div class="empty-state">
      <span class="codicon codicon-pie-chart"></span>
      <span>No specs found</span>
    </div>
  {:else}
    <DonutChart
      data={chartData}
      showLegend={true}
      size={160}
      onSegmentClick={handleSegmentClick}
    />
  {/if}
</div>

<style>
  .spec-health-widget {
    padding: 1rem;
    background: var(--color-bg-secondary);
    border: 1px solid var(--color-border);
    border-radius: 8px;
  }

  .widget-header {
    margin-bottom: 0.75rem;
  }

  .widget-title {
    margin: 0;
    font-size: 0.875rem;
    font-weight: 600;
    color: var(--color-text-primary);
  }

  .empty-state {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    height: 160px;
    gap: 0.5rem;
    color: var(--color-text-tertiary);
    font-size: 0.875rem;
  }

  .empty-state .codicon {
    font-size: 2rem;
    opacity: 0.5;
  }
</style>
