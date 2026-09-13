<!-- Section: ArtifactReviewPanel — the generated file next to what it was supposed to be -->
<script lang="ts">
  import { untrack } from 'svelte';
  import { Icon, Spinner } from '@sddp/ui';
  import { CodeEditor, Modal, formatFileSize } from '@sddp/shell';
  import { getArtifactDescriptor, getArtifactFileContent } from '../../services';
  import type { ArtifactDescriptor, ArtifactFileContent } from '../../types';
  import { ARTIFACT_STATUS_STYLES, getFileName } from '../../types';

  interface Props {
    open?: boolean;
    artifactId: string | null;
    tenantId: string;
    projectId: string;
    onClose?: () => void;
  }

  let { open = false, artifactId, tenantId, projectId, onClose }: Props = $props();

  let file = $state<ArtifactFileContent | null>(null);
  let descriptor = $state<ArtifactDescriptor | null>(null);
  let loading = $state(false);
  let fileMissing = $state(false);
  let error = $state<string | null>(null);

  // A generation counter discards responses from a previous artifact: without it, a slow
  // first request can land after a faster second one and overwrite it.
  let requestGeneration = 0;

  async function load(id: string, tenant: string, project: string) {
    const generation = ++requestGeneration;
    loading = true;
    error = null;
    fileMissing = false;
    file = null;
    descriptor = null;

    // The descriptor is projected from the server's records and always resolves; the file
    // may legitimately be absent, which is a state to show rather than an error.
    const [descriptorResult, fileResult] = await Promise.allSettled([
      getArtifactDescriptor(tenant, project, id),
      getArtifactFileContent(tenant, project, id),
    ]);

    if (generation !== requestGeneration) return;

    if (descriptorResult.status === 'fulfilled') {
      descriptor = descriptorResult.value;
    } else {
      error = 'Could not load the descriptor.';
    }

    if (fileResult.status === 'fulfilled') {
      file = fileResult.value;
    } else {
      fileMissing = true;
    }

    loading = false;
  }

  $effect(() => {
    const id = artifactId;
    const isOpen = open;

    untrack(() => {
      if (!isOpen || !id || !tenantId || !projectId) {
        requestGeneration++;
        return;
      }
      void load(id, tenantId, projectId);
    });
  });

  const statusStyle = $derived(
    file ? (ARTIFACT_STATUS_STYLES[file.status] ?? ARTIFACT_STATUS_STYLES.Unverified) : null
  );
  const title = $derived(file ? getFileName(file.path) : 'Artifact review');
</script>

<Modal {open} {title} size="xl" {onClose}>
  {#if loading}
    <div class="flex h-64 items-center justify-center">
      <Spinner />
    </div>
  {:else if error && !descriptor}
    <div
      class="flex h-64 flex-col items-center justify-center gap-2 text-[var(--color-text-secondary)]"
    >
      <Icon name="alert-circle" size="lg" />
      <p class="text-sm">{error}</p>
    </div>
  {:else}
    <div class="flex flex-col gap-4">
      {#if file && statusStyle}
        <div class="flex flex-wrap items-center gap-3 text-xs">
          <span
            class="inline-flex items-center gap-1 rounded px-2 py-1 {statusStyle.bgColor} {statusStyle.color}"
          >
            <Icon name={statusStyle.icon} size="xs" />
            {statusStyle.label}
          </span>
          <span class="font-mono text-[var(--color-text-tertiary)]">{file.path}</span>
          <span class="text-[var(--color-text-tertiary)]">{formatFileSize(file.sizeBytes)}</span>
        </div>
      {/if}

      <div class="grid gap-4 lg:grid-cols-2">
        <section class="flex min-w-0 flex-col gap-2">
          <h3 class="text-xs font-semibold text-[var(--color-text-secondary)] uppercase">
            Actual file
          </h3>
          {#if fileMissing}
            <div
              class="flex h-96 flex-col items-center justify-center gap-2 rounded border border-[var(--color-border)] text-[var(--color-text-secondary)]"
            >
              <Icon name="file-x" size="lg" />
              <p class="text-sm">This file is not present in the repository.</p>
            </div>
          {:else if file?.isBinary}
            <div
              class="flex h-96 flex-col items-center justify-center gap-2 rounded border border-[var(--color-border)] text-[var(--color-text-secondary)]"
            >
              <Icon name="file" size="lg" />
              <p class="text-sm">Binary file — {formatFileSize(file.sizeBytes)}</p>
            </div>
          {:else if file}
            <CodeEditor value={file.content ?? ''} language={file.language} height="24rem" />
          {/if}
        </section>

        <section class="flex min-w-0 flex-col gap-2">
          <h3 class="text-xs font-semibold text-[var(--color-text-secondary)] uppercase">
            Descriptor — what it should be
          </h3>
          {#if descriptor}
            <CodeEditor value={descriptor.yaml} language="yaml" height="24rem" />
          {/if}
        </section>
      </div>
    </div>
  {/if}
</Modal>
