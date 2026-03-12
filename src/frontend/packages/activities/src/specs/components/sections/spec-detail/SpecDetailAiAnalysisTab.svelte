<!-- Section: SpecDetailAiAnalysisTab — Specs > SpecDetailView -->
<script lang="ts">
  import { Badge, Button, Icon, Spinner } from '@sddp/ui';
  import type { BadgeVariant } from '@sddp/ui';
  import type {
    AiAnalysisType,
    AiReport,
    AiReportStatus,
    ImpactAnalysisResult,
    QualityAnalysisResult,
  } from '../../../../ai';

  type ParsedResult<T> = {
    data?: T;
    error?: string;
    raw?: string;
  };

  type QualityScoreEntry = {
    key: string;
    label: string;
    score?: number;
    issues?: string[];
  };

  interface Props {
    aiFeatureFlagEnabled: boolean;
    isReadOnly: boolean;
    analysisLoading: boolean;
    qualityReport: AiReport | null;
    impactReport: AiReport | null;
    qualityError: string | null;
    impactError: string | null;
    qualityTriggering: boolean;
    impactTriggering: boolean;
    qualityParsed: ParsedResult<QualityAnalysisResult>;
    impactParsed: ParsedResult<ImpactAnalysisResult>;
    qualityScoreEntries: QualityScoreEntry[];
    onRunAnalysis: (type: AiAnalysisType) => void | Promise<void>;
    formatDateStr: (dateStr: string) => string;
    getStatusVariant: (status?: AiReportStatus) => BadgeVariant;
    getScoreVariant: (score?: number) => BadgeVariant;
    getImpactVariant: (level?: string) => BadgeVariant;
    isProcessing: (status?: AiReportStatus) => boolean;
  }

  let {
    aiFeatureFlagEnabled,
    isReadOnly,
    analysisLoading,
    qualityReport,
    impactReport,
    qualityError,
    impactError,
    qualityTriggering,
    impactTriggering,
    qualityParsed,
    impactParsed,
    qualityScoreEntries,
    onRunAnalysis,
    formatDateStr,
    getStatusVariant,
    getScoreVariant,
    getImpactVariant,
    isProcessing,
  }: Props = $props();
</script>

