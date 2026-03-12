<!--
  ConversationItem Component
  Discord-style conversation item (# for Chat, list for Forum)
-->
<script lang="ts">
  import { Icon, Button } from '@sddp/ui';
  import type { ConversationSummary } from '../../types';

  interface Props {
    conversation: ConversationSummary;
    selected?: boolean;
    onClick?: (conversation: ConversationSummary) => void;
    onStarClick?: (conversation: ConversationSummary) => void;
    class?: string;
  }

  let {
    conversation,
    selected = false,
    onClick,
    onStarClick,
    class: className = '',
  }: Props = $props();

  const iconName = $derived(conversation.type === 'private' ? 'lock' : 'hash');
  const hasUnread = $derived(conversation.unreadCount > 0);

  function handleClick() {
    onClick?.(conversation);
  }

  function handleStarClick(e: MouseEvent) {
    e.stopPropagation();
    onStarClick?.(conversation);
  }
</script>

<div class="group relative border-b border-[var(--color-border-primary)] {className}">
  <Button
    variant="unstyled"
    onclick={handleClick}
    class="w-full text-left flex items-center gap-2 px-3 py-1.5 transition-colors
      {selected
        ? 'bg-[var(--color-accent-primary)]/10 text-[var(--color-text-primary)]'
        : 'text-[var(--color-text-secondary)] hover:bg-[var(--color-bg-tertiary)] hover:text-[var(--color-text-primary)]'}"
  >
    <span class="flex-shrink-0 w-4 h-4 flex items-center justify-center opacity-70">
      <Icon name={iconName} size="sm" />
    </span>

    <span
      class="flex-1 text-sm truncate pr-6
        {hasUnread ? 'font-medium text-[var(--color-text-primary)]' : ''}"
    >
      {conversation.name}
    </span>

    {#if hasUnread}
      <span
        class="flex-shrink-0 min-w-[18px] h-[18px] px-1 flex items-center justify-center text-xs font-medium
          rounded-full
          {conversation.hasUnreadMentions
            ? 'bg-[var(--color-error-500)] text-white'
            : 'bg-[var(--color-accent-primary)] text-white'}"
      >
        {conversation.unreadCount > 99 ? '99+' : conversation.unreadCount}
      </span>
    {/if}

    {#if conversation.muted}
      <Icon name="bell-off" size="xs" class="flex-shrink-0 text-[var(--color-text-tertiary)]" />
    {/if}
  </Button>

  <Button
    variant="unstyled"
    class="absolute right-2 top-1/2 -translate-y-1/2 z-10 p-0.5 rounded opacity-0 group-hover:opacity-100 transition-opacity
      {conversation.starred ? 'opacity-100' : ''}"
    onclick={handleStarClick}
    title={conversation.starred ? 'Unstar' : 'Star'}
  >
    <Icon
      name="star"
      size="xs"
      class={conversation.starred ? 'fill-current text-[var(--color-warning-500)]' : 'text-[var(--color-text-tertiary)]'}
    />
  </Button>
</div>
