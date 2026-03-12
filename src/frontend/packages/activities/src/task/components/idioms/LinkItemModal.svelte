<!--
  LinkItemModal Component
  Modal for selecting items to link to a task
  Supports: conversation, requirement, spec, artifact
-->
<script lang="ts">
  import { untrack } from 'svelte';
  import { Modal } from '@sddp/shell/components';
  import { toast } from '@sddp/shell';
  import { Icon, Button, Input, Spinner } from '@sddp/ui';
  import { getChannelList } from '../../../conversations/services/ConversationService';
  import { getRequirements } from '../../../requirements/services/RequirementService';
  import { getSpecs } from '../../../specs/services/SpecService';
  import { getArtifactsByProject } from '../../../artifact/services/ArtifactService';
  import type { ChannelSummary } from '../../../conversations/types';
  import type { Requirement } from '../../../requirements/types';
  import type { Spec } from '../../../specs/types';
  import type { ArtifactSummary } from '../../../artifact/types';

  type LinkedItemType = 'conversation' | 'requirement' | 'spec' | 'artifact';

  interface LinkableItem {
    id: string;
    title: string;
    subtitle?: string;
    icon: string;
  }

  interface Props {
    open: boolean;
    tenantId: string;
    projectId: string;
    existingLinkedIds?: string[];
    onClose: () => void;
    onLink: (type: LinkedItemType, entityId: string, entityTitle: string) => void;
  }

  let {
    open,
    tenantId,
    projectId,
    existingLinkedIds = [],
    onClose,
    onLink,
  }: Props = $props();

  // State
  let selectedType = $state<LinkedItemType>('conversation');
  let searchQuery = $state('');
  let items = $state<LinkableItem[]>([]);
  let loading = $state(false);
  let error = $state('');
  let selectedItemId = $state<string | null>(null);

  // Item types config
  const itemTypes: { value: LinkedItemType; label: string; icon: string }[] = [
    { value: 'conversation', label: 'Conversation', icon: 'message-circle' },
    { value: 'requirement', label: 'Requirement', icon: 'clipboard-list' },
    { value: 'spec', label: 'Spec', icon: 'file-text' },
    { value: 'artifact', label: 'Artifact', icon: 'package' },
  ];

  // Filtered items based on search query
  const filteredItems = $derived(
    items.filter((item) => {
      const query = searchQuery.toLowerCase();
      const matchesSearch =
        item.title.toLowerCase().includes(query) ||
        (item.subtitle?.toLowerCase().includes(query) ?? false);
      const notAlreadyLinked = !existingLinkedIds.includes(item.id);
      return matchesSearch && notAlreadyLinked;
    })
  );

  // Load items when type changes or modal opens
  $effect(() => {
    if (open && tenantId && projectId) {
      void selectedType; // explicitly track type changes
      untrack(() => loadItems());
    }
  });

  async function loadItems() {
    loading = true;
    error = '';
    items = [];
    selectedItemId = null;

    try {
      switch (selectedType) {
        case 'conversation': {
          const topics = await getChannelList(tenantId, projectId, { pageSize: 100 });
          items = topics.map((t: ChannelSummary) => ({
            id: t.id,
            title: t.title ?? t.topic,
            subtitle: `${t.status} - ${t.participantCount} participants`,
            icon: 'message-circle',
          }));
          break;
        }

        case 'requirement': {
          const reqPage = await getRequirements(tenantId, projectId, { pageSize: 100 });
          items = reqPage.items.map((r: Requirement) => ({
            id: r.id,
            title: `${r.code}: ${r.title}`,
            subtitle: `${r.level} - ${r.status}`,
            icon: 'clipboard-list',
          }));
          break;
        }

        case 'spec': {
          const specPage = await getSpecs(tenantId, projectId, { pageSize: 100 });
          items = specPage.items.map((s: Spec) => ({
            id: s.id,
            title: `${s.code}: ${s.title}`,
            subtitle: `v${s.version} - ${s.status}`,
            icon: 'file-text',
          }));
          break;
        }

        case 'artifact': {
          const artifacts = await getArtifactsByProject(tenantId, projectId);
          items = artifacts.map((a: ArtifactSummary) => ({
            id: a.id,
            title: a.artifactPath,
            subtitle: `${a.artifactType} - ${a.isValid ? 'Valid' : 'Modified'}`,
            icon: 'package',
          }));
          break;
        }
      }
    } catch (err) {
      error = err instanceof Error ? err.message : 'Failed to load items';
      toast.error(error);
      console.error('Failed to load items:', err);
    } finally {
      loading = false;
    }
  }

  function handleTypeChange(type: LinkedItemType) {
    selectedType = type;
    searchQuery = '';
    loadItems();
  }

  function handleSelectItem(itemId: string) {
    selectedItemId = selectedItemId === itemId ? null : itemId;
  }

  function handleConfirm() {
    if (!selectedItemId) return;
    const item = items.find((i) => i.id === selectedItemId);
    if (item) {
      onLink(selectedType, item.id, item.title);
    }
    handleClose();
  }

  function handleClose() {
    searchQuery = '';
    selectedItemId = null;
    error = '';
    onClose();
  }
</script>

