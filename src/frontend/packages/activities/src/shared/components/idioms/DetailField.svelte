<!--
  DetailField Idiom
  Standardized detail view field: label + optional FieldAuthorBadge + content.
  Used across all domain detail views (Specs, Requirements, Glossary, Artifacts).
-->
<script lang="ts">
  import type { Snippet } from 'svelte';
  import FieldAuthorBadge from './FieldAuthorBadge.svelte';
  import type { FieldAuthor } from '../../types';

  interface Props {
    label: string;
    author?: FieldAuthor | null;
    /** Show red asterisk for required fields (e.g., Draft status) */
    required?: boolean;
    children: Snippet;
    class?: string;
  }

  let {
    label,
    author = null,
    required = false,
    children,
    class: className = '',
  }: Props = $props();
</script>

<div class={className}>
  <div class="flex items-center gap-2 mb-1">
    <h4 class="text-xs text-[var(--color-text-muted)]">
      {label}
      {#if required}<span class="text-red-500">*</span>{/if}
    </h4>
    {#if author}
      <FieldAuthorBadge {author} />
    {/if}
  </div>
  {@render children()}
</div>
