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

  type GenerationResult = {
    label: string;
    status: 'success' | 'failed';
    detail: string;
  };

  interface Props {
    specStatus: SpecStatus;
    generationOptions: GenerationOption[];
    selectedGeneration: Set<GenerationOption['key']>;
    generationProgress: number;
    generationRunning: boolean;
    generationResults: GenerationResult[];
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
    generationProgress,
    generationRunning,
    generationResults,
    artifacts,
    artifactsLoading,
    artifactsError,
    onStartGeneration,
    onToggleGenerationOption,
  }: Props = $props();
</script>

<div class="rounded-lg border border-[var(--color-border-secondary)] bg-[var(--color-surface-50)] p-4 space-y-3">
  <div class="flex items-start justify-between gap-3">
    <div>
      <h3 class="text-sm font-medium text-[var(--color-text-secondary)]">Artifact Generation</h3>
      <p class="text-xs text-[var(--color-text-muted)]">
        Generate code, UI, tests, and docs from the locked spec.
      </p>
    </div>
    <Button
      variant="secondary"
      size="sm"
      onclick={onStartGeneration}
      disabled={specStatus !== 'Locked' || generationRunning}
    >
      {#if generationRunning}
        <Spinner size="lg" />
        Generating
      {:else}
        <Icon name="cpu" size="sm" />
        Generate
      {/if}
    </Button>
  </div>

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

  {#if generationRunning || generationProgress > 0}
    <div>
      <div class="flex items-center justify-between text-xs text-[var(--color-text-tertiary)] mb-1">
        <span>Progress</span>
        <span>{generationProgress}%</span>
      </div>
      <div class="h-2 rounded-full bg-[var(--color-surface-200)] overflow-hidden">
        <div
          class="h-full bg-[var(--color-accent-primary)] transition-all"
          style="width: {generationProgress}%"
        ></div>
      </div>
    </div>
  {/if}

  {#if generationResults.length > 0}
    <div class="space-y-2">
      <span class="text-xs font-medium text-[var(--color-text-secondary)]">Generation Results</span>
      {#each generationResults as result (result.label)}
        <div class="flex items-center justify-between text-xs px-2 py-1 rounded bg-[var(--color-surface-100)] border border-[var(--color-border-secondary)]">
          <span class="font-mono text-[var(--color-text-primary)]">{result.label}</span>
          <span class={result.status === 'success' ? 'text-[var(--color-success-600)]' : 'text-[var(--color-error-600)]'}>
            {result.status === 'success' ? 'Success' : 'Failed'}
          </span>
        </div>
      {/each}
    </div>
  {/if}

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
          <span class="px-2 py-1 rounded border border-[var(--color-border-secondary)] text-xs font-mono text-[var(--color-text-secondary)]">
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
