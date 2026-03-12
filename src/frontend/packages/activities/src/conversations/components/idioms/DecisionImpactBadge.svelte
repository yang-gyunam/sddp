<!--
  DecisionImpactBadge Component
  Shows how many entities are impacted by a Decision message
-->
<script lang="ts">
  import { untrack } from 'svelte';
  import { Icon, Button } from '@sddp/ui';
  import { getAuthState } from '@sddp/shell/auth';
  import { getDecisionImpact } from '../../../relationship/services/RelationshipService';
  import { ENTITY_TYPE_STYLES, type DecisionImpactItem } from '../../../relationship/types';

  interface Props {
    messageId: string;
    projectId?: string;
    referenceCount?: number;
  }

  let {
    messageId,
    projectId = '',
    referenceCount = 0,
  }: Props = $props();

  let items = $state<DecisionImpactItem[]>([]);
  let loading = $state(false);
  let showDetail = $state(false);
  let prevMessageId = $state<string | null>(null);

  const totalCount = $derived(items.length + referenceCount);

  $effect(() => {
    if (!messageId) { prevMessageId = null; return; }
    if (messageId === prevMessageId) return;
    prevMessageId = messageId;
    untrack(() => {
      const authState = getAuthState();
      const tenantId = authState.user?.tenantId ?? '';
      const resolvedProjectId = projectId || '';
      if (!tenantId || !resolvedProjectId) return;

      loading = true;
      getDecisionImpact(tenantId, resolvedProjectId, messageId)
        .then((data) => {
          items = data.items as DecisionImpactItem[];
        })
        .catch(() => {
          items = [];
        })
        .finally(() => {
          loading = false;
        });
    });
  });

  function handleToggleDetail(e: MouseEvent): void {
    e.stopPropagation();
    if (totalCount > 0) {
      showDetail = !showDetail;
    }
  }

  function handleCloseDetail(e: MouseEvent): void {
    e.stopPropagation();
    showDetail = false;
  }

  function getEntityIcon(entityType: string): string {
    const style = ENTITY_TYPE_STYLES[entityType as keyof typeof ENTITY_TYPE_STYLES];
    return style?.icon ?? 'link';
  }
</script>

{#if !loading && totalCount > 0}
  <div class="relative inline-flex">
    <Button
      variant="unstyled"
      class="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-medium
        bg-[var(--color-success-500)]/10 text-[var(--color-success-600)]
        hover:bg-[var(--color-success-500)]/20 transition-colors"
      title="{totalCount} impacted {totalCount === 1 ? 'entity' : 'entities'}"
      onclick={handleToggleDetail}
    >
      <Icon name="git-merge" size="xs" />
      <span>{totalCount} impact{totalCount !== 1 ? 's' : ''}</span>
    </Button>

    {#if showDetail}
      <!-- svelte-ignore a11y_click_events_have_key_events -->
      <!-- svelte-ignore a11y_no_static_element_interactions -->
      <div
        class="absolute top-full left-0 mt-1 z-30
          bg-[var(--color-bg-secondary)] border border-[var(--color-border)]
          rounded-lg shadow-lg min-w-[220px] max-w-[320px] py-2"
        onclick={handleCloseDetail}
      >
        <div class="px-3 pb-1.5 mb-1 border-b border-[var(--color-border-secondary)]">
          <span class="text-xs font-medium text-[var(--color-text-secondary)]">
            Decision Impact ({totalCount})
          </span>
        </div>

        <div class="max-h-[200px] overflow-y-auto">
          {#each items as item (item.entityId)}
            <div class="flex items-center gap-2 px-3 py-1.5 text-xs hover:bg-[var(--color-bg-tertiary)]">
              <Icon name={getEntityIcon(item.entityType)} size="xs" class="text-[var(--color-text-muted)] flex-shrink-0" />
              <span class="truncate text-[var(--color-text-primary)]">{item.label}</span>
              <span class="ml-auto text-[var(--color-text-muted)] flex-shrink-0">{item.relationType}</span>
            </div>
          {/each}

          {#if referenceCount > 0}
            <div class="flex items-center gap-2 px-3 py-1.5 text-xs text-[var(--color-text-muted)]">
              <Icon name="link" size="xs" class="flex-shrink-0" />
              <span>{referenceCount} reference{referenceCount !== 1 ? 's' : ''}</span>
            </div>
          {/if}
        </div>
      </div>
    {/if}
  </div>
{/if}
