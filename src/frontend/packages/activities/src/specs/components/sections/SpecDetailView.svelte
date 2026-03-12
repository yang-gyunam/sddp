<!-- Section: SpecDetailView — Projects > Specs -->
<script lang="ts">
  import { Button, Icon, IconButton, Spinner } from '@sddp/ui';
  import type { TabItem } from '@sddp/ui';
  import { untrack } from 'svelte';
  import { SvelteSet } from 'svelte/reactivity';
  import { Dropdown } from '@sddp/shell';
  import DetailHeader from '../../../shared/components/idioms/DetailHeader.svelte';
  import DetailTitle from '../../../shared/components/idioms/DetailTitle.svelte';
  import SpecDetailMainSections from './spec-detail/SpecDetailMainSections.svelte';
  import type { GlossarySegment } from './spec-detail/SpecDetailMainSections.svelte';
  import SpecDetailWorkflowSection from './spec-detail/SpecDetailWorkflowSection.svelte';
  import SpecDetailSupplementaryTabs from './spec-detail/SpecDetailSupplementaryTabs.svelte';
  import SpecDetailRejectModal from './spec-detail/SpecDetailRejectModal.svelte';
  import type { ReferenceItem } from '../../../shared/components/idioms/ReferencesSection.svelte';
  import type { MetadataItem } from '../../../shared/components/idioms/MetadataSection.svelte';

  import type { BadgeVariant } from '@sddp/ui';
  import { config as appConfig, formatDateTime, toast } from '@sddp/shell';

  import type { FieldAuthor } from '../../../shared/types';
  import type { SpecDetail as SpecDetailType, SpecStatus, SignOffRequest, SignOffSummary, SignOffDecision } from '../../types';
  import { isEditable, getAvailableActions, SPEC_STATUS_STYLES } from '../../types';
  import {
    canSubmitForReview as calcCanSubmitForReview,
    showSignOffSection as calcShowSignOffSection,
    canSignOff as calcCanSignOff,
    signOffComplete as calcSignOffComplete,
    workflowSteps as calcWorkflowSteps,
  } from '../../utils/spec-detail.utils';
  import { getSpecDocMarkdown, getSpecSummary, getFieldAuthors } from '../../services';
  import type { SpecSummaryResult } from '../../types';
  import { getArtifactService } from '../../../artifact/services';
  import type { Artifact } from '../../../artifact/types';
  import { getTermByTerm } from '../../../glossary/services/GlossaryService';
  import type { EntityMetadata, CreateEntityMetadataRequest, UpdateEntityMetadataRequest } from '../../../entities/types';
  import {
    getAiAnalysisService,
    type AiAnalysisType,
    type AiReport,
    type AiReportStatus,
    type ImpactAnalysisResult,
    type QualityAnalysisResult,
  } from '../../../ai';
  import { getAiStatus } from '../../../ai/services/AiStatusService';

  interface Props {
    spec: SpecDetailType;
    loading?: boolean;
    signOffSummary?: SignOffSummary | null;
    currentUserId?: string | null | undefined;
    entitySchemas?: EntityMetadata[];
    entitySchemasLoading?: boolean;
    entitySchemasError?: string | null;
    onEdit?: () => void;
    onSubmitForReview?: () => void;
    onApprove?: () => void;
    onReject?: (reason?: string) => void;
    onLock?: () => void;
    onNewVersion?: () => void;
    onRequirementClick?: (requirementId: string) => void;
    onConversationClick?: (conversationId: string) => void;
    onTransition?: (target: SpecStatus) => Promise<void>;
    onSignOff?: (request: SignOffRequest) => void;
    onLoadEntitySchemas?: () => void;
    onCreateEntitySchema?: (request: CreateEntityMetadataRequest) => void;
    onUpdateEntitySchema?: (entityId: string, request: UpdateEntityMetadataRequest) => void;
    onDeleteEntitySchema?: (entityId: string) => void;
    onDeactivate?: () => void;
    onClose?: () => void;
    /** Hide internal header (for Pattern B layout where header is rendered externally) */
    showHeader?: boolean;
    /** When true, hides all action buttons (edit, approve, generate, etc.) */
    readonly?: boolean;
    class?: string;
  }

  let {
    spec,
    loading = false,
    signOffSummary = null,
    currentUserId = '',
    entitySchemas = [],
    entitySchemasLoading = false,
    entitySchemasError = null,
    onEdit,
    onSubmitForReview,
    onApprove,
    onReject,
    onLock,
    onNewVersion,
    onRequirementClick,
    onConversationClick,
    onTransition,
    onSignOff,
    onLoadEntitySchemas,
    onCreateEntitySchema,
    onUpdateEntitySchema,
    onDeleteEntitySchema,
    onDeactivate,
    onClose,
    showHeader = true,
    readonly: isReadOnly = false,
    class: className = '',
  }: Props = $props();

  let showRejectModal = $state(false);
  let rejectReason = $state('');
  let signOffLoading = $state(false);
  let signOffDecision = $state<SignOffDecision | null>(null);
  let signOffConditions = $state('');
  let signOffComments = $state('');
  const aiService = getAiAnalysisService();

  function ensureAiContext(): void {
    aiService.setContext(spec.tenantId, spec.projectId);
  }

  function ensureArtifactContext(): void {
    artifactService.setContext(spec.tenantId, spec.projectId);
  }
  let analysisLoading = $state(false);
  let qualityReport = $state<AiReport | null>(null);
  let impactReport = $state<AiReport | null>(null);
  let qualityError = $state<string | null>(null);
  let impactError = $state<string | null>(null);
  let qualityTriggering = $state(false);
  let impactTriggering = $state(false);
  let pollTimer: ReturnType<typeof setInterval> | null = null;
  let lastSpecId: string | null = null;
  let aiStatusRequestId = $state(0);
  let aiEffectiveEnabled = $state(true);
  const aiFeatureFlagEnabled = appConfig.get('enableAiFeatures');
  const specSupplementaryTabsEnabled = appConfig.get('enableSpecSupplementaryTabs');
  const artifactService = getArtifactService();
  let artifacts = $state<Artifact[]>([]);
  let artifactsLoading = $state(false);
  let artifactsError = $state<string | null>(null);
  let docMarkdown = $state<string | null>(null);
  let docLoading = $state(false);
  let docError = $state<string | null>(null);
  let docViewMode = $state<'preview' | 'markdown'>('preview');
  let summaryResult = $state<SpecSummaryResult | null>(null);
  let summaryLoading = $state(false);
  let summaryError = $state<string | null>(null);
  let fieldAuthors = $state<FieldAuthor[]>([]);

  // Draft status: show required field markers for Submit for Review
  const isDraft = $derived(spec.status === 'Draft');

  // Submit for Review requires these fields (matches backend ValidateForReview)
  const canSubmitForReview = $derived(calcCanSubmitForReview(spec));

  // CollapsibleGroup expanded states (matching SpecForm layout)
  let basicExpanded = $state(true);
  let scopeExpanded = $state(true);
  let criteriaExpanded = $state(true);
  let governanceExpanded = $state(true);
  let referencesExpanded = $state(true);
  let metadataExpanded = $state(true);
  let workflowExpanded = $state(true);

  // Supplementary tab state (replaces old isAiAnalysisExpanded)
  let activeDetailTab = $state('documentation');

  type GenerationOption = {
    key: 'backend' | 'frontend' | 'tests' | 'docs';
    label: string;
    description: string;
  };

  const generationOptions: GenerationOption[] = [
    { key: 'backend', label: 'Backend Code', description: 'API, services, and data models' },
    { key: 'frontend', label: 'Frontend UI', description: 'Views and UI scaffolding' },
    { key: 'tests', label: 'Tests', description: 'Unit and integration tests' },
    { key: 'docs', label: 'Docs', description: 'Architecture and usage docs' },
  ];

  const selectedGeneration = new SvelteSet<GenerationOption['key']>(['backend', 'frontend']);
  let generationProgress = $state(0);
  let generationRunning = $state(false);
  let generationResults = $state<Array<{ label: string; status: 'success' | 'failed'; detail: string }>>([]);

  const canEdit = $derived(isEditable(spec.status));
  const availableActions = $derived(getAvailableActions(spec.status));
  // Show sign-off section when InReview (even 0/0: user can submit sign-off on-the-fly)
  const showSignOffSection = $derived(calcShowSignOffSection(spec));
  // Can current user sign off? (InReview + not already decided)
  const canSignOff = $derived(calcCanSignOff(spec, currentUserId, signOffSummary));
  const signOffComplete = $derived(calcSignOffComplete(spec, signOffSummary));
  const hasGeneratedArtifacts = $derived(
    artifacts.length > 0 || generationResults.length > 0
  );
  const qualityParsed = $derived.by(() =>
    parseResult<QualityAnalysisResult>(qualityReport?.resultJson ?? null)
  );
  const impactParsed = $derived.by(() =>
    parseResult<ImpactAnalysisResult>(impactReport?.resultJson ?? null)
  );
  const qualityScoreEntries = $derived.by(() => {
    if (!qualityParsed.data?.scores) return [];
    const scores = qualityParsed.data.scores;
    return [
      { key: 'clarity', label: 'Clarity', ...scores.clarity },
      { key: 'completeness', label: 'Completeness', ...scores.completeness },
      { key: 'consistency', label: 'Consistency', ...scores.consistency },
      { key: 'measurability', label: 'Measurability', ...scores.measurability },
      { key: 'terminology', label: 'Terminology', ...scores.terminology },
    ];
  });
  const detailTabs = $derived.by<TabItem[]>(() => {
    const tabs: TabItem[] = [];
    if (!specSupplementaryTabsEnabled) {
      return tabs;
    }
    if (aiFeatureFlagEnabled && aiEffectiveEnabled) {
      tabs.push({ id: 'ai-analysis', label: 'AI Analysis', icon: 'cpu' });
    }
    if (!isReadOnly) {
      tabs.push({ id: 'generation', label: 'Generation', icon: 'code' });
    }
    tabs.push(
      { id: 'documentation', label: 'Docs', icon: 'file-text' },
      { id: 'entity-schema', label: 'Schema', icon: 'database' },
    );
    return tabs;
  });
  const workflowSteps = $derived(calcWorkflowSteps(spec, signOffComplete, hasGeneratedArtifacts));

  $effect(() => {
    const tabs = detailTabs;
    if (tabs.length === 0) {
      activeDetailTab = '';
      return;
    }
    if (!tabs.some((tab) => tab.id === activeDetailTab)) {
      activeDetailTab = tabs[0]?.id ?? '';
    }
  });

  // Build reference items for ReferencesSection
  const referenceItems = $derived.by(() => {
    const items: ReferenceItem[] = [];
    // Born from Conversation
    if (spec.bornFromConversationId) {
      const icon = spec.bornFromConversationType === 'Channel' ? 'hash'
        : spec.bornFromConversationType === 'Forum' ? 'clipboard-list'
        : 'message-circle';
      const prefix = spec.bornFromConversationType === 'Channel' ? '#' : '';
      const name = spec.bornFromConversationName
        ? `${prefix}${spec.bornFromConversationName}`
        : (spec.bornFromConversationId ?? '');
      const value = spec.bornFromConversationDescription
        ? `${name} — ${spec.bornFromConversationDescription}`
        : name;
      items.push({
        icon,
        label: 'Source Conversation',
        value,
        sublabel: spec.bornFromConversationType ?? undefined,
        onClick: () => onConversationClick?.(spec.bornFromConversationId!),
      });
    }
    // Fulfills Requirement
    if (spec.requirementId) {
      items.push({
        icon: 'clipboard-list',
        label: 'Source Requirement',
        value: spec.requirementTitle || spec.requirementCode || spec.requirementId,
        sublabel: spec.requirementCode ?? undefined,
        onClick: () => onRequirementClick?.(spec.requirementId!),
      });
    }
    return items;
  });

  const metadataItems = $derived.by(() => {
    const items: MetadataItem[] = [];
    // Row 1: Status | Owner (matching Requirement pattern)
    const statusStyle = SPEC_STATUS_STYLES[spec.status];
    items.push({ label: 'Status', value: statusStyle.label, class: statusStyle.textColor });
    const ownerDisplay = spec.owners?.split(',').map(o => o.trim()).filter(Boolean).join(', ');
    items.push({ label: 'Owner', value: ownerDisplay || 'Unassigned' });
    // Row 2: Version | Valid From
    items.push({ label: 'Version', value: spec.version });
    // Row 2: Valid From | Locked At (Spec-specific)
    if (spec.validFrom) items.push({ label: 'Valid From', value: formatDateStr(spec.validFrom) });
    if (spec.lockedAt) items.push({ label: 'Locked At', value: formatDateStr(spec.lockedAt) });
    // Row 3: Created By | Created
    if (spec.createdBy?.name) items.push({ label: 'Created By', value: spec.createdBy.name, type: 'person', avatarUrl: spec.createdBy.avatarUrl });
    if (spec.createdAt) items.push({ label: 'Created', value: formatDateStr(spec.createdAt) });
    // Row 4: Updated By | Updated
    if (spec.updatedBy?.name) items.push({ label: 'Updated By', value: spec.updatedBy.name, type: 'person', avatarUrl: spec.updatedBy.avatarUrl });
    if (spec.updatedAt) items.push({ label: 'Updated', value: formatDateStr(spec.updatedAt) });
    // Valid To (if exists)
    if (spec.validTo) items.push({ label: 'Valid To', value: formatDateStr(spec.validTo) });
    return items;
  });

  function formatDateStr(dateStr: string): string {
    return formatDateTime(dateStr, { month: 'short' });
  }

  async function loadDocMarkdown(): Promise<void> {
    docLoading = true;
    docError = null;
    try {
      docMarkdown = await getSpecDocMarkdown(spec.tenantId, spec.projectId, spec.id);
    } catch (error) {
      docError = error instanceof Error ? error.message : 'Failed to load document';
      docMarkdown = null;
    } finally {
      docLoading = false;
    }
  }

  async function loadSummary(refresh = false): Promise<void> {
    summaryLoading = true;
    summaryError = null;
    try {
      summaryResult = await getSpecSummary(spec.tenantId, spec.projectId, spec.id, refresh);
    } catch (error) {
      summaryError = error instanceof Error ? error.message : 'Failed to load summary';
      summaryResult = null;
    } finally {
      summaryLoading = false;
    }
  }

  function downloadDocMarkdown(): void {
    if (!docMarkdown || typeof window === 'undefined') return;
    const blob = new Blob([docMarkdown], { type: 'text/markdown' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = `${spec.code || 'spec'}-doc.md`;
    link.click();
    URL.revokeObjectURL(url);
  }

  async function loadFieldAuthors(): Promise<void> {
    try {
      fieldAuthors = await getFieldAuthors(spec.tenantId, spec.projectId, spec.id);
    } catch {
      fieldAuthors = [];
    }
  }

  function getFieldAuthor(fieldName: string): FieldAuthor | null {
    return fieldAuthors.find((a) => a.fieldName.toLowerCase() === fieldName.toLowerCase()) ?? null;
  }

  function parseGlossaryMentions(text: string): GlossarySegment[] {
    if (!text) return [];

    const segments: GlossarySegment[] = [];
    const regex = /@\{([^}]+)\}/g;
    let lastIndex = 0;
    let match: RegExpExecArray | null;

    while ((match = regex.exec(text)) !== null) {
      const matchIndex = match.index ?? 0;
      const fullMatch = match[0];
      const term = match[1]?.trim() ?? '';

      if (matchIndex > lastIndex) {
        segments.push({ type: 'text', value: text.slice(lastIndex, matchIndex) });
      }

      if (term) {
        segments.push({ type: 'term', value: term });
      } else {
        segments.push({ type: 'text', value: fullMatch });
      }

      lastIndex = matchIndex + fullMatch.length;
    }

    if (lastIndex < text.length) {
      segments.push({ type: 'text', value: text.slice(lastIndex) });
    }

    return segments;
  }

  const definitionSegments = $derived.by(() => parseGlossaryMentions(spec.definitions ?? ''));

  async function handleGlossaryTermClick(term: string): Promise<void> {
    if (!term) return;
    if (!spec.tenantId || !spec.projectId) {
      toast.error('Glossary context is unavailable.');
      return;
    }

    try {
      const detail = await getTermByTerm(spec.tenantId, spec.projectId, term);
      if (typeof window === 'undefined') return;
      window.dispatchEvent(
        new CustomEvent('sddp:navigate', {
          detail: {
            type: 'glossary',
            id: detail.id,
            label: detail.term,
          },
        })
      );
    } catch (error) {
      console.error('Failed to open glossary term:', error);
      toast.error(`Glossary term not found: ${term}`);
    }
  }

  function handleAction(action: string): void {
    switch (action) {
      case 'submitForReview':
        onSubmitForReview?.();
        break;
      case 'approve':
        onApprove?.();
        break;
      case 'reject':
        showRejectModal = true;
        break;
      case 'lock':
        onLock?.();
        break;
      case 'newVersion':
        onNewVersion?.();
        break;
    }
  }

  function handleEditClick(): void {
    if (canEdit && onEdit) {
      onEdit();
      return;
    }
    if (onEdit) {
      toast.warning('Locked specs cannot be edited.', { title: 'Spec Locked' });
    }
  }

  function handleRejectSubmit(): void {
    onReject?.(rejectReason.trim() || undefined);
    showRejectModal = false;
    rejectReason = '';
  }

  async function handleSignOffSubmit(): Promise<void> {
    if (!signOffDecision) return;
    const request: SignOffRequest = {
      decision: signOffDecision,
      comments: signOffComments.trim() || undefined,
    };
    if (signOffDecision === 'Conditional' && signOffConditions.trim()) {
      request.conditions = signOffConditions.trim();
    }
    signOffLoading = true;
    try {
      const result = onSignOff?.(request);
      if (result && typeof result === 'object' && 'then' in result) {
        await result;
      }
      signOffDecision = null;
      signOffConditions = '';
      signOffComments = '';
    } finally {
      signOffLoading = false;
    }
  }

  function parseResult<T>(raw: string | null): { data?: T; error?: string; raw?: string } {
    if (!raw) return { error: 'No analysis result available' };
    try {
      return { data: JSON.parse(raw) as T };
    } catch {
      return { error: 'Invalid JSON response', raw };
    }
  }

  function getStatusVariant(status?: AiReportStatus): BadgeVariant {
    switch (status) {
      case 'Completed':
        return 'success';
      case 'Failed':
        return 'error';
      case 'Pending':
      case 'Processing':
        return 'warning';
      default:
        return 'default';
    }
  }

  function getScoreVariant(score?: number): BadgeVariant {
    if (score === undefined || score === null) return 'default';
    if (score >= 80) return 'success';
    if (score >= 60) return 'warning';
    return 'error';
  }

  function getImpactVariant(level?: string): BadgeVariant {
    switch ((level || '').toLowerCase()) {
      case 'high':
        return 'error';
      case 'medium':
        return 'warning';
      case 'low':
        return 'success';
      default:
        return 'default';
    }
  }

  function isProcessing(status?: AiReportStatus): boolean {
    return status === 'Pending' || status === 'Processing';
  }

  async function loadArtifacts(): Promise<void> {
    artifactsLoading = true;
    artifactsError = null;
    try {
      ensureArtifactContext();
      artifacts = await artifactService.getArtifactsBySpec(spec.id);
    } catch (error) {
      artifactsError = error instanceof Error ? error.message : 'Failed to load artifacts';
      artifacts = [];
    } finally {
      artifactsLoading = false;
    }
  }

  function toggleGenerationOption(option: GenerationOption['key']): void {
    if (selectedGeneration.has(option)) {
      selectedGeneration.delete(option);
    } else {
      selectedGeneration.add(option);
    }
  }

  function generateResultLabel(option: GenerationOption['key']): string {
    switch (option) {
      case 'backend':
        return `backend/${spec.code}/api`;
      case 'frontend':
        return `frontend/${spec.code}/ui`;
      case 'tests':
        return `tests/${spec.code}.spec`;
      case 'docs':
        return `docs/${spec.code}.md`;
      default:
        return spec.code;
    }
  }

  async function startGeneration(): Promise<void> {
    if (generationRunning) return;
    if (selectedGeneration.size === 0) {
      toast.warning('Select at least one output to generate.');
      return;
    }

    generationRunning = true;
    generationProgress = 0;
    generationResults = [];

    const totalSteps = 20;
    let step = 0;

    await new Promise<void>((resolve) => {
      const timer = setInterval(() => {
        step += 1;
        generationProgress = Math.min(100, Math.round((step / totalSteps) * 100));
        if (step >= totalSteps) {
          clearInterval(timer);
          resolve();
        }
      }, 120);
    });

    generationResults = Array.from(selectedGeneration).map((option) => ({
      label: generateResultLabel(option),
      status: 'success',
      detail: `${option.toUpperCase()} generated`,
    }));

    generationRunning = false;
    generationProgress = 100;
    loadArtifacts().catch((err) => console.warn('[SpecDetail] loadArtifacts after generation failed:', err));
    toast.success('Generation completed');
  }

  async function loadReportForType(type: AiAnalysisType): Promise<void> {
    const isQuality = type === 'Quality';
    const setReport = (report: AiReport | null) => {
      if (isQuality) {
        qualityReport = report;
      } else {
        impactReport = report;
      }
    };
    const setError = (message: string | null) => {
      if (isQuality) {
        qualityError = message;
      } else {
        impactError = message;
      }
    };

    try {
      const summaries = await aiService.getReportsByTarget(spec.id, type);
      const latest = summaries[0];
      if (!latest) {
        setReport(null);
        return;
      }
      const detail = await aiService.getReportById(latest.id);
      setReport(detail);
      setError(null);
    } catch (error) {
      const message = error instanceof Error ? error.message : 'Failed to load analysis';
      setError(message);
    }
  }

  async function loadAnalysisReports(silent = false): Promise<void> {
    ensureAiContext();
    if (!silent) {
      analysisLoading = true;
    }

    await Promise.all([
      loadReportForType('Quality'),
      loadReportForType('Impact'),
    ]);

    if (!silent) {
      analysisLoading = false;
    }
  }

  async function loadAiAvailabilityAndReports(tenantId: string, projectId: string): Promise<void> {
    if (!specSupplementaryTabsEnabled || !aiFeatureFlagEnabled) {
      aiEffectiveEnabled = false;
      analysisLoading = false;
      return;
    }

    const requestId = ++aiStatusRequestId;
    let effectiveEnabled = true;

    try {
      const status = await getAiStatus(tenantId, projectId);
      effectiveEnabled = status.effectiveEnabled;
    } catch {
      effectiveEnabled = true;
    }

    if (requestId !== aiStatusRequestId) return;

    aiEffectiveEnabled = effectiveEnabled;
    if (!effectiveEnabled) {
      activeDetailTab = 'documentation';
      analysisLoading = false;
      return;
    }

    activeDetailTab = 'ai-analysis';
    await loadAnalysisReports().catch(() => {
      analysisLoading = false;
    });
  }

  async function runAnalysis(type: AiAnalysisType): Promise<void> {
    if (type !== 'Quality' && type !== 'Impact') return;
    if (!specSupplementaryTabsEnabled || !aiFeatureFlagEnabled) {
      toast.info('AI analysis is coming soon.');
      return;
    }
    ensureAiContext();
    const setTriggering = (value: boolean) => {
      if (type === 'Quality') {
        qualityTriggering = value;
      } else {
        impactTriggering = value;
      }
    };
    const setError = (message: string | null) => {
      if (type === 'Quality') {
        qualityError = message;
      } else {
        impactError = message;
      }
    };

    setTriggering(true);
    try {
      const report = await aiService.triggerAnalysis({
        analysisType: type,
        projectId: spec.projectId,
        targetId: spec.id,
        targetType: 'spec',
      });

      if (type === 'Quality') {
        qualityReport = report;
      } else {
        impactReport = report;
      }

      setError(null);
    } catch (error) {
      const message = error instanceof Error ? error.message : 'Failed to trigger analysis';
      setError(message);
    } finally {
      setTriggering(false);
    }
  }

  $effect(() => {
    if (!spec?.id) return;
    if (spec.id === lastSpecId) return;
    lastSpecId = spec.id;
    ensureAiContext();
    qualityReport = null;
    impactReport = null;
    qualityError = null;
    impactError = null;
    generationRunning = false;
    generationProgress = 0;
    generationResults = [];
    selectedGeneration.clear();
    selectedGeneration.add('backend');
    selectedGeneration.add('frontend');
    artifacts = [];
    artifactsError = null;
    docMarkdown = null;
    docError = null;
    summaryResult = null;
    summaryError = null;
    fieldAuthors = [];
    // Reset CollapsibleGroup states (always expanded)
    basicExpanded = true;
    scopeExpanded = true;
    criteriaExpanded = true;
    governanceExpanded = true;
    aiEffectiveEnabled = false;
    activeDetailTab = 'documentation';
    untrack(() => {
      if (specSupplementaryTabsEnabled) {
        loadAiAvailabilityAndReports(spec.tenantId, spec.projectId).catch(() => {
          analysisLoading = false;
        });
        loadArtifacts().catch((err) => console.warn('[SpecDetail] loadArtifacts failed:', err));
        loadSummary().catch(() => {
          summaryLoading = false;
        });
      } else {
        analysisLoading = false;
        artifactsLoading = false;
        summaryLoading = false;
      }
      loadFieldAuthors().catch((err) => console.warn('[SpecDetail] loadFieldAuthors failed:', err));
    });
  });

  $effect(() => {
    const shouldPoll = isProcessing(qualityReport?.status) || isProcessing(impactReport?.status);
    if (shouldPoll && !pollTimer) {
      pollTimer = setInterval(() => {
        loadAnalysisReports(true).catch((err) => console.warn('[SpecDetail] analysis poll failed:', err));
      }, 15000);
    }
    if (!shouldPoll && pollTimer) {
      clearInterval(pollTimer);
      pollTimer = null;
    }

    return () => {
      if (pollTimer) {
        clearInterval(pollTimer);
        pollTimer = null;
      }
    };
  });
</script>

<div class="flex flex-col h-full {className}">
  {#if showHeader}
    <!-- Header -->
    <DetailHeader>
      {#snippet leading()}
        <Icon name="file-text" size="md" class="text-[var(--color-info-600)]" />
      {/snippet}
      <DetailTitle title={spec.title} code={spec.code}>
        <span class="text-xs text-[var(--color-text-tertiary)] flex-shrink-0">
          v{spec.version}
        </span>
      </DetailTitle>
      {#snippet actions()}
        {#if !isReadOnly && onEdit}
          <Button
            variant="ghost"
            size="sm"
            onclick={handleEditClick}
            class={canEdit ? '' : 'opacity-60'}
            title={canEdit ? 'Edit spec' : 'Specs can only be edited in Draft'}
          >
            <Icon name="edit" size="sm" />
            Edit
          </Button>
        {/if}
        {#if onClose}
          <Button variant="ghost" size="sm" onclick={onClose}>
            <Icon name="x" size="sm" />
          </Button>
        {/if}
        {#if !isReadOnly && onDeactivate && spec.status !== 'Locked'}
          <Dropdown position="bottom-right">
            {#snippet trigger()}
              <IconButton icon="more-vertical" variant="ghost" size="sm" title="More actions" />
            {/snippet}
            <div class="py-1 min-w-[160px]">
              <Button
                variant="unstyled"
                onclick={onDeactivate}
                class="w-full text-left px-3 py-1.5 text-xs font-medium hover:bg-[var(--color-surface-200)] transition-colors text-[var(--color-warning-600)] flex items-center gap-2"
              >
                <Icon name="lock" size="xs" />
                Deactivate
              </Button>
            </div>
          </Dropdown>
        {/if}
      {/snippet}
    </DetailHeader>
  {/if}

  <!-- Content -->
  <div class="flex-1 overflow-y-auto p-4">
    {#if loading}
      <div class="flex-1 flex items-center justify-center">
        <Spinner size="lg" />
      </div>
    {:else}
      <div class="space-y-5">

      <!-- === MAIN CONTENT: Form-Aligned CollapsibleGroups === -->
      <SpecDetailMainSections
        {spec}
        {isDraft}
        {basicExpanded}
        {scopeExpanded}
        {criteriaExpanded}
        {governanceExpanded}
        {referencesExpanded}
        {metadataExpanded}
        onToggleBasic={() => (basicExpanded = !basicExpanded)}
        onToggleScope={() => (scopeExpanded = !scopeExpanded)}
        onToggleCriteria={() => (criteriaExpanded = !criteriaExpanded)}
        onToggleGovernance={() => (governanceExpanded = !governanceExpanded)}
        onToggleReferences={() => (referencesExpanded = !referencesExpanded)}
        onToggleMetadata={() => (metadataExpanded = !metadataExpanded)}
        {getFieldAuthor}
        {definitionSegments}
        onGlossaryTermClick={handleGlossaryTermClick}
        {referenceItems}
        {metadataItems}
      />

      <SpecDetailWorkflowSection
        currentStatus={spec.status}
        {workflowExpanded}
        {workflowSteps}
        isReadOnly={isReadOnly}
        onToggleWorkflow={() => (workflowExpanded = !workflowExpanded)}
        {onTransition}
        onGenerate={startGeneration}
        {signOffComplete}
        {canSubmitForReview}
        {onNewVersion}
        {availableActions}
        onAction={handleAction}
        {showSignOffSection}
        signOffSummary={signOffSummary}
        currentUserId={currentUserId || null}
        {canSignOff}
        bind:signOffDecision={signOffDecision}
        bind:signOffConditions={signOffConditions}
        bind:signOffComments={signOffComments}
        {signOffLoading}
        onSignOffSubmit={handleSignOffSubmit}
      />
      </div>

      <SpecDetailSupplementaryTabs
        {detailTabs}
        activeDetailTab={activeDetailTab}
        onTabChange={(id) => (activeDetailTab = id)}
        onDocumentationTabSelected={() => {
          if (!docMarkdown && !docLoading) {
            loadDocMarkdown();
          }
        }}
        isReadOnly={isReadOnly}
        {aiFeatureFlagEnabled}
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
        onRunAnalysis={runAnalysis}
        {formatDateStr}
        {getStatusVariant}
        {getScoreVariant}
        {getImpactVariant}
        {isProcessing}
        specStatus={spec.status}
        {generationOptions}
        selectedGeneration={selectedGeneration}
        {generationProgress}
        {generationRunning}
        {generationResults}
        {artifacts}
        {artifactsLoading}
        {artifactsError}
        onStartGeneration={startGeneration}
        onToggleGenerationOption={toggleGenerationOption}
        {docMarkdown}
        {docLoading}
        {docError}
        {docViewMode}
        {summaryResult}
        {summaryLoading}
        {summaryError}
        onLoadDocMarkdown={loadDocMarkdown}
        onLoadSummary={loadSummary}
        onDownloadDocMarkdown={downloadDocMarkdown}
        onDocViewModeChange={(mode) => (docViewMode = mode)}
        {entitySchemas}
        {entitySchemasLoading}
        {entitySchemasError}
        {onLoadEntitySchemas}
        {onCreateEntitySchema}
        {onUpdateEntitySchema}
        {onDeleteEntitySchema}
      />
    {/if}
  </div>
</div>

<!-- Reject Modal -->
<SpecDetailRejectModal
  show={showRejectModal}
  bind:rejectReason={rejectReason}
  onCancel={() => {
    showRejectModal = false;
    rejectReason = '';
  }}
  onSubmit={handleRejectSubmit}
/>
