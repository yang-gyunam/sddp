<!-- Section: SpecDetailRejectModal — Specs > SpecDetailView -->
<script lang="ts">
  import { Button, Textarea } from '@sddp/ui';

  interface Props {
    show: boolean;
    rejectReason?: string;
    onCancel: () => void;
    onSubmit: () => void;
  }

  let {
    show,
    rejectReason = $bindable(''),
    onCancel,
    onSubmit,
  }: Props = $props();
</script>

{#if show}
  <div class="fixed inset-0 bg-[var(--color-overlay)]/50 flex items-center justify-center z-50">
    <div class="bg-[var(--color-surface-50)] rounded-lg shadow-xl p-6 w-full max-w-md">
      <h3 class="text-lg font-medium text-[var(--color-text-primary)] mb-4">Reject Spec</h3>
      <div class="mb-4">
        <Textarea
          label="Reason (optional)"
          bind:value={rejectReason}
          placeholder="Explain why this spec is being rejected..."
          rows={3}
        />
      </div>
      <div class="flex justify-end gap-2">
        <Button variant="ghost" onclick={onCancel}>
          Cancel
        </Button>
        <Button variant="danger" onclick={onSubmit}>
          Reject
        </Button>
      </div>
    </div>
  </div>
{/if}
