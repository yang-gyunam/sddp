<script lang="ts">
  import type { RendererInput2 } from '../../model/scene2';
  import { projectScene2, ref2, token2, resizable2, connectable2 } from '../../model/scene2';
  import { sameRef2 } from '../../model/model2';
  import SvgDiagramRenderer from './SvgDiagramRenderer.svelte';
  import DiagramShape2 from './DiagramShape2.svelte';
  import DiagramEdge2 from './DiagramEdge2.svelte';
  let {
    model,
    left,
    changes = [],
    selection,
    onselect,
    onmove,
    onresize,
    oncreateNode,
    onconnect,
    onconnectstart,
    onrename,
  }: RendererInput2 = $props();
  const projection = $derived(projectScene2(model, changes, left));
  const original = $derived(left ? projectScene2(left) : undefined);
</script>

<SvgDiagramRenderer
  model={projection.model}
  left={original?.model}
  changes={projection.changes}
  selection={selection
    ? { target: projection.model.edges[token2(selection)] ? 'edge' : 'node', id: token2(selection) }
    : null}
  onselect={onselect ? (value) => onselect(ref2(value.id)) : undefined}
  onmove={onmove ? (id, x, y) => onmove(ref2(id), x, y) : undefined}
  onresize={onresize ? (id, bounds) => onresize(ref2(id), bounds) : undefined}
  {oncreateNode}
  onconnect={onconnect ? (from, to) => onconnect(ref2(from), ref2(to)) : undefined}
  onconnectstart={onconnectstart ? (id) => onconnectstart(ref2(id)) : undefined}
  onrename={onrename ? (id) => onrename(ref2(id)) : undefined}
  canResizeNode={(id) => resizable2(model, ref2(id))}
  canConnectNode={(id) => connectable2(model, ref2(id))}
>
  {#snippet underlay()}
    {#if model.diagramType === 'sequence'}
      {#each Object.entries(projection.participantX) as [id, x] (id)}<path
          d={`M ${x} 95 V ${projection.bottom}`}
          fill="none"
          stroke="currentColor"
          stroke-width="1"
          stroke-dasharray="5 5"
          ><title>Lifeline {model.content.participants[id]?.name}</title></path
        >{/each}
    {/if}
  {/snippet}
  {#snippet renderNode(node)}<DiagramShape2
      {node}
      model={node.removed && left ? left : model}
      projection={node.removed && original ? original : projection}
      {changes}
      left={node.removed ? undefined : left}
      selected={sameRef2(selection, ref2(node.id))}
    />{/snippet}
  {#snippet renderEdge(edge)}<DiagramEdge2
      {edge}
      model={edge.removed && left ? left : model}
      selected={sameRef2(selection, ref2(edge.id))}
    />{/snippet}
</SvgDiagramRenderer>
{#if projection.diagnostics.length}<ul role="status" class="text-xs">
    {#each projection.diagnostics as diagnostic (diagnostic)}<li>{diagnostic}</li>{/each}
  </ul>{/if}
