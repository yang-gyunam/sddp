<!--
  WorkflowTransitionButton Component
  Handles spec workflow transitions with confirmations
-->
<script lang="ts">
  import { Button, Icon, Spinner } from '@sddp/ui';
  import { Modal } from '@sddp/shell';
  import type { SpecStatus } from '../../../specs/types';

  interface Props {
    currentStatus: SpecStatus;
    onTransition: (target: SpecStatus) => Promise<void>;
    signOffComplete?: boolean;
    /** Whether required fields are filled for review submission */
    canSubmitForReview?: boolean;
    onGenerate?: () => void;
    onNewVersion?: () => void;
    class?: string;
  }

  type TransitionAction = {
    key: string;
    label: string;
    icon: string;
    variant: 'primary' | 'secondary';
    target: SpecStatus;
    disabled?: boolean;
    confirm?: boolean;
  };

  type ButtonAction = {
    key: string;
    label: string;
    icon: string;
    variant: 'secondary';
    action: 'generate' | 'newVersion';
  };

  type WorkflowAction = TransitionAction | ButtonAction;

  let {
    currentStatus,
    onTransition,
    signOffComplete,
    canSubmitForReview = true,
    onGenerate,
    onNewVersion,
    class: className = '',
  }: Props = $props();

  let showLockConfirm = $state(false);
  let loadingAction = $state<string | null>(null);

  const canApprove = $derived(signOffComplete !== false);

  const actions = $derived.by<WorkflowAction[]>(() => {
    switch (currentStatus) {
      case 'Draft':
        return [
          { key: 'submit', label: 'Submit for Review', icon: 'send', variant: 'primary', target: 'InReview' as SpecStatus, disabled: !canSubmitForReview },
        ];
      case 'InReview':
        return [
          { key: 'approve', label: 'Approve', icon: 'check-circle', variant: 'primary', target: 'Approved' as SpecStatus, disabled: !canApprove },
        ];
      case 'Approved':
        return [
          { key: 'lock', label: 'Lock Spec', icon: 'lock', variant: 'primary', target: 'Locked' as SpecStatus, confirm: true },
        ];
      case 'Locked':
        return [
          { key: 'generate', label: 'Generate Artifacts', icon: 'cpu', variant: 'secondary', action: 'generate' as const },
          { key: 'newVersion', label: 'New Version', icon: 'git-branch', variant: 'secondary', action: 'newVersion' as const },
        ];
      default:
        return [];
    }
  });

  async function handleAction(action: WorkflowAction): Promise<void> {
    if ('target' in action) {
      if (action.confirm) {
        showLockConfirm = true;
        return;
      }
      loadingAction = action.key;
      try {
        await onTransition(action.target);
      } finally {
        loadingAction = null;
      }
      return;
    }

    if (action.action === 'generate') {
      onGenerate?.();
      return;
    }

    if (action.action === 'newVersion') {
      onNewVersion?.();
    }
  }

  async function confirmLock(): Promise<void> {
    showLockConfirm = false;
    loadingAction = 'lock';
    try {
      await onTransition('Locked');
    } finally {
      loadingAction = null;
    }
  }
</script>

<div class="flex items-center gap-2 {className}">
  {#each actions as action (action.key)}
    <Button
      variant={action.variant}
      size="sm"
      disabled={('disabled' in action && action.disabled) || loadingAction === action.key}
      onclick={() => handleAction(action)}
      title={('disabled' in action && action.disabled)
        ? action.key === 'submit' ? 'Fill in required fields (Title, Decision, Context, Acceptance Criteria)' : 'Waiting for all sign-offs'
        : action.label}
    >
      {#if loadingAction === action.key}
        <Spinner size="sm" />
      {:else if action.icon}
        <Icon name={action.icon} size="sm" />
      {/if}
      {action.label}
    </Button>
  {/each}
</div>

<Modal
  open={showLockConfirm}
  title="Lock Spec"
  onClose={() => (showLockConfirm = false)}
>
  <div class="p-4 space-y-4">
    <p class="text-sm text-[var(--color-text-secondary)]">
      Locking this spec will prevent further edits. Continue?
    </p>
    <div class="flex justify-end gap-2">
      <Button variant="ghost" onclick={() => (showLockConfirm = false)}>
        Cancel
      </Button>
      <Button variant="primary" onclick={confirmLock}>
        Lock Spec
      </Button>
    </div>
  </div>
</Modal>
