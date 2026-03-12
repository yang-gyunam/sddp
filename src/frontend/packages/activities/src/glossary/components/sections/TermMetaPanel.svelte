<!-- Section: TermMetaPanel — Glossary > Global -->
<script lang="ts">
  import { Icon, Spinner } from '@sddp/ui';
  import type { GlossaryTermDetail, GlossaryTermUsage } from '../../types';
  import { TERM_CATEGORY_STYLES } from '../../types/glossary.types';

  interface Props {
    term: GlossaryTermDetail | null;
    usage?: GlossaryTermUsage | null;
    loadingUsage?: boolean;
    class?: string;
  }

  let {
    term,
    usage = null,
    loadingUsage = false,
    class: className = '',
  }: Props = $props();

  const categoryStyle = $derived(term ? TERM_CATEGORY_STYLES[term.category] : null);
</script>

<div class="flex flex-col h-full bg-[var(--color-bg-secondary)] {className}">
  {#if !term}
    <div class="flex items-center justify-center h-full">
      <p class="text-sm text-[var(--color-text-tertiary)]">No term selected</p>
    </div>
  {:else}
    <!-- Quick Info -->
    <div class="p-3 border-b border-[var(--color-border-primary)]">
      <h3 class="text-xs font-semibold text-[var(--color-text-tertiary)] uppercase tracking-wider mb-2">
        Quick Info
      </h3>
      <div class="space-y-2">
        <div class="flex items-center justify-between text-sm">
          <span class="text-[var(--color-text-tertiary)]">Category</span>
          <span class="flex items-center gap-1 {categoryStyle?.color}">
            <Icon name={categoryStyle?.icon || 'tag'} size="xs" />
            {term.category}
          </span>
        </div>
        <div class="flex items-center justify-between text-sm">
          <span class="text-[var(--color-text-tertiary)]">Version</span>
          <span class="text-[var(--color-text-secondary)]">v{term.version}</span>
        </div>
        <div class="flex items-center justify-between text-sm">
          <span class="text-[var(--color-text-tertiary)]">Status</span>
          <span class="text-[var(--color-text-secondary)]">{term.status}</span>
        </div>
        {#if term.abbreviation}
          <div class="flex items-center justify-between text-sm">
            <span class="text-[var(--color-text-tertiary)]">Abbreviation</span>
            <span class="font-mono text-[var(--color-text-secondary)]">{term.abbreviation}</span>
          </div>
        {/if}
      </div>
    </div>

    <!-- Usage Section -->
    <div class="flex-1 overflow-y-auto p-3">
      <h3 class="text-xs font-semibold text-[var(--color-text-tertiary)] uppercase tracking-wider mb-2">
        Usage in Specs
      </h3>

      {#if loadingUsage}
        <div class="flex items-center gap-2 text-sm text-[var(--color-text-tertiary)] py-4">
          <Spinner size="sm" />
          Loading usage...
        </div>
      {:else if usage && usage.usages.length > 0}
        <!-- Usage stats -->
        <div class="flex items-center gap-2 mb-3 p-2 bg-[var(--color-bg-primary)] rounded-lg">
          <div class="flex-1 text-center">
            <div class="text-lg font-bold text-[var(--color-text-primary)]">
              {usage.usageCount}
            </div>
            <div class="text-xs text-[var(--color-text-tertiary)]">References</div>
          </div>
        </div>

        <!-- Usage list -->
        <div class="space-y-2">
          {#each usage.usages.slice(0, 10) as item (item.entityId)}
            <div class="p-2 bg-[var(--color-bg-primary)] rounded-lg">
              <div class="flex items-center justify-between">
                <span class="text-sm font-medium text-[var(--color-text-secondary)] truncate">
                  {item.entityTitle}
                </span>
                <span class="text-xs px-1.5 py-0.5 rounded
                  {item.entityType === 'Spec' ? 'bg-[var(--color-info-500)]/10 text-[var(--color-info-500)]' :
                   item.entityType === 'Requirement' ? 'bg-[var(--color-success-500)]/10 text-[var(--color-success-500)]' :
                   'bg-[var(--color-bg-tertiary)] text-[var(--color-text-secondary)]'}">
                  {item.entityType}
                </span>
              </div>
              <div class="text-xs text-[var(--color-text-tertiary)] mt-1">
                in {item.fieldName}
              </div>
            </div>
          {/each}
          {#if usage.usages.length > 10}
            <p class="text-xs text-[var(--color-text-tertiary)] text-center py-2">
              +{usage.usages.length - 10} more references
            </p>
          {/if}
        </div>
      {:else}
        <div class="flex flex-col items-center justify-center py-6 text-[var(--color-text-tertiary)]">
          <Icon name="link" size="lg" class="mb-2 opacity-50" />
          <p class="text-xs">Not used yet</p>
        </div>
      {/if}
    </div>

    <!-- Related Terms -->
    {#if term.relatedTermIds && term.relatedTermIds.length > 0}
      <div class="p-3 border-t border-[var(--color-border-primary)]">
        <h3 class="text-xs font-semibold text-[var(--color-text-tertiary)] uppercase tracking-wider mb-2">
          Related Terms
        </h3>
        <div class="flex flex-wrap gap-1">
          {#each term.relatedTermIds as relatedId (relatedId)}
            <span class="text-xs px-2 py-1 bg-[var(--color-bg-primary)] text-[var(--color-text-secondary)] rounded">
              {relatedId}
            </span>
          {/each}
        </div>
      </div>
    {/if}

    <!-- Replaced By (for deprecated terms) -->
    {#if term.status === 'Deprecated' && term.replacedByTermName}
      <div class="p-3 border-t border-[var(--color-border-primary)]">
        <h3 class="text-xs font-semibold text-[var(--color-text-tertiary)] uppercase tracking-wider mb-2">
          Replaced By
        </h3>
        <div class="p-2 bg-[var(--color-bg-primary)] rounded-lg">
          <div class="flex items-center gap-2 text-sm">
            <Icon name="arrow-right" size="sm" class="text-[var(--color-text-tertiary)]" />
            <span class="text-[var(--color-accent-primary)]">{term.replacedByTermName}</span>
          </div>
        </div>
      </div>
    {/if}
  {/if}
</div>
