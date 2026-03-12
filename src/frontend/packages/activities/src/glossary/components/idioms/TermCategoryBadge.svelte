<!--
  TermCategoryBadge Component
  Displays glossary term category with icon and color
  Wraps EnumBadge with glossary-specific styling
-->
<script lang="ts">
  import { EnumBadge } from '@sddp/shell';
  import type { TermCategory } from '../../types';
  import { TERM_CATEGORY_STYLES } from '../../types';
  import type { BadgeStyleConfig } from '@sddp/shell';

  interface Props {
    category: TermCategory;
    size?: 'sm' | 'md' | 'lg';
    showIcon?: boolean;
    class?: string;
  }

  let {
    category,
    size = 'md',
    showIcon = true,
    class: className = '',
  }: Props = $props();

  // Cast domain styles to BadgeStyleConfig (they are compatible)
  const styles = TERM_CATEGORY_STYLES as unknown as Record<TermCategory, BadgeStyleConfig>;
</script>

<EnumBadge
  value={category}
  {styles}
  {size}
  displayMode={showIcon ? 'full' : 'label-only'}
  shape="rounded-full"
  class={className}
/>
