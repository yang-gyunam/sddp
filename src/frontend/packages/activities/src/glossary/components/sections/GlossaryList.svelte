<!-- Section: GlossaryList -->
<script lang="ts">
  import type { GlossaryTerm } from '../../types';
  import { TermCategoryBadge, TermStatusBadge } from '../idioms';
  import { Icon, IconButton, Button } from '@sddp/ui';
  import { truncate, formatDate } from '@sddp/shell';

  interface Props {
    terms: GlossaryTerm[];
    selectedTermId?: string | null;
    onSelect?: (term: GlossaryTerm) => void;
    onApprove?: (term: GlossaryTerm) => void;
    onDeprecate?: (term: GlossaryTerm) => void;
    class?: string;
  }

  let {
    terms,
    selectedTermId = null,
    onSelect,
    onApprove,
    onDeprecate,
    class: className = '',
  }: Props = $props();

  function handleSelect(term: GlossaryTerm) {
    onSelect?.(term);
  }

  function handleApprove(e: Event, term: GlossaryTerm) {
    e.stopPropagation();
    onApprove?.(term);
  }

  function handleDeprecate(e: Event, term: GlossaryTerm) {
    e.stopPropagation();
    onDeprecate?.(term);
  }

  function formatDateShort(dateStr: string): string {
    return formatDate(dateStr, { month: 'short' });
  }
</script>

<div class="flex flex-col {className}">
  {#if terms.length === 0}
    <div class="text-center py-8 text-gray-500 dark:text-gray-400">
      <Icon name="book-open" size="xl" class="mx-auto mb-2 opacity-50" />
      <p>No terms found</p>
    </div>
  {:else}
    <div class="space-y-2">
      {#each terms as term (term.id)}
        {@const isSelected = term.id === selectedTermId}
        <Button
          variant="unstyled"
          class="w-full text-left p-4 rounded-lg transition-colors cursor-pointer
            {isSelected
              ? 'bg-[var(--color-accent-primary)]/10'
              : 'hover:bg-[var(--color-bg-tertiary)]'}"
          aria-label={term.term}
          onclick={() => handleSelect(term)}
        >
          <div class="flex items-start justify-between gap-3">
            <div class="flex-1 min-w-0">
              <!-- Term Name -->
              <div class="flex items-center gap-2 mb-2">
                <span class="text-lg font-semibold text-gray-800 dark:text-gray-200">
                  {term.term}
                </span>
                {#if term.abbreviation}
                  <span class="text-sm text-gray-500 dark:text-gray-400">
                    ({term.abbreviation})
                  </span>
                {/if}
              </div>

              <!-- Badges -->
              <div class="flex items-center gap-2 mb-2">
                <TermCategoryBadge category={term.category} size="sm" />
                <TermStatusBadge status={term.status} size="sm" />
              </div>

              <!-- Definition -->
              <p class="text-sm text-gray-600 dark:text-gray-400 mb-2">
                {truncate(term.definition, 100)}
              </p>

              <!-- Synonyms -->
              {#if term.synonyms}
                <div class="text-xs text-gray-500 dark:text-gray-500 mb-2">
                  <span class="font-medium">Synonyms:</span> {term.synonyms}
                </div>
              {/if}

              <!-- Meta info -->
              <div class="flex items-center gap-4 text-xs text-gray-400">
                <span>v{term.version}</span>
                <span>Updated: {formatDateShort(term.updatedAt)}</span>
              </div>
            </div>

            <!-- Actions -->
            <div class="flex flex-col gap-1">
              {#if term.status === 'Draft' && onApprove}
                <IconButton
                  icon="check"
                  size="sm"
                  variant="ghost"
                  class="p-1.5 rounded hover:bg-green-100 dark:hover:bg-green-900 text-gray-400 hover:text-green-600"
                  title="Approve term"
                  onclick={(e) => handleApprove(e, term)}
                />
              {/if}
              {#if term.status === 'Active' && onDeprecate}
                <IconButton
                  icon="archive"
                  size="sm"
                  variant="ghost"
                  class="p-1.5 rounded hover:bg-red-100 dark:hover:bg-red-900 text-gray-400 hover:text-red-600"
                  title="Deprecate term"
                  onclick={(e) => handleDeprecate(e, term)}
                />
              {/if}
            </div>
          </div>
        </Button>
      {/each}
    </div>
  {/if}
</div>
