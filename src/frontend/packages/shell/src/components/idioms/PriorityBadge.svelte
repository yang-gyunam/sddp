<!--
  PriorityBadge - System-wide priority badge (L/M/H/U)
  Displays abbreviated priority with color and tooltip
-->
<script lang="ts">
  import EnumBadge from './EnumBadge.svelte';
  import type { Priority } from '../../types/priority.types';
  import { PRIORITY_STYLES } from '../../types/priority.types';
  import type { BadgeStyleConfig } from '../../types/badge.types';

  interface Props {
    priority: Priority;
    size?: 'sm' | 'md';
    class?: string;
  }

  let { priority, size = 'sm', class: className = '' }: Props = $props();

  const styles = PRIORITY_STYLES as Record<string, BadgeStyleConfig>;
</script>

<EnumBadge
  value={priority}
  {styles}
  {size}
  displayMode="label-only"
  shape="rounded-md"
  showTooltip
  class={className}
/>
