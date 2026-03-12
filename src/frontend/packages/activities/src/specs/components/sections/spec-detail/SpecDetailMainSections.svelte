<!-- Section: SpecDetailMainSections — Specs > SpecDetailView -->
<script lang="ts">
  import { Button, Icon } from '@sddp/ui';
  import { CollapsibleGroup } from '@sddp/shell';
  import DetailField from '../../../../shared/components/idioms/DetailField.svelte';
  import ReferencesSection from '../../../../shared/components/idioms/ReferencesSection.svelte';
  import type { ReferenceItem } from '../../../../shared/components/idioms/ReferencesSection.svelte';
  import MetadataSection from '../../../../shared/components/idioms/MetadataSection.svelte';
  import type { MetadataItem } from '../../../../shared/components/idioms/MetadataSection.svelte';
  import type { FieldAuthor } from '../../../../shared/types';
  import type { SpecDetail as SpecDetailType } from '../../../types';

  export type GlossarySegment =
    | { type: 'text'; value: string }
    | { type: 'term'; value: string };

  interface Props {
    spec: SpecDetailType;
    isDraft: boolean;
    basicExpanded: boolean;
    scopeExpanded: boolean;
    criteriaExpanded: boolean;
    governanceExpanded: boolean;
    referencesExpanded: boolean;
    metadataExpanded: boolean;
    onToggleBasic: () => void;
    onToggleScope: () => void;
    onToggleCriteria: () => void;
    onToggleGovernance: () => void;
    onToggleReferences: () => void;
    onToggleMetadata: () => void;
    getFieldAuthor: (fieldName: string) => FieldAuthor | null;
    definitionSegments: GlossarySegment[];
    onGlossaryTermClick: (term: string) => void | Promise<void>;
    referenceItems: ReferenceItem[];
    metadataItems: MetadataItem[];
  }

  let {
    spec,
    isDraft,
    basicExpanded,
    scopeExpanded,
    criteriaExpanded,
    governanceExpanded,
    referencesExpanded,
    metadataExpanded,
    onToggleBasic,
    onToggleScope,
    onToggleCriteria,
    onToggleGovernance,
    onToggleReferences,
    onToggleMetadata,
    getFieldAuthor,
    definitionSegments,
    onGlossaryTermClick,
    referenceItems,
    metadataItems,
  }: Props = $props();
</script>

<!-- Basic Information -->
<CollapsibleGroup
  title="Basic Information"
  variant="plain"
  expanded={basicExpanded}
  onToggle={onToggleBasic}
>
  <div class="flex flex-col gap-4 pl-5 pt-2 pb-3">
    <DetailField label="Description" author={getFieldAuthor('Description')}>
      <div class="prose prose-sm dark:prose-invert max-w-none text-[var(--color-text-primary)] whitespace-pre-wrap">
        {spec.description || 'No description provided.'}
      </div>
    </DetailField>

    <DetailField label="Decision" author={getFieldAuthor('Decision')} required={isDraft}>
      <div class="p-3 rounded-lg bg-[var(--color-surface-200)] border {isDraft && !spec.decision?.trim()
        ? 'border-dashed border-[var(--color-warning-400)]'
        : 'border-[var(--color-border-secondary)]'}">
        <div class="prose prose-sm dark:prose-invert max-w-none whitespace-pre-wrap {isDraft && !spec.decision?.trim()
          ? 'text-[var(--color-text-muted)]'
          : 'text-[var(--color-text-primary)]'}">
          {spec.decision || 'No decision documented.'}
        </div>
      </div>
    </DetailField>
  </div>
</CollapsibleGroup>

<!-- Scope & Context -->
<CollapsibleGroup
  title="Scope & Context"
  variant="plain"
  expanded={scopeExpanded}
  onToggle={onToggleScope}
>
  <div class="flex flex-col gap-4 pl-5 pt-2 pb-3">
    <DetailField label="Context" author={getFieldAuthor('Context')} required={isDraft}>
      <div class="prose prose-sm dark:prose-invert max-w-none whitespace-pre-wrap {isDraft && !spec.context?.trim()
        ? 'text-[var(--color-text-muted)]'
        : 'text-[var(--color-text-primary)]'}">
        {spec.context || 'Not specified.'}
      </div>
    </DetailField>

    <DetailField label="Scope" author={getFieldAuthor('Scope')}>
      <div class="prose prose-sm dark:prose-invert max-w-none text-[var(--color-text-primary)] whitespace-pre-wrap">
        {spec.scope || 'Not specified.'}
      </div>
    </DetailField>

    <DetailField label="Out of Scope" author={getFieldAuthor('OutOfScope')}>
      <div class="prose prose-sm dark:prose-invert max-w-none text-[var(--color-text-primary)] whitespace-pre-wrap">
        {spec.outOfScope || 'Not specified.'}
      </div>
    </DetailField>
  </div>
