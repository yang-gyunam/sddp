<!--
  StatCard Component
  Unified statistics card for overview/dashboard sections.
  Supports 5 layout patterns:
    - status       → health card: left colored border + metrics list (Pattern E)
    - iconColor    → horizontal icon-pill layout (Pattern B)
    - icon         → vertical label/value layout (Pattern A) + optional selected highlight
    - no icon, sm  → compact label/value (Pattern F/G)
    - no icon      → simple value/label layout (Pattern C) + optional subtitle
-->
<script lang="ts">
  import { SurfaceCard, formatNumber, formatPercent } from '@sddp/shell';
  import { Icon } from '@sddp/ui';
  import type { HealthStatus, HealthMetric } from './types';

  interface Props {
    title: string;
    value?: number | string;
    subtitle?: string;
    icon?: string;
    iconColor?: string;
    valueColor?: string;
    size?: 'sm';
    selected?: boolean;
    status?: HealthStatus;
    metrics?: HealthMetric[];
    trend?: {
      value: number;
      direction: 'up' | 'down' | 'neutral';
    };
    onClick?: () => void;
    class?: string;
  }

  let {
    title,
    value,
    subtitle,
    icon,
    iconColor,
    valueColor,
    size,
    selected = false,
    status,
    metrics,
    trend,
    onClick,
    class: className = '',
  }: Props = $props();

  const trendColors = {
    up: 'text-green-600 dark:text-green-400',
    down: 'text-red-600 dark:text-red-400',
    neutral: 'text-gray-500 dark:text-gray-400',
  };

  const trendIcons = {
    up: 'trending-up',
    down: 'trending-down',
    neutral: 'minus',
  };

  const statusBorderColors: Record<HealthStatus, string> = {
    healthy: 'var(--color-success-500)',
    warning: 'var(--color-warning-500)',
    error: 'var(--color-danger-500)',
  };

  const statusIconNames: Record<HealthStatus, string> = {
    healthy: 'check-circle',
    warning: 'alert-triangle',
    error: 'x-circle',
  };

  const statusTextColors: Record<HealthStatus, string> = {
    healthy: 'text-[var(--color-success-500)]',
    warning: 'text-[var(--color-warning-500)]',
    error: 'text-[var(--color-danger-500)]',
  };

  const displayValue = $derived(
    value !== undefined
      ? typeof value === 'number' ? formatNumber(value) : value
      : ''
  );
  const trendLabel = $derived(
    trend ? formatPercent(Math.abs(trend.value) / 100, { maximumFractionDigits: 0 }) : ''
  );

  const selectedClass = $derived(
    selected
      ? 'border-[var(--color-accent-primary)]/50 bg-[var(--color-accent-primary)]/10'
      : ''
  );
</script>

{#if status}
  <!-- Pattern E: Health card with colored left border + metrics -->
  <div
    class="stat-card stat-card--health rounded-lg border border-[var(--color-border)] bg-[var(--color-bg-secondary)] p-6 {className}"
    style="border-left: 4px solid {statusBorderColors[status]}"
  >
    <div class="flex items-center justify-between mb-4">
      <span class="font-semibold text-[var(--color-text-primary)]">{title}</span>
      <Icon name={statusIconNames[status]} size="md" class={statusTextColors[status]} />
    </div>
    {#if metrics && metrics.length > 0}
      <div class="flex flex-col gap-2">
        {#each metrics as metric (metric.label)}
          <div class="flex justify-between text-sm">
            <span class="text-[var(--color-text-secondary)]">{metric.label}</span>
            <span class="font-medium text-[var(--color-text-primary)]">{metric.value}</span>
          </div>
        {/each}
      </div>
    {/if}
  </div>
{:else}
  <SurfaceCard
    as="button"
    padding={size === 'sm' ? 'md' : 'lg'}
    interactive={Boolean(onClick)}
    {selected}
    class="stat-card text-left w-full {onClick ? 'cursor-pointer' : 'cursor-default'} {selectedClass} {className}"
    onclick={onClick}
  >
    {#if iconColor && icon}
      <!-- Pattern B: horizontal icon-pill layout -->
      <div class="flex items-center gap-3">
        <div class="p-2 rounded-lg bg-{iconColor}/10">
          <Icon name={icon} size="md" class="text-{iconColor}" />
        </div>
        <div>
          <p class="surface-card__value">{displayValue}</p>
          <p class="surface-card__meta">{title}</p>
        </div>
      </div>
    {:else if icon}
      <!-- Pattern A: vertical label/value layout -->
      <div class="flex items-center justify-between mb-2">
        <span class="surface-card__label">{title}</span>
        <Icon name={icon} size="sm" class="text-[var(--color-text-tertiary)]" />
      </div>
      <div class="flex items-end gap-2">
        <span class="surface-card__value">{displayValue}</span>
        {#if trend}
          <span class="flex items-center gap-0.5 text-xs {trendColors[trend.direction]}">
            <Icon name={trendIcons[trend.direction]} size="xs" />
            {trendLabel}
          </span>
        {/if}
      </div>
      {#if subtitle}
        <span class="surface-card__meta mt-1">{subtitle}</span>
      {/if}
    {:else if size === 'sm'}
      <!-- Pattern F/G: compact label above, value below -->
      <p class="surface-card__meta mb-1">{title}</p>
      <p class="surface-card__value surface-card__value--sm">{displayValue}</p>
    {:else}
      <!-- Pattern C: simple value/label layout -->
      <div class="surface-card__value {valueColor ?? ''}">{displayValue}</div>
      <div class="surface-card__meta">{title}</div>
      {#if subtitle}
        <span class="surface-card__meta mt-1">{subtitle}</span>
      {/if}
    {/if}
  </SurfaceCard>
{/if}
