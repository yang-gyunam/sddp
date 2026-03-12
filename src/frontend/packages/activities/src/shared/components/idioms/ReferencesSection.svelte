<!--
  ReferencesSection Idiom
  Displays a list of linked entity references with semantic labels.
  Uses LinkedEntityCard internally.
-->
<script lang="ts">
  import { Icon, Button } from '@sddp/ui';
  import LinkedEntityCard from './LinkedEntityCard.svelte';
  import FieldAuthorBadge from './FieldAuthorBadge.svelte';
  import type { FieldAuthor } from '../../types';

  export interface ReferenceItem {
    icon: string;
    label: string;
    value: string;
    sublabel?: string;
    onClick?: () => void;
    onUnlink?: () => void;
    fieldAuthor?: FieldAuthor | null;
  }

  interface Props {
    items: ReferenceItem[];
    title?: string;
    class?: string;
  }

  let {
    items,
    title = 'References',
    class: className = '',
  }: Props = $props();

  const visibleItems = $derived(items.filter((item) => item.value));
</script>

{#if visibleItems.length > 0}
  <div class={className}>
    {#if title}
      <h3 class="text-sm font-medium text-[var(--color-text-secondary)] mb-2">{title}</h3>
    {/if}
    <div class="space-y-2">
      {#each visibleItems as item (item.label)}
        <div class="space-y-1">
          <div class="flex items-center gap-2">
            <span class="text-xs text-[var(--color-text-muted)]">{item.label}</span>
            {#if item.fieldAuthor}
              <FieldAuthorBadge author={item.fieldAuthor} />
            {/if}
          </div>
          <div class="flex items-center gap-2">
            <LinkedEntityCard
              icon={item.icon}
              label={item.value}
              sublabel={item.sublabel}
              onclick={item.onClick}
              class="flex-1"
            />
            {#if item.onUnlink}
              <Button variant="ghost" size="sm" onclick={item.onUnlink}>
                <Icon name="unlink" size="sm" />
              </Button>
            {/if}
          </div>
        </div>
      {/each}
    </div>
  </div>
{/if}
