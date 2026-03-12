<!-- Activity: Projects > Nav: Traceability (project-{id}-traceability) | Screen ID: PRJ-TRACE-001 -->
<script lang="ts">
  import { untrack } from 'svelte';
  import { Icon, IconButton, Button, Spinner, ResizeHandle } from '@sddp/ui';
  import { PageShell, PageHeader, PageBody, EmptyState, Dropdown, getTabState, setTabState } from '@sddp/shell';
  import { getAuthState } from '@sddp/shell/auth';
  import type { ProjectDetail } from '../../types';
  import type { EntityType } from '../../../relationship/types';
  import { ENTITY_TYPE_STYLES } from '../../../relationship/types';
  import { getTraceabilityMap } from '../../../relationship/services/TraceabilityService';
  import { TangledTreeGraph } from '../../../relationship/components/sections';
  import type { TraceabilityMapData, DetailPanelEntity } from '../../../relationship/types';
  import EntityFullDetailTab from '../../../shared/components/sections/EntityFullDetailTab.svelte';

  interface Props {
    projectId: string;
    projectName?: string;
    project?: ProjectDetail;
    tabId?: string;
    class?: string;
  }

  let { projectId, projectName = '', project, tabId = '', class: className = '' }: Props = $props();

  const pageTitle = 'Traceability';
  const pageMeta = $derived(project?.name || projectName || undefined);

  let data = $state<TraceabilityMapData | null>(null);
  let loading = $state(true);
  let error = $state<string | null>(null);

  // Filter state
  let showCrossLinks = $state(true);
  let showLabels = $state(true);
  let orientation = $state<'horizontal' | 'vertical'>('horizontal');

  // Detail panel state
  let openedEntities = $state<DetailPanelEntity[]>([]);
  let activeEntityId = $state('');
  const showDetailPanel = $derived(openedEntities.length > 0);
  const activeEntity = $derived(openedEntities.find((e) => e.id === activeEntityId) ?? null);

  // Detail panel resize state (vertical: height-based)
  let detailPanelHeight = $state(300);
  const MIN_DETAIL_HEIGHT = 150;
  const MAX_DETAIL_HEIGHT = 600;
  let isResizing = $state(false);
  let resizeRafId: number | null = null;

  // Tab state persistence
  interface TraceTabState {
    showCrossLinks: boolean;
    showLabels: boolean;
    orientation: 'horizontal' | 'vertical';
    detailPanelHeight: number;
  }

  const tabStateKey = $derived(tabId || `traceability-${projectId}`);
  let restoredKey = $state<string | null>(null);
  let isRestored = $state(false);

  // Restore tab state
  $effect(() => {
    if (!tabStateKey || restoredKey === tabStateKey) return;
    const saved = getTabState<TraceTabState>(tabStateKey);
    if (saved) {
      showCrossLinks = saved.showCrossLinks ?? true;
      showLabels = saved.showLabels ?? true;
      orientation = saved.orientation ?? 'horizontal';
      detailPanelHeight = saved.detailPanelHeight ?? 300;
    }
    restoredKey = tabStateKey;
    isRestored = true;
  });

  // Save tab state
  $effect(() => {
    if (!tabStateKey || !isRestored) return;
    setTabState<TraceTabState>(tabStateKey, {
      showCrossLinks,
      showLabels,
      orientation,
      detailPanelHeight,
    });
  });

  $effect(() => {
    untrack(() => loadData());
  });

  async function loadData() {
    loading = true;
    error = null;
    try {
      const authState = getAuthState();
      const tenantId = authState.user?.tenantId;
      if (!tenantId) throw new Error('No tenant context');
      data = await getTraceabilityMap(tenantId, projectId);
    } catch (err) {
      console.error('Failed to load traceability map:', err);
      error = err instanceof Error ? err.message : 'Failed to load traceability map';
    } finally {
      loading = false;
    }
  }

  function handleNodeClick(nodeId: string, entityType: EntityType) {
    const node = data?.nodes.find((n) => n.id === nodeId);
    if (!node) return;

    const entity: DetailPanelEntity = {
      id: nodeId,
      entityType,
      label: node.code ?? node.label,
    };

    const existingIndex = openedEntities.findIndex((e) => e.id === nodeId);
    if (existingIndex >= 0) {
      activeEntityId = nodeId;
    } else {
      openedEntities = [...openedEntities, entity];
      activeEntityId = nodeId;
    }
  }

  function handleTabChange(entityId: string) {
    activeEntityId = entityId;
  }

  function handleCloseTab(entityId: string, e?: MouseEvent) {
    e?.stopPropagation();
    openedEntities = openedEntities.filter((ent) => ent.id !== entityId);
    if (activeEntityId === entityId) {
      activeEntityId = openedEntities[openedEntities.length - 1]?.id ?? '';
    }
  }

  function handleCloseAllTabs() {
    openedEntities = [];
    activeEntityId = '';
  }

  function handleRefresh() {
    loadData();
  }

  function handleResizeStart(e: PointerEvent) {
    e.preventDefault();
    isResizing = true;
    const startY = e.clientY;
    const startHeight = detailPanelHeight;

    function onPointerMove(moveEvent: PointerEvent) {
      if (resizeRafId !== null) cancelAnimationFrame(resizeRafId);
      resizeRafId = requestAnimationFrame(() => {
        const delta = startY - moveEvent.clientY;
        detailPanelHeight = Math.min(MAX_DETAIL_HEIGHT, Math.max(MIN_DETAIL_HEIGHT, startHeight + delta));
        resizeRafId = null;
      });
    }

    function onPointerUp() {
      if (resizeRafId !== null) {
        cancelAnimationFrame(resizeRafId);
        resizeRafId = null;
      }
      isResizing = false;
      document.removeEventListener('pointermove', onPointerMove);
      document.removeEventListener('pointerup', onPointerUp);
    }

    document.addEventListener('pointermove', onPointerMove);
    document.addEventListener('pointerup', onPointerUp);
  }
