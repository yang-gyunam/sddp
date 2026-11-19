<!-- Section: EntityFullDetailTab — Projects > Traceability -->
<script lang="ts">
  import { onDestroy, untrack } from 'svelte';
  import { navigateToEntity } from '../../navigation/entityNavigation';
  import { Icon, IconButton, Spinner, ResizeHandle } from '@sddp/ui';
  import { getAuthState } from '@sddp/shell/auth';
  import { AccessDeniedError, ApiRequestError } from '../../api/fetchWithAuth';
  import DetailHeader from '../idioms/DetailHeader.svelte';
  import RequirementLevelBadge from '../../../requirements/components/idioms/RequirementLevelBadge.svelte';

  import SpecDetailView from '../../../specs/components/sections/SpecDetailView.svelte';
  import RequirementDetailView from '../../../requirements/components/sections/RequirementDetailView.svelte';
  import ChannelView from '../../../conversations/components/sections/ChannelView.svelte';
  import GlossaryDetail from '../../../glossary/components/sections/GlossaryDetail.svelte';
  import ArtifactContent from '../../../artifact/components/sections/ArtifactContent.svelte';
  import DiagramWorkbench from '../../../diagrams/components/sections/DiagramWorkbench2.svelte';
  import { getDiagram2 as getDiagram } from '../../../diagrams/services/DiagramService2';
  import {
    clearDiagramDraft,
    diagramDraftKey,
    diagramDraftScope,
  } from '../../../diagrams/services/DiagramDraftState';
  import type { DiagramV2Dto as DiagramDto } from '../../../diagrams/types2';
  import TraceGraphSection from '../../../relationship/components/sections/TraceGraphSection.svelte';

  import { getSpecById, getSignOffSummary } from '../../../specs/services';
  import { getRequirementById } from '../../../requirements/services';
  import { getConversationById } from '../../../conversations/services/ConversationService';
  import { getTermById, getTermUsage } from '../../../glossary/services/GlossaryService';
  import { getArtifactById } from '../../../artifact/services/ArtifactService';

  import type { SpecDetail, SignOffSummary } from '../../../specs/types';
  import type { RequirementDetail } from '../../../requirements/types';
  import type { GlossaryTermDetail, GlossaryTermUsage } from '../../../glossary/types';
  import type { ConversationSummaryDto } from '../../../conversations/services/ConversationService';
  import type { Artifact } from '../../../artifact/types';
  import type { ArtifactDetail } from '../../../artifact/types';
  import type { EntityType } from '../../../relationship/types';
  import { ENTITY_TYPE_STYLES } from '../../../relationship/types';

  interface Props {
    entityId: string;
    entityType: string;
    projectId?: string;
    tabId?: string;
    refreshKey?: number;
    ondiagramsaved?: (diagram: DiagramDto) => void;
    class?: string;
  }

  let {
    entityId,
    entityType,
    projectId,
    tabId,
    refreshKey = 0,
    ondiagramsaved,
    class: className = '',
  }: Props = $props();

  // Loading state
  let loading = $state(false);
  let error = $state<string | null>(null);
  let prevEntityKey = $state<string | null>(null);
  let loadGeneration = 0;
  onDestroy(() => {
    ++loadGeneration;
  });

  // Entity-specific data
  let specData = $state<SpecDetail | null>(null);
  let specSignOff = $state<SignOffSummary | null>(null);

  let requirementData = $state<RequirementDetail | null>(null);

  let conversationData = $state<ConversationSummaryDto | null>(null);

  let glossaryData = $state<GlossaryTermDetail | null>(null);
  let glossaryUsage = $state<GlossaryTermUsage | null>(null);

  let artifactData = $state<Artifact | null>(null);

  let diagramData = $state<DiagramDto | null>(null);
  let diagramWorkbench: { confirmLeave: () => Promise<boolean> } | undefined = $state();
  export function confirmLeave(): Promise<boolean> {
    return diagramWorkbench?.confirmLeave?.() ?? Promise.resolve(true);
  }
  async function refreshDiagram() {
    if (await confirmLeave()) await loadEntity();
  }

  // Right panel resize state
  let rightPanelWidth = $state(300);
  const minRightPanelWidth = 200;
  const maxRightPanelWidth = 480;
  let isResizing = $state(false);
  let resizeRafId: number | null = null;

  function handleResizeStart(e: PointerEvent) {
    e.preventDefault();
    isResizing = true;
    const startX = e.clientX;
    const startWidth = rightPanelWidth;

    function onPointerMove(moveEvent: PointerEvent) {
      if (resizeRafId !== null) {
        cancelAnimationFrame(resizeRafId);
      }
      resizeRafId = requestAnimationFrame(() => {
        // Drag left = increase right panel width
        const delta = startX - moveEvent.clientX;
        rightPanelWidth = Math.min(
          maxRightPanelWidth,
          Math.max(minRightPanelWidth, startWidth + delta)
        );
        resizeRafId = null;
      });
    }

    function onPointerUp() {
      if (resizeRafId !== null) {
        cancelAnimationFrame(resizeRafId);
        resizeRafId = null;
      }
      isResizing = false;
      document.removeEventListener('pointermove', onPointerMove);
      document.removeEventListener('pointerup', onPointerUp);
    }

    document.addEventListener('pointermove', onPointerMove);
    document.addEventListener('pointerup', onPointerUp);
  }

  $effect(() => {
    if (!entityId) {
      ++loadGeneration;
      loading = false;
      resetState();
      prevEntityKey = null;
      return;
    }
    const key = JSON.stringify([
      getAuthState().user?.tenantId,
      projectId,
      entityType,
      entityId,
      refreshKey,
    ]);
    if (key === prevEntityKey) return;
    prevEntityKey = key;
    untrack(() => loadEntity());
  });

  function resetState(): void {
    error = null;
    specData = null;
    specSignOff = null;
    requirementData = null;
    conversationData = null;
    glossaryData = null;
    glossaryUsage = null;
    artifactData = null;
    diagramData = null;
  }

  async function loadEntity(): Promise<void> {
    const request = ++loadGeneration;
    const user = getAuthState().user;
    const tenant = user?.tenantId ?? '';
    const refresh = refreshKey;
    const id = entityId;
    const type = entityType;
    const pid = projectId ?? '';
    const isCurrent = () =>
      request === loadGeneration &&
      entityId === id &&
      entityType === type &&
      (projectId ?? '') === pid &&
      refreshKey === refresh &&
      (getAuthState().user?.tenantId ?? '') === tenant;
    loading = true;
    resetState();
    try {
      const auth = getAuthState();
      const tenantId = auth.user?.tenantId ?? '';
      switch (type) {
        case 'Spec': {
          const [spec, signOff] = await Promise.all([
            getSpecById(tenantId, pid, id),
            getSignOffSummary(tenantId, pid, id).catch(() => null),
          ]);
          if (!isCurrent()) return;
          specData = spec;
          specSignOff = signOff;
          break;
        }
        case 'Requirement': {
          const data = await getRequirementById(tenantId, pid, id);
          if (isCurrent()) requirementData = data;
          break;
        }
        case 'Conversation': {
          const data = await getConversationById(tenantId, id);
          if (isCurrent()) conversationData = data;
          break;
        }
        case 'GlossaryTerm': {
          const [term, usage] = await Promise.all([
            getTermById(tenantId, pid, id),
            getTermUsage(tenantId, pid, id).catch(() => null),
          ]);
          if (!isCurrent()) return;
          glossaryData = term;
          glossaryUsage = usage;
          break;
        }
        case 'Artifact': {
          const data = await getArtifactById(tenantId, pid, id);
          if (isCurrent()) artifactData = data;
          break;
        }
        case 'Diagram': {
          const data = await getDiagram(tenantId, pid, id);
          if (isCurrent()) diagramData = data;
          break;
        }
        default:
          error = `Unknown entity type: ${entityType}`;
      }
    } catch (err) {
      if (isCurrent()) {
        if (
          type === 'Diagram' &&
          (err instanceof AccessDeniedError ||
            (err instanceof ApiRequestError && err.status === 404))
        ) {
          clearDiagramDraft(
            diagramDraftKey(tabId, pid, id),
            diagramDraftScope(tenant, user?.id ?? '', pid, id)
          );
        }
        error = err instanceof Error ? err.message : 'Failed to load entity';
      }
    } finally {
      if (isCurrent()) loading = false;
    }
  }

  const artifactDetail = $derived<ArtifactDetail | null>(
    artifactData ? { ...artifactData, status: 'Valid' as const } : null
  );

  const entityStyle = $derived(ENTITY_TYPE_STYLES[entityType as EntityType]);

  const headerTitle = $derived.by(() => {
    if (entityType === 'Diagram' && diagramData) return diagramData.title;
    if (entityType === 'Spec' && specData) return specData.title;
    if (entityType === 'Requirement' && requirementData) return requirementData.title;
    if (entityType === 'Conversation' && conversationData) return conversationData.name;
    if (entityType === 'GlossaryTerm' && glossaryData) return glossaryData.term;
    if (entityType === 'Artifact' && artifactData)
      return artifactData.artifactPath?.split('/').pop() ?? artifactData.entityName ?? 'Artifact';
    return '';
  });

  const headerSubtitle = $derived.by(() => {
    if (entityType === 'Diagram' && diagramData) return `Revision ${diagramData.revision}`;
    if (entityType === 'Spec' && specData) return specData.code;
    if (entityType === 'Requirement' && requirementData) return requirementData.code;
    if (entityType === 'Conversation' && conversationData) return conversationData.conversationType;
    if (entityType === 'GlossaryTerm' && glossaryData) return glossaryData.category;
    if (entityType === 'Artifact' && artifactData) return artifactData.artifactType ?? 'Artifact';
    return '';
  });

  const hasData = $derived(
    diagramData !== null ||
      specData !== null ||
      requirementData !== null ||
      conversationData !== null ||
      glossaryData !== null ||
      artifactData !== null
  );
