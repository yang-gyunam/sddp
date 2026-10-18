<script lang="ts">
  import { Button } from '@sddp/ui';
  import type { ConversationAiRun } from '../../stores/conversation-ai-run.svelte';
  let { run }: { run: ConversationAiRun } = $props();
  const labels: Record<string, string> = {
    queued: 'AI is waiting to respond.',
    running: 'AI is preparing an answer.',
    cancelling: 'Cancelling AI response…',
    cancelled: 'AI response cancelled.',
    completed: 'AI response completed.',
    failed: 'AI could not complete the response.',
    interrupted: 'AI response was interrupted.',
  };
  const errors: Record<string, string> = {
    AI_QUEUE_TIMEOUT: 'The AI request expired while waiting. You can try again.',
    AI_RETRIEVAL_INCOMPLETE:
      'The search could not establish sufficient accessible sources. Narrow the question and try again.',
    AI_TIMEOUT: 'AI response timed out. You can try again.',
    AI_PROVIDER_UNAVAILABLE: 'The AI service is unavailable. Try again later.',
    AI_RESPONSE_INVALID: 'The AI answer could not be verified and was not published.',
    AI_CONTEXT_CHANGED: 'The conversation or its sources changed. Review them before retrying.',
    AI_INPUT_INVALID: 'The selected message is no longer available for an AI response.',
    AI_POLICY_REVOKED:
      'AI access changed during this request. Check AI availability before retrying.',
  };
  const failureMessage = $derived.by(() => {
    const code = run.state.run?.errorCode;
    return code && Object.hasOwn(errors, code) ? errors[code] : labels.failed;
  });
</script>

<div
  class="shrink-0 px-4 py-2 text-xs text-[var(--color-text-muted)]"
  role="status"
  aria-live="polite"
>
  <span>AI: {run.view.message}</span>
  {#if run.view.unavailable}
    <Button variant="ghost" size="sm" onclick={run.refreshAvailability}
      >Refresh AI availability</Button
    >
  {/if}
  {#if run.restoring}
    <p>Checking previous AI request…</p>
  {:else if run.submitting}
    <p>Requesting an AI response…</p>
  {:else if run.state.unavailable}
    <p>AI request status could not be checked.</p>
    <Button variant="ghost" size="sm" onclick={run.refresh}>Check AI request</Button>
  {:else if run.state.run}
    {#if run.state.run.status === 'completed'}
      {#if run.delivering}
        <p>Loading AI answer…</p>
      {:else if !run.deliveryError}
        <p>{labels.completed}</p>
      {/if}
    {:else if run.state.run.status === 'failed'}
      <p>{failureMessage}</p>
      {#if run.state.run.errorCode === 'AI_POLICY_REVOKED' && !run.view.unavailable}
        <Button variant="ghost" size="sm" onclick={run.refreshAvailability}
          >Refresh AI availability</Button
        >
      {/if}
    {:else}
      <p>{labels[run.state.run.status]}</p>
    {/if}
    {#if run.canCancel}
      <Button variant="ghost" size="sm" onclick={run.cancel}>Cancel AI response</Button>
    {/if}
    {#if run.canRetry}
      <Button variant="ghost" size="sm" onclick={run.retry}>Retry AI response</Button>
    {/if}
  {/if}
  {#if run.state.run?.status === 'running' && run.state.preview && !run.state.unavailable}
    <section
      aria-label="Temporary AI response"
      class="mt-2 max-h-48 overflow-y-auto rounded border border-[var(--color-border)] p-2"
    >
      <p class="font-medium">임시 응답 · 본인에게만 표시</p>
      <p>
        검증된 항목입니다. 최종 답변 전 변경되거나 폐기될 수 있으며, 이미 읽은 내용은 회수할 수
        없습니다.
      </p>
      <p class="mt-1 whitespace-pre-wrap break-words">{run.state.preview.answer}</p>
    </section>
  {/if}
  {#if run.deliveryError}
    <p>The completed answer could not be loaded.</p>
    <Button variant="ghost" size="sm" onclick={run.refresh}>Reload AI answer</Button>
    <Button variant="ghost" size="sm" onclick={run.dismissDeliveryError}>Dismiss AI status</Button>
  {/if}
</div>
