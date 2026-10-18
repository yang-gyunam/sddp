<script lang="ts">
  import { Button } from '@sddp/ui';
  import { fromStore } from 'svelte/store';
  import { authStore } from '@sddp/shell/auth/stores';
  import { fetchWithAuth } from '../../../shared/api';

  let {
    messageId,
    references,
    projectId,
    tenantId = '',
    compactUnresolved = false,
  }: {
    messageId: string;
    references: string[];
    projectId: string;
    tenantId?: string;
    compactUnresolved?: boolean;
  } = $props();
  type Source = {
    sourceType: string;
    sourceId: string;
    label: string;
    content: string;
    isTruncated: boolean;
  };
  type Reference = { raw: string; type?: string; id?: string };
  const auth = fromStore(authStore);
  const tenant = $derived(tenantId || auth.current.user?.tenantId || '');
  const key = $derived(
    auth.current.isAuthenticated &&
      auth.current.sessionStatus === 'active' &&
      auth.current.user?.tenantId === tenant &&
      projectId
      ? JSON.stringify([auth.current.user.id, tenant, projectId, messageId, references])
      : ''
  );
  const items = $derived.by(() => {
    const result: Reference[] = [];
    for (const value of references) {
      const raw = value.trim();
      if (!raw) continue;
      const match =
        /^(spec|glossary|decision|artifact):([0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12})$/i.exec(
          raw
        );
      const normalized = match ? raw.toLowerCase() : raw;
      if (result.some((item) => item.raw === normalized)) continue;
      result.push(
        match
          ? { raw: normalized, type: match[1]!.toLowerCase(), id: match[2]!.toLowerCase() }
          : { raw }
      );
    }
    return result;
  });
  const unresolvedCount = $derived(items.filter((item) => !item.type).length);
  let owner = $state('');
  let selected = $state<Reference | null>(null);
  let source = $state<Source | null>(null);
  let loading = $state(false);
  let failed = $state(false);
  let generation = 0;
  const visible = $derived(!!key && owner === key);
  $effect(() => {
    void key;
    close();
    return () => {
      generation++;
    };
  });
  function close() {
    generation++;
    selected = null;
    source = null;
    loading = false;
    failed = false;
    owner = '';
  }
  async function load(reference: Reference) {
    if (!key || !reference.type || !reference.id) return;
    const epoch = ++generation;
    const requestedKey = key;
    owner = key;
    selected = reference;
    source = null;
    loading = true;
    failed = false;
    const current = () => epoch === generation && key === requestedKey;
    try {
      const value = await fetchWithAuth<Source>(
        `/ai/messages/${messageId}/references/${reference.type}/${reference.id}`,
        { tenantId: tenant, projectId }
      );
      if (!current()) return;
      if (
        !value ||
        value.sourceType !== reference.type ||
        value.sourceId.toLowerCase() !== reference.id ||
        typeof value.label !== 'string' ||
        value.label.length > 256 ||
        typeof value.content !== 'string' ||
        value.content.length > 16000 ||
        typeof value.isTruncated !== 'boolean'
      )
        throw new Error('Invalid reference response');
      source = value;
    } catch {
      if (current()) failed = true;
    } finally {
      if (current()) loading = false;
    }
  }
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
            disabled={!key}
            aria-expanded={visible && selected?.raw === reference.raw}
            onclick={() => (visible && selected?.raw === reference.raw ? close() : load(reference))}
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
      <div class="mt-2 rounded border border-[var(--color-border-secondary)] p-2 text-xs">
        {#if loading}
          <p role="status">Loading source…</p>
        {:else if failed}
          <p role="status">This source is unavailable or you no longer have access.</p>
          <Button variant="ghost" size="sm" onclick={() => selected && load(selected)}
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
        {/if}
        <Button variant="ghost" size="sm" onclick={close}>Close source</Button>
      </div>
    {/if}
  </section>
{/if}
