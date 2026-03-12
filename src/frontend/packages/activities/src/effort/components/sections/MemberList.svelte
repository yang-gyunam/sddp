<!-- Section: MemberList -->
<!--
  MemberList Component
  Displays list of project members with effort summaries
-->
<script lang="ts">
  import { Input, Spinner } from '@sddp/ui';
  import { EmptyState } from '@sddp/shell';
  import type { MemberEffortSummary } from '../../types';
  import MemberCard from '../idioms/MemberCard.svelte';

  interface Props {
    members: MemberEffortSummary[];
    selectedMemberId: string | null;
    onMemberSelect: (userId: string) => void;
    isLoading?: boolean;
    class?: string;
  }

  let {
    members,
    selectedMemberId,
    onMemberSelect,
    isLoading = false,
    class: className = '',
  }: Props = $props();

  let searchQuery = $state('');

  const filteredMembers = $derived.by(() => {
    if (!searchQuery.trim()) return members;

    const query = searchQuery.toLowerCase();
    return members.filter(
      (m) =>
        m.userName.toLowerCase().includes(query) ||
        m.userEmail.toLowerCase().includes(query) ||
        m.role.toLowerCase().includes(query)
    );
  });

  // Sort members by name only (keep stable order)
  const sortedMembers = $derived.by(() => {
    return [...filteredMembers].sort((a, b) => a.userName.localeCompare(b.userName));
  });
</script>

<div class="member-list {className}">
  <div class="member-list__toolbar">
    <div class="member-list__title-row">
      <h3 class="member-list__title">Team Members</h3>
      <span class="member-list__count">{members.length} members</span>
    </div>
    <div class="member-list__search">
      <Input
        type="search"
        placeholder="Search members..."
        bind:value={searchQuery}
        size="sm"
      />
    </div>
  </div>

  <div class="member-list__content">
    {#if isLoading}
      <div class="member-list__loading">
        <Spinner size="md" />
        <span>Loading members...</span>
      </div>
    {:else if sortedMembers.length === 0}
      <EmptyState
        icon="user"
        heading={searchQuery ? 'No matching members' : 'No members'}
        subtext={searchQuery
          ? 'Try a different search term'
          : 'No team members assigned to this project'}
      />
    {:else}
      <!-- Table Header -->
      <div class="member-list__header">
        <span class="member-list__col member-list__col--member">Member</span>
        <span class="member-list__col member-list__col--num">Allocated</span>
        <span class="member-list__col member-list__col--num">Spent</span>
        <span class="member-list__col member-list__col--num">Remaining</span>
        <span class="member-list__col member-list__col--progress"></span>
      </div>

      <!-- Table Body -->
      <div class="member-list__body">
        {#each sortedMembers as member (member.userId)}
          <MemberCard
            {member}
            isSelected={member.userId === selectedMemberId}
            onclick={() => onMemberSelect(member.userId)}
          />
        {/each}
      </div>
    {/if}
  </div>
</div>

<style>
  .member-list {
    display: flex;
    flex-direction: column;
    height: 100%;
    background-color: var(--color-bg-primary);
    border: 1px solid var(--color-border);
    border-radius: var(--radius-lg, 8px);
    overflow: hidden;
  }

  .member-list__toolbar {
    padding: 0.75rem 1rem;
    border-bottom: 1px solid var(--color-border);
    background-color: var(--color-bg-secondary);
  }

  .member-list__title-row {
    display: flex;
    align-items: center;
    justify-content: space-between;
    margin-bottom: 0.75rem;
  }

  .member-list__title {
    font-size: var(--text-sm);
    font-weight: 600;
    color: var(--color-text-primary);
    margin: 0;
  }

  .member-list__count {
    font-size: var(--text-xs);
    color: var(--color-text-tertiary);
  }

  .member-list__content {
    flex: 1;
    display: flex;
    flex-direction: column;
    overflow: hidden;
  }

  .member-list__loading {
    flex: 1;
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    gap: 0.75rem;
    padding: 2rem;
    color: var(--color-text-secondary);
  }

  :global(.member-list__spinner) {
    animation: spin 1s linear infinite;
  }

  @keyframes spin {
    from { transform: rotate(0deg); }
    to { transform: rotate(360deg); }
  }

  /* Table Header */
  .member-list__header {
    display: grid;
    grid-template-columns: 1fr 64px 64px 72px 60px;
    gap: 8px;
    padding: 8px 12px;
    border-bottom: 1px solid var(--color-border);
  }

  .member-list__col {
    font-size: var(--text-xs);
    font-weight: 500;
    color: var(--color-text-tertiary);
    text-transform: uppercase;
    letter-spacing: 0.025em;
    text-align: center;
  }

  /* Table Body */
  .member-list__body {
    flex: 1;
    overflow-y: auto;
  }
</style>
