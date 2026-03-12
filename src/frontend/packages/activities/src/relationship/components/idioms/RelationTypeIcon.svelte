<!--
  RelationTypeIcon Component
  Displays an icon for a relationship type with optional tooltip
-->
<script lang="ts">
  import { Icon } from '@sddp/ui';
  import type { RelationType } from '../../types';
  import { RELATION_TYPE_STYLES } from '../../types';

  interface Props {
    type: RelationType;
    size?: 'sm' | 'md' | 'lg';
    showTooltip?: boolean;
    class?: string;
  }

  let {
    type,
    size = 'md',
    showTooltip = true,
    class: className = '',
  }: Props = $props();

  const style = $derived(RELATION_TYPE_STYLES[type]);
  const iconSize = $derived(size === 'sm' ? 'sm' : size === 'lg' ? 'lg' : 'md');
</script>

<span
  class="inline-flex items-center justify-center {style.color} {className}"
  title={showTooltip ? style.description : undefined}
>
  <Icon name={style.icon} size={iconSize} />
</span>
