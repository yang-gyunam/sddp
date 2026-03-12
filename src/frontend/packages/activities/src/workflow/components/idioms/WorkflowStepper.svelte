<!--
  WorkflowStepper Component
  Responsive workflow progress indicator
  Single row by default, wraps with indent marker when container is narrow
-->
<script lang="ts">
  import { Icon } from '@sddp/ui';
  import { SvelteSet } from 'svelte/reactivity';
  import { untrack } from 'svelte';

  type StepStatus = 'complete' | 'current' | 'upcoming';

  export interface WorkflowStep {
    label: string;
    status: StepStatus;
    description?: string;
  }

  interface Props {
    steps?: WorkflowStep[];
    class?: string;
  }

  let { steps = [], class: className = '' }: Props = $props();

  const statusStyles: Record<StepStatus, { bg: string; text: string; border: string }> = {
    complete: {
      bg: 'bg-[var(--color-success-500)]',
      text: 'text-white',
      border: 'border-[var(--color-success-500)]',
    },
    current: {
      bg: 'bg-[var(--color-accent-primary)]',
      text: 'text-white',
      border: 'border-[var(--color-accent-primary)]',
    },
    upcoming: {
      bg: 'bg-[var(--color-surface-200)]',
      text: 'text-[var(--color-text-tertiary)]',
      border: 'border-[var(--color-border-secondary)]',
    },
  };

  let containerEl = $state<HTMLDivElement | null>(null);
  let stepEls = $state<(HTMLDivElement | null)[]>([]);
  const wrapStarts = new SvelteSet<number>();

  function detectWraps(): void {
    if (stepEls.length < 2 || !stepEls[0]) return;
    const newWraps = new SvelteSet<number>();
    let prevTop = stepEls[0].getBoundingClientRect().top;
    for (let i = 1; i < stepEls.length; i++) {
      const el = stepEls[i];
      if (!el) continue;
      const top = el.getBoundingClientRect().top;
      if (top > prevTop + 2) {
        newWraps.add(i);
      }
      prevTop = top;
    }
    const changed =
      newWraps.size !== wrapStarts.size ||
      [...newWraps].some((idx) => !wrapStarts.has(idx));
    if (changed) {
      wrapStarts.clear();
      for (const idx of newWraps) {
        wrapStarts.add(idx);
      }
    }
  }

  $effect(() => {
    if (!containerEl) return;
    const observer = new ResizeObserver(() => untrack(() => detectWraps()));
    observer.observe(containerEl);
    untrack(() => detectWraps());
    return () => observer.disconnect();
  });
</script>

{#snippet stepCircle(step: WorkflowStep, stepNumber: number)}
  {@const style = statusStyles[step.status]}
  <div
    class="w-6 h-6 rounded-full border flex items-center justify-center text-[0.625rem] font-semibold flex-shrink-0 {style.bg} {style.text} {style.border}"
    title={step.description ?? step.label}
  >
    {#if step.status === 'complete'}
      <Icon name="check" size="xs" />
    {:else}
      {stepNumber}
    {/if}
  </div>
{/snippet}

<div bind:this={containerEl} class="flex items-center flex-wrap gap-x-2 gap-y-1 {className}">
  {#each steps as step, i (step.label)}
    <div bind:this={stepEls[i]} class="flex items-center gap-2 py-1">
      {#if i > 0}
        {#if wrapStarts.has(i)}
          <Icon name="indent" size="xs" class="text-[var(--color-text-tertiary)]" />
        {:else}
          <Icon name="arrow-right" size="xs" class="text-[var(--color-text-tertiary)]" />
        {/if}
      {/if}
      {@render stepCircle(step, i + 1)}
      <span class="text-xs font-medium text-[var(--color-text-secondary)] whitespace-nowrap">
        {step.label}
      </span>
    </div>
  {/each}
</div>
