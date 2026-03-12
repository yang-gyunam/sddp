<!--
  ConversationHeader Idiom
  Reusable header for Channel/Forum views.
  Uses DetailHeader + DetailTitle for consistent styling across all domain headers.
-->
<script lang="ts">
  import { Icon } from '@sddp/ui';
  import type { Snippet } from 'svelte';
  import DetailHeader from '../../../shared/components/idioms/DetailHeader.svelte';
  import DetailTitle from '../../../shared/components/idioms/DetailTitle.svelte';

  interface Props {
    /** Conversation icon name */
    icon: string;
    /** Conversation title */
    title: string;
    /** Optional description / topic */
    description?: string;
    /** Optional creator name (shown as "by {name}") */
    byName?: string;
    /** Optional badge (e.g., count) */
    badge?: string | number;
    /** Optional action buttons (right side) */
    actions?: Snippet;
    /** Additional CSS classes */
    class?: string;
  }

  let {
    icon,
    title,
    description,
    byName,
    badge,
    actions: actionsSnippet,
    class: className = '',
  }: Props = $props();
</script>

<DetailHeader class={className}>
  {#snippet leading()}
    <Icon name={icon} size="md" class="text-[var(--color-text-tertiary)]" />
  {/snippet}
  <DetailTitle {title}>
    {#if description}
      <span class="text-sm font-normal text-[var(--color-text-tertiary)] truncate min-w-0">
        — {description}
      </span>
    {/if}
    {#if byName}
      <span class="text-xs text-[var(--color-text-tertiary)] truncate min-w-0">
        by {byName}
      </span>
    {/if}
    {#if badge !== undefined && badge !== null && badge !== ''}
      <span class="text-xs text-[var(--color-text-tertiary)] flex-shrink-0">
        ({badge})
      </span>
    {/if}
  </DetailTitle>
  {#snippet actions()}
    {#if actionsSnippet}
      {@render actionsSnippet()}
    {/if}
  {/snippet}
</DetailHeader>
