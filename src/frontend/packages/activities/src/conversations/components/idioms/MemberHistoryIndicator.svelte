<script lang="ts">
  import type { AiMemberHistoryStatus } from '../../types/member-history.types';

  let { history }: { history?: AiMemberHistoryStatus } = $props();

  const status = $derived(history?.status ?? 'unprepared');
  const preparing = $derived(status === 'queued' || status === 'running');
  const label = $derived(
    status === 'queued'
      ? 'History preparation queued'
      : status === 'running'
        ? `Preparing history (${history?.processedCount}/${history?.sourceCount})`
        : status === 'failed'
          ? `History preparation failed: ${history?.errorCode ?? 'Unknown error'}`
          : 'History not prepared'
  );
</script>

{#if status !== 'ready'}
  <span
    role="img"
    aria-label={label}
    title={label}
    class="absolute -bottom-0.5 -right-0.5 w-2.5 h-2.5 rounded-full border-2 border-[var(--color-bg-secondary)] {status ===
    'failed'
      ? 'bg-[var(--color-error-500)]'
      : 'bg-[var(--color-orange-500)]'} {preparing ? 'motion-safe:animate-pulse' : ''}"
  ></span>
{/if}
