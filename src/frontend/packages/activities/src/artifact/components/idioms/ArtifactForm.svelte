<!--
  ArtifactForm Component
  Form for creating/editing artifact tracking records.
  Follows GlossaryForm pattern with CollapsibleGroup sections.
-->
<script lang="ts">
  import type { ArtifactType } from '../../types';
  import { ARTIFACT_TYPES, ARTIFACT_TYPE_STYLES } from '../../types';
  import type { UpsertArtifactRequest } from '../../services/ArtifactService';
  import { Icon, Button, Input, Combobox } from '@sddp/ui';
  import type { ComboboxOption } from '@sddp/ui';
  import { CollapsibleGroup } from '@sddp/shell';

  interface Props {
    loading?: boolean;
    initialData?: Partial<UpsertArtifactRequest> & { sourceConversationId?: string; sourceRequirementId?: string; glossaryTermId?: string; ownerUserId?: string };
    memberCandidates?: ComboboxOption[];
    conversationCandidates?: ComboboxOption[];
    conversationSearchLoading?: boolean;
    requirementCandidates?: ComboboxOption[];
    requirementSearchLoading?: boolean;
    specCandidates?: ComboboxOption[];
    specSearchLoading?: boolean;
    glossaryCandidates?: ComboboxOption[];
    glossarySearchLoading?: boolean;
    onSearchConversation?: (query: string) => void;
    onSearchRequirement?: (query: string) => void;
    onSearchSpec?: (query: string) => void;
    onSearchGlossary?: (query: string) => void;
    onSubmit?: (data: UpsertArtifactRequest) => void;
    id?: string;
    class?: string;
  }

  let {
    loading = false,
    initialData,
    memberCandidates = [],
    conversationCandidates = [],
    conversationSearchLoading = false,
    requirementCandidates = [],
    requirementSearchLoading = false,
    specCandidates = [],
    specSearchLoading = false,
    glossaryCandidates = [],
    glossarySearchLoading = false,
    onSearchConversation,
    onSearchRequirement,
    onSearchSpec,
    onSearchGlossary,
    onSubmit,
    id: formId = 'artifact-form',
    class: className = '',
  }: Props = $props();

  // CollapsibleGroup state
  let basicExpanded = $state(true);
  let referencesExpanded = $state(true);

  // Form state (hydrated from initialData via $effect)
  // eslint-disable-next-line svelte/prefer-writable-derived
  let formData = $state({
    specId: '',
    artifactType: 'Entity' as ArtifactType,
    entityName: '',
    artifactPath: '',
    sourceConversationId: '',
    sourceRequirementId: '',
    glossaryTermId: '',
    ownerUserId: '',
  });

  // Hydrate form when initialData changes (including on mount)
  $effect(() => {
    formData = {
      specId: initialData?.specId ?? '',
      artifactType: (initialData?.artifactType ?? 'Entity') as ArtifactType,
      entityName: initialData?.entityName ?? '',
      artifactPath: initialData?.artifactPath ?? '',
      sourceConversationId: initialData?.sourceConversationId ?? '',
      sourceRequirementId: initialData?.sourceRequirementId ?? '',
      glossaryTermId: initialData?.glossaryTermId ?? '',
      ownerUserId: initialData?.ownerUserId ?? '',
    };
  });

  const isFormValid = $derived(
    formData.specId.trim().length > 0 &&
    formData.artifactPath.trim().length > 0 &&
    formData.entityName.trim().length > 0
  );

  function handleSubmit() {
    if (!isFormValid || loading) return;

    const data: UpsertArtifactRequest = {
      specId: formData.specId.trim(),
      artifactPath: formData.artifactPath.trim(),
      artifactType: formData.artifactType,
      contentHash: 'pending',
      entityName: formData.entityName.trim() || undefined,
      generatorVersion: '1.0.0',
      templateVersion: '1.0.0',
      glossaryTermId: formData.glossaryTermId.trim() || undefined,
      sourceConversationId: formData.sourceConversationId.trim() || undefined,
      sourceRequirementId: formData.sourceRequirementId.trim() || undefined,
      ownerUserId: formData.ownerUserId.trim() || undefined,
    };

    onSubmit?.(data);
  }
