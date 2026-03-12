<!--
  TermStatusBadge Component
  Displays glossary term status with icon and color
  Wraps EnumBadge with glossary-specific styling
-->
<script lang="ts">
  import { EnumBadge } from '@sddp/shell';
  import type { GlossaryTermStatus } from '../../types';
  import { TERM_STATUS_STYLES } from '../../types';
  import type { BadgeStyleConfig } from '@sddp/shell';

  interface Props {
    status: GlossaryTermStatus;
    size?: 'sm' | 'md' | 'lg';
    showIcon?: boolean;
    class?: string;
  }

  let {
    status,
    size = 'md',
    showIcon = true,
    class: className = '',
  }: Props = $props();

  // Cast domain styles to BadgeStyleConfig (they are compatible)
  const styles = TERM_STATUS_STYLES as unknown as Record<GlossaryTermStatus, BadgeStyleConfig>;
</script>

<EnumBadge
  value={status}
  {styles}
  {size}
  displayMode={showIcon ? 'full' : 'label-only'}
  shape="rounded-full"
  class={className}
/>
