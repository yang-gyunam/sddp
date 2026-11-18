<script lang="ts">
  import RelationshipLinkSearch from '../idioms/RelationshipLinkSearch.svelte';
  import { getAuthState } from '@sddp/shell/auth';
  import { searchEntities, createRelationship } from '../../services/RelationshipService';
  import type { EntitySearchResult } from '../../services/RelationshipService';
  import type { EntityType, RelationType } from '../../types';
  import { getAvailableRelationTypes } from '../../types';

  interface Props {
    entityType: EntityType;
    entityId: string;
    projectId: string;
    onLinked?: () => void;
    onClose?: () => void;
  }

  let { entityType, entityId, projectId, onLinked, onClose }: Props = $props();

  let searchQuery = $state('');
  let selectedType = $state<EntityType | 'All'>('All');
  let results = $state<EntitySearchResult[]>([]);
  let searching = $state(false);
  let selectedResult = $state<EntitySearchResult | null>(null);
  let linking = $state(false);

  function handleSearch(query: string) {
    searchQuery = query;
    selectedResult = null;
    performSearch();
  }

  function handleFilterChange(type: EntityType | 'All') {
    selectedType = type;
    selectedResult = null;
    if (searchQuery.trim()) {
      performSearch();
    }
  }

  async function performSearch() {
    if (!searchQuery.trim()) {
      results = [];
      return;
    }

    searching = true;
    try {
      const tenantId = getAuthState().user?.tenantId ?? '';
      results = await searchEntities(tenantId, projectId, searchQuery, selectedType, 15);
      // Exclude the current entity from results
      results = results.filter((r) => r.id !== entityId);
    } catch {
      results = [];
    } finally {
      searching = false;
    }
  }

  function handleResultClick(result: EntitySearchResult) {
    if (selectedResult?.id === result.id) {
      selectedResult = null;
    } else {
      selectedResult = result;
    }
  }

  async function handleLinkAs(relationType: RelationType) {
    if (!selectedResult) return;

    linking = true;
    try {
      const tenantId = getAuthState().user?.tenantId ?? '';
      await createRelationship(tenantId, projectId, {
        fromEntityType: entityType,
        fromEntityId: entityId,
        toEntityType: selectedResult.entityType,
        toEntityId: selectedResult.id,
        type: relationType,
      });
      searchQuery = '';
      results = [];
      selectedResult = null;
      onLinked?.();
    } catch {
      // Error handling via global error handler
    } finally {
      linking = false;
    }
  }

  function getRelationTypes(): RelationType[] {
    if (!selectedResult) return [];
    return getAvailableRelationTypes(entityType, selectedResult.entityType);
  }
</script>

<RelationshipLinkSearch
  {searchQuery}
  {selectedType}
  {results}
  {searching}
  {selectedResult}
  {linking}
  relationTypes={getRelationTypes()}
  onSearch={handleSearch}
  onTypeChange={handleFilterChange}
  onResultClick={handleResultClick}
  onLinkAs={handleLinkAs}
  {onClose}
/>
