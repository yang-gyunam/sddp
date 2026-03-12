<!-- Section: RelationshipPanel -->
<script lang="ts">
  import type {
    EntityType,
    RelationshipListData,
    RelationshipGraphData,
    SpecDiffResult,
    GraphNode,
    GraphEdge,
    RelationType,
    CreateRelationshipRequest,
  } from '../../types';
  import { getAvailableRelationTypes } from '../../types';
  import RelationshipList from './RelationshipList.svelte';
  import RelationshipGraph from './RelationshipGraph.svelte';
  import VersionTimeline from './VersionTimeline.svelte';
  import DiffViewer from './DiffViewer.svelte';
  import EntityTypeBadge from '../idioms/EntityTypeBadge.svelte';
  import type { Spec } from '../../../specs/types';
  import { Icon, Button, Combobox, Input, Select } from '@sddp/ui';
  import type { ComboboxOption } from '@sddp/ui';

  interface Props {
    entityType: EntityType;
    entityId: string;
    entityLabel?: string;
    relationships?: RelationshipListData | null;
    graph?: RelationshipGraphData | null;
    versions?: Spec[];
    diff?: SpecDiffResult | null;
    loading?: boolean;
    error?: string | null;
    graphDepth?: number;
    onLoadRelationships?: () => void;
    onLoadGraph?: (depth: number) => void;
    onLoadVersions?: () => void;
    onCompareVersions?: (specId1: string, specId2: string) => void;
    onCreateRelationship?: (request: CreateRelationshipRequest) => void;
    onInvalidateRelationship?: (relationshipId: string) => void;
    onNodeClick?: (node: GraphNode) => void;
    onEdgeClick?: (edge: GraphEdge) => void;
    onVersionSelect?: (version: Spec) => void;
    /** Candidate options for target entity search */
    entityCandidates?: ComboboxOption[];
    /** Called when user types in entity search */
    onSearchEntity?: (entityType: EntityType, query: string) => void;
    /** Loading state for entity search */
    entitySearchLoading?: boolean;
    class?: string;
  }

  let {
    entityType,
    entityId,
    entityLabel = '',
    relationships = null,
    graph = null,
    versions = [],
    diff = null,
    loading = false,
    error = null,
    graphDepth = 3,
    onLoadRelationships,
    onLoadGraph,
    onLoadVersions,
    onCompareVersions,
    onCreateRelationship,
    onInvalidateRelationship,
    onNodeClick,
    onEdgeClick,
    onVersionSelect,
    entityCandidates = [],
    onSearchEntity,
    entitySearchLoading = false,
    class: className = '',
  }: Props = $props();

  const formId = `relationship-panel-${Math.random().toString(36).substring(2, 9)}`;
  const targetTypeId = `${formId}-target-type`;
  const targetIdId = `${formId}-target-id`;
  const relationshipTypeId = `${formId}-relationship-type`;
  const relationshipReasonId = `${formId}-relationship-reason`;

  type TabId = 'list' | 'graph' | 'timeline' | 'diff';
  let activeTab: TabId = $state('list');

  // Create relationship form state
  let showCreateForm = $state(false);
  let createFormData = $state({
    toEntityType: 'Spec' as EntityType,
    toEntityId: '',
    type: 'DependsOn' as RelationType,
    reason: '',
  });

  function handleTabChange(tab: TabId) {
    activeTab = tab;

    // Load data if needed
    if (tab === 'list' && !relationships) {
      onLoadRelationships?.();
    } else if (tab === 'graph' && !graph) {
      onLoadGraph?.(graphDepth);
    } else if (tab === 'timeline' && versions.length === 0 && entityType === 'Spec') {
      onLoadVersions?.();
    }
  }

  function handleCompare(version1: Spec, version2: Spec) {
    onCompareVersions?.(version1.id, version2.id);
    activeTab = 'diff';
  }

  function handleCreateRelationship() {
    if (!createFormData.toEntityId.trim()) return;

    onCreateRelationship?.({
      fromEntityType: entityType,
      fromEntityId: entityId,
      toEntityType: createFormData.toEntityType,
      toEntityId: createFormData.toEntityId.trim(),
      type: createFormData.type,
      reason: createFormData.reason.trim() || undefined,
    });

    // Reset form
    showCreateForm = false;
    createFormData = {
      toEntityType: 'Spec',
      toEntityId: '',
      type: 'DependsOn',
      reason: '',
    };
  }

  const availableTypes = $derived(getAvailableRelationTypes(entityType, createFormData.toEntityType));
