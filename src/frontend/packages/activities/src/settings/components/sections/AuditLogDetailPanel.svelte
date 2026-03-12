<!-- Section: AuditLogDetailPanel — Settings > System Audit Logs -->
<script lang="ts">
  /**
   * AuditLogDetailPanel
   * Right panel for viewing audit log entry details
   */

  import { formatDateTime } from '@sddp/shell';
  import { Icon, IconButton, Button } from '@sddp/ui';
  import DetailHeader from '../../../shared/components/idioms/DetailHeader.svelte';
  import { getAuditActionColor } from '../../../shared/constants/semanticColors';
  import type { AuditLog } from '../../types';

  interface Props {
    log: AuditLog | null;
    onClose: () => void;
  }

  let { log, onClose }: Props = $props();

  let showDetails = $state(false);

  function formatDateStr(dateStr: string): string {
    return formatDateTime(dateStr, { month: 'short', second: '2-digit' });
  }

  function formatJson(obj: Record<string, unknown>): string {
    return JSON.stringify(obj, null, 2);
  }

  function getActionColor(action: string): string {
    return getAuditActionColor(action);
  }
</script>

<div class="panel">
  {#if log}
    <DetailHeader>
      {#snippet leading()}
        <Icon name="file-text" size="md" class="text-[var(--color-accent-primary)]" />
      {/snippet}
      <span class="text-sm font-semibold text-[var(--color-text-primary)]">Audit Log Detail</span>
      {#snippet actions()}
        <IconButton icon="x" onclick={onClose} title="Close" />
      {/snippet}
    </DetailHeader>

    <div class="panel-body">
      <!-- Action + Timestamp Header -->
      <div class="log-header">
        <span
          class="action-badge"
          style="background: {getActionColor(log.action)}20; color: {getActionColor(log.action)}"
        >
          {log.action}
        </span>
        <span class="timestamp">{formatDateStr(log.timestamp)}</span>
      </div>

      <!-- Detail Grid -->
      <div class="detail-grid">
        <div class="detail-item">
          <span class="detail-label">User</span>
          <span class="detail-value">{log.userName}</span>
        </div>
        <div class="detail-item">
          <span class="detail-label">User ID</span>
          <span class="detail-value mono">{log.userId}</span>
        </div>
        <div class="detail-item">
          <span class="detail-label">Resource Type</span>
          <span class="detail-value capitalize">{log.resourceType}</span>
        </div>
        <div class="detail-item">
          <span class="detail-label">Resource ID</span>
          <span class="detail-value mono">{log.resourceId}</span>
        </div>
        <div class="detail-item">
          <span class="detail-label">IP Address</span>
          <span class="detail-value mono">{log.ipAddress}</span>
        </div>
        <div class="detail-item">
          <span class="detail-label">Log ID</span>
          <span class="detail-value mono">{log.id}</span>
        </div>
      </div>

      <!-- Additional Details -->
      {#if log.details && Object.keys(log.details).length > 0}
        <div class="details-section">
          <Button
            variant="unstyled"
            class="details-toggle"
            onclick={() => (showDetails = !showDetails)}
          >
            <Icon name={showDetails ? 'chevron-down' : 'chevron-right'} size="sm" />
            <span>Additional Details</span>
          </Button>
          {#if showDetails}
            <pre class="details-json">{formatJson(log.details)}</pre>
          {/if}
        </div>
      {/if}
    </div>
  {/if}
</div>

<style>
  @import '../../styles/detail-panel.css';

  .log-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
  }

  .action-badge {
    display: inline-block;
    padding: 0.25rem 0.75rem;
    border-radius: 4px;
    font-size: 0.8125rem;
    font-weight: 500;
    text-transform: capitalize;
  }

  .timestamp {
    font-size: 0.8125rem;
    color: var(--color-text-secondary, #6b7280);
  }

  .detail-value.mono {
    font-family: 'Consolas', 'Monaco', monospace;
    font-size: 0.75rem;
    color: var(--color-text-secondary, #6b7280);
    word-break: break-all;
  }

  .detail-value.capitalize {
    text-transform: capitalize;
  }

  .details-section {
    border-top: 1px solid var(--color-border-primary, #e5e7eb);
    padding-top: 1rem;
  }

  :global(.details-toggle) {
    display: flex;
    align-items: center;
    gap: 0.375rem;
    background: none;
    border: none;
    padding: 0;
    cursor: pointer;
    font-size: 0.8125rem;
    font-weight: 600;
    color: var(--color-text-secondary, #6b7280);
  }

  :global(.details-toggle:hover) {
    color: var(--color-text-primary, #111);
  }

  .details-json {
    margin: 0.75rem 0 0;
    padding: 0.75rem;
    background: var(--color-bg-tertiary, #f3f4f6);
    border: 1px solid var(--color-border-primary, #e5e7eb);
    border-radius: 4px;
    font-family: 'Consolas', 'Monaco', monospace;
    font-size: 0.75rem;
    color: var(--color-text-primary, #111);
    overflow-x: auto;
    white-space: pre-wrap;
    word-break: break-all;
  }
</style>
