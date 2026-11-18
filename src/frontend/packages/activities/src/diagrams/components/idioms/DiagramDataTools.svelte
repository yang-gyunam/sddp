<script lang="ts">
  import { Button, Input, Select, Textarea, FileInput } from '@sddp/ui';
  import { SurfaceCard } from '@sddp/shell';
  import type { DiagramOrigin } from '../../types';
  import type { EntitySearchResult } from '../../../relationship/services/RelationshipService';
  import type { EntityType } from '../../../relationship/types';
  let {
    kind,
    hasDiagram,
    busy,
    canChange,
    yaml,
    pending,
    origins,
    labels,
    results,
    searching,
    onkind,
    onyaml,
    onimport,
    onfile,
    onexport,
    onsearch,
    onadd,
    onremove,
    onproject,
  }: {
    kind: 'authored' | 'derived';
    hasDiagram: boolean;
    busy: boolean;
    canChange: boolean;
    yaml: string;
    pending: boolean;
    origins: DiagramOrigin[];
    labels: Record<string, string>;
    results: EntitySearchResult[];
    searching: boolean;
    onkind: (kind: 'authored' | 'derived') => void;
    onyaml: (text: string) => void;
    onimport: () => void;
    onfile: (file: File) => void;
    onexport: () => void;
    onsearch: (query: string, type: EntityType) => void;
    onadd: (result: EntitySearchResult) => void;
    onremove: (origin: DiagramOrigin) => void;
    onproject: () => void;
  } = $props();
  let query = $state('');
  let type = $state<EntityType>('Spec');
  const types = ['Spec', 'Requirement', 'Conversation', 'GlossaryTerm', 'Artifact', 'Task'];
</script>

{#if !hasDiagram && canChange}
  <Select
    label="Diagram kind"
    value={kind}
    options={[
      { value: 'authored', label: 'Authored diagram' },
      { value: 'derived', label: 'Automatic diagram' },
    ]}
    disabled={busy}
    onchange={(value) => onkind(value as 'authored' | 'derived')}
  />
{/if}
{#if kind === 'derived'}
  <section aria-label="Automatic diagram">
    <SurfaceCard class="space-y-2">
      {#if pending}<p role="status" class="text-sm">
          Selection changed. Preview before saving this state.
        </p>{/if}
      <p class="text-sm">
        Select existing entities, then preview their current relationships. Refresh does not save a
        revision.
      </p>
      {#if canChange}
        <div
          class="grid grid-cols-1 xl:grid-cols-[minmax(0,1fr)_minmax(0,1fr)_auto] items-end gap-2"
        >
          <Select
            label="Origin type"
            bind:value={type}
            options={types.map((value) => ({ value, label: value }))}
            disabled={busy}
          />
          <Input label="Find origin" bind:value={query} disabled={busy} />
          <Button
            size="sm"
            class="whitespace-nowrap"
            disabled={busy || searching || !query.trim()}
            onclick={() => onsearch(query, type)}>Search origins</Button
          >
        </div>
        <ul aria-label="Origin search results" class="space-y-1 text-sm">
          {#each results as result (`${result.entityType}:${result.id}`)}
            <li>
              <Button variant="ghost" size="sm" disabled={busy} onclick={() => onadd(result)}
                >Add {result.entityType}: {result.label}</Button
              >
            </li>
          {/each}
        </ul>
      {/if}
      <ul aria-label="Selected origins" class="space-y-1 text-sm">
        {#each origins as origin (`${origin.type}:${origin.id}`)}
          <li class="flex items-center gap-2">
            <span
              >{[
                'Spec',
                'Requirement',
                'Topic',
                'GlossaryTerm',
                'Conversation',
                'Artifact',
                'Task',
              ][origin.type]} · {labels[`${origin.type}:${origin.id}`] ?? origin.id}</span
            >
            {#if canChange}<Button
                variant="ghost"
                size="sm"
                disabled={busy}
                aria-label={`Remove origin ${origin.id}`}
                onclick={() => onremove(origin)}>Remove</Button
              >{/if}
          </li>
        {/each}
      </ul>
      {#if canChange}<Button size="sm" disabled={busy || origins.length === 0} onclick={onproject}
          >Preview / refresh diagram</Button
        >{/if}
    </SurfaceCard>
  </section>
{/if}
<details>
  <summary class="text-sm cursor-pointer">YAML import / export</summary>
  <SurfaceCard class="space-y-2 mt-2">
    {#if canChange}
      <Textarea
        label="Diagram YAML"
        value={yaml}
        rows={8}
        disabled={busy}
        oninput={(event) => onyaml((event.target as HTMLTextAreaElement).value)}
      />
      <FileInput
        label="Import YAML file"
        accept=".yaml,.yml,text/yaml,application/yaml"
        disabled={busy}
        onselect={(files) => onfile(files[0]!)}
      />
      <Button size="sm" disabled={busy || !yaml.trim()} onclick={onimport}
        >Validate and apply YAML</Button
      >
    {/if}
    <Button size="sm" variant="secondary" disabled={busy} onclick={onexport}>Download YAML</Button>
  </SurfaceCard>
</details>
