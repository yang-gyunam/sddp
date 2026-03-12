/**
 * Pure utility functions for Spec detail business logic.
 *
 * Extracted from SpecDetailView.svelte so both the component
 * and tests can share the same source of truth.
 */
import type { SpecDetail, SignOffSummary } from '../types';

/**
 * Whether a spec has all required fields for submitting to review.
 * Matches backend ValidateForReview: title, decision, context, acceptanceCriteria.
 */
export function canSubmitForReview(spec: SpecDetail): boolean {
  return !!(
    spec.title?.trim() &&
    spec.decision?.trim() &&
    spec.context?.trim() &&
    spec.acceptanceCriteria?.trim()
  );
}

/**
 * Whether to show the sign-off section (InReview only; even 0/0 reviewers).
 */
export function showSignOffSection(spec: SpecDetail): boolean {
  return spec.status === 'InReview';
}

/**
 * Whether the current user can submit a sign-off.
 * InReview + logged in + no existing non-Pending decision.
 */
export function canSignOff(
  spec: SpecDetail,
  currentUserId: string | null,
  signOffSummary: SignOffSummary | null,
): boolean {
  const currentUserSignOff =
    currentUserId && signOffSummary
      ? signOffSummary.signOffs.find((s) => s.stakeholder.id === currentUserId) ?? null
      : null;
  return (
    spec.status === 'InReview' &&
    !!currentUserId &&
    (!currentUserSignOff || currentUserSignOff.decision === 'Pending')
  );
}

/**
 * Whether all sign-offs are complete (no pending reviewers).
 */
export function signOffComplete(
  spec: SpecDetail,
  signOffSummary: SignOffSummary | null,
): boolean {
  return (
    (!!signOffSummary && signOffSummary.pendingCount === 0) ||
    spec.status === 'Approved' ||
    spec.status === 'Locked'
  );
}

export type StepStatus = 'complete' | 'current' | 'upcoming';
export type StepResult = { label: string; status: StepStatus };

/**
 * Compute workflow stepper state for a spec.
 */
export function workflowSteps(
  spec: SpecDetail,
  _signOffComplete: boolean,
  hasGeneratedArtifacts: boolean,
): StepResult[] {
  const steps = [
    { label: 'Conversation', complete: !!spec.bornFromConversationId, optional: true },
    { label: 'Requirement', complete: !!spec.requirementId, optional: true },
    { label: 'Spec Draft', complete: spec.status !== 'Draft', optional: false },
    { label: 'Review', complete: spec.status === 'Approved' || spec.status === 'Locked', optional: false },
    { label: 'Sign-off', complete: _signOffComplete, optional: false },
    { label: 'Lock', complete: spec.status === 'Locked', optional: false },
    { label: 'Generate', complete: hasGeneratedArtifacts, optional: true },
    { label: 'Change', complete: !!spec.supersedesSpecId, optional: true },
  ];

  const firstIncompleteRequired = steps.findIndex((step) => !step.complete && !step.optional);
  const currentIndex = firstIncompleteRequired === -1 ? steps.length - 1 : firstIncompleteRequired;

  return steps.map((step, index) => ({
    label: step.label,
    status:
      index < currentIndex
        ? (step.complete ? 'complete' as const : 'upcoming' as const)
      : index === currentIndex ? 'current' as const
      : 'upcoming' as const,
  }));
}
