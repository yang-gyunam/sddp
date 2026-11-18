<script lang="ts">
  import { Button, Select, Textarea } from '@sddp/ui';
  import { SurfaceCard } from '@sddp/shell';
  import type {
    DiagramParserCapability2,
    DiagramIdentityPreview2,
    DiagramIdentityResolution2,
  } from '../../types2';
  import { refKey2 } from '../../model/model2';
  import { ref2 } from '../../model/scene2';
  let {
    adapters,
    adapterId,
    parserStatus,
    onadapter,
    text,
    busy,
    disabled,
    preview,
    ontext,
    onpreview,
    onresolve,
    onapply,
  }: {
    adapters: DiagramParserCapability2[];
    adapterId: string;
    parserStatus: string;
    onadapter: (id: string) => void;
    text: string;
    busy: boolean;
    disabled: boolean;
    preview: DiagramIdentityPreview2 | null;
    ontext: (text: string) => void;
    onpreview: () => void;
    onresolve: (resolution: DiagramIdentityResolution2) => void;
    onapply: () => void;
  } = $props();
  let decisions = $state<Record<string, string>>({});
  let fingerprint = '';
  $effect(() => {
    if (preview?.fingerprint !== fingerprint) {
      fingerprint = preview?.fingerprint ?? '';
      decisions = {};
    }
  });
  const selected = $derived(adapters.find((adapter) => adapter.id === adapterId));
  const unresolved = $derived(
    preview?.identityReport.filter((item) => item.reason === 'ambiguous') ?? []
  );
  function resolve() {
    if (preview)
      onresolve({
        fingerprint: preview.fingerprint,
        mappings: unresolved
          .filter((item) => decisions[item.candidateKey])
          .map((item) => ({
            candidateKey: item.candidateKey,
            target:
              decisions[item.candidateKey] === 'new' ? null : ref2(decisions[item.candidateKey]!),
          })),
        removed: preview.removed.filter((ref) => !Object.values(decisions).includes(refKey2(ref))),
      });
  }
</script>

<details>
  <summary class="text-sm cursor-pointer">Import diagram source</summary>
  <SurfaceCard class="space-y-2 mt-2">
    <Select
      label="Source parser"
      value={adapterId}
      options={adapters.map((a) => ({ value: a.id, label: `${a.id} ${a.version}` }))}
      placeholder="No parser available"
      disabled={disabled || busy}
      onchange={onadapter}
    />
    {#if parserStatus}<p role="status" class="text-xs">{parserStatus}</p>{/if}
    <Textarea
      label="Diagram source"
      value={text}
      disabled={disabled || busy}
      rows={7}
      oninput={(event) => ontext((event.target as HTMLTextAreaElement).value)}
    />
    <Button size="sm" disabled={disabled || busy || !text.trim() || !adapterId} onclick={onpreview}
      >Preview source</Button
    >
    {#if selected}
      <p class="text-xs text-[var(--color-text-secondary)]">
        Supported profiles: {selected.profiles
          .map((profile) => `${profile.id} v${profile.version}`)
          .join(', ')}.
        {selected.generatesEditedSource ? '' : 'Visual edits preserve the original source text.'}
      </p>
    {/if}
    {#if preview}
      {#each preview.diagnostics as diagnostic, i (i)}<p
          role={diagnostic.severity === 'error' ? 'alert' : 'status'}
          class="text-xs"
        >
          {diagnostic.code}: {diagnostic.message}{diagnostic.sourceRange
            ? ` (UTF-16 ${diagnostic.sourceRange.start}–${diagnostic.sourceRange.end})`
            : ''}
        </p>{/each}
      <details>
        <summary class="text-xs">Identity matches ({preview.identityReport.length})</summary>
        <ul class="text-xs">
          {#each preview.identityReport as item (item.candidateKey)}<li>
              {item.collection} · {item.label} · {item.reason} · {item.target?.id ?? 'unresolved'}
            </li>{/each}
        </ul>
      </details>
      {#each unresolved as item (item.candidateKey)}<Select
          label={`Identity for ${item.collection} · ${item.label}`}
          value={decisions[item.candidateKey] ?? ''}
          placeholder="Choose an existing identity or new"
          disabled={busy || disabled}
          options={[
            { value: 'new', label: 'Create a new identity' },
            ...item.candidates.map((ref) => ({
              value: refKey2(ref),
              label: `${ref.collection} · ${ref.id}`,
            })),
          ]}
          onchange={(value) => (decisions = { ...decisions, [item.candidateKey]: value })}
        />{/each}
      {#if preview.removed.length}<p class="text-xs">
          Existing identities removed unless mapped above:
        </p>
        <ul class="text-xs break-all">
          {#each preview.removed as ref (refKey2(ref))}<li>{ref.collection} · {ref.id}</li>{/each}
        </ul>{/if}
      {#if unresolved.length}<Button
          size="sm"
          disabled={busy || disabled || unresolved.some((item) => !decisions[item.candidateKey])}
          onclick={resolve}>Confirm identity and removals</Button
        >{/if}
      {#if preview.candidate}<p class="text-xs">
          {preview.changes?.changes.filter((c) => c.category === 'semantic').length ?? 0} semantic changes
          · {preview.changes?.changes.filter((c) => c.category === 'presentation').length ?? 0} presentation
          changes
        </p>
        <Button size="sm" disabled={busy || disabled} onclick={onapply}
          >Apply preview to draft</Button
        >{/if}
    {/if}
  </SurfaceCard>
</details>
