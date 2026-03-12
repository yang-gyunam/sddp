<!-- Section: TraceGraphSection — Projects > Specs/Requirements/Artifacts/Conversations -->
<script lang="ts">
  import { untrack } from 'svelte';
  import { Icon, Button, IconButton, Checkbox, Spinner } from '@sddp/ui';
  import { CollapsibleGroup } from '@sddp/shell';
  import { getAuthState } from '@sddp/shell/auth';
  import {
    navigateToSpec,
    navigateToRequirement,
    navigateToConversation,
    navigateToGlossary,
    navigateToArtifact,
  } from '@sddp/shell/core';
  import {
    getRelationshipsByEntity,
    getPrimaryFlow,
    invalidateRelationship,
  } from '../../services/RelationshipService';
  import RelationshipLinkSearch from '../idioms/RelationshipLinkSearch.svelte';
  import EntityFullDetailTab from '../../../shared/components/sections/EntityFullDetailTab.svelte';
  import type {
    EntityType,
    PrimaryFlowNode,
    PrimaryFlowNodeDto,
    RelationshipListData,
  } from '../../types';
  import {
    ENTITY_TYPE_STYLES,
    RELATION_TYPE_STYLES,
    getInverseRelationType,
  } from '../../types';

  import type { Snippet } from 'svelte';

  interface Props {
    entityType: EntityType;
    entityId: string;
    entityCode?: string;
    projectId: string;
    primaryFlow?: PrimaryFlowNode[];
    hidePrimaryFlow?: boolean;
    hideFilter?: boolean;
    onNodeClick?: (node: PrimaryFlowNode) => void;
    revisionHistory?: Snippet;
    class?: string;
  }

  let {
    entityType,
    entityId,
    entityCode: _entityCode,
    projectId,
    primaryFlow = [],
    hidePrimaryFlow = false,
    hideFilter = false,
    onNodeClick,
    revisionHistory,
    class: className = '',
  }: Props = $props();

  // Self-loading state for relationships + primary flow
  let relationships = $state<RelationshipListData | null>(null);
  let selfLoadedPrimaryFlow = $state<PrimaryFlowNode[]>([]);
  let loading = $state(false);
  let error = $state<string | null>(null);
  let prevEntityId = $state<string | null>(null);


  // Use self-loaded data; fall back to prop if provided
  const resolvedPrimaryFlow = $derived(
    selfLoadedPrimaryFlow.length > 0 ? selfLoadedPrimaryFlow : primaryFlow
  );

  $effect(() => {
    if (!entityId) {
      relationships = null;
      selfLoadedPrimaryFlow = [];
      error = null;
      prevEntityId = null;
      return;
    }
    if (entityId === prevEntityId) return;
    prevEntityId = entityId;
    untrack(() => loadData());
  });

  async function loadData() {
    if (!projectId) {
      // Personal tasks (no project) have no trace graph data
      relationships = null;
      selfLoadedPrimaryFlow = [];
      return;
    }
    loading = true;
    error = null;
    try {
      const tenantId = getAuthState().user?.tenantId ?? '';
      const [relResult, flowResult] = await Promise.allSettled([
        getRelationshipsByEntity(tenantId, projectId, entityType, entityId),
        getPrimaryFlow(tenantId, projectId, entityType, entityId),
      ]);
      relationships = relResult.status === 'fulfilled' ? relResult.value : null;
      selfLoadedPrimaryFlow = flowResult.status === 'fulfilled' ? flowResult.value.map(dtoToNode) : [];
      const failures = [relResult, flowResult].filter((r) => r.status === 'rejected');
      if (failures.length > 0) {
        error = failures.length === 2
          ? 'Failed to load trace data'
          : 'Some trace data failed to load';
      }
    } catch (err) {
      error = err instanceof Error ? err.message : 'Failed to load trace data';
      relationships = null;
      selfLoadedPrimaryFlow = [];
    } finally {
      loading = false;
    }
  }

  function dtoToNode(dto: PrimaryFlowNodeDto): PrimaryFlowNode {
    return {
      id: dto.id,
      entityType: dto.entityType as PrimaryFlowNode['entityType'],
      label: dto.label,
      code: dto.code ?? undefined,
      depth: dto.depth,
      isCurrent: dto.isCurrent,
    };
  }

  // Local state — each TraceGraphSection instance is independent
  let showPrimaryFlowOverride = $state<boolean | null>(null);
  const showPrimaryFlow = $derived(showPrimaryFlowOverride ?? !hidePrimaryFlow);
  let showRelationships = $state(true);
  let showDeprecated = $state(false);
  let isPrimaryFlowExpanded = $state(true);
  let isRelationshipsExpanded = $state(true);
  let isRevisionHistoryExpanded = $state(true);
  let isFilterExpanded = $state(false);
  let showLinkSearch = $state(false);

  // Combine incoming and outgoing relationships
  const allRelationships = $derived(
    relationships
      ? [
          ...relationships.incoming.map((r) => ({ ...r, direction: 'incoming' as const })),
          ...relationships.outgoing.map((r) => ({ ...r, direction: 'outgoing' as const })),
        ]
      : []
  );

  // Filter deprecated if needed
  const filteredRelationships = $derived(
    showDeprecated
      ? allRelationships
      : allRelationships.filter((r) => r.validTo === null)
  );

  function navigateByEntityType(type: string, id: string, label?: string): void {
    const detailProps = { entityId: id, entityType: type, projectId };
    switch (type) {
      case 'Spec':
        navigateToSpec(id, label, EntityFullDetailTab, detailProps);
        break;
      case 'Requirement':
        navigateToRequirement(id, label, EntityFullDetailTab, detailProps);
        break;
      case 'Conversation':
        navigateToConversation(id, label, EntityFullDetailTab, detailProps);
        break;
      case 'GlossaryTerm':
        navigateToGlossary(id, label, EntityFullDetailTab, detailProps);
        break;
      case 'Artifact':
        navigateToArtifact(id, label, EntityFullDetailTab, detailProps);
        break;
      case 'Task':
        // Task navigation is handled by onNodeClick in the parent component
        break;
    }
  }

  function handleNodeClick(node: PrimaryFlowNode): void {
    const nodeId = node.id ?? node.entityId;
    if (node.isCurrent || !nodeId) return;
    if (node.entityType === entityType) {
      onNodeClick?.(node);
    } else {
      navigateByEntityType(node.entityType, nodeId, node.label);
    }
  }

  function handleRelationshipItemClick(targetType: EntityType, targetId: string, targetLabel?: string): void {
    navigateByEntityType(targetType, targetId, targetLabel);
  }

  async function handleLinkCreated(): Promise<void> {
    showLinkSearch = false;
    await loadData();
  }

  async function handleUnlink(relationshipId: string): Promise<void> {
    const tenantId = getAuthState().user?.tenantId ?? '';
    await invalidateRelationship(tenantId, projectId, relationshipId);
    await loadData();
  }
