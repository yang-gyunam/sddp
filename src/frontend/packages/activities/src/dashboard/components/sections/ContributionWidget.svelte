<!-- Section: ContributionWidget — Dashboard > My -->
<script lang="ts">
  /**
   * ContributionWidget
   * GitHub-style contribution heatmap showing user activity over the last 28 days.
   */
  import { HeatmapGrid } from '../idioms';
  import type { ContributionHeatmap } from '../../types';

  interface Props {
    data: ContributionHeatmap | null;
  }

  let { data }: Props = $props();

  const totalContributions = $derived(data?.totalContributions ?? 0);
  const days = $derived(data?.days ?? []);
</script>

<div class="contribution-widget">
  <div class="widget-header">
    <h2 class="widget-title">Contributions</h2>
    {#if totalContributions > 0}
      <span class="contribution-count">
        {totalContributions} total
      </span>
    {/if}
  </div>

  <HeatmapGrid data={days} />
</div>

<style>
  .contribution-widget {
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
  }

  .widget-title {
    margin: 0;
    font-size: 0.875rem;
    font-weight: 600;
    color: var(--color-text-primary);
    flex: 1;
  }

  .contribution-count {
    font-size: 0.75rem;
    color: var(--color-text-tertiary);
    font-weight: 500;
  }
</style>
