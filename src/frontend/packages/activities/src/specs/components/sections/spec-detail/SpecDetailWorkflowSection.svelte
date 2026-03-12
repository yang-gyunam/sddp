<!-- Section: SpecDetailWorkflowSection — Specs > SpecDetailView -->
<script lang="ts">
  import { Button, Icon, Spinner, Textarea } from '@sddp/ui';
  import { CollapsibleGroup } from '@sddp/shell';
  import DetailField from '../../../../shared/components/idioms/DetailField.svelte';
  import WorkflowStepper from '../../../../workflow/components/idioms/WorkflowStepper.svelte';
  import WorkflowTransitionButton from '../../../../workflow/components/idioms/WorkflowTransitionButton.svelte';
  import type {
    SignOffSummary,
    SignOffDecision,
    SpecStatus,
  } from '../../../types';
  import { SIGN_OFF_DECISION_STYLES } from '../../../types';
  import type { StepResult } from '../../../utils/spec-detail.utils';

  type SpecAction = {
    action: string;
    label: string;
    variant: 'primary' | 'secondary' | 'danger';
  };

  interface Props {
    currentStatus: SpecStatus;
    workflowExpanded: boolean;
    workflowSteps: StepResult[];
    isReadOnly: boolean;
    onToggleWorkflow: () => void;
    onTransition?: (target: SpecStatus) => Promise<void>;
    onGenerate?: () => void | Promise<void>;
    signOffComplete: boolean;
    canSubmitForReview: boolean;
    onNewVersion?: () => void;
    availableActions: SpecAction[];
    onAction: (action: string) => void;
    showSignOffSection: boolean;
    signOffSummary: SignOffSummary | null;
    currentUserId: string | null;
    canSignOff: boolean;
    signOffDecision?: SignOffDecision | null;
    signOffConditions?: string;
    signOffComments?: string;
    signOffLoading: boolean;
    onSignOffSubmit: () => void | Promise<void>;
  }

  let {
    currentStatus,
    workflowExpanded,
    workflowSteps,
    isReadOnly,
    onToggleWorkflow,
    onTransition,
    onGenerate,
    signOffComplete,
    canSubmitForReview,
    onNewVersion,
    availableActions,
    onAction,
    showSignOffSection,
    signOffSummary,
    currentUserId,
    canSignOff,
    signOffDecision = $bindable(null as SignOffDecision | null),
    signOffConditions = $bindable(''),
    signOffComments = $bindable(''),
    signOffLoading,
    onSignOffSubmit,
  }: Props = $props();
</script>

<!-- Workflow & Review -->
<CollapsibleGroup
  title="Workflow & Review"
  variant="plain"
  expanded={workflowExpanded}
  onToggle={onToggleWorkflow}