{#if !aiFeatureFlagEnabled}
  <div class="rounded-lg border border-[var(--color-warning-200)] bg-[var(--color-warning-50)] p-5">
    <div class="flex items-center gap-2">
      <Icon name="cpu" size="sm" />
      <span class="text-sm font-semibold text-[var(--color-text-primary)]">AI Analysis</span>
      <Badge variant="warning" size="sm">Coming Soon</Badge>
    </div>
    <p class="mt-2 text-sm text-[var(--color-text-muted)]">
 AI..
    </p>
  </div>
{:else}
  <div class="grid gap-4 lg:grid-cols-2">
    <!-- Quality Analysis -->
    <div class="rounded-lg border border-[var(--color-border-secondary)] bg-[var(--color-surface-50)] p-4 space-y-3">
      <div class="flex items-start justify-between gap-3">
        <div>
          <span class="text-sm font-semibold text-[var(--color-text-primary)] flex items-center gap-1"><Icon name="sparkles" size="xs" /> Quality Analysis</span>
          <p class="text-xs text-[var(--color-text-muted)]">Clarity, completeness, and terminology.</p>
        </div>
        {#if !isReadOnly}
          <Button
            variant="secondary"
            size="sm"
            onclick={() => onRunAnalysis('Quality')}
            disabled={qualityTriggering || isProcessing(qualityReport?.status)}
          >
            {#if qualityTriggering || isProcessing(qualityReport?.status)}
              <Spinner size="lg" />
              Running
            {:else}
              <Icon name="sparkles" size="sm" />
              Run
            {/if}
          </Button>
        {/if}
      </div>

      {#if qualityReport}
        <div class="flex flex-wrap items-center gap-2 text-xs">
          <Badge variant={getStatusVariant(qualityReport.status)} size="sm">
            {qualityReport.status}
          </Badge>
          {#if qualityReport.completedAt}
            <span class="text-[var(--color-text-muted)]">
              Updated {formatDateStr(qualityReport.completedAt)}
            </span>
          {:else if qualityReport.startedAt}
            <span class="text-[var(--color-text-muted)]">
              Started {formatDateStr(qualityReport.startedAt)}
            </span>
          {:else}
            <span class="text-[var(--color-text-muted)]">
              Created {formatDateStr(qualityReport.createdAt)}
            </span>
          {/if}
          {#if qualityReport.modelUsed}
            <span class="text-[var(--color-text-muted)]">Model {qualityReport.modelUsed}</span>
          {/if}
          {#if qualityReport.tokensUsed}
            <span class="text-[var(--color-text-muted)]">{qualityReport.tokensUsed} tokens</span>
          {/if}
        </div>
      {/if}

      {#if analysisLoading && !qualityReport}
        <div class="flex-1 flex items-center justify-center">
          <Spinner size="lg" />
        </div>
      {:else if qualityError}
        <div class="text-sm text-[var(--color-error-600)]">{qualityError}</div>
      {:else if !qualityReport}
        <div class="text-sm text-[var(--color-text-muted)]">No quality analysis run yet.</div>
      {:else if qualityReport.status === 'Failed'}
        <div class="text-sm text-[var(--color-error-600)]">
          {qualityReport.errorMessage || 'Quality analysis failed.'}
        </div>
      {:else if isProcessing(qualityReport.status)}
        <div class="flex-1 flex items-center justify-center">
          <Spinner size="lg" />
        </div>
      {:else if qualityParsed.data}
        <div class="space-y-3">
          <div class="flex items-center gap-3">
            <div class="text-3xl font-semibold text-[var(--color-text-primary)]">
              {qualityParsed.data.overallScore ?? 0}
            </div>
            <div class="space-y-1">
              <div class="text-xs text-[var(--color-text-muted)]">Overall Score</div>
              <Badge variant={getScoreVariant(qualityParsed.data.overallScore)} size="sm">
                {qualityParsed.data.overallScore ?? 0}
              </Badge>
            </div>
          </div>

          <div class="space-y-2">
            {#each qualityScoreEntries as entry (entry.key)}
              <div class="rounded-md border border-[var(--color-border-secondary)] bg-[var(--color-surface-100)] p-2">
                <div class="flex items-center justify-between">
                  <span class="text-xs font-medium text-[var(--color-text-secondary)]">{entry.label}</span>
                  <Badge variant={getScoreVariant(entry.score)} size="sm">
                    {entry.score}
                  </Badge>
                </div>
                {#if entry.issues?.length}
                  <ul class="mt-1 text-xs text-[var(--color-text-muted)] list-disc list-inside space-y-0.5">
                    {#each entry.issues as issue, issueIdx (issueIdx)}
                      <li>{issue}</li>
                    {/each}
                  </ul>
                {:else}
                  <span class="mt-1 text-[0.6875rem] text-[var(--color-text-muted)]">No issues noted.</span>
                {/if}
              </div>
            {/each}
          </div>

          {#if qualityParsed.data.improvements?.length}
            <div class="space-y-2">
              <h5 class="text-xs font-medium text-[var(--color-text-secondary)]">Recommendations</h5>
              {#each qualityParsed.data.improvements as item, improvementIdx (improvementIdx)}
                <div class="rounded-md border border-[var(--color-border-secondary)] bg-[var(--color-surface-100)] p-2">
                  <div class="text-xs font-medium text-[var(--color-text-secondary)]">
                    {item.section}
                  </div>
                  <div class="text-xs text-[var(--color-text-primary)]">{item.issue}</div>
                  <div class="text-xs text-[var(--color-text-muted)]">
                    Suggestion: {item.suggestion}
                  </div>
                </div>
              {/each}
            </div>
          {/if}

          {#if qualityParsed.data.strengths?.length}
            <div class="space-y-1">
              <h5 class="text-xs font-medium text-[var(--color-text-secondary)]">Highlights</h5>
              <ul class="text-xs text-[var(--color-text-muted)] list-disc list-inside space-y-0.5">
                {#each qualityParsed.data.strengths as strength, strengthIdx (strengthIdx)}
                  <li>{strength}</li>
                {/each}
              </ul>
            </div>
          {/if}
        </div>
      {:else}
        <div class="text-xs text-[var(--color-text-muted)]">
          {qualityParsed.error}
        </div>
        {#if qualityParsed.raw}
          <pre class="mt-2 text-xs bg-[var(--color-surface-100)] border border-[var(--color-border-secondary)] rounded p-2 overflow-auto">{qualityParsed.raw}</pre>
        {/if}
      {/if}
    </div>

    <!-- Impact Analysis -->
    <div class="rounded-lg border border-[var(--color-border-secondary)] bg-[var(--color-surface-50)] p-4 space-y-3">
      <div class="flex items-start justify-between gap-3">
        <div>
          <span class="text-sm font-semibold text-[var(--color-text-primary)] flex items-center gap-1"><Icon name="sparkles" size="xs" /> Impact Analysis</span>
          <p class="text-xs text-[var(--color-text-muted)]">Affected specs, artifacts, and risk.</p>
        </div>
        {#if !isReadOnly}
          <Button
            variant="secondary"
            size="sm"
            onclick={() => onRunAnalysis('Impact')}
            disabled={impactTriggering || isProcessing(impactReport?.status)}
          >
            {#if impactTriggering || isProcessing(impactReport?.status)}
              <Spinner size="lg" />
              Running
            {:else}
              <Icon name="sparkles" size="sm" />
              Run
            {/if}
          </Button>
        {/if}
      </div>

      {#if impactReport}
        <div class="flex flex-wrap items-center gap-2 text-xs">
          <Badge variant={getStatusVariant(impactReport.status)} size="sm">
            {impactReport.status}
          </Badge>
          {#if impactReport.completedAt}
            <span class="text-[var(--color-text-muted)]">
              Updated {formatDateStr(impactReport.completedAt)}
            </span>
          {:else if impactReport.startedAt}
            <span class="text-[var(--color-text-muted)]">
              Started {formatDateStr(impactReport.startedAt)}
            </span>
          {:else}
            <span class="text-[var(--color-text-muted)]">
              Created {formatDateStr(impactReport.createdAt)}
            </span>
          {/if}
          {#if impactReport.modelUsed}
            <span class="text-[var(--color-text-muted)]">Model {impactReport.modelUsed}</span>
          {/if}
          {#if impactReport.tokensUsed}
            <span class="text-[var(--color-text-muted)]">{impactReport.tokensUsed} tokens</span>
          {/if}
        </div>
      {/if}

      {#if analysisLoading && !impactReport}
        <div class="flex-1 flex items-center justify-center">
          <Spinner size="lg" />
        </div>
      {:else if impactError}
        <div class="text-sm text-[var(--color-error-600)]">{impactError}</div>
      {:else if !impactReport}
        <div class="text-sm text-[var(--color-text-muted)]">No impact analysis run yet.</div>
      {:else if impactReport.status === 'Failed'}
        <div class="text-sm text-[var(--color-error-600)]">
          {impactReport.errorMessage || 'Impact analysis failed.'}
        </div>
      {:else if isProcessing(impactReport.status)}
        <div class="flex-1 flex items-center justify-center">
          <Spinner size="lg" />
        </div>
      {:else if impactParsed.data}
        <div class="space-y-3">
          <div class="flex flex-wrap items-center gap-2">
            <Badge variant={getImpactVariant(impactParsed.data.impactLevel)} size="sm">
              {impactParsed.data.impactLevel ? impactParsed.data.impactLevel.toUpperCase() : 'UNKNOWN'}
            </Badge>
            {#if impactParsed.data.breakingChanges}
              <Badge variant="error" size="sm">Breaking Changes</Badge>
            {/if}
          </div>

          {#if impactParsed.data.recommendation}
            <div class="text-sm text-[var(--color-text-primary)]">
              {impactParsed.data.recommendation}
            </div>
          {/if}

          <div class="space-y-2">
            <h5 class="text-xs font-medium text-[var(--color-text-secondary)]">Affected Specs</h5>
            {#if impactParsed.data.affectedSpecs?.length}
              <div class="space-y-2">
                {#each impactParsed.data.affectedSpecs as item (item.specCode)}
                  <div class="rounded-md border border-[var(--color-border-secondary)] bg-[var(--color-surface-100)] p-2 space-y-1">
                    <div class="flex items-center justify-between">
                      <span class="text-xs font-mono text-[var(--color-text-primary)]">{item.specCode}</span>
                      <Badge variant={getImpactVariant(item.impactLevel)} size="sm">
                        {item.impactLevel}
                      </Badge>
                    </div>
                    <div class="text-xs text-[var(--color-text-muted)]">{item.relationship}</div>
                    <div class="text-xs text-[var(--color-text-primary)]">{item.description}</div>
                    {#if item.action}
                      <div class="text-xs text-[var(--color-text-muted)]">Action: {item.action}</div>
                    {/if}
                  </div>
                {/each}
              </div>
            {:else}
              <div class="text-xs text-[var(--color-text-muted)]">No impacted specs reported.</div>
            {/if}
          </div>

          {#if impactParsed.data.affectedArtifacts?.length}
            <div class="space-y-2">
              <h5 class="text-xs font-medium text-[var(--color-text-secondary)]">Affected Artifacts</h5>
              <div class="space-y-2">
                {#each impactParsed.data.affectedArtifacts as artifact (artifact.artifactPath)}
                  <div class="rounded-md border border-[var(--color-border-secondary)] bg-[var(--color-surface-100)] p-2">
                    <div class="flex items-center justify-between">
                      <span class="text-xs font-mono text-[var(--color-text-primary)]">{artifact.artifactPath}</span>
                      <Badge variant={getImpactVariant(artifact.impactLevel)} size="sm">
                        {artifact.impactLevel}
                      </Badge>
                    </div>
                    <div class="text-xs text-[var(--color-text-muted)]">{artifact.description}</div>
                  </div>
                {/each}
              </div>
            </div>
          {/if}
        </div>
      {:else}
        <div class="text-xs text-[var(--color-text-muted)]">
          {impactParsed.error}
        </div>
        {#if impactParsed.raw}
          <pre class="mt-2 text-xs bg-[var(--color-surface-100)] border border-[var(--color-border-secondary)] rounded p-2 overflow-auto">{impactParsed.raw}</pre>
        {/if}
      {/if}
    </div>
  </div>
{/if}
