<!-- Section: ConversationSidebar — Conversations > Global -->
<script lang="ts">
  import type { Snippet } from 'svelte';
  import { SvelteSet } from 'svelte/reactivity';
  import { Icon, Spinner, Button, Input } from '@sddp/ui';
  import { CollapsiblePanel } from '@sddp/shell';
  import type {
    ProjectConversationGroup as ProjectConversationGroupType,
    ConversationSummary,
  } from '../../types';
  import { ProjectConversationGroup, ConversationItem } from '../idioms';

  interface Props {
    // Flat structure props (Global Conversations view)
    channels?: ConversationSummary[];
    privateChannels?: ConversationSummary[];
    topics?: ConversationSummary[];
    onSectionToggle?: (section: string) => void;
    // Legacy: Project-grouped structure (Projects → Conversations tab)
    projectGroups?: ProjectConversationGroupType[];
    onProjectToggle?: (projectId: string, expanded: boolean) => void;
    // Common props
    starredConversations: ConversationSummary[];
    selectedConversationId?: string | null;
    searchQuery?: string;
    loading?: boolean;
    hasMore?: boolean;
    loadingMore?: boolean;
    onConversationSelect?: (conversationId: string) => void;
    onConversationStarToggle?: (conversationId: string) => void;
    onSearchChange?: (query: string) => void;
    onCreateChannel?: () => void;
    onCreateForum?: () => void;
    onLoadMore?: () => void;
    children?: Snippet;
    class?: string;
  }

  let {
    // Flat structure
    channels = [],
    privateChannels = [],
    topics = [],
    onSectionToggle,
    // Legacy: project-grouped
    projectGroups = [],
    onProjectToggle,
    // Common
    starredConversations,
    selectedConversationId = null,
    searchQuery: _externalSearchQuery = '',
    loading = false,
    hasMore = false,
    loadingMore = false,
    onConversationSelect,
    onConversationStarToggle,
    onSearchChange,
    onCreateChannel,
    onCreateForum,
    onLoadMore,
    children,
    class: className = '',
  }: Props = $props();

  // Local search state for client-side filtering
  let localSearchQuery = $state('');

  // Local expanded state (not dependent on parent)
  let localExpandedSections = new SvelteSet(['channels', 'topics']);
  let starredExpanded = $state(true);

  // Determine if using flat structure (Global view) or project-grouped (legacy)
  let useFlatStructure = $derived(channels.length > 0 || privateChannels.length > 0 || topics.length > 0 || projectGroups.length === 0);

  // Client-side filtering (same UX as Project > Conversations)
  const filteredChannels = $derived.by(() => {
    const query = localSearchQuery.trim().toLowerCase();
    const all = [...channels, ...privateChannels];
    if (!query) return all;
    return all.filter(c => c.name.toLowerCase().includes(query));
  });

  const filteredTopics = $derived.by(() => {
    const query = localSearchQuery.trim().toLowerCase();
    if (!query) return topics;
    return topics.filter(c => c.name.toLowerCase().includes(query));
  });

  const filteredStarred = $derived.by(() => {
    const query = localSearchQuery.trim().toLowerCase();
    if (!query) return starredConversations;
    return starredConversations.filter(c => c.name.toLowerCase().includes(query));
  });

  function handleSectionToggle(section: string) {
    if (localExpandedSections.has(section)) localExpandedSections.delete(section);
    else localExpandedSections.add(section);
    onSectionToggle?.(section);
  }

  function isSectionExpanded(section: string): boolean {
    return localExpandedSections.has(section);
  }

  function handleConversationClick(conversation: ConversationSummary) {
    onConversationSelect?.(conversation.id);
  }

  function handleConversationStarClick(conversation: ConversationSummary) {
    onConversationStarToggle?.(conversation.id);
  }

  function handleSearchInput(e: Event) {
    const target = e.target as HTMLInputElement;
    localSearchQuery = target.value;
    onSearchChange?.(target.value);
  }

  function handleScroll(e: Event): void {
    if (!hasMore || loadingMore) return;

    const target = e.currentTarget as HTMLElement;
    const nearBottom = target.scrollHeight - target.scrollTop - target.clientHeight <= 100;
    if (nearBottom) {
      onLoadMore?.();
    }
  }

  function handleCreateChannel() { onCreateChannel?.(); }
  function handleCreateForum() { onCreateForum?.(); }
</script>

<aside
  class="conversation-sidebar flex flex-col h-full bg-[var(--color-bg-primary)] {className}"
