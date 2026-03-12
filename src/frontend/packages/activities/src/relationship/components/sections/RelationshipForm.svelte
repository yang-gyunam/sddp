<!-- Section: RelationshipForm -->
<!--
  RelationshipForm Component
  Form for creating/editing relationships between entities
-->
<script lang="ts">
  import { Icon, IconButton, Button, Textarea, Select, Combobox } from '@sddp/ui';
  import type { ComboboxOption } from '@sddp/ui';
  import { formatPercent } from '@sddp/shell';
  import type { EntityType, RelationType, CreateRelationshipRequest } from '../../types';
  import {
    ENTITY_TYPE_STYLES,
    RELATION_TYPE_STYLES,
    getAvailableRelationTypes,
  } from '../../types';

  interface Props {
    /** Source entity type (fixed) */
    fromEntityType: EntityType;
    /** Source entity ID (fixed) */
    fromEntityId: string;
    /** Source entity label for display */
    fromEntityLabel?: string;
    /** Initial target entity type */
    initialToEntityType?: EntityType;
    /** Initial target entity ID */
    initialToEntityId?: string;
    /** Initial relationship type */
    initialType?: RelationType;
    /** Initial reason */
    initialReason?: string;
    /** Loading state */
    loading?: boolean;
    /** Error message */
    error?: string | null;
    /** Candidate options for target entity search */
    entityCandidates?: ComboboxOption[];
    /** Called when user types in entity search */
    onSearchEntity?: (entityType: EntityType, query: string) => void;
    /** Loading state for entity search */
    entitySearchLoading?: boolean;
    /** Called when form is submitted */
    onSubmit?: (request: CreateRelationshipRequest) => void;
    /** Called when form is cancelled */
    onCancel?: () => void;
    class?: string;
  }

  let {
    fromEntityType,
    fromEntityId,
    fromEntityLabel,
    initialToEntityType = 'Spec',
    initialToEntityId = '',
    initialType = 'DependsOn',
    initialReason = '',
    loading = false,
    error = null,
    entityCandidates = [],
    onSearchEntity,
    entitySearchLoading = false,
    onSubmit,
    onCancel,
    class: className = '',
  }: Props = $props();

  const formId = `relationship-form-${Math.random().toString(36).substring(2, 9)}`;
  const toTypeId = `${formId}-to-type`;
  const toEntityIdFieldId = `${formId}-to-entity-id`;
  const strengthId = `${formId}-strength`;
  const reasonId = `${formId}-reason`;

  // Form state
  let toEntityType = $state<EntityType>('Spec');
  let toEntityId = $state('');
  let relationType = $state<RelationType>('DependsOn');
  let reason = $state('');
  let strength = $state(50);
  const strengthLabel = $derived(
    formatPercent(strength / 100, { maximumFractionDigits: 0 })
  );
  const fromStyle = $derived(ENTITY_TYPE_STYLES[fromEntityType]);

  // Available relationship types based on entity types
  const availableTypes = $derived(getAvailableRelationTypes(fromEntityType, toEntityType));

  // Validation
  const isValid = $derived(toEntityId.trim().length > 0 && availableTypes.includes(relationType));

  // Entity type options
  const entityTypeOptions: { value: EntityType; label: string }[] = [
    { value: 'Spec', label: 'Spec' },
    { value: 'Requirement', label: 'Requirement' },
    { value: 'Conversation', label: 'Conversation' },
    { value: 'GlossaryTerm', label: 'Glossary Term' },
  ];

  $effect(() => {
    toEntityType = initialToEntityType;
    toEntityId = initialToEntityId;
    relationType = initialType;
    reason = initialReason;
  });

  // Reset relationship type if not available
  $effect(() => {
    if (!availableTypes.includes(relationType)) {
      relationType = availableTypes[0] || 'DependsOn';
    }
  });

  function handleSubmit(e: Event) {
    e.preventDefault();
    if (!isValid || loading) return;

    onSubmit?.({
      fromEntityType,
      fromEntityId,
      toEntityType,
      toEntityId: toEntityId.trim(),
      type: relationType,
      strength: strength / 100,
      reason: reason.trim() || undefined,
    });
  }

  function handleReset() {
    toEntityType = initialToEntityType;
    toEntityId = initialToEntityId;
    relationType = initialType;
    reason = initialReason;
    strength = 50;
  }
</script>

<form
  class="flex flex-col gap-4 p-4 bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-700 {className}"
  onsubmit={handleSubmit}