</CollapsibleGroup>

<!-- Criteria & Definitions -->
<CollapsibleGroup
  title="Criteria & Definitions"
  variant="plain"
  expanded={criteriaExpanded}
  onToggle={onToggleCriteria}
>
  <div class="flex flex-col gap-4 pl-5 pt-2 pb-3">
    <DetailField label="Definitions" author={getFieldAuthor('Definitions')}>
      <div class="prose prose-sm dark:prose-invert max-w-none text-[var(--color-text-primary)] whitespace-pre-wrap">
        {#if definitionSegments.length > 0}
          {#each definitionSegments as segment, index (index)}
            {#if segment.type === 'term'}
              <Button
                variant="unstyled"
                class="glossary-link text-[var(--color-accent-primary)] underline decoration-dotted decoration-1 underline-offset-2 hover:decoration-solid cursor-pointer bg-transparent p-0 border-0"
                onclick={() => onGlossaryTermClick(segment.value)}
              >
                @{segment.value}
              </Button>
            {:else}
              {segment.value}
            {/if}
          {/each}
        {:else}
          No definitions provided.
        {/if}
      </div>
    </DetailField>

    <DetailField label="Acceptance Criteria" author={getFieldAuthor('AcceptanceCriteria')} required={isDraft}>
      <div class="p-3 rounded-lg bg-[var(--color-surface-200)] border {isDraft && !spec.acceptanceCriteria?.trim()
        ? 'border-dashed border-[var(--color-warning-400)]'
        : 'border-[var(--color-border-secondary)]'}">
        <div class="prose prose-sm dark:prose-invert max-w-none whitespace-pre-wrap {isDraft && !spec.acceptanceCriteria?.trim()
          ? 'text-[var(--color-text-muted)]'
          : 'text-[var(--color-text-primary)]'}">
          {spec.acceptanceCriteria || 'No acceptance criteria defined.'}
        </div>
      </div>
    </DetailField>
  </div>
</CollapsibleGroup>

<!-- Governance -->
<CollapsibleGroup
  title="Governance"
  variant="plain"
  expanded={governanceExpanded}
  onToggle={onToggleGovernance}
>
  <div class="flex flex-col gap-4 pl-5 pt-2 pb-3">
    <DetailField label="Owners" author={getFieldAuthor('Owners')}>
      {#if spec.owners?.split(',').filter((o) => o.trim()).length}
        <div class="flex flex-wrap gap-2">
          {#each spec.owners.split(',').filter((o) => o.trim()) as owner, ownerIdx (ownerIdx)}
            <span class="inline-flex items-center px-2 py-1 rounded-md bg-[var(--color-surface-200)] text-sm text-[var(--color-text-primary)]">
              <Icon name="user" size="xs" class="mr-1 text-[var(--color-text-muted)]" />
              {owner.trim()}
            </span>
          {/each}
        </div>
      {:else}
        <div class="prose prose-sm dark:prose-invert max-w-none text-[var(--color-text-primary)]">
          No owners assigned.
        </div>
      {/if}
    </DetailField>

    <DetailField label="Review Trigger" author={getFieldAuthor('ReviewTrigger')}>
      <div class="prose prose-sm dark:prose-invert max-w-none text-[var(--color-text-primary)] whitespace-pre-wrap">
        {spec.reviewTrigger || 'No review trigger defined.'}
      </div>
    </DetailField>
  </div>
</CollapsibleGroup>

<!-- References -->
<CollapsibleGroup
  title="References"
  variant="plain"
  badge={referenceItems.length || undefined}
  expanded={referencesExpanded}
  onToggle={onToggleReferences}
>
  <div class="pl-5 pt-2 pb-3">
    {#if referenceItems.length > 0}
      <ReferencesSection items={referenceItems} title="" />
    {:else}
      <div class="prose prose-sm dark:prose-invert max-w-none text-[var(--color-text-primary)]">
        No references linked.
      </div>
    {/if}
  </div>
</CollapsibleGroup>

<!-- Metadata -->
<CollapsibleGroup
  title="Metadata"
  variant="plain"
  expanded={metadataExpanded}
  onToggle={onToggleMetadata}
>
  <div class="pl-5 pt-2 pb-3">
    <MetadataSection items={metadataItems} />
  </div>
</CollapsibleGroup>
