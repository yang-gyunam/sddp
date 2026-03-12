<!-- Section: SpecDetailSupplementaryTabs — Specs > SpecDetailView -->
<script lang="ts">
  import { TabBar } from '@sddp/ui';
  import type { TabItem, BadgeVariant } from '@sddp/ui';
  import type { SpecStatus, SpecSummaryResult } from '../../../types';
  import type {
    EntityMetadata,
    CreateEntityMetadataRequest,
    UpdateEntityMetadataRequest,
  } from '../../../../entities/types';
  import type { Artifact } from '../../../../artifact/types';
  import type {
    AiAnalysisType,
    AiReport,
    AiReportStatus,
    ImpactAnalysisResult,
    QualityAnalysisResult,
  } from '../../../../ai';
  import EntitySchemaSection from '../EntitySchemaSection.svelte';
  import SpecDetailAiAnalysisTab from './SpecDetailAiAnalysisTab.svelte';
  import SpecDetailGenerationTab from './SpecDetailGenerationTab.svelte';
  import SpecDetailDocumentationTab from './SpecDetailDocumentationTab.svelte';

  type GenerationOption = {
    key: 'backend' | 'frontend' | 'tests' | 'docs';
    label: string;
    description: string;
  };

  type GenerationResult = {
    label: string;
    status: 'success' | 'failed';
    detail: string;
  };

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

  type DocViewMode = 'preview' | 'markdown';

  interface Props {
    detailTabs: TabItem[];
    activeDetailTab: string;
    onTabChange: (id: string) => void;
    onDocumentationTabSelected?: () => void | Promise<void>;
    isReadOnly: boolean;

    aiFeatureFlagEnabled: boolean;
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

    specStatus: SpecStatus;
    generationOptions: GenerationOption[];
    selectedGeneration: Set<GenerationOption['key']>;
    generationProgress: number;
    generationRunning: boolean;
    generationResults: GenerationResult[];
    artifacts: Artifact[];
    artifactsLoading: boolean;
    artifactsError: string | null;
    onStartGeneration: () => void | Promise<void>;
    onToggleGenerationOption: (option: GenerationOption['key']) => void;

    docMarkdown: string | null;
    docLoading: boolean;
    docError: string | null;
    docViewMode: DocViewMode;
    summaryResult: SpecSummaryResult | null;
    summaryLoading: boolean;
    summaryError: string | null;
    onLoadDocMarkdown: () => void | Promise<void>;
    onLoadSummary: (refresh?: boolean) => void | Promise<void>;
    onDownloadDocMarkdown: () => void;
    onDocViewModeChange: (mode: DocViewMode) => void;

    entitySchemas: EntityMetadata[];
    entitySchemasLoading: boolean;
    entitySchemasError: string | null;
    onLoadEntitySchemas?: () => void;
    onCreateEntitySchema?: (request: CreateEntityMetadataRequest) => void;
    onUpdateEntitySchema?: (entityId: string, request: UpdateEntityMetadataRequest) => void;
    onDeleteEntitySchema?: (entityId: string) => void;
  }

  let {
    detailTabs,
    activeDetailTab,
    onTabChange,
    onDocumentationTabSelected,
    isReadOnly,

    aiFeatureFlagEnabled,
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

    specStatus,
    generationOptions,
    selectedGeneration,
    generationProgress,
    generationRunning,
    generationResults,
    artifacts,
    artifactsLoading,
    artifactsError,
    onStartGeneration,
    onToggleGenerationOption,

    docMarkdown,
    docLoading,
    docError,
    docViewMode,
    summaryResult,
    summaryLoading,
    summaryError,
    onLoadDocMarkdown,
    onLoadSummary,
    onDownloadDocMarkdown,
    onDocViewModeChange,

    entitySchemas,
    entitySchemasLoading,
    entitySchemasError,
    onLoadEntitySchemas,
    onCreateEntitySchema,
    onUpdateEntitySchema,
    onDeleteEntitySchema,
  }: Props = $props();
</script>

<!-- === SUPPLEMENTARY TABS === -->
{#if detailTabs.length > 0}
  <div class="mt-5 border-t border-[var(--color-border-secondary)] pt-4">
    <TabBar
      tabs={detailTabs}
      activeTab={activeDetailTab}
      size="sm"
      variant="underline"
      onchange={(id) => {
        onTabChange(id);
        if (id === 'documentation') {
          onDocumentationTabSelected?.();
        }
      }}
    />

    <div class="mt-4">
      {#if activeDetailTab === 'ai-analysis'}
        <SpecDetailAiAnalysisTab
          {aiFeatureFlagEnabled}
          isReadOnly={isReadOnly}
          {analysisLoading}
          {qualityReport}
          {impactReport}
          {qualityError}
          {impactError}
          {qualityTriggering}
          {impactTriggering}
          {qualityParsed}
          {impactParsed}
          {qualityScoreEntries}
          onRunAnalysis={onRunAnalysis}
          {formatDateStr}
          {getStatusVariant}
          {getScoreVariant}
          {getImpactVariant}
          {isProcessing}
        />
      {:else if activeDetailTab === 'generation'}
        <SpecDetailGenerationTab
          {specStatus}
          {generationOptions}
          {selectedGeneration}
          {generationProgress}
          {generationRunning}
          {generationResults}
          {artifacts}
          {artifactsLoading}
          {artifactsError}
          onStartGeneration={onStartGeneration}
          onToggleGenerationOption={onToggleGenerationOption}
        />
      {:else if activeDetailTab === 'documentation'}
        <SpecDetailDocumentationTab
          isReadOnly={isReadOnly}
          {docMarkdown}
          {docLoading}
          {docError}
          {docViewMode}
          {summaryResult}
          {summaryLoading}
          {summaryError}
          {formatDateStr}
          onLoadDocMarkdown={onLoadDocMarkdown}
          onLoadSummary={onLoadSummary}
          onDownloadDocMarkdown={onDownloadDocMarkdown}
          onDocViewModeChange={onDocViewModeChange}
        />
      {:else if activeDetailTab === 'entity-schema'}
        <EntitySchemaSection
          entities={entitySchemas}
          loading={entitySchemasLoading}
          error={entitySchemasError}
          onLoad={onLoadEntitySchemas}
          onCreate={isReadOnly ? undefined : onCreateEntitySchema}
          onUpdate={isReadOnly ? undefined : onUpdateEntitySchema}
          onDelete={isReadOnly ? undefined : onDeleteEntitySchema}
        />
      {/if}
    </div>
  </div>
{/if}
