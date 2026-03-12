<!--
  RequirementForm Component
  Form for creating or editing a requirement
  - Create mode: Level is auto-determined from parent selection
  - Edit mode: Priority can be changed
-->
<script lang="ts">
  import { Input, Textarea, Select, Combobox } from '@sddp/ui';
  import type { SelectOption, ComboboxOption } from '@sddp/ui';
  import { CollapsibleGroup } from '@sddp/shell';
  import type {
    RequirementPriority,
    CreateRequirementRequest,
    UpdateRequirementRequest,
  } from '../../types';

  interface Props {
    mode: 'create' | 'edit';
    initialData?: {
      code?: string;
      title?: string;
      description?: string;
      priority?: RequirementPriority;
      parentId?: string;
      ownerUserId?: string;
      conversationId?: string;
    };
    parentCandidates?: ComboboxOption[];
    memberCandidates?: ComboboxOption[];
    conversationCandidates?: ComboboxOption[];
    loading?: boolean;
    onSearchParent?: (query: string) => void;
    parentSearchLoading?: boolean;
    onSearchConversation?: (query: string) => void;
    conversationSearchLoading?: boolean;
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    onSubmit: (data: any) => void | Promise<void>;
    id?: string;
    class?: string;
  }

  let {
    mode,
    initialData = {},
    parentCandidates = [],
    memberCandidates = [],
    conversationCandidates = [],
    loading = false,
    onSearchParent,
    parentSearchLoading = false,
    onSearchConversation,
    conversationSearchLoading = false,
    onSubmit,
    id: formId = 'requirement-form',
    class: className = '',
  }: Props = $props();

  let code = $state('');
  let title = $state('');
  let description = $state('');
  let priority = $state<RequirementPriority>('Medium');
  let parentId = $state('');
  let ownerUserId = $state('');
  let conversationId = $state('');

  // CollapsibleGroup expanded states (always expanded)
  let basicExpanded = $state(true);
  let referencesExpanded = $state(true);

  // Sync form data when initialData prop changes
  $effect(() => {
    code = initialData.code || '';
    title = initialData.title || '';
    description = initialData.description || '';
    priority = initialData.priority || 'Medium';
    parentId = initialData.parentId || '';
    ownerUserId = initialData.ownerUserId || '';
    conversationId = initialData.conversationId || '';

  });

  const priorityOptions: SelectOption[] = [
    { value: 'Low', label: 'Low' },
    { value: 'Medium', label: 'Medium' },
    { value: 'High', label: 'High' },
    { value: 'Urgent', label: 'Urgent' },
  ];

  // Auto-determine level based on parent selection (display only)
  const autoLevel = $derived.by(() => {
    if (mode !== 'create') return '';
    if (!parentId.trim()) return 'A';
    const parent = parentCandidates.find((c) => c.value === parentId);
    if (!parent?.description) return '';
    // description contains "Level X" from flattenTreeToOptions
    if (parent.description.includes('Level A')) return 'B';
    if (parent.description.includes('Level B')) return 'C';
    return '';
  });

  const autoLevelDescription = $derived(
    autoLevel === 'A'
      ? 'Business requirement (top-level)'
      : autoLevel === 'B'
        ? 'User behavior requirement'
        : autoLevel === 'C'
          ? 'Detailed specification'
          : ''
  );

  const isValid = $derived(
    mode === 'create'
      ? code.trim() !== '' && title.trim() !== ''
      : title.trim() !== ''
  );

  function handlePriorityChange(value: string): void {
    priority = value as RequirementPriority;
  }

  function handleSubmit(): void {
    if (!isValid || loading) return;

    if (mode === 'create') {
      const data: CreateRequirementRequest = {
        code: code.trim(),
        title: title.trim(),
        description: description.trim(),
        priority,
      };
      if (parentId.trim()) {
        data.parentId = parentId.trim();
      }
      if (ownerUserId.trim()) {
        data.ownerUserId = ownerUserId.trim();
      }
      if (conversationId.trim()) {
        data.conversationId = conversationId.trim();
      }
      onSubmit(data);
    } else {
      const data: UpdateRequirementRequest = {
        title: title.trim(),
        description: description.trim(),
        priority,
        parentId: parentId.trim() || undefined,
        ownerUserId: ownerUserId.trim() || undefined,
        conversationId: conversationId.trim() || undefined,
      };
      onSubmit(data);
    }
  }
</script>

<form id={formId} novalidate onsubmit={(e) => { e.preventDefault(); handleSubmit(); }} class="flex flex-col gap-4 {className}">
  <!-- Basic Information -->
  <CollapsibleGroup
    title="Basic Information"
    variant="plain"
    expanded={basicExpanded}
    onToggle={() => basicExpanded = !basicExpanded}
  >
    <div class="flex flex-col gap-4 pl-5 pt-2 pb-3">
      {#if mode === 'create'}
        <Input
          label="Code"
          required
          mono
          bind:value={code}
          placeholder="e.g., REQ-001"
          hint="Unique identifier for this requirement"
        />
      {:else if code}
        <div>
          <dt class="text-xs text-[var(--color-text-muted)] mb-1">Code</dt>
          <dd class="text-sm font-mono text-[var(--color-text-primary)]">{code}</dd>
        </div>
      {/if}

      <Input
        label="Title"
        required
        bind:value={title}
        placeholder="Enter requirement title..."
      />

      <Textarea
        label="Description"
        bind:value={description}
        placeholder="Describe the requirement..."
        rows={4}
      />

      <Select
        label="Priority"
        options={priorityOptions}
        value={priority}
        onchange={handlePriorityChange}
        placeholder=""
      />

      {#if memberCandidates.length > 0}
        <Combobox
          label="Owner"
          options={memberCandidates}
          bind:value={ownerUserId}
          placeholder="Search by name..."
          hint="Person responsible for this requirement"
        />
      {/if}
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
        bind:value={conversationId}
        placeholder="Search by name..."
        hint="Link a conversation to this requirement"
        onsearch={onSearchConversation}
        loading={conversationSearchLoading}
      />

      <!-- Parent Requirement -->
      <Combobox
        label="Parent Requirement"
        options={parentCandidates}
        bind:value={parentId}
        placeholder="Search by code or title..."
        hint="Leave empty for top-level requirement"
        onsearch={onSearchParent}
        loading={parentSearchLoading}
      />

      <!-- Auto-determined Level (create only) -->
      {#if mode === 'create' && autoLevel}
        <div class="flex items-center gap-2 px-3 py-2 rounded-lg bg-[var(--color-bg-tertiary)] border border-[var(--color-border-secondary)]">
          <span class="text-xs font-medium text-[var(--color-text-secondary)]">Level:</span>
          <span class="text-xs font-bold text-[var(--color-text-primary)]">{autoLevel}</span>
          {#if autoLevelDescription}
            <span class="text-xs text-[var(--color-text-muted)]">({autoLevelDescription})</span>
          {/if}
        </div>
      {/if}
    </div>
  </CollapsibleGroup>

</form>
