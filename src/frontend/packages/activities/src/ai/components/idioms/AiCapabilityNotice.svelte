<script lang="ts">
  import { Button } from '@sddp/ui';
  import { createResourceAiCapability } from '../../stores/resource-ai-capability.svelte';
  import type { AiFeature } from '../../services/AiStatusService';
  let {
    tenantId = '',
    projectId,
    feature = 'ai.conversation',
  }: {
    tenantId?: string;
    projectId?: string;
    feature?: AiFeature;
  } = $props();
  const capability = createResourceAiCapability(() => ({ tenantId, projectId }));
  const view = $derived(capability.view(feature));
</script>

{#if view.visible || view.distributionUnavailable}
  <div class="px-4 py-2 text-xs text-[var(--color-text-muted)]" role="status">
    AI: {view.message}
    {#if view.unavailable}
      <Button variant="ghost" size="sm" onclick={capability.refresh}>Retry</Button>
    {/if}
  </div>
{/if}
