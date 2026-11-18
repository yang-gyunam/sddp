<script lang="ts">
  import type { SceneEdge } from '../../model/scene';
  import type { DiagramRevision2 } from '../../types2';
  import { getElement2 } from '../../model/model2';
  import { ref2 } from '../../model/scene2';
  import { svgConnectionGeometry } from './svgGeometry';
  let {
    edge,
    model,
    selected = false,
  }: { edge: SceneEdge; model: DiagramRevision2; selected?: boolean } = $props();
  const value = $derived(getElement2(model, ref2(edge.id))!);
  const geometry = $derived(svgConnectionGeometry(edge.start!, edge.end!, edge.from === edge.to));
  const color = $derived(
    edge.removed
      ? '#dc2626'
      : edge.semantic
        ? '#b45309'
        : edge.presentation
          ? '#2563eb'
          : 'currentColor'
  );
  const prefix = `typed-edge-${crypto.randomUUID()}`;
  const triangle = $derived(value.kind === 'inheritance' || value.kind === 'realization');
  const diamond = $derived(value.kind === 'composition' || value.kind === 'aggregation');
  const dashed = $derived(
    edge.removed ||
      value.kind === 'dependency' ||
      value.kind === 'realization' ||
      value.identifying === false
  );
  const cardinal = (v: unknown) =>
    (
      ({ zeroOrOne: '0..1', one: '1', zeroOrMore: '0..*', oneOrMore: '1..*' }) as Record<
        string,
        string
      >
    )[String(v)] ?? '';
</script>

<defs>
  <marker
    id={`${prefix}-end`}
    viewBox="0 0 14 14"
    refX="13"
    refY="7"
    markerWidth="9"
    markerHeight="9"
    orient="auto-start-reverse"
    ><path
      d={triangle ? 'M 1 1 L 13 7 L 1 13 Z' : 'M 2 2 L 12 7 L 2 12'}
      stroke={color}
      fill={triangle ? 'var(--color-bg-primary)' : 'none'}
    /></marker
  >
  <marker
    id={`${prefix}-start`}
    viewBox="0 0 20 14"
    refX="1"
    refY="7"
    markerWidth="12"
    markerHeight="9"
    orient="auto-start-reverse"
    ><path
      d="M 1 7 L 10 1 L 19 7 L 10 13 Z"
      stroke={color}
      fill={value.kind === 'composition' ? color : 'var(--color-bg-primary)'}
    /></marker
  >
</defs>
<path
  d={geometry.path}
  fill="none"
  stroke={color}
  stroke-width={selected ? 4 : 2}
  stroke-dasharray={dashed ? '6 4' : undefined}
  marker-start={diamond ? `url(#${prefix}-start)` : undefined}
  marker-end={model.diagramType === 'er' || diamond || value.kind === 'association'
    ? undefined
    : `url(#${prefix}-end)`}
/>
<text
  x={geometry.labelX}
  y={geometry.labelY}
  text-anchor={geometry.anchor}
  stroke="var(--color-bg-primary)"
  stroke-width="3"
  paint-order="stroke"
  fill={color}
  font-size="12">{edge.label}{value.technology ? ` [${value.technology}]` : ''}</text
>
{#if model.diagramType === 'er'}
  <text
    x={edge.start!.x + edge.start!.width / 2}
    y={edge.start!.y - 8}
    text-anchor="middle"
    fill={color}
    font-size="12">{cardinal(value.fromCardinality)}</text
  >
  <text
    x={edge.end!.x + edge.end!.width / 2}
    y={edge.end!.y - 8}
    text-anchor="middle"
    fill={color}
    font-size="12">{cardinal(value.toCardinality)}</text
  >
{/if}
