<script lang="ts">
  import type { TypographyVariant } from '../types';

  interface Props {
    as?: 'h1' | 'h2' | 'h3' | 'h4' | 'h5' | 'h6' | 'p' | 'span' | 'div' | 'label';
    variant?: TypographyVariant;
    color?: 'primary' | 'secondary' | 'tertiary' | 'inherit';
    class?: string;
    children?: import('svelte').Snippet;
  }

  let {
    as = 'p',
    variant = 'body1',
    color = 'primary',
    class: className = '',
    children,
  }: Props = $props();

  const variantClasses: Record<TypographyVariant, string> = {
    h1: 'text-4xl font-bold leading-tight tracking-tight',
    h2: 'text-3xl font-bold leading-tight tracking-tight',
    h3: 'text-2xl font-semibold leading-snug',
    h4: 'text-xl font-semibold leading-snug',
    h5: 'text-lg font-medium leading-normal',
    h6: 'text-base font-medium leading-normal',
    body1: 'text-base leading-relaxed',
    body2: 'text-sm leading-relaxed',
    caption: 'text-xs leading-normal',
    overline: 'text-xs uppercase tracking-wider font-medium',
  };

  const colorClasses: Record<string, string> = {
    primary: 'text-[var(--color-text-primary)]',
    secondary: 'text-[var(--color-text-secondary)]',
    tertiary: 'text-[var(--color-text-tertiary)]',
    inherit: 'text-inherit',
  };
</script>

{#if as === 'h1'}
  <h1 class="{variantClasses[variant]} {colorClasses[color]} {className}">
    {#if children}{@render children()}{/if}
  </h1>
{:else if as === 'h2'}
  <h2 class="{variantClasses[variant]} {colorClasses[color]} {className}">
    {#if children}{@render children()}{/if}
  </h2>
{:else if as === 'h3'}
  <h3 class="{variantClasses[variant]} {colorClasses[color]} {className}">
    {#if children}{@render children()}{/if}
  </h3>
{:else if as === 'h4'}
  <h4 class="{variantClasses[variant]} {colorClasses[color]} {className}">
    {#if children}{@render children()}{/if}
  </h4>
{:else if as === 'h5'}
  <h5 class="{variantClasses[variant]} {colorClasses[color]} {className}">
    {#if children}{@render children()}{/if}
  </h5>
{:else if as === 'h6'}
  <h6 class="{variantClasses[variant]} {colorClasses[color]} {className}">
    {#if children}{@render children()}{/if}
  </h6>
{:else if as === 'span'}
  <span class="{variantClasses[variant]} {colorClasses[color]} {className}">
    {#if children}{@render children()}{/if}
  </span>
{:else if as === 'div'}
  <div class="{variantClasses[variant]} {colorClasses[color]} {className}">
    {#if children}{@render children()}{/if}
  </div>
{:else if as === 'label'}
  <label class="{variantClasses[variant]} {colorClasses[color]} {className}">
    {#if children}{@render children()}{/if}
  </label>
{:else}
  <p class="{variantClasses[variant]} {colorClasses[color]} {className}">
    {#if children}{@render children()}{/if}
  </p>
{/if}
