<!--
  FieldAuthorBadge Component
  Displays the last modifier information for an entity field
-->
<script lang="ts">
  import { Icon } from '@sddp/ui';
  import { formatRelativeTime, formatDateTime } from '@sddp/shell';
  import type { FieldAuthor } from '../../types';

  interface Props {
    author: FieldAuthor | null;
    class?: string;
  }

  let { author, class: className = '' }: Props = $props();
</script>

{#if author}
  <span
    class="ml-auto inline-flex items-center gap-1.5 text-xs text-[var(--color-text-muted)] {className}"
    title="Last modified by {author.userName} at {formatDateTime(author.timestamp)}"
  >
    <Icon name="user" size="xs" class="opacity-60" />
    <span class="font-medium">{author.userName}</span>
    <span class="opacity-60">·</span>
    <span class="opacity-80">{formatRelativeTime(author.timestamp)}</span>
  </span>
{/if}
