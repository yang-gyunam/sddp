<!--
  RichListItem Component
  3-row rich list item with flexible slots
  Pattern from app-shell reference (SessionLogPanel, AgentsPanel)
-->
<script lang="ts">
  import type { Snippet } from 'svelte';
  import { Button, Icon } from '@sddp/ui';

  interface Props {
    // Core
    selected?: boolean;
    disabled?: boolean;
    onclick?: (event: MouseEvent) => void;
    class?: string;

    // Layout
    density?: 'compact' | 'regular' | 'relaxed';
    separator?: 'border' | 'none';

    // Row 1: Title
    title: string;
    titleLink?: boolean;
    titleClass?: string;
    meta?: string;

    // Row 2: Description
    description?: string;
    descriptionLines?: 1 | 2;

    // Leading icon
    leadingIcon?: string;
    leadingIconClass?: string;

    // Snippets (for flexibility)
    leading?: Snippet;
    trailing?: Snippet;
    badges?: Snippet;
    footerMeta?: Snippet;
  }

  let {
    selected = false,
    disabled = false,
    onclick,
    class: className = '',
    density = 'regular',
    separator = 'border',
    title,
    titleLink = false,
    titleClass = '',
    meta,
    description,
    descriptionLines = 1,
    leadingIcon,
    leadingIconClass = '',
    leading,
    trailing,
    badges,
    footerMeta,
  }: Props = $props();

  // Density-based padding
  const paddingClass = $derived({
    compact: 'px-3 py-2',
    regular: 'px-3 py-3',
    relaxed: 'px-4 py-4',
  }[density]);

  // Description line clamp
  const descLineClamp = $derived(
    descriptionLines === 1 ? 'line-clamp-1' : 'line-clamp-2'
  );

  // Has footer content
  const hasFooter = $derived(!!badges || !!footerMeta);

  function handleClick(e: MouseEvent) {
    if (!disabled) {
      onclick?.(e);
    }
  }

</script>

<Button
  variant="unstyled"
  class="rich-list-item w-full text-left transition-colors cursor-pointer
    {paddingClass}
    {separator === 'border' ? 'border-b border-[var(--color-border-primary)]' : ''}
    {selected
      ? 'bg-[var(--color-accent-primary)]/10'
      : 'hover:bg-[var(--color-bg-tertiary)]'}
    {disabled ? 'opacity-50 cursor-not-allowed' : ''}
    {className}"
  tabindex={disabled ? -1 : undefined}
  aria-label={title}
  onclick={handleClick}
  {disabled}
>
  <div class="flex items-start gap-2">
    <!-- Leading -->
    {#if leading}
      <div class="flex-shrink-0 mt-0.5">
        {@render leading()}
      </div>
    {:else if leadingIcon}
      <Icon name={leadingIcon} size="sm" class="flex-shrink-0 mt-0.5 {leadingIconClass}" />
    {/if}

    <!-- Content -->
    <div class="flex-1 min-w-0">
      <!-- Row 1: Title + Meta -->
      <div class="flex items-center justify-between gap-2">
        <div class="flex items-center gap-2 min-w-0 flex-1">
          <span
            class="text-sm font-medium truncate
              {titleLink
                ? 'text-[var(--color-info-500)]'
                : 'text-[var(--color-text-primary)]'}
              {titleClass}"
          >
            {title}
          </span>
          {#if trailing}
            {@render trailing()}
          {/if}
        </div>
        {#if meta}
          <span class="flex-shrink-0 text-xs text-[var(--color-text-tertiary)]">
            {meta}
          </span>
        {/if}
      </div>

      <!-- Row 2: Description -->
      {#if description}
        <p class="mt-1 text-[0.8125rem] text-[var(--color-text-secondary)] {descLineClamp}">
          {description}
        </p>
      {/if}

      <!-- Row 3: Footer (Badges + Secondary Meta) -->
      {#if hasFooter}
        <div class="flex items-center justify-between gap-2 mt-1.5">
          <div class="flex items-center gap-2">
            {#if badges}
              {@render badges()}
            {/if}
          </div>
          {#if footerMeta}
            <div class="flex items-center gap-2 text-xs text-[var(--color-text-tertiary)]">
              {@render footerMeta()}
            </div>
          {/if}
        </div>
      {/if}
    </div>
  </div>
</Button>
