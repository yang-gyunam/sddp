<script lang="ts">
  /**
   * Spinner
   * Loading indicator with two variants:
   *   - ring (default): hollow circle spinner
   *   - onbrick: brand-themed 2x2 block animation (ci_onbrick)
   */

  type SpinnerSize = 'sm' | 'md' | 'lg' | 'xl';
  type SpinnerVariant = 'ring' | 'onbrick';

  interface Props {
    size?: SpinnerSize;
    variant?: SpinnerVariant;
    class?: string;
  }

  let { size = 'md', variant = 'ring', class: className = '' }: Props = $props();

  // Ring variant sizing (px)
  const ringSizeMap: Record<SpinnerSize, number> = { sm: 16, md: 24, lg: 32, xl: 48 };
  const ringStrokeMap: Record<SpinnerSize, number> = { sm: 2, md: 2.5, lg: 3, xl: 4 };
  const ringPx = $derived(ringSizeMap[size]);
  const ringStroke = $derived(ringStrokeMap[size]);
  const ringRadius = $derived((ringPx - ringStroke) / 2);
  const ringCircumference = $derived(2 * Math.PI * ringRadius);

  // OnBrick variant sizing (px)
  const obWidthMap: Record<SpinnerSize, number> = { sm: 16, md: 24, lg: 32, xl: 48 };
  const obW = $derived(obWidthMap[size]);
  const obH = $derived(Math.round(obW * 5 / 7));
  const obBlockW = $derived(obW / 2);
  const obBlockH = $derived(obH / 2);

  const obColors = {
    brown: '#694d4a',
    green: '#286b61',
    orange: '#e47e1c',
    red: '#ff0000',
  };
</script>

<svelte:head>
  <style>
    /* Ring spinner */
    @keyframes sddp-ring-spin {
      to { transform: rotate(360deg); }
    }

    /* OnBrick animations */
    @keyframes sddp-b3 {
      0%, 6%    { opacity: 0; }
      8%, 75%   { opacity: 1; }
      85%, 100% { opacity: 0; }
    }
    @keyframes sddp-b1 {
      0%, 8%    { opacity: 0; transform: translateY(100%); }
      10%       { opacity: 1; transform: translateY(100%); }
      22%, 75%  { opacity: 1; transform: translateY(0); }
      85%, 100% { opacity: 0; transform: translateY(0); }
    }
    @keyframes sddp-b4 {
      0%, 18%   { opacity: 0; transform: translateX(-100%); }
      20%       { opacity: 1; transform: translateX(-100%); }
      32%, 75%  { opacity: 1; transform: translateX(0); }
      85%, 100% { opacity: 0; transform: translateX(0); }
    }
    @keyframes sddp-b2 {
      0%, 33%   { opacity: 0; transform: translate(-100%, 100%); }
      34%       { opacity: 1; transform: translate(-100%, 100%); }
      67%, 75%  { opacity: 1; transform: translate(0, 0); }
      85%, 100% { opacity: 0; transform: translate(0, 0); }
    }
  </style>
</svelte:head>

{#if variant === 'ring'}
  <svg
    class="spinner {className}"
    width={ringPx}
    height={ringPx}
    viewBox="0 0 {ringPx} {ringPx}"
    role="status"
    aria-label="Loading"
    style="flex-shrink:0;"
  >
    <!-- Track -->
    <circle
      cx={ringPx / 2}
      cy={ringPx / 2}
      r={ringRadius}
      fill="none"
      stroke="var(--color-border-secondary, #e5e7eb)"
      stroke-width={ringStroke}
    />
    <!-- Indicator -->
    <circle
      cx={ringPx / 2}
      cy={ringPx / 2}
      r={ringRadius}
      fill="none"
      stroke="var(--color-accent-primary, #6366f1)"
      stroke-width={ringStroke}
      stroke-linecap="round"
      stroke-dasharray="{ringCircumference * 0.7} {ringCircumference * 0.3}"
      style="animation: sddp-ring-spin 1s linear infinite; transform-origin: center;"
    />
  </svg>
{:else}
  <div
    class="spinner {className}"
    style="display:inline-grid; grid-template-columns:{obBlockW}px {obBlockW}px; grid-template-rows:{obBlockH}px {obBlockH}px; width:{obW}px; height:{obH}px; flex-shrink:0;"
    role="status"
    aria-label="Loading"
  >
    <div style="width:{obBlockW}px; height:{obBlockH}px; background:{obColors.green}; animation: sddp-b1 3s ease-out infinite; opacity:0;"></div>
    <div style="width:{obBlockW}px; height:{obBlockH}px; background:{obColors.red}; clip-path: polygon(70% 0, 100% 0, 100% 36%); animation: sddp-b2 3s ease-out infinite; opacity:0;"></div>
    <div style="width:{obBlockW}px; height:{obBlockH}px; background:{obColors.brown}; animation: sddp-b3 3s ease-out infinite; opacity:0; z-index:1;"></div>
    <div style="width:{obBlockW}px; height:{obBlockH}px; background:{obColors.orange}; animation: sddp-b4 3s ease-out infinite; opacity:0;"></div>
  </div>
{/if}