</script>

<div class="flex flex-col h-full {className}">
  {#if loading}
    <div class="flex-1 flex items-center justify-center">
      <Spinner size="lg" />
    </div>
  {:else if error}
    <div class="flex-1 flex flex-col items-center justify-center gap-3 px-6">
      <Icon name="alert-circle" size="lg" class="text-[var(--color-error-500)]" />
      <p class="text-sm text-[var(--color-text-secondary)] text-center">{error}</p>
    </div>
  {:else if hasData}
    <!-- Shared Header (full width) -->
    <DetailHeader>
      {#snippet leading()}
        {#if entityType === 'Requirement' && requirementData}
          <RequirementLevelBadge level={requirementData.level} />
        {:else if entityStyle}
          {#if entityType === 'Diagram'}
            <Icon name={entityStyle.icon} size="md" class={entityStyle.color} />
          {:else}
            <div class="flex items-center justify-center w-6 h-6 rounded {entityStyle.bgColor}">
              <Icon name={entityStyle.icon} size="sm" class={entityStyle.color} />
            </div>
          {/if}
        {/if}
      {/snippet}
      <h2 class="text-sm font-semibold text-[var(--color-text-primary)] truncate">{headerTitle}</h2>
      {#if headerSubtitle}
        <span class="text-xs text-[var(--color-text-tertiary)] truncate">{headerSubtitle}</span>
      {/if}
      {#snippet actions()}
        {#if entityType === 'Diagram'}<IconButton
            icon="refresh-cw"
            size="sm"
            variant="ghost"
            title="Refresh diagram"
            onclick={refreshDiagram}
          />{/if}
      {/snippet}
    </DetailHeader>

    <!-- 2-Column Body -->
    <div class="flex flex-1 min-h-0">
      <!-- Left: Detail Content (flex-1, flexible) -->
      <div class="flex-1 min-w-0 overflow-auto">
        {#if entityType === 'Diagram' && diagramData}
          <DiagramWorkbench
            bind:this={diagramWorkbench}
            diagram={diagramData}
            projectId={projectId ?? ''}
            tabId={tabId ?? ''}
            onsaved={(saved) => {
              diagramData = saved;
              ondiagramsaved?.(saved);
            }}
            onreaddenied={() => {
              diagramData = null;
              error = 'Access denied';
            }}
          />
        {:else if entityType === 'Spec' && specData}
          <SpecDetailView
            spec={specData}
            signOffSummary={specSignOff}
            onRequirementClick={(id) => navigateToEntity(projectId, 'requirement', id)}
            onConversationClick={(id) => navigateToEntity(projectId, 'conversation', id)}
            readonly={true}
            showHeader={false}
          />
        {:else if entityType === 'Requirement' && requirementData}
          <RequirementDetailView
            requirement={requirementData}
            readonly={true}
            showHeader={false}
            onParentSelect={(id) => navigateToEntity(projectId, 'requirement', id)}
            onChildSelect={(child) => navigateToEntity(projectId, 'requirement', child.id)}
            onConversationClick={(id) => navigateToEntity(projectId, 'conversation', id)}
          />
        {:else if entityType === 'Conversation' && conversationData}
          <ChannelView
            conversationId={conversationData.id}
            conversationName={conversationData.name}
            conversationDescription={conversationData.description ?? undefined}
            sourceType="conversation"
            {projectId}
            readonly={true}
            hideParticipantPanel={true}
            showHeader={false}
            {tabId}
          />
        {:else if entityType === 'GlossaryTerm' && glossaryData}
          <GlossaryDetail
            term={glossaryData}
            usage={glossaryUsage}
            onTermSelect={(id) => navigateToEntity(projectId, 'glossary', id)}
            onSpecSelect={(id) => navigateToEntity(projectId, 'spec', id)}
            onConversationSelect={(id) => navigateToEntity(projectId, 'conversation', id)}
            onRequirementSelect={(id) => navigateToEntity(projectId, 'requirement', id)}
            readonly={true}
            showHeader={false}
          />
        {:else if entityType === 'Artifact' && artifactDetail}
          <ArtifactContent
            artifact={artifactDetail}
            showHeader={false}
            onViewSpec={() =>
              artifactDetail?.specId && navigateToEntity(projectId, 'spec', artifactDetail.specId)}
          />
        {/if}
      </div>

      <!-- Resize Handle (z-10 wrapper to stay above content) -->
      <div class="relative z-10 flex-shrink-0">
        <ResizeHandle
          orientation="vertical"
          {isResizing}
          onpointerdown={handleResizeStart}
          ariaLabel="Resize trace graph panel"
        />
      </div>

      <!-- Right: TraceGraphSection (fixed width, resizable) -->
      <div
        class="flex-shrink-0 border-l border-[var(--color-border-primary)] overflow-hidden"
        class:right-panel-resizing={isResizing}
        style="width: {rightPanelWidth}px"
      >
        {#key JSON.stringify([projectId, entityType, entityId])}
          <TraceGraphSection
            entityType={entityType as EntityType}
            {entityId}
            projectId={projectId ?? ''}
            hidePrimaryFlow={entityType === 'Diagram'}
          />
        {/key}
      </div>
    </div>
  {:else}
    <div class="flex-1 flex flex-col items-center justify-center gap-3 px-6">
      <Icon
        name={entityStyle?.icon ?? 'file'}
        size="lg"
        class="text-[var(--color-text-quaternary)]"
      />
      <p class="text-sm text-[var(--color-text-tertiary)]">Select an entity to view details</p>
    </div>
  {/if}
</div>

<style>
  .right-panel-resizing {
    will-change: width;
    pointer-events: none;
    user-select: none;
  }
</style>
