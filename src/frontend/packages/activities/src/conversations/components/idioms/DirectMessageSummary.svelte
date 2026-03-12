<!--
  DirectMessageSummary Component
  Compact summary for sidebar panel (Favorites + Recent)
-->
<script lang="ts">
  import { Icon, Button } from '@sddp/ui';
  import { Avatar } from '@sddp/shell';
  import type { DirectMessage } from '../../types';

  // Accept items with either 'name' (DirectMessage) or 'participantName' (DirectMessageSummary)
  type DirectMessageItem = DirectMessage & { participantName?: string };

  interface Props {
    items: DirectMessageItem[];
    selectedId?: string | null;
    onSelect: (item: DirectMessageItem) => void;
    class?: string;
  }

  let { items, selectedId = null, onSelect, class: className = '' }: Props = $props();

  function getDisplayName(item: DirectMessageItem): string {
    return item.name || item.participantName || '??';
  }

  const favorites = $derived(items.slice(0, 5));
  const recents = $derived(items.slice(0, 6));
</script>

<div class="flex flex-col h-full {className}">
  <div class="px-4 pt-3">
    <h3 class="text-xs font-semibold text-[var(--color-text-muted)] uppercase tracking-[0.08em]">
      Favorites
    </h3>
    <div class="flex items-center gap-2 mt-2">
      {#each favorites as fav (fav.id)}
        <Button
          variant="unstyled"
          class="relative"
          onclick={() => onSelect(fav)}
          title={getDisplayName(fav)}
        >
          <Avatar name={getDisplayName(fav)} size="md" />
          <span
            class="absolute -bottom-0.5 -right-0.5 w-2.5 h-2.5 rounded-full border-2 border-[var(--color-surface-100)] {fav.isOnline
              ? 'bg-[var(--color-success-500)]'
              : 'bg-[var(--color-neutral-400)]'}"
          ></span>
        </Button>
      {/each}
      {#if favorites.length === 0}
        <span class="text-xs text-[var(--color-text-muted)]">No favorites</span>
      {/if}
    </div>
  </div>

  <div class="mt-4">
    <div class="px-4 pb-2 flex items-center justify-between">
      <h3 class="text-xs font-semibold text-[var(--color-text-muted)] uppercase tracking-[0.08em]">
        Recents
      </h3>
      <Icon name="message-circle" size="xs" class="text-[var(--color-text-muted)]" />
    </div>
    <div class="flex-1 overflow-y-auto">
      {#if recents.length === 0}
        <div class="px-4 py-6 text-center text-sm text-[var(--color-text-muted)]">
          No direct messages yet
        </div>
      {:else}
        <ul class="divide-y divide-[var(--color-border-secondary)]">
          {#each recents as item (item.id)}
            <li>
              <Button
                variant="unstyled"
                onclick={() => onSelect(item)}
                class="w-full text-left px-4 py-1.5 hover:bg-[var(--color-surface-200)] transition-colors flex items-center gap-3
                  {selectedId === item.id ? 'bg-[var(--color-surface-200)]' : ''}"
              >
                <div class="relative flex-shrink-0">
                  <Avatar name={getDisplayName(item)} size="md" />
                  <span
                    class="absolute -bottom-0.5 -right-0.5 w-2.5 h-2.5 rounded-full border-2 border-[var(--color-surface-100)] {item.isOnline
                      ? 'bg-[var(--color-success-500)]'
                      : 'bg-[var(--color-neutral-400)]'}"
                  ></span>
                </div>
                <div class="flex-1 min-w-0">
                  <div class="flex items-center justify-between gap-2">
                    <span class="text-sm font-medium text-[var(--color-text-primary)] truncate">
                      {getDisplayName(item)}
                    </span>
                    {#if item.unreadCount && item.unreadCount > 0}
                      <span
                        class="min-w-4 h-4 px-1 text-[0.625rem] rounded-full bg-[var(--color-accent-primary)] text-white flex items-center justify-center"
                      >
                        {item.unreadCount}
                      </span>
                    {/if}
                  </div>
                  <p class="text-xs text-[var(--color-text-muted)] truncate">
                    {item.lastMessage ?? ' '}
                  </p>
                </div>
              </Button>
            </li>
          {/each}
        </ul>
      {/if}
    </div>
  </div>
</div>
