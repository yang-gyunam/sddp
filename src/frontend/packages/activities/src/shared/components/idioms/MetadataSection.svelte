<!--
  MetadataSection Idiom
  Displays key-value metadata in a consistent 2-column grid.
  Reusable across Requirements, Specs, Glossary, and Artifacts.
  Supports optional Avatar rendering for person-type items.

  Typography:
    - Label: text-xs text-muted (consistent with DetailField)
    - Value: text-sm font-medium text-primary (default)
    - Use `class` prop for color overrides only (e.g., status color)

  Usage:
    <MetadataSection items={[
      { label: 'Status', value: 'Draft', class: 'text-blue-600' },
      { label: 'Owner', value: 'alex.kim', type: 'person' },
      { label: 'Version', value: '1.0.1' },
      { label: 'Valid From', value: '2026-02-20' },
    ]} />
-->
<script lang="ts">
  import { Avatar } from '@sddp/shell';

  export interface MetadataItem {
    label: string;
    value: string;
    /** Item type: 'text' (default) or 'person' (renders Avatar + name) */
    type?: 'text' | 'person';
    /** Avatar URL for person type (color:#hex, preset:id, image URL) */
    avatarUrl?: string | null;
    /** Optional CSS class for value color override (e.g., status color). Do NOT pass font-weight here. */
    class?: string;
  }

  interface Props {
    items: MetadataItem[];
    class?: string;
  }

  let {
    items,
    class: className = '',
  }: Props = $props();
</script>

{#if items.length > 0}
  <dl class="grid grid-cols-2 gap-x-4 gap-y-3 {className}">
    {#each items as item (item.label)}
      <div>
        <dt class="text-xs text-[var(--color-text-muted)]">{item.label}</dt>
        <dd class="mt-1">
          {#if item.type === 'person' && item.value}
            <span class="inline-flex items-center gap-1.5 text-sm font-medium text-[var(--color-text-primary)]">
              <Avatar name={item.value} avatarUrl={item.avatarUrl} size="xs" />
              {item.value}
            </span>
          {:else}
            <span class="text-sm font-medium {item.class || 'text-[var(--color-text-primary)]'}">{item.value}</span>
          {/if}
        </dd>
      </div>
    {/each}
  </dl>
{/if}
