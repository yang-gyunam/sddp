<script lang="ts">
  import { Button, IconButton, Input, Select } from '@sddp/ui';
  import { SurfaceCard } from '@sddp/shell';
  import type { DiagramRevision2, ElementRef } from '../../types2';
  import type { DiagramCommand2 } from '../../model/editor2';
  import { fields2 } from '../../model/editor2';
  import { getElement2, elements2, elementName2, owner2, refKey2 } from '../../model/model2';
  import { projectScene2, token2, ref2, resizable2 } from '../../model/scene2';
  let {
    model,
    selection,
    disabled,
    commands,
    oncommand,
    onselect,
  }: {
    model: DiagramRevision2;
    selection: ElementRef | null;
    disabled: boolean;
    commands: readonly DiagramCommand2['type'][];
    oncommand: (command: DiagramCommand2) => void;
    onselect: (ref: ElementRef) => void;
  } = $props();
  let properties: HTMLDivElement | undefined = $state();
  export function focusLabel() {
    const input = properties?.querySelector('input');
    input?.focus();
    input?.select();
  }
  const element = $derived(selection ? getElement2(model, selection) : undefined);
  const fields = $derived(selection ? fields2(model, selection) : []);
  const all = $derived(elements2(model));
  const owner = $derived(selection ? owner2(model, selection) : null);
  const ordered = $derived.by(() => {
    if (!selection || !owner) return [];
    if (model.diagramType === 'class' && selection.collection === 'parameters')
      return model.content.operations[owner.id]?.parameterOrder ?? [];
    if (model.diagramType === 'sequence' && selection.collection === 'events')
      return owner.collection === 'diagram'
        ? model.content.timeline
        : (model.content.branches[owner.id]?.events ?? []);
    return [];
  });
  const options = $derived(
    all.map(({ ref, element }) => ({
      value: refKey2(ref),
      label: `${ref.collection} · ${elementName2(element)}`,
    }))
  );
  const position = $derived(
    selection ? projectScene2(model).model.nodes[token2(selection)]?.presentation : undefined
  );
  function set(field: string, value: unknown) {
    if (selection) oncommand({ type: 'set', target: selection, field, value });
  }
  function geometry(field: 'x' | 'y' | 'width' | 'height', value: number) {
    if (!selection || !position) return;
    const bounds = {
      x: Number(position.x),
      y: Number(position.y),
      width: Number(position.width),
      height: Number(position.height),
      [field]: value,
    };
    oncommand(
      field === 'x' || field === 'y'
        ? { type: 'move', target: selection, x: bounds.x, y: bounds.y }
        : { type: 'resize', target: selection, ...bounds }
    );
  }
</script>

