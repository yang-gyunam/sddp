<!-- Section: ArtifactMetaPanel — Artifacts > Global -->
<script lang="ts">
  import { Icon } from '@sddp/ui';
  import { formatNumber, formatPercent, truncate } from '@sddp/shell';
  import type { ArtifactDetail, ArtifactFilterType } from '../../types';
  import { ARTIFACT_TYPE_STYLES, ARTIFACT_STATUS_STYLES } from '../../types';

  interface Props {
    artifact: ArtifactDetail | null;
    statusCounts?: Record<ArtifactFilterType, number>;
    class?: string;
  }

  let {
    artifact,
    statusCounts = { all: 0, valid: 0, modified: 0, missing: 0 },
    class: className = '',
  }: Props = $props();

  const typeStyle = $derived(artifact ? ARTIFACT_TYPE_STYLES[artifact.artifactType] : null);
  const statusStyle = $derived(artifact ? (ARTIFACT_STATUS_STYLES[artifact.status] ?? ARTIFACT_STATUS_STYLES.Unverified) : null);

  // Calculate health percentage
  const healthPercentage = $derived(
    statusCounts.all > 0
      ? Math.round((statusCounts.valid / statusCounts.all) * 100)
      : 100
  );

  const healthColor = $derived(
    healthPercentage >= 90 ? 'text-[var(--color-success-500)]' :
    healthPercentage >= 70 ? 'text-[var(--color-warning-500)]' : 'text-[var(--color-error-500)]'
  );

  const healthBarColor = $derived(
    healthPercentage >= 90 ? 'bg-[var(--color-success-500)]' :
    healthPercentage >= 70 ? 'bg-[var(--color-warning-500)]' : 'bg-[var(--color-error-500)]'
  );
  const healthPercentageLabel = $derived(
    formatPercent(healthPercentage / 100, { maximumFractionDigits: 0 })
  );
</script>

<div class="flex flex-col h-full bg-[var(--color-bg-secondary)] border-l border-[var(--color-border-primary)] {className}">
  <!-- Overall Stats -->
  <div class="p-3 border-b border-[var(--color-border-primary)]">
    <h3 class="text-xs font-semibold text-[var(--color-text-tertiary)] uppercase tracking-wider mb-2">
      Artifact Health
    </h3>
    <div class="flex items-center gap-3 p-3 bg-[var(--color-bg-primary)] rounded-lg">
      <div class="text-2xl font-bold {healthColor}">
        {healthPercentageLabel}
      </div>
      <div class="flex-1">
        <div class="h-2 bg-[var(--color-bg-tertiary)] rounded-full overflow-hidden">
          <div
            class="h-full transition-all duration-300 {healthBarColor}"
            style="width: {healthPercentage}%"
          ></div>
        </div>
        <div class="flex justify-between text-xs text-[var(--color-text-tertiary)] mt-1">
          <span>{formatNumber(statusCounts.valid)} valid</span>
          <span>{formatNumber(statusCounts.all)} total</span>
        </div>
      </div>
    </div>
  </div>

  <!-- Status Breakdown -->
  <div class="p-3 border-b border-[var(--color-border-primary)]">
    <h3 class="text-xs font-semibold text-[var(--color-text-tertiary)] uppercase tracking-wider mb-2">
      Status Breakdown
    </h3>
    <div class="space-y-2">
      <div class="flex items-center justify-between p-2 bg-[var(--color-bg-primary)] rounded-lg">
        <div class="flex items-center gap-2 text-[var(--color-success-600)] dark:text-[var(--color-success-400)]">
          <Icon name="check-circle" size="sm" />
          <span class="text-sm">Valid</span>
        </div>
        <span class="font-medium text-[var(--color-text-primary)]">{formatNumber(statusCounts.valid)}</span>
      </div>
      <div class="flex items-center justify-between p-2 bg-[var(--color-bg-primary)] rounded-lg">
        <div class="flex items-center gap-2 text-[var(--color-warning-600)] dark:text-[var(--color-warning-400)]">
          <Icon name="alert-triangle" size="sm" />
          <span class="text-sm">Modified</span>
        </div>
        <span class="font-medium text-[var(--color-text-primary)]">{formatNumber(statusCounts.modified)}</span>
      </div>
      <div class="flex items-center justify-between p-2 bg-[var(--color-bg-primary)] rounded-lg">
        <div class="flex items-center gap-2 text-[var(--color-error-600)] dark:text-[var(--color-error-400)]">
          <Icon name="x-circle" size="sm" />
          <span class="text-sm">Missing</span>
        </div>
        <span class="font-medium text-[var(--color-text-primary)]">{formatNumber(statusCounts.missing)}</span>
      </div>
    </div>
  </div>

  <!-- Selected Artifact Info -->
  {#if artifact}
    <div class="flex-1 overflow-y-auto p-3">
      <h3 class="text-xs font-semibold text-[var(--color-text-tertiary)] uppercase tracking-wider mb-2">
        Selected Artifact
      </h3>

      <div class="space-y-3">
        <!-- Type -->
        <div class="p-2 bg-[var(--color-bg-primary)] rounded-lg">
          <div class="text-xs text-[var(--color-text-tertiary)] mb-1">Type</div>
          <div class="flex items-center gap-2 {typeStyle?.color}">
            <Icon name={typeStyle?.icon || 'file'} size="sm" />
            <span class="text-sm font-medium">{typeStyle?.label}</span>
          </div>
        </div>

        <!-- Status -->
        <div class="p-2 bg-[var(--color-bg-primary)] rounded-lg">
          <div class="text-xs text-[var(--color-text-tertiary)] mb-1">Status</div>
          <div class="flex items-center gap-2 {statusStyle?.color}">
            <Icon name={statusStyle?.icon || 'circle'} size="sm" />
            <span class="text-sm font-medium">{statusStyle?.label}</span>
          </div>
        </div>

        <!-- Entity -->
        <div class="p-2 bg-[var(--color-bg-primary)] rounded-lg">
          <div class="text-xs text-[var(--color-text-tertiary)] mb-1">Entity</div>
          <div class="text-sm font-mono text-[var(--color-text-secondary)]">
            {artifact.entityName}
          </div>
        </div>

        <!-- Hash (truncated) -->
        <div class="p-2 bg-[var(--color-bg-primary)] rounded-lg">
          <div class="text-xs text-[var(--color-text-tertiary)] mb-1">Content Hash</div>
          <div class="text-xs font-mono text-[var(--color-text-tertiary)] truncate" title={artifact.contentHash}>
            {truncate(artifact.contentHash, 24)}
          </div>
        </div>
      </div>
    </div>
  {:else}
    <div class="flex-1 flex items-center justify-center p-3">
      <p class="text-sm text-[var(--color-text-tertiary)]">No artifact selected</p>
    </div>
  {/if}
</div>