</script>

<div class="flex flex-col h-full {className}">
  <!-- Header -->
  <div class="flex items-center justify-between px-4 py-3 border-b border-gray-200 dark:border-gray-700">
    <div class="flex items-center gap-2">
      <EntityTypeBadge type={entityType} size="sm" />
      <span class="text-sm font-medium text-gray-700 dark:text-gray-300">
        {entityLabel || entityId.substring(0, 8)}
      </span>
    </div>

    <Button
      variant="unstyled"
      class="flex items-center gap-1 px-2 py-1 text-sm text-blue-600 hover:text-blue-700 dark:text-blue-400 dark:hover:text-blue-300"
      onclick={() => (showCreateForm = !showCreateForm)}
    >
      <Icon name={showCreateForm ? 'x' : 'plus'} size="sm" />
      {showCreateForm ? 'Cancel' : 'Add Relationship'}
    </Button>
  </div>

  <!-- Create relationship form -->
  {#if showCreateForm}
    <div class="p-4 bg-gray-50 dark:bg-gray-900 border-b border-gray-200 dark:border-gray-700">
      <div class="grid grid-cols-2 gap-4">
        <div>
          <label for={targetTypeId} class="block text-xs font-medium text-gray-600 dark:text-gray-400 mb-1">
            Target Entity Type
          </label>
          <Select
            unstyled
            id={targetTypeId}
            bind:value={createFormData.toEntityType}
            class="w-full px-3 py-2 text-sm border border-gray-200 dark:border-gray-700 rounded-lg bg-white dark:bg-gray-800 text-gray-700 dark:text-gray-300"
          >
            <option value="Spec">Spec</option>
            <option value="Requirement">Requirement</option>
            <option value="Conversation">Conversation</option>
            <option value="GlossaryTerm">Glossary Term</option>
          </Select>
        </div>
        <div>
          <Combobox
            id={targetIdId}
            label="Target Entity"
            options={entityCandidates}
            bind:value={createFormData.toEntityId}
            placeholder="Search by code or title..."
            hint="Search for the target {createFormData.toEntityType.toLowerCase()}"
            onsearch={(q) => onSearchEntity?.(createFormData.toEntityType, q)}
            loading={entitySearchLoading}
            required
          />
        </div>
        <div>
          <label for={relationshipTypeId} class="block text-xs font-medium text-gray-600 dark:text-gray-400 mb-1">
            Relationship Type
          </label>
          <Select
            unstyled
            id={relationshipTypeId}
            bind:value={createFormData.type}
            class="w-full px-3 py-2 text-sm border border-gray-200 dark:border-gray-700 rounded-lg bg-white dark:bg-gray-800 text-gray-700 dark:text-gray-300"
          >
            {#each availableTypes as type (type)}
              <option value={type}>{type}</option>
            {/each}
          </Select>
        </div>
        <div>
          <label for={relationshipReasonId} class="block text-xs font-medium text-gray-600 dark:text-gray-400 mb-1">
            Reason (optional)
          </label>
          <Input
            unstyled
            id={relationshipReasonId}
            bind:value={createFormData.reason}
            placeholder="Why this relationship?"
            class="w-full px-3 py-2 text-sm border border-gray-200 dark:border-gray-700 rounded-lg bg-white dark:bg-gray-800 text-gray-700 dark:text-gray-300"
          />
        </div>
      </div>
      <div class="mt-3 flex justify-end">
        <Button variant="primary" size="sm" disabled={!createFormData.toEntityId.trim()} onclick={handleCreateRelationship}>
          Create Relationship
        </Button>
      </div>
    </div>
  {/if}

  <!-- Tabs -->
  <div class="flex border-b border-gray-200 dark:border-gray-700">
    <Button
      variant="unstyled"
      class="px-4 py-2 text-sm font-medium border-b-2 -mb-px transition-colors
        {activeTab === 'list'
          ? 'border-blue-500 text-blue-600 dark:text-blue-400'
          : 'border-transparent text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-300'}"
      onclick={() => handleTabChange('list')}
    >
      <Icon name="list" size="sm" class="inline mr-1" />
      List
    </Button>
    <Button
      variant="unstyled"
      class="px-4 py-2 text-sm font-medium border-b-2 -mb-px transition-colors
        {activeTab === 'graph'
          ? 'border-blue-500 text-blue-600 dark:text-blue-400'
          : 'border-transparent text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-300'}"
      onclick={() => handleTabChange('graph')}
    >
      <Icon name="git-branch" size="sm" class="inline mr-1" />
      Graph
    </Button>
    {#if entityType === 'Spec'}
      <Button
        variant="unstyled"
        class="px-4 py-2 text-sm font-medium border-b-2 -mb-px transition-colors
          {activeTab === 'timeline'
            ? 'border-blue-500 text-blue-600 dark:text-blue-400'
            : 'border-transparent text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-300'}"
        onclick={() => handleTabChange('timeline')}
      >
        <Icon name="clock" size="sm" class="inline mr-1" />
        Timeline
      </Button>
      <Button
        variant="unstyled"
        class="px-4 py-2 text-sm font-medium border-b-2 -mb-px transition-colors
          {activeTab === 'diff'
            ? 'border-blue-500 text-blue-600 dark:text-blue-400'
            : 'border-transparent text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-300'}"
        onclick={() => handleTabChange('diff')}
      >
        <Icon name="git-compare" size="sm" class="inline mr-1" />
        Diff
      </Button>
    {/if}
  </div>

  <!-- Content -->
  <div class="flex-1 overflow-auto p-4">
    {#if loading}
      <div class="flex items-center justify-center h-full">
        <div class="text-center text-gray-500 dark:text-gray-400">
          <div class="animate-spin w-8 h-8 border-2 border-blue-500 border-t-transparent rounded-full mx-auto mb-2"></div>
          <p class="text-sm">Loading...</p>
        </div>
      </div>
    {:else if error}
      <div class="flex items-center justify-center h-full">
        <div class="text-center text-red-500 dark:text-red-400">
          <Icon name="alert-circle" size="xl" class="mx-auto mb-2" />
          <p class="text-sm">{error}</p>
        </div>
      </div>
    {:else if activeTab === 'list'}
      {#if relationships}
        <RelationshipList
          {relationships}
          onInvalidate={onInvalidateRelationship}
        />
      {:else}
        <div class="text-center py-8 text-gray-500 dark:text-gray-400">
          <Icon name="link-2" size="xl" class="mx-auto mb-2 opacity-50" />
          <p>Click to load relationships</p>
          <Button variant="ghost" size="sm" class="mt-2" onclick={() => onLoadRelationships?.()}>
            Load Relationships
          </Button>
        </div>
      {/if}
    {:else if activeTab === 'graph'}
      {#if graph}
        <RelationshipGraph
          {graph}
          width={560}
          height={400}
          showControls={false}
          {onNodeClick}
          {onEdgeClick}
        />
      {:else}
        <div class="text-center py-8 text-gray-500 dark:text-gray-400">
          <Icon name="git-branch" size="xl" class="mx-auto mb-2 opacity-50" />
          <p>Click to load relationship graph</p>
          <Button variant="ghost" size="sm" class="mt-2" onclick={() => onLoadGraph?.(graphDepth)}>
            Load Graph
          </Button>
        </div>
      {/if}
    {:else if activeTab === 'timeline'}
      <VersionTimeline
        {versions}
        currentVersionId={entityId}
        onVersionSelect={onVersionSelect}
        onCompare={handleCompare}
      />
    {:else if activeTab === 'diff'}
      {#if diff}
        <DiffViewer {diff} />
      {:else}
        <div class="text-center py-8 text-gray-500 dark:text-gray-400">
          <Icon name="git-compare" size="xl" class="mx-auto mb-2 opacity-50" />
          <p>Select two versions from the Timeline tab to compare</p>
        </div>
      {/if}
    {/if}
  </div>
</div>
