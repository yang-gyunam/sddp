<script lang="ts">
  import { Button, Icon } from '@sddp/ui';
  import { SurfaceCard } from '@sddp/shell';
  import type { Stencil2 } from '../../model/editor2';
  let {
    stencils,
    canAdd,
    canConnect,
    tool,
    onadd,
    ontool,
  }: {
    stencils: Stencil2[];
    canAdd: boolean;
    canConnect: boolean;
    tool: 'select' | 'connect';
    onadd: (stencil: string) => void;
    ontool: (tool: 'select' | 'connect') => void;
  } = $props();
</script>

<aside aria-label="Diagram palette" class="w-32 shrink-0">
  <SurfaceCard padding="sm" class="space-y-2">
    <p class="text-xs font-semibold">Tools</p>
    <Button
      size="sm"
      class="w-full"
      variant={tool === 'select' ? 'primary' : 'ghost'}
      aria-pressed={tool === 'select'}
      onclick={() => ontool('select')}>Select</Button
    >
    {#if canConnect}<Button
        size="sm"
        class="w-full"
        variant={tool === 'connect' ? 'primary' : 'ghost'}
        aria-pressed={tool === 'connect'}
        aria-label="Add connection"
        onclick={() => ontool('connect')}><Icon name="link" size="sm" />Connect</Button
      >{/if}
    {#if canAdd}
      {#each stencils as stencil (stencil.id)}
        <Button
          size="sm"
          variant="secondary"
          class="w-full flex-col py-2"
          aria-label={`Add ${stencil.label}`}
          draggable="true"
          ondragstart={(event) => {
            event.dataTransfer?.setData('application/x-sddp-diagram-node', stencil.id);
            if (event.dataTransfer) event.dataTransfer.effectAllowed = 'copy';
          }}
          onclick={() => onadd(stencil.id)}
        >
          <svg viewBox="0 0 60 36" width="50" height="30" aria-hidden="true">
            {#if stencil.id === 'decision'}<polygon
                points="30,2 58,18 30,34 2,18"
                fill="none"
                stroke="currentColor"
              />{:else if ['initial', 'final'].includes(stencil.id)}<circle
                cx="30"
                cy="18"
                r="14"
                fill={stencil.id === 'initial' ? 'currentColor' : 'none'}
                stroke="currentColor"
              />{:else}<rect
                x="2"
                y="2"
                width="56"
                height="32"
                rx={stencil.id === 'terminator' ? 16 : 4}
                fill="none"
                stroke="currentColor"
              />{/if}
          </svg><span class="text-xs">{stencil.label}</span>
        </Button>
      {/each}
      <p class="text-xs text-[var(--color-text-tertiary)]">Drag a shape onto the canvas.</p>
    {/if}
  </SurfaceCard>
</aside>
