<!--
  TaskPriorityBadge Component
  Displays task priority with icon and color
  Delegates to system-wide PriorityBadge from @sddp/shell
-->
<script lang="ts">
  import { PriorityBadge } from '@sddp/shell';
  import type { Priority } from '@sddp/shell';
  import type { TaskPriority } from '../../types';

  interface Props {
    priority: TaskPriority;
    size?: 'sm' | 'md';
    showLabel?: boolean;
    class?: string;
  }

  let { priority, size = 'sm', class: className = '' }: Props = $props();
</script>

<PriorityBadge priority={priority as Priority} {size} class={className} />
