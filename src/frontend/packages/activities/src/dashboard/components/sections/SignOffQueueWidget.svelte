<!-- Section: SignOffQueueWidget — Dashboard > My -->
<script lang="ts">
  /**
   * SignOffQueueWidget
   * Displays pending sign-off items with urgency badges.
   */
  import { Button } from '@sddp/ui';
  import { setActiveActivityItem } from '@sddp/shell';
  import type { MySignOffQueue, PendingSignOffItem } from '../../types';

  interface Props {
    data: MySignOffQueue | null;
  }

  let { data }: Props = $props();

  const pendingCount = $derived(data?.pendingCount ?? 0);
  const items = $derived((data?.items ?? []).slice(0, 5));

  function urgencyColor(urgency: PendingSignOffItem['urgency']): string {
    if (urgency === 'overdue') return 'var(--color-error-500)';
    if (urgency === 'urgent') return 'var(--color-warning-500)';
    return 'var(--color-text-tertiary)';
  }

  function urgencyBg(urgency: PendingSignOffItem['urgency']): string {
    if (urgency === 'overdue') return 'var(--color-error-12)';
    if (urgency === 'urgent') return 'var(--color-warning-12)';
    return 'var(--color-bg-tertiary)';
  }

  function handleItemClick(_item: PendingSignOffItem) {
    setActiveActivityItem('projects', true);
  }
</script>

<div class="signoff-queue-widget">
  <div class="widget-header">
    <h2 class="widget-title">Sign-Off Queue</h2>
    {#if pendingCount > 0}
      <span class="pending-badge">{pendingCount}</span>
    {/if}
  </div>

  {#if items.length === 0}
    <div class="empty-state">
      <span class="codicon codicon-check-all"></span>
      <span>No pending sign-offs</span>
    </div>
  {:else}
    <div class="item-list">
      {#each items as item (item.signOffId)}
        <Button
          variant="unstyled"
          class="queue-item"
          onclick={() => handleItemClick(item)}
        >
          <div class="item-main">
            <span class="spec-code">{item.specCode}</span>
            <span class="spec-title">{item.specTitle}</span>
          </div>
          <div class="item-meta">
            <span class="project-name">{item.projectName}</span>
            <span
              class="waiting-badge"
              style="color: {urgencyColor(item.urgency)}; background: {urgencyBg(item.urgency)};"
            >
              {item.waitingDays}d
            </span>
          </div>
        </Button>
      {/each}
    </div>
  {/if}
</div>

<style>
  .signoff-queue-widget {
    padding: 1rem;
    background: var(--color-bg-secondary);
    border: 1px solid var(--color-border);
    border-radius: 8px;
  }

  .widget-header {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    margin-bottom: 0.75rem;
  }

  .widget-title {
    margin: 0;
    font-size: 0.875rem;
    font-weight: 600;
    color: var(--color-text-primary);
    flex: 1;
  }

  .pending-badge {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    min-width: 20px;
    height: 20px;
    padding: 0 6px;
    border-radius: 10px;
    background: var(--color-error-500);
    color: white;
    font-size: 0.6875rem;
    font-weight: 700;
    line-height: 1;
  }

  .item-list {
    display: flex;
    flex-direction: column;
    gap: 0.375rem;
    max-height: 220px;
    overflow-y: auto;
  }

  :global(.queue-item) {
    display: flex;
    flex-direction: column;
    gap: 0.25rem;
    width: 100%;
    text-align: left;
    padding: 0.5rem 0.625rem;
    border-radius: 6px;
    border: 1px solid var(--color-border);
    background: var(--color-bg-primary);
    cursor: pointer;
    transition: border-color 0.15s ease, background-color 0.15s ease;
  }

  :global(.queue-item:hover) {
    border-color: var(--color-accent-primary);
    background: color-mix(in srgb, var(--color-accent-primary) 6%, transparent);
  }

  .item-main {
    display: flex;
    align-items: center;
    gap: 0.375rem;
    overflow: hidden;
  }

  .spec-code {
    font-size: 0.6875rem;
    font-weight: 700;
    color: var(--color-accent-primary);
    white-space: nowrap;
    flex-shrink: 0;
  }

  .spec-title {
    font-size: 0.8125rem;
    font-weight: 500;
    color: var(--color-text-primary);
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
  }

  .item-meta {
    display: flex;
    align-items: center;
    gap: 0.5rem;
  }

  .project-name {
    font-size: 0.6875rem;
    color: var(--color-text-tertiary);
    flex: 1;
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
  }

  .waiting-badge {
    display: inline-flex;
    align-items: center;
    padding: 1px 6px;
    border-radius: 4px;
    font-size: 0.6875rem;
    font-weight: 600;
    white-space: nowrap;
    flex-shrink: 0;
  }

  .empty-state {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    height: 100px;
    gap: 0.5rem;
    color: var(--color-text-tertiary);
    font-size: 0.875rem;
  }

  .empty-state .codicon {
    font-size: 1.75rem;
    opacity: 0.5;
  }
</style>
