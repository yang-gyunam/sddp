<script lang="ts">
  import type { DividerOrientation } from '../types';

  interface Props {
    orientation?: DividerOrientation;
    label?: string;
    class?: string;
  }

  let {
    orientation = 'horizontal',
    label,
    class: className = '',
  }: Props = $props();
</script>

{#if orientation === 'horizontal'}
  {#if label}
    <div class="relative flex items-center {className}">
      <div class="flex-grow border-t border-[var(--color-border)]"></div>
      <span class="flex-shrink mx-3 text-xs text-[var(--color-text-tertiary)]">{label}</span>
      <div class="flex-grow border-t border-[var(--color-border)]"></div>
    </div>
  {:else}
    <hr class="border-t border-[var(--color-border)] {className}" />
  {/if}
{:else}
  <div
    class="inline-block h-full w-px bg-[var(--color-border)] {className}"
    role="separator"
    aria-orientation="vertical"
  ></div>
{/if}
