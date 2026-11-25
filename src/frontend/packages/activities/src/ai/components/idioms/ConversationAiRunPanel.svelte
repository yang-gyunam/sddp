<script lang="ts">
  import { Button } from '@sddp/ui';
  import type { ConversationAiRun } from '../../stores/conversation-ai-run.svelte';

  let { run }: { run: ConversationAiRun } = $props();
</script>

<div class="shrink-0 px-4 py-2 text-xs text-[var(--color-text-muted)]" role="status" aria-live="polite">
  <span>AI: {run.view.message}</span>
  {#if run.view.unavailable}
    <Button variant="ghost" size="sm" onclick={run.refreshAvailability}>Refresh AI availability</Button>
  {/if}
</div>
