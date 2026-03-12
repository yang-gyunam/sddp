<!-- Section: RelationshipList -->
<script lang="ts">
  import type { RelationshipListData, Relationship } from '../../types';
  import { getInverseRelationType, isRelationshipValid } from '../../types';
  import RelationshipBadge from '../idioms/RelationshipBadge.svelte';
  import EntityTypeBadge from '../idioms/EntityTypeBadge.svelte';
  import { Icon, Button, IconButton } from '@sddp/ui';
  import { formatDate as formatDateUtil } from '@sddp/shell';

  interface Props {
    relationships: RelationshipListData;
    showIncoming?: boolean;
    showOutgoing?: boolean;
    onSelect?: (relationship: Relationship) => void;
    onInvalidate?: (relationshipId: string) => void;
    class?: string;
  }

  let {
    relationships,
    showIncoming = true,
    showOutgoing = true,
    onSelect,
    onInvalidate,
    class: className = '',
  }: Props = $props();

  let activeTab: 'outgoing' | 'incoming' = $state('outgoing');

  const displayedRelationships = $derived(
    activeTab === 'outgoing' ? relationships.outgoing : relationships.incoming
  );

  function handleSelect(relationship: Relationship) {
    onSelect?.(relationship);
  }

  function handleInvalidate(e: Event, relationshipId: string) {
    e.stopPropagation();
    onInvalidate?.(relationshipId);
  }

  function formatDateStr(dateStr: string): string {
    return formatDateUtil(dateStr, { month: 'short' });
  }
</script>

<div class="flex flex-col {className}">
  <!-- Tabs -->
  <div class="flex border-b border-gray-200 dark:border-gray-700 mb-4">
    {#if showOutgoing}
      <Button
        variant="unstyled"
        class="px-4 py-2 text-sm font-medium border-b-2 -mb-px transition-colors
          {activeTab === 'outgoing'
            ? 'border-blue-500 text-blue-600 dark:text-blue-400'
            : 'border-transparent text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-300'}"
        onclick={() => (activeTab = 'outgoing')}
      >
        Outgoing ({relationships.outgoing.length})
      </Button>
    {/if}
    {#if showIncoming}
      <Button
        variant="unstyled"
        class="px-4 py-2 text-sm font-medium border-b-2 -mb-px transition-colors
          {activeTab === 'incoming'
            ? 'border-blue-500 text-blue-600 dark:text-blue-400'
            : 'border-transparent text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-300'}"
        onclick={() => (activeTab = 'incoming')}
      >
        Incoming ({relationships.incoming.length})
      </Button>
    {/if}
  </div>

  <!-- List -->
  <div class="flex-1 overflow-auto">
    {#if displayedRelationships.length === 0}
      <div class="text-center py-8 text-gray-500 dark:text-gray-400">
        <Icon name="link-2-off" size="lg" class="mx-auto mb-2 opacity-50" />
        <p>No {activeTab} relationships</p>
      </div>
    {:else}
      <div class="space-y-2">
        {#each displayedRelationships as relationship (relationship.id)}
          {@const isValid = isRelationshipValid(relationship)}
          <div
            class="p-3 rounded-lg border transition-colors cursor-pointer
              {isValid
                ? 'bg-white dark:bg-gray-800 border-gray-200 dark:border-gray-700 hover:bg-gray-50 dark:hover:bg-gray-750'
                : 'bg-gray-50 dark:bg-gray-900 border-gray-200 dark:border-gray-700 opacity-60'}"
            role="button"
            tabindex="0"
            aria-label="{relationship.type}: {relationship.fromEntityId} to {relationship.toEntityId}"
            onclick={() => handleSelect(relationship)}
            onkeydown={(e) => e.key === 'Enter' && handleSelect(relationship)}
          >
            <div class="flex items-start justify-between gap-3">
              <div class="flex-1 min-w-0">
                <!-- Relationship Type -->
                <div class="flex items-center gap-2 mb-2">
                  <RelationshipBadge type={relationship.type} size="sm" />
                  {#if !isValid}
                    <span class="text-xs text-gray-500 dark:text-gray-400">(Invalidated)</span>
                  {/if}
                </div>

                <!-- Target Entity -->
                <div class="flex items-center gap-2 text-sm">
                  {#if activeTab === 'outgoing'}
                    <Icon name="arrow-right" size="sm" class="text-gray-400" />
                    <EntityTypeBadge type={relationship.toEntityType} size="sm" />
                    <span class="text-gray-600 dark:text-gray-300 truncate">
                      {relationship.toEntityId.substring(0, 8)}...
                    </span>
                  {:else}
                    <Icon name="arrow-left" size="sm" class="text-gray-400" />
                    <EntityTypeBadge type={relationship.fromEntityType} size="sm" />
                    <span class="text-gray-600 dark:text-gray-300 truncate">
                      {relationship.fromEntityId.substring(0, 8)}...
                    </span>
                    <span class="text-xs text-gray-400">
                      ({getInverseRelationType(relationship.type)})
                    </span>
                  {/if}
                </div>

                <!-- Reason -->
                {#if relationship.reason}
                  <p class="mt-1 text-xs text-gray-500 dark:text-gray-400 line-clamp-2">
                    {relationship.reason}
                  </p>
                {/if}

                <!-- Date -->
                <div class="mt-2 text-xs text-gray-400">
                  Created: {formatDateStr(relationship.createdAt)}
                  {#if relationship.validTo}
                    <span class="ml-2">| Invalidated: {formatDateStr(relationship.validTo)}</span>
                  {/if}
                </div>
              </div>

              <!-- Actions -->
              {#if isValid && onInvalidate}
                <IconButton icon="x" variant="danger" size="md" title="Invalidate relationship" onclick={(e) => handleInvalidate(e, relationship.id)} />
              {/if}
            </div>
          </div>
        {/each}
      </div>
    {/if}
  </div>
</div>
