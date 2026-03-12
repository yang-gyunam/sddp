<!--
  AiAnalysisTypeBadge Component
  Displays a badge for AI analysis type
-->
<script lang="ts">
  import { Icon } from '@sddp/ui';
  import type { AiAnalysisType } from '../../types';
  import { AI_ANALYSIS_TYPE_STYLES } from '../../types';

  interface Props {
    type: AiAnalysisType;
    size?: 'sm' | 'md' | 'lg';
    showLabel?: boolean;
    class?: string;
  }

  let {
    type,
    size = 'md',
    showLabel = true,
    class: className = '',
  }: Props = $props();

  const style = $derived(AI_ANALYSIS_TYPE_STYLES[type]);
  const iconSize = $derived(size === 'sm' ? 'sm' : size === 'lg' ? 'lg' : 'md');
  const textSize = $derived(size === 'sm' ? 'text-xs' : size === 'lg' ? 'text-base' : 'text-sm');
</script>

<span
  class="inline-flex items-center gap-1.5 {style.color} {className}"
  title={style.description}
>
  <Icon name={style.icon} size={iconSize} />
  {#if showLabel}
    <span class="{textSize} font-medium">{style.label}</span>
  {/if}
</span>
