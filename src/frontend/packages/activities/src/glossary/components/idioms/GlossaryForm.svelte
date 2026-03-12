<script lang="ts">
  import type {
    TermCategory,
    GlossaryTermDetail,
    CreateGlossaryTermRequest,
    UpdateGlossaryTermRequest,
    GlossaryConflictResult,
  } from '../../types';
  import { TERM_CATEGORIES, TERM_CATEGORY_STYLES } from '../../types';
  import { isFormValid as calcIsFormValid, buildCreateRequest } from '../../utils/glossary-form.utils';
  import { Icon, Button, IconButton, Input, Textarea, Combobox } from '@sddp/ui';
  import type { ComboboxOption } from '@sddp/ui';
  import { CollapsibleGroup } from '@sddp/shell';

  interface Props {
    term?: GlossaryTermDetail | null;
    conflictResult?: GlossaryConflictResult | null;
    conflictLoading?: boolean;
    loading?: boolean;
    specCandidates?: ComboboxOption[];
    specSearchLoading?: boolean;
    memberCandidates?: ComboboxOption[];
    conversationCandidates?: ComboboxOption[];
    conversationSearchLoading?: boolean;
    requirementCandidates?: ComboboxOption[];
    requirementSearchLoading?: boolean;
    onSearchSpec?: (query: string) => void;
    onSearchConversation?: (query: string) => void;
    onSearchRequirement?: (query: string) => void;
    onSubmit?: (data: CreateGlossaryTermRequest | UpdateGlossaryTermRequest) => void;
    onCheckConflict?: (term: string, definition: string) => void;
    id?: string;
    class?: string;
  }

  let {
    term = null,
    conflictResult = null,
    conflictLoading = false,
    loading = false,
    specCandidates = [],
    specSearchLoading = false,
    memberCandidates = [],
    conversationCandidates = [],
    conversationSearchLoading = false,
    requirementCandidates = [],
    requirementSearchLoading = false,
    onSearchSpec,
    onSearchConversation,
    onSearchRequirement,
    onSubmit,
    onCheckConflict,
    id: formId = 'glossary-form',
    class: className = '',
  }: Props = $props();

  const isEditing = $derived(!!term);

  // CollapsibleGroup expanded states
  let basicExpanded = $state(true);
  let referencesExpanded = $state(true);

  // Helper to build form data from a term (avoids state_referenced_locally warning)
  function buildFormData(t?: GlossaryTermDetail | null) {
    return {
      term: t?.term ?? '',
      definition: t?.definition ?? '',
      category: (t?.category ?? 'Business') as TermCategory,
      source: t?.source ?? '',
      synonyms: t?.synonyms ?? '',
      abbreviation: t?.abbreviation ?? '',
      usageExamples: [...(t?.usageExamples ?? [])] as string[],
      relatedTermIds: [...(t?.relatedTermIds ?? [])] as string[],
      sourceSpecId: t?.sourceSpecId ?? '',
      sourceConversationId: t?.sourceConversationId ?? '',
      sourceRequirementId: t?.sourceRequirementId ?? '',
      ownerUserId: t?.owner?.id ?? '',
    };
  }

  // Form state - mutable $state for user interaction (initialized empty, $effect syncs from prop)
  let formData = $state(buildFormData());

  // Sync form when term prop changes (edit mode: partial → full detail)
  $effect(() => {
    if (!term) return;
    formData = buildFormData(term);
  });

  let newExample = $state('');

  const isFormValid = $derived(calcIsFormValid(formData.term, formData.definition));

  function handleSubmit() {
    if (!isFormValid || loading) return;

    const data: CreateGlossaryTermRequest | UpdateGlossaryTermRequest = buildCreateRequest({
      term: formData.term,
      definition: formData.definition,
      category: formData.category,
      source: formData.source,
      synonyms: formData.synonyms,
      abbreviation: formData.abbreviation,
      usageExamples: formData.usageExamples,
      relatedTermIds: formData.relatedTermIds,
      sourceSpecId: formData.sourceSpecId,
      sourceConversationId: formData.sourceConversationId,
      sourceRequirementId: formData.sourceRequirementId,
      ownerUserId: formData.ownerUserId,
    });

    onSubmit?.(data);
  }

  function handleCheckConflict() {
    if (formData.term.trim()) {
      onCheckConflict?.(formData.term.trim(), formData.definition.trim());
    }
  }

  function addExample() {
    if (newExample.trim()) {
      formData.usageExamples = [...formData.usageExamples, newExample.trim()];
      newExample = '';
    }
  }

  function removeExample(index: number) {
    formData.usageExamples = formData.usageExamples.filter((_, i) => i !== index);
  }

  const categories = TERM_CATEGORIES;
</script>

