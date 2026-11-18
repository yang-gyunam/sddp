<script lang="ts">
  import type { Snippet } from 'svelte';
  import { Button } from '@sddp/ui';
  import { SurfaceCard } from '@sddp/shell';
  import type {
    ConversationEvidenceReference as Reference,
    ConversationEvidenceSource as Source,
  } from '../../types/evidence.types';
  let {
    items,
    compactUnresolved = false,
    canLoad,
    visible,
    selected,
    source,
    loading,
    failed,
    onselect,
    onclose,
    actions,
  }: {
    items: Reference[];
    compactUnresolved?: boolean;
    canLoad: boolean;
    visible: boolean;
    selected: Reference | null;
    source: Source | null;
    loading: boolean;
    failed: boolean;
    onselect: (reference: Reference) => void;
    onclose: () => void;
    actions?: Snippet<[Reference]>;
  } = $props();
  const unresolvedCount = $derived(items.filter((item) => !item.type).length);
</script>

{#if items.length}
  <section
    class="mt-2 pt-2 border-t border-[var(--color-border-secondary)]"
    aria-label="AI evidence"
  >
    <span class="text-xs text-[var(--color-text-muted)]"
      >{compactUnresolved ? 'Original sources:' : 'Sources:'}</span
    >
    <div class="flex flex-wrap gap-1 mt-1">
      {#each items as reference (reference.raw)}
        {#if reference.type}
          <Button
            variant="ghost"
            size="sm"
            disabled={!canLoad}
            aria-expanded={visible && selected?.raw === reference.raw}
            onclick={() =>
              visible && selected?.raw === reference.raw ? onclose() : onselect(reference)}
          >
            {reference.type}
            {reference.id?.slice(0, 8)}…
          </Button>
        {:else if !compactUnresolved}
          <span class="text-xs break-all" title="Unsupported source reference">{reference.raw}</span
          >
        {/if}
      {/each}
    </div>
    {#if compactUnresolved && unresolvedCount}
      <p class="mt-1 text-xs text-[var(--color-text-muted)]">
        {unresolvedCount} references without previews. Links to the report and its sources are unavailable.
      </p>
    {/if}
    {#if visible && selected}
      <SurfaceCard padding="sm" class="mt-2 text-xs">
        {#if loading}
          <p role="status">Loading source…</p>
        {:else if failed}
          <p role="status">This source is unavailable or you no longer have access.</p>
          <Button variant="ghost" size="sm" onclick={() => selected && onselect(selected)}
            >Retry source</Button
          >
        {:else if source}
          <p class="font-semibold break-all">{source.label}</p>
          <p class="text-[var(--color-text-muted)]">
            Current source; it may have changed since this answer.
          </p>
          <pre
            class="mt-2 whitespace-pre-wrap break-words font-inherit max-h-80 overflow-auto">{source.content}</pre>
          {#if source.isTruncated}<p>Source preview truncated.</p>{/if}
          {#if actions}{@render actions(selected)}{/if}
        {/if}
        <Button variant="ghost" size="sm" onclick={onclose}>Close source</Button>
      </SurfaceCard>
    {/if}
  </section>
{/if}
