<script lang="ts">
  import type { DiagramRevision2, DiagramChange2, ElementRef } from '../../types2';
  import type { SceneNode } from '../../model/scene';
  import type { Projection2 } from '../../model/scene2';
  import { ref2 } from '../../model/scene2';
  import { elements2, getElement2, owner2, sameRef2, elementName2 } from '../../model/model2';
  let {
    node,
    model,
    projection,
    changes = [],
    left,
    selected = false,
  }: {
    node: SceneNode;
    model: DiagramRevision2;
    projection: Projection2;
    changes?: DiagramChange2[];
    left?: DiagramRevision2;
    selected?: boolean;
  } = $props();
  const ref = $derived(ref2(node.id));
  const element = $derived(getElement2(model, ref)!);
  const color = $derived(
    node.removed
      ? '#dc2626'
      : node.semantic
        ? '#b45309'
        : node.presentation
          ? '#2563eb'
          : 'currentColor'
  );
  const members = $derived(
    [
      ...elements2(model).filter((e) => sameRef2(owner2(model, e.ref), ref)),
      ...(left
        ? elements2(left)
            .filter((e) => sameRef2(owner2(left, e.ref), ref) && !getElement2(model, e.ref))
            .map((e) => ({ ...e, removed: true }))
        : []),
    ].sort((a, b) => {
      const order = element.presentation.order;
      const ai = order.findIndex((r) => sameRef2(r, a.ref)),
        bi = order.findIndex((r) => sameRef2(r, b.ref));
      return ai >= 0 || bi >= 0
        ? (ai < 0 ? Infinity : ai) - (bi < 0 ? Infinity : bi)
        : a.ref.collection < b.ref.collection
          ? -1
          : a.ref.collection > b.ref.collection
            ? 1
            : a.ref.id < b.ref.id
              ? -1
              : 1;
    })
  );

  const sw = $derived(selected ? 4 : 2);
  const fill = 'var(--color-bg-secondary)';
  const memberColor = (target: ElementRef) =>
    changes.some((c) => sameRef2(c.target, target) && c.category === 'semantic')
      ? '#b45309'
      : changes.some((c) => sameRef2(c.target, target))
        ? '#2563eb'
        : 'currentColor';
  function memberText(e: Record<string, unknown>) {
    const visibility =
      ({ public: '+', private: '-', protected: '#', internal: '~' } as Record<string, string>)[
        String(e.visibility)
      ] ?? '';
    if ('parameterOrder' in e && model.diagramType === 'class')
      return `${visibility}${e.name}(${(e.parameterOrder as string[])
        .map((id) => {
          const p =
            model.content.parameters[id] ??
            (left?.diagramType === 'class' ? left.content.parameters[id] : undefined);
          if (!p) return id;
          return p.name + (p.typeName === null ? '' : `: ${p.typeName}`);
        })
        .join(', ')})${e.returnType === null ? '' : `: ${e.returnType}`}`;
    return `${visibility}${e.name}: ${e.typeName}${Array.isArray(e.keys) && e.keys.length ? ` [${e.keys.join(', ')}]` : ''}`;
  }
  const clip = $derived(`shape-clip-${encodeURIComponent(node.id)}`);
</script>

