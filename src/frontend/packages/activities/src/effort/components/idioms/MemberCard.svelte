<!--
  MemberCard Component
  Row-style display of a project member's effort summary (grid layout)
-->
<script lang="ts">
  import { Button, RadialProgress } from '@sddp/ui';
  import { formatPercent, Avatar } from '@sddp/shell';
  import type { MemberEffortSummary } from '../../types';
  import { formatHours, getUtilizationGrade } from '../../stores';

  interface Props {
    member: MemberEffortSummary;
    isSelected?: boolean;
    onclick?: () => void;
    class?: string;
  }

  let { member, isSelected = false, onclick, class: className = '' }: Props = $props();

  // Calculated values
  const remaining = $derived(member.totalAllocated - member.totalSpent);
  const utilizationVariant = $derived(getUtilizationGrade(member.utilizationRate).variant);
  const utilizationLabel = $derived(
    formatPercent(Math.round(member.utilizationRate) / 100, { maximumFractionDigits: 0 })
  );
</script>

<Button
  variant="unstyled"
  class="member-card {isSelected ? 'member-card--selected' : ''} {className}"
  {onclick}
>
  <!-- Member Info -->
  <div class="member-card__member">
    <div class="member-card__avatar">
      <Avatar name={member.userName} avatarUrl={member.avatarUrl} size="sm" />
    </div>
    <div class="member-card__member-info">
      <span class="member-card__name">{member.userName}</span>
      <div class="member-card__meta" aria-label="Produced items">
        <span class="member-card__meta-item">Req {member.requirementsCreated}</span>
        <span class="member-card__meta-item">Spec {member.specsCreated}</span>
        <span class="member-card__meta-item">Gloss {member.glossaryTermsCreated}</span>
        <span class="member-card__meta-item">Art {member.artifactsCreated}</span>
      </div>
    </div>
  </div>

  <!-- Allocated -->
  <span class="member-card__num">{formatHours(member.totalAllocated)}</span>

  <!-- Spent -->
  <span class="member-card__num">{formatHours(member.totalSpent)}</span>

  <!-- Remaining -->
  <span class="member-card__num member-card__num--remaining" class:member-card__num--negative={remaining < 0}>
    {formatHours(remaining)}
  </span>

  <!-- Progress -->
  <div class="member-card__progress" title={utilizationLabel}>
    <RadialProgress
      value={member.utilizationRate}
      size="xs"
      variant={utilizationVariant}
    />
  </div>
</Button>

<style>
  :global(.member-card) {
    display: grid;
    grid-template-columns: 1fr 68px 52px 72px 48px;
    gap: 4px;
    align-items: center;
    width: 100%;
    padding: 0.5rem 0.75rem;
    background-color: transparent;
    border: none;
    border-bottom: 1px solid var(--color-border);
    cursor: pointer;
    transition: background-color 150ms ease;
    text-align: left;
  }

  :global(.member-card:hover) {
    background-color: var(--color-bg-tertiary);
  }

  :global(.member-card--selected) {
    background-color: color-mix(in srgb, var(--color-accent-primary) 10%, transparent);
  }

  :global(.member-card--selected:hover) {
    background-color: color-mix(in srgb, var(--color-accent-primary) 15%, transparent);
  }

  /* Member Info */
  .member-card__member {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    min-width: 0;
  }

  .member-card__avatar {
    flex-shrink: 0;
    width: 28px;
    height: 28px;
    border-radius: 50%;
    overflow: hidden;
    background-color: var(--color-bg-tertiary);
  }

  :global(.member-card__avatar-text) {
    display: flex;
    align-items: center;
    justify-content: center;
    width: 100%;
    height: 100%;
    font-size: 11px;
    font-weight: 600;
    color: white;
    /* background color set dynamically via Tailwind class */
  }

  .member-card__name {
    font-size: var(--text-sm);
    font-weight: 500;
    color: var(--color-text-primary);
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
  }

  .member-card__member-info {
    display: flex;
    flex-direction: column;
    gap: 0.15rem;
    min-width: 0;
  }

  .member-card__meta {
    display: flex;
    flex-wrap: wrap;
    gap: 0.35rem 0.5rem;
    font-size: var(--text-2xs, 0.65rem);
    color: var(--color-text-tertiary);
  }

  .member-card__meta-item {
    font-variant-numeric: tabular-nums;
  }

  /* Numeric Values */
  .member-card__num {
    font-size: var(--text-xs);
    font-weight: 500;
    color: var(--color-text-secondary);
    text-align: center;
    font-variant-numeric: tabular-nums;
  }

  .member-card__num--negative {
    color: var(--color-danger-500);
  }

  /* Progress */
  .member-card__progress {
    display: flex;
    justify-content: center;
  }
</style>
