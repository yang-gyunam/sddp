<script lang="ts">
  import { Button } from '@sddp/ui';
  import { SurfaceCard } from '@sddp/shell';
  import type { RendererInput2 } from '../../model/scene2';
  import {
    elements2,
    refKey2,
    sameRef2,
    owner2,
    getElement2,
    elementName2,
  } from '../../model/model2';
  let { model, left, changes = [], selection, onselect }: RendererInput2 = $props();
  const rows = $derived([
    ...elements2(model),
    ...(left ? elements2(left).filter((e) => !getElement2(model, e.ref)) : []),
  ]);
</script>

<div
  aria-label="Diagram table"
  class="space-y-2 border-t border-[var(--color-border-primary)] pt-2"
>
  {#each rows as row (refKey2(row.ref))}
    {@const flags = changes.filter((c) => sameRef2(c.target, row.ref))}
    {@const removed = !getElement2(model, row.ref)}
    <SurfaceCard
      padding="sm"
      class={sameRef2(selection, row.ref) ? 'ring-1 ring-[var(--color-accent-primary)]' : ''}
    >
      <Button
        size="sm"
        variant="ghost"
        class="max-w-full break-all"
        aria-pressed={sameRef2(selection, row.ref)}
        onclick={() => onselect?.(row.ref)}
        >{row.ref.collection} · {elementName2(row.element)}{removed ? ' · Removed' : ''}</Button
      >
      <dl class="grid grid-cols-[auto_minmax(0,1fr)] gap-x-3 gap-y-1 text-xs mt-2">
        <dt>Identity</dt>
        <dd class="break-all">{row.ref.id}</dd>
        <dt>Owner</dt>
        <dd class="break-all">{JSON.stringify(owner2(removed && left ? left : model, row.ref))}</dd>
        <dt>Typed fields</dt>
        <dd>
          <pre class="whitespace-pre-wrap break-all">{JSON.stringify(row.element, null, 2)}</pre>
        </dd>
        <dt>Changes</dt>
        <dd>
          {flags.map((c) => `${c.category} ${c.kind} ${c.field?.join('.') ?? ''}`).join(', ') ||
            'Unchanged'}
        </dd>
      </dl>
    </SurfaceCard>
  {/each}
</div>
