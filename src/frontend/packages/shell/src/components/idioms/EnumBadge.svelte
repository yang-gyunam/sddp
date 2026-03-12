<!--
  EnumBadge - Generic badge component for displaying enum-based status/type
  Use this component as a base for domain-specific badges (SpecStatusBadge, TaskStatusBadge, etc.)
-->
<script lang="ts" generics="T extends string">
  import { Icon } from '@sddp/ui';
  import type { IconSize } from '@sddp/ui';
  import type { BadgeStyleConfig, BadgeSize, BadgeDisplayMode, BadgeShape } from '../../types/badge.types';

  let {
    value,
    styles,
    size = 'md',
    displayMode = 'full',
    shape = 'rounded',
    class: className = '',
    showTooltip = true,
  }: {
    /** The enum value to display */
    value: T;
    /** Style mapping for all possible enum values */
    styles: Record<T, BadgeStyleConfig>;
    /** Size of the badge */
    size?: BadgeSize;
    /** What to display: full (icon + label), icon-only, or label-only */
    displayMode?: BadgeDisplayMode;
    /** Badge shape */
    shape?: BadgeShape;
    /** Additional CSS classes */
    class?: string;
    /** Whether to show tooltip with description (if available) */
    showTooltip?: boolean;
  } = $props();

  const style = $derived(styles[value]);

  // Support both textColor and color properties (domain styles use different naming)
  const textColorClass = $derived(style?.textColor ?? style?.color ?? '');

  const sizeClasses = $derived<string>(
    {
      sm: 'px-1.5 py-0.5 text-xs gap-0.5',
      md: 'px-2 py-1 text-sm gap-1',
      lg: 'px-3 py-1.5 text-base gap-1.5',
    }[size]
  );

  const iconSizeMap: Record<BadgeSize, IconSize> = {
    sm: 'xs',
    md: 'sm',
    lg: 'md',
  };

  const iconSize = $derived<IconSize>(iconSizeMap[size]);

  const showIcon = $derived(displayMode !== 'label-only' && style?.icon);
  const showLabel = $derived(displayMode !== 'icon-only');
</script>

{#if style}
  <span
    class="inline-flex items-center font-medium border {shape} {style.bgColor} {style.borderColor} {textColorClass} {sizeClasses} {className}"
    title={showTooltip && style.description ? style.description : undefined}
  >
    {#if showIcon}
      <Icon name={style.icon!} size={iconSize} />
    {/if}
    {#if showLabel}
      <span>{style.label}</span>
    {/if}
  </span>
{/if}
