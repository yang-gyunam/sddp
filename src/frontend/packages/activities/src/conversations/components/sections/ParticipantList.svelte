<!-- Section: ParticipantList — Conversations (internal, used by ParticipantsPanel) -->
<script lang="ts">
  import { Icon } from '@sddp/ui';
  import type { ConversationMember } from '../../types';

  interface Props {
    participants: ConversationMember[];
    onlineUsers?: Set<string>;
    class?: string;
  }

  let { participants, onlineUsers = new Set(), class: className = '' }: Props = $props();

  function isOnline(userId: string): boolean {
    return onlineUsers.has(userId);
  }

  function getRoleColor(role: string): string {
    switch (role.toLowerCase()) {
      case 'productowner':
        return 'bg-[var(--color-bg-secondary)] text-[var(--color-accent-primary)] border border-[var(--color-border-secondary)]';
      case 'domainexpert':
        return 'bg-[var(--color-bg-secondary)] text-[var(--color-info-600)] border border-[var(--color-border-secondary)]';
      case 'developer':
        return 'bg-[var(--color-bg-secondary)] text-[var(--color-success-600)] border border-[var(--color-border-secondary)]';
      case 'admin':
        return 'bg-[var(--color-bg-secondary)] text-[var(--color-warning-700)] border border-[var(--color-border-secondary)]';
      default:
        return 'bg-[var(--color-bg-secondary)] text-[var(--color-text-secondary)] border border-[var(--color-border-secondary)]';
    }
  }

  // Separate AI and human participants
  const aiParticipants = $derived(participants.filter((p) => p.type === 'AI'));
  const humanParticipants = $derived(participants.filter((p) => p.type === 'Human'));
</script>

<div class="flex flex-col h-full {className}">
  <!-- Header -->
  <div class="px-4 py-3 border-b border-[var(--color-border-primary)]">
    <h3 class="text-sm font-medium text-[var(--color-text-primary)]">
      Participants ({participants.length})
    </h3>
  </div>

  <!-- Participant list -->
  <div class="flex-1 overflow-y-auto py-2">
    <!-- AI Agents -->
    {#if aiParticipants.length > 0}
      <div class="px-4 py-2">
        <h4 class="text-xs font-medium text-[var(--color-text-muted)] uppercase tracking-wider mb-2">
          AI Agents
        </h4>
        <ul class="space-y-2">
          {#each aiParticipants as participant (participant.id)}
            <li class="flex items-center gap-3">
              <div class="relative">
                <div
                  class="w-8 h-8 rounded-full flex items-center justify-center bg-[var(--color-accent-primary)] text-[var(--color-accent-primary)]"
                >
                  <Icon name="bot" size="sm" />
                </div>
                <span
                  class="absolute -bottom-0.5 -right-0.5 w-3 h-3 rounded-full border-2 border-[var(--color-surface-100)] {isOnline(
                    participant.user.id
                  )
                    ? 'bg-[var(--color-success-500)]'
                    : 'bg-[var(--color-neutral-400)]'}"
                ></span>
              </div>
              <div class="flex-1 min-w-0">
                <p class="text-sm font-medium text-[var(--color-text-primary)] truncate">
                  {participant.user.name}
                </p>
                <span
                  class="inline-block text-xs px-1.5 py-0.5 rounded {getRoleColor(participant.role)}"
                >
                  {participant.role}
                </span>
              </div>
            </li>
          {/each}
        </ul>
      </div>
    {/if}

    <!-- Human Participants -->
    {#if humanParticipants.length > 0}
      <div class="px-4 py-2">
        <h4 class="text-xs font-medium text-[var(--color-text-muted)] uppercase tracking-wider mb-2">
          Team Members
        </h4>
        <ul class="space-y-2">
          {#each humanParticipants as participant (participant.id)}
            <li class="flex items-center gap-3">
              <div class="relative">
                <div
                  class="w-8 h-8 rounded-full flex items-center justify-center bg-[var(--color-surface-300)] text-[var(--color-text-secondary)] text-xs font-medium"
                >
                  {(participant.user.name ?? '').substring(0, 2).toUpperCase()}
                </div>
                <span
                  class="absolute -bottom-0.5 -right-0.5 w-3 h-3 rounded-full border-2 border-[var(--color-surface-100)] {isOnline(
                    participant.user.id
                  )
                    ? 'bg-[var(--color-success-500)]'
                    : 'bg-[var(--color-neutral-400)]'}"
                ></span>
              </div>
              <div class="flex-1 min-w-0">
                <p class="text-sm font-medium text-[var(--color-text-primary)] truncate">
                  {participant.user.name}
                </p>
                <span
                  class="inline-block text-xs px-1.5 py-0.5 rounded {getRoleColor(participant.role)}"
                >
                  {participant.role}
                </span>
              </div>
            </li>
          {/each}
        </ul>
      </div>
    {/if}
  </div>
</div>