</script>

<div class="flex flex-col h-full bg-[var(--color-bg-secondary)] {className}">
  <div class="relative flex-1 overflow-y-auto">
    <!-- Loading overlay (does NOT destroy content below) -->
    {#if loading}
      <div class="absolute inset-0 flex items-center justify-center bg-[var(--color-bg-secondary)]/80 z-10">
        <Spinner size="lg" />
      </div>
    {/if}

    <!-- Error banner (does NOT destroy content below) -->
    {#if error}
      <div class="flex-shrink-0 flex items-center gap-2 px-4 py-2 text-[var(--color-error-500)] bg-[var(--color-error-500)]/10 border-b border-[var(--color-error-500)]/20">
        <Icon name="alert-circle" size="sm" />
        <p class="text-xs">{error}</p>
      </div>
    {/if}

    <!-- Primary Flow Section -->
    {#if showPrimaryFlow}
      <div>
        <CollapsibleGroup
          title="Primary Flow"
          expanded={isPrimaryFlowExpanded}
          onToggle={() => isPrimaryFlowExpanded = !isPrimaryFlowExpanded}
        >
          <div class="pl-2 pr-3 pt-2 pb-3">
            {#if resolvedPrimaryFlow.length === 0}
              <div class="text-center py-4">
                <p class="text-sm text-[var(--color-text-tertiary)]">No primary flow data</p>
              </div>
            {:else}
              <div class="space-y-0.5">
                {#each resolvedPrimaryFlow as node, i (node.id ?? node.entityId ?? `node-${i}`)}
                  {@const style = ENTITY_TYPE_STYLES[node.entityType as EntityType]}
                  {@const depth = node.depth ?? 0}
                  {@const isIndented = depth >= 1}

                  <div style={isIndented ? `padding-left: ${depth * 1.5}rem` : ''}>
                    <Button
                      variant="unstyled"
                      class="w-full flex items-center gap-2 p-1.5 rounded-lg transition-colors
                        {node.isCurrent
                          ? 'bg-[var(--color-accent-primary)]/10 border border-[var(--color-accent-primary)]/30'
                          : 'hover:bg-[var(--color-bg-tertiary)]'}"
                      onclick={() => handleNodeClick(node)}
                    >
                      <div class="flex items-center justify-center w-5 h-5 rounded flex-shrink-0 {style?.bgColor || 'bg-[var(--color-bg-tertiary)]'}">
                        <Icon name={style?.icon || 'file'} size="xs" class={style?.color || 'text-[var(--color-text-tertiary)]'} />
                      </div>
                      <div class="flex-1 min-w-0 text-left">
                        <div class="text-sm font-medium text-[var(--color-text-primary)] truncate">
                          {node.label}
                        </div>
                        {#if node.code}
                          <div class="text-xs text-[var(--color-text-tertiary)] truncate">
                            {node.code}
                          </div>
                        {/if}
                      </div>
                      {#if node.isCurrent}
                        <span class="text-xs text-[var(--color-accent-primary)] font-medium flex-shrink-0">Current</span>
                      {/if}
                    </Button>
                  </div>
                {/each}
              </div>
            {/if}
          </div>
        </CollapsibleGroup>
      </div>
    {/if}

    <!-- Related Items Section (user-linked lateral relationships) -->
    {#if showRelationships}
      <div>
        <CollapsibleGroup
          title="Related Items"
          expanded={isRelationshipsExpanded}
          onToggle={() => isRelationshipsExpanded = !isRelationshipsExpanded}
          actions={[{
            id: 'add-relationship',
            icon: 'plus',
            label: 'Link entity',
            onClick: () => showLinkSearch = !showLinkSearch,
          }]}
        >
          <div class="px-4 pt-2 pb-3">
            {#if showLinkSearch}
              <div class="mb-3">
                <RelationshipLinkSearch
                  {entityType}
                  {entityId}
                  {projectId}
                  onLinked={handleLinkCreated}
                  onClose={() => showLinkSearch = false}
                />
              </div>
            {/if}

            {#if !relationships || filteredRelationships.length === 0}
              {#if !showLinkSearch}
                <p class="text-sm text-[var(--color-text-tertiary)] text-center py-4">
                  No related items
                </p>
              {/if}
            {:else}
              <div class="space-y-0.5">
                {#each filteredRelationships as rel (rel.id)}
                  {@const relStyle = RELATION_TYPE_STYLES[rel.type]}
                  {@const targetType = rel.direction === 'incoming' ? rel.fromEntityType : rel.toEntityType}
                  {@const targetId = rel.direction === 'incoming' ? rel.fromEntityId : rel.toEntityId}
                  {@const targetLabel = rel.direction === 'incoming' ? rel.fromEntityLabel : rel.toEntityLabel}
                  {@const targetCode = rel.direction === 'incoming' ? rel.fromEntityCode : rel.toEntityCode}
                  {@const entityStyle = ENTITY_TYPE_STYLES[targetType]}
                  {@const relationLabel = rel.direction === 'incoming' ? getInverseRelationType(rel.type) : relStyle.label}
                  <div class="group flex items-center gap-0.5">
                    <Button
                      variant="unstyled"
                      class="flex-1 min-w-0 flex items-center gap-2 p-1.5 rounded-lg hover:bg-[var(--color-bg-tertiary)] transition-colors
                        {rel.validTo ? 'opacity-50' : ''}"
                      onclick={() => handleRelationshipItemClick(targetType, targetId, targetLabel ?? undefined)}
                    >
                      <div
                        class="flex items-center justify-center w-5 h-5 rounded flex-shrink-0 {entityStyle?.bgColor ?? 'bg-[var(--color-bg-tertiary)]'}"
                        title={relationLabel}
                      >
                        <Icon name={entityStyle?.icon ?? 'file'} size="xs" class={entityStyle?.color ?? 'text-[var(--color-text-tertiary)]'} />
                      </div>
                      <div class="flex-1 min-w-0 text-left">
                        <div class="text-sm font-medium text-[var(--color-text-primary)] truncate">
                          {targetLabel || `${entityStyle?.label ?? targetType}`}
                        </div>
                        {#if targetCode}
                          <div class="text-xs text-[var(--color-text-tertiary)] truncate">
                            {targetCode}
                          </div>
                        {/if}
                      </div>
                    </Button>
                    <IconButton
                      icon="x"
                      variant="ghost"
                      size="sm"
                      class="flex-shrink-0 opacity-0 group-hover:opacity-100 transition-all"
                      onclick={(e) => { e.stopPropagation(); handleUnlink(rel.id); }}
                      title="Remove link"
                    />
                  </div>
                {/each}
              </div>
            {/if}
          </div>
        </CollapsibleGroup>
      </div>
    {/if}

    <!-- Revision History Section (injected via snippet) -->
    {#if revisionHistory}
      <div>
        <CollapsibleGroup
          title="Revision History"
          expanded={isRevisionHistoryExpanded}
          onToggle={() => isRevisionHistoryExpanded = !isRevisionHistoryExpanded}
        >
          {@render revisionHistory()}
        </CollapsibleGroup>
      </div>
    {/if}

    <!-- Filter Section -->
    {#if !hideFilter}
    <div>
      <CollapsibleGroup
        title="Filter"
        expanded={isFilterExpanded}
        onToggle={() => isFilterExpanded = !isFilterExpanded}
      >
        <div class="px-4 pt-2 pb-3 space-y-2">
          <label class="flex items-center gap-2 cursor-pointer">
            <Checkbox
              checked={showPrimaryFlow}
              onchange={() => showPrimaryFlowOverride = !showPrimaryFlow}
            />
            <span class="text-sm text-[var(--color-text-secondary)]">Show Primary Flow</span>
          </label>
          <label class="flex items-center gap-2 cursor-pointer">
            <Checkbox
              checked={showRelationships}
              onchange={() => showRelationships = !showRelationships}
            />
            <span class="text-sm text-[var(--color-text-secondary)]">Show Relationships</span>
          </label>
          <label class="flex items-center gap-2 cursor-pointer">
            <Checkbox
              checked={showDeprecated}
              onchange={() => showDeprecated = !showDeprecated}
            />
            <span class="text-sm text-[var(--color-text-secondary)]">Show Deprecated</span>
          </label>
        </div>
      </CollapsibleGroup>
    </div>
    {/if}
  </div>
</div>