>
  <!-- Search Bar -->
  <div class="flex-shrink-0 flex items-center p-2 min-h-12 border-b border-[var(--color-border-primary)]">
    <div class="flex items-center gap-1 w-full">
      <!-- Search Input -->
      <div class="relative flex-1">
        <Icon
          name="search"
          size="sm"
          class="absolute left-2.5 top-1/2 -translate-y-1/2 text-[var(--color-text-tertiary)]"
        />
        <Input
          unstyled
          placeholder="Search channels..."
          value={localSearchQuery}
          oninput={handleSearchInput}
          class="conv-search-input pl-8 w-full"
        />
      </div>
    </div>
  </div>

  <!-- Scrollable Content -->
  <div class="flex-1 overflow-y-auto pb-1" onscroll={handleScroll}>
    {#if loading}
      <div class="flex-1 flex items-center justify-center">
        <Spinner size="lg" />
      </div>
    {:else}
      <!-- Starred Conversations Section -->
      {#if filteredStarred.length > 0}
        <CollapsiblePanel
          title="STARRED"
          expanded={starredExpanded}
          onToggle={(expanded) => (starredExpanded = expanded)}
        >
          <div class="bg-[var(--color-bg-primary)]">
            {#each filteredStarred as conversation (conversation.id)}
              <ConversationItem
                {conversation}
                selected={selectedConversationId === conversation.id}
                onClick={handleConversationClick}
                onStarClick={handleConversationStarClick}
              />
            {/each}
          </div>
        </CollapsiblePanel>
      {/if}

      {#if useFlatStructure}
        <!-- Flat Structure: Global Conversations View -->

        <!-- Channels Section (includes private channels with lock icon) -->
        <CollapsiblePanel
          title="CHANNELS"
          expanded={isSectionExpanded('channels')}
          onToggle={() => handleSectionToggle('channels')}
          actions={[{ id: 'add-channel', icon: 'plus', label: 'New Channel', onClick: handleCreateChannel }]}
        >
          <div class="bg-[var(--color-bg-primary)]">
            {#each filteredChannels as conversation (conversation.id)}
              <Button
                variant="unstyled"
                class="w-full flex items-center gap-2 px-2 py-1.5 cursor-pointer
                  transition-colors duration-150
                  {selectedConversationId === conversation.id
                    ? 'bg-[var(--color-accent-primary)]/10'
                    : 'hover:bg-[var(--color-bg-tertiary)]'}"
                onclick={() => handleConversationClick(conversation)}
              >
                <Icon
                  name={conversation.type === 'private' ? 'lock' : 'hash'}
                  size="sm"
                  class="flex-shrink-0 text-[var(--color-text-tertiary)]"
                />
                <span class="flex-1 text-left text-sm truncate text-[var(--color-text-primary)]">
                  {conversation.name}
                </span>
                {#if (conversation.unreadCount ?? 0) > 0}
                  <span
                    class="min-w-[18px] h-[18px] px-1 flex items-center justify-center text-xs font-medium
                      rounded-full bg-[var(--color-accent-primary)] text-white"
                  >
                    {(conversation.unreadCount ?? 0) > 99 ? '99+' : conversation.unreadCount}
                  </span>
                {/if}
              </Button>
            {/each}
            {#if filteredChannels.length === 0}
              <div class="px-2 py-2 text-center text-sm text-[var(--color-text-secondary)]">
                No channels
              </div>
            {/if}
          </div>
        </CollapsiblePanel>

        <!-- Forums Section -->
        <CollapsiblePanel
          title="FORUMS"
          expanded={isSectionExpanded('topics')}
          onToggle={() => handleSectionToggle('topics')}
          actions={[{ id: 'add-forum', icon: 'plus', label: 'New Forum', onClick: handleCreateForum }]}
        >
          <div class="bg-[var(--color-bg-primary)]">
            {#each filteredTopics as conversation (conversation.id)}
              <Button
                variant="unstyled"
                class="w-full flex items-center gap-2 px-2 py-1.5 cursor-pointer
                  transition-colors duration-150
                  {selectedConversationId === conversation.id
                    ? 'bg-[var(--color-accent-primary)]/10'
                    : 'hover:bg-[var(--color-bg-tertiary)]'}"
                onclick={() => handleConversationClick(conversation)}
              >
                <Icon
                  name="list"
                  size="sm"
                  class="flex-shrink-0 text-[var(--color-text-tertiary)]"
                />
                <span class="flex-1 text-left text-sm truncate text-[var(--color-text-primary)]">
                  {conversation.name}
                </span>
                {#if (conversation.unreadCount ?? 0) > 0}
                  <span
                    class="min-w-[18px] h-[18px] px-1 flex items-center justify-center text-xs font-medium
                      rounded-full bg-[var(--color-accent-primary)] text-white"
                  >
                    {(conversation.unreadCount ?? 0) > 99 ? '99+' : conversation.unreadCount}
                  </span>
                {/if}
              </Button>
            {/each}
            {#if filteredTopics.length === 0}
              <div class="px-2 py-2 text-center text-sm text-[var(--color-text-secondary)]">
                No forums
              </div>
            {/if}
          </div>
        </CollapsiblePanel>
      {:else}
        <!-- Legacy: Project-grouped Structure -->
        <div class="py-1">
          <div class="px-2 py-1 text-xs font-semibold uppercase tracking-wide text-[var(--color-text-tertiary)]">
            By Project
          </div>
          {#each projectGroups as group (group.projectId)}
            <ProjectConversationGroup
              {group}
              {selectedConversationId}
              onToggle={onProjectToggle}
              onConversationClick={handleConversationClick}
              onConversationStarClick={handleConversationStarClick}
            />
          {/each}

          {#if projectGroups.length === 0}
            <div class="px-2 py-4 text-center text-sm text-[var(--color-text-secondary)]">
              No conversations found
            </div>
          {/if}
        </div>

      {/if}
    {/if}

    {#if children}
      {@render children()}
    {/if}

    {#if loadingMore}
      <div class="flex items-center justify-center py-3">
        <Spinner size="sm" />
      </div>
    {/if}
  </div>
</aside>

<style>
  :global(.conv-search-input) {
    display: block;
    width: 100%;
    padding: 0.25rem 0.5rem 0.25rem 2rem;
    font-size: var(--text-xs);
    line-height: 1.5;
    color: var(--color-text-primary);
    background: var(--color-bg-primary);
    border: 1px solid var(--color-border);
    border-radius: 0.375rem;
    transition: border-color 0.15s;
  }

  :global(.conv-search-input:focus) {
    outline: none;
    border-color: var(--color-accent-primary);
  }

  :global(.conv-search-input::placeholder) {
    color: var(--color-text-tertiary);
  }

  /* Hide browser native clear button */
  :global(.conv-search-input::-webkit-search-cancel-button),
  :global(.conv-search-input::-webkit-search-decoration) {
    -webkit-appearance: none;
    display: none;
  }
</style>
