<script lang="ts">
  import { onMount } from 'svelte';
  import { Button } from '@sddp/ui';
  import Modal from './Modal.svelte';
  import {
    answerTabClosePrompt,
    installTabClosePromptDecider,
    tabClosePromptStore,
    type TabClosePromptState,
  } from '../../stores/tab-close-prompt.store';

  let promptState = $state<TabClosePromptState>(tabClosePromptStore.get());

  onMount(() => {
    const stopListening = tabClosePromptStore.subscribe((state) => {
      promptState = state;
    });
    const uninstall = installTabClosePromptDecider();

    return () => {
      uninstall();
      stopListening();
    };
  });

  const prompt = $derived(promptState.current);
  const queued = $derived(promptState.queued);
</script>

<Modal
  open={prompt !== null}
  title="Unsaved changes"
  size="sm"
  closeOnOverlay
  closeOnEscape
  onClose={() => answerTabClosePrompt('cancel')}
>
  <p class="text-sm text-[var(--color-text-primary)]">
    {#if prompt}
      Save your changes to <span class="font-medium">{prompt.title || 'this tab'}</span> before closing
      it?
    {/if}
  </p>
  <p class="mt-2 text-xs text-[var(--color-text-tertiary)]">
    {#if queued > 0}
      {queued} more {queued === 1 ? 'tab' : 'tabs'} waiting. Cancel keeps this tab and everything after
      it.
    {:else}
      If you don't save, the changes in this tab are lost.
    {/if}
  </p>

  {#snippet footer()}
    <div class="flex justify-end gap-2">
      <Button variant="ghost" size="sm" onclick={() => answerTabClosePrompt('cancel')}>
        Cancel
      </Button>
      <Button variant="secondary" size="sm" onclick={() => answerTabClosePrompt('discard')}>
        Don't Save
      </Button>
      <Button variant="primary" size="sm" onclick={() => answerTabClosePrompt('save')}>Save</Button>
    </div>
  {/snippet}
</Modal>
