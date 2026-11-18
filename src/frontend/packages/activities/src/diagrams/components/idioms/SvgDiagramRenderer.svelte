<script lang="ts">
  import { onDestroy, onMount, untrack } from 'svelte';
  import { IconButton } from '@sddp/ui';
  import { SurfaceCard } from '@sddp/shell';
  import {
    resizeHandles,
    resizedBounds,
    resizeHandlePoint,
    type NodeBounds,
    type ResizeHandle,
  } from '../../model/nodeGeometry';
  import { svgConnectionGeometry } from './svgGeometry';
  import { diagramScene, type RendererInput } from '../../model/scene';
  let {
    renderNode,
    renderEdge,
    underlay,
    canResizeNode,
    canConnectNode,
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
  }: RendererInput = $props();
  const baseScene = $derived(diagramScene(model, left, changes));
  let moving = $state<{ id: string; x: number; y: number } | null>(null);
  let resizing = $state<(NodeBounds & { id: string }) | null>(null);
  let unitsPerPixel = $state(1);
  const scene = $derived.by(() => {
    if (!moving && !resizing) return baseScene;
    const position = (resizing ?? moving)!;
    const nodes = baseScene.nodes.map((node) =>
      node.id === position.id ? { ...node, ...position } : node
    );
    const positions = new Map(nodes.map((node) => [node.id, node]));
    return {
      ...baseScene,
      nodes,
      edges: baseScene.edges.map((edge) =>
        edge.removed
          ? edge
          : { ...edge, start: positions.get(edge.from), end: positions.get(edge.to) }
      ),
    };
  });
  // Editing never silently fits again: canvas coordinates stay under the pointer.
  let fittedBox = $state('0 0 600 300');
  const viewScope = $derived(
    `${model.diagramId}|${left ? `${left.revision}:${model.revision}` : 'draft'}`
  );
  let displayedScope = '';
  $effect(() => {
    if (viewScope !== displayedScope) {
      displayedScope = viewScope;
      untrack(() => {
        releaseDrag();
        fit();
      });
    }
  });
  function fit() {
    releaseDrag();
    fittedBox = baseScene.viewBox;
    zoom = 1;
  }
  const marker = `diagram-arrow-${crypto.randomUUID()}`;
  let svg: SVGSVGElement;
  let zoom = $state(1);
  let preview = $state<{ x: number; y: number; endX: number; endY: number } | null>(null);
  const viewBox = $derived.by(() => {
    const [x = 0, y = 0, width = 600, height = 300] = fittedBox.split(' ').map(Number);
    return `${x + (width * (1 - 1 / zoom)) / 2} ${y + (height * (1 - 1 / zoom)) / 2} ${width / zoom} ${height / zoom}`;
  });
  function updateUnits() {
    const matrix = svg?.getScreenCTM?.();
    const scale = matrix ? Math.hypot(matrix.a, matrix.b) : 0;
    unitsPerPixel = Number.isFinite(scale) && scale > 0 ? 1 / scale : 1;
  }
  $effect(() => {
    void viewBox;
    untrack(updateUnits);
  });
  onMount(() => {
    if (typeof ResizeObserver === 'undefined') return;
    const observer = new ResizeObserver(updateUnits);
    observer.observe(svg);
    updateUnits();
    return () => observer.disconnect();
  });
  // A new model/selection or revoked editing capability invalidates an in-flight gesture.
  $effect(() => {
    void [model, selection?.target, selection?.id, onresize, onmove];
    untrack(() => cancelResize());
  });
  let cancelResize = () => {};
  function beginResize(
    event: PointerEvent,
    node: (typeof scene.nodes)[number],
    handle: ResizeHandle
  ) {
    if (!onresize || node.removed || event.button !== 0) return;
    const matrix = svg.getScreenCTM()?.inverse();
    if (!matrix) return;
    event.preventDefault();
    event.stopPropagation();
    releaseDrag();
    const origin = new DOMPoint(event.clientX, event.clientY).matrixTransform(matrix);
    const commit = onresize;
    const move = (next: PointerEvent) => {
      const point = new DOMPoint(next.clientX, next.clientY).matrixTransform(matrix);
      const bounds = resizedBounds(
        node,
        handle,
        Math.round(point.x - origin.x),
        Math.round(point.y - origin.y)
      );
      resizing = { id: node.id, ...bounds };
    };
    const cancel = () => {
      window.removeEventListener('pointermove', move);
      window.removeEventListener('pointerup', up);
      window.removeEventListener('pointercancel', cancel);
      window.removeEventListener('keydown', escape);
      resizing = null;
      cancelResize = () => {};
    };
    const escape = (key: KeyboardEvent) => {
      if (key.key === 'Escape') {
        key.preventDefault();
        key.stopPropagation();
        cancel();
      }
    };
    const up = (end: PointerEvent) => {
      move(end);
      const bounds = resizing;
      cancel();
      if (
        bounds &&
        ['x', 'y', 'width', 'height'].some(
          (key) => bounds[key as keyof NodeBounds] !== node[key as keyof NodeBounds]
        )
      )
        commit(node.id, { x: bounds.x, y: bounds.y, width: bounds.width, height: bounds.height });
    };
    releaseDrag = cancel;
    cancelResize = cancel;
    window.addEventListener('pointermove', move);
    window.addEventListener('pointerup', up, { once: true });
    window.addEventListener('pointercancel', cancel, { once: true });
    window.addEventListener('keydown', escape);
  }
  function resizeKey(
    event: KeyboardEvent,
    node: (typeof scene.nodes)[number],
    handle: ResizeHandle
  ) {
    if (!onresize || !['ArrowLeft', 'ArrowRight', 'ArrowUp', 'ArrowDown'].includes(event.key))
      return;
    event.preventDefault();
    event.stopPropagation();
    releaseDrag();
    const step = event.shiftKey ? 10 : 1;
    const bounds = resizedBounds(
      node,
      handle,
      event.key === 'ArrowLeft' ? -step : event.key === 'ArrowRight' ? step : 0,
      event.key === 'ArrowUp' ? -step : event.key === 'ArrowDown' ? step : 0
    );
    if (
      ['x', 'y', 'width', 'height'].some(
        (key) => bounds[key as keyof NodeBounds] !== node[key as keyof NodeBounds]
      )
    )
      onresize(node.id, bounds);
  }
  function canvasPoint(event: { clientX: number; clientY: number }) {
    const matrix = svg.getScreenCTM()?.inverse();
    return matrix ? new DOMPoint(event.clientX, event.clientY).matrixTransform(matrix) : null;
  }
  function dropNode(event: DragEvent) {
    if (!oncreateNode || !event.dataTransfer?.getData('application/x-sddp-diagram-node')) return;
    event.preventDefault();
    event.stopPropagation();
    const point = canvasPoint(event);
    if (point) {
      const [visibleX = 0, visibleY = 0] = viewBox.split(' ').map(Number);
      oncreateNode(
        Math.max(visibleX + 10, Math.round(point.x - 80)),
        Math.max(visibleY + 10, Math.round(point.y - 25)),
        event.dataTransfer?.getData('application/x-sddp-diagram-node')
      );
    }
  }
  function beginConnect(event: PointerEvent, node: (typeof scene.nodes)[number]) {
    if (!onconnect || event.button !== 0) return;
    event.preventDefault();
    event.stopPropagation();
    releaseDrag();
    onconnectstart?.(node.id);
    preview = {
      x: node.x + node.width,
      y: node.y + node.height / 2,
      endX: node.x + node.width,
      endY: node.y + node.height / 2,
    };
    const move = (next: PointerEvent) => {
      const point = canvasPoint(next);
      if (preview && point) preview = { ...preview, endX: point.x, endY: point.y };
    };
    const cancel = () => {
      window.removeEventListener('pointermove', move);
      window.removeEventListener('pointerup', up);
      window.removeEventListener('pointercancel', cancel);
      preview = null;
    };
    const up = (end: PointerEvent) => {
      const target = document
        .elementFromPoint(end.clientX, end.clientY)
        ?.closest('[data-node-id]')
        ?.getAttribute('data-node-id');
      cancel();
      if (target && Object.hasOwn(model.nodes, target)) onconnect?.(node.id, target);
    };
    releaseDrag = cancel;
    window.addEventListener('pointermove', move);
    window.addEventListener('pointerup', up, { once: true });
    window.addEventListener('pointercancel', cancel, { once: true });
  }

  let releaseDrag = () => {};
  onDestroy(() => releaseDrag());
  function beginMove(event: PointerEvent, node: (typeof scene.nodes)[number]) {
    releaseDrag();
    if (!onmove || node.removed || event.button !== 0) return;
    const matrix = svg.getScreenCTM()?.inverse();
    if (!matrix) return;
    event.preventDefault();
    const origin = new DOMPoint(event.clientX, event.clientY).matrixTransform(matrix);
    const commit = onmove;
    const move = (next: PointerEvent) => {
      const point = new DOMPoint(next.clientX, next.clientY).matrixTransform(matrix);
      if (Math.hypot(point.x - origin.x, point.y - origin.y) > 2)
        moving = {
          id: node.id,
          x: Math.round(node.x + point.x - origin.x),
          y: Math.round(node.y + point.y - origin.y),
        };
    };
    const cancel = () => {
      window.removeEventListener('pointermove', move);
      window.removeEventListener('pointerup', up);
      window.removeEventListener('pointercancel', cancel);
      moving = null;
    };
    const up = (end: PointerEvent) => {
      move(end);
      const position = moving;
      cancel();
      if (position) commit(node.id, position.x, position.y);
    };
    releaseDrag = cancel;
    window.addEventListener('pointermove', move);
    window.addEventListener('pointerup', up, { once: true });
    window.addEventListener('pointercancel', cancel, { once: true });
  }
