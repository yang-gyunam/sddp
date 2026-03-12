<!--
  TraceGraphLegend Component
  Displays legend for trace graph showing relationship types and entity types
-->
<script lang="ts">
  import { Icon } from '@sddp/ui';
  import type { RelationType, EntityType } from '../../types';
  import {
    RELATION_TYPE_STYLES,
    ENTITY_TYPE_STYLES,
    PRIMARY_FLOW_STYLES,
  } from '../../types';

  interface Props {
    showRelationTypes?: boolean;
    showEntityTypes?: boolean;
    showPrimaryFlow?: boolean;
    compact?: boolean;
    class?: string;
  }

  let {
    showRelationTypes = true,
    showEntityTypes = true,
    showPrimaryFlow = false,
    compact = false,
    class: className = '',
  }: Props = $props();

  const relationTypes: RelationType[] = [
    'DependsOn',
    'Implements',
    'Extends',
    'Supersedes',
    'EvolvesFrom',
    'ConflictsWith',
    'Replaces',
    'Affects',
  ];

  const entityTypes: EntityType[] = ['Spec', 'Requirement', 'Conversation', 'GlossaryTerm'];
</script>

<div class="text-xs {className}">
  {#if showPrimaryFlow}
    <div class="mb-3">
      <div class="font-semibold text-[var(--color-text-tertiary)] uppercase tracking-wider mb-2">
        Primary Flow
      </div>
      <div class="flex flex-wrap gap-2">
        {#each Object.entries(PRIMARY_FLOW_STYLES) as [_key, style] (_key)}
          <div class="flex items-center gap-1.5 {compact ? '' : 'px-2 py-1 rounded bg-[var(--color-bg-tertiary)]'}">
            <Icon name={style.icon} size="sm" class={style.color} />
            <span class="text-[var(--color-text-secondary)]">{style.label}</span>
          </div>
        {/each}
      </div>
    </div>
  {/if}

  {#if showRelationTypes}
    <div class="mb-3">
      <div class="font-semibold text-[var(--color-text-tertiary)] uppercase tracking-wider mb-2">
        Relationship Types
      </div>
      <div class="grid {compact ? 'grid-cols-2' : 'grid-cols-2 lg:grid-cols-4'} gap-1.5">
        {#each relationTypes as type (type)}
          {@const style = RELATION_TYPE_STYLES[type]}
          <div class="flex items-center gap-1.5 {compact ? '' : 'px-2 py-1 rounded ' + style.bgColor}">
            <Icon name={style.icon} size="sm" class={style.color} />
            <span class="text-[var(--color-text-secondary)] truncate">{style.label}</span>
          </div>
        {/each}
      </div>
    </div>
  {/if}

  {#if showEntityTypes}
    <div>
      <div class="font-semibold text-[var(--color-text-tertiary)] uppercase tracking-wider mb-2">
        Entity Types
      </div>
      <div class="flex flex-wrap gap-2">
        {#each entityTypes as type (type)}
          {@const style = ENTITY_TYPE_STYLES[type]}
          <div class="flex items-center gap-1.5 {compact ? '' : 'px-2 py-1 rounded ' + style.bgColor}">
            <Icon name={style.icon} size="sm" class={style.color} />
            <span class="text-[var(--color-text-secondary)]">{style.label}</span>
          </div>
        {/each}
      </div>
    </div>
  {/if}
</div>
