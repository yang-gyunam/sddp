<!--
  RelationshipBadge Component
  Displays relationship type with icon and color
  Wraps EnumBadge with relationship-specific styling
-->
<script lang="ts">
  import { EnumBadge } from '@sddp/shell';
  import type { RelationType } from '../../types';
  import { RELATION_TYPE_STYLES } from '../../types';
  import type { BadgeStyleConfig, BadgeDisplayMode } from '@sddp/shell';

  interface Props {
    type: RelationType;
    size?: 'sm' | 'md' | 'lg';
    showLabel?: boolean;
    showIcon?: boolean;
    class?: string;
  }

  let { type, size = 'md', showLabel = true, showIcon = true, class: className = '' }: Props = $props();

  // Cast domain styles to BadgeStyleConfig (they are compatible)
  const styles = RELATION_TYPE_STYLES as unknown as Record<RelationType, BadgeStyleConfig>;

  // Derive displayMode from showIcon and showLabel
  const displayMode = $derived<BadgeDisplayMode>(
    showIcon && showLabel ? 'full' : showIcon ? 'icon-only' : 'label-only'
  );
</script>

<EnumBadge
  value={type}
  {styles}
  {size}
  {displayMode}
  shape="rounded-full"
  showTooltip={true}
  class={className}
/>
