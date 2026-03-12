<!-- Activity: Artifacts > Nav: Global (ACT-ART-001) -->
<script lang="ts">
  import { untrack } from 'svelte';
  import { getTabState, setTabState, toast } from '@sddp/shell';
  import ArtifactSidebar from '../sections/ArtifactSidebar.svelte';
  import ArtifactContent from '../sections/ArtifactContent.svelte';
  import ArtifactMetaPanel from '../sections/ArtifactMetaPanel.svelte';
  import {
    subscribeArtifact,
    getFilteredTypeGroups,
    getArtifactStatusCounts,
    removeArtifactFromGroups,
    setSelectedArtifact,
    setArtifactSearchQuery,
    setArtifactFilterType,
    toggleTypeExpanded,
  } from '../../stores';
  import { getArtifactById, deactivateArtifact } from '../../services/ArtifactService';
  import type {
    ArtifactTypeGroup,
    ArtifactFilterType,
    ArtifactDetail,
    ArtifactType,
  } from '../../types';

  interface Props {
    tenantId?: string;
    projectId?: string;
    tabId?: string;
    class?: string;
  }

  let { tenantId: _tenantId = '', projectId: _projectId = '', tabId = '', class: className = '' }: Props = $props();

  // State
  let typeGroups = $state<ArtifactTypeGroup[]>([]);
  let selectedArtifactId = $state<string | null>(null);
  let searchQuery = $state('');
  let filterType = $state<ArtifactFilterType>('all');
  let statusCounts = $state<Record<ArtifactFilterType, number>>({
    all: 0,
    valid: 0,
    modified: 0,
    missing: 0,
  });

  // Current artifact detail
  let currentArtifact = $state<ArtifactDetail | null>(null);
  let loadingArtifact = $state(false);

  // Tab State Persistence
  interface ArtifactsTabState {
    selectedArtifactId: string | null;
    searchQuery: string;
    filterType: ArtifactFilterType;
  }

  const tabStateKey = $derived(tabId || 'global-artifacts');
  let restoredKey = $state<string | null>(null);
  let isRestored = $state(false);

  $effect(() => {
    if (!tabStateKey || restoredKey === tabStateKey) return;
    const saved = getTabState<ArtifactsTabState>(tabStateKey);
    if (saved) {
      if (saved.searchQuery) setArtifactSearchQuery(saved.searchQuery);
      if (saved.filterType) setArtifactFilterType(saved.filterType);
      if (saved.selectedArtifactId) setSelectedArtifact(saved.selectedArtifactId);
    }
    restoredKey = tabStateKey;
    isRestored = true;
  });

  $effect(() => {
    if (!tabStateKey || !isRestored) return;
    setTabState<ArtifactsTabState>(tabStateKey, {
      selectedArtifactId,
      searchQuery,
      filterType,
    });
  });

  // Subscribe to store
  $effect(() => {
    const unsubscribe = subscribeArtifact((state) => {
      selectedArtifactId = state.selectedArtifactId;
      searchQuery = state.searchQuery;
      filterType = state.filterType;
    });
    return unsubscribe;
  });

  // Update filtered groups and counts
  $effect(() => {
    typeGroups = getFilteredTypeGroups();
    statusCounts = getArtifactStatusCounts();
  });

  // Load artifact detail when selected
  let prevSelectedArtifactId = $state<string | null>(null);
  $effect(() => {
    if (!selectedArtifactId) {
      prevSelectedArtifactId = null;
      currentArtifact = null;
      return;
    }
    if (selectedArtifactId === prevSelectedArtifactId) return;
    prevSelectedArtifactId = selectedArtifactId;
    untrack(() => loadArtifactDetail(selectedArtifactId!));
  });

  // Load artifact detail from API
  async function loadArtifactDetail(artifactId: string) {
    if (!_tenantId || !_projectId) return;

    loadingArtifact = true;
    try {
      const artifact = await getArtifactById(_tenantId, _projectId, artifactId);
      currentArtifact = {
        ...artifact,
        status: 'Valid',
      };
    } catch (err) {
      console.error('Failed to load artifact detail:', err);
      currentArtifact = null;
    } finally {
      loadingArtifact = false;
    }
  }

  // Handlers
  function handleSearch(query: string) {
    setArtifactSearchQuery(query);
  }

  function handleFilterChange(filter: ArtifactFilterType) {
    setArtifactFilterType(filter);
  }

  function handleToggleType(type: string) {
    toggleTypeExpanded(type as ArtifactType);
  }

  function handleSelectArtifact(artifactId: string) {
    setSelectedArtifact(artifactId);
  }

  function handleRegenerate() {
    console.log('Regenerate artifact:', selectedArtifactId);
  }

  function handleViewSource() {
    console.log('View source:', currentArtifact?.artifactPath);
  }

  function handleVerify() {
    console.log('Verify artifact:', selectedArtifactId);
  }

  function handleVerifyAll() {
    console.log('Verify all artifacts');
  }

  function handleViewSpec() {
    console.log('View spec:', currentArtifact?.specId);
  }

  async function handleDeactivateArtifact() {
    if (!currentArtifact || !_tenantId || !_projectId) return;
    if (!confirm('Are you sure you want to deactivate this artifact?')) return;

    try {
      await deactivateArtifact(_tenantId, _projectId, currentArtifact.id);
      removeArtifactFromGroups(currentArtifact.id);
      setSelectedArtifact(null);
      currentArtifact = null;
      typeGroups = getFilteredTypeGroups();
      statusCounts = getArtifactStatusCounts();
      toast.success('Artifact deactivated');
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Failed to deactivate artifact');
    }
  }
</script>

<div class="flex h-full {className}">
  <!-- Sidebar -->
  <div class="w-64 flex-shrink-0 border-r border-gray-200 dark:border-gray-700">
    <ArtifactSidebar
      {typeGroups}
      {selectedArtifactId}
      {searchQuery}
      {filterType}
      {statusCounts}
      onSearch={handleSearch}
      onFilterChange={handleFilterChange}
      onToggleType={handleToggleType}
      onSelectArtifact={handleSelectArtifact}
      onVerifyAll={handleVerifyAll}
    />
  </div>

  <!-- Main Content -->
  <div class="flex-1 min-w-0">
    <ArtifactContent
      artifact={currentArtifact}
      loading={loadingArtifact}
      onRegenerate={handleRegenerate}
      onViewSource={handleViewSource}
      onVerify={handleVerify}
      onViewSpec={handleViewSpec}
      onDeactivate={handleDeactivateArtifact}
    />
  </div>

  <!-- Meta Panel -->
  <div class="w-64 flex-shrink-0">
    <ArtifactMetaPanel
      artifact={currentArtifact}
      {statusCounts}
    />
  </div>
</div>