>
  <!-- Header -->
  <div class="flex items-center justify-between">
    <h3 class="text-sm font-semibold text-[var(--color-text-primary)]">
      Create Relationship
    </h3>
    <div class="flex items-center gap-1">
      <IconButton icon="rotate-ccw" variant="ghost" onclick={handleReset} title="Reset" />
      <IconButton icon="check" variant={isValid ? 'success' : 'ghost'} type="submit" disabled={!isValid || loading} title="Create" />
      {#if onCancel}
        <IconButton icon="x" variant="ghost" onclick={onCancel} title="Cancel" />
      {/if}
    </div>
  </div>

  <!-- Source Entity (Read-only) -->
  <div>
    <span class="block text-xs font-medium text-gray-500 dark:text-gray-400 mb-1.5">
      From (Source)
    </span>
    <div class="flex items-center gap-2 px-3 py-2 bg-gray-50 dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700">
      <div class="flex items-center justify-center w-6 h-6 rounded {fromStyle.bgColor}">
        <Icon name={fromStyle.icon} size="sm" class={fromStyle.color} />
      </div>
      <div class="flex-1 min-w-0">
        <span class="text-sm text-gray-700 dark:text-gray-300">
          {fromEntityLabel || `${fromEntityType} #${fromEntityId.slice(-4)}`}
        </span>
      </div>
      <span class="text-xs text-gray-400">{fromEntityType}</span>
    </div>
  </div>

  <!-- Target Entity Type -->
  <div>
    <label for={toTypeId} class="block text-xs font-medium text-gray-500 dark:text-gray-400 mb-1.5">
      To (Target) Entity Type
    </label>
    <Select
      id={toTypeId}
      bind:value={toEntityType}
      options={entityTypeOptions}
      placeholder="Select entity type"
    />
  </div>

  <!-- Target Entity -->
  <div>
    <Combobox
      id={toEntityIdFieldId}
      label="Target Entity"
      options={entityCandidates}
      bind:value={toEntityId}
      placeholder="Search by code or title..."
      hint="Search for the target {toEntityType.toLowerCase()}"
      onsearch={(q) => onSearchEntity?.(toEntityType, q)}
      loading={entitySearchLoading}
      required
    />
  </div>

  <!-- Relationship Type -->
  <div>
    <span class="block text-xs font-medium text-gray-500 dark:text-gray-400 mb-1.5">
      Relationship Type
    </span>
    <div class="grid grid-cols-2 gap-2">
      {#each availableTypes as type (type)}
        {@const style = RELATION_TYPE_STYLES[type]}
        <Button
          variant="unstyled"
          class="flex items-center gap-2 px-3 py-2 rounded-lg border transition-all text-left
            {relationType === type
              ? `${style.bgColor} ${style.borderColor} border-2`
              : 'border-gray-200 dark:border-gray-700 hover:border-gray-300 dark:hover:border-gray-600'}"
          onclick={() => (relationType = type)}
        >
          <Icon name={style.icon} size="sm" class={style.color} />
          <span class="text-sm {relationType === type ? style.color : 'text-gray-600 dark:text-gray-400'}">
            {style.label}
          </span>
        </Button>
      {/each}
    </div>
    {#if relationType}
      <p class="mt-2 text-xs text-gray-500">
        {RELATION_TYPE_STYLES[relationType].description}
      </p>
    {/if}
  </div>

  <!-- Strength -->
  <div>
    <label for={strengthId} class="block text-xs font-medium text-gray-500 dark:text-gray-400 mb-1.5">
      Relationship Strength: {strengthLabel}
    </label>
    <!-- eslint-disable-next-line svelte/no-restricted-html-elements -->
    <input
      id={strengthId}
      type="range"
      bind:value={strength}
      min="0"
      max="100"
      step="10"
      class="w-full h-2 bg-gray-200 dark:bg-gray-700 rounded-lg appearance-none cursor-pointer"
    />
    <div class="flex justify-between text-xs text-gray-400 mt-1">
      <span>Weak</span>
      <span>Strong</span>
    </div>
  </div>

  <!-- Reason -->
  <div>
    <label for={reasonId} class="block text-xs font-medium text-gray-500 dark:text-gray-400 mb-1.5">
      Reason (Optional)
    </label>
    <Textarea
      id={reasonId}
      bind:value={reason}
      placeholder="Why does this relationship exist?"
      rows={2}
    />
  </div>

  <!-- Error -->
  {#if error}
    <div class="flex items-center gap-2 px-3 py-2 bg-red-50 dark:bg-red-950 text-red-600 dark:text-red-400 rounded-lg text-sm">
      <Icon name="alert-circle" size="sm" />
      {error}
    </div>
  {/if}

</form>
