<script lang="ts">
  /**
   * Stat Badge
   * Small badge for displaying statistics
   */

  interface Props {
    value: number | string;
    label: string;
    variant?: 'default' | 'success' | 'warning' | 'error';
  }

  let { value, label, variant = 'default' }: Props = $props();
</script>

<div class="stat-badge" class:success={variant === 'success'} class:warning={variant === 'warning'} class:error={variant === 'error'}>
  <span class="stat-value">{value}</span>
  <span class="stat-label">{label}</span>
</div>

<style>
  .stat-badge {
    display: inline-flex;
    flex-direction: column;
    align-items: center;
    padding: 0.5rem 0.75rem;
    background: var(--bg-secondary);
    border: 1px solid var(--border-color);
    border-radius: 4px;
    font-size: 0.75rem;
  }

  .stat-badge.success {
    background: color-mix(in srgb, var(--color-success-500) 10%, transparent);
    border-color: var(--color-success-500);
  }

  .stat-badge.warning {
    background: color-mix(in srgb, var(--color-warning-500) 10%, transparent);
    border-color: var(--color-warning-500);
  }

  .stat-badge.error {
    background: color-mix(in srgb, var(--color-error-500) 10%, transparent);
    border-color: var(--color-error-500);
  }

  .stat-value {
    font-size: 1.25rem;
    font-weight: 700;
    color: var(--text-primary);
  }

  .stat-label {
    color: var(--text-secondary);
    margin-top: 0.125rem;
  }
</style>
