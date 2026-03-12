<!-- Section: GlossaryDetail — Projects > Glossary -->
<script lang="ts">
  import type { GlossaryTermDetail, GlossaryTermUsage } from '../../types';
  import { TERM_STATUS_STYLES } from '../../types';
  import { TermCategoryBadge, TermStatusBadge } from '../idioms';
  import { Button, Icon, IconButton, Spinner } from '@sddp/ui';
  import { untrack } from 'svelte';
  import { CollapsibleGroup, Dropdown, renderMarkdown, formatDateTime } from '@sddp/shell';
  import DetailHeader from '../../../shared/components/idioms/DetailHeader.svelte';
  import DetailTitle from '../../../shared/components/idioms/DetailTitle.svelte';
  import DetailField from '../../../shared/components/idioms/DetailField.svelte';
  import ReferencesSection from '../../../shared/components/idioms/ReferencesSection.svelte';
  import type { ReferenceItem } from '../../../shared/components/idioms/ReferencesSection.svelte';
  import MetadataSection from '../../../shared/components/idioms/MetadataSection.svelte';
  import type { MetadataItem } from '../../../shared/components/idioms/MetadataSection.svelte';
  import type { FieldAuthor } from '../../../shared/types';
  import { getFieldAuthors } from '../../services/GlossaryService';

  interface Props {
    term: GlossaryTermDetail;
    usage?: GlossaryTermUsage | null;
    loadingUsage?: boolean;
    onEdit?: () => void;
    onApprove?: () => void;
    onDeprecate?: () => void;
    onClose?: () => void;
    onTermSelect?: (termId: string) => void;
    onSpecSelect?: (specId: string) => void;
    onConversationSelect?: (conversationId: string) => void;
    onRequirementSelect?: (requirementId: string) => void;
    /** When true, hides all action buttons (edit, approve, deprecate) */
    readonly?: boolean;
    /** Hide internal header (for Pattern B layout where header is rendered externally) */
    showHeader?: boolean;
    class?: string;
  }

  let {
    term,
    usage = null,
    loadingUsage = false,
    onEdit,
    onApprove,
    onDeprecate,
    onClose,
    onTermSelect,
    onSpecSelect,
    onConversationSelect,
    onRequirementSelect,
    readonly: isReadOnly = false,
    showHeader: showHeaderProp = true,
    class: className = '',
  }: Props = $props();

  const canApprove = $derived(!isReadOnly && term.status === 'Draft' && !!onApprove);
  const canDeprecate = $derived(!isReadOnly && term.status === 'Active' && !!onDeprecate);
  const canEdit = $derived(!isReadOnly && !!onEdit && term.status !== 'Deprecated');

  // Field authors (who last modified each field)
  let fieldAuthors = $state<FieldAuthor[]>([]);

  async function loadFieldAuthors(): Promise<void> {
    try {
      fieldAuthors = await getFieldAuthors(term.tenantId, term.projectId, term.id);
    } catch {
      fieldAuthors = [];
    }
  }

  function getFieldAuthor(fieldName: string): FieldAuthor | null {
    return fieldAuthors.find((a) => a.fieldName.toLowerCase() === fieldName.toLowerCase()) ?? null;
  }

  // Load field authors on mount / term change
  let prevTermId = $state<string | null>(null);
  $effect(() => {
    if (!term.id) return;
    if (term.id === prevTermId) return;
    prevTermId = term.id;
    untrack(() => loadFieldAuthors().catch((err) => console.warn('[GlossaryDetail] loadFieldAuthors failed:', err)));
  });

  // CollapsibleGroup states
  let basicExpanded = $state(true);
  let referencesExpanded = $state(true);
  let usageExpanded = $state(true);
  let metadataExpanded = $state(true);

  // Build reference items for ReferencesSection
  // Order: Conversation → Requirement → Spec → Replaced By
  const referenceItems = $derived.by(() => {
    const items: ReferenceItem[] = [];
    // Source Conversation
    if (term.sourceConversationId && term.sourceConversationName) {
      const convIcon = term.sourceConversationType === 'Channel' ? 'hash'
        : term.sourceConversationType === 'Forum' ? 'clipboard-list'
        : 'message-circle';
      const prefix = term.sourceConversationType === 'Channel' ? '#' : '';
      items.push({
        icon: convIcon,
        label: 'Source Conversation',
        value: `${prefix}${term.sourceConversationName}`,
        sublabel: term.sourceConversationType ?? undefined,
        onClick: () => onConversationSelect?.(term.sourceConversationId!),
      });
    }
    // Source Requirement
    if (term.sourceRequirementId && (term.sourceRequirementCode || term.sourceRequirementTitle)) {
      items.push({
        icon: 'clipboard-check',
        label: 'Source Requirement',
        value: term.sourceRequirementTitle || term.sourceRequirementCode || term.sourceRequirementId,
        onClick: () => onRequirementSelect?.(term.sourceRequirementId!),
      });
    }
    // Source Spec
    if (term.sourceSpecId && (term.sourceSpecCode || term.sourceSpecTitle)) {
      items.push({
        icon: 'file-code',
        label: 'Source Spec',
        value: term.sourceSpecTitle || term.sourceSpecCode || term.sourceSpecId,
        sublabel: term.sourceSpecCode ?? undefined,
        onClick: () => onSpecSelect?.(term.sourceSpecId!),
      });
    }
    // Replaced By (when deprecated)
    if (term.replacedByTermId && term.replacedByTermName) {
      items.push({
        icon: 'arrow-right',
        label: 'Replaced By',
        value: term.replacedByTermName,
        onClick: () => onTermSelect?.(term.replacedByTermId!),
      });
    }
    return items;
  });

  // Build metadata items for MetadataSection
  const metadataItems = $derived.by(() => {
    const items: MetadataItem[] = [];
    const statusStyle = TERM_STATUS_STYLES[term.status];
    // Row 1: Status | Version
    items.push({ label: 'Status', value: statusStyle.label, class: statusStyle.color });
    items.push({ label: 'Version', value: `v${term.version}` });
    // Row 2: Valid From | Owner
    if (term.validFrom) {
      items.push({ label: 'Valid From', value: formatDateTime(term.validFrom, { month: 'short' }) });
    }
    items.push({ label: 'Owner', value: term.owner?.name || 'Unassigned', type: term.owner?.name ? 'person' : 'text' });
    // Row 3: Created By | Created
    if (term.createdBy?.name) {
      items.push({ label: 'Created By', value: term.createdBy.name, type: 'person' });
    }
    if (term.createdAt) {
      items.push({ label: 'Created', value: formatDateTime(term.createdAt, { month: 'short' }) });
    }
    // Row 5: Updated By | Updated
    if (term.updatedBy?.name) {
      items.push({ label: 'Updated By', value: term.updatedBy.name, type: 'person' });
    }
    if (term.updatedAt) {
      items.push({ label: 'Updated', value: formatDateTime(term.updatedAt, { month: 'short' }) });
    }
    // Row 6: Approved By | Approved (conditional)
    if (term.approvedBy?.name) {
      items.push({ label: 'Approved By', value: term.approvedBy.name, type: 'person' });
      if (term.approvedAt) {
        items.push({ label: 'Approved', value: formatDateTime(term.approvedAt, { month: 'short' }) });
      }
    }
    if (term.validTo) {
      items.push({ label: 'Valid To', value: formatDateTime(term.validTo, { month: 'short' }) });
    }
    return items;
  });
