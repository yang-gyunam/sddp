<!-- Section: SpecDetailGenerationTab — Specs > SpecDetailView -->
<script lang="ts">
  import { Button, Checkbox, Icon, Spinner } from '@sddp/ui';
  import type { Artifact } from '../../../../artifact/types';
  import type { SpecStatus } from '../../../types';

  type GenerationOption = {
    key: 'backend' | 'frontend' | 'tests' | 'docs';
    label: string;
    description: string;
  };

  interface Props {
    specStatus: SpecStatus;
    generationOptions: GenerationOption[];
    selectedGeneration: Set<GenerationOption['key']>;
    /** False until a real generation path exists. Nothing here may report success while false. */
    generationAvailable: boolean;
    artifacts: Artifact[];
    artifactsLoading: boolean;
    artifactsError: string | null;
    onStartGeneration: () => void | Promise<void>;
    onToggleGenerationOption: (option: GenerationOption['key']) => void;
  }

  let {
    specStatus,
    generationOptions,
    selectedGeneration,
    generationAvailable,
    artifacts,
    artifactsLoading,
    artifactsError,
    onStartGeneration,
    onToggleGenerationOption,
  }: Props = $props();

  const canGenerate = $derived(generationAvailable && specStatus === 'Locked');
</script>

<div
  class="rounded-lg border border-[var(--color-border-secondary)] bg-[var(--color-surface-50)] p-4 space-y-3"
>
  <div class="flex items-start justify-between gap-3">
    <div>
      <h3 class="text-sm font-medium text-[var(--color-text-secondary)]">Artifact Generation</h3>
      <p class="text-xs text-[var(--color-text-muted)]">
        {#if generationAvailable}
          Generate code, UI, tests, and docs from the locked spec.
        {:else}
          Generating code, UI, tests, and docs from a locked spec is not available yet.
        {/if}
      </p>
    </div>
    <Button variant="secondary" size="sm" onclick={onStartGeneration} disabled={!canGenerate}>
      <Icon name="cpu" size="sm" />
      Generate
    </Button>
  </div>

  {#if !generationAvailable}
    <div
      class="flex items-start gap-2 rounded border border-[var(--color-border-secondary)] bg-[var(--color-surface-100)] px-3 py-2"
      data-testid="generation-unavailable"
    >
      <Icon name="info" size="sm" />
      <p class="text-xs text-[var(--color-text-secondary)]">
        Generation tools are not included in this installation. Contact your deployment operator
        to generate artifacts using separately managed tooling. This screen shows recorded results;
        selecting outputs does not start generation.
      </p>
    </div>
  {/if}

  <div class="grid grid-cols-2 gap-2 text-xs">
    {#each generationOptions as option (option.key)}
      <Button
        variant="unstyled"
        class="flex items-start gap-2 px-2 py-2 rounded border text-left transition-colors
          {selectedGeneration.has(option.key)
          ? 'bg-[var(--color-accent-primary)]/10 border-[var(--color-accent-primary)]/30'
          : 'bg-[var(--color-surface-100)] border-[var(--color-border-secondary)] hover:bg-[var(--color-surface-200)]'}"
        onclick={() => onToggleGenerationOption(option.key)}
        disabled={specStatus !== 'Locked'}
      >
        <div class="pointer-events-none mt-0.5">
          <Checkbox
            checked={selectedGeneration.has(option.key)}
            disabled={specStatus !== 'Locked'}
            size="sm"
          />
        </div>
        <div>
          <div class="text-sm font-medium text-[var(--color-text-primary)]">{option.label}</div>
          <div class="text-xs text-[var(--color-text-tertiary)]">{option.description}</div>
        </div>
      </Button>
    {/each}
  </div>

  {#if artifactsLoading}
    <div class="flex-1 flex items-center justify-center">
      <Spinner size="lg" />
    </div>
  {:else if artifactsError}
    <div class="text-xs text-[var(--color-error-600)]">{artifactsError}</div>
  {:else if artifacts.length > 0}
    <div class="space-y-2">
      <span class="text-xs font-medium text-[var(--color-text-secondary)]">Existing Artifacts</span>
      <div class="flex flex-wrap gap-2">
        {#each artifacts as artifact (artifact.id)}
          <span
            class="px-2 py-1 rounded border border-[var(--color-border-secondary)] text-xs font-mono text-[var(--color-text-secondary)]"
          >
            {artifact.artifactPath}
          </span>
        {/each}
      </div>
    </div>
  {:else if specStatus !== 'Locked'}
    <div class="text-xs text-[var(--color-text-tertiary)]">
      Lock the spec to enable artifact generation.
    </div>
  {/if}
</div>