>
  <div class="flex flex-col gap-4 pl-5 pt-2 pb-3">
    <!-- Workflow Stepper -->
    <WorkflowStepper steps={workflowSteps} />

    <!-- Actions -->
    {#if !isReadOnly}
      <div class="flex items-center justify-end gap-2">
        {#if onTransition}
          <WorkflowTransitionButton
            currentStatus={currentStatus}
            {signOffComplete}
            {canSubmitForReview}
            {onGenerate}
            {onNewVersion}
            {onTransition}
          />
        {:else}
          {#each availableActions as action (action.action)}
            <Button
              variant={action.variant === 'primary' ? 'primary' : action.variant === 'danger' ? 'danger' : 'secondary'}
              size="sm"
              onclick={() => onAction(action.action)}
            >
              {action.label}
            </Button>
          {/each}
        {/if}
      </div>
    {/if}

    <!-- Sign-off Section (inline, replaces modal) -->
    {#if showSignOffSection}
      <div class="rounded-lg border border-[var(--color-border-secondary)] bg-[var(--color-surface-50)] p-4 space-y-3">
        <DetailField label="Review & Sign-offs">
          {#if signOffSummary && signOffSummary.totalCount > 0}
            <div class="text-xs text-[var(--color-text-muted)] mb-2">
              {signOffSummary.approvedCount + signOffSummary.conditionalCount}/{signOffSummary.totalCount} completed
            </div>
          {/if}
        </DetailField>

        <!-- Reviewer list -->
        <div class="space-y-2">
          {#each (signOffSummary?.signOffs ?? []) as signOff (signOff.id)}
            {@const style = SIGN_OFF_DECISION_STYLES[signOff.decision]}
            {@const isCurrentUser = signOff.stakeholder.id === currentUserId}
            <div class="flex items-center gap-3 px-3 py-2 rounded-lg border bg-[var(--color-surface-100)] {isCurrentUser
              ? 'border-[var(--color-accent-primary)]/30'
              : 'border-[var(--color-border-secondary)]'}">
              <div class="flex-1 min-w-0 flex items-center gap-2">
                <span class="text-sm font-medium text-[var(--color-text-primary)] truncate">
                  {signOff.stakeholder.name}
                </span>
                {#if isCurrentUser}
                  <span class="text-[0.625rem] px-1 py-0.5 rounded bg-[var(--color-accent-primary)] text-white leading-none">You</span>
                {/if}
                <span class="text-xs text-[var(--color-text-muted)]">{signOff.role}</span>
              </div>
              <div class="flex items-center gap-1 px-2 py-0.5 rounded-full {style.bgColor} {style.textColor}">
                <Icon name={style.icon} size="xs" />
                <span class="text-xs font-medium">{style.label}</span>
              </div>
            </div>
            {#if signOff.conditions}
              <div class="ml-3 px-3 py-1.5 text-xs rounded bg-[var(--color-warning-100)] dark:bg-[var(--color-warning-900)]/20 text-[var(--color-text-secondary)]">
                <span class="font-medium">Conditions:</span> {signOff.conditions}
              </div>
            {/if}
            {#if signOff.comments}
              <div class="ml-3 px-3 py-1.5 text-xs text-[var(--color-text-secondary)] italic">
                "{signOff.comments}"
              </div>
            {/if}
          {/each}
        </div>

        <!-- Current user sign-off form -->
        {#if canSignOff}
          <div class="pt-3 border-t border-[var(--color-border-secondary)] space-y-3">
            <DetailField label="Your Decision">
              <div class="flex flex-wrap gap-2">
                {#each (['Approved', 'Conditional', 'Rejected'] as const) as d (d)}
                  {@const ds = SIGN_OFF_DECISION_STYLES[d]}
                  <Button
                    variant="unstyled"
                    class="flex items-center gap-1.5 px-3 py-1.5 text-sm rounded-lg border-2 transition-colors
                      {signOffDecision === d
                        ? `${ds.bgColor} ${ds.textColor} ${ds.borderColor}`
                        : 'border-[var(--color-border-primary)] text-[var(--color-text-secondary)] hover:bg-[var(--color-bg-tertiary)]'}"
                    onclick={() => {
                      signOffDecision = d;
                      if (d !== 'Conditional') {
                        signOffConditions = '';
                      }
                    }}
                  >
                    <Icon name={ds.icon} size="sm" />
                    {ds.label}
                  </Button>
                {/each}
              </div>

              {#if signOffDecision === 'Conditional'}
                <Textarea
                  label="Conditions"
                  required
                  bind:value={signOffConditions}
                  placeholder="Specify the conditions that must be met..."
                  rows={2}
                />
              {/if}

              {#if signOffDecision}
                <Textarea
                  label="Comments"
                  required={signOffDecision === 'Rejected'}
                  bind:value={signOffComments}
                  placeholder={signOffDecision === 'Rejected' ? 'Please explain the rejection...' : 'Additional comments...'}
                  rows={2}
                />
                <div class="flex justify-end">
                  <Button
                    variant="primary"
                    size="sm"
                    onclick={onSignOffSubmit}
                    disabled={
                      signOffLoading ||
                      (signOffDecision === 'Conditional' && !signOffConditions.trim()) ||
                      (signOffDecision === 'Rejected' && !signOffComments.trim())
                    }
                  >
                    {#if signOffLoading}
                      <Spinner size="sm" />
                    {/if}
                    Submit Sign-off
                  </Button>
                </div>
              {/if}
            </DetailField>
          </div>
        {/if}
      </div>
    {/if}
  </div>
</CollapsibleGroup>
