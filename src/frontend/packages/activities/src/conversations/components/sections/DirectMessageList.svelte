<!-- Section: DirectMessageList — Projects > Conversations -->
<script lang="ts">
  import { Button } from '@sddp/ui';
  import {
    formatDateByPreference,
    Avatar,
  } from '@sddp/shell';

  interface DirectMessage {
    id: string;
    name: string;
    lastMessage?: string;
    lastActiveAt?: string;
    unreadCount?: number;
    isOnline?: boolean;
  }

  interface Props {
    items: DirectMessage[];
    selectedId?: string | null;
    onSelect: (item: DirectMessage) => void;
    class?: string;
    query?: string;
  }

  let {
    items,
    selectedId = null,
    onSelect,
    class: className = '',
    query = '',
  }: Props = $props();

  const filteredItems = $derived(
    query.trim()
      ? items.filter((item) =>
          item.name.toLowerCase().includes(query.trim().toLowerCase())
        )
      : items
  );

  function formatTime(dateStr?: string): string {
    if (!dateStr) return '';
    return formatDateByPreference(dateStr);
  }
</script>

<div class="{className}">
  {#if filteredItems.length === 0}
    <div class="flex flex-col items-center justify-center h-20 text-center px-4">
      <p class="text-sm text-[var(--color-text-muted)]">No direct messages</p>
    </div>
  {:else}
    {#each filteredItems as item (item.id)}
      <Button
        variant="unstyled"
        class="w-full flex items-center gap-2 px-2 py-1.5 rounded cursor-pointer
          transition-colors duration-150
          {selectedId === item.id
            ? 'bg-[var(--color-accent-primary)]/10 border border-[var(--color-accent-primary)]/30'
            : 'hover:bg-[var(--color-bg-tertiary)] border border-transparent'}"
        onclick={() => onSelect(item)}
      >
        <!-- Avatar (compact) -->
        <div class="relative flex-shrink-0">
          <Avatar name={item.name} size="xs" />
          <span
            class="absolute -bottom-0.5 -right-0.5 w-2 h-2 rounded-full border border-[var(--color-bg-primary)] {item.isOnline
              ? 'bg-[var(--color-success-500)]'
              : 'bg-[var(--color-neutral-400)]'}"
          ></span>
        </div>
        <span class="flex-1 text-left text-sm truncate text-[var(--color-text-primary)]">
          {item.name}
        </span>
        {#if item.lastActiveAt}
          <span class="text-[0.625rem] text-[var(--color-text-muted)] flex-shrink-0">
            {formatTime(item.lastActiveAt)}
          </span>
        {/if}
        {#if (item.unreadCount ?? 0) > 0}
          <span
            class="min-w-[18px] h-[18px] px-1 flex items-center justify-center text-xs font-medium
              rounded-full bg-[var(--color-accent-primary)] text-white"
          >
            {(item.unreadCount ?? 0) > 99 ? '99+' : item.unreadCount}
          </span>
        {/if}
      </Button>
    {/each}
  {/if}
</div>
