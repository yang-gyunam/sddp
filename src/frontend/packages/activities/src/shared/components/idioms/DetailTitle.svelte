<!--
  DetailTitle Idiom
  Standardized title + code display for detail view headers.
  Ensures consistent typography (text-lg font-semibold) and code-after-title ordering.

  Usage:
    <DetailTitle title="My Feature" code="SPC-001" />
    <DetailTitle title="Payment API" code="SPC-002">
      <span class="text-xs text-[var(--color-text-tertiary)]">v2.1</span>
    </DetailTitle>
-->
<script lang="ts">
  import type { Snippet } from 'svelte';

  interface Props {
    title: string;
    /** Secondary identifier shown after title (e.g., code, abbreviation) */
    code?: string | null;
    /** Additional elements rendered after title+code (e.g., version, status badges) */
    children?: Snippet;
    class?: string;
  }

  let {
    title,
    code = null,
    children,
    class: className = '',
  }: Props = $props();
</script>

<div class="flex items-center gap-2 min-w-0 {className}">
  <h2 class="text-lg font-semibold text-[var(--color-text-primary)] truncate min-w-0">
    {title}
  </h2>
  {#if code}
    <span class="text-xs text-[var(--color-text-tertiary)] font-mono flex-shrink-0">
      {code}
    </span>
  {/if}
  {#if children}
    {@render children()}
  {/if}
</div>
