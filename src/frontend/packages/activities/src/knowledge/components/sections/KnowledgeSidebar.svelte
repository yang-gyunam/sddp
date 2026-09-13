<script lang="ts">
  import { Button, Icon, IconButton, Input, Spinner } from '@sddp/ui';
  import { Dropdown } from '@sddp/shell';
  import type {
    FlattenedKnowledgePage,
    KnowledgePageStatus,
    KnowledgeSearchResult,
  } from '../../types';

  interface KnowledgeCounts {
    all: number;
    Draft: number;
    Published: number;
    Archived: number;
  }

  interface Props {
    query: string;
    statusFilter: KnowledgePageStatus | 'all';
    counts: KnowledgeCounts;
    loading: boolean;
    searchResults: KnowledgeSearchResult[] | null;
    pages: FlattenedKnowledgePage[];
    selectedPageId?: string | null;
    onSearchInput: (event: Event) => void;
    onStatusFilterChange: (status: KnowledgePageStatus | 'all') => void;
    onSelectPage: (pageId: string) => void;
  }

  let {
    query,
    statusFilter,
    counts,
    loading,
    searchResults,
    pages,
    selectedPageId = null,
    onSearchInput,
    onStatusFilterChange,
    onSelectPage,
  }: Props = $props();

  const instanceId = $props.id();
  const searchInputId = `knowledge-sidebar-search-${instanceId}`;

  function statusIcon(status: KnowledgePageStatus): 'edit' | 'check-circle' | 'archive' {
    if (status === 'Published') return 'check-circle';
    if (status === 'Draft') return 'edit';
    return 'archive';
  }

  function statusIconClass(status: KnowledgePageStatus): string {
    if (status === 'Published') return 'text-[var(--color-success-600)]';
    if (status === 'Draft') return 'text-[var(--color-warning-600)]';
    return 'text-[var(--color-text-tertiary)]';
  }


  /**
   * Sibling position for a flat, always-expanded list. Siblings are the run of items at the
   * same depth up to the first shallower item; a deeper excursion between two of them does not
   * end the run. Derived from depth alone rather than parentId, because flattenTree normalises
   * an item whose parent is absent from the list to a root while leaving its parentId intact --
   * grouping by that value would disagree with what is actually rendered.
   */
  const positions = $derived.by(() => {
    const openGroup: (number | undefined)[] = [];
    const groupOf: number[] = [];
    const groupSize: number[] = [];

    for (const page of pages) {
      // Close every group deeper than this item; the one at its own depth stays open, so
      // siblings separated by their own children remain one set.
      openGroup.length = Math.min(openGroup.length, page.depth + 1);

      let group = openGroup[page.depth];
      if (group === undefined) {
        group = groupSize.length;
        openGroup[page.depth] = group;
        groupSize.push(0);
      }

      groupOf.push(group);
      groupSize[group] = (groupSize[group] ?? 0) + 1;
    }

    // posinset is the running count at the time the item was seen; setsize is only known once
    // its group has ended, hence the second pass.
    const running: number[] = [];
    return groupOf.map((group) => {
      running[group] = (running[group] ?? 0) + 1;
      return { posinset: running[group] ?? 1, setsize: groupSize[group] ?? 1 };
    });
  });

  /**
   * The single tab stop. Derived rather than stored, so a page that filtering removed cannot
   * leave the tree with no reachable item -- it falls back to the selection, then to the first.
   */
  let activeId = $state<string | null>(null);

  const activeIndex = $derived.by(() => {
    if (pages.length === 0) return -1;
    const fromActive = activeId ? pages.findIndex((page) => page.id === activeId) : -1;
    if (fromActive >= 0) return fromActive;
    const fromSelected = selectedPageId
      ? pages.findIndex((page) => page.id === selectedPageId)
      : -1;
    return fromSelected >= 0 ? fromSelected : 0;
  });

  let treeEl = $state<HTMLElement | null>(null);

  function focusIndex(index: number): void {
    const page = pages[index];
    if (!page) return;
    activeId = page.id;
    treeEl?.querySelectorAll<HTMLElement>('[role="treeitem"]')[index]?.focus();
  }

  /** Nearest preceding item one level up. ArrowLeft is movement here, not collapse. */
  function parentIndex(from: number): number {
    const depth = pages[from]?.depth;
    if (depth === undefined) return -1;
    for (let index = from - 1; index >= 0; index--) {
      const candidate = pages[index];
      if (candidate && candidate.depth < depth) return index;
    }
    return -1;
  }

  function onTreeKeydown(event: KeyboardEvent): void {
    const current = pages[activeIndex];
    if (!current) return;

    switch (event.key) {
      case 'ArrowDown':
        focusIndex(Math.min(activeIndex + 1, pages.length - 1));
        break;
      case 'ArrowUp':
        focusIndex(Math.max(activeIndex - 1, 0));
        break;
      case 'Home':
        focusIndex(0);
        break;
      case 'End':
        focusIndex(pages.length - 1);
        break;
      case 'ArrowRight': {
        // First child, when there is one. Nothing to expand: the list is always fully expanded,
        // which is why aria-expanded is absent rather than hardcoded true.
        const next = pages[activeIndex + 1];
        if (next && next.depth > current.depth) focusIndex(activeIndex + 1);
        break;
      }
      case 'ArrowLeft':
        focusIndex(parentIndex(activeIndex));
        break;
      case 'Enter':
      case ' ':
        // Opening is deliberately separate from moving: selecting a page fetches its body and
        // can raise the unsaved-changes guard, so arrowing through the tree must not do it.
        onSelectPage(current.id);
        break;
      default:
        return;
    }

    event.preventDefault();
  }