<g stroke={color} stroke-width={sw} stroke-dasharray={node.removed ? '6 4' : undefined}>
  {#if model.diagramType === 'sequence' && ref.collection === 'events' && element.type === 'message'}
    {@const from = projection.participantX[String(element.from)]!}{@const to =
      projection.participantX[String(element.to)]!}{@const y = node.y + 25}
    <rect
      x={node.x}
      y={node.y}
      width={node.width}
      height={node.height}
      fill="transparent"
      stroke={selected ? color : 'transparent'}
    />
    <path
      d={from === to ? `M ${from} ${y} h 65 v 20 h -65` : `M ${from} ${y} L ${to} ${y}`}
      fill="none"
      stroke-dasharray={element.kind === 'dashedArrow' ? '6 4' : undefined}
    />
    <path
      d={from === to
        ? `M ${to + 9} ${y + 14} L ${to} ${y + 20} L ${to + 9} ${y + 26}`
        : `M ${to + (to > from ? -10 : 10)} ${y - 6} L ${to} ${y} L ${to + (to > from ? -10 : 10)} ${y + 6}`}
      fill="none"
    />
    <text
      x={(from + to) / 2 + (from === to ? 35 : 0)}
      y={y - 9}
      text-anchor="middle"
      stroke="none"
      fill={color}
      font-size="12">{String(element.text)}</text
    >
  {:else if model.diagramType === 'sequence' && ref.collection === 'events' && element.type === 'fragment'}
    <rect x={node.x} y={node.y} width={node.width} height={node.height} fill="transparent" />
    <path d={`M ${node.x} ${node.y + 25} h 65 l 12 -12 v -13`} {fill} />
    <text x={node.x + 8} y={node.y + 17} stroke="none" fill={color} font-size="12"
      >{String(element.kind)}</text
    >
  {:else if model.diagramType === 'sequence' && ref.collection === 'branches'}
    <rect
      x={node.x}
      y={node.y}
      width={node.width}
      height={node.height}
      fill="transparent"
      stroke={selected ? color : 'transparent'}
    />
    <path d={`M ${node.x} ${node.y} h ${node.width}`} stroke-dasharray="4 3" />
    <text x={node.x + 12} y={node.y + 20} stroke="none" fill={color} font-size="12"
      >[{String(element.label)}]</text
    >
  {:else if model.diagramType === 'sequence' && ref.collection === 'events'}
    {@const end =
      element.type === 'activationStart'
        ? Object.entries(model.content.events).find(
            ([, e]) => e.type === 'activationEnd' && e.start === ref.id
          )
        : undefined}
    {@const height = end ? Math.max(20, (projection.eventY[end[0]] ?? node.y) - node.y) : 12}
    <rect x={node.x} y={node.y} width="20" {height} {fill} />
    <text x={node.x + 25} y={node.y + 14} stroke="none" fill={color} font-size="10"
      >{element.type === 'activationStart' ? 'activate' : 'deactivate'}</text
    >
  {:else if model.diagramType === 'flowchart' && element.kind === 'decision'}
    <polygon
      points={`${node.x + node.width / 2},${node.y} ${node.x + node.width},${node.y + node.height / 2} ${node.x + node.width / 2},${node.y + node.height} ${node.x},${node.y + node.height / 2}`}
      {fill}
    />
  {:else if model.diagramType === 'state' && element.kind !== 'state'}
    <circle
      cx={node.x + node.width / 2}
      cy={node.y + node.height / 2}
      r={Math.min(node.width, node.height) / 2 - 3}
      fill={element.kind === 'initial' ? color : fill}
    />
    {#if element.kind === 'final'}<circle
        cx={node.x + node.width / 2}
        cy={node.y + node.height / 2}
        r={Math.min(node.width, node.height) / 2 - 9}
        fill={color}
      />{/if}
  {:else if model.diagramType === 'c4' && ref.collection === 'boundaries'}
    <rect
      x={node.x}
      y={node.y}
      width={node.width}
      height={node.height}
      rx="4"
      fill="transparent"
      stroke-dasharray="8 4"
    />
    <text x={node.x + 12} y={node.y + 23} stroke="none" fill={color} font-size="13"
      >{String(element.name)} · System boundary</text
    >
  {:else if model.diagramType === 'c4' && element.kind === 'database'}
    <path
      d={`M ${node.x} ${node.y + 16} a ${node.width / 2} 16 0 0 1 ${node.width} 0 v ${node.height - 32} a ${node.width / 2} 16 0 0 1 -${node.width} 0 z`}
      {fill}
    />
    <ellipse cx={node.x + node.width / 2} cy={node.y + 16} rx={node.width / 2} ry="16" {fill} />
  {:else if (model.diagramType === 'c4' && element.kind === 'person') || (model.diagramType === 'sequence' && element.kind === 'actor')}
    <rect x={node.x} y={node.y} width={node.width} height={node.height} rx="6" {fill} />
    <circle cx={node.x + 20} cy={node.y + 22} r="9" fill="none" />
    <path
      d={`M ${node.x + 20} ${node.y + 32} v 24 m -12 -15 h 24 m -12 15 l -12 14 m 12 -14 l 12 14`}
      fill="none"
    />
  {:else}
    <rect
      x={node.x}
      y={node.y}
      width={node.width}
      height={node.height}
      rx={model.diagramType === 'flowchart' && element.kind === 'terminator' ? node.height / 2 : 6}
      {fill}
    />
  {/if}
  {#if model.diagramType === 'class' || model.diagramType === 'er'}
    <defs
      ><clipPath id={clip}
        ><rect
          x={node.x + 4}
          y={node.y + 4}
          width={node.width - 8}
          height={node.height - 8}
        /></clipPath
      ></defs
    >
    <g clip-path={`url(#${clip})`} stroke="none" fill={color} font-size="12">
      <text x={node.x + 12} y={node.y + 25} font-weight="600">{String(element.name)}</text>
      {#each members as member, i (member.ref.id)}<text
          x={node.x + 12}
          y={node.y + 58 + i * 25}
          fill={'removed' in member ? '#dc2626' : memberColor(member.ref)}
          text-decoration={'removed' in member ? 'line-through' : undefined}
          ><title>{memberText(member.element)}</title>{memberText(member.element)}</text
        >{/each}
    </g>
    <path d={`M ${node.x} ${node.y + 38} h ${node.width}`} stroke-width="1" />
  {:else if (model.diagramType !== 'sequence' || ref.collection === 'participants') && (model.diagramType !== 'state' || element.kind === 'state') && ref.collection !== 'boundaries'}
    <text
      x={node.x + node.width / 2}
      y={node.y + node.height / 2 - (model.diagramType === 'c4' ? 12 : -4)}
      text-anchor="middle"
      stroke="none"
      fill={color}
      font-size="13"
      ><title>{elementName2(element)}</title>{elementName2(element).slice(
        0,
        Math.max(6, Math.floor((node.width - 24) / 7))
      )}</text
    >
    {#if model.diagramType === 'c4'}
      <text
        x={node.x + node.width / 2}
        y={node.y + node.height / 2 + 8}
        text-anchor="middle"
        stroke="none"
        fill={color}
        font-size="11"
        >[{String(element.kind)}{element.technology ? ` · ${element.technology}` : ''}]</text
      >
      <text
        x={node.x + node.width / 2}
        y={node.y + node.height / 2 + 27}
        text-anchor="middle"
        stroke="none"
        fill={color}
        font-size="11"
        ><title>{String(element.description ?? '')}</title>{String(element.description ?? '').slice(
          0,
          Math.floor((node.width - 20) / 6)
        )}</text
      >
    {/if}
  {/if}
</g>
