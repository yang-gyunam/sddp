<script lang="ts">
  import { onMount } from 'svelte';
  import { Button } from '@sddp/ui';
  import Modal from './Modal.svelte';
  import {
    confirmationStore,
    answerConfirmation,
    installConfirmationHost,
  } from '../../stores/confirmation.store';

  onMount(installConfirmationHost);
</script>

{#each $confirmationStore.current ? [$confirmationStore.current] : [] as prompt (prompt.id)}
  <Modal open title={prompt.title} size="md" onClose={() => answerConfirmation(prompt.id, null)}>
    <p class="whitespace-pre-line text-sm text-[var(--color-text-primary)]">{prompt.message}</p>
    {#snippet footer()}
      <div class="flex flex-wrap justify-end gap-2">
        <Button variant="ghost" size="sm" onclick={() => answerConfirmation(prompt.id, null)}
          >Cancel</Button
        >
        {#each prompt.choices as choice (choice.value)}
          <Button
            variant={choice.variant ?? 'primary'}
            size="sm"
            onclick={() => answerConfirmation(prompt.id, choice.value)}>{choice.label}</Button
          >
        {/each}
      </div>
    {/snippet}
  </Modal>
{/each}
