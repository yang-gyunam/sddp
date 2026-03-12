<!--
  SpecForm Component
  Form for creating or editing a spec
-->
<script lang="ts">
  import { Input, Icon, Spinner, Textarea, Combobox, Button } from '@sddp/ui';
  import type { ComboboxOption } from '@sddp/ui';
  import { CollapsibleGroup } from '@sddp/shell';
  import { tick } from 'svelte';
  import type { CreateSpecRequest, UpdateSpecRequest } from '../../types';
  import GlossarySuggestionList from './GlossarySuggestionList.svelte';
  import type { GlossaryTermSummary } from '../../../glossary/types';
  import { autocomplete } from '../../../glossary/services/GlossaryService';

  interface Props {
    mode: 'create' | 'edit';
    initialData?: {
      code?: string;
      title?: string;
      description?: string;
      decision?: string;
      context?: string;
      scope?: string;
      outOfScope?: string;
      definitions?: string;
      acceptanceCriteria?: string;
      owners?: string;
      reviewTrigger?: string;
      requirementId?: string;
      bornFromConversationId?: string;
    };
    tenantId?: string;
    projectId?: string;
    loading?: boolean;
    memberCandidates?: ComboboxOption[];
    requirementCandidates?: ComboboxOption[];
    conversationCandidates?: ComboboxOption[];
    onSearchRequirement?: (query: string) => void;
    requirementSearchLoading?: boolean;
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
    tenantId = '',
    projectId = '',
    loading = false,
    memberCandidates = [],
    requirementCandidates = [],
    conversationCandidates = [],
    onSearchRequirement,
    requirementSearchLoading = false,
    onSearchConversation,
    conversationSearchLoading = false,
    onSubmit,
    id: formId = 'spec-form',
    class: className = '',
  }: Props = $props();

  let code = $state('');
  let title = $state('');
  let description = $state('');
  let decision = $state('');
  let context = $state('');
  let scope = $state('');
  let outOfScope = $state('');
  let definitions = $state('');
  let acceptanceCriteria = $state('');
  let ownerIds = $state<string[]>([]);
  let ownerComboValue = $state('');
  let reviewTrigger = $state('');
  let requirementId = $state('');
  let bornFromConversationId = $state('');

  let definitionsInput: HTMLTextAreaElement | undefined = $state(undefined);
  let glossarySuggestions = $state<GlossaryTermSummary[]>([]);
  let glossarySuggestionsOpen = $state(false);
  let glossarySuggestionsLoading = $state(false);
  let glossarySelectedIndex = $state(0);
  let glossaryQueryRange = $state<{ start: number; end: number; query: string } | null>(null);
  let glossaryRequestToken = 0;
  let glossaryDebounceTimer: ReturnType<typeof setTimeout> | null = $state(null);

  // CollapsibleGroup expanded states (always expanded)
  let basicExpanded = $state(true);
  let scopeExpanded = $state(true);
  let criteriaExpanded = $state(true);
  let governanceExpanded = $state(true);
  let referencesExpanded = $state(true);

  // Sync form data when initialData prop changes
  $effect(() => {
    code = initialData.code || '';
    title = initialData.title || '';
    description = initialData.description || '';
    decision = initialData.decision || '';
    context = initialData.context || '';
    scope = initialData.scope || '';
    outOfScope = initialData.outOfScope || '';
    definitions = initialData.definitions || '';
    acceptanceCriteria = initialData.acceptanceCriteria || '';
    ownerIds = (initialData.owners || '').split(',').map(s => s.trim()).filter(Boolean);
    reviewTrigger = initialData.reviewTrigger || '';
    requirementId = initialData.requirementId || '';
    bornFromConversationId = initialData.bornFromConversationId || '';

    // Auto-expand groups with data in edit mode
    if (mode === 'edit') {
      scopeExpanded = !!(initialData.context || initialData.scope || initialData.outOfScope);
      criteriaExpanded = !!(initialData.definitions || initialData.acceptanceCriteria);
      governanceExpanded = !!(initialData.owners?.trim() || initialData.reviewTrigger);
      referencesExpanded = !!(initialData.requirementId || initialData.bornFromConversationId);
    }
  });

  // Owners: comma-separated string for API
  const owners = $derived(ownerIds.join(', '));

  // Available members excluding already selected owners
  const availableMembers = $derived(
    memberCandidates.filter((m) => !ownerIds.includes(m.value))
  );

  function addOwner(userId: string): void {
    if (!userId || ownerIds.includes(userId)) return;
    ownerIds = [...ownerIds, userId];
    ownerComboValue = '';
  }

  function removeOwner(userId: string): void {
    ownerIds = ownerIds.filter((id) => id !== userId);
  }

  function getOwnerLabel(userId: string): string {
    return memberCandidates.find((m) => m.value === userId)?.label ?? userId;
  }

  const isValid = $derived(
    mode === 'create'
      ? code.trim() !== '' && title.trim() !== '' && decision.trim() !== '' && context.trim() !== '' && acceptanceCriteria.trim() !== ''
      : title.trim() !== ''
  );

  function extractGlossaryQuery(text: string, cursor: number): { start: number; end: number; query: string } | null {
    const uptoCursor = text.slice(0, cursor);
    const atIndex = uptoCursor.lastIndexOf('@');
    if (atIndex === -1) return null;

    const previousChar = atIndex > 0 ? uptoCursor[atIndex - 1] : ' ';
    if (previousChar && !/\s|[([{>.,;:"'`]/.test(previousChar)) {
      return null;
    }

    const query = uptoCursor.slice(atIndex + 1);
    if (!query || /\s/.test(query) || query.includes('}')) return null;

    return { start: atIndex, end: cursor, query };
  }

  function closeGlossarySuggestions(): void {
    if (glossaryDebounceTimer) {
      clearTimeout(glossaryDebounceTimer);
      glossaryDebounceTimer = null;
    }
    glossaryRequestToken += 1;
    glossarySuggestionsOpen = false;
    glossarySuggestionsLoading = false;
    glossarySuggestions = [];
    glossarySelectedIndex = 0;
    glossaryQueryRange = null;
  }

  async function loadGlossarySuggestions(query: string): Promise<void> {
    if (!tenantId || !projectId) {
      closeGlossarySuggestions();
      return;
    }

    const requestToken = ++glossaryRequestToken;
    glossarySuggestionsLoading = true;
    glossarySuggestionsOpen = true;

    const normalizedQuery = query.replace(/^\{/, '').replace(/\}$/, '');
    if (normalizedQuery.length < 1) {
      closeGlossarySuggestions();
      return;
    }

    try {
      const results = await autocomplete(tenantId, projectId, normalizedQuery, 8);
      if (requestToken !== glossaryRequestToken) return;

      glossarySuggestions = results.filter((term) => term.status === 'Active');
      glossarySelectedIndex = 0;
      glossarySuggestionsOpen = true;
    } catch (error) {
      if (requestToken !== glossaryRequestToken) return;
      console.error('Glossary autocomplete error:', error);
      glossarySuggestions = [];
      glossarySuggestionsOpen = true;
    } finally {
      if (requestToken === glossaryRequestToken) {
        glossarySuggestionsLoading = false;
      }
    }
  }

  function scheduleGlossarySuggestions(query: string): void {
    if (glossaryDebounceTimer) {
      clearTimeout(glossaryDebounceTimer);
    }

    glossaryDebounceTimer = setTimeout(() => {
      glossaryDebounceTimer = null;
      void loadGlossarySuggestions(query);
    }, 200);
  }

  async function handleDefinitionsInput(event: Event): Promise<void> {
    const target = event.currentTarget as HTMLTextAreaElement;
    const cursor = target.selectionStart ?? target.value.length;

    const queryInfo = extractGlossaryQuery(target.value, cursor);
    if (!queryInfo) {
      closeGlossarySuggestions();
      return;
    }

    glossaryQueryRange = queryInfo;
    scheduleGlossarySuggestions(queryInfo.query);
  }

  function handleDefinitionsKeyDown(event: KeyboardEvent): void {
    if (!glossarySuggestionsOpen) return;

    if (event.key === 'ArrowDown') {
      event.preventDefault();
      if (glossarySuggestions.length === 0) return;
      glossarySelectedIndex = (glossarySelectedIndex + 1) % glossarySuggestions.length;
      return;
    }

    if (event.key === 'ArrowUp') {
      event.preventDefault();
      if (glossarySuggestions.length === 0) return;
      glossarySelectedIndex =
        (glossarySelectedIndex - 1 + glossarySuggestions.length) % glossarySuggestions.length;
      return;
    }

    if (event.key === 'Enter' || event.key === 'Tab') {
      if (glossarySuggestions.length === 0) return;
      event.preventDefault();
      const term = glossarySuggestions[glossarySelectedIndex];
      if (term) {
        insertGlossaryTerm(term);
      }
      return;
    }

    if (event.key === 'Escape') {
      event.preventDefault();
      closeGlossarySuggestions();
    }
  }

  function insertGlossaryTerm(term: GlossaryTermSummary): void {
    if (!glossaryQueryRange) return;

    const before = definitions.slice(0, glossaryQueryRange.start);
    const after = definitions.slice(glossaryQueryRange.end);
    const insertText = `@{${term.term}}`;
    const needsSpace = after.length > 0 && !/^\s/.test(after);

    definitions = `${before}${insertText}${needsSpace ? ' ' : ''}${after}`;
    closeGlossarySuggestions();

    if (typeof window !== 'undefined') {
      window.dispatchEvent(
        new CustomEvent('sddp:navigate', {
          detail: {
            type: 'glossary',
            id: term.id,
            label: term.term,
          },
        })
      );
    }

    void tick().then(() => {
      if (!definitionsInput) return;
      const newCursor = before.length + insertText.length + (needsSpace ? 1 : 0);
      definitionsInput.setSelectionRange(newCursor, newCursor);
      definitionsInput.focus();
    });
  }

  function handleSubmit(): void {
    if (!isValid || loading) return;

    if (mode === 'create') {
      const data: CreateSpecRequest = {
        code: code.trim(),
        title: title.trim(),
        description: description.trim(),
        decision: decision.trim(),
        context: context.trim(),
        scope: scope.trim(),
        outOfScope: outOfScope.trim(),
        definitions: definitions.trim(),
        acceptanceCriteria: acceptanceCriteria.trim(),
        owners: owners.trim(),
        reviewTrigger: reviewTrigger.trim(),
      };
      if (requirementId.trim()) {
        data.requirementId = requirementId.trim();
      }
      if (bornFromConversationId.trim()) {
        data.bornFromConversationId = bornFromConversationId.trim();
      }
      onSubmit(data);
    } else {
      const data: UpdateSpecRequest = {
        title: title.trim(),
        description: description.trim(),
        decision: decision.trim(),
        context: context.trim(),
        scope: scope.trim(),
        outOfScope: outOfScope.trim(),
        definitions: definitions.trim(),
        acceptanceCriteria: acceptanceCriteria.trim(),
        owners: owners.trim(),
        reviewTrigger: reviewTrigger.trim(),
        requirementId: requirementId.trim() || undefined,
        bornFromConversationId: bornFromConversationId.trim() || undefined,
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
          placeholder="e.g., SPEC-001"
          hint="Unique identifier for this spec"
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
        placeholder="Enter spec title..."
      />

      <Textarea
        label="Description"
        bind:value={description}
        placeholder="Describe the problem or requirement..."
        rows={3}
      />

      <Textarea
        label="Decision"
        required
        bind:value={decision}
        placeholder="What was decided and why..."
        rows={3}
        hint="The chosen solution and rationale"
      />
    </div>
  </CollapsibleGroup>

  <!-- Scope & Context -->
  <CollapsibleGroup
    title="Scope & Context"
    variant="plain"
    expanded={scopeExpanded}
    onToggle={() => scopeExpanded = !scopeExpanded}
  >
    <div class="flex flex-col gap-4 pl-5 pt-2 pb-3">
      <Textarea
        label="Context"
        required
        bind:value={context}
        placeholder="Background information and constraints..."
        rows={2}
      />
      <Textarea
        label="Scope"
        bind:value={scope}
        placeholder="What is included in this spec..."
        rows={2}
      />
      <Textarea
        label="Out of Scope"
        bind:value={outOfScope}
        placeholder="What is explicitly NOT included..."
        rows={2}
      />
    </div>
  </CollapsibleGroup>

  <!-- Criteria & Definitions -->
  <CollapsibleGroup
    title="Criteria & Definitions"
    variant="plain"
    expanded={criteriaExpanded}
    onToggle={() => criteriaExpanded = !criteriaExpanded}
  >
    <div class="flex flex-col gap-4 pl-5 pt-2 pb-3">
      <!-- Definitions (custom: glossary autocomplete with element ref) -->
      <div>
        <div class="relative">
          <Textarea
            label="Definitions"
            id="definitions"
            bind:element={definitionsInput}
            bind:value={definitions}
            placeholder="Key terms and concepts used in this spec... (Type @ to link glossary terms)"
            rows={2}
            hint="Define any domain-specific terms or concepts. Use @ to link glossary terms."
            oninput={handleDefinitionsInput}
            onkeydown={handleDefinitionsKeyDown}
            onblur={closeGlossarySuggestions}
          />

          {#if glossarySuggestionsOpen}
            <div
              class="absolute z-20 mt-1 w-full"
              role="presentation"
              aria-hidden="true"
              onmousedown={(event) => event.preventDefault()}
            >
              {#if glossarySuggestionsLoading}
                <div class="bg-[var(--color-bg-primary)] border border-[var(--color-border-primary)] rounded-lg shadow-lg p-3">
                  <div class="flex-1 flex items-center justify-center">
                    <Spinner size="lg" />
                  </div>
                </div>
              {:else}
                <GlossarySuggestionList
                  items={glossarySuggestions}
                  selectedIndex={glossarySelectedIndex}
                  onSelect={insertGlossaryTerm}
                />
              {/if}
            </div>
          {/if}
        </div>
      </div>

      <Textarea
        label="Acceptance Criteria"
        required
        bind:value={acceptanceCriteria}
        placeholder="Criteria to verify completion..."
        rows={3}
        hint="Measurable conditions that must be met for this spec to be considered complete"
      />
    </div>
  </CollapsibleGroup>

  <!-- Governance -->
  <CollapsibleGroup
    title="Governance"
    variant="plain"
    expanded={governanceExpanded}
    onToggle={() => governanceExpanded = !governanceExpanded}
  >
    <div class="flex flex-col gap-4 pl-5 pt-2 pb-3">
      <div>
        <Combobox
          label="Owners"
          options={availableMembers}
          bind:value={ownerComboValue}
          placeholder="Search by name..."
          hint="Select members responsible for this spec"
          onchange={(value) => addOwner(value)}
        />
        {#if ownerIds.length > 0}
          <div class="flex flex-wrap gap-1.5 mt-2">
            {#each ownerIds as userId (userId)}
              <span class="inline-flex items-center gap-1 px-2 py-0.5 rounded-md bg-[var(--color-surface-200)] text-sm text-[var(--color-text-primary)]">
                <Icon name="user" size="xs" class="text-[var(--color-text-muted)]" />
                {getOwnerLabel(userId)}
                <Button
                  variant="unstyled"
                  onclick={() => removeOwner(userId)}
                  class="ml-0.5 text-[var(--color-text-muted)] hover:text-[var(--color-text-primary)] transition-colors"
                >
                  <Icon name="x" size="xs" />
                </Button>
              </span>
            {/each}
          </div>
        {/if}
      </div>
      <Textarea
        label="Review Trigger"
        bind:value={reviewTrigger}
        placeholder="When should this spec be reviewed..."
        rows={2}
        hint="Conditions that should trigger a review of this spec"
      />
    </div>
  </CollapsibleGroup>

  <!-- References -->
    <CollapsibleGroup
      title="References"
      variant="plain"
      expanded={referencesExpanded}
      onToggle={() => referencesExpanded = !referencesExpanded}
    >
      <div class="flex flex-col gap-4 pl-5 pt-2 pb-3">
        <Combobox
          label="Source Conversation"
          options={conversationCandidates}
          bind:value={bornFromConversationId}
          placeholder="Search by name..."
          hint="The conversation that led to this spec"
          onsearch={onSearchConversation}
          loading={conversationSearchLoading}
        />
        <Combobox
          label="Source Requirement"
          options={requirementCandidates}
          bind:value={requirementId}
          placeholder="Search by code or title..."
          hint="Link a requirement that this spec fulfills"
          onsearch={onSearchRequirement}
          loading={requirementSearchLoading}
        />
      </div>
    </CollapsibleGroup>

</form>
