<!--
  TaskStatusBadge Component
  Displays task status with icon and color
  Wraps EnumBadge with task-specific styling
-->
<script lang="ts">
  import { EnumBadge } from '@sddp/shell';
  import type { TaskStatus } from '../../types';
  import { TASK_STATUS_STYLES } from '../../types';
  import type { BadgeStyleConfig } from '@sddp/shell';

  interface Props {
    status: TaskStatus;
    size?: 'sm' | 'md';
    showLabel?: boolean;
    class?: string;
  }

  let { status, size = 'sm', showLabel = true, class: className = '' }: Props = $props();

  // Cast domain styles to BadgeStyleConfig (they are compatible)
  const styles = TASK_STATUS_STYLES as unknown as Record<TaskStatus, BadgeStyleConfig>;
</script>

<EnumBadge
  value={status}
  {styles}
  {size}
  displayMode={showLabel ? 'full' : 'icon-only'}
  shape="rounded-md"
  class={className}
/>
