<!-- Section: TeamMemberList -->
<script lang="ts">
  import { Button } from '@sddp/ui';
  import { getAvatarHexColor } from '@sddp/shell';
  import type { TeamMemberActivity } from '../../types/dashboard.types';

  /**
   * Team Member List
   * Display team members with activity count
   */

  interface Props {
    members?: TeamMemberActivity[];
    maxDisplay?: number;
  }

  let { members = [], maxDisplay = 5 }: Props = $props();

  let displayedMembers = $derived(members.slice(0, maxDisplay));
  let hasMore = $derived(members.length > maxDisplay);
</script>

<div class="team-member-list">
  {#if members.length === 0}
    <p class="empty-state">No team members</p>
  {:else}
    <ul class="member-list">
      {#each displayedMembers as member (member.userId)}
        <li class="member-item">
          <div class="member-info">
            <div class="member-avatar" style="background-color: {getAvatarHexColor(member.userName)}">
              {member.userName.substring(0, 2).toUpperCase()}
            </div>
            <div class="member-details">
              <div class="member-name">{member.userName}</div>
              <div class="member-role">{member.role}</div>
            </div>
          </div>
          <div class="member-activity">
            {member.activityCount} activities
          </div>
        </li>
      {/each}
    </ul>
    
    {#if hasMore}
      <Button variant="ghost">View All Members ({members.length})</Button>
    {/if}
  {/if}
</div>

<style>
  .team-member-list {
    padding: 1rem;
    background: var(--bg-secondary);
    border: 1px solid var(--border-color);
    border-radius: 4px;
  }

  .empty-state {
    color: var(--text-tertiary);
    font-style: italic;
    text-align: center;
    padding: 2rem 0;
  }

  .member-list {
    list-style: none;
    padding: 0;
    margin: 0;
  }

  .member-item {
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding: 0.75rem 0;
    border-bottom: 1px solid var(--border-color);
  }

  .member-item:last-child {
    border-bottom: none;
  }

  .member-info {
    display: flex;
    align-items: center;
    gap: 0.75rem;
  }

  .member-avatar {
    width: 32px;
    height: 32px;
    border-radius: 50%;
    /* background color set dynamically via Tailwind class */
    color: white;
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: 0.75rem;
    font-weight: 600;
  }

  .member-details {
    display: flex;
    flex-direction: column;
    gap: 0.25rem;
  }

  .member-name {
    font-size: 0.875rem;
    font-weight: 500;
    color: var(--text-primary);
  }

  .member-role {
    font-size: 0.75rem;
    color: var(--text-tertiary);
  }

  .member-activity {
    font-size: 0.75rem;
    color: var(--text-secondary);
  }

</style>
