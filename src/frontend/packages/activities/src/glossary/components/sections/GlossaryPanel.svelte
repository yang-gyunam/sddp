<!-- Section: GlossaryPanel — Glossary > glossary-{id} (menu-registry) -->
<script lang="ts">
  import type {
    GlossaryTerm,
    GlossaryTermDetail,
    GlossaryTermSuggestion,
    GlossaryTermUsage,
    TermCategory,
    GlossaryTermStatus,
    CreateGlossaryTermRequest,
    UpdateGlossaryTermRequest,
    GlossaryConflictResult,
  } from '../../types';
  import { GlossarySearch, GlossaryForm } from '../idioms';
  import GlossaryList from './GlossaryList.svelte';
  import GlossaryDetail from './GlossaryDetail.svelte';
  import { Button, Icon, IconButton, Spinner } from '@sddp/ui';
  import { getTabState, setTabState } from '@sddp/shell';

  type ViewMode = 'list' | 'detail' | 'create' | 'edit';

  interface Props {
    terms: GlossaryTerm[];
    currentTerm?: GlossaryTermDetail | null;
    suggestions?: GlossaryTermSuggestion[];
    usage?: GlossaryTermUsage | null;
    conflictResult?: GlossaryConflictResult | null;
    loading?: boolean;
    loadingSuggestions?: boolean;
    loadingUsage?: boolean;
    loadingForm?: boolean;
    searchQuery?: string;
    categoryFilter?: TermCategory | null;
    statusFilter?: GlossaryTermStatus | null;
    totalCount?: number;
    onSearch?: (query: string) => void;
    onQueryChange?: (query: string) => void;
    onCategoryChange?: (category: TermCategory | null) => void;
    onStatusChange?: (status: GlossaryTermStatus | null) => void;
    onSelectTerm?: (term: GlossaryTerm) => void;
    onSelectSuggestion?: (suggestion: GlossaryTermSuggestion) => void;
    onCreate?: (data: CreateGlossaryTermRequest) => void;
    onUpdate?: (data: UpdateGlossaryTermRequest) => void;
    onApprove?: (term: GlossaryTerm) => void;
    onDeprecate?: (term: GlossaryTerm) => void;
    onCheckConflict?: (term: string, definition: string) => void;
    onLoadMore?: () => void;
    tabId?: string;
    class?: string;
  }

  let {
    terms,
    currentTerm = null,
    suggestions = [],
    usage = null,
    conflictResult = null,
    loading = false,
    loadingSuggestions = false,
    loadingUsage = false,
    loadingForm = false,
    searchQuery = '',
    categoryFilter = null,
    statusFilter = null,
    totalCount = 0,
    onSearch,
    onQueryChange,
    onCategoryChange,
    onStatusChange,
    onSelectTerm,
    onSelectSuggestion,
    onCreate,
    onUpdate,
    onApprove,
    onDeprecate,
    onCheckConflict,
    onLoadMore,
    tabId = '',
    class: className = '',
  }: Props = $props();

  let viewMode = $state<ViewMode>('list');
  let selectedTermId = $state<string | null>(null);

  // Tab state persistence
  interface GlossaryPanelTabState {
    viewMode: ViewMode;
    selectedTermId: string | null;
  }
  const tabStateKey = $derived(tabId || 'glossary-panel');
  let restoredKey = $state<string | null>(null);
  let isRestored = $state(false);

  $effect(() => {
    if (!tabStateKey || restoredKey === tabStateKey) return;
    const saved = getTabState<GlossaryPanelTabState>(tabStateKey);
    if (saved) {
      viewMode = saved.viewMode ?? 'list';
      selectedTermId = saved.selectedTermId ?? null;
    }
    restoredKey = tabStateKey;
    isRestored = true;
  });

  $effect(() => {
    if (!tabStateKey || !isRestored) return;
    setTabState<GlossaryPanelTabState>(tabStateKey, { viewMode, selectedTermId });
  });

  // Auto-switch to detail view when currentTerm is loaded
  $effect(() => {
    if (currentTerm && viewMode === 'list') {
      viewMode = 'detail';
    }
  });

  function handleSelectTerm(term: GlossaryTerm) {
    selectedTermId = term.id;
    onSelectTerm?.(term);
  }

  function handleSelectSuggestion(suggestion: GlossaryTermSuggestion) {
    selectedTermId = suggestion.id;
    onSelectSuggestion?.(suggestion);
  }

  function handleCreateClick() {
    viewMode = 'create';
  }

  function handleEditClick() {
    viewMode = 'edit';
  }

  function handleBackToList() {
    viewMode = 'list';
    selectedTermId = null;
  }

  function handleFormSubmit(data: CreateGlossaryTermRequest | UpdateGlossaryTermRequest) {
    if (viewMode === 'create') {
      onCreate?.(data as CreateGlossaryTermRequest);
    } else if (viewMode === 'edit') {
      onUpdate?.(data as UpdateGlossaryTermRequest);
    }
  }


  function handleApprove() {
    if (currentTerm) {
      onApprove?.(currentTerm as GlossaryTerm);
    }
  }

  function handleDeprecate() {
    if (currentTerm) {
      onDeprecate?.(currentTerm as GlossaryTerm);
    }
  }

  const hasMore = $derived(terms.length < totalCount);
