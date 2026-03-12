<!--
  RequirementLevelBadge Component
  Displays a colored badge for requirement level (A/B/C)
-->
<script lang="ts">
  import type { RequirementLevel } from '../../types';
  import { REQUIREMENT_LEVEL_STYLES } from '../../types';

  interface Props {
    level: RequirementLevel;
    size?: 'sm' | 'md';
    showIcon?: boolean;
    class?: string;
  }

  let { level, size = 'sm', class: className = '' }: Props = $props();

  const style = $derived(REQUIREMENT_LEVEL_STYLES[level]);
  const sizeClasses = $derived(
    size === 'sm' ? 'text-xs px-1.5 py-0.5' : 'text-sm px-2 py-1'
  );
</script>

<span
  class="inline-flex items-center font-medium rounded {style.bgColor} {style.textColor} border {style.borderColor} {sizeClasses} {className}"
>
  {level}
</span>