<Modal {open} title="Link Item to Task" onClose={handleClose}>
  <div class="link-modal">
    <!-- Type Selector -->
    <div class="type-selector">
      {#each itemTypes as type (type.value)}
        <Button
          variant="unstyled"
          class="type-btn {selectedType === type.value ? 'active' : ''}"
          onclick={() => handleTypeChange(type.value)}
        >
          <Icon name={type.icon} size="sm" />
          <span>{type.label}</span>
        </Button>
      {/each}
    </div>

    <!-- Search Input -->
    <div class="search-field">
      <Icon name="search" size="sm" class="search-icon" />
      <Input
        unstyled
        placeholder="Search {itemTypes.find((t) => t.value === selectedType)?.label ?? ''}..."
        bind:value={searchQuery}
        disabled={loading}
        class="search-input"
      />
    </div>

    <!-- Items List -->
    <div class="items-list">
      {#if loading}
        <div class="loading-state">
          <Spinner size="lg" />
          <span>Loading items...</span>
        </div>
      {:else if error}
        <div class="error-state">
          <Icon name="alert-circle" size="lg" />
          <span>{error}</span>
          <Button variant="secondary" onclick={loadItems}>Retry</Button>
        </div>
      {:else if filteredItems.length === 0}
        <div class="empty-state">
          <Icon name="inbox" size="lg" />
          <span>
            {searchQuery
              ? 'No matching items found'
              : existingLinkedIds.length > 0
                ? 'All available items are already linked'
                : 'No items available'}
          </span>
        </div>
      {:else}
        {#each filteredItems as item (item.id)}
          <Button
            variant="unstyled"
            class="item-row {selectedItemId === item.id ? 'selected' : ''}"
            onclick={() => handleSelectItem(item.id)}
          >
            <Icon name={item.icon} size="sm" class="item-icon" />
            <div class="item-content">
              <span class="item-title">{item.title}</span>
              {#if item.subtitle}
                <span class="item-subtitle">{item.subtitle}</span>
              {/if}
            </div>
            {#if selectedItemId === item.id}
              <Icon name="check" size="sm" class="check-icon" />
            {/if}
          </Button>
        {/each}
      {/if}
    </div>

    <!-- Actions -->
    <div class="modal-actions">
      <Button variant="secondary" onclick={handleClose}>Cancel</Button>
      <Button
        variant="primary"
        onclick={handleConfirm}
        disabled={!selectedItemId}
      >
        Link Item
      </Button>
    </div>
  </div>
</Modal>

<style>
  .link-modal {
    display: flex;
    flex-direction: column;
    gap: 1rem;
    min-width: 480px;
    max-width: 560px;
  }

  .type-selector {
    display: flex;
    gap: 0.5rem;
    padding: 0.25rem;
    background: var(--bg-secondary);
    border-radius: 8px;
  }

  .link-modal :global(.type-btn) {
    flex: 1;
    display: flex;
    align-items: center;
    justify-content: center;
    gap: 0.375rem;
    padding: 0.5rem 0.75rem;
    background: transparent;
    border: none;
    border-radius: 6px;
    font-size: 0.8125rem;
    font-weight: 500;
    color: var(--text-secondary);
    cursor: pointer;
    transition: all 0.15s ease;
  }

  .link-modal :global(.type-btn:hover:not(:disabled)) {
    background: var(--bg-tertiary);
    color: var(--text-primary);
  }

  .link-modal :global(.type-btn.active) {
    background: var(--bg-primary);
    color: var(--accent-color);
    box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
  }

  .search-field {
    position: relative;
    display: flex;
    align-items: center;
  }

  .search-field :global(.search-icon) {
    position: absolute;
    left: 0.75rem;
    color: var(--text-tertiary);
  }

  .search-field :global(.search-input) {
    width: 100%;
    padding: 0.625rem 0.75rem 0.625rem 2.25rem;
    background: var(--bg-primary);
    border: 1px solid var(--border-color);
    border-radius: 6px;
    color: var(--text-primary);
    font-size: 0.875rem;
  }

  .search-field :global(.search-input:focus) {
    outline: none;
    border-color: var(--accent-color);
  }


  .items-list {
    min-height: 200px;
    max-height: 320px;
    overflow-y: auto;
    border: 1px solid var(--border-color);
    border-radius: 6px;
    background: var(--bg-primary);
  }

  .loading-state,
  .error-state,
  .empty-state {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    gap: 0.5rem;
    padding: 2rem;
    color: var(--text-secondary);
    font-size: 0.875rem;
    text-align: center;
  }

  .loading-state :global(.spinner) {
    animation: spin 1s linear infinite;
  }

  @keyframes spin {
    from {
      transform: rotate(0deg);
    }
    to {
      transform: rotate(360deg);
    }
  }

  .error-state {
    color: var(--error-color, #ef4444);
  }

  .link-modal :global(.item-row) {
    display: flex;
    align-items: center;
    gap: 0.75rem;
    width: 100%;
    padding: 0.75rem 1rem;
    background: transparent;
    border: none;
    border-bottom: 1px solid var(--border-color);
    text-align: left;
    cursor: pointer;
    transition: background-color 0.15s ease;
  }

  .link-modal :global(.item-row:last-child) {
    border-bottom: none;
  }

  .link-modal :global(.item-row:hover:not(:disabled)) {
    background: var(--hover-bg);
  }

  .link-modal :global(.item-row.selected) {
    background: var(--accent-color-10, rgba(59, 130, 246, 0.1));
  }

  .link-modal :global(.item-icon) {
    flex-shrink: 0;
    color: var(--text-tertiary);
  }


  .item-content {
    flex: 1;
    min-width: 0;
    display: flex;
    flex-direction: column;
    gap: 0.125rem;
  }

  .item-title {
    font-size: 0.875rem;
    font-weight: 500;
    color: var(--text-primary);
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
  }

  .item-subtitle {
    font-size: 0.75rem;
    color: var(--text-tertiary);
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
  }

  .link-modal :global(.check-icon) {
    flex-shrink: 0;
    color: var(--accent-color);
  }


  .modal-actions {
    display: flex;
    justify-content: flex-end;
    gap: 0.5rem;
    margin-top: 0.5rem;
  }

</style>