<form id={formId} novalidate class="flex flex-col gap-4 {className}" onsubmit={(e) => { e.preventDefault(); handleSubmit(); }}>
  <!-- Basic Information -->
  <CollapsibleGroup
    title="Basic Information"
    variant="plain"
    expanded={basicExpanded}
    onToggle={() => basicExpanded = !basicExpanded}
  >
    <div class="flex flex-col gap-4 pl-5 pt-2 pb-3">
      <!-- Term Name -->
      {#if !isEditing}
        <div class="flex gap-2 items-end">
          <div class="flex-1">
            <Input
              label="Term"
              required
              bind:value={formData.term}
              placeholder="Enter term name..."
            />
          </div>
          {#if onCheckConflict}
            <Button
              variant="secondary"
              size="sm"
              onclick={handleCheckConflict}
              disabled={!formData.term.trim() || conflictLoading}
            >
              {#if conflictLoading}
                Checking...
              {:else}
                Check Conflicts
              {/if}
            </Button>
          {/if}
        </div>
        {#if conflictResult?.hasConflict}
          <div class="mt-2 p-2 bg-yellow-50 dark:bg-yellow-950 border border-yellow-200 dark:border-yellow-800 rounded-lg">
            <div class="flex items-center gap-2 text-yellow-700 dark:text-yellow-300 text-sm font-medium mb-1">
              <Icon name="alert-triangle" size="sm" />
              Conflicts detected
            </div>
            <ul class="text-xs text-yellow-600 dark:text-yellow-400 space-y-1">
              {#each conflictResult.conflicts as conflict (conflict.message)}
                <li>- {conflict.message}</li>
              {/each}
            </ul>
          </div>
        {/if}
      {:else if formData.term}
        <div>
          <dt class="text-xs text-[var(--color-text-muted)] mb-1">Term</dt>
          <dd class="text-sm font-medium text-[var(--color-text-primary)]">{formData.term}</dd>
        </div>
      {/if}

      <!-- Definition -->
      <Textarea
        label="Definition"
        required
        bind:value={formData.definition}
        placeholder="Enter definition (Markdown supported)..."
        rows={4}
        hint="Markdown supported"
      />

      <!-- Category -->
      <div>
        <span class="block text-xs text-[var(--color-text-muted)] mb-1">
          Category
        </span>
        <div class="flex flex-wrap gap-2">
          {#each categories as cat (cat)}
            {@const style = TERM_CATEGORY_STYLES[cat]}
            <Button
              variant="ghost"
              size="sm"
              onclick={() => (formData.category = cat)}
              class="rounded-lg border {formData.category === cat
                ? `${style.bgColor} ${style.color} ${style.borderColor}`
                : 'border-transparent'}"
            >
              <Icon name={style.icon} size="sm" class="inline mr-1 {formData.category !== cat ? style.color : ''}" />
              {style.label}
            </Button>
          {/each}
        </div>
      </div>

      <!-- Abbreviation & Synonyms -->
      <div class="grid grid-cols-2 gap-4">
        <Input
          label="Abbreviation"
          bind:value={formData.abbreviation}
          placeholder="e.g., API, SDK"
        />
        <Input
          label="Synonyms"
          bind:value={formData.synonyms}
          placeholder="comma separated"
        />
      </div>

      <!-- Source -->
      <Input
        label="Source"
        bind:value={formData.source}
        placeholder="Reference document or URL..."
      />

      <!-- Owner -->
      <Combobox
        label="Owner"
        options={memberCandidates}
        bind:value={formData.ownerUserId}
        placeholder="Search by name..."
        hint="Person responsible for maintaining this term"
      />

      <!-- Usage Examples -->
      <div>
        <span class="block text-xs text-[var(--color-text-muted)] mb-1">
          Usage Examples
        </span>
        <div class="space-y-2">
          {#each formData.usageExamples as example, i (i)}
            <div class="flex items-center gap-2">
              <span class="flex-1 px-3 py-2 text-sm bg-[var(--color-bg-tertiary)] border border-[var(--color-border-secondary)] rounded-lg text-[var(--color-text-primary)]">
                {example}
              </span>
              <IconButton
                icon="x"
                variant="ghost"
                size="sm"
                title="Remove example"
                onclick={() => removeExample(i)}
              />
            </div>
          {/each}
          <div class="flex gap-2 items-end">
            <div class="flex-1">
              <Input
                bind:value={newExample}
                placeholder="Add usage example..."
                onkeydown={(e) => e.key === 'Enter' && (e.preventDefault(), addExample())}
              />
            </div>
            <Button
              variant="secondary"
              size="sm"
              onclick={addExample}
              disabled={!newExample.trim()}
            >
              Add
            </Button>
          </div>
        </div>
      </div>
    </div>
  </CollapsibleGroup>

  <!-- References -->
  {#if onSearchConversation || onSearchRequirement || onSearchSpec}
    <CollapsibleGroup
      title="References"
      variant="plain"
      allowOverflow
      expanded={referencesExpanded}
      onToggle={() => referencesExpanded = !referencesExpanded}
    >
      <div class="flex flex-col gap-4 pl-5 pt-2 pb-3">
        {#if onSearchConversation}
          <Combobox
            label="Source Conversation"
            options={conversationCandidates}
            bind:value={formData.sourceConversationId}
            placeholder="Search by name..."
            hint="Link a conversation where this term was discussed"
            onsearch={onSearchConversation}
            loading={conversationSearchLoading}
          />
        {/if}
        {#if onSearchRequirement}
          <Combobox
            label="Source Requirement"
            options={requirementCandidates}
            bind:value={formData.sourceRequirementId}
            placeholder="Search by code or title..."
            hint="Link a requirement that defines this term"
            onsearch={onSearchRequirement}
            loading={requirementSearchLoading}
          />
        {/if}
        {#if onSearchSpec}
          <Combobox
            label="Source Spec"
            options={specCandidates}
            bind:value={formData.sourceSpecId}
            placeholder="Search by code or title..."
            hint="Link a spec that defines this term"
            onsearch={onSearchSpec}
            loading={specSearchLoading}
          />
        {/if}
      </div>
    </CollapsibleGroup>
  {/if}

</form>
