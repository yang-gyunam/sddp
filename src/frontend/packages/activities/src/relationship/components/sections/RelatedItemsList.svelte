<!-- Section: RelatedItemsList -->
<!--
  RelatedItemsList Component
  Displays related items in a list format for Main Content areas
  Groups items by entity type with collapsible sections
-->
<script lang="ts">
  import { SvelteSet } from 'svelte/reactivity';
  import { Icon, Button, Badge, Spinner } from '@sddp/ui';
  import { formatDate as formatDateUtil } from '@sddp/shell';
  import type { RelatedItem, EntityType, RelationType } from '../../types';
  import {
    ENTITY_TYPE_STYLES,
    RELATION_TYPE_STYLES,
    groupRelatedItems,
    sortRelatedItems,
    getInverseRelationType,
  } from '../../types';

  interface Props {
    items: RelatedItem[];
    loading?: boolean;
    error?: string | null;
    showEmpty?: boolean;
    groupByType?: boolean;
    maxItems?: number;
    onItemClick?: (item: RelatedItem) => void;
    onLoadMore?: () => void;
    onAddRelationship?: () => void;
    class?: string;
  }

  let {
    items,
    loading = false,
    error = null,
    showEmpty = true,
    groupByType = true,
    maxItems = 0,
    onItemClick,
    onLoadMore,
    onAddRelationship,
    class: className = '',
  }: Props = $props();

  // Expanded groups state
  let expandedGroups = new SvelteSet<string>(['specs', 'requirements', 'conversations']);

  // Process items
  const sortedItems = $derived(sortRelatedItems(items));
  const displayItems = $derived(maxItems > 0 ? sortedItems.slice(0, maxItems) : sortedItems);
  const hasMore = $derived(maxItems > 0 && sortedItems.length > maxItems);
  const groupedItems = $derived(groupByType ? groupRelatedItems(displayItems) : null);

  function toggleGroup(groupId: string) {
    if (expandedGroups.has(groupId)) {
      expandedGroups.delete(groupId);
    } else {
      expandedGroups.add(groupId);
    }
  }

  function getGroupLabel(groupId: string): string {
    const labels: Record<string, string> = {
      conversations: 'Conversations',
      requirements: 'Requirements',
      specs: 'Specs',
      glossaryTerms: 'Glossary Terms',
      artifacts: 'Artifacts',
    };
    return labels[groupId] || groupId;
  }

  function formatDateStr(dateStr: string): string {
    return formatDateUtil(dateStr, { locale: 'en-US', month: 'short', year: undefined });
  }
</script>

