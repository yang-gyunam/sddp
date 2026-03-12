<!--
  RadialProgress Component
  Circular progress indicator showing percentage

  Visual progression:
  0%   → dot
  20%  → small arc
  60%  → half circle
  80%  → almost complete
  100% → full circle
-->
<script lang="ts">
  interface Props {
    /** Progress value (0-100) */
    value: number;
    /** Size of the circle */
    size?: 'xs' | 'sm' | 'md' | 'lg';
    /** Stroke width */
    strokeWidth?: number;
    /** Show percentage text */
    showValue?: boolean;
    /** Color variant */
    variant?: 'default' | 'primary' | 'success' | 'warning' | 'danger';
    /** Additional CSS classes */
    class?: string;
  }

  let {
    value,
    size = 'md',
    strokeWidth = 3,
    showValue = false,
    variant = 'default',
    class: className = '',
  }: Props = $props();

  // Clamp value between 0 and 100
  const clampedValue = $derived(Math.max(0, Math.min(100, value)));

  // Size configurations
  const sizeConfigs: Record<NonNullable<Props['size']>, { diameter: number; fontSize: string }> = {
    xs: { diameter: 20, fontSize: 'text-[8px]' },
    sm: { diameter: 28, fontSize: 'text-[0.625rem]' },
    md: { diameter: 36, fontSize: 'text-xs' },
    lg: { diameter: 48, fontSize: 'text-sm' },
  };
  const sizeConfig = $derived(sizeConfigs[size]);

  // Calculate SVG parameters
  const radius = $derived((sizeConfig.diameter - strokeWidth) / 2);
  const circumference = $derived(2 * Math.PI * radius);
  const strokeDashoffset = $derived(circumference - (clampedValue / 100) * circumference);

  // Color based on variant
  const strokeColor = $derived.by(() => {
    const colors: Record<string, string> = {
      default: 'var(--color-text-primary)',
      primary: 'var(--color-accent-primary)',
      success: 'var(--color-success-500)',
      warning: 'var(--color-warning-500)',
      danger: 'var(--color-error-500)',
    };
    return colors[variant];
  });

  // Track color (background circle)
  const trackColor = 'var(--color-border-secondary)';
</script>

<div
  class="radial-progress {className}"
  style="width: {sizeConfig.diameter}px; height: {sizeConfig.diameter}px;"
  role="progressbar"
  aria-valuenow={clampedValue}
  aria-valuemin={0}
  aria-valuemax={100}
>
  <svg
    width={sizeConfig.diameter}
    height={sizeConfig.diameter}
    viewBox="0 0 {sizeConfig.diameter} {sizeConfig.diameter}"
  >
    <!-- Background track -->
    {#if clampedValue < 100}
      <circle
        cx={sizeConfig.diameter / 2}
        cy={sizeConfig.diameter / 2}
        r={radius}
        fill="none"
        stroke={trackColor}
        stroke-width={strokeWidth * 0.5}
        opacity="0.3"
      />
    {/if}

    <!-- Progress arc -->
    {#if clampedValue > 0}
      <circle
        cx={sizeConfig.diameter / 2}
        cy={sizeConfig.diameter / 2}
        r={radius}
        fill="none"
        stroke={strokeColor}
        stroke-width={strokeWidth}
        stroke-linecap="round"
        stroke-dasharray={circumference}
        stroke-dashoffset={strokeDashoffset}
        transform="rotate(-90 {sizeConfig.diameter / 2} {sizeConfig.diameter / 2})"
        class="progress-circle"
      />
    {:else}
      <!-- Dot for 0% -->
      <circle
        cx={sizeConfig.diameter / 2}
        cy={strokeWidth / 2 + 1}
        r={strokeWidth / 2}
        fill={strokeColor}
        opacity="0.5"
      />
    {/if}
  </svg>

  {#if showValue}
    <span class="radial-progress__value {sizeConfig.fontSize}">
      {Math.round(clampedValue)}%
    </span>
  {/if}
</div>

<style>
  .radial-progress {
    position: relative;
    display: inline-flex;
    align-items: center;
    justify-content: center;
  }

  .radial-progress svg {
    display: block;
  }

  .progress-circle {
    transition: stroke-dashoffset 300ms ease-in-out;
  }

  .radial-progress__value {
    position: absolute;
    font-weight: 600;
    color: var(--color-text-primary);
  }
</style>
