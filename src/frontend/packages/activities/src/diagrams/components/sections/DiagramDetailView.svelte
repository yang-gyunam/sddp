<script lang="ts">
  import type { DiagramDto } from '../../types';
  let { diagram }: { diagram: DiagramDto } = $props();
</script>

<div class="p-4 space-y-5" aria-label="Diagram content">
  <dl class="grid grid-cols-2 gap-3 text-sm">
    <div>
      <dt class="text-[var(--color-text-tertiary)]">Kind</dt>
      <dd>{diagram.kind === 'authored' ? 'Authored' : 'Derived'}</dd>
    </div>
    <div>
      <dt class="text-[var(--color-text-tertiary)]">Revision</dt>
      <dd>{diagram.revision}</dd>
    </div>
  </dl>
  <section aria-label="Diagram nodes">
    <h3 class="text-sm font-semibold mb-2">Nodes ({Object.keys(diagram.model.nodes).length})</h3>
    {#if Object.keys(diagram.model.nodes).length}
      <ul class="divide-y divide-[var(--color-border-secondary)]">
        {#each Object.entries(diagram.model.nodes) as [id, node] (id)}
          <li class="py-2 flex justify-between gap-3 text-sm">
            <span>{node.label}</span><span
              class="text-xs text-[var(--color-text-tertiary)] font-mono">{id}</span
            >
          </li>
        {/each}
      </ul>
    {:else}<p class="text-sm text-[var(--color-text-tertiary)]">No nodes</p>{/if}
  </section>
  <section aria-label="Diagram connections">
    <h3 class="text-sm font-semibold mb-2">
      Connections ({Object.keys(diagram.model.edges).length})
    </h3>
    {#if Object.keys(diagram.model.edges).length}
      <ul class="divide-y divide-[var(--color-border-secondary)]">
        {#each Object.entries(diagram.model.edges) as [id, edge] (id)}
          <li class="py-2 text-sm">
            <span
              >{diagram.model.nodes[edge.from]?.label ?? edge.from} → {diagram.model.nodes[edge.to]
                ?.label ?? edge.to}</span
            >{#if edge.label}<span class="ml-2 text-[var(--color-text-secondary)]"
                >{edge.label}</span
              >{/if}
          </li>
        {/each}
      </ul>
    {:else}<p class="text-sm text-[var(--color-text-tertiary)]">No connections</p>{/if}
  </section>
  <section aria-label="Diagram source">
    <h3 class="text-sm font-semibold mb-2">Source</h3>
    <pre
      class="text-sm whitespace-pre-wrap break-words rounded p-3 bg-[var(--color-bg-secondary)]">{diagram
        .model.source.text || 'No source text'}</pre>
  </section>
</div>
