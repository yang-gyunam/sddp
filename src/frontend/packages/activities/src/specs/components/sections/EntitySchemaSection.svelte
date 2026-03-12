<!-- Section: EntitySchemaSection -->
<script lang="ts">
  import { Button, Icon, Checkbox, IconButton, Input, Select, Spinner, Textarea } from '@sddp/ui';
  import type {
    EntityMetadata,
    EntityFieldInput,
    CreateEntityMetadataRequest,
    UpdateEntityMetadataRequest,
    FieldType,
  } from '../../../entities/types';

  interface Props {
    entities: EntityMetadata[];
    loading?: boolean;
    error?: string | null;
    onLoad?: () => void;
    onCreate?: (request: CreateEntityMetadataRequest) => void;
    onUpdate?: (entityId: string, request: UpdateEntityMetadataRequest) => void;
    onDelete?: (entityId: string) => void;
  }

  let {
    entities,
    loading = false,
    error = null,
    onLoad,
    onCreate,
    onUpdate,
    onDelete,
  }: Props = $props();

  type FieldForm = EntityFieldInput & { localId: string };

  const fieldTypeOptions: FieldType[] = [
    'String',
    'Int',
    'Long',
    'Decimal',
    'Boolean',
    'DateTime',
    'Guid',
    'Json',
  ];

  const baseClassOptions = [
    'AuditableEntityBase',
    'EntityBase',
    'VersionedEntityBase',
  ];

  const formId = `entity-schema-${Math.random().toString(36).substring(2, 9)}`;
  const entityNameId = `${formId}-entity-name`;
  const tableNameId = `${formId}-table-name`;
  const namespaceId = `${formId}-namespace`;
  const baseClassId = `${formId}-base-class`;
  const descriptionId = `${formId}-description`;

  let showForm = $state(false);
  let editingEntityId = $state<string | null>(null);
  let formState = $state({
    entityName: '',
    tableName: '',
    namespace: 'Sddp.Domain.Entities',
    baseClass: 'AuditableEntityBase',
    description: '',
    isGenerated: true,
    fields: [] as FieldForm[],
  });

  function openCreateForm(): void {
    editingEntityId = null;
    formState = {
      entityName: '',
      tableName: '',
      namespace: 'Sddp.Domain.Entities',
      baseClass: 'AuditableEntityBase',
      description: '',
      isGenerated: true,
      fields: [],
    };
    showForm = true;
  }

  function openEditForm(entity: EntityMetadata): void {
    editingEntityId = entity.id;
    formState = {
      entityName: entity.entityName,
      tableName: entity.tableName,
      namespace: entity.namespace,
      baseClass: entity.baseClass,
      description: entity.description,
      isGenerated: entity.isGenerated,
      fields: entity.fields.map((field) => ({
        id: field.id,
        fieldName: field.fieldName,
        columnName: field.columnName,
        fieldType: field.fieldType,
        isRequired: field.isRequired,
        isUnique: field.isUnique,
        maxLength: field.maxLength ?? null,
        minLength: field.minLength ?? null,
        validationType: field.validationType ?? '',
        pattern: field.pattern ?? '',
        defaultValue: field.defaultValue ?? '',
        description: field.description ?? '',
        displayOrder: field.displayOrder ?? 0,
        localId: field.id,
      })),
    };
    showForm = true;
  }

  function addField(): void {
    const createLocalId = () => {
      if (typeof crypto !== 'undefined' && 'randomUUID' in crypto) {
        return crypto.randomUUID();
      }
      return `field-${Date.now()}-${Math.random().toString(36).slice(2, 8)}`;
    };
    formState = {
      ...formState,
      fields: [
        ...formState.fields,
        {
          fieldName: '',
          columnName: '',
          fieldType: 'String',
          isRequired: false,
          isUnique: false,
          maxLength: null,
          minLength: null,
          validationType: '',
          pattern: '',
          defaultValue: '',
          description: '',
          displayOrder: formState.fields.length + 1,
          localId: createLocalId(),
        },
      ],
    };
  }

  function removeField(localId: string): void {
    const nextFields = formState.fields.filter((field) => field.localId !== localId);
    formState = {
      ...formState,
      fields: nextFields.map((field, index) => ({
        ...field,
        displayOrder: index + 1,
      })),
    };
  }

  function moveField(localId: string, direction: -1 | 1): void {
    const idx = formState.fields.findIndex((field) => field.localId === localId);
    if (idx < 0) return;
    const nextIndex = idx + direction;
    if (nextIndex < 0 || nextIndex >= formState.fields.length) return;
    const nextFields = [...formState.fields];
    const [moved] = nextFields.splice(idx, 1);
    nextFields.splice(nextIndex, 0, moved!);
    formState = {
      ...formState,
      fields: nextFields.map((field, index) => ({
        ...field,
        displayOrder: index + 1,
      })),
    };
  }

  function updateField(localId: string, updates: Partial<FieldForm>): void {
    formState = {
      ...formState,
      fields: formState.fields.map((field) =>
        field.localId === localId ? { ...field, ...updates } : field
      ),
    };
  }

  function buildRequestPayload(): CreateEntityMetadataRequest {
    return {
      entityName: formState.entityName.trim(),
      tableName: formState.tableName.trim(),
      namespace: formState.namespace.trim(),
      baseClass: formState.baseClass,
      description: formState.description.trim(),
      isGenerated: formState.isGenerated,
      fields: formState.fields.map((field, index) => ({
        id: field.id,
        fieldName: field.fieldName.trim(),
        columnName: field.columnName.trim(),
        fieldType: field.fieldType,
        isRequired: field.isRequired ?? false,
        isUnique: field.isUnique ?? false,
        maxLength: field.maxLength ?? null,
        minLength: field.minLength ?? null,
        validationType: field.validationType?.trim() || undefined,
        pattern: field.pattern?.trim() || undefined,
        defaultValue: field.defaultValue?.trim() || undefined,
        description: field.description?.trim() || '',
        displayOrder: index + 1,
      })),
    };
  }

  function saveEntity(): void {
    const payload = buildRequestPayload();
    if (!payload.entityName || !payload.tableName) return;
    if (editingEntityId) {
      onUpdate?.(editingEntityId, payload);
    } else {
      onCreate?.(payload);
    }
    showForm = false;
  }

  function cancelForm(): void {
    showForm = false;
  }

  function mapFieldTypeToCSharp(type: FieldType, required: boolean): string {
    const map: Record<FieldType, string> = {
      String: 'string',
      Int: 'int',
      Long: 'long',
      Decimal: 'decimal',
      Boolean: 'bool',
      DateTime: 'DateTimeOffset',
      Guid: 'Guid',
      Json: 'string',
    };
    const base = map[type];
    return required ? base : `${base}?`;
  }

  const previewCode = $derived.by(() => {
    if (!showForm || !formState.entityName.trim()) return '';
    const lines = formState.fields
      .filter((field) => field.fieldName.trim())
      .map((field) => {
        const type = mapFieldTypeToCSharp(field.fieldType, field.isRequired ?? false);
        return `  public ${type} ${field.fieldName.trim()} { get; set; }`;
      });
    return [
      `namespace ${formState.namespace.trim() || 'Sddp.Domain.Entities'};`,
      '',
      `public class ${formState.entityName.trim()} : ${formState.baseClass}`,
      '{',
      ...lines,
      '}',
    ].join('\n');
  });
