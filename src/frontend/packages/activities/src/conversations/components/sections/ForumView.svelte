<!-- Section: ForumView — Conversations > Forum/Global, Projects > Conversations -->
<script lang="ts">
  import { untrack } from 'svelte';
  import { Icon, IconButton, Input, CardGrid, Spinner, Button, Textarea, ResizeHandle } from '@sddp/ui';
  import { ListItem, toast, EmptyState, Dropdown } from '@sddp/shell';
  import { PageShell, PageBody, PageHeader } from '@sddp/shell';
  import TopicView from './TopicView.svelte';
  import { ConversationHeader } from '../idioms';
  import DetailHeader from '../../../shared/components/idioms/DetailHeader.svelte';
  import DetailTitle from '../../../shared/components/idioms/DetailTitle.svelte';
  import { StatCard } from '../../../shared/components/idioms';
  import type { Topic, TopicStatus } from '../../types';
  import { getConversationService, type TopicDto } from '../../services';

  interface Props {
    tenantId: string;
    projectId?: string;
    conversationId: string;
    conversationName: string;
    selectedTopicId?: string;
    class?: string;
  }

  let {
    tenantId,
    projectId,
    conversationId,
    conversationName,
    selectedTopicId,
    class: className = '',
  }: Props = $props();

  const effectiveSelectedTopicId = $derived(selectedTopicId ?? '');

  // Service instance
  const conversationService = getConversationService();

  // Forum topics state
  let topics = $state<Topic[]>([]);
  let loading = $state(true);

  // Create topic inline form state
  let showCreateForm = $state(false);
  let newTopicTitle = $state('');
  let newTopicContent = $state('');
  let isCreating = $state(false);

  // Load topics when conversationId changes
  let prevConversationId = $state<string | null>(null);
  $effect(() => {
    if (!conversationId) { prevConversationId = null; return; }
    if (conversationId === prevConversationId) return;
    prevConversationId = conversationId;
    untrack(() => loadTopics());
  });

  // Auto-select topic when selectedTopicId prop is provided
  $effect(() => {
    if (effectiveSelectedTopicId && topics.length > 0) {
      const topic = topics.find((t) => t.id === effectiveSelectedTopicId);
      if (topic) {
        selectedTopic = topic;
      }
    }
  });

  function convertToTopic(dto: TopicDto): Topic {
    return {
      id: dto.id,
      forumId: dto.forumId,
      title: dto.title,
      author: dto.author,
      status: dto.status,
      isPinned: dto.isPinned,
      isLocked: dto.isLocked,
      decisionSpecId: dto.decisionSpecId ?? undefined,
      messageCount: dto.messageCount,
      createdAt: dto.createdAt,
      updatedAt: dto.updatedAt,
    };
  }

  async function loadTopics(): Promise<void> {
    loading = true;
    try {
      conversationService.setContext(tenantId, projectId ?? '');
      const result = await conversationService.getTopics(conversationId);
      topics = result.topics.map(convertToTopic);
    } catch (err) {
      console.error('Failed to load forum topics:', err);
      toast.error(err instanceof Error ? err.message : 'Failed to load forum topics');
      topics = [];
    } finally {
      loading = false;
    }
  }

  let query = $state('');
  let filterStatus = $state<'all' | TopicStatus>('all');

  const filterStatusOptions: { value: 'all' | TopicStatus; label: string; icon: string }[] = [
    { value: 'all', label: 'All', icon: 'list' },
    { value: 'Open', label: 'Open', icon: 'circle' },
    { value: 'Closed', label: 'Closed', icon: 'check-circle' },
  ];

  const filteredTopics = $derived.by(() => {
    let result = topics;

    // Status filter
    if (filterStatus !== 'all') {
      result = result.filter((t) => t.status === filterStatus);
    }

    // Search filter
    const normalized = query.trim().toLowerCase();
    if (normalized) {
      result = result.filter(
        (t) =>
          t.title.toLowerCase().includes(normalized) ||
          (t.author?.name ?? '').toLowerCase().includes(normalized)
      );
    }

    return result;
  });

  // Selected topic state
  let selectedTopic = $state<Topic | null>(null);

  // Forum statistics (derived)
  const stats = $derived({
    total: topics.length,
    open: topics.filter((t) => t.status === 'Open').length,
    closed: topics.filter((t) => t.status === 'Closed').length,
    totalMessages: topics.reduce((sum, t) => sum + t.messageCount, 0),
    pinned: topics.filter((t) => t.isPinned).length,
  });

  // Resize state
  let leftPanelWidth = $state(320); // default 320px
  let isResizing = $state(false);
  let containerElement: HTMLDivElement | undefined;
  void containerElement;

  const MIN_LEFT_WIDTH = 200;
  const MAX_LEFT_WIDTH = 500;

  function getStatusColor(status: TopicStatus): string {
    switch (status) {
      case 'Open':
        return 'bg-[var(--color-success-500)]';
      case 'Closed':
        return 'bg-[var(--color-neutral-400)]';
      default:
        return 'bg-[var(--color-neutral-400)]';
    }
  }

  function handleTopicClick(topic: Topic) {
    selectedTopic = topic;
  }

  function handleBackToList() {
    selectedTopic = null;
  }

  function handleCreateTopic() {
    selectedTopic = null;
    showCreateForm = true;
    newTopicTitle = '';
    newTopicContent = '';
  }

  function handleCloseCreateForm() {
    showCreateForm = false;
    newTopicTitle = '';
    newTopicContent = '';
  }

  async function handleSubmitCreateTopic() {
    if (!newTopicTitle.trim()) {
      toast.error('Topic title is required');
      return;
    }

    isCreating = true;
    try {
      conversationService.setContext(tenantId, projectId ?? '');
      const newTopic = await conversationService.createTopic(conversationId, {
        title: newTopicTitle.trim(),
        initialMessageContent: newTopicContent.trim() || undefined,
      });

      const topic = convertToTopic(newTopic);
      topics = [topic, ...topics];
      selectedTopic = topic;

      toast.success('Topic created successfully');
      handleCloseCreateForm();
    } catch (err) {
      console.error('Failed to create topic:', err);
      toast.error('Failed to create topic');
    } finally {
      isCreating = false;
    }
  }

  function handleTopicClosed() {
    loadTopics();
    selectedTopic = null;
  }

  // --- Markdown toolbar for create form ---
  let newTopicTextareaEl: HTMLTextAreaElement | undefined = $state();

  function createFormApplyWrap(prefix: string, suffix: string): void {
    if (!newTopicTextareaEl) return;
    const start = newTopicTextareaEl.selectionStart;
    const end = newTopicTextareaEl.selectionEnd;
    const selected = newTopicContent.slice(start, end);
    newTopicContent = newTopicContent.slice(0, start) + prefix + selected + suffix + newTopicContent.slice(end);
    requestAnimationFrame(() => {
      if (!newTopicTextareaEl) return;
      newTopicTextareaEl.focus();
      newTopicTextareaEl.selectionStart = start + prefix.length;
      newTopicTextareaEl.selectionEnd = end + prefix.length;
    });
  }

  function createFormApplyLinePrefix(prefix: string): void {
    if (!newTopicTextareaEl) return;
    const start = newTopicTextareaEl.selectionStart;
    const end = newTopicTextareaEl.selectionEnd;
    const selected = newTopicContent.slice(start, end);
    const formatted = selected.split('\n').map((line) => prefix + line).join('\n');
    newTopicContent = newTopicContent.slice(0, start) + formatted + newTopicContent.slice(end);
    requestAnimationFrame(() => {
      if (!newTopicTextareaEl) return;
      newTopicTextareaEl.focus();
      newTopicTextareaEl.selectionStart = start;
      newTopicTextareaEl.selectionEnd = start + formatted.length;
    });
  }

  function createFormToolbarAction(e: MouseEvent, action: () => void): void {
    e.preventDefault();
    action();
  }

  function createFormToolbarLink(e: MouseEvent): void {
    e.preventDefault();
    if (!newTopicTextareaEl) return;
    if (newTopicTextareaEl.selectionEnd > newTopicTextareaEl.selectionStart) {
      createFormApplyWrap('[', '](url)');
    } else {
      createFormApplyWrap('[text](', ')');
    }
  }

  // Resize handlers
  function handleResizeStart(e: PointerEvent) {
    e.preventDefault();
    isResizing = true;

    const startX = e.clientX;
    const startWidth = leftPanelWidth;

    function handleMove(moveEvent: PointerEvent) {
      const deltaX = moveEvent.clientX - startX;
      const newWidth = Math.min(MAX_LEFT_WIDTH, Math.max(MIN_LEFT_WIDTH, startWidth + deltaX));
      leftPanelWidth = newWidth;
    }

    function handleUp() {
      isResizing = false;
      document.removeEventListener('pointermove', handleMove);
      document.removeEventListener('pointerup', handleUp);
    }

    document.addEventListener('pointermove', handleMove);
    document.addEventListener('pointerup', handleUp);
  }
