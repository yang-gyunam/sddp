<script lang="ts">
  /**
   * Audit Log Item
   * Displays a single audit log entry with icon, action, and metadata
   */

  import type { AuditActionType } from './types';

  interface Props {
    actionType: AuditActionType;
    action: string;
    user?: string;
    meta?: string;
  }

  let { actionType, action, user, meta }: Props = $props();

  const icons: Record<AuditActionType, string> = {
    login: 'sign-in',
    logout: 'sign-out',
    create: 'add',
    update: 'edit',
    delete: 'trash',
    approve: 'check',
    reject: 'close',
  };
</script>

<div class="audit-item">
  <div class="audit-icon {actionType}">
    <span class="codicon codicon-{icons[actionType]}"></span>
  </div>
  <div class="audit-content">
    <div class="audit-action">
      {#if user}
        User <strong>{user}</strong>
      {/if}
      {action}
    </div>
    {#if meta}
      <div class="audit-meta">{meta}</div>
    {/if}
  </div>
</div>

<style>
  .audit-item {
    display: flex;
    gap: 1rem;
    padding: 1rem;
    background: var(--color-bg-secondary);
    border: 1px solid var(--color-border);
    border-radius: 4px;
    transition: background 0.2s;
  }

  .audit-item:hover {
    background: var(--color-bg-tertiary);
  }

  .audit-icon {
    display: flex;
    align-items: center;
    justify-content: center;
    width: 2rem;
    height: 2rem;
    border-radius: 50%;
    flex-shrink: 0;
  }

  .audit-icon.login {
    background: var(--color-info)/15;
    color: var(--color-info);
  }

  .audit-icon.logout {
    background: var(--color-neutral-500)/15;
    color: var(--color-neutral-500);
  }

  .audit-icon.create {
    background: color-mix(in srgb, var(--color-success-500) 15%, transparent);
    color: var(--color-success-500);
  }

  .audit-icon.update {
    background: color-mix(in srgb, var(--color-warning-500) 15%, transparent);
    color: var(--color-warning-500);
  }

  .audit-icon.delete {
    background: color-mix(in srgb, var(--color-danger-500) 15%, transparent);
    color: var(--color-danger-500);
  }

  .audit-icon.approve {
    background: color-mix(in srgb, var(--color-success-500) 15%, transparent);
    color: var(--color-success-500);
  }

  .audit-icon.reject {
    background: color-mix(in srgb, var(--color-danger-500) 15%, transparent);
    color: var(--color-danger-500);
  }

  .audit-content {
    flex: 1;
    min-width: 0;
  }

  .audit-action {
    font-size: 0.875rem;
    color: var(--color-text-primary);
    margin-bottom: 0.25rem;
  }

  .audit-action strong {
    font-weight: 600;
  }

  .audit-meta {
    font-size: 0.75rem;
    color: var(--color-text-tertiary);
  }
</style>