</script>

<div class="flex flex-col h-full {className}">
  <!-- Header -->
  <div class="flex items-center justify-between p-4 border-b border-gray-200 dark:border-gray-700">
    <div class="flex items-center gap-2">
      {#if viewMode !== 'list'}
        <IconButton
          icon="arrow-left"
          size="sm"
          variant="ghost"
          title="Back to list"
          onclick={handleBackToList}
        />
      {/if}
      <h2 class="text-lg font-semibold text-gray-800 dark:text-gray-200">
        {#if viewMode === 'create'}
          New Term
        {:else if viewMode === 'edit'}
          Edit Term
        {:else if viewMode === 'detail' && currentTerm}
          {currentTerm.term}
        {:else}
          Glossary
        {/if}
      </h2>
    </div>

    {#if viewMode === 'list'}
      <Button
        variant="primary"
        size="sm"
        onclick={handleCreateClick}
      >
        <Icon name="plus" size="sm" />
        New Term
      </Button>
    {/if}
  </div>

  <!-- Content -->
  <div class="flex-1 overflow-hidden">
    {#if viewMode === 'list'}
      <div class="flex flex-col h-full">
        <!-- Search -->
        <div class="p-4 border-b border-gray-200 dark:border-gray-700">
          <GlossarySearch
            value={searchQuery}
            {suggestions}
            {loadingSuggestions}
            {categoryFilter}
            {statusFilter}
            {onSearch}
            {onQueryChange}
            onSelectSuggestion={handleSelectSuggestion}
            {onCategoryChange}
            {onStatusChange}
          />
        </div>

        <!-- List -->
        <div class="flex-1 overflow-y-auto p-4">
          {#if loading && terms.length === 0}
            <div class="flex-1 flex items-center justify-center">
              <Spinner size="lg" />
            </div>
          {:else}
            <GlossaryList
              {terms}
              {selectedTermId}
              onSelect={handleSelectTerm}
              onApprove={(term) => onApprove?.(term)}
              onDeprecate={(term) => onDeprecate?.(term)}
            />

            {#if hasMore}
              <div class="flex justify-center pt-4">
                <Button
                  variant="unstyled"
                  class="px-4 py-2 text-sm text-blue-600 hover:text-blue-700 hover:bg-blue-50 dark:hover:bg-blue-950 rounded-lg"
                  onclick={onLoadMore}
                  disabled={loading}
                >
                  {#if loading}
                    <Spinner size="sm" />
                  {/if}
                  Load more ({terms.length} / {totalCount})
                </Button>
              </div>
            {/if}
          {/if}
        </div>
      </div>
    {:else if viewMode === 'detail' && currentTerm}
      <GlossaryDetail
        term={currentTerm}
        {usage}
        {loadingUsage}
        onEdit={handleEditClick}
        onApprove={handleApprove}
        onDeprecate={handleDeprecate}
        class="h-full"
      />
    {:else if viewMode === 'create'}
      <div class="p-4 overflow-y-auto h-full">
        <GlossaryForm
          {conflictResult}
          loading={loadingForm}
          onSubmit={handleFormSubmit}
          {onCheckConflict}
        />
      </div>
    {:else if viewMode === 'edit' && currentTerm}
      <div class="p-4 overflow-y-auto h-full">
        <GlossaryForm
          term={currentTerm}
          {conflictResult}
          loading={loadingForm}
          onSubmit={handleFormSubmit}
        />
      </div>
    {/if}
  </div>
</div>