</script>

<PageShell class={className}>
  <PageHeader title={pageTitle} meta={pageMeta} {loading}>
    {#snippet actions()}
      <div class="flex items-center gap-1">
        <IconButton
          icon="refresh-cw"
          size="sm"
          variant="ghost"
          title="Refresh"
          onclick={handleRefresh}
        />
        <Dropdown position="bottom-right">
          {#snippet trigger()}
            <IconButton icon="more-vertical" size="sm" variant="ghost" title="Options" />
          {/snippet}
          <div class="py-1 min-w-[180px]">
            <Button
              variant="unstyled"
              class="w-full flex items-center gap-2 px-3 py-1.5 text-xs text-[var(--color-text-primary)] hover:bg-[var(--color-bg-tertiary)] transition-colors"
              onclick={() => { orientation = orientation === 'horizontal' ? 'vertical' : 'horizontal'; }}
            >
              <Icon name={orientation === 'horizontal' ? 'flip-horizontal-2' : 'flip-vertical-2'} size="xs" />
              <span>{orientation === 'horizontal' ? 'Switch to vertical' : 'Switch to horizontal'}</span>
            </Button>
            <Button
              variant="unstyled"
              class="w-full flex items-center gap-2 px-3 py-1.5 text-xs text-[var(--color-text-primary)] hover:bg-[var(--color-bg-tertiary)] transition-colors"
              onclick={() => { showLabels = !showLabels; }}
            >
              <Icon name={showLabels ? 'tag-x' : 'tag'} size="xs" />
              <span>{showLabels ? 'Hide labels' : 'Show labels'}</span>
            </Button>
            <Button
              variant="unstyled"
              class="w-full flex items-center gap-2 px-3 py-1.5 text-xs text-[var(--color-text-primary)] hover:bg-[var(--color-bg-tertiary)] transition-colors"
              onclick={() => { showCrossLinks = !showCrossLinks; }}
            >
              <Icon name={showCrossLinks ? 'link-2-off' : 'link'} size="xs" />
              <span>{showCrossLinks ? 'Hide cross-links' : 'Show cross-links'}</span>
            </Button>
          </div>
        </Dropdown>
      </div>
    {/snippet}
  </PageHeader>

  <PageBody class="p-0 flex flex-col" scrollable={false}>
    {#if loading && !data}
      <div class="flex items-center justify-center h-full">
        <Spinner size="lg" />
      </div>
    {:else if error}
      <div class="flex flex-col items-center justify-center h-full gap-3 text-center px-8">
        <Icon name="alert-circle" size="lg" class="text-red-500 opacity-50" />
        <p class="text-sm text-[var(--color-text-secondary)]">{error}</p>
        <Button
          variant="unstyled"
          class="text-xs text-[var(--color-accent-primary)] hover:underline"
          onclick={handleRefresh}
        >
          Retry
        </Button>
      </div>
    {:else if data && data.nodes.length === 0}
      <div class="flex flex-col items-center justify-center h-full gap-3 text-center px-8">
        <Icon name="network" size="xl" class="text-[var(--color-text-tertiary)] opacity-30" />
        <p class="text-sm text-[var(--color-text-tertiary)]">
          No entities found in this project
        </p>
      </div>
    {:else if data}
      <!-- Stats bar (full width) -->
      <div class="flex-shrink-0 flex items-center justify-end gap-4 px-4 min-h-12 border-b border-[var(--color-border-primary)] text-xs text-[var(--color-text-tertiary)]">
        <span>{data.stats.conversationCount} conversations</span>
        <span>{data.stats.requirementCount} requirements</span>
        <span>{data.stats.specCount} specs</span>
        <span>{data.stats.taskCount} tasks</span>
        <span>{data.stats.artifactCount} artifacts</span>
        {#if data.stats.crossLinkCount > 0}
          <span>{data.stats.crossLinkCount} cross-links</span>
        {/if}
      </div>

      <!-- Split layout: Graph (top) + Detail Panel (bottom) -->
      <div class="flex flex-col flex-1 min-h-0">
        <!-- Top: Graph panel (flexible) -->
        <div
          class="flex-1 min-w-0 min-h-0 relative"
          class:graph-resizing={isResizing}
        >
          <TangledTreeGraph
            nodes={data.nodes}
            crossLinks={data.crossLinks}
            {orientation}
            {showCrossLinks}
            {showLabels}
            selectedNodeId={activeEntityId || null}
            onNodeClick={handleNodeClick}
          />
        </div>

        <!-- Resize Handle (horizontal) -->
        <div class="relative z-10 flex-shrink-0">
          <ResizeHandle
            orientation="horizontal"
            {isResizing}
            onpointerdown={handleResizeStart}
            ariaLabel="Resize graph and detail panels"
          />
        </div>

        <!-- Bottom: Detail Panel -->
        <div
          class="flex-shrink-0 flex flex-col border-t border-[var(--color-border-primary)] overflow-hidden bg-[var(--color-bg-secondary)]"
          class:detail-resizing={isResizing}
          style="height: {detailPanelHeight}px;"
        >
          {#if showDetailPanel}
            <!-- Custom closable tab bar -->
            <div class="flex items-center border-b border-[var(--color-border-secondary)] bg-[var(--color-surface-50)] flex-shrink-0">
              <div class="flex-1 min-w-0 flex items-center overflow-x-auto px-1">
                {#each openedEntities as entity (entity.id)}
                  {@const entityStyle = ENTITY_TYPE_STYLES[entity.entityType]}
                  <Button
                    variant="unstyled"
                    onclick={() => handleTabChange(entity.id)}
                    class="group flex items-center gap-1.5 px-3 py-1.5 text-xs font-medium transition-colors border-b-2 flex-shrink-0
                           {activeEntityId === entity.id
                             ? 'border-[var(--color-accent-primary)] text-[var(--color-text-primary)]'
                             : 'border-transparent text-[var(--color-text-muted)] hover:text-[var(--color-text-secondary)]'}"
                  >
                    {#if entityStyle}
                      <Icon name={entityStyle.icon} size="xs" class={entityStyle.color} />
                    {/if}
                    <span class="max-w-[100px] truncate">{entity.label}</span>
                    <Button
                      variant="unstyled"
                      tabindex={-1}
                      aria-label="Close tab"
                      onclick={(e: MouseEvent) => handleCloseTab(entity.id, e)}
                      class="ml-0.5 w-4 h-4 flex items-center justify-center rounded opacity-0 group-hover:opacity-100 hover:bg-[var(--color-surface-200)] transition-opacity text-[var(--color-text-muted)] cursor-pointer"
                      title="Close tab"
                    >
                      <Icon name="x" size="xs" />
                    </Button>
                  </Button>
                {/each}
              </div>
              {#if openedEntities.length > 1}
                <div class="flex-shrink-0 px-1">
                  <IconButton
                    icon="x"
                    size="sm"
                    variant="ghost"
                    title="Close all tabs"
                    onclick={handleCloseAllTabs}
                  />
                </div>
              {/if}
            </div>

            <!-- Detail content -->
            <div class="flex-1 min-h-0 overflow-auto">
              {#if activeEntity}
                {#key activeEntity.id}
                  <EntityFullDetailTab
                    entityId={activeEntity.id}
                    entityType={activeEntity.entityType}
                    {projectId}
                  />
                {/key}
              {/if}
            </div>
          {:else}
            <!-- Empty state: no node selected -->
            <div class="flex-1 min-h-0">
              <EmptyState
                icon="mouse-pointer"
                heading="Select a node to view details"
                subtext="Click on a node in the graph above to view its details here."
                iconSize="lg"
              />
            </div>
          {/if}
        </div>
      </div>
    {/if}
  </PageBody>
</PageShell>

<style>
  .graph-resizing,
  .detail-resizing {
    will-change: height;
    pointer-events: none;
    user-select: none;
  }
</style>
