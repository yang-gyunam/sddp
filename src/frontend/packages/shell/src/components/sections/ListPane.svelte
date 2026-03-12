<!--
  ListPane Component
  Standardized left-pane layout with header, search, and scrollable list area
-->
<script lang="ts">
  import { Icon, Input } from '@sddp/ui';
  import PageBody from './PageBody.svelte';
  import PageHeader from './PageHeader.svelte';
  import PageShell from './PageShell.svelte';
  import type { Snippet } from 'svelte';

  interface Props {
    title: string;
    badge?: string | number;
    leading?: Snippet;
    actions?: Snippet;
    showSearch?: boolean;
    searchPlaceholder?: string;
    query?: string;
    class?: string;
    bodyClass?: string;
    children?: Snippet;
  }

  let {
    title,
    badge,
    leading,
    actions,
    showSearch = true,
    searchPlaceholder = 'Search',
    query = $bindable(''),
    class: className = '',
    bodyClass = '',
    children,
  }: Props = $props();
</script>

{#snippet badgeAddon()}
  <span class="text-xs text-[var(--color-text-tertiary)] flex-shrink-0">
    ({badge})
  </span>
{/snippet}

<PageShell class="h-full bg-[var(--color-bg-section)] {className}">
  <PageHeader
    title={title}
    {actions}
    {leading}
    titleAddon={badge !== undefined && badge !== null && badge !== '' ? badgeAddon : undefined}
  />

  <PageBody padded={false} scrollable={false} class="flex flex-col min-h-0 {bodyClass}">
    {#if showSearch}
      <div class="px-3 pt-3 pb-2">
        <div class="relative">
          <Icon
            name="search"
            size="xs"
            class="absolute left-2 top-1/2 -translate-y-1/2 text-[var(--color-text-muted)]"
          />
          <Input
            unstyled
            class="w-full pl-7 pr-2 py-1.5 text-xs rounded border border-[var(--color-border-secondary)] bg-[var(--color-bg-card)] text-[var(--color-text-primary)] placeholder:text-[var(--color-text-muted)] focus:outline-none focus:border-[var(--color-accent-primary)]"
            placeholder={searchPlaceholder}
            bind:value={query}
          />
        </div>
      </div>
    {/if}

    <div class="flex-1 min-h-0 overflow-y-auto">
      {#if children}
        {@render children()}
      {/if}
    </div>
  </PageBody>
</PageShell>
