<!--
  UserListItem Idiom
  Avatar + DisplayName + Email user row component.
  Wraps RichListItem with user-specific defaults (Avatar leading, AI badge trailing).
-->
<script lang="ts">
  import type { Snippet } from 'svelte';
  import RichListItem from './RichListItem.svelte';
  import Avatar from './Avatar.svelte';

  interface UserInfo {
    displayName: string;
    email?: string;
    isAi?: boolean;
    avatarUrl?: string;
  }

  interface Props {
    user: UserInfo;
    selected?: boolean;
    disabled?: boolean;
    density?: 'compact' | 'regular';
    separator?: 'border' | 'none';
    onclick?: (event: MouseEvent) => void;
    leading?: Snippet;
    trailing?: Snippet;
    class?: string;
  }

  let {
    user,
    selected = false,
    disabled = false,
    density = 'regular',
    separator = 'border',
    onclick,
    leading: leadingSnippet,
    trailing: trailingSnippet,
    class: className = '',
  }: Props = $props();
</script>

<RichListItem
  {selected}
  {disabled}
  {density}
  {separator}
  {onclick}
  title={user.displayName}
  description={user.email}
  class={className}
>
  {#snippet leading()}
    {#if leadingSnippet}
      {@render leadingSnippet()}
    {:else}
      <Avatar name={user.displayName} avatarUrl={user.avatarUrl} isAI={user.isAi} size="sm" />
    {/if}
  {/snippet}
  {#snippet trailing()}
    {#if trailingSnippet}
      {@render trailingSnippet()}
    {:else if user.isAi}
      <span class="text-xs text-[var(--color-accent-primary)]">AI</span>
    {/if}
  {/snippet}
</RichListItem>
