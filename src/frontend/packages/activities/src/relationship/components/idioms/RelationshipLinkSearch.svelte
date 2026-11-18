<script lang="ts">
  import { Icon, IconButton, Button, SearchField, Spinner } from '@sddp/ui';
  import type { EntitySearchResult } from '../../services/RelationshipService';
  import type { EntityType, RelationType } from '../../types';
  import { ENTITY_TYPE_STYLES, RELATION_TYPE_STYLES } from '../../types';
  let {
    searchQuery,
    selectedType,
    results,
    searching,
    selectedResult,
    linking,
    relationTypes,
    onSearch,
    onTypeChange,
    onResultClick,
    onLinkAs,
    onClose,
  }: {
    searchQuery: string;
    selectedType: EntityType | 'All';
    results: EntitySearchResult[];
    searching: boolean;
    selectedResult: EntitySearchResult | null;
    linking: boolean;
    relationTypes: RelationType[];
    onSearch: (query: string) => void;
    onTypeChange: (type: EntityType | 'All') => void;
    onResultClick: (result: EntitySearchResult) => void;
    onLinkAs: (relation: RelationType) => void;
    onClose?: () => void;
  } = $props();
  const filterTypes: Array<{ value: EntityType | 'All'; label: string }> = [
    { value: 'All', label: 'All' },
    { value: 'Spec', label: 'Spec' },
    { value: 'Requirement', label: 'Req' },
    { value: 'Conversation', label: 'Conv' },
    { value: 'GlossaryTerm', label: 'Glossary' },
    { value: 'Artifact', label: 'Artifact' },
    { value: 'Task', label: 'Task' },
    { value: 'Diagram', label: 'Diagram' },
  ];
</script>

<section class="flex flex-col gap-2" aria-label="Link entity search">
  <!-- Header row -->
  <div class="flex items-center justify-between">
    <span class="text-xs font-medium text-[var(--color-text-secondary)]">Link Entity</span>
    <IconButton icon="x" size="sm" variant="ghost" onclick={() => onClose?.()} title="Close" />
  </div>

  <!-- Search input -->
  <SearchField bind:value={searchQuery} placeholder="Search entities..." {onSearch} size="sm" />

  <!-- Type filter chips -->
  <div class="flex gap-1 flex-wrap">
    {#each filterTypes as ft (ft.value)}
      <Button
        variant="unstyled"
        class="px-2 py-0.5 text-xs rounded-full border transition-colors
          {selectedType === ft.value
          ? 'bg-[var(--color-accent-primary)]/10 border-[var(--color-accent-primary)]/30 text-[var(--color-accent-text)]'
          : 'bg-transparent border-[var(--color-border-secondary)] text-[var(--color-text-tertiary)] hover:border-[var(--color-border-primary)]'}"
        onclick={() => onTypeChange(ft.value)}
      >
        {ft.label}
      </Button>
    {/each}
  </div>

  <!-- Results -->
  {#if searching}
    <div class="flex justify-center py-3">
      <Spinner size="sm" />
    </div>
  {:else if results.length > 0}
    <div class="max-h-[200px] overflow-y-auto space-y-0.5">
      {#each results as result (result.id)}
        {@const style = ENTITY_TYPE_STYLES[result.entityType]}
        {@const isSelected = selectedResult?.id === result.id}
        <div>
          <Button
            variant="unstyled"
            class="w-full flex items-center gap-2 p-1.5 rounded-lg transition-colors
              {isSelected
              ? 'bg-[var(--color-accent-primary)]/10 border border-[var(--color-accent-primary)]/30'
              : 'hover:bg-[var(--color-bg-tertiary)] border border-transparent'}"
            onclick={() => onResultClick(result)}
          >
            <div
              class="flex items-center justify-center w-5 h-5 rounded flex-shrink-0 {style.bgColor}"
            >
              <Icon name={style.icon} size="xs" class={style.color} />
            </div>
            <div class="flex-1 min-w-0 text-left">
              <div class="text-sm text-[var(--color-text-primary)] truncate">
                {result.label}
              </div>
              {#if result.code}
                <div class="text-xs text-[var(--color-text-tertiary)] truncate">
                  {result.code}
                </div>
              {/if}
            </div>
            <span class="text-[0.625rem] text-[var(--color-text-tertiary)] flex-shrink-0">
              {style.label}
            </span>
          </Button>

          <!-- Relation type chips (shown when this result is selected) -->
          {#if isSelected}
            <div class="flex gap-1 flex-wrap pl-7 py-1">
              {#if linking}
                <Spinner size="sm" />
              {:else}
                {#each relationTypes as relType (relType)}
                  {@const relStyle = RELATION_TYPE_STYLES[relType]}
                  <Button
                    variant="unstyled"
                    class="px-2 py-0.5 text-xs rounded-full border transition-colors
                      {relStyle.bgColor} {relStyle.borderColor} {relStyle.color}
                      hover:opacity-80"
                    onclick={() => onLinkAs(relType)}
                    title={relStyle.description ?? relStyle.label}
                  >
                    {relStyle.label}
                  </Button>
                {/each}
              {/if}
            </div>
          {/if}
        </div>
      {/each}
    </div>
  {:else if searchQuery.trim() && !searching}
    <p class="text-xs text-[var(--color-text-tertiary)] text-center py-2">No results found</p>
  {/if}
</section>