</script>

<aside
  class="flex h-full min-h-0 flex-col bg-[var(--color-bg-primary)]"
  aria-label="Knowledge pages"
>
  <div
    class="flex min-h-12 flex-shrink-0 items-center border-b border-[var(--color-border-primary)] p-2"
  >
    <div class="flex w-full items-center gap-1">
      <div class="relative flex-1">
        <label for={searchInputId} class="sr-only">Search Knowledge pages</label>
        <Icon
          name="search"
          size="sm"
          class="absolute left-2.5 top-1/2 -translate-y-1/2 text-[var(--color-text-tertiary)]"
        />
        <Input
          id={searchInputId}
          value={query}
          oninput={onSearchInput}
          placeholder="Search Knowledge..."
          type="text"
          size="sm"
          variant="flat"
          class="w-full pl-8"
        />
      </div>
      <Dropdown position="bottom-right">
        {#snippet trigger({ toggle, open })}
          <IconButton
            onclick={toggle}
            ariaHasPopup
            ariaExpanded={open}
            icon="more-vertical"
            size="sm"
            variant="ghost"
            title="Filter options"
          />
        {/snippet}
        <div class="min-w-[180px] py-1">
          <div class="px-3 py-1 text-xs font-semibold uppercase text-[var(--color-text-tertiary)]">
            Status
          </div>
          <Button
            variant="unstyled"
            class="flex w-full items-center gap-2 px-3 py-1.5 text-left text-sm transition-colors
              {statusFilter === 'all'
              ? 'bg-[var(--color-accent-primary)]/10 text-[var(--color-accent-text)]'
              : 'text-[var(--color-text-secondary)] hover:bg-[var(--color-bg-tertiary)]'}"
            onclick={() => onStatusFilterChange('all')}
          >
            <Icon name="layers" size="sm" />
            <span class="flex-1">All</span>
            {#if statusFilter === 'all'}
              <Icon name="check" size="sm" class="text-[var(--color-accent-text)]" />
            {/if}
            <span class="text-xs opacity-70">({counts.all})</span>
          </Button>
          {#each ['Draft', 'Published', 'Archived'] as KnowledgePageStatus[] as status (status)}
            <Button
              variant="unstyled"
              class="flex w-full items-center gap-2 px-3 py-1.5 text-left text-sm transition-colors
                {statusFilter === status
                ? 'bg-[var(--color-accent-primary)]/10 text-[var(--color-accent-text)]'
                : 'text-[var(--color-text-secondary)] hover:bg-[var(--color-bg-tertiary)]'}"
              onclick={() => onStatusFilterChange(status)}
            >
              <Icon name={statusIcon(status)} size="sm" class={statusIconClass(status)} />
              <span class="flex-1">{status}</span>
              {#if statusFilter === status}
                <Icon name="check" size="sm" class="text-[var(--color-accent-text)]" />
              {/if}
              <span class="text-xs opacity-70">({counts[status]})</span>
            </Button>
          {/each}
        </div>
      </Dropdown>
    </div>
  </div>

  <div
    class="flex flex-shrink-0 items-center gap-2 border-b border-[var(--color-border-primary)] bg-[var(--color-bg-secondary)] px-3 py-1.5"
  >
    <span class="w-4"></span>
    <span class="flex-1 text-xs font-medium uppercase text-[var(--color-text-tertiary)]">
      {searchResults ? 'Search results' : 'Page'}
    </span>
    <span class="w-8 text-right text-xs font-medium uppercase text-[var(--color-text-tertiary)]"
      >Rev</span
    >
  </div>

  <div class="flex-1 overflow-y-auto pb-1">
    {#if loading}
      <div class="flex justify-center p-6"><Spinner size="md" /></div>
    {:else if searchResults}
      {#each searchResults as result (result.id)}
        <Button
          variant="unstyled"
          class="w-full border-b border-[var(--color-border-primary)] px-3 py-2 text-left transition-colors hover:bg-[var(--color-bg-tertiary)]"
          onclick={() => onSelectPage(result.id)}
        >
          <div class="flex items-start gap-2">
            <Icon
              name="file-text"
              size="sm"
              class="mt-0.5 flex-shrink-0 text-[var(--color-info-600)]"
            />
            <div class="min-w-0 flex-1">
              <div class="truncate text-sm font-medium text-[var(--color-text-primary)]">
                {result.title}
              </div>
              <div class="mt-1 line-clamp-1 text-xs text-[var(--color-text-tertiary)]">
                {result.summary || 'No text content'}
              </div>
            </div>
            <span class="w-8 flex-shrink-0 text-right text-xs text-[var(--color-text-tertiary)]">
              r{result.revision}
            </span>
          </div>
        </Button>
      {:else}
        <div class="p-4 text-center text-xs text-[var(--color-text-tertiary)]">
          No matching pages
        </div>
      {/each}
    {:else}
      <!-- Empty state lives outside the tree: role="tree" may only own treeitems, so a bare
           div in here is an axe aria-required-children (WCAG 1.3.1) violation. -->
      {#if pages.length === 0}
        <div class="p-6 text-center text-sm text-[var(--color-text-tertiary)]">
          Create the first page in this Knowledge space.
        </div>
      {:else}
        <!-- tabindex="-1" keeps the container out of the tab order: the single tab stop is
             the active treeitem (roving tabindex), not the tree itself. -->
        <div
          bind:this={treeEl}
          role="tree"
          aria-label="Knowledge page tree"
          tabindex={-1}
          onkeydown={onTreeKeydown}
        >
          {#each pages as page, index (page.id)}
            <!-- A div rather than a Button: treeitem is not an allowed role for <button>, and a
                 native button would put every row back in the tab order. Keyboard activation is
                 handled once on the tree, not per row. -->
            <!-- svelte-ignore a11y_click_events_have_key_events -->
            <div
              class="flex w-full cursor-pointer items-center gap-2 border-b border-[var(--color-border-primary)] px-3 py-2 text-left transition-colors
                {selectedPageId === page.id
                ? 'bg-[var(--color-accent-primary)]/10 text-[var(--color-accent-text)]'
                : 'text-[var(--color-text-secondary)] hover:bg-[var(--color-bg-tertiary)]'}"
              style={`padding-left: ${0.75 + page.depth * 0.875}rem`}
              role="treeitem"
              tabindex={index === activeIndex ? 0 : -1}
              aria-level={page.depth + 1}
              aria-posinset={positions[index]?.posinset}
              aria-setsize={positions[index]?.setsize}
              aria-selected={selectedPageId === page.id}
              onclick={() => {
                activeId = page.id;
                onSelectPage(page.id);
              }}
              onfocus={() => (activeId = page.id)}
            >
              <Icon
                name={page.hasChildren ? 'folder-open' : 'file-text'}
                size="sm"
                class="flex-shrink-0 {page.status === 'Published'
                  ? 'text-[var(--color-success-600)]'
                  : page.status === 'Draft'
                    ? 'text-[var(--color-warning-600)]'
                    : 'text-[var(--color-text-tertiary)]'}"
              />
              <div class="min-w-0 flex-1">
                <div class="truncate text-sm font-medium">{page.title}</div>
                <div class="mt-1 text-[0.625rem] text-[var(--color-text-tertiary)]">
                  {page.status}
                </div>
              </div>
              <span class="w-8 flex-shrink-0 text-right text-xs text-[var(--color-text-tertiary)]">
                r{page.revision}
              </span>
            </div>
          {/each}
        </div>
      {/if}
    {/if}
  </div>
</aside>