</script>

<div bind:this={containerElement} class="flex h-full {className}">
  <!-- Left Panel: Topic List -->
  <div
    class="flex flex-col flex-shrink-0"
    style="width: {leftPanelWidth}px"
  >
    <div class="flex flex-col h-full bg-[var(--color-bg-section)]">
      <ConversationHeader icon="list" title={conversationName} badge={topics.length}>
        {#snippet actions()}
          <IconButton icon="plus" size="sm" variant="ghost" onclick={handleCreateTopic} title="New Topic" />
        {/snippet}
      </ConversationHeader>

      <div class="flex-1 flex flex-col min-h-0">
      <!-- Search + Filter (matching ConversationSidebar style) -->
      <div class="flex-shrink-0 flex items-center gap-1 px-2 pt-2 pb-1.5">
        <div class="relative flex-1">
          <Icon
            name="search"
            size="sm"
            class="absolute left-2.5 top-1/2 -translate-y-1/2 text-[var(--color-text-tertiary)]"
          />
          <Input
            unstyled
            placeholder="Search topics..."
            bind:value={query}
            class="forum-search-input pl-8 w-full"
          />
        </div>
        <!-- Filter Dropdown -->
        <Dropdown position="bottom-right">
          {#snippet trigger()}
            <IconButton icon="more-vertical" size="sm" variant="ghost" title="Filter options" />
          {/snippet}
          <div class="py-1 min-w-[160px]">
            <div class="px-3 py-1 text-xs font-semibold text-[var(--color-text-tertiary)] uppercase">
              Status
            </div>
            {#each filterStatusOptions as option (option.value)}
              <Button
                variant="unstyled"
                class="w-full flex items-center gap-2 px-3 py-1.5 text-sm text-left
                       hover:bg-[var(--color-bg-tertiary)] transition-colors"
                onclick={() => (filterStatus = option.value)}
              >
                <Icon name={option.icon} size="sm" class="text-[var(--color-text-tertiary)]" />
                <span class="flex-1">{option.label}</span>
                {#if filterStatus === option.value}
                  <Icon name="check" size="sm" class="text-[var(--color-accent-primary)]" />
                {/if}
              </Button>
            {/each}
          </div>
        </Dropdown>
      </div>
      <div class="border-b border-[var(--color-border-primary)]"></div>

      <div class="flex-1 min-h-0 overflow-y-auto">
        {#if loading}
          <div class="flex-1 flex items-center justify-center"><Spinner size="lg" /></div>
        {:else if topics.length === 0}
          <EmptyState
            icon="folder-open"
            heading="No topics yet"
            subtext="Create a topic to start a discussion"
          />
        {:else if filteredTopics.length === 0}
          <div class="flex flex-col items-center justify-center h-32 text-center px-4">
            <Icon name="search" size="lg" class="text-[var(--color-text-muted)] mb-2" />
            <p class="text-sm text-[var(--color-text-muted)]">No matching topics</p>
          </div>
        {:else}
          <div>
            {#each filteredTopics as topic (topic.id)}
              <ListItem
                selected={selectedTopic?.id === topic.id}
                onclick={() => handleTopicClick(topic)}
              >
                <div class="flex-1 min-w-0">
                  <div class="flex items-center gap-2">
                    {#if topic.isPinned}
                      <Icon
                        name="pin"
                        size="xs"
                        class="flex-shrink-0 text-[var(--color-accent-primary)]"
                      />
                    {/if}
                    <span
                      class="w-2 h-2 rounded-full flex-shrink-0 {getStatusColor(topic.status)}"
                      title={topic.status}
                    ></span>
                    <h3 class="text-sm font-medium text-[var(--color-text-primary)] truncate">
                      {topic.title}
                    </h3>
                    <span
                      class="ml-auto flex-shrink-0 px-1.5 py-px rounded text-[0.6875rem] leading-tight
                        {topic.status === 'Open'
                        ? 'text-[var(--color-success-600)]'
                        : topic.status === 'Closed'
                          ? 'text-[var(--color-text-tertiary)]'
                          : 'text-[var(--color-warning-700)]'}"
                    >
                      {topic.status}
                    </span>
                  </div>
                  <div class="flex items-center gap-3 mt-0.5 text-xs text-[var(--color-text-muted)]">
                    <span>{topic.author?.name ?? 'Unknown'}</span>
                    <span class="flex items-center gap-1">
                      <Icon name="message-square" size="xs" />
                      {topic.messageCount}
                    </span>
                  </div>
                </div>
              </ListItem>
            {/each}
          </div>
        {/if}
      </div>
      </div>
    </div>
  </div>

  <!-- Resize Handle -->
  <ResizeHandle orientation="vertical" onpointerdown={handleResizeStart} {isResizing} />

  <!-- Right Panel: Create Form, Topic Detail, or Statistics -->
  <div class="flex-1 min-w-0 border-l border-[var(--color-border-primary)]">
    {#if showCreateForm}
      <!-- Inline Create Topic Form -->
      <PageShell class="h-full">
        <PageHeader title="New Topic">
          {#snippet actions()}
            <IconButton icon="check" variant={newTopicTitle.trim() ? 'success' : 'ghost'} onclick={() => handleSubmitCreateTopic()} disabled={isCreating || !newTopicTitle.trim()} title="Create" />
            <IconButton icon="x" variant="ghost" onclick={handleCloseCreateForm} disabled={isCreating} title="Cancel" />
          {/snippet}
        </PageHeader>
        <PageBody padded={false} class="p-4">
          <form
            onsubmit={(e) => {
              e.preventDefault();
              handleSubmitCreateTopic();
            }}
            class="space-y-4"
          >
            <div>
              <label for="topic-title" class="block text-sm font-medium text-[var(--color-text-secondary)] mb-1">
                Title <span class="text-[var(--color-error-500)]">*</span>
              </label>
              <Input
                id="topic-title"
                bind:value={newTopicTitle}
                placeholder="Enter topic title..."
                disabled={isCreating}
              />
            </div>

            <div>
              <label for="topic-content" class="block text-sm font-medium text-[var(--color-text-secondary)] mb-1">
                Initial Message (optional)
              </label>
              <div class="rounded-lg border border-[var(--color-border-secondary)] bg-[var(--color-bg-primary)] overflow-hidden focus-within:border-[var(--color-accent-primary)] transition-colors">
                <!-- Markdown toolbar -->
                <div class="md-toolbar flex items-center gap-0.5 px-2 pt-1.5 pb-0.5 border-b border-[var(--color-border-secondary)]">
                  <Button variant="unstyled" class="md-toolbar-btn" title="Heading" onmousedown={(e) => createFormToolbarAction(e, () => createFormApplyLinePrefix('## '))}>
                    <span class="md-toolbar-text font-bold">H</span>
                  </Button>
                  <Button variant="unstyled" class="md-toolbar-btn" title="Bold" onmousedown={(e) => createFormToolbarAction(e, () => createFormApplyWrap('**', '**'))}>
                    <span class="md-toolbar-text font-bold">B</span>
                  </Button>
                  <Button variant="unstyled" class="md-toolbar-btn" title="Italic" onmousedown={(e) => createFormToolbarAction(e, () => createFormApplyWrap('*', '*'))}>
                    <span class="md-toolbar-text italic">I</span>
                  </Button>
                  <Button variant="unstyled" class="md-toolbar-btn" title="Strikethrough" onmousedown={(e) => createFormToolbarAction(e, () => createFormApplyWrap('~~', '~~'))}>
                    <span class="md-toolbar-text line-through">S</span>
                  </Button>
                  <span class="md-toolbar-sep"></span>
                  <Button variant="unstyled" class="md-toolbar-btn" title="Inline code" onmousedown={(e) => createFormToolbarAction(e, () => createFormApplyWrap('`', '`'))}>
                    <Icon name="code" size="xs" />
                  </Button>
                  <Button variant="unstyled" class="md-toolbar-btn" title="Code block" onmousedown={(e) => createFormToolbarAction(e, () => createFormApplyWrap('```\n', '\n```'))}>
                    <span class="md-toolbar-text font-mono text-[0.5625rem]">{'{}'}</span>
                  </Button>
                  <Button variant="unstyled" class="md-toolbar-btn" title="Blockquote" onmousedown={(e) => createFormToolbarAction(e, () => createFormApplyLinePrefix('> '))}>
                    <span class="md-toolbar-text text-[0.8125rem]">"</span>
                  </Button>
                  <span class="md-toolbar-sep"></span>
                  <Button variant="unstyled" class="md-toolbar-btn" title="Bullet list" onmousedown={(e) => createFormToolbarAction(e, () => createFormApplyLinePrefix('- '))}>
                    <Icon name="list" size="xs" />
                  </Button>
                  <Button variant="unstyled" class="md-toolbar-btn" title="Numbered list" onmousedown={(e) => createFormToolbarAction(e, () => createFormApplyLinePrefix('1. '))}>
                    <span class="md-toolbar-text font-mono text-[0.5625rem]">1.</span>
                  </Button>
                  <Button variant="unstyled" class="md-toolbar-btn" title="Link" onmousedown={createFormToolbarLink}>
                    <Icon name="link" size="xs" />
                  </Button>
                </div>
                <!-- Textarea -->
                <Textarea
                  unstyled
                  id="topic-content"
                  bind:element={newTopicTextareaEl}
                  bind:value={newTopicContent}
                  placeholder="Start the conversation..."
                  rows={6}
                  disabled={isCreating}
                  class="w-full px-3 py-2 text-sm text-[var(--color-text-primary)] placeholder:text-[var(--color-text-muted)] bg-transparent resize-none focus:outline-none"
                  style="min-height: 9.75rem"
                />
              </div>
            </div>

          </form>
        </PageBody>
      </PageShell>
    {:else if selectedTopic}
      <TopicView
        topic={selectedTopic}
        {tenantId}
        {projectId}
        onBack={handleBackToList}
        onTopicClosed={handleTopicClosed}
      />
    {:else}
      <!-- Forum Statistics -->
      <PageShell class="h-full">
        <DetailHeader>
          <DetailTitle title="Forum Overview" />
        </DetailHeader>
        <PageBody padded={false} class="p-3 space-y-3">
          {#if loading}
            <div class="flex items-center justify-center h-32">
              <Spinner size="lg" />
            </div>
          {:else}
            <!-- Stats Grid -->
            <CardGrid cols={4} gap="md">
              <StatCard title="Total Topics" value={stats.total} icon="list" iconColor="blue-500" />
              <StatCard title="Messages" value={stats.totalMessages} icon="message-square" iconColor="purple-500" />
              <StatCard title="Open" value={stats.open} icon="check-circle" iconColor="green-500" />
              <StatCard title="Closed" value={stats.closed} icon="lock" iconColor="gray-500" />
            </CardGrid>

            <!-- Additional Info -->
            {#if stats.pinned > 0}
              <div class="flex items-center gap-2 text-xs text-[var(--color-text-muted)] mb-2">
                <Icon name="pin" size="xs" class="text-[var(--color-accent-primary)]" />
                <span>{stats.pinned} pinned topic{stats.pinned > 1 ? 's' : ''}</span>
              </div>
            {/if}
            {#if topics.length === 0}
              <div class="flex flex-col items-center justify-center text-center pt-8">
                <Icon name="message-square" size="xl" class="text-[var(--color-text-muted)] mb-3" />
                <p class="text-sm text-[var(--color-text-muted)]">
                  No topics yet. Create one to start a discussion.
                </p>
              </div>
            {:else}
              <p class="text-xs text-[var(--color-text-muted)] mt-4">
                Select a topic from the list to view its discussion.
              </p>
            {/if}
          {/if}
        </PageBody>
      </PageShell>
    {/if}
  </div>
</div>

<style>
  /* Markdown toolbar — :global() needed because targets are inside Button child components */
  .md-toolbar :global(.md-toolbar-btn) {
    display: flex;
    align-items: center;
    justify-content: center;
    width: 26px;
    height: 24px;
    border: none;
    border-radius: 4px;
    background: transparent;
    color: var(--color-text-secondary);
    cursor: pointer;
    transition: background-color 0.1s ease, color 0.1s ease;
  }

  .md-toolbar :global(.md-toolbar-btn:hover) {
    background: var(--color-surface-200);
    color: var(--color-text-primary);
  }

  .md-toolbar :global(.md-toolbar-text) {
    font-size: 12px;
    line-height: 1;
    user-select: none;
  }

  .md-toolbar-sep {
    width: 1px;
    height: 14px;
    margin: 0 4px;
    background: var(--color-border-secondary);
    flex-shrink: 0;
  }

  /* Search input (flat, no rounded corners) — inside <Input> child component */
  :global(.forum-search-input) {
    display: block;
    width: 100%;
    padding: 0.25rem 0.5rem 0.25rem 2rem;
    font-size: var(--text-xs);
    line-height: 1.5;
    color: var(--color-text-primary);
    background: var(--color-bg-primary);
    border: 1px solid var(--color-border);
    border-radius: 0;
    transition: border-color 0.15s;
  }

  :global(.forum-search-input:focus) {
    outline: none;
    border-color: var(--color-accent-primary);
  }

  :global(.forum-search-input::placeholder) {
    color: var(--color-text-tertiary);
  }

  :global(.forum-search-input::-webkit-search-cancel-button),
  :global(.forum-search-input::-webkit-search-decoration) {
    -webkit-appearance: none;
    display: none;
  }
</style>
