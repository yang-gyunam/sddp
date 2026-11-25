<script lang="ts">
  import { Button } from '@sddp/ui';

  type ComposerMode = 'write' | 'preview';

  let { mode, onchange }: { mode: ComposerMode; onchange: (mode: ComposerMode) => void } = $props();
</script>

<div class="flex shrink-0 items-center" role="group" aria-label="Editor mode">
  {#each ['write', 'preview'] as const as option (option)}
    <Button
      type="button"
      variant="unstyled"
      aria-pressed={mode === option}
      onclick={() => onchange(option)}
      class="rounded px-2 py-1 text-xs {mode === option
        ? 'bg-[var(--color-surface-200)] text-[var(--color-text-primary)]'
        : 'text-[var(--color-text-secondary)] hover:bg-[var(--color-surface-100)]'}"
      >{option === 'write' ? 'Write' : 'Preview'}</Button
    >
  {/each}
</div>