</script>

<form id={formId} novalidate class="flex flex-col gap-4 {className}" onsubmit={(e) => { e.preventDefault(); handleSubmit(); }}>
  <!-- Basic Information -->
  <CollapsibleGroup
    title="Basic Information"
    variant="plain"
    allowOverflow
    expanded={basicExpanded}
    onToggle={() => basicExpanded = !basicExpanded}
  >
    <div class="flex flex-col gap-4 pl-5 pt-2 pb-3">
      <!-- Entity Name -->
      <Input
        label="Entity Name"
        required
        bind:value={formData.entityName}
        placeholder="e.g., Spec, User, Requirement"
        hint="The domain entity this artifact represents"
      />

      <!-- Owner -->
      <Combobox
        label="Owner"
        options={memberCandidates}
        bind:value={formData.ownerUserId}
        placeholder="Search by name..."
        hint="Person responsible for this artifact"
      />

      <!-- Artifact Type (button group) -->
      <div>
        <span class="block text-xs text-[var(--color-text-muted)] mb-1">
          Artifact Type <span class="text-[var(--color-error-500)]">*</span>
        </span>
        <div class="flex flex-wrap gap-2">
          {#each ARTIFACT_TYPES as type (type)}
            {@const style = ARTIFACT_TYPE_STYLES[type]}
            <Button
              variant="ghost"
              size="sm"
              onclick={() => (formData.artifactType = type)}
              class="rounded-lg border {formData.artifactType === type
                ? `${style.bgColor} ${style.color} ${style.borderColor}`
                : 'border-transparent'}"
            >
              <Icon name={style.icon} size="sm" class="inline mr-1 {formData.artifactType !== type ? style.color : ''}" />
              {style.label}
            </Button>
          {/each}
        </div>
      </div>

      <!-- Artifact Path -->
      <Input
        label="Artifact Path"
        required
        mono
        bind:value={formData.artifactPath}
        placeholder="src/backend/Sddp.Domain/Entities/Spec.cs"
        hint="Relative path from repository root"
      />
    </div>
  </CollapsibleGroup>

  <!-- References -->
  <CollapsibleGroup
    title="References"
    variant="plain"
    allowOverflow
    expanded={referencesExpanded}
    onToggle={() => referencesExpanded = !referencesExpanded}
  >
    <div class="flex flex-col gap-4 pl-5 pt-2 pb-3">
      <!-- Source Conversation -->
      <Combobox
        label="Source Conversation"
        options={conversationCandidates}
        bind:value={formData.sourceConversationId}
        placeholder="Search by name..."
        hint="Link a conversation where this artifact was discussed"
        onsearch={onSearchConversation}
        loading={conversationSearchLoading}
      />

      <!-- Source Requirement -->
      <Combobox
        label="Source Requirement"
        options={requirementCandidates}
        bind:value={formData.sourceRequirementId}
        placeholder="Search by code or title..."
        hint="Link a requirement this artifact fulfills"
        onsearch={onSearchRequirement}
        loading={requirementSearchLoading}
      />

      <!-- Source Spec -->
      <Combobox
        label="Source Spec"
        required
        options={specCandidates}
        bind:value={formData.specId}
        placeholder="Search by code or title..."
        hint="The spec this artifact is generated from"
        onsearch={onSearchSpec}
        loading={specSearchLoading}
      />

      <!-- Source Glossary -->
      <Combobox
        label="Source Glossary"
        options={glossaryCandidates}
        bind:value={formData.glossaryTermId}
        placeholder="Search glossary terms..."
        hint="Optional glossary term linked to this artifact"
        onsearch={onSearchGlossary}
        loading={glossarySearchLoading}
      />
    </div>
  </CollapsibleGroup>

</form>