</script>

<div class="flex flex-col h-full {className}">
  {#if showHeaderProp}
    <!-- Header -->
    <DetailHeader>
      {#snippet leading()}
        <TermCategoryBadge category={term.category} size="md" />
      {/snippet}
      <DetailTitle title={term.term} code={term.abbreviation ? `(${term.abbreviation})` : null}>
        <TermStatusBadge status={term.status} size="sm" />
      </DetailTitle>
      {#snippet actions()}
        {#if canEdit}
          <IconButton icon="edit" variant="ghost" size="sm" title="Edit" onclick={onEdit} />
        {/if}
        {#if onClose}
          <IconButton icon="x" variant="ghost" size="sm" title="Close" onclick={onClose} />
        {/if}
        {#if canApprove || canDeprecate}
          <Dropdown position="bottom-right">
            {#snippet trigger()}
              <IconButton icon="more-vertical" variant="ghost" size="sm" title="More actions" />
            {/snippet}
            <div class="py-1 min-w-[160px]">
              {#if canApprove}
                <Button
                  variant="unstyled"
                  onclick={onApprove}
                  class="w-full text-left px-3 py-1.5 text-xs font-medium hover:bg-[var(--color-surface-200)] transition-colors text-[var(--color-success-600)]"
                >
                  Approve
                </Button>
              {/if}
              {#if canDeprecate}
                <Button
                  variant="unstyled"
                  onclick={onDeprecate}
                  class="w-full text-left px-3 py-1.5 text-xs font-medium hover:bg-[var(--color-surface-200)] transition-colors text-[var(--color-error-600)]"
                >
                  Deprecate
                </Button>
              {/if}
            </div>
          </Dropdown>
        {/if}
      {/snippet}
    </DetailHeader>
  {/if}

  <!-- Content -->
  <div class="flex-1 overflow-y-auto p-4">
    <div class="space-y-5">
      <!-- Basic Information -->
      <CollapsibleGroup
        title="Basic Information"
        variant="plain"
        expanded={basicExpanded}
        onToggle={() => basicExpanded = !basicExpanded}
      >
        <div class="flex flex-col gap-4 pl-5 pt-2 pb-3">
          <DetailField label="Definition" author={getFieldAuthor('Definition')}>
            <div class="prose prose-sm dark:prose-invert max-w-none text-[var(--color-text-primary)] whitespace-pre-wrap">
              <!-- eslint-disable-next-line svelte/no-at-html-tags -->
              {@html renderMarkdown(term.definition)}
            </div>
          </DetailField>

          <DetailField label="Category" author={getFieldAuthor('Category')}>
            <TermCategoryBadge category={term.category} size="sm" />
          </DetailField>

          {#if term.abbreviation}
            <DetailField label="Abbreviation" author={getFieldAuthor('Abbreviation')}>
              <span class="px-2 py-1 text-sm font-mono rounded bg-[var(--color-surface-200)] text-[var(--color-text-primary)] border border-[var(--color-border-secondary)]">
                {term.abbreviation}
              </span>
            </DetailField>
          {/if}

          {#if term.synonyms}
            <DetailField label="Synonyms" author={getFieldAuthor('Synonyms')}>
              <div class="flex flex-wrap gap-2">
                {#each term.synonyms.split(',').map(s => s.trim()).filter(Boolean) as synonym (synonym)}
                  <span class="px-2 py-1 text-xs bg-[var(--color-surface-200)] text-[var(--color-text-secondary)] rounded">
                    {synonym}
                  </span>
                {/each}
              </div>
            </DetailField>
          {/if}

          {#if term.source}
            <DetailField label="Source" author={getFieldAuthor('Source')}>
              <p class="text-sm text-[var(--color-text-secondary)]">
                {#if term.source.startsWith('http')}
                  <a
                    href={term.source}
                    target="_blank"
                    rel="noopener noreferrer"
                    class="text-[var(--color-accent-primary)] hover:underline"
                  >
                    {term.source}
                  </a>
                {:else}
                  {term.source}
                {/if}
              </p>
            </DetailField>
          {/if}

          {#if term.usageExamples && term.usageExamples.length > 0}
            <DetailField label="Usage Examples">
              <ul class="space-y-2">
                {#each term.usageExamples as example, i (i)}
                  <li class="flex items-start gap-2 text-sm text-[var(--color-text-secondary)]">
                    <Icon name="quote" size="sm" class="mt-0.5 text-[var(--color-text-tertiary)] flex-shrink-0" />
                    <span class="italic">{example}</span>
                  </li>
                {/each}
              </ul>
            </DetailField>
          {/if}
        </div>
      </CollapsibleGroup>

      <!-- References -->
      {#if referenceItems.length > 0}
        <CollapsibleGroup
          title="References"
          variant="plain"
          badge={referenceItems.length}
          expanded={referencesExpanded}
          onToggle={() => referencesExpanded = !referencesExpanded}
        >
          <div class="pl-5 pt-2 pb-3">
            <ReferencesSection items={referenceItems} title="" />
          </div>
        </CollapsibleGroup>
      {/if}

      <!-- Usage in Specs -->
      {#if usage !== null}
        <CollapsibleGroup
          title="Usage in Specs"
          variant="plain"
          badge={usage?.usageCount ?? undefined}
          expanded={usageExpanded}
          onToggle={() => usageExpanded = !usageExpanded}
        >
          <div class="pl-5 pt-2 pb-3">
            {#if loadingUsage}
              <div class="flex items-center gap-2 text-sm text-[var(--color-text-muted)]">
                <Spinner size="sm" />
                Loading usage...
              </div>
            {:else if usage && usage.usages.length > 0}
              <ul class="space-y-2">
                {#each usage.usages as item (item.entityId)}
                  <li class="p-2 bg-[var(--color-surface-100)] rounded-lg">
                    <div class="flex items-center justify-between">
                      <span class="text-sm font-medium text-[var(--color-text-secondary)]">
                        {item.entityTitle}
                      </span>
                      <span class="text-xs px-1.5 py-0.5 rounded bg-[var(--color-surface-200)] text-[var(--color-text-secondary)]">
                        {item.entityType}
                      </span>
                    </div>
                    <div class="text-xs text-[var(--color-text-muted)] mt-1">
                      Found in: {item.fieldName}
                    </div>
                  </li>
                {/each}
              </ul>
              <p class="text-xs text-[var(--color-text-tertiary)] mt-2">
                Total: {usage.usageCount} references
              </p>
            {:else}
              <p class="text-sm text-[var(--color-text-muted)]">
                Not used in any specs yet.
              </p>
            {/if}
          </div>
        </CollapsibleGroup>
      {/if}

      <!-- Metadata -->
      <CollapsibleGroup
        title="Metadata"
        variant="plain"
        expanded={metadataExpanded}
        onToggle={() => metadataExpanded = !metadataExpanded}
      >
        <div class="pl-5 pt-2 pb-3">
          <MetadataSection items={metadataItems} />
        </div>
      </CollapsibleGroup>
    </div>
  </div>
</div>