</script>

<div class="rounded-lg border border-[var(--color-border-secondary)] bg-[var(--color-surface-50)] p-4 space-y-3">
  <div class="flex items-start justify-between gap-3">
    <div>
      <h3 class="text-sm font-medium text-[var(--color-text-secondary)]">Entity Schema</h3>
      <p class="text-xs text-[var(--color-text-muted)]">
        Define entities and fields to generate code and docs.
      </p>
    </div>
    <div class="flex items-center gap-2">
      <Button variant="ghost" size="sm" onclick={openCreateForm}>
        <Icon name="plus" size="sm" />
        Add Entity
      </Button>
      <Button variant="ghost" size="sm" onclick={() => onLoad?.()}>
        <Icon name="refresh-cw" size="sm" />
        Refresh
      </Button>
    </div>
  </div>

  {#if showForm}
    <div class="rounded-lg border border-[var(--color-border-secondary)] bg-[var(--color-surface-100)] p-3 space-y-4">
      <div class="flex items-center justify-between pb-2 border-b border-[var(--color-border-secondary)]">
        <span class="text-xs font-semibold text-[var(--color-text-primary)]">
          {editingEntityId ? 'Edit Entity' : 'New Entity'}
        </span>
        <div class="flex items-center gap-1">
          <IconButton icon="check" size="sm" variant={formState.entityName.trim() && formState.tableName.trim() ? 'success' : 'ghost'} onclick={saveEntity} disabled={!formState.entityName.trim() || !formState.tableName.trim()} title={editingEntityId ? 'Update' : 'Create'} />
          <IconButton icon="x" size="sm" variant="ghost" onclick={cancelForm} title="Cancel" />
        </div>
      </div>
      <div class="grid gap-3 md:grid-cols-2">
        <div>
          <label for={entityNameId} class="block text-xs font-medium text-[var(--color-text-secondary)] mb-1">Entity Name</label>
          <Input
            unstyled
            id={entityNameId}
            bind:value={formState.entityName}
            placeholder="Employee"
            class="w-full px-3 py-2 text-sm rounded-lg border border-[var(--color-border-secondary)] bg-[var(--color-surface-50)]"
          />
        </div>
        <div>
          <label for={tableNameId} class="block text-xs font-medium text-[var(--color-text-secondary)] mb-1">Table Name</label>
          <Input
            unstyled
            id={tableNameId}
            bind:value={formState.tableName}
            placeholder="employees"
            class="w-full px-3 py-2 text-sm rounded-lg border border-[var(--color-border-secondary)] bg-[var(--color-surface-50)]"
          />
        </div>
        <div>
          <label for={namespaceId} class="block text-xs font-medium text-[var(--color-text-secondary)] mb-1">Namespace</label>
          <Input
            unstyled
            id={namespaceId}
            bind:value={formState.namespace}
            class="w-full px-3 py-2 text-sm rounded-lg border border-[var(--color-border-secondary)] bg-[var(--color-surface-50)]"
          />
        </div>
        <div>
          <label for={baseClassId} class="block text-xs font-medium text-[var(--color-text-secondary)] mb-1">Base Class</label>
          <Select
            unstyled
            id={baseClassId}
            bind:value={formState.baseClass}
            class="w-full px-3 py-2 text-sm rounded-lg border border-[var(--color-border-secondary)] bg-[var(--color-surface-50)]"
          >
            {#each baseClassOptions as baseClass (baseClass)}
              <option value={baseClass}>{baseClass}</option>
            {/each}
          </Select>
        </div>
        <div class="md:col-span-2">
          <label for={descriptionId} class="block text-xs font-medium text-[var(--color-text-secondary)] mb-1">Description</label>
          <Textarea
            id={descriptionId}
            rows={2}
            bind:value={formState.description}
            resize="none"
          />
        </div>
      </div>

      <div class="flex items-center gap-2 text-xs">
        <Checkbox bind:checked={formState.isGenerated} label="Include in generation" />
      </div>

      <div class="space-y-2">
        <div class="flex items-center justify-between">
          <h4 class="text-xs font-medium text-[var(--color-text-secondary)]">Fields</h4>
          <Button variant="ghost" size="sm" onclick={addField}>
            <Icon name="plus" size="sm" />
            Add Field
          </Button>
        </div>
        {#if formState.fields.length === 0}
          <div class="text-xs text-[var(--color-text-tertiary)]">No fields added.</div>
        {:else}
          <div class="space-y-2">
            {#each formState.fields as field (field.localId)}
              <div class="grid gap-2 md:grid-cols-[1.2fr_1.2fr_0.8fr_0.6fr_0.6fr_auto] items-center">
                <Input
                  unstyled
                  value={field.fieldName}
                  placeholder="FieldName"
                  class="w-full px-2 py-1 text-sm rounded border border-[var(--color-border-secondary)] bg-[var(--color-surface-50)]"
                  oninput={(e) => updateField(field.localId, { fieldName: (e.target as HTMLInputElement).value })}
                />
                <Input
                  unstyled
                  value={field.columnName}
                  placeholder="column_name"
                  class="w-full px-2 py-1 text-sm rounded border border-[var(--color-border-secondary)] bg-[var(--color-surface-50)]"
                  oninput={(e) => updateField(field.localId, { columnName: (e.target as HTMLInputElement).value })}
                />
                <Select
                  unstyled
                  value={field.fieldType}
                  class="w-full px-2 py-1 text-sm rounded border border-[var(--color-border-secondary)] bg-[var(--color-surface-50)]"
                  onchange={(value) => updateField(field.localId, { fieldType: value as FieldType })}
                >
                  {#each fieldTypeOptions as option (option)}
                    <option value={option}>{option}</option>
                  {/each}
                </Select>
                <label class="flex items-center gap-1 text-xs text-[var(--color-text-tertiary)]">
                  <Checkbox
                    unstyled
                    checked={field.isRequired}
                    onchange={(checked) => updateField(field.localId, { isRequired: checked })}
                  />
                  Req
                </label>
                <label class="flex items-center gap-1 text-xs text-[var(--color-text-tertiary)]">
                  <Checkbox
                    unstyled
                    checked={field.isUnique}
                    onchange={(checked) => updateField(field.localId, { isUnique: checked })}
                  />
                  Unique
                </label>
                <div class="flex items-center gap-1">
                  <IconButton icon="chevron-up" variant="ghost" size="sm" onclick={() => moveField(field.localId, -1)} title="Move up" />
                  <IconButton icon="chevron-down" variant="ghost" size="sm" onclick={() => moveField(field.localId, 1)} title="Move down" />
                  <IconButton icon="trash" variant="danger" size="sm" onclick={() => removeField(field.localId)} title="Remove field" />
                </div>
              </div>
            {/each}
          </div>
        {/if}
      </div>

      {#if previewCode}
        <div>
          <h4 class="text-xs font-medium text-[var(--color-text-secondary)] mb-2">Generated Preview</h4>
          <pre class="text-xs bg-[var(--color-surface-200)] border border-[var(--color-border-secondary)] rounded p-3 overflow-auto">{previewCode}</pre>
        </div>
      {/if}

    </div>
  {/if}

  {#if loading}
    <div class="flex-1 flex items-center justify-center">
      <Spinner size="lg" />
    </div>
  {:else if error}
    <div class="text-xs text-[var(--color-error-600)]">{error}</div>
  {:else if entities.length === 0}
    <div class="text-xs text-[var(--color-text-tertiary)]">No entities defined yet.</div>
  {:else}
    <div class="space-y-3">
      {#each entities as entity (entity.id)}
        <div class="rounded-lg border border-[var(--color-border-secondary)] bg-[var(--color-surface-100)] p-3 space-y-2">
          <div class="flex items-start justify-between gap-2">
            <div>
              <div class="text-sm font-medium text-[var(--color-text-primary)]">
                {entity.entityName}
                <span class="text-xs text-[var(--color-text-tertiary)]">({entity.tableName})</span>
              </div>
              <div class="text-xs text-[var(--color-text-tertiary)]">{entity.description || 'No description'}</div>
            </div>
            <div class="flex items-center gap-2">
              <Button variant="ghost" size="sm" onclick={() => openEditForm(entity)}>
                <Icon name="edit" size="sm" />
                Edit
              </Button>
              <Button variant="ghost" size="sm" onclick={() => onDelete?.(entity.id)}>
                <Icon name="trash-2" size="sm" />
              </Button>
            </div>
          </div>
          {#if entity.fields.length > 0}
            <div class="grid gap-2 md:grid-cols-2">
              {#each entity.fields as field (field.id)}
                <div class="rounded border border-[var(--color-border-secondary)] bg-[var(--color-surface-50)] px-2 py-1">
                  <div class="text-xs font-medium text-[var(--color-text-primary)]">
                    {field.fieldName}
                  </div>
                  <div class="text-[0.625rem] text-[var(--color-text-tertiary)]">
                    {field.fieldType}{field.isRequired ? ' · required' : ''}
                  </div>
                </div>
              {/each}
            </div>
          {:else}
            <div class="text-xs text-[var(--color-text-tertiary)]">No fields defined.</div>
          {/if}
        </div>
      {/each}
    </div>
  {/if}
</div>
