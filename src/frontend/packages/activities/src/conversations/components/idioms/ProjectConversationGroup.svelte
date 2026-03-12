<!--
  ProjectConversationGroup Component
  Collapsible project section with conversations
  Uses CollapsibleGroup from shell for consistent behavior and animation
-->
<script lang="ts">
  import { Icon, Button } from '@sddp/ui';
  import { CollapsibleGroup } from '@sddp/shell';
  import type { ProjectConversationGroup as ProjectConversationGroupType, ConversationSummary } from '../../types';
  import ConversationItem from './ConversationItem.svelte';

  interface Props {
    group: ProjectConversationGroupType;
    selectedConversationId?: string | null;
    onToggle?: (projectId: string, expanded: boolean) => void;
    onConversationClick?: (conversation: ConversationSummary) => void;
    onConversationStarClick?: (conversation: ConversationSummary) => void;
    class?: string;
  }

  let {
    group,
    selectedConversationId = null,
    onToggle,
    onConversationClick,
    onConversationStarClick,
    class: className = '',
  }: Props = $props();

  const hasUnread = $derived(group.unreadCount > 0);
  const visibleConversations = $derived(group.conversations.slice(0, 5));
  const hiddenCount = $derived(group.conversations.length - 5);
  const hasMoreConversations = $derived(hiddenCount > 0);

  function handleToggle() {
    onToggle?.(group.projectId, !group.expanded);
  }
</script>

<CollapsibleGroup
  title={group.projectCode}
  icon="folder"
  badge={hasUnread ? group.unreadCount : undefined}
  badgeClass="text-white bg-[var(--color-accent-primary)]"
  expanded={group.expanded}
  onToggle={handleToggle}
  class={className}
>
  <div class="py-1" role="group" aria-label="{group.projectCode} conversations">
    {#each visibleConversations as conversation (conversation.id)}
      <ConversationItem
        {conversation}
        selected={selectedConversationId === conversation.id}
        onClick={onConversationClick}
        onStarClick={onConversationStarClick}
      />
    {/each}

    <!-- Show More Button -->
    {#if hasMoreConversations}
      <Button
        variant="unstyled"
        class="w-full px-3 py-1 text-xs text-left text-[var(--color-text-tertiary)]
          hover:text-[var(--color-text-secondary)] hover:bg-[var(--color-bg-tertiary)]
          transition-colors"
      >
        <span class="flex items-center gap-1">
          <Icon name="plus" size="xs" />
          {hiddenCount} more conversation{hiddenCount > 1 ? 's' : ''}...
        </span>
      </Button>
    {/if}

    {#if group.conversations.length === 0}
      <div class="px-4 py-3 text-sm text-[var(--color-text-tertiary)] italic">
        No conversations
      </div>
    {/if}
  </div>
</CollapsibleGroup>