<SurfaceCard class="space-y-3">
  <Select
    label="Selected element"
    value={selection ? refKey2(selection) : ''}
    placeholder="Select an element"
    {options}
    onchange={(value) => {
      if (value) onselect(ref2(value));
    }}
  />
  {#if selection && element}
    <div aria-label="Diagram element properties" class="space-y-2" bind:this={properties}>
      <details class="text-xs">
        <summary>Identity</summary><code class="break-all"
          >{selection.collection} · {selection.id}</code
        >
      </details>
      {#each fields as field (field.key)}
        {#if field.nullable}<Select
            label={`${field.label} value`}
            value={element[field.key] === null ? 'null' : 'set'}
            options={[
              { value: 'null', label: 'Not specified (null)' },
              { value: 'set', label: 'Specified' },
            ]}
            disabled={disabled || !commands.includes('set')}
            onchange={(value) =>
              set(
                field.key,
                value === 'null'
                  ? null
                  : field.kind === 'select'
                    ? (field.options?.[0]?.value ?? '')
                    : ''
              )}
          />{/if}
        {#if element[field.key] !== null}
          {#if field.kind === 'select' || field.kind === 'boolean'}
            <Select
              label={field.label}
              value={String(element[field.key])}
              options={field.kind === 'boolean'
                ? [
                    { value: 'true', label: 'Yes' },
                    { value: 'false', label: 'No' },
                  ]
                : (field.options ?? [])}
              disabled={disabled || !commands.includes('set')}
              placeholder=""
              onchange={(value) =>
                set(field.key, field.kind === 'boolean' ? value === 'true' : value)}
            />
          {:else if field.kind === 'keys'}
            <div class="flex items-center gap-2">
              <span class="text-xs">Keys</span
              >{#each ['primary', 'foreign', 'unique'] as key (key)}<Button
                  size="sm"
                  variant={(element[field.key] as string[]).includes(key) ? 'primary' : 'secondary'}
                  disabled={disabled || !commands.includes('set')}
                  onclick={() =>
                    set(
                      field.key,
                      (element![field.key] as string[]).includes(key)
                        ? (element![field.key] as string[]).filter((k) => k !== key)
                        : [...(element![field.key] as string[]), key]
                    )}>{key}</Button
                >{/each}
            </div>
          {:else}
            <Input
              label={field.label}
              value={String(element[field.key] ?? '')}
              disabled={disabled || !commands.includes('set')}
              oninput={(event) => set(field.key, (event.target as HTMLInputElement).value)}
            />
          {/if}
        {/if}
      {/each}
      {#if position && resizable2(model, selection)}
        <div class="grid grid-cols-2 gap-2">
          {#each ['x', 'y', 'width', 'height'] as key (key)}<Input
              label={key.toUpperCase()}
              type="number"
              value={position[key] ?? ''}
              disabled={disabled ||
                !commands.includes(key === 'x' || key === 'y' ? 'move' : 'resize')}
              onchange={(event) =>
                geometry(
                  key as 'x' | 'y' | 'width' | 'height',
                  Number((event.target as HTMLInputElement).value)
                )}
            />{/each}
        </div>
      {/if}
      {#if ordered.length && owner}
        <Select
          label="Semantic owner"
          value={refKey2(owner)}
          options={model.diagramType === 'sequence'
            ? [
                {
                  value: refKey2({ collection: 'diagram', id: model.diagramId }),
                  label: 'Root timeline',
                },
                ...all
                  .filter((v) => v.ref.collection === 'branches')
                  .map((v) => ({ value: refKey2(v.ref), label: elementName2(v.element) })),
              ]
            : all
                .filter((v) => v.ref.collection === 'operations')
                .map((v) => ({ value: refKey2(v.ref), label: elementName2(v.element) }))}
          disabled={disabled || !commands.includes('reorder')}
          onchange={(value) =>
            oncommand({ type: 'reorder', target: selection!, owner: ref2(value), index: 0 })}
        />
        <Input
          label="Semantic position (1-based)"
          type="number"
          value={ordered.indexOf(selection.id) + 1}
          disabled={disabled || !commands.includes('reorder')}
          onchange={(event) =>
            oncommand({
              type: 'reorder',
              target: selection!,
              owner: owner!,
              index: Number((event.target as HTMLInputElement).value) - 1,
            })}
        />
      {/if}
      <div class="flex gap-2 flex-wrap">
        {#if ['types', 'entities'].includes(selection.collection)}<Button
            size="sm"
            variant="secondary"
            disabled={disabled || !commands.includes('add')}
            onclick={() => oncommand({ type: 'add', stencil: 'attribute', owner: selection! })}
            >Add attribute</Button
          >{/if}
        {#if selection.collection === 'types'}<Button
            size="sm"
            variant="secondary"
            disabled={disabled || !commands.includes('add')}
            onclick={() => oncommand({ type: 'add', stencil: 'operation', owner: selection! })}
            >Add operation</Button
          >{/if}
        {#if selection.collection === 'operations'}<Button
            size="sm"
            variant="secondary"
            disabled={disabled || !commands.includes('add')}
            onclick={() => oncommand({ type: 'add', stencil: 'parameter', owner: selection! })}
            >Add parameter</Button
          >{/if}
        {#if element.type === 'fragment' && element.kind === 'alt'}<Button
            size="sm"
            variant="secondary"
            disabled={disabled || !commands.includes('add')}
            onclick={() => oncommand({ type: 'add', stencil: 'else', owner: selection! })}
            >Add ELSE branch</Button
          >{/if}
        {#if selection.collection === 'participants'}<Button
            size="sm"
            variant="secondary"
            disabled={disabled || !commands.includes('activation')}
            onclick={() => oncommand({ type: 'activation', participant: selection!.id })}
            >Add activation pair</Button
          >{/if}
        <IconButton
          icon="trash"
          variant="danger"
          size="sm"
          title="Delete element and dependents"
          disabled={disabled || !commands.includes('remove')}
          onclick={() => oncommand({ type: 'remove', target: selection! })}
        />
      </div>
    </div>
  {/if}
</SurfaceCard>