<div class="flex flex-col {className}">
  <!-- Header -->
  <div class="flex items-center justify-between mb-3">
    <h3 class="text-sm font-semibold text-gray-700 dark:text-gray-300 flex items-center gap-2">
      <Icon name="link" size="sm" class="text-gray-400" />
      Related Items
      {#if items.length > 0}
        <Badge variant="secondary" size="sm">{items.length}</Badge>
      {/if}
    </h3>
    {#if onAddRelationship}
      <Button size="sm" variant="ghost" onclick={onAddRelationship}>
        <Icon name="plus" size="sm" />
        Add
      </Button>
    {/if}
  </div>

  <!-- Content -->
  {#if loading}
    <div class="flex-1 flex items-center justify-center">
      <Spinner size="lg" />
    </div>
  {:else if error}
    <div class="flex flex-col items-center justify-center py-8 text-red-500 dark:text-red-400">
      <Icon name="alert-circle" size="md" class="mb-2" />
      <p class="text-sm">{error}</p>
    </div>
  {:else if items.length === 0}
    {#if showEmpty}
      <div class="flex flex-col items-center justify-center py-8 text-gray-400">
        <Icon name="link-2-off" size="lg" class="mb-2 opacity-50" />
        <p class="text-sm">No related items</p>
        {#if onAddRelationship}
          <Button size="sm" variant="ghost" class="mt-2" onclick={onAddRelationship}>
            Add Relationship
          </Button>
        {/if}
      </div>
    {/if}
  {:else if groupByType && groupedItems}
    <!-- Grouped by type -->
    <div class="space-y-2">
      {#each Object.entries(groupedItems) as [groupId, groupItems] (groupId)}
        {#if groupItems.length > 0}
          {@const isExpanded = expandedGroups.has(groupId)}
          <div class="border border-gray-200 dark:border-gray-700 rounded-lg overflow-hidden">
            <Button
              variant="unstyled"
              class="w-full flex items-center justify-between px-3 py-2 bg-gray-50 dark:bg-gray-800 hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors"
              onclick={() => toggleGroup(groupId)}
            >
              <span class="text-sm font-medium text-gray-700 dark:text-gray-300 flex items-center gap-2">
                {getGroupLabel(groupId)}
                <Badge variant="secondary" size="sm">{groupItems.length}</Badge>
              </span>
              <Icon
                name="chevron-down"
                size="sm"
                class="text-gray-400 transition-transform {isExpanded ? '' : '-rotate-90'}"
              />
            </Button>

            {#if isExpanded}
              <div class="divide-y divide-gray-100 dark:divide-gray-700">
                {#each groupItems as item (item.id)}
                  {@const entityStyle = ENTITY_TYPE_STYLES[item.entityType as EntityType]}
                  {@const relStyle = RELATION_TYPE_STYLES[item.relationshipType as RelationType]}
                  <Button
                    variant="unstyled"
                    class="w-full flex items-center gap-3 px-3 py-2.5 hover:bg-gray-50 dark:hover:bg-gray-700 transition-colors text-left
                      {item.isDeprecated ? 'opacity-50' : ''}"
                    onclick={() => onItemClick?.(item)}
                  >
                    <div class="flex items-center justify-center w-8 h-8 rounded-lg {entityStyle.bgColor}">
                      <Icon name={entityStyle.icon} size="sm" class={entityStyle.color} />
                    </div>
                    <div class="flex-1 min-w-0">
                      <div class="flex items-center gap-2">
                        <span class="text-sm font-medium text-gray-700 dark:text-gray-300 truncate">
                          {item.title}
                        </span>
                        {#if item.isDeprecated}
                          <Badge variant="warning" size="sm">Deprecated</Badge>
                        {/if}
                      </div>
                      <div class="flex items-center gap-2 text-xs text-gray-500">
                        <span>{item.code}</span>
                        <span>·</span>
                        <span class="flex items-center gap-1">
                          <Icon name={relStyle.icon} size="sm" class={relStyle.color} />
                          {item.direction === 'incoming'
                            ? getInverseRelationType(item.relationshipType)
                            : relStyle.label}
                        </span>
                      </div>
                    </div>
                    <Icon name="chevron-right" size="sm" class="text-gray-400" />
                  </Button>
                {/each}
              </div>
            {/if}
          </div>
        {/if}
      {/each}
    </div>
  {:else}
    <!-- Flat list -->
    <div class="space-y-1">
      {#each displayItems as item (item.id)}
        {@const entityStyle = ENTITY_TYPE_STYLES[item.entityType as EntityType]}
        {@const relStyle = RELATION_TYPE_STYLES[item.relationshipType]}
        <Button
          variant="unstyled"
          class="w-full flex items-center gap-3 p-3 rounded-lg border border-gray-200 dark:border-gray-700 hover:bg-gray-50 dark:hover:bg-gray-700 transition-colors text-left
            {item.isDeprecated ? 'opacity-50' : ''}"
          onclick={() => onItemClick?.(item)}
        >
          <div class="flex items-center justify-center w-10 h-10 rounded-lg {entityStyle.bgColor}">
            <Icon name={entityStyle.icon} size="md" class={entityStyle.color} />
          </div>
          <div class="flex-1 min-w-0">
            <div class="flex items-center gap-2">
              <span class="text-sm font-medium text-gray-700 dark:text-gray-300 truncate">
                {item.title}
              </span>
              {#if item.isDeprecated}
                <Badge variant="warning" size="sm">Deprecated</Badge>
              {/if}
            </div>
            <div class="flex items-center gap-2 text-xs text-gray-500 mt-0.5">
              <span class="font-mono">{item.code}</span>
              <span>·</span>
              <span class="flex items-center gap-1 {relStyle.color}">
                <Icon name={relStyle.icon} size="sm" />
                {item.direction === 'incoming'
                  ? getInverseRelationType(item.relationshipType)
                  : relStyle.label}
              </span>
              <span>·</span>
              <span>{formatDateStr(item.createdAt)}</span>
            </div>
          </div>
          <Icon name="external-link" size="sm" class="text-gray-400" />
        </Button>
      {/each}
    </div>
  {/if}

  <!-- Load more -->
  {#if hasMore && onLoadMore}
    <div class="mt-3 text-center">
      <Button size="sm" variant="ghost" onclick={onLoadMore}>
        Show {sortedItems.length - maxItems} more items
      </Button>
    </div>
  {/if}
</div>
