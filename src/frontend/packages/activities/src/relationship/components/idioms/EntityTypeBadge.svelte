<!--
  EntityTypeBadge Component
  Displays entity type with icon and color
  Wraps EnumBadge with relationship-specific styling
-->
<script lang="ts">
  import { EnumBadge } from '@sddp/shell';
  import type { EntityType } from '../../types';
  import { ENTITY_TYPE_STYLES } from '../../types';
  import type { BadgeStyleConfig, BadgeDisplayMode } from '@sddp/shell';

  interface Props {
    type: EntityType;
    size?: 'sm' | 'md' | 'lg';
    showLabel?: boolean;
    showIcon?: boolean;
    class?: string;
  }

  let { type, size = 'md', showLabel = true, showIcon = true, class: className = '' }: Props = $props();

  // Cast domain styles to BadgeStyleConfig (they are compatible)
  const styles = ENTITY_TYPE_STYLES as unknown as Record<EntityType, BadgeStyleConfig>;

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
  shape="rounded-md"
  class={className}
/>