</script>

<SurfaceCard padding="none" class="overflow-hidden">
  <div class="flex items-center justify-end gap-1 p-2" role="group" aria-label="Canvas view">
    <IconButton
      icon="minus"
      size="sm"
      title="Zoom out"
      disabled={zoom <= 0.25}
      onclick={() => {
        releaseDrag();
        zoom = Math.max(0.25, zoom / 1.25);
      }}
    />
    <span class="text-xs w-10 text-center" aria-label="Canvas zoom">{Math.round(zoom * 100)}%</span>
    <IconButton
      icon="plus"
      size="sm"
      title="Zoom in"
      disabled={zoom >= 4}
      onclick={() => {
        releaseDrag();
        zoom = Math.min(4, zoom * 1.25);
      }}
    />
    <IconButton icon="maximize-2" size="sm" title="Fit diagram" onclick={fit} />
  </div>
  <div
    class="overflow-auto"
    role="region"
    aria-label="Diagram drop area"
    ondragover={(event) => {
      if (oncreateNode && event.dataTransfer?.types.includes('application/x-sddp-diagram-node'))
        event.preventDefault();
    }}
    ondrop={dropNode}
  >
    <svg
      bind:this={svg}
      {viewBox}
      class="w-full min-w-[500px] min-h-[300px] max-h-[600px]"
      role="img"
      aria-label="Diagram canvas"
    >
      <defs
        ><marker
          id={marker}
          viewBox="0 0 10 10"
          refX="9"
          refY="5"
          markerWidth="7"
          markerHeight="7"
          orient="auto-start-reverse"><path d="M 0 0 L 10 5 L 0 10 z" fill="currentColor" /></marker
        ></defs
      >
      {#if underlay}{@render underlay()}{/if}
      {#each scene.edges as edge (edge.id)}
        {#if edge.start && edge.end}
          {@const geometry = svgConnectionGeometry(edge.start, edge.end, edge.from === edge.to)}
          <g
            role="button"
            tabindex="0"
            aria-label={`Connection ${edge.label || edge.id}${edge.removed ? ' (removed)' : ''}`}
            onclick={() => onselect?.({ target: 'edge', id: edge.id })}
            onkeydown={(e) => {
              if (e.key === 'Enter' || e.key === ' ') {
                e.preventDefault();
                onselect?.({ target: 'edge', id: edge.id });
              }
            }}
            class="cursor-pointer"
            data-element-id={edge.id}
            data-change={edge.removed
              ? 'removed'
              : edge.semantic
                ? 'semantic'
                : edge.presentation
                  ? 'presentation'
                  : 'unchanged'}
          >
            {#if renderEdge}{@render renderEdge(edge)}{:else}
              <path
                d={geometry.path}
                fill="none"
                stroke={edge.removed
                  ? '#dc2626'
                  : edge.semantic
                    ? '#b45309'
                    : edge.presentation
                      ? '#2563eb'
                      : 'currentColor'}
                stroke-width={selection?.target === 'edge' && selection.id === edge.id ? 4 : 2}
                stroke-dasharray={edge.removed ? '6 4' : undefined}
                marker-end={`url(#${marker})`}
              />
              <text
                x={geometry.labelX}
                y={geometry.labelY}
                text-anchor={geometry.anchor}
                stroke="var(--color-bg-primary)"
                stroke-width="3"
                paint-order="stroke"
                fill="currentColor"
                font-size="12">{edge.label}</text
              >
            {/if}
          </g>
        {/if}
      {/each}
      {#each scene.nodes as node (node.id)}
        <g
          role="button"
          tabindex="0"
          aria-label={`Node ${node.label}${node.removed ? ' (removed)' : ''}`}
          data-element-id={node.id}
          data-node-id={node.id}
          data-change={node.removed
            ? 'removed'
            : node.added
              ? 'added'
              : node.semantic
                ? 'semantic'
                : node.presentation
                  ? 'presentation'
                  : 'unchanged'}
          onclick={() => onselect?.({ target: 'node', id: node.id })}
          onkeydown={(e) => {
            if (e.key === 'Enter' || e.key === ' ') {
              e.preventDefault();
              onselect?.({ target: 'node', id: node.id });
            }
          }}
          ondblclick={() => {
            if (!node.removed) onrename?.(node.id);
          }}
          onpointerdown={(event) => beginMove(event, node)}
          class="cursor-pointer select-none touch-none"
        >
          {#if renderNode}{@render renderNode(node)}{:else}
            <rect
              x={node.x}
              y={node.y}
              width={node.width}
              height={node.height}
              rx="7"
              fill="var(--color-bg-secondary)"
              stroke={node.removed
                ? '#dc2626'
                : node.semantic
                  ? '#b45309'
                  : node.presentation
                    ? '#2563eb'
                    : 'currentColor'}
              stroke-width={selection?.target === 'node' && selection.id === node.id ? 4 : 2}
              stroke-dasharray={node.removed ? '6 4' : undefined}
            />
            <text
              x={node.x + 10}
              y={node.y + node.height / 2 + 4}
              fill="currentColor"
              font-size="13"
              ><title>{node.label}</title>{node.label.length > Math.floor((node.width - 20) / 7)
                ? node.label.slice(0, Math.floor((node.width - 20) / 7)) + '…'
                : node.label}</text
            >
          {/if}
          {#if node.presentation}<circle
              cx={node.x + node.width - 10}
              cy={node.y + 8}
              r="4"
              fill="#2563eb"><title>Presentation changed</title></circle
            >{/if}
        </g>
      {/each}
      {#each scene.nodes as node (node.id)}
        {#if onconnect && !node.removed && (!canConnectNode || canConnectNode(node.id))}
          <circle
            cx={node.x + node.width + 18 * unitsPerPixel}
            cy={node.y + node.height / 2}
            r={6 * unitsPerPixel}
            fill="var(--color-accent-primary)"
            stroke="var(--color-bg-primary)"
            stroke-width="2"
            role="button"
            tabindex="0"
            aria-label={`Connect from ${node.label}`}
            class="cursor-crosshair touch-none"
            onpointerdown={(event) => beginConnect(event, node)}
            onkeydown={(event) => {
              if (event.key === 'Enter' || event.key === ' ') {
                event.preventDefault();
                onconnectstart?.(node.id);
              }
            }}
          />
        {/if}
      {/each}
      {#if onresize && selection?.target === 'node'}
        {#each scene.nodes.filter((node) => node.id === selection?.id && !node.removed && (!canResizeNode || canResizeNode(node.id))) as node (node.id)}
          {#each resizeHandles as handle (handle)}
            {@const point = resizeHandlePoint(node, handle)}
            <g
              role="button"
              tabindex="0"
              aria-label={`Resize ${handle.toUpperCase()} ${node.label}`}
              data-resize-handle={handle}
              style:cursor={`${handle}-resize`}
              class="touch-none"
              onpointerdown={(event) => beginResize(event, node, handle)}
              onkeydown={(event) => resizeKey(event, node, handle)}
            >
              <rect
                x={point.x - 8 * unitsPerPixel}
                y={point.y - 8 * unitsPerPixel}
                width={16 * unitsPerPixel}
                height={16 * unitsPerPixel}
                fill="transparent"
              />
              <rect
                x={point.x - 4 * unitsPerPixel}
                y={point.y - 4 * unitsPerPixel}
                width={8 * unitsPerPixel}
                height={8 * unitsPerPixel}
                fill="var(--color-bg-primary)"
                stroke="var(--color-accent-primary)"
                stroke-width="1.5"
                vector-effect="non-scaling-stroke"
              />
            </g>
          {/each}
        {/each}
      {/if}
      {#if preview}<path
          d={`M ${preview.x} ${preview.y} L ${preview.endX} ${preview.endY}`}
          fill="none"
          stroke="var(--color-accent-primary)"
          stroke-width="2"
          stroke-dasharray="5 4"
          pointer-events="none"
        />{/if}
    </svg>
  </div>
</SurfaceCard>
{#if scene.nodes.length === 0}<p class="text-sm text-[var(--color-text-tertiary)]">No nodes</p>{/if}
{#if scene.diagnostics.length}<ul
    role="status"
    class="text-xs text-[var(--color-text-secondary)] mt-2"
  >
    {#each scene.diagnostics as diagnostic (diagnostic)}<li>{diagnostic}</li>{/each}
  </ul>{/if}
