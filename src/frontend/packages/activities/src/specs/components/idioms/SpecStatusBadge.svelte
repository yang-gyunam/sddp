<!--
  SpecStatusBadge Component
  Displays a colored badge for spec status
  Wraps EnumBadge with spec-specific styling
-->
<script lang="ts">
  import { EnumBadge } from '@sddp/shell';
  import type { SpecStatus } from '../../types';
  import { SPEC_STATUS_STYLES } from '../../types';
  import type { BadgeStyleConfig } from '@sddp/shell';

  interface Props {
    status: SpecStatus;
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
  const styles = SPEC_STATUS_STYLES as unknown as Record<SpecStatus, BadgeStyleConfig>;
</script>

<EnumBadge
  value={status}
  {styles}
  {size}
  displayMode={showIcon ? 'full' : 'label-only'}
  class={className}
/>
