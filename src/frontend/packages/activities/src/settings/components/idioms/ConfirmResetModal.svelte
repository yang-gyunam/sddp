<script lang="ts">
  /**
   * ConfirmResetModal
   * Confirmation modal for destructive data reset operations.
   * Requires user to type the exact confirmation code before enabling the action.
   */

  import { Modal } from '@sddp/shell';
  import { Button, Input } from '@sddp/ui';

  interface Props {
    open: boolean;
    title: string;
    description: string;
    confirmationCode: string;
    stats?: Record<string, number>;
    loading?: boolean;
    onConfirm: () => void;
    onCancel: () => void;
  }

  let {
    open,
    title,
    description,
    confirmationCode,
    stats,
    loading = false,
    onConfirm,
    onCancel,
  }: Props = $props();

  let inputValue = $state('');
  let isMatch = $derived(inputValue === confirmationCode);

  function handleConfirm() {
    if (!isMatch || loading) return;
    onConfirm();
  }

  function handleClose() {
    inputValue = '';
    onCancel();
  }
</script>

<Modal {open} title={title} size="md" onClose={handleClose}>
  <div class="confirm-reset-content">
    <p class="confirm-description">{description}</p>

    {#if stats && Object.keys(stats).length > 0}
      <div class="stats-section">
        <h4 class="stats-title">Data to be deleted:</h4>
        <div class="stats-grid">
          {#each Object.entries(stats) as [table, count] (table)}
            <div class="stat-row">
              <span class="stat-label">{table}</span>
              <span class="stat-value">{count}</span>
            </div>
          {/each}
        </div>
      </div>
    {/if}

    <div class="confirm-input-section">
      <p class="confirm-instruction">
        Type <code class="confirm-code">{confirmationCode}</code> to confirm:
      </p>
      <Input
        bind:value={inputValue}
        placeholder={confirmationCode}
        disabled={loading}
      />
    </div>

    <div class="confirm-actions">
      <Button variant="ghost" onclick={handleClose} disabled={loading}>
        Cancel
      </Button>
      <Button
        variant="danger"
        onclick={handleConfirm}
        disabled={!isMatch || loading}
      >
        {loading ? 'Resetting...' : 'Reset Data'}
      </Button>
    </div>
  </div>
</Modal>

<style>
  .confirm-reset-content {
    display: flex;
    flex-direction: column;
    gap: 1rem;
    padding: 0.5rem 0;
  }

  .confirm-description {
    margin: 0;
    font-size: 0.875rem;
    color: var(--text-secondary);
    line-height: 1.5;
  }

  .stats-section {
    padding: 0.75rem;
    background: var(--bg-secondary);
    border-radius: 6px;
    border: 1px solid var(--border-primary);
  }

  .stats-title {
    margin: 0 0 0.5rem;
    font-size: 0.8125rem;
    font-weight: 600;
    color: var(--text-primary);
  }

  .stats-grid {
    display: flex;
    flex-direction: column;
    gap: 0.25rem;
  }

  .stat-row {
    display: flex;
    justify-content: space-between;
    font-size: 0.8125rem;
  }

  .stat-label {
    color: var(--text-secondary);
    text-transform: capitalize;
  }

  .stat-value {
    color: var(--text-primary);
    font-weight: 500;
    font-variant-numeric: tabular-nums;
  }

  .confirm-input-section {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
  }

  .confirm-instruction {
    margin: 0;
    font-size: 0.8125rem;
    color: var(--text-primary);
  }

  .confirm-code {
    padding: 0.125rem 0.375rem;
    background: var(--bg-tertiary);
    border-radius: 3px;
    font-family: monospace;
    font-size: 0.8125rem;
    color: var(--error-color, #ef4444);
    font-weight: 600;
  }

  .confirm-actions {
    display: flex;
    justify-content: flex-end;
    gap: 0.5rem;
    padding-top: 0.5rem;
    border-top: 1px solid var(--border-primary);
  }
</style>
