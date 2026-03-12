<!--
  RequirementStatusBadge Component
  Displays a colored badge for requirement status
  Wraps EnumBadge with requirement-specific styling
-->
<script lang="ts">
  import { EnumBadge } from '@sddp/shell';
  import type { RequirementStatus } from '../../types';
  import { REQUIREMENT_STATUS_STYLES } from '../../types';
  import type { BadgeStyleConfig } from '@sddp/shell';

  interface Props {
    status: RequirementStatus;
    showIcon?: boolean;
    size?: 'sm' | 'md';
    class?: string;
  }

  let {
    status,
    showIcon = true,
    size = 'sm',
    class: className = '',
  }: Props = $props();

  // Cast domain styles to BadgeStyleConfig (they are compatible)
  const styles = REQUIREMENT_STATUS_STYLES as unknown as Record<RequirementStatus, BadgeStyleConfig>;
</script>

<EnumBadge
  value={status}
  {styles}
  {size}
  displayMode={showIcon ? 'full' : 'label-only'}
  class={className}
/>
